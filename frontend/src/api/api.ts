import { request, requestJson } from './client'
import type {
  AuthResponse,
  CareType,
  NewPlant,
  Photo,
  PlantDetail,
  PlantSummary,
  SpeciesSuggestion,
} from './types'

export const api = {
  register: (email: string, password: string) =>
    requestJson<AuthResponse>('/api/auth/register', { method: 'POST', body: JSON.stringify({ email, password }) }),

  login: (email: string, password: string) =>
    requestJson<AuthResponse>('/api/auth/login', { method: 'POST', body: JSON.stringify({ email, password }) }),

  listPlants: () => requestJson<PlantSummary[]>('/api/plants'),

  getPlant: (id: string) => requestJson<PlantDetail>(`/api/plants/${id}`),

  createPlant: (plant: NewPlant) => {
    const form = new FormData()
    form.set('name', plant.name)
    form.set('species', plant.species)
    form.set('wateringIntervalDays', plant.wateringIntervalDays)
    form.set('feedingIntervalDays', plant.feedingIntervalDays)
    form.set('lastWateredOn', plant.lastWateredOn)
    form.set('lastFedOn', plant.lastFedOn)
    if (plant.photo) form.set('photo', plant.photo)
    return requestJson<PlantDetail>('/api/plants', { method: 'POST', body: form })
  },

  deletePlant: (id: string) => request(`/api/plants/${id}`, { method: 'DELETE' }).then(() => undefined),

  recordCare: (id: string, type: CareType) =>
    requestJson<PlantSummary>(`/api/plants/${id}/care-events`, { method: 'POST', body: JSON.stringify({ type }) }),

  uploadPhoto: (id: string, photo: File) => {
    const form = new FormData()
    form.set('photo', photo)
    return requestJson<Photo>(`/api/plants/${id}/photos`, { method: 'POST', body: form })
  },

  /** Raw image bytes; needs the auth header, so it cannot be a plain <img src>. */
  photoBlob: (plantId: string, photoId: string) =>
    request(`/api/plants/${plantId}/photos/${photoId}`).then((r) => r.blob()),

  identify: (photo: File) => {
    const form = new FormData()
    form.set('photo', photo)
    return requestJson<{ suggestions: SpeciesSuggestion[] }>('/api/plant-recognition', { method: 'POST', body: form })
  },
}
