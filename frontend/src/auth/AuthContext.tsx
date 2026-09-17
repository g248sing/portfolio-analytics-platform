import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import * as authApi from '../api/authApi'
import { registerUnauthorizedHandler, setAccessToken } from '../api/client'
import { decodeAccessToken } from './jwt'

interface AuthUser {
  id: string
  email: string
}

interface AuthContextValue {
  user: AuthUser | null
  status: 'loading' | 'authenticated' | 'unauthenticated'
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
}

const AuthContext = createContext<AuthContextValue | null>(null)

function toUser(accessToken: string): AuthUser | null {
  const claims = decodeAccessToken(accessToken)
  if (!claims) return null
  return { id: claims.sub, email: claims.email }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [status, setStatus] = useState<AuthContextValue['status']>('loading')

  const applyToken = useCallback((accessToken: string) => {
    setAccessToken(accessToken)
    setUser(toUser(accessToken))
    setStatus('authenticated')
  }, [])

  const clearSession = useCallback(() => {
    setAccessToken(null)
    setUser(null)
    setStatus('unauthenticated')
  }, [])

  useEffect(() => {
    registerUnauthorizedHandler(async () => {
      try {
        const result = await authApi.refresh()
        applyToken(result.accessToken)
        return result.accessToken
      } catch {
        clearSession()
        return null
      }
    })
  }, [applyToken, clearSession])

  useEffect(() => {
    authApi
      .refresh()
      .then((result) => applyToken(result.accessToken))
      .catch(() => clearSession())
  }, [applyToken, clearSession])

  const login = useCallback(
    async (email: string, password: string) => {
      const result = await authApi.login(email, password)
      applyToken(result.accessToken)
    },
    [applyToken],
  )

  const registerUser = useCallback(
    async (email: string, password: string) => {
      const result = await authApi.register(email, password)
      applyToken(result.accessToken)
    },
    [applyToken],
  )

  const logout = useCallback(async () => {
    try {
      await authApi.logout()
    } finally {
      clearSession()
    }
  }, [clearSession])

  const value = useMemo(
    () => ({ user, status, login, register: registerUser, logout }),
    [user, status, login, registerUser, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth() {
  const context = useContext(AuthContext)
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider')
  }
  return context
}
