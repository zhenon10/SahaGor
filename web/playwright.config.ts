import { defineConfig, devices } from "@playwright/test";

/**
 * SG-421: web admin panelinin gercek bir tarayicida, gercek backend API'ye ve gercek
 * PostgreSQL/PostGIS'e karsi calisan uctan uca (E2E) testleri. Yerel gelistirmede web,
 * api ve veritabaninin ayrica ayaga kaldirilmasi gerekir; CI'da bunu .github/workflows/
 * e2e-ci.yml yapar (bkz. o dosyadaki adimlar).
 */
export default defineConfig({
  testDir: "./e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [["html", { open: "never" }], ["list"]] : "list",
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3000",
    trace: "on-first-retry",
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
