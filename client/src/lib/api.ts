// Thin fetch wrapper. Same-origin in production; in dev Vite proxies /api to the
// API, so cookies flow either way with credentials: 'include'.

export interface CurrentUser {
  id: string
  userName: string
  email: string
  displayName: string | null
  roles: string[]
}

export interface AppStatus {
  setupCompleted: boolean
  siteName: string
  user: CurrentUser | null
}

export interface SetupDefaults {
  siteName: string
  mediaPath: string
  transcodeMode: string
}

export interface SetupPayload {
  siteName: string
  adminUserName: string
  adminEmail: string
  adminPassword: string
  mediaPath: string
  transcodeMode: string
}

export interface LoginPayload {
  userNameOrEmail: string
  password: string
  rememberMe?: boolean
}

export class ApiError extends Error {
  status: number
  constructor(message: string, status: number) {
    super(message)
    this.status = status
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`/api${path}`, {
    credentials: 'include',
    headers: { 'Content-Type': 'application/json' },
    ...init,
  })

  if (!res.ok) {
    throw new ApiError(await readError(res), res.status)
  }

  if (res.status === 204) return undefined as T
  const text = await res.text()
  return text ? (JSON.parse(text) as T) : (undefined as T)
}

// Turn a failed response into the most useful message we can: a plain { message },
// or an RFC7807 ProblemDetails validation payload, or a status fallback.
async function readError(res: Response): Promise<string> {
  try {
    const body = await res.json()
    if (typeof body?.message === 'string') return body.message
    if (body?.errors && typeof body.errors === 'object') {
      const msgs = Object.values(body.errors as Record<string, string[]>).flat()
      if (msgs.length) return msgs.join(' ')
    }
    if (typeof body?.title === 'string') return body.title
  } catch {
    /* not JSON */
  }
  return `Request failed (${res.status})`
}

export type VideoStatus = 'Uploading' | 'Processing' | 'Ready' | 'Failed'

export interface VideoListItem {
  id: string
  title: string
  status: VideoStatus
  statusMessage: string | null
  durationSeconds: number | null
  thumbnailUrl: string | null
  createdUtc: string
}

export interface Rendition {
  id: string
  label: string
  format: string
  width: number | null
  height: number | null
  sizeBytes: number | null
  isOriginal: boolean
}

export interface VideoDetail extends VideoListItem {
  description: string | null
  width: number | null
  height: number | null
  originalFileName: string | null
  renditions: Rendition[]
}

export const api = {
  status: () => request<AppStatus>('/auth/status'),
  videos: {
    list: (search?: string) =>
      request<VideoListItem[]>(`/videos${search ? `?search=${encodeURIComponent(search)}` : ''}`),
    get: (id: string) => request<VideoDetail>(`/videos/${id}`),
    remove: (id: string) => request<void>(`/videos/${id}`, { method: 'DELETE' }),
  },
  setupDefaults: () => request<SetupDefaults>('/setup/defaults'),
  completeSetup: (payload: SetupPayload) =>
    request<CurrentUser>('/setup', { method: 'POST', body: JSON.stringify(payload) }),
  login: (payload: LoginPayload) =>
    request<CurrentUser>('/auth/login', { method: 'POST', body: JSON.stringify(payload) }),
  logout: () => request<void>('/auth/logout', { method: 'POST' }),
}
