import { ApiIstemcisi } from "./apiIstemcisi";
import type { EkipYaniti } from "@/tipler/ekip";

export const EkiplerApi = {
  listele: (birimId?: string) => ApiIstemcisi.get<EkipYaniti[]>(`/api/ekipler${birimId ? `?birimId=${birimId}` : ""}`),

  detay: (id: string) => ApiIstemcisi.get<EkipYaniti>(`/api/ekipler/${id}`),

  olustur: (birimId: string, ad: string) => ApiIstemcisi.post<EkipYaniti>("/api/ekipler", { birimId, ad }),

  durumGuncelle: (id: string, aktifMi: boolean) => ApiIstemcisi.patch<void>(`/api/ekipler/${id}/durum`, { aktifMi }),

  uyeEkle: (id: string, personelId: string) =>
    ApiIstemcisi.post<EkipYaniti>(`/api/ekipler/${id}/uyeler`, { personelId }),

  uyeCikar: (id: string, personelId: string) =>
    ApiIstemcisi.delete<EkipYaniti>(`/api/ekipler/${id}/uyeler/${personelId}`),

  uzmanlikEkle: (id: string, gorevKategorisiId: string) =>
    ApiIstemcisi.post<EkipYaniti>(`/api/ekipler/${id}/uzmanlik-alanlari`, { gorevKategorisiId }),

  uzmanlikCikar: (id: string, kategoriId: string) =>
    ApiIstemcisi.delete<EkipYaniti>(`/api/ekipler/${id}/uzmanlik-alanlari/${kategoriId}`),
};
