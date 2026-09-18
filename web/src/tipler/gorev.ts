/** Backend'deki SahaGor.Application.Gorevler.Dtolar.GorevTalebiOzetYaniti ile bire bir eslesir. */
export interface GorevOzeti {
  id: string;
  baslik: string;
  kategoriAdi: string;
  durum: string;
  oncelik: string;
  enlem: number;
  boylam: number;
  bolgeAdi: string | null;
  atananEkipAdi: string | null;
  olusturulmaZamaniUtc: string;
  slaHedefZamaniUtc: string;
  slaIhlalEdildiMi: boolean;
}

export interface SayfalanmisSonuc<T> {
  kayitlar: T[];
  toplamKayitSayisi: number;
  sayfa: number;
  sayfaBoyutu: number;
}

/** Backend'deki GorevTalebiDetayYaniti ile bire bir eslesir. */
export interface GorevDetayi {
  id: string;
  baslik: string;
  aciklama: string | null;
  kategoriAdi: string;
  durum: string;
  oncelik: string;
  kaynak: string;
  enlem: number;
  boylam: number;
  bolgeAdi: string | null;
  atananEkipAdi: string | null;
  atananPersonelAdi: string | null;
  bildirenTelefonNumarasi: string | null;
  olusturulmaZamaniUtc: string;
  slaHedefZamaniUtc: string;
  slaIhlalEdildiMi: boolean;
  durumGecmisi: { oncekiDurum: string; yeniDurum: string; degisiklikZamaniUtc: string; not: string | null }[];
  fotograflar: { id: string; dosyaYolu: string; asama: string; cekilmeZamaniUtc: string }[];
}

/** Backend'deki GorevIstatistikleriYaniti ile bire bir eslesir (SG-321). */
export interface GorevIstatistikleri {
  acikGorevSayisi: number;
  slaIhlalSayisi: number;
  slaIhlalOrani: number;
  ortalamaCozumSuresiDakika: number | null;
  pencereIcindeOlusturulanGorevSayisi: number;
  pencereIcindeTamamlananGorevSayisi: number;
  gunSayisi: number;
}

/** Kapali sayilan (haritada gosterilmeyen) gorev durumlari. */
export const KAPALI_GOREV_DURUMLARI = new Set(["Tamamlandi", "Dogrulandi", "Iptal"]);

/** SignalR uzerinden gelen GorevBildirimi ile bire bir eslesir. */
export interface GorevBildirimi {
  gorevId: string;
  baslik: string;
  durum: string;
  zamanUtc: string;
}
