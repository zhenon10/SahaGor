import { expect, test } from "@playwright/test";
import { SeedSistemYoneticisi, seedGorevOlustur } from "./destek/apiIstemcisi";

/**
 * SG-421: dogrudan backend API'sinde olusturulan bir gorevin, web panelinin hem liste hem
 * de detay ekraninda dogru sekilde gorunduguni dogrular. Bu, sadece web UI'sinin degil,
 * UI -> gercek API -> gercek PostgreSQL zincirinin butunun uctan uca calistigini kanitlar.
 */
test.describe("Görev listesi ve detayı", () => {
  test.beforeEach(async ({ page }) => {
    await page.goto("/giris");
    await page.locator("#kullaniciAdi").fill(SeedSistemYoneticisi.kullaniciAdi);
    await page.locator("#sifre").fill(SeedSistemYoneticisi.sifre);
    await page.getByRole("button", { name: "Giriş Yap" }).click();
    await expect(page).toHaveURL("/");
  });

  test("API üzerinden oluşturulan bir görev, görev listesinde ve detay sayfasında görünür", async ({
    page,
    request,
  }) => {
    const { baslik } = await seedGorevOlustur(request);

    await page.goto("/gorevler");
    const gorevLinki = page.getByRole("link", { name: baslik });
    await expect(gorevLinki).toBeVisible();

    await gorevLinki.click();
    await expect(page).toHaveURL(/\/gorevler\/[0-9a-fA-F-]+/);
    await expect(page.getByRole("heading", { name: baslik })).toBeVisible();
    await expect(page.getByText("Kırık Kaldırım")).toBeVisible();
  });
});
