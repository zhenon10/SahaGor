"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { GorevlerApi } from "@/lib/gorevlerApi";
import { ApiHatasi } from "@/tipler/api";
import type { GorevIstatistikleri } from "@/tipler/gorev";

/** Komuta paneli KPI dashboard'u (SG-321): acik gorev sayisi, SLA ihlal orani, ortalama cozum suresi. */
export default function DashboardSayfasi() {
  const [istatistikler, setIstatistikler] = useState<GorevIstatistikleri | null>(null);
  const [hataMesaji, setHataMesaji] = useState<string | null>(null);

  useEffect(() => {
    let iptalEdildiMi = false;

    void (async () => {
      try {
        const sonuc = await GorevlerApi.istatistikler(7);
        if (!iptalEdildiMi) setIstatistikler(sonuc);
      } catch (hata) {
        if (!iptalEdildiMi) {
          setHataMesaji(hata instanceof ApiHatasi ? hata.message : "İstatistikler yüklenemedi.");
        }
      }
    })();

    return () => {
      iptalEdildiMi = true;
    };
  }, []);

  return (
    <div className="mx-auto max-w-5xl p-6">
      <div className="mb-6 flex items-center justify-between">
        <h1 className="text-xl font-bold text-white">Genel Bakış</h1>
        <div className="flex gap-3">
          <Link href="/harita" className="rounded-lg bg-vurgu px-4 py-2 text-sm font-semibold text-white hover:brightness-110">
            Canlı Haritayı Aç
          </Link>
          <Link
            href="/gorevler"
            className="rounded-lg border border-kenarlik px-4 py-2 text-sm font-semibold text-white hover:bg-yuzey"
          >
            Görev Listesi
          </Link>
        </div>
      </div>

      {hataMesaji && (
        <div className="mb-4 rounded-lg border border-red-500/40 bg-red-950/40 px-4 py-2 text-sm text-red-300">
          {hataMesaji}
        </div>
      )}

      {!istatistikler ? (
        <div className="flex h-32 items-center justify-center">
          <div className="h-6 w-6 animate-spin rounded-full border-2 border-vurgu border-t-transparent" />
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <KpiKarti baslik="Açık Görev" deger={String(istatistikler.acikGorevSayisi)} />
          <KpiKarti
            baslik="SLA İhlal Oranı"
            deger={`%${Math.round(istatistikler.slaIhlalOrani * 100)}`}
            vurgu={istatistikler.slaIhlalOrani > 0.2}
            altMetin={`${istatistikler.slaIhlalSayisi} görev SLA'yı aştı`}
          />
          <KpiKarti
            baslik="Ort. Çözüm Süresi"
            deger={
              istatistikler.ortalamaCozumSuresiDakika != null
                ? sureyiBicimlendir(istatistikler.ortalamaCozumSuresiDakika)
                : "—"
            }
            altMetin={`Son ${istatistikler.gunSayisi} gün`}
          />
          <KpiKarti
            baslik="Son 7 Gün"
            deger={String(istatistikler.pencereIcindeOlusturulanGorevSayisi)}
            altMetin={`${istatistikler.pencereIcindeTamamlananGorevSayisi} tamamlandı`}
          />
        </div>
      )}
    </div>
  );
}

function sureyiBicimlendir(dakika: number): string {
  if (dakika < 60) return `${Math.round(dakika)} dk`;
  const saat = dakika / 60;
  if (saat < 24) return `${saat.toFixed(1)} sa`;
  return `${(saat / 24).toFixed(1)} gün`;
}

function KpiKarti({
  baslik,
  deger,
  altMetin,
  vurgu,
}: {
  baslik: string;
  deger: string;
  altMetin?: string;
  vurgu?: boolean;
}) {
  return (
    <div className="rounded-xl bg-yuzey p-5">
      <p className="text-xs font-medium uppercase tracking-wide text-sonuc">{baslik}</p>
      <p className={`mt-2 text-3xl font-bold ${vurgu ? "text-red-400" : "text-white"}`}>{deger}</p>
      {altMetin && <p className="mt-1 text-xs text-sonuc">{altMetin}</p>}
    </div>
  );
}
