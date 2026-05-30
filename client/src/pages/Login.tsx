import { useState } from 'react'
import { ApiError } from '../lib/api'
import { useLogin } from '../lib/hooks'
import { Alert, Button, Field } from '../components/ui'

export default function Login({ siteName }: { siteName: string }) {
  const login = useLogin()
  const [userNameOrEmail, setUser] = useState('')
  const [password, setPassword] = useState('')
  const [rememberMe, setRemember] = useState(true)

  const error = login.error instanceof ApiError ? login.error.message : null

  function submit(e: React.FormEvent) {
    e.preventDefault()
    login.mutate({ userNameOrEmail, password, rememberMe })
  }

  return (
    <div className="mx-auto flex min-h-full max-w-sm flex-col justify-center px-4 py-10">
      <div className="mb-6 text-center">
        <h1 className="text-2xl font-semibold text-white">{siteName}</h1>
        <p className="mt-1 text-sm text-gray-400">Sign in to continue</p>
      </div>
      <form onSubmit={submit} className="space-y-4 rounded-2xl border border-white/10 bg-white/5 p-6 shadow-xl">
        <Field label="Username or email" value={userNameOrEmail} autoFocus
          onChange={(e) => setUser(e.target.value)} />
        <Field label="Password" type="password" value={password}
          onChange={(e) => setPassword(e.target.value)} />
        <label className="flex items-center gap-2 text-sm text-gray-300">
          <input type="checkbox" checked={rememberMe} onChange={(e) => setRemember(e.target.checked)} />
          Remember me
        </label>
        {error && <Alert>{error}</Alert>}
        <Button type="submit" className="w-full" disabled={login.isPending}>
          {login.isPending ? 'Signing in…' : 'Sign in'}
        </Button>
      </form>
    </div>
  )
}
