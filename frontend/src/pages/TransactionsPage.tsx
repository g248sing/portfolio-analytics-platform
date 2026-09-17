import { PortfolioSelector } from '../components/PortfolioSelector'
import { TransactionForm } from '../components/TransactionForm'
import { TransactionsTable } from '../components/TransactionsTable'
import { usePortfolioContext } from '../portfolio/PortfolioContext'

export function TransactionsPage() {
  const { portfolioId } = usePortfolioContext()

  return (
    <main>
      <PortfolioSelector />

      {portfolioId && (
        <>
          <TransactionForm portfolioId={portfolioId} />
          <TransactionsTable portfolioId={portfolioId} />
        </>
      )}
    </main>
  )
}
