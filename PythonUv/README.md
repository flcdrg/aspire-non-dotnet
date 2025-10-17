# PythonUv API

This FastAPI service exposes product data backed by MongoDB and powers the local frontend.

## Prerequisites

- Python 3.13+
- MongoDB running locally or accessible via connection string

## Installation

```bash
uv sync
```

## Configuration

- `MONGO_CONNECTION_STRING` (optional): defaults to `mongodb://localhost:27017`. Override via environment variable or a `.env` file alongside the service.
- `PAYMENT_API_BASE_URL` (optional): defaults to `http://127.0.0.1:8080`. Points at the Rust payment API.

The service connects to the `petstore` database and uses the `products` collection. Seed data with `mongodb/populate.ps1` if needed.

## Run Locally

```bash
uv run fastapi dev src/api
```

The `/products` endpoint returns all documents from the MongoDB collection with Mongo-specific `_id` fields removed.
