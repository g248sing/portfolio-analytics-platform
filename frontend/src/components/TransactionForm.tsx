import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { createTransaction } from '../api/transactionsApi'
import type { TransactionType } from '../api/types'

export function TransactionForm({ portfolioId }: { portfolioId: string }) {
  const queryClient = useQueryClient()
  const [symbol, setSymbol] = useState('')
  const [type, setType] = useState<TransactionType>('Buy')
  const [quantity, setQuantity] = useState('')
  const [pricePerUnit, setPricePerUnit] = useState('')
  const [fees, setFees] = useState('0')
  const [tradeDate, setTradeDate] = useState(() => new Date().toISOString().slice(0, 10))
  const [error, setError] = useState<string | null>(null)

  const mutation = useMutation({
    mutationFn: () =>
      createTransaction(portfolioId, {
        symbol: symbol.toUpperCase(),
        type,
        quantity: Number(quantity),
        pricePerUnit: Number(pricePerUnit),
        fees: Number(fees) || 0,
        tradeDate,
      }),
    onSuccess: () => {
      setSymbol('')
      setQuantity('')
      setPricePerUnit('')
      setFees('0')
      setError(null)
      queryClient.invalidateQueries({ queryKey: ['transactions', portfolioId] })
      queryClient.invalidateQueries({ queryKey: ['holdings', portfolioId] })
      queryClient.invalidateQueries({ queryKey: ['value-history', portfolioId] })
      queryClient.invalidateQueries({ queryKey: ['allocation', portfolioId] })
      queryClient.invalidateQueries({ queryKey: ['performers', portfolioId] })
    },
    onError: () => setError('Could not save this transaction. Check the values and try again.'),
  })

  function handleSubmit(event: FormEvent) {
    event.preventDefault()
    mutation.mutate()
  }

  return (
    <form className="card transaction-form" onSubmit={handleSubmit}>
      <h3>Add Transaction</h3>
      <div className="form-row">
        <label>
          Symbol
          <input value={symbol} onChange={(e) => setSymbol(e.target.value)} placeholder="AAPL" required />
        </label>
        <label>
          Type
          <select value={type} onChange={(e) => setType(e.target.value as TransactionType)}>
            <option value="Buy">Buy</option>
            <option value="Sell">Sell</option>
            <option value="Dividend">Dividend</option>
          </select>
        </label>
        <label>
          Quantity
          <input type="number" step="any" min="0" value={quantity} onChange={(e) => setQuantity(e.target.value)} required />
        </label>
        <label>
          Price/Unit
          <input type="number" step="any" min="0" value={pricePerUnit} onChange={(e) => setPricePerUnit(e.target.value)} required />
        </label>
        <label>
          Fees
          <input type="number" step="any" min="0" value={fees} onChange={(e) => setFees(e.target.value)} />
        </label>
        <label>
          Trade Date
          <input type="date" value={tradeDate} onChange={(e) => setTradeDate(e.target.value)} required />
        </label>
        <button type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? 'Saving...' : 'Add'}
        </button>
      </div>
      {error && <p role="alert">{error}</p>}
    </form>
  )
}
