import { useEffect, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { api, ApiError } from '../lib/api'
import { useCompleteSetup } from '../lib/hooks'
import { Alert, Button, Field } from '../components/ui'

type Mode = 'original-only' | 'auto-renditions'

const STEPS = ['Admin account', 'Site & storage', 'Transcoding', 'Review'] as const

export default function SetupWizard() {
  const defaults = useQuery({ queryKey: ['setup-defaults'], queryFn: api.setupDefaults, retry: false })
  const complete = useCompleteSetup()

  const [step, setStep] = useState(0)
  const [form, setForm] = useState({
    adminUserName: '',
    adminEmail: '',
    adminPassword: '',
    confirmPassword: '',
    siteName: '',
    mediaPath: '',
    transcodeMode: 'original-only' as Mode,
  })
  const [stepError, setStepError] = useState<string | null>(null)

  // Prefill site name / media path / mode from server defaults once they arrive.
  useEffect(() => {
    if (defaults.data) {
      setForm((f) => ({
        ...f,
        siteName: f.siteName || defaults.data.siteName,
        mediaPath: f.mediaPath || defaults.data.mediaPath,
        transcodeMode: (defaults.data.transcodeMode as Mode) || f.transcodeMode,
      }))
    }
  }, [defaults.data])

  const set = (patch: Partial<typeof form>) => setForm((f) => ({ ...f, ...patch }))

  function validateStep(): boolean {
    setStepError(null)
    if (step === 0) {
      if (form.adminUserName.trim().length < 2) return fail('Choose a username (at least 2 characters).')
      if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(form.adminEmail)) return fail('Enter a valid email address.')
      if (form.adminPassword.length < 8) return fail('Password must be at least 8 characters.')
      if (form.adminPassword !== form.confirmPassword) return fail('Passwords do not match.')
    }
    if (step === 1) {
      if (form.siteName.trim().length < 1) return fail('Give your site a name.')
      if (form.mediaPath.trim().length < 1) return fail('Set a media storage path.')
    }
    return true
  }

  function fail(msg: string) {
    setStepError(msg)
    return false
  }

  function next() {
    if (validateStep()) setStep((s) => Math.min(s + 1, STEPS.length - 1))
  }

  function submit() {
    complete.mutate({
      siteName: form.siteName.trim(),
      adminUserName: form.adminUserName.trim(),
      adminEmail: form.adminEmail.trim(),
      adminPassword: form.adminPassword,
      mediaPath: form.mediaPath.trim(),
      transcodeMode: form.transcodeMode,
    })
  }

  const serverError = complete.error instanceof ApiError ? complete.error.message : null

  return (
    <div className="mx-auto flex min-h-full max-w-xl flex-col justify-center px-4 py-10">
      <div className="mb-6 text-center">
        <h1 className="text-2xl font-semibold text-white">Welcome to Tubestead</h1>
        <p className="mt-1 text-sm text-gray-400">Let’s get your video library set up. This only happens once.</p>
      </div>

      <Stepper step={step} />

      <div className="rounded-2xl border border-white/10 bg-white/5 p-6 shadow-xl">
        {step === 0 && (
          <div className="space-y-4">
            <h2 className="text-lg font-medium text-white">Create the admin account</h2>
            <Field label="Username" value={form.adminUserName} autoFocus
              onChange={(e) => set({ adminUserName: e.target.value })} placeholder="chris" />
            <Field label="Email" type="email" value={form.adminEmail}
              onChange={(e) => set({ adminEmail: e.target.value })} placeholder="you@example.com" />
            <Field label="Password" type="password" value={form.adminPassword}
              onChange={(e) => set({ adminPassword: e.target.value })}
              hint="At least 8 characters, including a number." />
            <Field label="Confirm password" type="password" value={form.confirmPassword}
              onChange={(e) => set({ confirmPassword: e.target.value })} />
          </div>
        )}

        {step === 1 && (
          <div className="space-y-4">
            <h2 className="text-lg font-medium text-white">Site & storage</h2>
            <Field label="Site name" value={form.siteName}
              onChange={(e) => set({ siteName: e.target.value })} placeholder="Tubestead" />
            <Field label="Media storage path" value={form.mediaPath}
              onChange={(e) => set({ mediaPath: e.target.value })}
              hint="Where uploaded videos are stored. In Docker this is your mounted NAS share (e.g. /media)." />
          </div>
        )}

        {step === 2 && (
          <div className="space-y-4">
            <h2 className="text-lg font-medium text-white">Transcoding</h2>
            <p className="text-sm text-gray-400">
              How should uploads be processed? You can change this later in Admin settings.
            </p>
            <ModeCard
              active={form.transcodeMode === 'original-only'}
              onClick={() => set({ transcodeMode: 'original-only' })}
              title="Original only (recommended)"
              body="Videos are made watchable immediately with just a thumbnail. Best for low-power NAS hardware. You can transcode individual videos later."
            />
            <ModeCard
              active={form.transcodeMode === 'auto-renditions'}
              onClick={() => set({ transcodeMode: 'auto-renditions' })}
              title="Auto-generate quality renditions"
              body="Every upload is transcoded to multiple resolutions (1080p/720p/480p) for adaptive streaming. Best playback, but can heavily load a weak CPU."
            />
          </div>
        )}

        {step === 3 && (
          <div className="space-y-3">
            <h2 className="text-lg font-medium text-white">Review</h2>
            <Review label="Admin" value={`${form.adminUserName} · ${form.adminEmail}`} />
            <Review label="Site name" value={form.siteName} />
            <Review label="Media path" value={form.mediaPath} />
            <Review label="Transcoding" value={form.transcodeMode === 'original-only' ? 'Original only' : 'Auto renditions'} />
          </div>
        )}

        {(stepError || serverError) && <div className="mt-4"><Alert>{stepError ?? serverError}</Alert></div>}

        <div className="mt-6 flex items-center justify-between">
          <Button variant="ghost" onClick={() => setStep((s) => Math.max(s - 1, 0))} disabled={step === 0 || complete.isPending}>
            Back
          </Button>
          {step < STEPS.length - 1 ? (
            <Button onClick={next} disabled={defaults.isLoading}>Continue</Button>
          ) : (
            <Button onClick={submit} disabled={complete.isPending}>
              {complete.isPending ? 'Setting up…' : 'Finish setup'}
            </Button>
          )}
        </div>
      </div>
    </div>
  )
}

function Stepper({ step }: { step: number }) {
  return (
    <ol className="mb-4 flex items-center justify-center gap-2 text-xs">
      {STEPS.map((label, i) => (
        <li key={label} className="flex items-center gap-2">
          <span className={`flex h-6 w-6 items-center justify-center rounded-full border ${
            i <= step ? 'border-emerald-400 bg-emerald-400 text-emerald-950' : 'border-white/20 text-gray-400'
          }`}>{i + 1}</span>
          <span className={i <= step ? 'text-gray-200' : 'text-gray-500'}>{label}</span>
          {i < STEPS.length - 1 && <span className="mx-1 text-gray-600">›</span>}
        </li>
      ))}
    </ol>
  )
}

function ModeCard({ active, onClick, title, body }: { active: boolean; onClick: () => void; title: string; body: string }) {
  return (
    <button type="button" onClick={onClick}
      className={`w-full rounded-xl border p-4 text-left transition-colors ${
        active ? 'border-emerald-400/70 bg-emerald-400/10' : 'border-white/10 bg-white/5 hover:border-white/25'
      }`}>
      <div className="flex items-center gap-2">
        <span className={`h-4 w-4 rounded-full border ${active ? 'border-emerald-400 bg-emerald-400' : 'border-white/30'}`} />
        <span className="font-medium text-white">{title}</span>
      </div>
      <p className="mt-1 pl-6 text-sm text-gray-400">{body}</p>
    </button>
  )
}

function Review({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex justify-between gap-4 border-b border-white/5 py-2 text-sm">
      <span className="text-gray-400">{label}</span>
      <span className="text-right font-medium text-gray-100">{value}</span>
    </div>
  )
}
