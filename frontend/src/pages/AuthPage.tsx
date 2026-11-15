import { Leaf } from 'lucide-react'
import { useState, type FormEvent } from 'react'
import { Link, Navigate, useLocation, useNavigate } from 'react-router-dom'
import { ApiError } from '../api/client'
import { useAuth } from '../auth/useAuth'
import { Field } from '../components/Field'

/** Login and registration share one form (spec AC1). Validation happens on the server. */
export function AuthPage({ mode }: { mode: 'login' | 'register' }) {
  const { email: signedInAs, login, register } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)

  const isLogin = mode === 'login'
  const from = (location.state as { from?: string } | null)?.from ?? '/'

  if (signedInAs) return <Navigate to={from} replace />

  async function submit(e: FormEvent) {
    e.preventDefault()
    setBusy(true)
    setError(null)
    setFieldErrors({})
    try {
      await (isLogin ? login : register)(email, password)
      navigate(from, { replace: true })
    } catch (err) {
      if (err instanceof ApiError && Object.keys(err.fieldErrors).length > 0) setFieldErrors(err.fieldErrors)
      else setError(err instanceof Error ? err.message : 'Váratlan hiba történt.')
    } finally {
      setBusy(false)
    }
  }

  return (
    <div className="auth-screen">
      <form className="auth-card" onSubmit={submit} noValidate>
        <div className="auth-brand">
          <Leaf size={28} aria-hidden />
          <span>PlantCare</span>
        </div>
        <h1>{isLogin ? 'Bejelentkezés' : 'Regisztráció'}</h1>
        <p className="muted">{isLogin ? 'Üdv újra! ' : 'Hozz létre fiókot a növényeid nyilvántartásához.'}</p>

        {error && (
          <div className="alert alert-error" role="alert">
            {error}
          </div>
        )}

        <Field id="email" label="E-mail cím" error={fieldErrors.email}>
          <input
            id="email"
            type="email"
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            aria-invalid={!!fieldErrors.email}
            aria-describedby="email-error"
            autoFocus
          />
        </Field>
        <Field
          id="password"
          label="Jelszó"
          error={fieldErrors.password}
          hint={isLogin ? undefined : 'Legalább 8 karakter.'}
        >
          <input
            id="password"
            type="password"
            autoComplete={isLogin ? 'current-password' : 'new-password'}
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            aria-invalid={!!fieldErrors.password}
            aria-describedby="password-error"
          />
        </Field>

        <button type="submit" className="btn btn-primary btn-block" disabled={busy}>
          {busy ? 'Pillanat…' : isLogin ? 'Bejelentkezés' : 'Fiók létrehozása'}
        </button>

        <p className="auth-switch muted">
          {isLogin ? (
            <>
              Még nincs fiókod? <Link to="/register">Regisztrálj</Link>
            </>
          ) : (
            <>
              Már van fiókod? <Link to="/login">Jelentkezz be</Link>
            </>
          )}
        </p>
      </form>
    </div>
  )
}
