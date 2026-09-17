import { apiClient } from './client'
import type { HoldingDto } from './types'

export function getHoldings(portfolioId: string) {
  return apiClient.get<HoldingDto[]>(`/portfolios/${portfolioId}/holdings`).then((res) => res.data)
}
