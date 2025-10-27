import os
from typing import Any

from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
import httpx
from motor.motor_asyncio import AsyncIOMotorClient
from pymongo.errors import PyMongoError
from opentelemetry.instrumentation.httpx import HTTPXClientInstrumentor
from opentelemetry import trace
from opentelemetry.instrumentation.pymongo import PymongoInstrumentor

load_dotenv()

MONGO_CONNECTION_STRING = os.getenv(
    "MONGO_CONNECTION_STRING", "mongodb://localhost:27017"
)
PAYMENT_API_BASE_URL = os.getenv("PAYMENT_API_BASE_URL", "http://127.0.0.1:8080")
DATABASE_NAME = "petstore"
COLLECTION_NAME = "products"

app = FastAPI()

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


@app.on_event("startup")
async def connect_to_mongo() -> None:
    # Ping on startup so we fail fast if MongoDB is unreachable.
    app.state.mongo_client = AsyncIOMotorClient(MONGO_CONNECTION_STRING)
    try:
        await app.state.mongo_client.admin.command("ping")
    except Exception as exc:
        # Close the client to avoid dangling connections in failure scenarios.
        app.state.mongo_client.close()
        raise exc

    app.state.products_collection = app.state.mongo_client[DATABASE_NAME][
        COLLECTION_NAME
    ]

    if not hasattr(app.state, "http_client"):
        app.state.http_client = httpx.AsyncClient(
            base_url=PAYMENT_API_BASE_URL, timeout=5.0
        )


@app.on_event("shutdown")
async def close_mongo() -> None:
    client = getattr(app.state, "mongo_client", None)
    if client:
        client.close()

    http_client = getattr(app.state, "http_client", None)
    if http_client:
        await http_client.aclose()


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
