import { useQuery } from '@tanstack/react-query'
import { getValueHistory } from '../api/analyticsApi'

const currency = (value: number) =>
  value.toLocaleString('en-US', { style: 'currency', currency: 'USD', maximumFractionDigits: 0 })

export function HeroStat({ portfolioId }: { portfolioId: string }) {
  const { data } = useQuery({
    queryKey: ['value-history', portfolioId],
    queryFn: () => getValueHistory(portfolioId),
  })

  if (!data || data.length === 0) {
    return null
  }

  const latest = data[data.length - 1]
  const gainLoss = latest.totalMarketValue - latest.totalCostBasis
  const gainLossPercent = latest.totalCostBasis === 0 ? 0 : (gainLoss / latest.totalCostBasis) * 100
  const isPositive = gainLoss >= 0

  return (
    <section className="hero-stat">
      <span className="hero-stat-label">Total Value · {latest.date}</span>
      <span className="hero-stat-value">{currency(latest.totalMarketValue)}</span>
      <span className={isPositive ? 'positive' : 'negative'}>
        {isPositive ? '▲' : '▼'} {currency(Math.abs(gainLoss))} ({gainLossPercent.toFixed(1)}%) all-time
      </span>
    </section>
  )
}
