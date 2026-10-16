import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Dev: Vite serves the UI on :5173 and proxies /api to the .NET API on :5080.
// Build: output goes into the API's wwwroot so the API can serve the UI in production.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5080',
    },
  },
  build: {
    outDir: '../src/CarPrices.Api/wwwroot',
    emptyOutDir: true,
  },
})
