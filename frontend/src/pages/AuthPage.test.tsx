import { screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { api } from '../api/api'
import { ApiError, session } from '../api/client'
import { renderPage } from '../test/render'
import { AuthPage } from './AuthPage'

vi.mock('../api/api', () => ({ api: { login: vi.fn(), register: vi.fn() } }))

describe('AuthPage (AC1)', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    localStorage.clear()
  })

  it('shows the server message when the login fails', async () => {
    vi.mocked(api.login).mockRejectedValue(new ApiError(401, 'Hibás e-mail cím vagy jelszó.'))
    renderPage(<AuthPage mode="login" />)

    await userEvent.type(screen.getByLabelText('E-mail cím'), 'anna@example.com')
    await userEvent.type(screen.getByLabelText('Jelszó'), 'rosszjelszo')
    await userEvent.click(screen.getByRole('button', { name: 'Bejelentkezés' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Hibás e-mail cím vagy jelszó.')
    expect(session.token()).toBeNull()
  })

  it('shows field errors returned for invalid registration data', async () => {
    vi.mocked(api.register).mockRejectedValue(
      new ApiError(400, 'Hibás adatok.', {
        email: 'Érvénytelen e-mail cím.',
        password: 'A jelszónak legalább 8 karakterből kell állnia.',
      }),
    )
    renderPage(<AuthPage mode="register" />)

    await userEvent.type(screen.getByLabelText('E-mail cím'), 'nem-email')
    await userEvent.type(screen.getByLabelText('Jelszó'), 'rovid')
    await userEvent.click(screen.getByRole('button', { name: 'Fiók létrehozása' }))

    expect(await screen.findByText('Érvénytelen e-mail cím.')).toBeInTheDocument()
    expect(screen.getByText('A jelszónak legalább 8 karakterből kell állnia.')).toBeInTheDocument()
    expect(screen.getByLabelText('E-mail cím')).toHaveAttribute('aria-invalid', 'true')
  })

  it('stores the session and leaves the page after a successful registration', async () => {
    vi.mocked(api.register).mockResolvedValue({ token: 'tok', email: 'anna@example.com' })
    renderPage(<AuthPage mode="register" />, { path: '/register', url: '/register' })

    await userEvent.type(screen.getByLabelText('E-mail cím'), 'anna@example.com')
    await userEvent.type(screen.getByLabelText('Jelszó'), 'jelszo123')
    await userEvent.click(screen.getByRole('button', { name: 'Fiók létrehozása' }))

    expect(await screen.findByText('navigated-away')).toBeInTheDocument()
    expect(session.token()).toBe('tok')
    expect(api.register).toHaveBeenCalledWith('anna@example.com', 'jelszo123')
  })
})
