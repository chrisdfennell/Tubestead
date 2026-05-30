import { useState } from 'react'
import { Link } from 'react-router-dom'
import type { CurrentUser, VideoListItem } from '../lib/api'
import { useVideos } from '../lib/hooks'
import { formatDuration } from '../lib/format'
import { Button, Spinner } from '../components/ui'
import StatusBadge from '../components/StatusBadge'
import UploadDialog from '../components/UploadDialog'

export default function Home({ user }: { user: CurrentUser }) {
  const isAdmin = user.roles.includes('Admin')
  const [search, setSearch] = useState('')
  const [uploadOpen, setUploadOpen] = useState(false)
  const { data: videos, isLoading } = useVideos(search || undefined)

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-xl font-semibold text-white">Library</h1>
        <div className="flex items-center gap-2">
          <input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Search titles…"
            className="w-48 rounded-lg border border-white/10 bg-white/5 px-3 py-1.5 text-sm text-gray-100 outline-none focus:border-emerald-400/60"
          />
          {isAdmin && <Button onClick={() => setUploadOpen(true)}>Upload</Button>}
        </div>
      </div>

      {isLoading ? (
        <div className="py-20"><Spinner label="Loading library…" /></div>
      ) : !videos || videos.length === 0 ? (
        <EmptyState isAdmin={isAdmin} onUpload={() => setUploadOpen(true)} hasSearch={!!search} />
      ) : (
        <div className="grid grid-cols-1 gap-5 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
          {videos.map((v) => <VideoCard key={v.id} video={v} />)}
        </div>
      )}

      {uploadOpen && <UploadDialog onClose={() => setUploadOpen(false)} />}
    </div>
  )
}

function VideoCard({ video }: { video: VideoListItem }) {
  const duration = formatDuration(video.durationSeconds)
  const playable = video.status === 'Ready'

  const inner = (
    <>
      <div className="relative aspect-video w-full overflow-hidden rounded-xl border border-white/10 bg-white/5">
        {video.thumbnailUrl ? (
          <img src={video.thumbnailUrl} alt="" className="h-full w-full object-cover" />
        ) : (
          <div className="flex h-full w-full items-center justify-center text-3xl text-white/15">▶</div>
        )}
        {duration && (
          <span className="absolute bottom-1.5 right-1.5 rounded bg-black/70 px-1.5 py-0.5 text-xs text-white">
            {duration}
          </span>
        )}
      </div>
      <div className="mt-2 flex items-start justify-between gap-2">
        <h3 className="line-clamp-2 text-sm font-medium text-gray-100">{video.title}</h3>
        <StatusBadge status={video.status} />
      </div>
      {video.status === 'Failed' && video.statusMessage && (
        <p className="mt-1 text-xs text-red-300/80">{video.statusMessage}</p>
      )}
    </>
  )

  return playable ? (
    <Link to={`/videos/${video.id}`} className="group block">{inner}</Link>
  ) : (
    <div className="cursor-default opacity-90">{inner}</div>
  )
}

function EmptyState({ isAdmin, onUpload, hasSearch }: { isAdmin: boolean; onUpload: () => void; hasSearch: boolean }) {
  return (
    <div className="rounded-2xl border border-white/10 bg-white/5 p-12 text-center">
      <p className="text-gray-300">{hasSearch ? 'No videos match your search.' : 'No videos yet.'}</p>
      {!hasSearch && (
        <p className="mt-1 text-sm text-gray-500">
          {isAdmin ? 'Upload your first video to get started.' : 'Once an admin uploads videos, they’ll appear here.'}
        </p>
      )}
      {isAdmin && !hasSearch && (
        <div className="mt-4 flex justify-center">
          <Button onClick={onUpload}>Upload a video</Button>
        </div>
      )}
    </div>
  )
}
