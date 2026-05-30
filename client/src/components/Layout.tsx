import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'
import type { CurrentUser } from '../lib/api'
import { useLogout } from '../lib/hooks'
import { Button } from './ui'

export default function Layout({
  siteName,
  user,
  children,
}: {
  siteName: string
  user: CurrentUser
  children: ReactNode
}) {
  const logout = useLogout()
  const isAdmin = user.roles.includes('Admin')

  return (
    <div className="min-h-full">
      <header className="sticky top-0 z-10 border-b border-white/10 bg-[#0b0f14]/80 backdrop-blur">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3">
          <Link to="/" className="flex items-center gap-2 text-lg font-semibold text-white">
            <span className="text-emerald-400">▶</span> {siteName}
          </Link>
          <div className="flex items-center gap-3 text-sm">
            {isAdmin && <span className="rounded-full bg-emerald-400/15 px-2 py-0.5 text-xs font-medium text-emerald-300">Admin</span>}
            <span className="text-gray-300">{user.displayName ?? user.userName}</span>
            <Button variant="ghost" onClick={() => logout.mutate()} disabled={logout.isPending}>
              Sign out
            </Button>
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-6xl px-4 py-6">{children}</main>
    </div>
  )
}
