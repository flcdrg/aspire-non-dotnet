# Cozy Critter Supply Company

A demo pet-supply storefront built with React 19, TypeScript, and Vite. Browse a curated product list, add items to your cart, and run a mock checkout flow that can be swapped for a live API when you are ready.

## Getting started

```bash
pnpm install
pnpm dev
```

Open the site at <http://localhost:5173>.

## API configuration

The application ships with an in-memory mock API so you can try it without additional services. To enable this set your .env file as follows:

```env
VITE_API_MODE=mock
```

To connect to a real backend, configure the following environment variables (e.g. in a `.env` file):

```env
VITE_API_BASE_URL=https://your-api.example.com
```

## Key files

- `src/services/productService.ts` – abstraction over the product and purchase endpoints with mock and remote implementations.
- `src/App.tsx` – main UI with catalog browsing, cart management, and checkout flow.
- `src/App.css` – layout and visual styling.
