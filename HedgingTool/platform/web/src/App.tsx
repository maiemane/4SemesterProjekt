import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import './App.css'

type LoginResponse = {
  accessToken: string
  expiresAtUtc: string
  name: string
  email: string
  role: string
}

type AuthState = LoginResponse

const authStorageKey = 'hedging-tool.auth'

function loadAuthState(): AuthState | null {
  const rawState = window.sessionStorage.getItem(authStorageKey)

  if (!rawState) {
    return null
  }

  try {
    const state = JSON.parse(rawState) as AuthState
    const expiresAt = new Date(state.expiresAtUtc)

    if (!state.accessToken || Number.isNaN(expiresAt.getTime()) || expiresAt <= new Date()) {
      window.sessionStorage.removeItem(authStorageKey)
      return null
    }

    return state
  } catch {
    window.sessionStorage.removeItem(authStorageKey)
    return null
  }
}

function App() {
  const [authState, setAuthState] = useState<AuthState | null>(() => loadAuthState())
  const [path, setPath] = useState(() => window.location.pathname)

  useEffect(() => {
    const handlePopState = () => setPath(window.location.pathname)

    window.addEventListener('popstate', handlePopState)
    return () => window.removeEventListener('popstate', handlePopState)
  }, [])

  const isAuthenticated = useMemo(() => authState !== null, [authState])

  useEffect(() => {
    if (isAuthenticated && (path === '/' || path === '/login')) {
      window.history.replaceState(null, '', '/home')
      setPath('/home')
    }

    if (!isAuthenticated && path !== '/' && path !== '/login') {
      window.history.replaceState(null, '', '/login')
      setPath('/login')
    }
  }, [isAuthenticated, path])

  const handleLogin = (nextAuthState: AuthState) => {
    window.sessionStorage.setItem(authStorageKey, JSON.stringify(nextAuthState))
    setAuthState(nextAuthState)
    window.history.replaceState(null, '', '/home')
    setPath('/home')
  }

  const handleLogout = () => {
    window.sessionStorage.removeItem(authStorageKey)
    setAuthState(null)
    window.history.replaceState(null, '', '/login')
    setPath('/login')
  }

  if (authState === null) {
    return <LoginPage onLogin={handleLogin} />
  }

  return <HomePage user={authState} onLogout={handleLogout} />
}

function LoginPage({ onLogin }: { onLogin: (authState: AuthState) => void }) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setErrorMessage(null)

    if (!email.trim() || !password) {
      setErrorMessage('Email og password er påkrævet.')
      return
    }

    setIsSubmitting(true)

    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          email: email.trim(),
          password,
        }),
      })

      if (response.status === 401) {
        setErrorMessage('Login mislykkedes. Tjek email og password.')
        return
      }

      if (response.status === 400) {
        const message = await response.text()
        setErrorMessage(message || 'Ugyldig loginanmodning.')
        return
      }

      if (!response.ok) {
        setErrorMessage('Kunne ikke kontakte login-servicen.')
        return
      }

      const loginResponse = (await response.json()) as LoginResponse

      if (!loginResponse.accessToken || !loginResponse.expiresAtUtc) {
        setErrorMessage("Login-svaret fra API'et var ugyldigt.")
        return
      }

      onLogin(loginResponse)
    } catch {
      setErrorMessage("Kunne ikke oprette forbindelse til API'et. Tjek at API'et kører.")
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="login-shell">
      <section className="login-panel" aria-labelledby="login-title">
        <div className="login-heading">
          <p>Hedging Tool</p>
          <h1 id="login-title">Log ind</h1>
        </div>

        <form className="login-form" onSubmit={handleSubmit}>
          {errorMessage ? <div className="login-error">{errorMessage}</div> : null}

          <label className="login-field" htmlFor="email">
            <span>Email</span>
            <input
              autoComplete="email"
              id="email"
              name="email"
              onChange={(event) => setEmail(event.target.value)}
              type="email"
              value={email}
            />
          </label>

          <label className="login-field" htmlFor="password">
            <span>Password</span>
            <input
              autoComplete="current-password"
              id="password"
              name="password"
              onChange={(event) => setPassword(event.target.value)}
              type="password"
              value={password}
            />
          </label>

          <button className="login-submit" disabled={isSubmitting} type="submit">
            {isSubmitting ? 'Logger ind...' : 'Log ind'}
          </button>
        </form>
      </section>
    </main>
  )
}

function HomePage({ user, onLogout }: { user: AuthState; onLogout: () => void }) {
  return (
    <main className="home-shell">
      <header className="home-header">
        <div>
          <p>{user.name}</p>
          <h1>Homepage</h1>
        </div>
        <button type="button" onClick={onLogout}>
          Log ud
        </button>
      </header>
    </main>
  )
}

export default App
