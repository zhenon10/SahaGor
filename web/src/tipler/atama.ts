/** Backend'deki SahaGor.Application.Atama.Dtolar.AtamaAdayiYaniti ile bire bir eslesir. */
export interface AtamaAdayi {
  ekipId: string;
  ekipAdi: string;
  toplamSkor: number;
  mesafeSkoru: number;
  musaitlikSkoru: number;
  yetkinlikSkoru: number;
  isYukuSkoru: number;
  mesafeMetre: number | null;
  aktifGorevSayisi: number;
}

/** Backend'deki AtamaOnerisiYaniti ile bire bir eslesir. */
export interface AtamaOnerisi {
  gorevId: string;
  onerilenEkip: AtamaAdayi | null;
  tumAdaylar: AtamaAdayi[];
}
