import { apiClient } from './client'

export interface AccessTokenResponse {
  accessToken: string
  expiresAt: string
}

export function register(email: string, password: string) {
  return apiClient.post<AccessTokenResponse>('/auth/register', { email, password }).then((res) => res.data)
}

export function login(email: string, password: string) {
  return apiClient.post<AccessTokenResponse>('/auth/login', { email, password }).then((res) => res.data)
}

export function refresh() {
  return apiClient.post<AccessTokenResponse>('/auth/refresh').then((res) => res.data)
}

export function logout() {
  return apiClient.post('/auth/logout')
}
