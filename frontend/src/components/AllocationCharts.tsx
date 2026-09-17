import { useQuery } from '@tanstack/react-query'
import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts'
import { getAllocation } from '../api/analyticsApi'
import type { AllocationSliceDto } from '../api/types'

const COLORS = ['#ffb020', '#2dd9c4', '#ff8f4d', '#6ee7d0', '#d4a05a', '#7fa8c9', '#c77d5c', '#9d8fd4']

const tooltipStyle = {
  background: 'var(--bg-elevated)',
  border: '1px solid var(--border-bright)',
  borderRadius: 2,
  fontFamily: 'var(--mono)',
  fontSize: 12,
}

function PieCard({ title, slices }: { title: string; slices: AllocationSliceDto[] }) {
  return (
    <div className="card">
      <h3>{title}</h3>
      {slices.length === 0 ? (
        <p>No holdings yet.</p>
      ) : (
        <ResponsiveContainer width="100%" height={260}>
          <PieChart>
            <Pie
              data={slices}
              dataKey="value"
              nameKey="label"
              outerRadius={90}
              label={(entry: { name?: string; percent?: number }) => `${entry.name} (${((entry.percent ?? 0) * 100).toFixed(0)}%)`}
              labelLine={{ stroke: 'var(--border-bright)' }}
              stroke="var(--bg-elevated)"
            >
              {slices.map((slice, index) => (
                <Cell key={slice.label} fill={COLORS[index % COLORS.length]} />
              ))}
            </Pie>
            <Tooltip contentStyle={tooltipStyle} formatter={(value) => Number(value).toLocaleString('en-US', { style: 'currency', currency: 'USD' })} />
            <Legend wrapperStyle={{ fontSize: 12 }} />
          </PieChart>
        </ResponsiveContainer>
      )}
    </div>
  )
}

export function AllocationCharts({ portfolioId }: { portfolioId: string }) {
  const { data, isLoading } = useQuery({
    queryKey: ['allocation', portfolioId],
    queryFn: () => getAllocation(portfolioId),
  })

  if (isLoading) return <p>Loading allocation...</p>
  if (!data) return null

  return (
    <div className="allocation-grid">
      <PieCard title="By Sector" slices={data.bySector} />
      <PieCard title="By Asset Class" slices={data.byAssetClass} />
    </div>
  )
}
