import { AllocationCharts } from '../components/AllocationCharts'
import { HeroStat } from '../components/HeroStat'
import { HoldingsTable } from '../components/HoldingsTable'
import { PerformersTable } from '../components/PerformersTable'
import { PortfolioSelector } from '../components/PortfolioSelector'
import { ValueHistoryChart } from '../components/ValueHistoryChart'
import { usePortfolioContext } from '../portfolio/PortfolioContext'

export function DashboardPage() {
  const { portfolioId } = usePortfolioContext()

  return (
    <main>
      <PortfolioSelector />

      {portfolioId && (
        <>
          <HeroStat portfolioId={portfolioId} />

          <section className="card">
            <h3>Portfolio Value Over Time</h3>
            <ValueHistoryChart portfolioId={portfolioId} />
          </section>

          <AllocationCharts portfolioId={portfolioId} />
          <PerformersTable portfolioId={portfolioId} />
          <HoldingsTable portfolioId={portfolioId} />
        </>
      )}
    </main>
  )
}
