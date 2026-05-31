import { defineConfig, loadEnv } from "vite"
import react from "@vitejs/plugin-react"

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd())
  // Standalone kiosk API (see api/KjMatzosLookup.Api/Properties/launchSettings.json)
  const apiTarget = env.VITE_API_PROXY_TARGET || "http://localhost:5190"

  return {
    plugins: [react()],
    server: {
      host: "0.0.0.0",
      port: 5180,
      proxy: {
        "/api": {
          target: apiTarget,
          changeOrigin: true,
          secure: false,
        },
      },
    },
  }
})
