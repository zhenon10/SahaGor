/** Backend'deki SahaGor.Domain.Enumlar.GorevFotografAsamasi ile bire bir eslesir. */
export type GorevFotografAsamasi = "Once" | "Sirasinda" | "Sonra";

/** Uygulama ici kamerayla cekilmis kanit fotografi (SG-210, SG-211, SG-212). */
export interface YerelFotograf {
  id: number;
  gorevId: string;
  dosyaYolu: string;
  asama: GorevFotografAsamasi;
  enlem: number;
  boylam: number;
  cekilmeZamaniUtc: string;
  yuklendiMi: boolean;
}
