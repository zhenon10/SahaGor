/** Backend'deki SahaGor.Application.Birimler.Dtolar.BirimYaniti ile bire bir eslesir. */
export interface BirimYaniti {
  id: string;
  kurumId: string;
  kurumAdi: string;
  ad: string;
  aciklama: string | null;
  aktifMi: boolean;
  personelSayisi: number;
  ekipSayisi: number;
}
