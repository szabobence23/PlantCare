import { ArrowLeft, Check, ImagePlus, Sparkles, X } from 'lucide-react'
import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api } from '../api/api'
import { ApiError } from '../api/client'
import type { NewPlant, SpeciesSuggestion } from '../api/types'
import { Field } from '../components/Field'
import { todayIso } from '../lib/care'

type Recognition =
  | { state: 'idle' }
  | { state: 'loading' }
  | { state: 'done'; suggestions: SpeciesSuggestion[] }
  | { state: 'unavailable'; message: string }

/**
 * New plant form (spec AC2, AC5, AC6). An optional photo is sent to the recognition API right
 * after selection; the suggested species is only applied when the user accepts it, and the
 * species field stays editable either way. All validation messages come from the server.
 */
export function AddPlantPage() {
  const navigate = useNavigate()
  const today = todayIso()
  const [form, setForm] = useState<NewPlant>({
    name: '',
    species: '',
    wateringIntervalDays: '7',
    feedingIntervalDays: '30',
    lastWateredOn: today,
    lastFedOn: today,
    photo: null,
  })
  const [previewUrl, setPreviewUrl] = useState<string | null>(null)
  const [recognition, setRecognition] = useState<Recognition>({ state: 'idle' })
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({})
  const [error, setError] = useState<string | null>(null)
  const [saving, setSaving] = useState(false)
  const recognitionRun = useRef(0)
  const fileInput = useRef<HTMLInputElement>(null)

  useEffect(() => () => {
    if (previewUrl) URL.revokeObjectURL(previewUrl)
  }, [previewUrl])

  const set = <K extends keyof NewPlant>(key: K, value: NewPlant[K]) => setForm((f) => ({ ...f, [key]: value }))

  async function choosePhoto(file: File | null) {
    const run = ++recognitionRun.current
    set('photo', file)
    setPreviewUrl(file ? URL.createObjectURL(file) : null)
    setFieldErrors((errs) => {
      const rest = { ...errs }
      delete rest.photo
      return rest
    })
    if (!file) {
      setRecognition({ state: 'idle' })
      return
    }

    setRecognition({ state: 'loading' })
    try {
      const { suggestions } = await api.identify(file)
      if (run === recognitionRun.current) setRecognition({ state: 'done', suggestions })
    } catch (e) {
      if (run !== recognitionRun.current) return
      if (e instanceof ApiError && e.fieldErrors.photo) {
        setFieldErrors((errs) => ({ ...errs, photo: e.fieldErrors.photo }))
        setRecognition({ state: 'idle' })
      } else {
        setRecognition({ state: 'unavailable', message: (e as Error).message })
      }
    }
  }

  function removePhoto() {
    if (fileInput.current) fileInput.current.value = ''
    void choosePhoto(null)
  }

  async function submit(e: FormEvent) {
    e.preventDefault()
    setSaving(true)
    setError(null)
    setFieldErrors({})
    try {
      const plant = await api.createPlant(form)
      navigate(`/plants/${plant.id}`)
    } catch (err) {
      if (err instanceof ApiError && Object.keys(err.fieldErrors).length > 0) {
        setFieldErrors(err.fieldErrors)
        setError('A növény nem lett elmentve. Javítsd a jelölt mezőket.')
      } else {
        setError(err instanceof Error ? err.message : 'Váratlan hiba történt.')
      }
    } finally {
      setSaving(false)
    }
  }

  const errorProps = (key: string) => ({ 'aria-invalid': !!fieldErrors[key], 'aria-describedby': `${key}-error` })

  return (
    <div className="narrow">
      <Link to="/" className="back-link">
        <ArrowLeft size={16} aria-hidden /> Vissza
      </Link>
      <h1>Új növény</h1>

      {error && (
        <div className="alert alert-error" role="alert">
          {error}
        </div>
      )}

      <form className="form-card" onSubmit={submit} noValidate>
        <Field id="photo" label="Fénykép (opcionális)" error={fieldErrors.photo} hint="JPEG vagy PNG, legfeljebb 10 MB.">
          {previewUrl ? (
            <div className="photo-preview">
              <img src={previewUrl} alt="A kiválasztott kép előnézete" />
              <button type="button" className="btn btn-ghost btn-icon photo-remove" onClick={removePhoto} aria-label="Kép eltávolítása">
                <X size={16} aria-hidden />
              </button>
            </div>
          ) : (
            <label htmlFor="photo" className="dropzone">
              <ImagePlus size={22} aria-hidden />
              <span>Kép kiválasztása</span>
            </label>
          )}
          <input
            ref={fileInput}
            id="photo"
            type="file"
            accept="image/jpeg,image/png"
            className="visually-hidden"
            onChange={(e) => void choosePhoto(e.target.files?.[0] ?? null)}
            {...errorProps('photo')}
          />
        </Field>

        <RecognitionPanel recognition={recognition} species={form.species} onAccept={(s) => set('species', s)} />

        <Field id="name" label="Név" error={fieldErrors.name}>
          <input id="name" value={form.name} onChange={(e) => set('name', e.target.value)} placeholder="pl. Nappali fikusz" {...errorProps('name')} />
        </Field>

        <Field id="species" label="Faj" error={fieldErrors.species} hint="Kép nélkül kézzel add meg, vagy fogadd el a javaslatot.">
          <input id="species" value={form.species} onChange={(e) => set('species', e.target.value)} placeholder="pl. Ficus elastica" {...errorProps('species')} />
        </Field>

        <div className="form-row">
          <Field id="wateringIntervalDays" label="Locsolás gyakorisága (nap)" error={fieldErrors.wateringIntervalDays}>
            <input
              id="wateringIntervalDays"
              type="number"
              inputMode="numeric"
              value={form.wateringIntervalDays}
              onChange={(e) => set('wateringIntervalDays', e.target.value)}
              {...errorProps('wateringIntervalDays')}
            />
          </Field>
          <Field id="lastWateredOn" label="Legutóbbi locsolás" error={fieldErrors.lastWateredOn}>
            <input id="lastWateredOn" type="date" max={today} value={form.lastWateredOn} onChange={(e) => set('lastWateredOn', e.target.value)} {...errorProps('lastWateredOn')} />
          </Field>
        </div>

        <div className="form-row">
          <Field id="feedingIntervalDays" label="Tápozás gyakorisága (nap)" error={fieldErrors.feedingIntervalDays}>
            <input
              id="feedingIntervalDays"
              type="number"
              inputMode="numeric"
              value={form.feedingIntervalDays}
              onChange={(e) => set('feedingIntervalDays', e.target.value)}
              {...errorProps('feedingIntervalDays')}
            />
          </Field>
          <Field id="lastFedOn" label="Legutóbbi tápozás" error={fieldErrors.lastFedOn}>
            <input id="lastFedOn" type="date" max={today} value={form.lastFedOn} onChange={(e) => set('lastFedOn', e.target.value)} {...errorProps('lastFedOn')} />
          </Field>
        </div>

        <div className="form-actions">
          <Link to="/" className="btn btn-ghost">
            Mégse
          </Link>
          <button type="submit" className="btn btn-primary" disabled={saving}>
            {saving ? 'Mentés…' : 'Növény mentése'}
          </button>
        </div>
      </form>
    </div>
  )
}

function RecognitionPanel({
  recognition,
  species,
  onAccept,
}: {
  recognition: Recognition
  species: string
  onAccept: (species: string) => void
}) {
  if (recognition.state === 'idle') return null
  if (recognition.state === 'loading') {
    return (
      <div className="recognition" aria-live="polite">
        <Sparkles size={16} aria-hidden /> Felismerés folyamatban…
      </div>
    )
  }
  if (recognition.state === 'unavailable') {
    return (
      <div className="recognition recognition-muted" role="status">
        {recognition.message}
      </div>
    )
  }
  if (recognition.suggestions.length === 0) {
    return (
      <div className="recognition recognition-muted" role="status">
        A képen nem sikerült növényt felismerni. Add meg a fajt kézzel.
      </div>
    )
  }

  const [best, ...others] = recognition.suggestions
  return (
    <div className="recognition" aria-live="polite" data-testid="recognition">
      <div className="recognition-title">
        <Sparkles size={16} aria-hidden /> Javasolt faj
      </div>
      <SuggestionRow suggestion={best} accepted={species === best.scientificName} onAccept={onAccept} primary />
      {others.length > 0 && (
        <>
          <div className="recognition-sub">További lehetőségek</div>
          {others.map((s) => (
            <SuggestionRow key={s.scientificName} suggestion={s} accepted={species === s.scientificName} onAccept={onAccept} />
          ))}
        </>
      )}
    </div>
  )
}

function SuggestionRow({
  suggestion,
  accepted,
  onAccept,
  primary = false,
}: {
  suggestion: SpeciesSuggestion
  accepted: boolean
  onAccept: (species: string) => void
  primary?: boolean
}) {
  return (
    <div className={`suggestion ${primary ? 'suggestion-primary' : ''}`}>
      <div className="suggestion-text">
        <span className="suggestion-name">{suggestion.scientificName}</span>
        {suggestion.commonNames.length > 0 && <span className="muted"> · {suggestion.commonNames.slice(0, 2).join(', ')}</span>}
      </div>
      <span className="suggestion-score">{Math.round(suggestion.score * 100)}%</span>
      {accepted ? (
        <span className="accepted">
          <Check size={14} aria-hidden /> Elfogadva
        </span>
      ) : (
        <button type="button" className={`btn btn-sm ${primary ? 'btn-primary' : 'btn-ghost'}`} onClick={() => onAccept(suggestion.scientificName)}>
          Elfogadom
        </button>
      )}
    </div>
  )
}
