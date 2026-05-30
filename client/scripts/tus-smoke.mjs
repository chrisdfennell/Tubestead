// Standalone smoke test: performs a real resumable (tus) upload against a running
// Tubestead API. Usage: node tus-smoke.mjs <baseUrl> <cookie>
import * as tus from 'tus-js-client'

const [, , baseUrl, cookie] = process.argv
const data = Buffer.alloc(2 * 1024 * 1024, 7) // 2 MB of fake "video" bytes

const upload = new tus.Upload(data, {
  endpoint: `${baseUrl}/api/uploads`,
  chunkSize: 1 * 1024 * 1024,
  uploadSize: data.length,
  headers: { Cookie: cookie },
  metadata: { filename: 'smoke-test.mp4', filetype: 'video/mp4', title: 'Smoke Test Clip' },
  onError: (err) => { console.error('UPLOAD_ERROR', err.message); process.exit(1) },
  onSuccess: () => { console.log('UPLOAD_OK', upload.url); process.exit(0) },
})

upload.start()
