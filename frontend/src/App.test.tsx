import { render, screen } from '@testing-library/react'
import { BrowserRouter } from 'react-router-dom'
import { describe, expect, it } from 'vitest'
import App from './App'
import { AuthProvider } from './auth/AuthContext'

describe('App', () => {
  it('redirects unauthenticated users to the login page', async () => {
    render(
      <BrowserRouter>
        <AuthProvider>
          <App />
        </AuthProvider>
      </BrowserRouter>,
    )
    expect(await screen.findByRole('heading', { name: /welcome back/i })).toBeInTheDocument()
  })
})
