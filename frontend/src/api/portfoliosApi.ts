import { apiClient } from './client'
import type { PortfolioDto } from './types'

export function getPortfolios() {
  return apiClient.get<PortfolioDto[]>('/portfolios').then((res) => res.data)
}

export function createPortfolio(name: string, baseCurrency = 'USD') {
  return apiClient.post<PortfolioDto>('/portfolios', { name, baseCurrency }).then((res) => res.data)
}
