import { apiClient } from './client'
import type { PagedResult, TransactionDto, TransactionType } from './types'

export function getTransactions(portfolioId: string, page = 1, pageSize = 25) {
  return apiClient
    .get<PagedResult<TransactionDto>>(`/portfolios/${portfolioId}/transactions`, { params: { page, pageSize } })
    .then((res) => res.data)
}

export interface CreateTransactionInput {
  symbol: string
  type: TransactionType
  quantity: number
  pricePerUnit: number
  fees: number
  tradeDate: string
}

export function createTransaction(portfolioId: string, input: CreateTransactionInput) {
  return apiClient.post<TransactionDto>(`/portfolios/${portfolioId}/transactions`, input).then((res) => res.data)
}

export function deleteTransaction(portfolioId: string, transactionId: string) {
  return apiClient.delete(`/portfolios/${portfolioId}/transactions/${transactionId}`)
}
