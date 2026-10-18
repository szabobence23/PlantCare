import { Leaf, Plus } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { api } from '../api/api'
import type { CareType, PlantSummary } from '../api/types'
import { AuthImage } from '../components/AuthImage'
import { CareIcon } from '../components/CareIcon'
import { StatusBadge } from '../components/StatusBadge'
import { careDoneLabel, careLabel, formatDate, mostUrgent, relativeDue } from '../lib/care'

/** The user's plants with what is due or approaching (spec AC2, AC3, AC4). */
export function PlantsPage() {
  const [plants, setPlants] = useState<PlantSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState<string | null>(null)

  useEffect(() => {
    api
      .listPlants()
      .then(setPlants)
      .catch((e: Error) => setError(e.message))
  }, [])

  async function recordCare(plant: PlantSummary, type: CareType) {
    setBusy(`${plant.id}:${type}`)
    setError(null)
    try {
      const updated = await api.recordCare(plant.id, type)
      setPlants((list) => list?.map((p) => (p.id === updated.id ? updated : p)) ?? null)
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setBusy(null)
    }
  }

  if (!plants) {
    return error ? <div className="alert alert-error">{error}</div> : <p className="muted">Betöltés…</p>
  }

  const tasks = plants.flatMap((p) => [p.watering, p.feeding])
  const dueCount = tasks.filter((t) => t.status === 'due' || t.status === 'overdue').length
  const upcomingCount = tasks.filter((t) => t.status === 'upcoming').length

  return (
    <>
      <div className="page-header">
        <div>
          <h1>Növényeim</h1>
          {plants.length > 0 && (
            <p className="muted">
              {dueCount > 0 ? `${dueCount} teendő esedékes` : 'Ma nincs esedékes teendő'}
              {upcomingCount > 0 && ` · ${upcomingCount} közeledik`}
            </p>
          )}
        </div>
      </div>

      {error && (
        <div className="alert alert-error" role="alert">
          {error}
        </div>
      )}

      {plants.length === 0 ? (
        <div className="empty-state">
          <Leaf size={40} aria-hidden />
          <h2>Még nincs növényed</h2>
          <p className="muted">Add hozzá az elsőt, és szólunk, ha locsolni vagy tápozni kell.</p>
          <Link to="/plants/new" className="btn btn-primary">
            <Plus size={16} aria-hidden /> Új növény
          </Link>
        </div>
      ) : (
        <ul className="plant-grid">
          {plants.map((plant) => (
            <li key={plant.id} className={`plant-card accent-${mostUrgent(plant.watering, plant.feeding)}`} data-testid="plant-card">
              <Link to={`/plants/${plant.id}`} className="plant-card-head">
                {plant.latestPhotoId ? (
                  <AuthImage plantId={plant.id} photoId={plant.latestPhotoId} alt={plant.name} className="thumb" />
                ) : (
                  <div className="thumb thumb-empty">
                    <Leaf size={22} aria-hidden />
                  </div>
                )}
                <div className="plant-card-title">
                  <h2>{plant.name}</h2>
                  <p className="species">{plant.species ?? 'Faj nincs megadva'}</p>
                </div>
              </Link>
              <div className="task-list">
                {(['watering', 'feeding'] as const).map((type) => {
                  const task = plant[type]
                  return (
                    <div key={type} className="task-row" data-testid={`task-${type}`}>
                      <CareIcon type={type} />
                      <div className="task-text">
                        <span className="task-name">{careLabel[type]}</span>
                        <span className="task-when" title={formatDate(task.nextDueOn)}>
                          {relativeDue(task)}
                        </span>
                      </div>
                      <StatusBadge status={task.status} />
                      <button
                        type="button"
                        className="btn btn-ghost btn-sm"
                        onClick={() => recordCare(plant, type)}
                        disabled={busy !== null}
                      >
                        {busy === `${plant.id}:${type}` ? '…' : careDoneLabel[type]}
                      </button>
                    </div>
                  )
                })}
              </div>
            </li>
          ))}
        </ul>
      )}
    </>
  )
}
