import { Route, Routes } from 'react-router-dom'

function Dashboard() {
  return (
    <main>
      <h1>Portfolio Analytics</h1>
      <p>Dashboard coming soon.</p>
    </main>
  )
}

function App() {
  return (
    <Routes>
      <Route path="/" element={<Dashboard />} />
    </Routes>
  )
}

export default App
