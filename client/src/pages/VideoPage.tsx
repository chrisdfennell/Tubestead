import { useQuery } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router-dom'
import type { CurrentUser } from '../lib/api'
import { api } from '../lib/api'
import { useDeleteVideo } from '../lib/hooks'
import { formatBytes, formatDuration } from '../lib/format'
import { Button, Spinner } from '../components/ui'
import StatusBadge from '../components/StatusBadge'

export default function VideoPage({ user }: { user: CurrentUser }) {
  const { id = '' } = useParams()
  const navigate = useNavigate()
  const isAdmin = user.roles.includes('Admin')
  const del = useDeleteVideo()

  const { data: video, isLoading, isError } = useQuery({
    queryKey: ['video', id],
    queryFn: () => api.videos.get(id),
    retry: false,
  })

  if (isLoading) return <div className="py-20"><Spinner label="Loading…" /></div>
  if (isError || !video) {
    return (
      <div className="py-20 text-center text-gray-400">
        <p className="text-white">Video not found.</p>
        <Link to="/" className="mt-2 inline-block text-sm text-emerald-400">← Back to library</Link>
      </div>
    )
  }

  async function remove() {
    if (!confirm('Delete this video and all its files? This cannot be undone.')) return
    await del.mutateAsync(id)
    navigate('/')
  }

  return (
    <div className="mx-auto max-w-4xl space-y-5">
      <Link to="/" className="text-sm text-gray-400 hover:text-gray-200">← Library</Link>

      {/* Player placeholder until M3 wires hls.js + vidstack. */}
      <div className="flex aspect-video w-full items-center justify-center rounded-2xl border border-white/10 bg-black/40 text-center">
        {video.status === 'Ready' ? (
          <div className="text-gray-400">
            <div className="text-4xl text-white/20">▶</div>
            <p className="mt-2 text-sm">In-browser playback with seeking arrives in M3.</p>
          </div>
        ) : (
          <div className="text-gray-400">
            <StatusBadge status={video.status} />
            <p className="mt-2 text-sm">{video.statusMessage ?? 'This video is being processed.'}</p>
          </div>
        )}
      </div>

      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-white">{video.title}</h1>
          <div className="mt-1 flex items-center gap-3 text-sm text-gray-400">
            <StatusBadge status={video.status} />
            {formatDuration(video.durationSeconds) && <span>{formatDuration(video.durationSeconds)}</span>}
            <span>{new Date(video.createdUtc).toLocaleDateString()}</span>
          </div>
        </div>
        {isAdmin && (
          <Button variant="ghost" onClick={remove} disabled={del.isPending}>
            {del.isPending ? 'Deleting…' : 'Delete'}
          </Button>
        )}
      </div>

      {video.description && <p className="whitespace-pre-wrap text-sm text-gray-300">{video.description}</p>}

      <div className="rounded-2xl border border-white/10 bg-white/5 p-4">
        <h2 className="mb-2 text-sm font-medium text-gray-200">Files</h2>
        <p className="mb-3 text-xs text-gray-500">Download buttons arrive in M4. For now this lists what’s stored.</p>
        <ul className="divide-y divide-white/5 text-sm">
          {video.renditions.map((r) => (
            <li key={r.id} className="flex items-center justify-between py-2">
              <span className="text-gray-200">
                {r.label}
                {r.isOriginal && <span className="ml-2 text-xs text-gray-500">({video.originalFileName})</span>}
              </span>
              <span className="text-gray-400">{r.format.toUpperCase()} · {formatBytes(r.sizeBytes)}</span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  )
}
