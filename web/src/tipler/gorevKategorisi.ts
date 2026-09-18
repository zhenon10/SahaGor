/** GET /api/gorev-kategorileri yanitindaki (anonim projeksiyon) alanlarla eslesir. */
export interface GorevKategorisiYaniti {
  id: string;
  ad: string;
  aciklama: string | null;
  slaYanitSuresiDakika: number;
  slaCozumSuresiDakika: number;
}
