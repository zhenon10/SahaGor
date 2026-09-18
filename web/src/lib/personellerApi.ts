import { ApiIstemcisi } from "./apiIstemcisi";
import type { PersonelGuncelleIstegi, PersonelOlusturIstegi, PersonelYaniti } from "@/tipler/personel";

export const PersonellerApi = {
  listele: (birimId?: string) =>
    ApiIstemcisi.get<PersonelYaniti[]>(`/api/personeller${birimId ? `?birimId=${birimId}` : ""}`),

  olustur: (istek: PersonelOlusturIstegi) => ApiIstemcisi.post<PersonelYaniti>("/api/personeller", istek),

  guncelle: (id: string, istek: PersonelGuncelleIstegi) =>
    ApiIstemcisi.put<PersonelYaniti>(`/api/personeller/${id}`, istek),

  durumGuncelle: (id: string, aktifMi: boolean) =>
    ApiIstemcisi.patch<void>(`/api/personeller/${id}/durum`, { aktifMi }),

  sifreSifirla: (id: string, yeniSifre: string) =>
    ApiIstemcisi.post<void>(`/api/personeller/${id}/sifre-sifirla`, { yeniSifre }),
};
