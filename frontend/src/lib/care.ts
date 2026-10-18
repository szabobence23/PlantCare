import type { CareStatus, CareTask, CareType } from '../api/types'

export const statusLabel: Record<CareStatus, string> = {
  overdue: 'Lejárt',
  due: 'Esedékes',
  upcoming: 'Közeledik',
  ok: 'Rendben',
}

export const careLabel: Record<CareType, string> = {
  watering: 'Locsolás',
  feeding: 'Tápozás',
}

export const careDoneLabel: Record<CareType, string> = {
  watering: 'Meglocsoltam',
  feeding: 'Megtápoztam',
}

/** "ma", "holnap", "3 nap múlva", "2 napja esedékes". */
export function relativeDue(task: Pick<CareTask, 'daysUntilDue'>): string {
  const d = task.daysUntilDue
  if (d < 0) return `${-d} napja esedékes`
  if (d === 0) return 'ma'
  if (d === 1) return 'holnap'
  return `${d} nap múlva`
}

/** yyyy-MM-dd as a local calendar date (no time-zone shift). */
export function parseDate(isoDate: string): Date {
  const [y, m, d] = isoDate.split('-').map(Number)
  return new Date(y, m - 1, d)
}

const dateFormat = new Intl.DateTimeFormat('hu-HU', { year: 'numeric', month: 'long', day: 'numeric' })
const dateTimeFormat = new Intl.DateTimeFormat('hu-HU', { dateStyle: 'medium', timeStyle: 'short' })

export const formatDate = (isoDate: string) => dateFormat.format(parseDate(isoDate))
export const formatDateTime = (isoDateTime: string) => dateTimeFormat.format(new Date(isoDateTime))

/** Today's date in the browser's time zone as yyyy-MM-dd (default for the date inputs). */
export function todayIso(now = new Date()): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${now.getFullYear()}-${pad(now.getMonth() + 1)}-${pad(now.getDate())}`
}

/** The more urgent of the tasks decides the card accent. */
export function mostUrgent(...tasks: CareTask[]): CareStatus {
  const order: CareStatus[] = ['overdue', 'due', 'upcoming', 'ok']
  return order.find((s) => tasks.some((t) => t.status === s)) ?? 'ok'
}
