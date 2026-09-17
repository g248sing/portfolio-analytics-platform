import { useQuery, useQueryClient } from '@tanstack/react-query'
import { createContext, useCallback, useContext, useMemo, useState, type ReactNode } from 'react'
import { createPortfolio, getPortfolios } from '../api/portfoliosApi'
import type { PortfolioDto } from '../api/types'

const SELECTED_PORTFOLIO_KEY = 'selectedPortfolioId'

interface PortfolioContextValue {
  portfolioId: string | null
  setPortfolioId: (id: string) => void
  portfolios: PortfolioDto[] | undefined
  isLoading: boolean
  createNewPortfolio: (name: string) => Promise<void>
}

const PortfolioContext = createContext<PortfolioContextValue | null>(null)

export function PortfolioProvider({ children }: { children: ReactNode }) {
  const queryClient = useQueryClient()
  const { data: portfolios, isLoading } = useQuery({ queryKey: ['portfolios'], queryFn: getPortfolios })
  const [storedPortfolioId, setStoredPortfolioId] = useState<string | null>(() => {
    try {
      return localStorage.getItem(SELECTED_PORTFOLIO_KEY)
    } catch {
      return null
    }
  })

  // Fall back to the first portfolio if nothing is stored yet, or the stored
  // id no longer exists (e.g. it was deleted elsewhere) — derived at render
  // time rather than via an effect, since it's just a function of the query result.
  const portfolioId = useMemo(() => {
    if (!portfolios || portfolios.length === 0) return null
    if (storedPortfolioId && portfolios.some((p) => p.id === storedPortfolioId)) return storedPortfolioId
    return portfolios[0].id
  }, [portfolios, storedPortfolioId])

  const setPortfolioId = useCallback((id: string) => {
    setStoredPortfolioId(id)
    try {
      localStorage.setItem(SELECTED_PORTFOLIO_KEY, id)
    } catch {
      // Ignore write failures (private browsing, storage disabled, etc).
    }
  }, [])

  const createNewPortfolio = useCallback(
    async (name: string) => {
      const created = await createPortfolio(name)
      await queryClient.invalidateQueries({ queryKey: ['portfolios'] })
      setPortfolioId(created.id)
    },
    [queryClient, setPortfolioId],
  )

  const value = useMemo(
    () => ({ portfolioId, setPortfolioId, portfolios, isLoading, createNewPortfolio }),
    [portfolioId, setPortfolioId, portfolios, isLoading, createNewPortfolio],
  )

  return <PortfolioContext.Provider value={value}>{children}</PortfolioContext.Provider>
}

export function usePortfolioContext() {
  const context = useContext(PortfolioContext)
  if (!context) {
    throw new Error('usePortfolioContext must be used within a PortfolioProvider')
  }
  return context
}
