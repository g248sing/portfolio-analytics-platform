export interface PortfolioDto {
  id: string
  name: string
  baseCurrency: string
  createdAt: string
}

export type TransactionType = 'Buy' | 'Sell' | 'Dividend'

export interface TransactionDto {
  id: string
  symbol: string
  type: TransactionType
  quantity: number
  pricePerUnit: number
  fees: number
  tradeDate: string
  createdAt: string
  realizedGainLoss: number | null
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  page: number
  pageSize: number
}

export interface HoldingDto {
  symbol: string
  securityName: string
  sector: string | null
  assetClass: string
  quantity: number
  averageCostBasisPerUnit: number
  totalCostBasis: number
  currentPrice: number | null
  marketValue: number | null
  unrealizedGainLoss: number | null
  unrealizedGainLossPercent: number | null
  realizedGainLoss: number
}

export interface ValueHistoryPointDto {
  date: string
  totalMarketValue: number
  totalCostBasis: number
}

export interface AllocationSliceDto {
  label: string
  value: number
  percentage: number
}

export interface AllocationResponseDto {
  bySector: AllocationSliceDto[]
  byAssetClass: AllocationSliceDto[]
}

export interface PerformerEntryDto {
  symbol: string
  securityName: string
  unrealizedGainLoss: number
  unrealizedGainLossPercent: number
}

export interface PerformersResponseDto {
  top: PerformerEntryDto[]
  bottom: PerformerEntryDto[]
}
