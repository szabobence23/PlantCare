import type { CareStatus } from '../api/types'
import { statusLabel } from '../lib/care'

export function StatusBadge({ status }: { status: CareStatus }) {
  return <span className={`badge badge-${status}`}>{statusLabel[status]}</span>
}
