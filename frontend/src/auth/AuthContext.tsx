import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react'
import { api } from '../api/api'
import { onUnauthorized, session } from '../api/client'

interface AuthContextValue {
  email: string | null
  login: (email: string, password: string) => Promise<void>
  register: (email: string, password: string) => Promise<void>
  logout: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

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

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>')
  return ctx
}
