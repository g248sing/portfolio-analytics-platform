import { useQuery } from '@tanstack/react-query'
import { getPerformers } from '../api/analyticsApi'
import type { PerformerEntryDto } from '../api/types'

function PerformerList({ title, entries }: { title: string; entries: PerformerEntryDto[] }) {
  return (
    <div className="card">
      <h3>{title}</h3>
      {entries.length === 0 ? (
        <p>Not enough priced holdings yet.</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Symbol</th>
              <th>Name</th>
              <th>Gain/Loss</th>
              <th>%</th>
            </tr>
          </thead>
          <tbody>
            {entries.map((entry) => (
              <tr key={entry.symbol}>
                <td>{entry.symbol}</td>
                <td>{entry.securityName}</td>
                <td className={entry.unrealizedGainLoss >= 0 ? 'positive' : 'negative'}>
                  {entry.unrealizedGainLoss.toLocaleString('en-US', { style: 'currency', currency: 'USD' })}
                </td>
                <td className={entry.unrealizedGainLossPercent >= 0 ? 'positive' : 'negative'}>
                  {entry.unrealizedGainLossPercent.toFixed(2)}%
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  )
}

export function PerformersTable({ portfolioId }: { portfolioId: string }) {
  const { data, isLoading } = useQuery({
    queryKey: ['performers', portfolioId],
    queryFn: () => getPerformers(portfolioId),
  })

  if (isLoading) return <p>Loading performers...</p>
  if (!data) return null

  return (
    <div className="performers-grid">
      <PerformerList title="Top Performers" entries={data.top} />
      <PerformerList title="Bottom Performers" entries={data.bottom} />
    </div>
  )
}
