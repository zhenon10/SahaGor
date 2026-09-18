import { ApiIstemcisi } from "./apiIstemcisi";
import type { GorevFiltresi, GorevOzeti, SayfalanmisSonuc } from "../tipler/gorev";
import type { YerelFotograf } from "../tipler/fotograf";

function sorguDizesiOlustur(filtre: GorevFiltresi): string {
  const parametreler = new URLSearchParams();

  if (filtre.atananEkipId) parametreler.set("atananEkipId", filtre.atananEkipId);
  if (filtre.durum) parametreler.set("durum", filtre.durum);
  if (filtre.sayfa) parametreler.set("sayfa", String(filtre.sayfa));
  if (filtre.sayfaBoyutu) parametreler.set("sayfaBoyutu", String(filtre.sayfaBoyutu));

  const dize = parametreler.toString();
  return dize ? `?${dize}` : "";
}

/** Durum degistiren uc noktalarin donusunden sadece bu iki alan kullanilir. */
export interface GorevIslemSonucu {
  id: string;
  durum: string;
}

/** Backend'deki GorevFotografiYaniti ile bire bir eslesir. */
export interface GorevFotografiYaniti {
  id: string;
  dosyaYolu: string;
  asama: string;
  cekilmeZamaniUtc: string;
}

export const GorevlerApi = {
  listele: (filtre: GorevFiltresi) =>
    ApiIstemcisi.get<SayfalanmisSonuc<GorevOzeti>>(`/api/gorevler${sorguDizesiOlustur(filtre)}`),

  yolaCik: (gorevId: string) => ApiIstemcisi.post<GorevIslemSonucu>(`/api/gorevler/${gorevId}/yola-cik`, {}),

  basla: (gorevId: string) => ApiIstemcisi.post<GorevIslemSonucu>(`/api/gorevler/${gorevId}/basla`, {}),

  /** Kanit fotografini multipart/form-data olarak yukler (SG-212). */
  fotografYukle: (fotograf: YerelFotograf) => {
    const formVerisi = new FormData();

    // React Native'in fetch/FormData implementasyonu, { uri, name, type } seklindeki
    // nesneleri dosya parcasi olarak taniyip cok parcali (multipart) govdeye ekler;
    // bu web DOM tiplerinden farkli oldugu icin Blob'a cast edilir.
    formVerisi.append("dosya", {
      uri: fotograf.dosyaYolu,
      name: `${fotograf.gorevId}-${fotograf.id}.jpg`,
      type: "image/jpeg",
    } as unknown as Blob);
    formVerisi.append("asama", fotograf.asama);
    formVerisi.append("enlem", String(fotograf.enlem));
    formVerisi.append("boylam", String(fotograf.boylam));
    formVerisi.append("cekilmeZamaniUtc", fotograf.cekilmeZamaniUtc);

    return ApiIstemcisi.postForm<GorevFotografiYaniti>(`/api/gorevler/${fotograf.gorevId}/fotograflar`, formVerisi);
  },
};
