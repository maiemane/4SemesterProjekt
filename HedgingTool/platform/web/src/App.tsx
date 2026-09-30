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

type FundSummary = {
  instrumentId: number
  instrumentType: string
  bloombergTicker: string
  name: string
  currency: string
  quoteUnit: string
  latestNav: number | null
  latestNavDate: string | null
  latestPrice: number | null
  latestPriceDate: string | null
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

  const navigateTo = (nextPath: string) => {
    window.history.pushState(null, '', nextPath)
    setPath(nextPath)
  }

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

  if (path.startsWith('/hedges/')) {
    const instrumentId = Number(path.split('/')[2])

    return (
      <HedgeSearchPage
        accessToken={authState.accessToken}
        instrumentId={instrumentId}
        onBack={() => navigateTo('/home')}
        onLogout={handleLogout}
        user={authState}
      />
    )
  }

  return (
    <HomePage
      accessToken={authState.accessToken}
      onLogout={handleLogout}
      onSearchHedges={(instrumentId) => navigateTo(`/hedges/${instrumentId}`)}
      user={authState}
    />
  )
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
      setErrorMessage('Email and password are required.')
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
        setErrorMessage('Sign-in failed. Check email and password.')
        return
      }

      if (response.status === 400) {
        const message = await response.text()
        setErrorMessage(message || 'Invalid login request.')
        return
      }

      if (!response.ok) {
        setErrorMessage('Could not contact the login service.')
        return
      }

      const loginResponse = (await response.json()) as LoginResponse

      if (!loginResponse.accessToken || !loginResponse.expiresAtUtc) {
        setErrorMessage('The login response from the API was invalid.')
        return
      }

      onLogin(loginResponse)
    } catch {
      setErrorMessage('Could not connect to the API. Check that the API is running.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="login-shell">
      <section className="login-panel" aria-labelledby="login-title">
        <div className="login-heading">
          <p>Hedging Tool</p>
          <h1 id="login-title">Sign in</h1>
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
            {isSubmitting ? 'Signing in...' : 'Sign in'}
          </button>
        </form>
      </section>
    </main>
  )
}

function HomePage({
  accessToken,
  onLogout,
  onSearchHedges,
  user,
}: {
  accessToken: string
  onLogout: () => void
  onSearchHedges: (instrumentId: number) => void
  user: AuthState
}) {
  const [funds, setFunds] = useState<FundSummary[]>([])
  const [searchTerm, setSearchTerm] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let ignoreResult = false

    async function loadFunds() {
      setIsLoading(true)
      setErrorMessage(null)

      try {
        const response = await fetch('/api/funds', {
          headers: {
            Authorization: `Bearer ${accessToken}`,
          },
        })

        if (!response.ok) {
          setErrorMessage('Could not load funds from the API.')
          return
        }

        const fundResponse = (await response.json()) as FundSummary[]

        if (!ignoreResult) {
          setFunds(fundResponse)
        }
      } catch {
        if (!ignoreResult) {
          setErrorMessage('Could not connect to the API.')
        }
      } finally {
        if (!ignoreResult) {
          setIsLoading(false)
        }
      }
    }

    void loadFunds()

    return () => {
      ignoreResult = true
    }
  }, [accessToken])

  const filteredFunds = useMemo(() => {
    const normalizedSearchTerm = searchTerm.trim().toLowerCase()

    if (!normalizedSearchTerm) {
      return funds
    }

    return funds.filter((fund) => {
      return `${fund.name} ${fund.bloombergTicker} ${fund.currency} ${fund.instrumentType}`
        .toLowerCase()
        .includes(normalizedSearchTerm)
    })
  }, [funds, searchTerm])
  const fundStats = useMemo(() => getFundStats(funds), [funds])

  return (
    <main className="app-shell">
      <TopNav onLogout={onLogout} user={user} />
      <PageHeader
        subtitle="Review fund pricing, NAV and PD levels before searching for hedge candidates."
        title="Funds"
      />

      <section className="summary-grid" aria-label="Fund overview">
        <div className="summary-item">
          <span>Total funds</span>
          <strong>{funds.length}</strong>
        </div>
        <div className="summary-item">
          <span>Latest data date</span>
          <strong>{fundStats.latestDate}</strong>
          <small>Newest available price or NAV date</small>
        </div>
        <div className="summary-item">
          <span>Funds on latest date</span>
          <strong>{fundStats.latestDateFundCount}</strong>
          <small>Funds with data on the latest date</small>
        </div>
      </section>

      <section className="fund-panel" aria-labelledby="funds-title">
        <div className="fund-toolbar">
          <label className="fund-search" htmlFor="fund-search">
            <span>Search fund / ticker</span>
            <input
              id="fund-search"
              onChange={(event) => setSearchTerm(event.target.value)}
              placeholder="Search by name, ticker, currency or type"
              type="search"
              value={searchTerm}
            />
          </label>
          <div className="fund-count">
            <strong>{filteredFunds.length}</strong>
            <span>{filteredFunds.length === 1 ? 'fund' : 'funds'}</span>
          </div>
        </div>

        <div className="fund-table-shell">
          <table className="fund-table">
            <thead>
              <tr>
                <th scope="col">Fund / ticker</th>
                <th scope="col">Price</th>
                <th scope="col">NAV</th>
                <th scope="col">PD level</th>
                <th scope="col">Updated</th>
                <th className="action-column" aria-label="Actions" scope="col"></th>
              </tr>
            </thead>
            <tbody>
              {isLoading ? (
                <tr>
                  <td className="table-message" colSpan={6}>
                    Loading funds...
                  </td>
                </tr>
              ) : null}

              {!isLoading && errorMessage ? (
                <tr>
                  <td className="table-message table-message-error" colSpan={6}>
                    {errorMessage}
                  </td>
                </tr>
              ) : null}

              {!isLoading && !errorMessage && filteredFunds.length === 0 ? (
                <tr>
                  <td className="table-message" colSpan={6}>
                    No funds match the search.
                  </td>
                </tr>
              ) : null}

              {!isLoading && !errorMessage
                ? filteredFunds.map((fund) => {
                    const spread = getPriceNavSpread(fund)
                    const latestDate = getLatestDate(fund)
                    const fundSecondaryText = formatFundSecondaryText(fund)

                    return (
                      <tr key={fund.instrumentId}>
                        <td>
                          <div className="fund-name-cell">
                            <strong>{fund.name}</strong>
                            {fundSecondaryText ? <span>{fundSecondaryText}</span> : null}
                          </div>
                        </td>
                        <td className="numeric-cell">{formatOptionalNumber(fund.latestPrice)}</td>
                        <td className="numeric-cell">{formatOptionalNumber(fund.latestNav)}</td>
                        <td>
                          <span className={spread.className}>{spread.label}</span>
                        </td>
                        <td>{latestDate ? formatDate(latestDate) : '-'}</td>
                        <td className="action-column">
                          <button
                            className="hedge-button"
                            aria-label={`Search hedges for ${fund.name}`}
                            onClick={() => onSearchHedges(fund.instrumentId)}
                            type="button"
                          >
                            Search
                            <span aria-hidden="true">→</span>
                          </button>
                        </td>
                      </tr>
                    )
                  })
                : null}
            </tbody>
          </table>
        </div>
      </section>
    </main>
  )
}

function HedgeSearchPage({
  accessToken,
  instrumentId,
  onBack,
  onLogout,
  user,
}: {
  accessToken: string
  instrumentId: number
  onBack: () => void
  onLogout: () => void
  user: AuthState
}) {
  const [fund, setFund] = useState<FundSummary | null>(null)
  const [isLoading, setIsLoading] = useState(true)

  useEffect(() => {
    let ignoreResult = false

    async function loadFund() {
      setIsLoading(true)

      try {
        const response = await fetch('/api/funds', {
          headers: {
            Authorization: `Bearer ${accessToken}`,
          },
        })

        if (!response.ok) {
          return
        }

        const funds = (await response.json()) as FundSummary[]
        const selectedFund = funds.find((item) => item.instrumentId === instrumentId) ?? null

        if (!ignoreResult) {
          setFund(selectedFund)
        }
      } finally {
        if (!ignoreResult) {
          setIsLoading(false)
        }
      }
    }

    void loadFund()

    return () => {
      ignoreResult = true
    }
  }, [accessToken, instrumentId])

  return (
    <main className="app-shell">
      <TopNav onLogout={onLogout} user={user} />
      <PageHeader
        subtitle="Use the selected fund as the starting point for finding relevant hedging candidates."
        title="Search Hedges"
      />

      <section className="hedge-page">
        <button className="back-button" onClick={onBack} type="button">
          ← Back to funds
        </button>

        <div className="hedge-detail">
          <p>Selected fund</p>
          <h2>{isLoading ? 'Loading fund...' : fund?.name ?? 'Fund not found'}</h2>
          {fund ? (
            <dl className="fund-facts">
              <div>
                <dt>Ticker</dt>
                <dd>{fund.bloombergTicker || '-'}</dd>
              </div>
              <div>
                <dt>Currency</dt>
                <dd>{fund.currency || '-'}</dd>
              </div>
              <div>
                <dt>Latest NAV</dt>
                <dd>{formatMetric(fund.latestNav, fund.latestNavDate, fund.quoteUnit)}</dd>
              </div>
              <div>
                <dt>Latest close</dt>
                <dd>{formatMetric(fund.latestPrice, fund.latestPriceDate, fund.quoteUnit)}</dd>
              </div>
            </dl>
          ) : null}
        </div>
      </section>
    </main>
  )
}

function TopNav({
  onLogout,
  user,
}: {
  onLogout: () => void
  user: AuthState
}) {
  const [isAccountMenuOpen, setIsAccountMenuOpen] = useState(false)

  return (
    <nav className="top-nav" aria-label="Main navigation">
      <div className="brand-mark" aria-hidden="true">
        NC
      </div>
      <div className="brand-copy">
        <strong>Nordic Cap Hedging Tool</strong>
        <span>Fund universe</span>
      </div>
      <div className="user-menu">
        <button
          className="account-trigger"
          aria-expanded={isAccountMenuOpen}
          aria-haspopup="menu"
          onClick={() => setIsAccountMenuOpen((current) => !current)}
          type="button"
        >
          <span className="user-avatar" aria-hidden="true">
            {getInitials(user.name)}
          </span>
          <span className="account-caret" aria-hidden="true">
          </span>
        </button>

        {isAccountMenuOpen ? (
          <div className="account-menu" role="menu">
            <div className="account-menu-header">
              <strong>{user.name}</strong>
              <span>{user.email}</span>
            </div>
            <button type="button" role="menuitem" onClick={onLogout}>
              Sign out
            </button>
          </div>
        ) : null}
      </div>
    </nav>
  )
}

function PageHeader({ subtitle, title }: { subtitle: string; title: string }) {
  return (
    <header className="page-header">
      <div className="page-title">
        <h1>{title}</h1>
        <p>{subtitle}</p>
      </div>
    </header>
  )
}

function formatMetric(value: number | null, date: string | null, unit: string) {
  if (value === null) {
    return '-'
  }

  const formattedValue = new Intl.NumberFormat('da-DK', {
    maximumFractionDigits: 4,
    minimumFractionDigits: 0,
  }).format(value)

  return `${formattedValue}${unit ? ` ${unit}` : ''}${date ? ` · ${formatDate(date)}` : ''}`
}

function getPriceNavSpread(fund: FundSummary) {
  if (fund.latestNav === null || fund.latestPrice === null || fund.latestNav === 0) {
    return {
      className: 'spread-pill spread-pill-neutral',
      label: '-',
    }
  }

  const spread = ((fund.latestPrice - fund.latestNav) / fund.latestNav) * 100
  const formattedSpread = new Intl.NumberFormat('en-US', {
    maximumFractionDigits: 1,
    minimumFractionDigits: 1,
  }).format(spread)
  const label = `${spread > 0 ? '+' : ''}${formattedSpread}%`

  return {
    className:
      Math.abs(spread) < 0.5
        ? 'spread-pill spread-pill-neutral'
        : spread < 0
          ? 'spread-pill spread-pill-discount'
          : 'spread-pill spread-pill-premium',
    label,
  }
}

function getLatestDate(fund: FundSummary) {
  if (fund.latestNavDate && fund.latestPriceDate) {
    return fund.latestNavDate > fund.latestPriceDate ? fund.latestNavDate : fund.latestPriceDate
  }

  return fund.latestNavDate ?? fund.latestPriceDate
}

function formatSecondaryMeta(fund: FundSummary) {
  const parts = [fund.currency, fund.instrumentType].filter(
    (part) => part && part.toUpperCase() !== 'UNKNOWN' && part.toUpperCase() !== 'FUND',
  )

  return parts.join(' · ')
}

function formatFundSecondaryText(fund: FundSummary) {
  const secondaryParts = []
  const normalizedName = fund.name.trim().toLowerCase()
  const normalizedTicker = fund.bloombergTicker.trim().toLowerCase()
  const secondaryMeta = formatSecondaryMeta(fund)

  if (fund.bloombergTicker && normalizedTicker !== normalizedName) {
    secondaryParts.push(fund.bloombergTicker)
  }

  if (secondaryMeta) {
    secondaryParts.push(secondaryMeta)
  }

  return secondaryParts.join(' · ')
}

function formatNumber(value: number) {
  return new Intl.NumberFormat('da-DK', {
    maximumFractionDigits: 2,
    minimumFractionDigits: 0,
  }).format(value)
}

function formatOptionalNumber(value: number | null) {
  return value === null ? '-' : formatNumber(value)
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('da-DK', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date(value))
}

function getInitials(name: string) {
  const initials = name
    .split(' ')
    .filter(Boolean)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toUpperCase()

  return initials || 'U'
}

function getFundStats(funds: FundSummary[]) {
  const dates = funds
    .map(getLatestDate)
    .filter((value): value is string => value !== null)
    .sort()
  const latestDate = dates.at(-1)
  const latestDateFundCount = latestDate
    ? funds.filter((fund) => getLatestDate(fund) === latestDate).length
    : 0

  return {
    latestDate: latestDate ? formatDate(latestDate) : '-',
    latestDateFundCount: latestDate ? `${latestDateFundCount} / ${funds.length}` : '-',
  }
}

export default App
