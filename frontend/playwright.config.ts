import { defineConfig } from '@playwright/test'

/**
 * End-to-end test of the main scenario against the real backend and PostgreSQL.
 * Prerequisite: `docker compose up -d` in the repository root. Uses the locally installed
 * Google Chrome, so no browser download is needed.
 */
export default defineConfig({
  testDir: './e2e',
  timeout: 60_000,
  fullyParallel: false,
  reporter: [['list']],
  use: {
    baseURL: 'http://localhost:5173',
    channel: 'chrome',
    locale: 'hu-HU',
    timezoneId: 'Europe/Budapest',
    trace: 'retain-on-failure',
  },
  webServer: [
    {
      command: 'dotnet run --project ../backend/src/PlantCare.Api --launch-profile http',
      url: 'http://localhost:5080/api/plants', // 401 counts as "up"
      reuseExistingServer: true,
      timeout: 120_000,
    },
    {
      command: 'npm run dev',
      url: 'http://localhost:5173',
      reuseExistingServer: true,
      timeout: 60_000,
    },
  ],
})
