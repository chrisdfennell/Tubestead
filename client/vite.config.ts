import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'node:path'

// Dev: Vite serves the SPA and proxies /api to the ASP.NET Core dev server,
// so the browser sees a single origin (no CORS).
// Prod: `npm run build` emits straight into the API's wwwroot, which the API
// serves itself (single container, single origin).
const API_TARGET = process.env.TUBESTEAD_API_TARGET ?? 'http://localhost:5099'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: API_TARGET, changeOrigin: true },
    },
  },
  build: {
    outDir: path.resolve(__dirname, '../src/Tubestead.Api/wwwroot'),
    emptyOutDir: true,
  },
})
