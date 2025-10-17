import { useEffect, useMemo, useState } from 'react'
import './App.css'
import {
  createPetStoreApi,
  type Product,
  type PurchaseResponse,
} from './services/productService'

type CartItem = {
  product: Product
  quantity: number
}

type PurchaseAlert = {
  type: 'success' | 'error'
  text: string
}

function App() {
  const api = useMemo(() => createPetStoreApi(), [])

  const [products, setProducts] = useState<Product[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [cartItems, setCartItems] = useState<CartItem[]>([])
  const [isPurchasing, setIsPurchasing] = useState(false)
  const [purchaseAlert, setPurchaseAlert] = useState<PurchaseAlert | null>(
    null,
  )

  useEffect(() => {
    let isActive = true

    const loadProducts = async () => {
      setIsLoading(true)
      setLoadError(null)

      try {
        const data = await api.getProducts()

        if (isActive) {
          setProducts(data)
        }
      } catch (error) {
        const message =
          error instanceof Error ? error.message : 'Something went wrong.'

        if (isActive) {
          setLoadError(message)
        }
      } finally {
        if (isActive) {
          setIsLoading(false)
        }
      }
    }

    loadProducts()

    return () => {
      isActive = false
    }
  }, [api])

  const handleAddToCart = (product: Product) => {
    setCartItems((current) => {
      const existing = current.find((item) => item.product.id === product.id)

      if (existing) {
        return current.map((item) =>
          item.product.id === product.id
            ? { ...item, quantity: item.quantity + 1 }
            : item,
        )
      }

      return [...current, { product, quantity: 1 }]
    })

    setPurchaseAlert(null)
  }

  const handleRemoveFromCart = (productId: string) => {
    setCartItems((current) =>
      current.filter((item) => item.product.id !== productId),
    )
  }

  const subtotal = cartItems.reduce(
    (total, item) => total + item.product.price * item.quantity,
    0,
  )

  const totalItems = cartItems.reduce((total, item) => total + item.quantity, 0)

  const handlePurchase = async () => {
    setIsPurchasing(true)
    setPurchaseAlert(null)

    try {
      const response: PurchaseResponse = await api.purchase({
        items: cartItems.map((item) => ({
          productId: item.product.id,
          quantity: item.quantity,
        })),
        totalAmount: subtotal,
      })

      if (response.success) {
        setCartItems([])
        setPurchaseAlert({
          type: 'success',
          text: response.status
            ? `${response.message} (status: ${response.status})`
            : response.message,
        })
      } else {
        setPurchaseAlert({
          type: 'error',
          text: response.status
            ? `${response.message} (status: ${response.status})`
            : response.message,
        })
      }
    } catch (error) {
      const message =
        error instanceof Error ? error.message : 'Purchase failed. Please try again.'

      setPurchaseAlert({ type: 'error', text: message })
    } finally {
      setIsPurchasing(false)
    }
  }

  const isPurchaseDisabled = cartItems.length === 0 || isPurchasing

  return (
    <div className="app">
      <header className="hero">
        <div>
          <p className="hero-kicker">Cozy Critter Supply Co.</p>
          <h1>Pet-friendly goodies for every feather, fin, and fur.</h1>
          <p className="hero-subtitle">
            Stock up on curated treats and essentials for the pets that make life
            brighter.
          </p>
        </div>
      </header>

      <main className="content">
        <section className="catalog">
          <div className="section-header">
            <h2>Featured Products</h2>
            <span className="chip">{products.length} items</span>
          </div>

          {isLoading && <p className="status">Loading products...</p>}
          {loadError && (
            <p className="status status-error">Failed to load products: {loadError}</p>
          )}

          {!isLoading && !loadError && products.length === 0 && (
            <p className="status">No products available right now.</p>
          )}

          <div className="product-grid">
            {products.map((product) => (
              <article className="product-card" key={product.id}>
                <div className="product-image">
                  <img src={product.imageUrl} alt={product.name} loading="lazy" />
                </div>
                <div className="product-body">
                  <span className="product-category">{product.category}</span>
                  <h3>{product.name}</h3>
                  <p>{product.description}</p>
                </div>
                <div className="product-footer">
                  <span className="product-price">${product.price.toFixed(2)}</span>
                  <button onClick={() => handleAddToCart(product)}>Add to cart</button>
                </div>
              </article>
            ))}
          </div>
        </section>

        <aside className="cart">
          <h2>Your Cart</h2>

          {cartItems.length === 0 ? (
            <p className="cart-empty">Your cart is feeling lonely.</p>
          ) : (
            <ul className="cart-items">
              {cartItems.map((item) => (
                <li key={item.product.id} className="cart-item">
                  <div>
                    <h3>{item.product.name}</h3>
                    <p>
                      {item.quantity} × ${item.product.price.toFixed(2)}
                    </p>
                  </div>
                  <div className="cart-item-actions">
                    <span>
                      ${(item.product.price * item.quantity).toFixed(2)}
                    </span>
                    <button
                      className="link-button"
                      onClick={() => handleRemoveFromCart(item.product.id)}
                    >
                      Remove
                    </button>
                  </div>
                </li>
              ))}
            </ul>
          )}

          <div className="cart-summary">
            <span>{totalItems} item{totalItems === 1 ? '' : 's'}</span>
            <span className="cart-total">${subtotal.toFixed(2)}</span>
          </div>

          {purchaseAlert && (
            <p className={`purchase-alert ${purchaseAlert.type}`}>
              {purchaseAlert.text}
            </p>
          )}

          <button
            className="purchase-button"
            onClick={handlePurchase}
            disabled={isPurchaseDisabled}
          >
            {isPurchasing ? 'Processing...' : 'Purchase'}
          </button>
        </aside>
      </main>
    </div>
  )
}

export default App
