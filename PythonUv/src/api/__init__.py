import httpx
import opentelemetry.instrumentation.fastapi as otel_fastapi
import os
from . import telemetry
from contextlib import asynccontextmanager
from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from motor.motor_asyncio import AsyncIOMotorClient
from opentelemetry import trace
from opentelemetry.instrumentation.httpx import HTTPXClientInstrumentor
from opentelemetry.instrumentation.pymongo import PymongoInstrumentor
from pymongo.errors import PyMongoError
from typing import Any

load_dotenv()

MONGO_CONNECTION_STRING = os.getenv(
    "MONGO_CONNECTION_STRING", "mongodb://localhost:27017"
)
PAYMENT_API_BASE_URL = os.getenv("PAYMENT_API_BASE_URL", "http://127.0.0.1:8080")
DATABASE_NAME = "petstore"
COLLECTION_NAME = "products"

@asynccontextmanager
async def lifespan(app: FastAPI):
    telemetry.configure_opentelemetry()
    
    # Startup: connect to Mongo and prepare HTTP client
    app.state.mongo_client = AsyncIOMotorClient(MONGO_CONNECTION_STRING)
    try:
        await app.state.mongo_client.admin.command("ping")
    except Exception as exc:
        app.state.mongo_client.close()
        raise exc

    app.state.products_collection = app.state.mongo_client[DATABASE_NAME][
        COLLECTION_NAME
    ]

    if not hasattr(app.state, "http_client"):
        app.state.http_client = httpx.AsyncClient(
            base_url=PAYMENT_API_BASE_URL, timeout=5.0
        )

    # Yield control to the application
    try:
        yield
    finally:
        # Shutdown: close resources
        client = getattr(app.state, "mongo_client", None)
        if client:
            client.close()

        http_client = getattr(app.state, "http_client", None)
        if http_client:
            await http_client.aclose()


app = FastAPI(lifespan=lifespan)
otel_fastapi.FastAPIInstrumentor.instrument_app(app, exclude_spans=["send"])

# Instrument httpx for OpenTelemetry tracing
HTTPXClientInstrumentor().instrument()
PymongoInstrumentor().instrument()

tracer = trace.get_tracer(__name__)

# Allow all origins for development simplicity.
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

@app.get("/")
async def root():
    with tracer.start_as_current_span("root_endpoint") as root_span:
        root_span.set_attribute("custom.attribute", "value")

        async with httpx.AsyncClient() as client:
            r = await client.get("https://httpbin.org/delay/1")
            upstream_ms = r.elapsed.total_seconds() * 1000
            root_span.set_attribute("upstream.response_time", upstream_ms)
            return "Hello world"


@app.get("/products")
async def get_products():
    collection = getattr(app.state, "products_collection", None)
    if collection is None:
        raise HTTPException(
            status_code=500, detail="Product collection not initialized."
        )

    try:
        products = []
        async for document in collection.find():
            document.pop(
                "_id", None
            )  # Remove MongoDB ObjectId before returning to clients.
            products.append(document)
        return products
    except PyMongoError as exc:
        raise HTTPException(
            status_code=500, detail=f"Failed to load products: {exc}"
        ) from exc


@app.post("/payments")
async def create_payment(payload: dict[str, Any]):
    total_amount = payload.get("total_amount")
    if not isinstance(total_amount, (int, float)):
        raise HTTPException(status_code=400, detail="total_amount must be a number.")

    if total_amount <= 0:
        raise HTTPException(
            status_code=400, detail="total_amount must be greater than zero."
        )

    http_client: httpx.AsyncClient
    if hasattr(app.state, "http_client"):
        http_client = app.state.http_client
    else:
        app.state.http_client = httpx.AsyncClient(
            base_url=PAYMENT_API_BASE_URL, timeout=5.0
        )
        http_client = app.state.http_client

    try:
        response = await http_client.post(
            "/payment", json={"total_amount": total_amount}
        )
        response.raise_for_status()
    except httpx.HTTPStatusError as exc:
        raise HTTPException(
            status_code=exc.response.status_code, detail=exc.response.text
        ) from exc
    except httpx.RequestError as exc:
        raise HTTPException(
            status_code=502, detail=f"Payment service unavailable: {exc}"
        ) from exc

    return response.json()
