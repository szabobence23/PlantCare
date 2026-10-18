import { Droplet, Sprout } from 'lucide-react'
import type { CareType } from '../api/types'

export function CareIcon({ type, size = 16 }: { type: CareType; size?: number }) {
  return type === 'watering' ? (
    <Droplet size={size} className="icon-water" aria-hidden />
  ) : (
    <Sprout size={size} className="icon-feed" aria-hidden />
  )
}
