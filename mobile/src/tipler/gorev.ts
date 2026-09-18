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

/** GET /api/gorevler icin destekelenen filtre parametreleri (bkz. backend GorevTalebiFiltre). */
export interface GorevFiltresi {
  atananEkipId?: string;
  durum?: string;
  sayfa?: number;
  sayfaBoyutu?: number;
}
