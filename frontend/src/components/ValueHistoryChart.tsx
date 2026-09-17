import { useQuery } from '@tanstack/react-query'
import { CartesianGrid, Legend, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { getValueHistory } from '../api/analyticsApi'

const currencyFormatter = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

const tooltipStyle = {
  background: 'var(--bg-elevated)',
  border: '1px solid var(--border-bright)',
  borderRadius: 2,
  fontFamily: 'var(--mono)',
  fontSize: 12,
}

export function ValueHistoryChart({ portfolioId }: { portfolioId: string }) {
  const { data, isLoading } = useQuery({
    queryKey: ['value-history', portfolioId],
    queryFn: () => getValueHistory(portfolioId),
  })

  if (isLoading) return <p>Loading chart...</p>
  if (!data || data.length === 0) return <p>No value history yet. Add transactions and refresh prices to see this chart.</p>

  return (
    <ResponsiveContainer width="100%" height={320}>
      <LineChart data={data}>
        <CartesianGrid strokeDasharray="2 6" stroke="var(--border)" />
        <XAxis dataKey="date" tick={{ fontSize: 11, fill: 'var(--text-dim)' }} stroke="var(--border)" />
        <YAxis
          tickFormatter={(v: number) => currencyFormatter.format(v)}
          width={90}
          tick={{ fontSize: 11, fill: 'var(--text-dim)' }}
          stroke="var(--border)"
        />
        <Tooltip contentStyle={tooltipStyle} labelStyle={{ color: 'var(--text-h)' }} formatter={(value) => currencyFormatter.format(Number(value))} />
        <Legend wrapperStyle={{ fontSize: 12 }} />
        <Line type="monotone" dataKey="totalMarketValue" name="Market Value" stroke="#ffb020" dot={false} strokeWidth={2.5} />
        <Line type="monotone" dataKey="totalCostBasis" name="Cost Basis" stroke="#2dd9c4" dot={false} strokeWidth={2} strokeDasharray="4 3" />
      </LineChart>
    </ResponsiveContainer>
  )
}
