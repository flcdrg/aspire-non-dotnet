# .Aspire with non .NET applications

Example using [Aspire](https://aspire.dev/) (formerly .NET Aspire) with non-.NET applications

## Manual startup

1. In [mongodb](./mongodb/), run `docker compose up -d`
2. The first time you do this, also run `./populate.ps` to load data into the database
3. In [PythonUv](./PythonUv/), run `uv run fastapi dev src/api`
4. In [RustPaymentApi](./RustPaymentApi/), run `cargo run`
5. In [web-vite-react](./web-vite-react/), run `pnpm dev`

## Incremental Aspire integration

Once Aspire is added, if you have the Aspire CLI installed you can run:

```bash
aspire run
```

Each increment is in its own branch and builds on the previous one:

- `main`: no Aspire integration
- `aspire-add-aspire`: Add empty Aspire AppHost project
- `aspire-add-mongo`: Add MongoDB container to Aspire
- `aspire-add-pythonuv`: Add PythonUv to Aspire
- `aspire-add-rust`: Add RustPaymentApi to Aspire
- `aspire-add-web`: Add web-vite-react web app to Aspire
- `aspire-dynamic-ports`: Use dynamic ports for all services in Aspire (no hardcoded ports)
- `aspire-add-opentelemetry`: Add OpenTelemetry tracing to all services in Aspire
- `aspire-add-nodejs`: Add a Node.js backend service to Aspire

## OpenTelemetry

Once OpenTelemetry is added, use the following to launch the Aspire AppHost (to avoid issues with self-signed dev certificates which can cause issues with some of the non-.NET services):

```bash
dotnet run --project ./AspireAppHost/AspireAppHost.csproj --launch-profile http
```
