export interface Product {
  id: string
  name: string
  description: string
  price: number
  imageUrl: string
  category: string
}

export interface CartItemInput {
  productId: string
  quantity: number
}

export interface PurchaseResponse {
  success: boolean
  message: string
}

export interface PetStoreApi {
  getProducts: () => Promise<Product[]>
  purchase: (items: CartItemInput[]) => Promise<PurchaseResponse>
}

type ApiMode = 'mock' | 'remote'

interface ApiConfig {
  mode: ApiMode
  baseUrl?: string
  purchaseUrl?: string
}

const MOCK_PRODUCTS: Product[] = [
  {
    id: 'chicken-coop-cleaner',
    name: 'Cozy Coop Cleaner',
    description: 'Keep your hens happy with a lavender-scented, pet-safe coop spray.',
    price: 14.99,
    imageUrl:
        'https://images.unsplash.com/photo-1573333744619-00d101e99133??auto=format&fit=crop&w=600&q=80',
    category: 'Chickens',
  },
  {
    id: 'turtle-terrarium-kit',
    name: 'Lagoon Terrarium Starter Kit',
    description: 'All-in-one habitat kit for small turtles with basking dock and LED lighting.',
    price: 89.5,
    imageUrl:
      'https://images.unsplash.com/photo-1663907181190-6ed43256458d?auto=format&fit=crop&w=600&q=80',
    category: 'Turtles',
  },
  {
    id: 'catnip-toy-set',
    name: 'Feline Fiesta Catnip Toys',
    description: 'A trio of hand-stitched toys packed with organic catnip.',
    price: 22.0,
    imageUrl:
      'https://images.unsplash.com/photo-1518791841217-8f162f1e1131?auto=format&fit=crop&w=600&q=80',
    category: 'Cats',
  },
  {
    id: 'guinea-pig-salad',
    name: 'Garden Greens Salad Mix',
    description: 'Dried chamomile, carrot curls, and rose hips for guinea pigs and rabbits.',
    price: 11.75,
    imageUrl:
      'https://images.unsplash.com/photo-1612267168669-679c961c5b31?auto=format&fit=crop&w=600&q=80',
    category: 'Small Pets',
  },
  {
    id: 'dog-spa-shampoo',
    name: 'Tail Waggers Spa Shampoo',
    description: 'Oatmeal and aloe shampoo that soothes dry skin and keeps coats shiny.',
    price: 18.25,
    imageUrl:
      'https://images.unsplash.com/photo-1518717758536-85ae29035b6d?auto=format&fit=crop&w=600&q=80',
    category: 'Dogs',
  },
  {
    id: 'parakeet-playground',
    name: 'Skyline Play Tower',
    description: 'Colorful perches and bells designed to keep parakeets entertained for hours.',
    price: 32.4,
    imageUrl:
      'https://images.unsplash.com/photo-1652536122320-ca870caea2ae?auto=format&fit=crop&w=600&q=80',
    category: 'Birds',
  },
]

const delay = (ms: number) => new Promise((resolve) => setTimeout(resolve, ms))

const createMockApi = (): PetStoreApi => ({
  async getProducts() {
    await delay(300)
    return MOCK_PRODUCTS
  },
  async purchase(items) {
    await delay(500)

    if (items.length === 0) {
      return {
        success: false,
        message: 'Add at least one product before purchasing.',
      }
    }

    const success = Math.random() > 0.2

    if (success) {
      return {
        success: true,
        message: 'Thanks for shopping with the Cozy Critter Supply Co.!',
      }
    }

    return {
      success: false,
      message: 'The purchase could not be completed. Please try again.',
    }
  },
})

const joinUrl = (baseUrl: string, path: string) => {
  if (!baseUrl) {
    return path
  }

  const trimmedBase = baseUrl.endsWith('/') ? baseUrl.slice(0, -1) : baseUrl
  const trimmedPath = path.startsWith('/') ? path.slice(1) : path

  return `${trimmedBase}/${trimmedPath}`
}

const createRemoteApi = (baseUrl: string, purchaseUrl: string): PetStoreApi => ({
  async getProducts() {
    const response = await fetch(joinUrl(baseUrl, 'products'))

    if (!response.ok) {
      const details = await response.text().catch(() => '')
      throw new Error(details || 'Failed to load products.')
    }

    return (await response.json()) as Product[]
  },
  async purchase(items) {
    const response = await fetch(purchaseUrl, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ items }),
    })

    if (!response.ok) {
      const details = await response.text().catch(() => '')
      throw new Error(details || 'Purchase failed.')
    }

    return (await response.json()) as PurchaseResponse
  },
})

export const createPetStoreApi = (overrides: Partial<ApiConfig> = {}): PetStoreApi => {
  const mode = (overrides.mode ?? (import.meta.env.VITE_API_MODE as ApiMode)) || 'mock'
  const baseUrl = overrides.baseUrl ?? import.meta.env.VITE_API_BASE_URL
  const purchaseUrl = overrides.purchaseUrl ?? import.meta.env.VITE_PURCHASE_URL

  if (mode === 'remote') {
    if (baseUrl && purchaseUrl) {
      return createRemoteApi(baseUrl, purchaseUrl)
    }

    console.warn('PetStoreApi remote mode requires VITE_API_BASE_URL and VITE_PURCHASE_URL. Falling back to mock data.')
  }

  return createMockApi()
}
