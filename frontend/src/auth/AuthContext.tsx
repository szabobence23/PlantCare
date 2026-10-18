import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api } from '../api/api'
import { onUnauthorized, session } from '../api/client'
import { AuthContext, type AuthContextValue } from './useAuth'

/** Holds the signed-in user's e-mail; the JWT itself lives in localStorage (see api/client.ts). */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [email, setEmail] = useState<string | null>(() => (session.token() ? session.email() : null))

  const logout = useCallback(() => {
    session.clear()
    setEmail(null)
  }, [])

  useEffect(() => {
    onUnauthorized(logout)
    return () => onUnauthorized(null)
  }, [logout])

  const value = useMemo<AuthContextValue>(
    () => ({
      email,
      logout,
      login: async (e, p) => {
        const auth = await api.login(e, p)
        session.save(auth.token, auth.email)
        setEmail(auth.email)
      },
      register: async (e, p) => {
        const auth = await api.register(e, p)
        session.save(auth.token, auth.email)
        setEmail(auth.email)
      },
    }),
    [email, logout],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}
