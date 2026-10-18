// Mirrors backend/src/PlantCare.Api/Contracts.

export type CareType = 'watering' | 'feeding'
export type CareStatus = 'ok' | 'upcoming' | 'due' | 'overdue'

export interface CareTask {
  lastDoneOn: string // yyyy-MM-dd
  intervalDays: number
  nextDueOn: string
  daysUntilDue: number
  status: CareStatus
}

export interface PlantSummary {
  id: string
  name: string
  species: string | null
  watering: CareTask
  feeding: CareTask
  latestPhotoId: string | null
}

export interface CareEvent {
  id: string
  type: CareType
  date: string
  recordedAt: string
}

export interface Photo {
  id: string
  uploadedAt: string
  sizeBytes: number
}

export interface PlantDetail {
  id: string
  name: string
  species: string | null
  createdAt: string
  watering: CareTask
  feeding: CareTask
  history: CareEvent[]
  photos: Photo[]
}

export interface SpeciesSuggestion {
  scientificName: string
  commonNames: string[]
  score: number
}

export interface AuthResponse {
  token: string
  email: string
}

export interface NewPlant {
  name: string
  species: string
  wateringIntervalDays: string
  feedingIntervalDays: string
  lastWateredOn: string
  lastFedOn: string
  photo: File | null
}
