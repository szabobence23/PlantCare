import { ArrowLeft, Camera, Leaf, Trash2, X } from 'lucide-react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { api } from '../api/api'
import { ApiError } from '../api/client'
import type { CareType, PlantDetail } from '../api/types'
import { AuthImage } from '../components/AuthImage'
import { CareIcon } from '../components/CareIcon'
import { StatusBadge } from '../components/StatusBadge'
import { careDoneLabel, careLabel, formatDate, formatDateTime, relativeDue } from '../lib/care'

/** Schedule, care history, growth photos and delete (spec AC3, AC4, AC7, AC8). */
export function PlantDetailPage() {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const [plant, setPlant] = useState<PlantDetail | null>(null)
  const [notFound, setNotFound] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState<string | null>(null)
  const [confirmDelete, setConfirmDelete] = useState(false)
  const [viewing, setViewing] = useState<string | null>(null)
  const fileInput = useRef<HTMLInputElement>(null)

  const load = useCallback(
    () =>
      api
        .getPlant(id)
        .then(setPlant)
        .catch((e: Error) => (e instanceof ApiError && e.status === 404 ? setNotFound(true) : setError(e.message))),
    [id],
  )

  useEffect(() => {
    void load()
  }, [load])

  async function run(key: string, action: () => Promise<unknown>) {
    setBusy(key)
    setError(null)
    try {
      await action()
      await load()
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setBusy(null)
    }
  }

  const recordCare = (type: CareType) => run(type, () => api.recordCare(id, type))

  const uploadPhoto = (file: File | undefined) => {
    if (fileInput.current) fileInput.current.value = ''
    if (!file) return
    void run('photo', async () => {
      try {
        await api.uploadPhoto(id, file)
      } catch (e) {
        throw e instanceof ApiError && e.fieldErrors.photo ? new Error(e.fieldErrors.photo) : e
      }
    })
  }

  async function deletePlant() {
    setBusy('delete')
    try {
      await api.deletePlant(id)
      navigate('/', { replace: true })
    } catch (e) {
      setError((e as Error).message)
      setBusy(null)
    }
  }

  if (notFound) {
    return (
      <div className="empty-state">
        <h2>A növény nem található</h2>
        <Link to="/" className="btn btn-ghost">
          Vissza a listához
        </Link>
      </div>
    )
  }
  if (!plant) return error ? <div className="alert alert-error">{error}</div> : <p className="muted">Betöltés…</p>

  const latestPhoto = plant.photos.at(-1)

  return (
    <div className="detail">
      <Link to="/" className="back-link">
        <ArrowLeft size={16} aria-hidden /> Növényeim
      </Link>

      <section className="detail-hero">
        {latestPhoto ? (
          <AuthImage plantId={plant.id} photoId={latestPhoto.id} alt={plant.name} className="hero-img" />
        ) : (
          <div className="hero-img thumb-empty">
            <Leaf size={36} aria-hidden />
          </div>
        )}
        <div className="detail-title">
          <h1>{plant.name}</h1>
          <p className="species">{plant.species ?? 'Faj nincs megadva'}</p>
          <p className="muted small">Hozzáadva: {formatDateTime(plant.createdAt)}</p>
        </div>
        <div className="detail-actions">
          {confirmDelete ? (
            <div className="confirm" role="alertdialog" aria-label="Törlés megerősítése">
              <span>Biztosan törlöd? A képek és az előzmények is törlődnek.</span>
              <div className="confirm-buttons">
                <button type="button" className="btn btn-ghost btn-sm" onClick={() => setConfirmDelete(false)}>
                  Mégse
                </button>
                <button type="button" className="btn btn-danger btn-sm" onClick={deletePlant} disabled={busy !== null}>
                  Végleges törlés
                </button>
              </div>
            </div>
          ) : (
            <button type="button" className="btn btn-ghost btn-sm danger-text" onClick={() => setConfirmDelete(true)}>
              <Trash2 size={16} aria-hidden /> Törlés
            </button>
          )}
        </div>
      </section>

      {error && (
        <div className="alert alert-error" role="alert">
          {error}
        </div>
      )}

      <section className="care-cards">
        {(['watering', 'feeding'] as const).map((type) => {
          const task = plant[type]
          return (
            <div key={type} className={`care-card accent-${task.status}`} data-testid={`care-${type}`}>
              <div className="care-card-head">
                <CareIcon type={type} size={18} />
                <h2>{careLabel[type]}</h2>
                <StatusBadge status={task.status} />
              </div>
              <div className="care-next">
                <span className="care-next-label">Következő</span>
                <span className="care-next-date">{formatDate(task.nextDueOn)}</span>
                <span className="muted">{relativeDue(task)}</span>
              </div>
              <dl className="care-meta">
                <div>
                  <dt>Gyakoriság</dt>
                  <dd>{task.intervalDays} naponta</dd>
                </div>
                <div>
                  <dt>Legutóbb</dt>
                  <dd>{formatDate(task.lastDoneOn)}</dd>
                </div>
              </dl>
              <button type="button" className="btn btn-primary btn-block" onClick={() => recordCare(type)} disabled={busy !== null}>
                {busy === type ? 'Mentés…' : careDoneLabel[type]}
              </button>
            </div>
          )
        })}
      </section>

      <div className="detail-columns">
        <section className="panel">
          <h2>Előzmények</h2>
          {plant.history.length === 0 ? (
            <p className="muted">Még nincs rögzített gondozás.</p>
          ) : (
            <ul className="history" data-testid="history">
              {plant.history.map((event) => (
                <li key={event.id}>
                  <CareIcon type={event.type} />
                  <span>{careLabel[event.type]}</span>
                  <span className="muted">{formatDate(event.date)}</span>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="panel">
          <div className="panel-head">
            <h2>Fejlődés</h2>
            <label className="btn btn-ghost btn-sm" htmlFor="growth-photo">
              <Camera size={16} aria-hidden /> {busy === 'photo' ? 'Feltöltés…' : 'Kép feltöltése'}
            </label>
            <input
              ref={fileInput}
              id="growth-photo"
              type="file"
              accept="image/jpeg,image/png"
              className="visually-hidden"
              onChange={(e) => uploadPhoto(e.target.files?.[0])}
              disabled={busy !== null}
            />
          </div>
          {plant.photos.length === 0 ? (
            <p className="muted">Tölts fel képeket rendszeresen, hogy lásd, hogyan fejlődik.</p>
          ) : (
            <ul className="gallery" data-testid="gallery">
              {plant.photos.map((photo) => (
                <li key={photo.id}>
                  <AuthImage plantId={plant.id} photoId={photo.id} alt={`${plant.name}, ${formatDateTime(photo.uploadedAt)}`} className="gallery-img" onClick={() => setViewing(photo.id)} />
                  <span className="gallery-date">{formatDateTime(photo.uploadedAt)}</span>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>

      {viewing && (
        <div className="lightbox" role="dialog" aria-label="Kép nagyítva" onClick={() => setViewing(null)}>
          <button type="button" className="btn btn-ghost btn-icon lightbox-close" aria-label="Bezárás">
            <X size={20} aria-hidden />
          </button>
          <AuthImage plantId={plant.id} photoId={viewing} alt={plant.name} className="lightbox-img" />
        </div>
      )}
    </div>
  )
}
