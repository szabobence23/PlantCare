import type { ReactNode } from 'react'

interface Props {
  id: string
  label: string
  error?: string
  hint?: ReactNode
  children: ReactNode
}

/** Label + control + inline error. The control should set aria-describedby={`${id}-error`}. */
export function Field({ id, label, error, hint, children }: Props) {
  return (
    <div className={`field ${error ? 'field-invalid' : ''}`}>
      <label htmlFor={id}>{label}</label>
      {children}
      {hint && !error && <div className="field-hint">{hint}</div>}
      {error && (
        <div className="field-error" id={`${id}-error`} role="alert">
          {error}
        </div>
      )}
    </div>
  )
}
