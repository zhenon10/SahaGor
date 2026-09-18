/** Backend'deki SahaGor.Application.Personeller.Dtolar.PersonelYaniti ile bire bir eslesir. */
export interface PersonelBilgisi {
  id: string;
  birimId: string;
  birimAdi: string;
  ekipId: string | null;
  ekipAdi: string | null;
  adSoyad: string;
  kullaniciAdi: string;
  telefon: string;
  eposta: string | null;
  rol: string;
  aktifMi: boolean;
  musaitMi: boolean;
  kilitliMi: boolean;
}
