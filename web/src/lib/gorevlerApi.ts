import { ApiIstemcisi } from "./apiIstemcisi";
import type { GorevDetayi, GorevIstatistikleri, GorevOzeti, SayfalanmisSonuc } from "@/tipler/gorev";
import type { AtamaOnerisi } from "@/tipler/atama";

export interface GorevListeFiltresi {
  durum?: string;
  sayfa?: number;
  sayfaBoyutu?: number;
}

function sorguDizesiOlustur(filtre: GorevListeFiltresi): string {
  const parametreler = new URLSearchParams();
  if (filtre.durum) parametreler.set("durum", filtre.durum);
  parametreler.set("sayfa", String(filtre.sayfa ?? 1));
  parametreler.set("sayfaBoyutu", String(filtre.sayfaBoyutu ?? 20));
  return `?${parametreler.toString()}`;
}

export const GorevlerApi = {
  /**
   * Harita icin acik gorevleri getirir. Backend'in filtre sozlesmesi tek bir "durum"
   * degeri kabul ettiginden (bkz. GorevTalebiFiltre), "acik" kumesi (Tamamlandi/
   * Dogrulandi/Iptal disindaki tum durumlar) istemci tarafinda filtrelenir.
   */
  listele: () => ApiIstemcisi.get<SayfalanmisSonuc<GorevOzeti>>("/api/gorevler?sayfaBoyutu=100"),

  listeleFiltreli: (filtre: GorevListeFiltresi) =>
    ApiIstemcisi.get<SayfalanmisSonuc<GorevOzeti>>(`/api/gorevler${sorguDizesiOlustur(filtre)}`),

  detay: (id: string) => ApiIstemcisi.get<GorevDetayi>(`/api/gorevler/${id}`),

  atamaOnerisi: (id: string) => ApiIstemcisi.get<AtamaOnerisi>(`/api/gorevler/${id}/atama-onerisi`),

  ata: (id: string, ekipId: string, sorumluPersonelId?: string) =>
    ApiIstemcisi.post<GorevDetayi>(`/api/gorevler/${id}/ata`, { ekipId, sorumluPersonelId: sorumluPersonelId ?? null }),

  iptalEt: (id: string, neden: string) => ApiIstemcisi.delete<void>(`/api/gorevler/${id}`, { neden }),

  istatistikler: (gunSayisi = 7) =>
    ApiIstemcisi.get<GorevIstatistikleri>(`/api/gorevler/istatistikler?gunSayisi=${gunSayisi}`),
};
