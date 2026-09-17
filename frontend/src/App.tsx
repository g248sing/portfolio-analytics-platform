import { Route, Routes } from 'react-router-dom'
import { ProtectedRoute } from './auth/ProtectedRoute'
import { useAuth } from './auth/AuthContext'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'

function Dashboard() {
  const { user, logout } = useAuth()

  return (
    <main>
      <h1>Portfolio Analytics</h1>
      <p>Signed in as {user?.email}</p>
      <button type="button" onClick={() => logout()}>
        Log out
      </button>
      <p>Dashboard coming soon.</p>
    </main>
  )
}

function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route
        path="/"
        element={
          <ProtectedRoute>
            <Dashboard />
          </ProtectedRoute>
        }
      />
    </Routes>
  )
}

export default App
