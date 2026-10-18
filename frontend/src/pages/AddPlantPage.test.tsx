import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import { api } from '../api/api'
import { ApiError } from '../api/client'
import { renderPage } from '../test/render'
import { AddPlantPage } from './AddPlantPage'

vi.mock('../api/api', () => ({ api: { identify: vi.fn(), createPlant: vi.fn() } }))

const photo = () => new File([new Uint8Array([0xff, 0xd8, 0xff, 0xe0])], 'fikusz.jpg', { type: 'image/jpeg' })

describe('AddPlantPage', () => {
  beforeEach(() => {
    vi.resetAllMocks()
    URL.createObjectURL = vi.fn(() => 'blob:preview')
    URL.revokeObjectURL = vi.fn()
  })

  it('AC5: shows the server error for a zero interval and stays on the form', async () => {
    vi.mocked(api.createPlant).mockRejectedValue(
      new ApiError(400, 'Hibás adatok.', {
        wateringIntervalDays: 'A locsolási gyakoriságnak pozitív egész számnak kell lennie.',
      }),
    )
    renderPage(<AddPlantPage />)

    await userEvent.type(screen.getByLabelText('Név'), 'Fikusz')
    await userEvent.clear(screen.getByLabelText('Locsolás gyakorisága (nap)'))
    await userEvent.type(screen.getByLabelText('Locsolás gyakorisága (nap)'), '0')
    await userEvent.click(screen.getByRole('button', { name: 'Növény mentése' }))

    expect(await screen.findByText('A locsolási gyakoriságnak pozitív egész számnak kell lennie.')).toBeInTheDocument()
    expect(screen.getByText(/nem lett elmentve/)).toBeInTheDocument()
    expect(screen.getByLabelText('Locsolás gyakorisága (nap)')).toHaveAttribute('aria-invalid', 'true')
    expect(screen.queryByText('navigated-away')).not.toBeInTheDocument()
    expect(vi.mocked(api.createPlant).mock.calls[0][0].wateringIntervalDays).toBe('0')
  })

  it('AC6: a recognised species is applied only after accepting it, and stays editable', async () => {
    vi.mocked(api.identify).mockResolvedValue({
      suggestions: [
        { scientificName: 'Ficus elastica', commonNames: ['Gumifa'], score: 0.81 },
        { scientificName: 'Ficus lyrata', commonNames: [], score: 0.1 },
      ],
    })
    renderPage(<AddPlantPage />)

    await userEvent.upload(screen.getByLabelText('Fénykép (opcionális)'), photo())

    const panel = await screen.findByTestId('recognition')
    expect(panel).toHaveTextContent('Ficus elastica')
    expect(panel).toHaveTextContent('81%')
    expect(screen.getByLabelText('Faj')).toHaveValue('') // not applied automatically

    await userEvent.click(screen.getAllByRole('button', { name: 'Elfogadom' })[0])
    expect(screen.getByLabelText('Faj')).toHaveValue('Ficus elastica')

    await userEvent.clear(screen.getByLabelText('Faj'))
    await userEvent.type(screen.getByLabelText('Faj'), 'Ficus elastica Tineke')
    expect(screen.getByLabelText('Faj')).toHaveValue('Ficus elastica Tineke')
  })

  it('AC6: when recognition is unavailable the species can still be typed and the plant saved with the photo', async () => {
    vi.mocked(api.identify).mockRejectedValue(new ApiError(503, 'A növényfelismerés jelenleg nem érhető el, add meg a fajt kézzel.'))
    vi.mocked(api.createPlant).mockResolvedValue({ id: 'p1' } as never)
    renderPage(<AddPlantPage />)

    const file = photo()
    await userEvent.upload(screen.getByLabelText('Fénykép (opcionális)'), file)
    expect(await screen.findByText(/add meg a fajt kézzel/)).toBeInTheDocument()

    await userEvent.type(screen.getByLabelText('Név'), 'Fikusz')
    await userEvent.type(screen.getByLabelText('Faj'), 'Ficus elastica')
    await userEvent.click(screen.getByRole('button', { name: 'Növény mentése' }))

    await waitFor(() => expect(api.createPlant).toHaveBeenCalled())
    const sent = vi.mocked(api.createPlant).mock.calls[0][0]
    expect(sent.species).toBe('Ficus elastica')
    expect(sent.photo).toBe(file)
  })

  it('AC6: an invalid file is reported under the photo field', async () => {
    vi.mocked(api.identify).mockRejectedValue(new ApiError(400, 'Hibás adatok.', { photo: 'Csak JPEG vagy PNG kép tölthető fel.' }))
    renderPage(<AddPlantPage />)

    await userEvent.upload(screen.getByLabelText('Fénykép (opcionális)'), photo())

    expect(await screen.findByText('Csak JPEG vagy PNG kép tölthető fel.')).toBeInTheDocument()
    expect(screen.queryByTestId('recognition')).not.toBeInTheDocument()
  })
})
