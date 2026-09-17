import { apiClient } from './client'
import type { TickerEntryDto } from './types'

export function getTicker(count = 24) {
  return apiClient.get<TickerEntryDto[]>('/market-data/ticker', { params: { count } }).then((res) => res.data)
}
