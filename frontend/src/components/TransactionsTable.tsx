import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { deleteTransaction, getTransactions } from '../api/transactionsApi'

const currency = (value: number) => value.toLocaleString('en-US', { style: 'currency', currency: 'USD' })

export function TransactionsTable({ portfolioId }: { portfolioId: string }) {
  const queryClient = useQueryClient()
  const [page, setPage] = useState(1)
  const pageSize = 10

  const { data, isLoading } = useQuery({
    queryKey: ['transactions', portfolioId, page],
    queryFn: () => getTransactions(portfolioId, page, pageSize),
  })

  const deleteMutation = useMutation({
    mutationFn: (transactionId: string) => deleteTransaction(portfolioId, transactionId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['transactions', portfolioId] })
      queryClient.invalidateQueries({ queryKey: ['holdings', portfolioId] })
      queryClient.invalidateQueries({ queryKey: ['value-history', portfolioId] })
      queryClient.invalidateQueries({ queryKey: ['allocation', portfolioId] })
      queryClient.invalidateQueries({ queryKey: ['performers', portfolioId] })
    },
  })

  if (isLoading) return <p>Loading transactions...</p>
  if (!data || data.items.length === 0) return <p>No transactions yet.</p>

  const totalPages = Math.max(1, Math.ceil(data.totalCount / pageSize))

  return (
    <div className="card">
      <h3>Transaction History</h3>
      <table>
        <thead>
          <tr>
            <th>Date</th>
            <th>Symbol</th>
            <th>Type</th>
            <th>Qty</th>
            <th>Price</th>
            <th>Fees</th>
            <th>Realized P&amp;L</th>
            <th />
          </tr>
        </thead>
        <tbody>
          {data.items.map((tx) => (
            <tr key={tx.id}>
              <td>{tx.tradeDate}</td>
              <td>{tx.symbol}</td>
              <td>{tx.type}</td>
              <td>{tx.quantity}</td>
              <td>{currency(tx.pricePerUnit)}</td>
              <td>{currency(tx.fees)}</td>
              <td>{tx.realizedGainLoss === null ? '—' : currency(tx.realizedGainLoss)}</td>
              <td>
                <button
                  type="button"
                  onClick={() => deleteMutation.mutate(tx.id)}
                  disabled={deleteMutation.isPending}
                  aria-label={`Delete ${tx.type} transaction for ${tx.symbol}`}
                >
                  Delete
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
      <div className="pagination">
        <button type="button" onClick={() => setPage((p) => p - 1)} disabled={page <= 1}>
          Previous
        </button>
        <span>
          Page {page} of {totalPages}
        </span>
        <button type="button" onClick={() => setPage((p) => p + 1)} disabled={page >= totalPages}>
          Next
        </button>
      </div>
    </div>
  )
}
