import { useQuery } from '@tanstack/react-query'
import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts'
import { getAllocation } from '../api/analyticsApi'
import type { AllocationSliceDto } from '../api/types'

const COLORS = ['#2563eb', '#16a34a', '#d97706', '#dc2626', '#7c3aed', '#0891b2', '#be185d', '#65a30d']

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
            >
              {slices.map((slice, index) => (
                <Cell key={slice.label} fill={COLORS[index % COLORS.length]} />
              ))}
            </Pie>
            <Tooltip formatter={(value) => Number(value).toLocaleString('en-US', { style: 'currency', currency: 'USD' })} />
            <Legend />
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
