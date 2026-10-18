import { render } from '@testing-library/react'
import type { ReactElement } from 'react'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { AuthProvider } from '../auth/AuthContext'

/** Renders a page inside the router/auth providers; other routes render a marker text. */
export function renderPage(element: ReactElement, { path = '/', url = '/' } = {}) {
  return render(
    <AuthProvider>
      <MemoryRouter initialEntries={[url]}>
        <Routes>
          <Route path={path} element={element} />
          <Route path="*" element={<div>navigated-away</div>} />
        </Routes>
      </MemoryRouter>
    </AuthProvider>,
  )
}
