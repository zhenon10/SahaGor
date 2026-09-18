/** Su an desteklenen offline durum degisikligi islemleri (SG-202). Ilerideki adimlarda "TAMAMLA" eklenecektir. */
export type GorevIslemTuru = "YOLA_CIK" | "BASLA";

export interface SenkronKuyrukOgesi {
  id: number;
  gorevId: string;
  islemTuru: GorevIslemTuru;
  olusturulmaZamaniUtc: string;
  denemeSayisi: number;
  sonHataMesaji: string | null;
}
