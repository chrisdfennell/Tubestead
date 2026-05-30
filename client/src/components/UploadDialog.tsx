import { useRef, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import * as tus from 'tus-js-client'
import { VIDEOS_KEY } from '../lib/hooks'
import { Alert, Button } from './ui'

type Phase = 'idle' | 'uploading' | 'done' | 'error'

// Resumable upload against the backend tus endpoint. Chunked so a dropped home
// connection can resume instead of restarting.
export default function UploadDialog({ onClose }: { onClose: () => void }) {
  const qc = useQueryClient()
  const [file, setFile] = useState<File | null>(null)
  const [title, setTitle] = useState('')
  const [phase, setPhase] = useState<Phase>('idle')
  const [progress, setProgress] = useState(0)
  const [error, setError] = useState<string | null>(null)
  const uploadRef = useRef<tus.Upload | null>(null)

  function pick(f: File | null) {
    setFile(f)
    if (f && !title) setTitle(f.name.replace(/\.[^.]+$/, ''))
  }

  function start() {
    if (!file) return
    setPhase('uploading')
    setError(null)
    setProgress(0)

    const upload = new tus.Upload(file, {
      endpoint: '/api/uploads',
      retryDelays: [0, 1000, 3000, 5000, 10000],
      chunkSize: 50 * 1024 * 1024, // 50 MB chunks → resumable, proxy-friendly
      removeFingerprintOnSuccess: true,
      metadata: {
        filename: file.name,
        filetype: file.type || 'application/octet-stream',
        title: title.trim() || file.name,
      },
      onError: (err) => {
        setPhase('error')
        setError(err.message || 'Upload failed.')
      },
      onProgress: (sent, total) => setProgress(Math.round((sent / total) * 100)),
      onSuccess: () => {
        setPhase('done')
        qc.invalidateQueries({ queryKey: VIDEOS_KEY })
      },
    })

    uploadRef.current = upload
    upload.start()
  }

  function cancel() {
    uploadRef.current?.abort()
    onClose()
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4" onClick={cancel}>
      <div
        className="w-full max-w-md rounded-2xl border border-white/10 bg-[#11161d] p-6 shadow-2xl"
        onClick={(e) => e.stopPropagation()}
      >
        <h2 className="text-lg font-medium text-white">Upload a video</h2>

        {phase === 'done' ? (
          <div className="mt-4 space-y-4">
            <div className="rounded-lg border border-emerald-500/40 bg-emerald-500/10 px-3 py-2 text-sm text-emerald-200">
              Upload complete — your video is now processing.
            </div>
            <div className="flex justify-end">
              <Button onClick={onClose}>Done</Button>
            </div>
          </div>
        ) : (
          <div className="mt-4 space-y-4">
            <label className="block">
              <span className="mb-1 block text-sm font-medium text-gray-200">Video file</span>
              <input
                type="file"
                accept="video/*"
                disabled={phase === 'uploading'}
                onChange={(e) => pick(e.target.files?.[0] ?? null)}
                className="block w-full text-sm text-gray-300 file:mr-3 file:rounded-lg file:border-0 file:bg-emerald-500 file:px-3 file:py-2 file:text-sm file:font-medium file:text-emerald-950 hover:file:bg-emerald-400"
              />
            </label>

            <label className="block">
              <span className="mb-1 block text-sm font-medium text-gray-200">Title</span>
              <input
                value={title}
                disabled={phase === 'uploading'}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="Give it a name"
                className="w-full rounded-lg border border-white/10 bg-white/5 px-3 py-2 text-sm text-gray-100 outline-none focus:border-emerald-400/60"
              />
            </label>

            {phase === 'uploading' && (
              <div>
                <div className="h-2 w-full overflow-hidden rounded-full bg-white/10">
                  <div className="h-full bg-emerald-400 transition-all" style={{ width: `${progress}%` }} />
                </div>
                <p className="mt-1 text-xs text-gray-400">{progress}% uploaded</p>
              </div>
            )}

            {error && <Alert>{error}</Alert>}

            <div className="flex justify-between">
              <Button variant="ghost" onClick={cancel}>Cancel</Button>
              {phase === 'error' ? (
                <Button onClick={start} disabled={!file}>Retry</Button>
              ) : (
                <Button onClick={start} disabled={!file || phase === 'uploading'}>
                  {phase === 'uploading' ? 'Uploading…' : 'Start upload'}
                </Button>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
