interface AccessTokenClaims {
  sub: string
  email: string
  exp: number
}

export function decodeAccessToken(token: string): AccessTokenClaims | null {
  try {
    const payload = token.split('.')[1]
    const json = atob(payload.replace(/-/g, '+').replace(/_/g, '/'))
    return JSON.parse(json) as AccessTokenClaims
  } catch {
    return null
  }
}
