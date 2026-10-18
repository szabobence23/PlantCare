import { screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { api } from '../api/api'
import type { PlantSummary } from '../api/types'
import { renderPage } from '../test/render'
import { PlantsPage } from './PlantsPage'

vi.mock('../api/api', () => ({ api: { listPlants: vi.fn(), recordCare: vi.fn(), photoBlob: vi.fn() } }))

const fikusz: PlantSummary = {
  id: 'p1',
  name: 'Fikusz',
  species: 'Ficus elastica',
  latestPhotoId: null,
  watering: { lastDoneOn: '2026-10-11', intervalDays: 7, nextDueOn: '2026-10-18', daysUntilDue: 0, status: 'due' },
  feeding: { lastDoneOn: '2026-10-17', intervalDays: 3, nextDueOn: '2026-10-20', daysUntilDue: 2, status: 'upcoming' },
}

describe('PlantsPage', () => {
  beforeEach(() => vi.resetAllMocks())

  it('AC3: shows due and approaching tasks', async () => {
    vi.mocked(api.listPlants).mockResolvedValue([fikusz])
    renderPage(<PlantsPage />)

    const watering = await screen.findByTestId('task-watering')
    expect(watering).toHaveTextContent('Esedékes')
    expect(watering).toHaveTextContent('ma')
    expect(screen.getByTestId('task-feeding')).toHaveTextContent('Közeledik')
    expect(screen.getByText(/1 teendő esedékes · 1 közeledik/)).toBeInTheDocument()
  })

  it('AC4: "Meglocsoltam" records the event and shows the new schedule', async () => {
    vi.mocked(api.listPlants).mockResolvedValue([fikusz])
    vi.mocked(api.recordCare).mockResolvedValue({
      ...fikusz,
      watering: { lastDoneOn: '2026-10-18', intervalDays: 7, nextDueOn: '2026-10-25', daysUntilDue: 7, status: 'ok' },
    })
    renderPage(<PlantsPage />)

    const watering = await screen.findByTestId('task-watering')
    await userEvent.click(within(watering).getByRole('button', { name: 'Meglocsoltam' }))

    expect(api.recordCare).toHaveBeenCalledWith('p1', 'watering')
    expect(await within(watering).findByText('Rendben')).toBeInTheDocument()
    expect(watering).toHaveTextContent('7 nap múlva')
  })

  it('shows an empty state when the user has no plants', async () => {
    vi.mocked(api.listPlants).mockResolvedValue([])
    renderPage(<PlantsPage />)
    expect(await screen.findByText('Még nincs növényed')).toBeInTheDocument()
  })
})
