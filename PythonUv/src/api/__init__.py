import os

from dotenv import load_dotenv
from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware
from motor.motor_asyncio import AsyncIOMotorClient
from pymongo.errors import PyMongoError

load_dotenv()

MONGO_CONNECTION_STRING = os.getenv("MONGO_CONNECTION_STRING", "mongodb://localhost:27017")
DATABASE_NAME = "petstore"
COLLECTION_NAME = "products"

app = FastAPI()

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

    app.state.products_collection = app.state.mongo_client[DATABASE_NAME][COLLECTION_NAME]


@app.on_event("shutdown")
async def close_mongo() -> None:
    client = getattr(app.state, "mongo_client", None)
    if client:
        client.close()


@app.get("/")
async def root():
    return "Hello world"


@app.get("/products")
async def get_products():
    collection = getattr(app.state, "products_collection", None)
    if collection is None:
        raise HTTPException(status_code=500, detail="Product collection not initialized.")

    try:
        products = []
        async for document in collection.find():
            document.pop("_id", None)  # Remove MongoDB ObjectId before returning to clients.
            products.append(document)
        return products
    except PyMongoError as exc:
        raise HTTPException(status_code=500, detail="Failed to load products.") from exc