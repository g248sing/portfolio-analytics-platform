import { NavLink } from 'react-router-dom'
import { useAuth } from '../auth/AuthContext'

export function Nav() {
  const { user, logout } = useAuth()

  return (
    <nav className="top-nav">
      <span className="brand">Portfolio Analytics</span>
      <NavLink to="/" end>
        Dashboard
      </NavLink>
      <NavLink to="/transactions">Transactions</NavLink>
      <span className="spacer" />
      <span>{user?.email}</span>
      <button type="button" onClick={() => logout()}>
        Log out
      </button>
    </nav>
  )
}
