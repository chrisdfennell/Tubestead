import type { CurrentUser } from '../lib/api'

// M1 placeholder library. The grid, upload flow, and player land in M2–M4.
export default function Home({ user }: { user: CurrentUser }) {
  const isAdmin = user.roles.includes('Admin')
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-xl font-semibold text-white">Library</h1>
        {isAdmin && (
          <span className="rounded-lg border border-dashed border-white/20 px-3 py-1.5 text-xs text-gray-400">
            Upload — coming in M2
          </span>
        )}
      </div>

      <div className="rounded-2xl border border-white/10 bg-white/5 p-10 text-center">
        <p className="text-gray-300">No videos yet.</p>
        <p className="mt-1 text-sm text-gray-500">
          {isAdmin
            ? 'Resumable uploads arrive in the next milestone — then your videos will show up here.'
            : 'Once an admin uploads videos, they’ll appear here to watch and download.'}
        </p>
      </div>

      <p className="text-center text-xs text-gray-600">
        Signed in as {user.email} · roles: {user.roles.join(', ')}
      </p>
    </div>
  )
}
