import type { VideoStatus } from '../lib/api'

const STYLES: Record<VideoStatus, string> = {
  Ready: 'bg-emerald-500/15 text-emerald-300',
  Processing: 'bg-amber-500/15 text-amber-300',
  Uploading: 'bg-sky-500/15 text-sky-300',
  Failed: 'bg-red-500/15 text-red-300',
}

export default function StatusBadge({ status }: { status: VideoStatus }) {
  return (
    <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium ${STYLES[status]}`}>
      {(status === 'Processing' || status === 'Uploading') && (
        <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-current" />
      )}
      {status}
    </span>
  )
}
