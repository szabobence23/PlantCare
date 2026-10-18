import { describe, expect, it } from 'vitest'
import type { CareTask } from '../api/types'
import { formatDate, mostUrgent, relativeDue, statusLabel, todayIso } from './care'

const task = (status: CareTask['status'], daysUntilDue = 0): CareTask => ({
  lastDoneOn: '2026-10-11',
  intervalDays: 7,
  nextDueOn: '2026-10-18',
  daysUntilDue,
  status,
})

describe('care helpers (AC3)', () => {
  it('describes the due date relative to today', () => {
    expect(relativeDue({ daysUntilDue: -2 })).toBe('2 napja esedékes')
    expect(relativeDue({ daysUntilDue: 0 })).toBe('ma')
    expect(relativeDue({ daysUntilDue: 1 })).toBe('holnap')
    expect(relativeDue({ daysUntilDue: 5 })).toBe('5 nap múlva')
  })

  it('has a Hungarian label for every status', () => {
    expect(statusLabel).toEqual({ overdue: 'Lejárt', due: 'Esedékes', upcoming: 'Közeledik', ok: 'Rendben' })
  })

  it('picks the most urgent status of a plant', () => {
    expect(mostUrgent(task('ok'), task('upcoming'))).toBe('upcoming')
    expect(mostUrgent(task('due'), task('overdue'))).toBe('overdue')
    expect(mostUrgent(task('ok'), task('ok'))).toBe('ok')
  })

  it('formats calendar dates without a time-zone shift', () => {
    expect(formatDate('2026-10-18')).toBe('2026. október 18.')
    expect(todayIso(new Date(2026, 0, 5, 23, 59))).toBe('2026-01-05')
  })
})
