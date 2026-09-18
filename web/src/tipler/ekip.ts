/** Backend'deki SahaGor.Application.Ekipler.Dtolar.EkipYaniti ile bire bir eslesir. */
export interface EkipYaniti {
  id: string;
  birimId: string;
  birimAdi: string;
  ad: string;
  aktifMi: boolean;
  guncelKonumEnlem: number | null;
  guncelKonumBoylam: number | null;
  konumGuncellenmeZamaniUtc: string | null;
  uyeler: { personelId: string; adSoyad: string; aktifMi: boolean; musaitMi: boolean }[];
  uzmanlikAlanlari: { gorevKategorisiId: string; ad: string }[];
}

/** SignalR uzerinden gelen EkipKonumBildirimi ile bire bir eslesir. */
export interface EkipKonumBildirimi {
  ekipId: string;
  ekipAdi: string;
  enlem: number;
  boylam: number;
  zamanUtc: string;
}
