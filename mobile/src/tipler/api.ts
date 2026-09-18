/**
 * Backend'in ASP.NET Core ProblemDetails formatinda dondurdugu hata govdesi. Index
 * imzasi, GorevDurumCakismasiException gibi ozel istisnalarin ekledigi ekstra alanlari
 * ("gorevId", "mevcutDurum" vb.) da okuyabilmek icindir.
 */
export interface ProblemDetaylari {
  title?: string;
  detail?: string;
  status?: number;
  izlemeId?: string;
  [anahtar: string]: unknown;
}

/** API'den donen basarisiz yanitlari temsil eden, kullanicilara gosterilebilir mesaj tasiyan hata sinifi. */
export class ApiHatasi extends Error {
  readonly durumKodu: number;
  readonly detaylar?: ProblemDetaylari;

  constructor(mesaj: string, durumKodu: number, detaylar?: ProblemDetaylari) {
    super(mesaj);
    this.name = "ApiHatasi";
    this.durumKodu = durumKodu;
    this.detaylar = detaylar;
  }
}

/** Yenileme tokeni de gecersiz oldugunda (oturum tamamen sona erdiginde) firlatilir. */
export class OturumSuresiDolduHatasi extends Error {
  constructor() {
    super("Oturumunuzun suresi doldu. Lutfen tekrar giris yapin.");
    this.name = "OturumSuresiDolduHatasi";
  }
}
