# Cozy Critter Supply Company

A demo pet-supply storefront built with React 19, TypeScript, and Vite. Browse a curated product list, add items to your cart, and run a mock checkout flow that can be swapped for a live API when you are ready.

## Getting started

```bash
pnpm install
pnpm dev
```

Open the site at <http://localhost:5173>.

## API configuration

The application ships with an in-memory mock API so you can try it without additional services. To connect to a real backend, configure the following environment variables (e.g. in a `.env` file):

```env
VITE_API_MODE=remote
VITE_API_BASE_URL=https://your-api.example.com
VITE_PURCHASE_URL=https://your-api.example.com/purchase
```

- `VITE_API_BASE_URL` should return product data at `/products`.
- `VITE_PURCHASE_URL` should accept a `POST` with `{ items: [{ productId, quantity }] }` and respond with `{ success: boolean, message: string }`.

Omit `VITE_API_MODE` or set it to `mock` to fall back to the built-in dataset.

## Key files

- `src/services/productService.ts` – abstraction over the product and purchase endpoints with mock and remote implementations.
- `src/App.tsx` – main UI with catalog browsing, cart management, and checkout flow.
- `src/App.css` – layout and visual styling.
