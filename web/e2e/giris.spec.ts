import { expect, test } from "@playwright/test";
import { SeedSistemYoneticisi } from "./destek/apiIstemcisi";

/**
 * SG-421: gercek backend'e karsi giris akisinin uctan uca dogrulamasi. Kullanilan hesap,
 * migration seed verisindeki sabit "sistem.yoneticisi" hesabidir (bkz. BaslangicVerisiSabitleri).
 */
test.describe("Giriş akışı", () => {
  test("doğru kullanıcı adı/şifre ile giriş yapınca komuta paneline yönlendirir", async ({ page }) => {
    await page.goto("/giris");

    await page.locator("#kullaniciAdi").fill(SeedSistemYoneticisi.kullaniciAdi);
    await page.locator("#sifre").fill(SeedSistemYoneticisi.sifre);
    await page.getByRole("button", { name: "Giriş Yap" }).click();

    await expect(page).toHaveURL("/");
    await expect(page.getByRole("heading", { name: "Genel Bakış" })).toBeVisible();
  });

  test("yanlış şifreyle giriş denemesi hata mesajı gösterir ve giriş sayfasında kalır", async ({ page }) => {
    await page.goto("/giris");

    await page.locator("#kullaniciAdi").fill(SeedSistemYoneticisi.kullaniciAdi);
    await page.locator("#sifre").fill("kesinlikle-yanlis-bir-sifre");
    await page.getByRole("button", { name: "Giriş Yap" }).click();

    await expect(page.getByText(/kullanıcı adı veya şifre|hatalı|geçersiz/i)).toBeVisible();
    await expect(page).toHaveURL(/\/giris/);
  });
});
