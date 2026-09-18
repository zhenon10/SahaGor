import type { APIRequestContext } from "@playwright/test";

/**
 * E2E testlerinin, tarayici UI'sini kullanmadan dogrudan backend API'ye karsi test verisi
 * (gorev vb.) olusturmak icin kullandigi yardimci. UI akislarini test etmek istedigimizden,
 * "once veriyi API ile hazirla, sonra tarayicida dogrula" deseni izlenir - boylece testler
 * hem UI'yi hem de UI-API entegrasyonunu (gercek backend'e karsi) dogrulamis olur.
 */
export const E2eApiTabanAdresi = process.env.E2E_API_BASE_URL ?? "http://localhost:5299";

/** Migration seed verisindeki sabit sistem yoneticisi hesabi (bkz. BaslangicVerisiSabitleri). */
export const SeedSistemYoneticisi = {
  kullaniciAdi: "sistem.yoneticisi",
  sifre: "DegistirilmeliSifre#2026",
};

/** Migration seed verisindeki sabit gorev kategorisi adi. */
export const SeedKategoriAdi = "Kırık Kaldırım";

export async function girisYapVeTokenAl(istekBaglami: APIRequestContext): Promise<string> {
  const yanit = await istekBaglami.post(`${E2eApiTabanAdresi}/api/auth/login`, {
    data: { kullaniciAdi: SeedSistemYoneticisi.kullaniciAdi, sifre: SeedSistemYoneticisi.sifre },
  });

  if (!yanit.ok()) {
    throw new Error(`Seed girisi basarisiz: ${yanit.status()} ${await yanit.text()}`);
  }

  const govde = (await yanit.json()) as { erisimTokeni: string };
  return govde.erisimTokeni;
}

interface OlusturulanGorev {
  id: string;
  baslik: string;
}

/** Kategori adini kategori Id'sine cevirir (seed verisindeki sabit GUID'e bagli kalmamak icin). */
async function kategoriIdBul(istekBaglami: APIRequestContext, erisimTokeni: string, kategoriAdi: string): Promise<string> {
  const yanit = await istekBaglami.get(`${E2eApiTabanAdresi}/api/gorev-kategorileri`, {
    headers: { Authorization: `Bearer ${erisimTokeni}` },
  });

  if (!yanit.ok()) {
    throw new Error(`Kategori listesi alinamadi: ${yanit.status()} ${await yanit.text()}`);
  }

  const kategoriler = (await yanit.json()) as Array<{ id: string; ad: string }>;
  const kategori = kategoriler.find((k) => k.ad === kategoriAdi);

  if (!kategori) {
    throw new Error(`'${kategoriAdi}' adinda bir kategori bulunamadi. Seed verisi degismis olabilir.`);
  }

  return kategori.id;
}

/** E2E dogrulamasi icin dogrudan backend API'de rastgele (benzersiz basliga sahip) bir gorev olusturur. */
export async function seedGorevOlustur(istekBaglami: APIRequestContext): Promise<OlusturulanGorev> {
  const erisimTokeni = await girisYapVeTokenAl(istekBaglami);
  const kategoriId = await kategoriIdBul(istekBaglami, erisimTokeni, SeedKategoriAdi);

  const baslik = `E2E test gorevi ${Date.now()}`;
  const yanit = await istekBaglami.post(`${E2eApiTabanAdresi}/api/gorevler`, {
    headers: { Authorization: `Bearer ${erisimTokeni}` },
    data: {
      baslik,
      aciklama: "Playwright E2E testi tarafindan olusturuldu.",
      kategoriId,
      enlem: 41.015137,
      boylam: 28.97953,
      oncelik: "Normal",
      kaynak: "OperatorGirisi",
      bildirenTelefonNumarasi: null,
      disKaynakReferansNo: null,
    },
  });

  if (!yanit.ok()) {
    throw new Error(`Gorev olusturulamadi: ${yanit.status()} ${await yanit.text()}`);
  }

  const govde = (await yanit.json()) as { id: string };
  return { id: govde.id, baslik };
}
