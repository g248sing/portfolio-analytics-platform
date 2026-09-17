import { useState, type FormEvent } from 'react'
import { usePortfolioContext } from '../portfolio/PortfolioContext'

export function PortfolioSelector() {
  const { portfolios, portfolioId, setPortfolioId, isLoading, createNewPortfolio } = usePortfolioContext()
  const [showCreateForm, setShowCreateForm] = useState(false)
  const [newName, setNewName] = useState('')

  async function handleCreate(event: FormEvent) {
    event.preventDefault()
    if (!newName.trim()) return
    await createNewPortfolio(newName.trim())
    setNewName('')
    setShowCreateForm(false)
  }

  if (isLoading) {
    return <p>Loading portfolios...</p>
  }

  if (!portfolios || portfolios.length === 0) {
    return (
      <form onSubmit={handleCreate}>
        <label>
          Create your first portfolio
          <input value={newName} onChange={(e) => setNewName(e.target.value)} placeholder="e.g. Retirement" required />
        </label>
        <button type="submit">Create</button>
      </form>
    )
  }

  return (
    <div className="portfolio-selector">
      <select value={portfolioId ?? ''} onChange={(e) => setPortfolioId(e.target.value)}>
        {portfolios.map((p) => (
          <option key={p.id} value={p.id}>
            {p.name}
          </option>
        ))}
      </select>
      {showCreateForm ? (
        <form onSubmit={handleCreate}>
          <input value={newName} onChange={(e) => setNewName(e.target.value)} placeholder="Portfolio name" required autoFocus />
          <button type="submit">Add</button>
          <button type="button" onClick={() => setShowCreateForm(false)}>
            Cancel
          </button>
        </form>
      ) : (
        <button type="button" onClick={() => setShowCreateForm(true)}>
          + New portfolio
        </button>
      )}
    </div>
  )
}
