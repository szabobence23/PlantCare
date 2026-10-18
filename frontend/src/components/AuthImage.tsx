import { useEffect, useState } from 'react'
import { api } from '../api/api'

interface Props {
  plantId: string
  photoId: string
  alt: string
  className?: string
  onClick?: () => void
}

/** Photos need the bearer token, so they are fetched as blobs and shown via object URLs. */
export function AuthImage({ plantId, photoId, alt, className, onClick }: Props) {
  const [url, setUrl] = useState<string | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    let objectUrl: string | null = null
    let cancelled = false
    api
      .photoBlob(plantId, photoId)
      .then((blob) => {
        if (cancelled) return
        objectUrl = URL.createObjectURL(blob)
        setUrl(objectUrl)
      })
      .catch(() => !cancelled && setFailed(true))
    return () => {
      cancelled = true
      if (objectUrl) URL.revokeObjectURL(objectUrl)
    }
  }, [plantId, photoId])

  if (failed) return <div className={`img-placeholder ${className ?? ''}`}>Nem tölthető be</div>
  if (!url) return <div className={`img-placeholder shimmer ${className ?? ''}`} aria-busy />
  return <img src={url} alt={alt} className={className} onClick={onClick} />
}
