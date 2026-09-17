import { apiClient } from './client'
import type { AllocationResponseDto, PerformersResponseDto, ValueHistoryPointDto } from './types'

export function getValueHistory(portfolioId: string) {
  return apiClient
    .get<ValueHistoryPointDto[]>(`/portfolios/${portfolioId}/analytics/value-history`)
    .then((res) => res.data)
}

export function getAllocation(portfolioId: string) {
  return apiClient.get<AllocationResponseDto>(`/portfolios/${portfolioId}/analytics/allocation`).then((res) => res.data)
}

export function getPerformers(portfolioId: string, count = 5) {
  return apiClient
    .get<PerformersResponseDto>(`/portfolios/${portfolioId}/analytics/performers`, { params: { count } })
    .then((res) => res.data)
}
