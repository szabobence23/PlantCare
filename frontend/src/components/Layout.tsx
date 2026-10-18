import { Leaf, LogOut, Plus } from 'lucide-react'
import { Link, Navigate, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../auth/useAuth'

/** Shell of the signed-in part of the app; redirects to login when there is no session. */
export function Layout() {
  const { email, logout } = useAuth()
  const location = useLocation()

  if (!email) return <Navigate to="/login" replace state={{ from: location.pathname }} />

  return (
    <div className="shell">
      <header className="topbar">
        <Link to="/" className="brand">
          <Leaf size={20} aria-hidden />
          <span>PlantCare</span>
        </Link>
        <div className="topbar-actions">
          <Link to="/plants/new" className="btn btn-primary btn-sm">
            <Plus size={16} aria-hidden />
            <span>Új növény</span>
          </Link>
          <span className="user-email" title={email}>
            {email}
          </span>
          <button type="button" className="btn btn-ghost btn-icon" onClick={logout} aria-label="Kijelentkezés" title="Kijelentkezés">
            <LogOut size={18} aria-hidden />
          </button>
        </div>
      </header>
      <main className="content">
        <Outlet />
      </main>
    </div>
  )
}
