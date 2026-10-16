import { defineConfig } from "@playwright/test"
export default defineConfig({
  testDir: "./monitor-tests",
  timeout: 45000,
  use: { baseURL: process.env.MONITOR_WEB_URL ?? "http://127.0.0.1:5174", channel: "msedge", headless: true },
  webServer: process.env.MONITOR_WEB_URL ? undefined : {
    command: "node node_modules/vite/bin/vite.js --host 127.0.0.1 --port 5174 --strictPort",
    url: "http://127.0.0.1:5174",
    env: { VITE_TELEMETRY_ENABLED: "true", VITE_API_BASE_URL: "" },
    reuseExistingServer: false,
  },
})
