import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { downloadHoldingsCsv, downloadHoldingsXlsx } from '../api/exportApi'
import { getHoldings } from '../api/holdingsApi'

const currency = (value: number | null) =>
  value === null ? '—' : value.toLocaleString('en-US', { style: 'currency', currency: 'USD' })

export function HoldingsTable({ portfolioId }: { portfolioId: string }) {
  const { data, isLoading } = useQuery({
    queryKey: ['holdings', portfolioId],
    queryFn: () => getHoldings(portfolioId),
  })
  const [exporting, setExporting] = useState<'csv' | 'xlsx' | null>(null)

  async function handleExport(format: 'csv' | 'xlsx') {
    setExporting(format)
    try {
      await (format === 'csv' ? downloadHoldingsCsv(portfolioId) : downloadHoldingsXlsx(portfolioId))
    } finally {
      setExporting(null)
    }
  }

  if (isLoading) return <p>Loading holdings...</p>

  return (
    <div className="card">
      <div className="card-header">
        <h3>Holdings</h3>
        <div className="export-buttons">
          <button type="button" onClick={() => handleExport('csv')} disabled={exporting !== null || !data?.length}>
            {exporting === 'csv' ? 'Exporting...' : 'Export CSV'}
          </button>
          <button type="button" onClick={() => handleExport('xlsx')} disabled={exporting !== null || !data?.length}>
            {exporting === 'xlsx' ? 'Exporting...' : 'Export Excel'}
          </button>
        </div>
      </div>
      {!data || data.length === 0 ? (
        <p>No open positions yet.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Symbol</th>
              <th>Qty</th>
              <th>Avg Cost</th>
              <th>Price</th>
              <th>Market Value</th>
              <th>Unrealized P&amp;L</th>
              <th>Realized P&amp;L</th>
            </tr>
          </thead>
          <tbody>
            {data.map((holding) => (
              <tr key={holding.symbol}>
                <td>{holding.symbol}</td>
                <td>{holding.quantity}</td>
                <td>{currency(holding.averageCostBasisPerUnit)}</td>
                <td>{currency(holding.currentPrice)}</td>
                <td>{currency(holding.marketValue)}</td>
                <td className={(holding.unrealizedGainLoss ?? 0) >= 0 ? 'positive' : 'negative'}>
                  {currency(holding.unrealizedGainLoss)}
                </td>
                <td className={holding.realizedGainLoss >= 0 ? 'positive' : 'negative'}>{currency(holding.realizedGainLoss)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}
