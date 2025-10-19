# .NET Aspire with non .NET applications

Example using .NET Aspire with non-.NET applications

## Manual startup

1. In [mongodb](./mongodb/), run `docker compose up -d`
2. The first time you do this, also run `./populate.ps` to load data into the database
3. In [PythonUv](./PythonUv/), run `uv run fastapi dev src/api`
4. In [RustPaymentApi](./RustPaymentApi/), run `cargo run`
5. In [web-vite-react](./web-vite-react/), run `pnpm dev`
