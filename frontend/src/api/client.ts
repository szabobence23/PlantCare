/** Error returned by the API, with field-level messages for 400 responses. */
export class ApiError extends Error {
  readonly status: number
  readonly fieldErrors: Record<string, string>

  constructor(status: number, message: string, fieldErrors: Record<string, string> = {}) {
    super(message)
    this.status = status
    this.fieldErrors = fieldErrors
  }
}

const TOKEN_KEY = 'plantcare.token'
const EMAIL_KEY = 'plantcare.email'

let unauthorizedHandler: (() => void) | null = null

export const session = {
  token: () => localStorage.getItem(TOKEN_KEY),
  email: () => localStorage.getItem(EMAIL_KEY),
  save: (token: string, email: string) => {
    localStorage.setItem(TOKEN_KEY, token)
    localStorage.setItem(EMAIL_KEY, email)
  },
  clear: () => {
    localStorage.removeItem(TOKEN_KEY)
    localStorage.removeItem(EMAIL_KEY)
  },
}

/** Called when a signed-in request gets 401 (e.g. expired token). */
export function onUnauthorized(handler: (() => void) | null) {
  unauthorizedHandler = handler
}

interface ProblemBody {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

async function toApiError(response: Response): Promise<ApiError> {
  let body: ProblemBody = {}
  try {
    body = await response.json()
  } catch {
    // Not JSON (e.g. a proxy error page): fall back to a generic message.
  }
  const fieldErrors = Object.fromEntries(
    Object.entries(body.errors ?? {}).map(([field, messages]) => [field, messages[0] ?? '']),
  )
  const message =
    body.detail ??
    body.title ??
    (response.status >= 500 ? 'A szerver nem érhető el, próbáld újra később.' : 'Váratlan hiba történt.')
  return new ApiError(response.status, message, fieldErrors)
}

export async function request(path: string, init: RequestInit = {}): Promise<Response> {
  const token = session.token()
  const headers = new Headers(init.headers)
  if (token) headers.set('Authorization', `Bearer ${token}`)
  if (init.body && !(init.body instanceof FormData)) headers.set('Content-Type', 'application/json')

  let response: Response
  try {
    response = await fetch(path, { ...init, headers })
  } catch {
    throw new ApiError(0, 'Nem sikerült elérni a szervert.')
  }

  if (!response.ok) {
    if (response.status === 401 && token) unauthorizedHandler?.()
    throw await toApiError(response)
  }
  return response
}

export async function requestJson<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await request(path, init)
  return (await response.json()) as T
}
