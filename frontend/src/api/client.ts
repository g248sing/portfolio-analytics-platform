import axios from 'axios'

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5199/api'

export const apiClient = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
})

let accessToken: string | null = null

export function setAccessToken(token: string | null) {
  accessToken = token
}

apiClient.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }
  return config
})

type RefreshHandler = () => Promise<string | null>

let onUnauthorized: RefreshHandler | null = null

export function registerUnauthorizedHandler(handler: RefreshHandler) {
  onUnauthorized = handler
}

let refreshPromise: Promise<string | null> | null = null

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config
    const isAuthEndpoint = originalRequest?.url?.includes('/auth/')

    if (error.response?.status === 401 && !originalRequest._retry && !isAuthEndpoint && onUnauthorized) {
      originalRequest._retry = true

      refreshPromise ??= onUnauthorized().finally(() => {
        refreshPromise = null
      })

      const newToken = await refreshPromise
      if (newToken) {
        originalRequest.headers.Authorization = `Bearer ${newToken}`
        return apiClient(originalRequest)
      }
    }

    return Promise.reject(error)
  },
)
