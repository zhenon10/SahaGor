/** Backend'deki SahaGor.Application.Personeller.Dtolar.PersonelYaniti ile bire bir eslesir. */
export interface PersonelYaniti {
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

export interface PersonelOlusturIstegi {
  birimId: string;
  adSoyad: string;
  kullaniciAdi: string;
  sifre: string;
  telefon: string;
  rol: string;
  eposta?: string | null;
}

export interface PersonelGuncelleIstegi {
  adSoyad: string;
  telefon: string;
  eposta?: string | null;
}

export const PERSONEL_ROLLERI = ["Operator", "SahaPersoneli", "Amir", "SistemYoneticisi"] as const;
