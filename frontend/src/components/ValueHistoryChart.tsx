import { useQuery } from '@tanstack/react-query'
import { CartesianGrid, Legend, Line, LineChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { getValueHistory } from '../api/analyticsApi'

const currencyFormatter = new Intl.NumberFormat('en-US', { style: 'currency', currency: 'USD' })

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
        <CartesianGrid strokeDasharray="3 3" />
        <XAxis dataKey="date" tick={{ fontSize: 12 }} />
        <YAxis tickFormatter={(v: number) => currencyFormatter.format(v)} width={90} tick={{ fontSize: 12 }} />
        <Tooltip formatter={(value) => currencyFormatter.format(Number(value))} />
        <Legend />
        <Line type="monotone" dataKey="totalMarketValue" name="Market Value" stroke="#2563eb" dot={false} strokeWidth={2} />
        <Line type="monotone" dataKey="totalCostBasis" name="Cost Basis" stroke="#94a3b8" dot={false} strokeWidth={2} />
      </LineChart>
    </ResponsiveContainer>
  )
}
