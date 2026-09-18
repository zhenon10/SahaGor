/** Backend'in ASP.NET Core ProblemDetails formatinda dondurdugu hata govdesi. */
export interface ProblemDetaylari {
  title?: string;
  detail?: string;
  status?: number;
  izlemeId?: string;
  [anahtar: string]: unknown;
}

/** Backend cagrilarindan donen basarisiz yanitlari temsil eden hata sinifi. */
export class ApiHatasi extends Error {
  readonly durumKodu: number;
  readonly detaylar?: ProblemDetaylari;

  constructor(mesaj: string, durumKodu: number, detaylar?: ProblemDetaylari) {
    super(mesaj);
    this.name = "ApiHatasi";
    this.durumKodu = durumKodu;
    this.detaylar = detaylar;
  }
}
