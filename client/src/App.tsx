import { Navigate, Route, Routes } from 'react-router-dom'
import { useAppStatus } from './lib/hooks'
import { Spinner } from './components/ui'
import Layout from './components/Layout'
import SetupWizard from './pages/SetupWizard'
import Login from './pages/Login'
import Home from './pages/Home'
import VideoPage from './pages/VideoPage'

export default function App() {
  const { data: status, isLoading, isError } = useAppStatus()

  if (isLoading) return <Spinner label="Loading…" />

  if (isError || !status) {
    return (
      <div className="flex h-full items-center justify-center text-center text-gray-400">
        <div>
          <p className="text-lg text-white">Can’t reach the server</p>
          <p className="mt-1 text-sm">Make sure the Tubestead API is running.</p>
        </div>
      </div>
    )
  }

  // 1) Brand-new instance → force the setup wizard for any route.
  if (!status.setupCompleted) {
    return (
      <Routes>
        <Route path="*" element={<SetupWizard />} />
      </Routes>
    )
  }

  // 2) Configured but signed out → login.
  if (!status.user) {
    return (
      <Routes>
        <Route path="*" element={<Login siteName={status.siteName} />} />
      </Routes>
    )
  }

  // 3) Authenticated app shell.
  return (
    <Layout siteName={status.siteName} user={status.user}>
      <Routes>
        <Route path="/" element={<Home user={status.user} />} />
        <Route path="/videos/:id" element={<VideoPage user={status.user} />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </Layout>
  )
}
