import { Outlet } from 'react-router-dom'
import { PortfolioProvider } from '../portfolio/PortfolioContext'
import { Nav } from './Nav'

export function Layout() {
  return (
    <PortfolioProvider>
      <Nav />
      <Outlet />
    </PortfolioProvider>
  )
}
