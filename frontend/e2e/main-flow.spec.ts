import { expect, test, type Page } from '@playwright/test'

// A real 1×1 PNG, so the browser can actually display it.
const PNG = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==',
  'base64',
)

const isoDaysAgo = (days: number) => {
  const d = new Date()
  d.setDate(d.getDate() - days)
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`
}

/** Set SCREENSHOTS=1 to refresh the images used in the README. */
async function shot(page: Page, name: string) {
  if (!process.env.SCREENSHOTS) return
  await page.evaluate(() => window.scrollTo(0, 0)) // keeps the sticky header at the top of the capture
  await page.screenshot({ path: `../docs/screenshots/${name}.png`, fullPage: true })
}

test('main scenario: register → add plant → water → growth photo → delete → log in again', async ({ page }) => {
  const email = `e2e-${Date.now()}@example.com`
  const password = 'jelszo123'

  // AC1 – invalid, then valid registration
  await page.goto('/register')
  await page.getByLabel('E-mail cím').fill('nem-email')
  await page.getByLabel('Jelszó').fill('rovid')
  await page.getByRole('button', { name: 'Fiók létrehozása' }).click()
  await expect(page.getByText('Érvénytelen e-mail cím.')).toBeVisible()
  await expect(page.getByText('A jelszónak legalább 8 karakterből kell állnia.')).toBeVisible()
  await shot(page, '01-register-errors')

  await page.getByLabel('E-mail cím').fill(email)
  await page.getByLabel('Jelszó').fill(password)
  await page.getByRole('button', { name: 'Fiók létrehozása' }).click()
  await expect(page.getByRole('heading', { name: 'Növényeim' })).toBeVisible()
  await expect(page.getByText('Még nincs növényed')).toBeVisible()

  // AC5 – zero interval is rejected, nothing saved
  await page.getByRole('link', { name: 'Új növény' }).first().click()
  await page.getByLabel('Név').fill('Nappali fikusz')
  await page.getByLabel('Faj').fill('Ficus elastica')
  await page.getByLabel('Locsolás gyakorisága (nap)').fill('0')
  await page.getByRole('button', { name: 'Növény mentése' }).click()
  await expect(page.getByText('A locsolási gyakoriságnak pozitív egész számnak kell lennie.')).toBeVisible()
  await expect(page.getByText('A növény nem lett elmentve.', { exact: false })).toBeVisible()
  await shot(page, '02-invalid-interval')

  // AC2 + AC3 – valid plant, watered exactly one interval ago → due today
  await page.getByLabel('Locsolás gyakorisága (nap)').fill('7')
  await page.getByLabel('Legutóbbi locsolás').fill(isoDaysAgo(7))
  await page.getByLabel('Tápozás gyakorisága (nap)').fill('30')
  await page.getByLabel('Legutóbbi tápozás').fill(isoDaysAgo(29))
  await page.getByRole('button', { name: 'Növény mentése' }).click()

  await expect(page.getByRole('heading', { name: 'Nappali fikusz' })).toBeVisible()
  const watering = page.getByTestId('care-watering')
  await expect(watering.getByText('Esedékes')).toBeVisible()
  await expect(page.getByTestId('care-feeding').getByText('Közeledik')).toBeVisible()

  // AC4 – "Meglocsoltam" moves the next due date and shows up in the history
  await watering.getByRole('button', { name: 'Meglocsoltam' }).click()
  await expect(watering.getByText('Rendben')).toBeVisible()
  await expect(watering.getByText('7 nap múlva')).toBeVisible()
  await expect(page.getByTestId('history')).toContainText('Locsolás')

  // AC7 – growth photo is stored and shown in the gallery
  await page.locator('#growth-photo').setInputFiles({ name: 'fikusz.png', mimeType: 'image/png', buffer: PNG })
  await expect(page.getByTestId('gallery').locator('img')).toHaveCount(1)
  await shot(page, '03-plant-detail')

  // The list shows the plant with its schedule
  await page.getByRole('link', { name: 'Növényeim' }).click()
  await expect(page.getByTestId('plant-card')).toHaveCount(1)
  await expect(page.getByTestId('task-watering')).toContainText('Rendben')
  await shot(page, '04-plant-list')

  // AC8 – delete with confirmation
  await page.getByTestId('plant-card').getByRole('link').click()
  await page.getByRole('button', { name: 'Törlés' }).click()
  await page.getByRole('button', { name: 'Végleges törlés' }).click()
  await expect(page.getByText('Még nincs növényed')).toBeVisible()

  // AC1 – log out, wrong password, then log in again
  await page.getByRole('button', { name: 'Kijelentkezés' }).click()
  await page.getByLabel('E-mail cím').fill(email)
  await page.getByLabel('Jelszó').fill('rosszjelszo')
  await page.getByRole('button', { name: 'Bejelentkezés' }).click()
  await expect(page.getByRole('alert')).toHaveText('Hibás e-mail cím vagy jelszó.')
  await page.getByLabel('Jelszó').fill(password)
  await page.getByRole('button', { name: 'Bejelentkezés' }).click()
  await expect(page.getByText('Még nincs növényed')).toBeVisible()
})
