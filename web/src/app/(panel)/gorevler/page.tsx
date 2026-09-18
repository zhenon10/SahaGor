"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { GorevlerApi } from "@/lib/gorevlerApi";
import { ApiHatasi } from "@/tipler/api";
import type { GorevOzeti } from "@/tipler/gorev";

const DURUM_SECENEKLERI = [
  { deger: "", etiket: "Tüm durumlar" },
  { deger: "Acildi", etiket: "Açıldı" },
  { deger: "Atanamadi", etiket: "Atanamadı" },
  { deger: "Atandi", etiket: "Atandı" },
  { deger: "YolaCikildi", etiket: "Yola Çıkıldı" },
  { deger: "Baslandi", etiket: "Başlandı" },
  { deger: "Tamamlandi", etiket: "Tamamlandı" },
  { deger: "Dogrulandi", etiket: "Doğrulandı" },
  { deger: "Iptal", etiket: "İptal" },
];

/** Görev listesi sayfası (SG-320'nin harita dışı, tablo bazlı görünümü). */
export default function GorevlerSayfasi() {
  const [gorevler, setGorevler] = useState<GorevOzeti[]>([]);
  const [durumFiltresi, setDurumFiltresi] = useState("");
  const [yukleniyorMu, setYukleniyorMu] = useState(true);
  const [hataMesaji, setHataMesaji] = useState<string | null>(null);

  useEffect(() => {
    let iptalEdildiMi = false;

    void (async () => {
      setYukleniyorMu(true);
      try {
        const sonuc = await GorevlerApi.listeleFiltreli({ durum: durumFiltresi || undefined, sayfaBoyutu: 50 });
        if (!iptalEdildiMi) setGorevler(sonuc.kayitlar);
      } catch (hata) {
        if (!iptalEdildiMi) setHataMesaji(hata instanceof ApiHatasi ? hata.message : "Görevler yüklenemedi.");
      } finally {
        if (!iptalEdildiMi) setYukleniyorMu(false);
      }
    })();

    return () => {
      iptalEdildiMi = true;
    };
  }, [durumFiltresi]);

  return (
    <div className="mx-auto max-w-5xl p-6">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-bold text-white">Görevler</h1>
        <select
          value={durumFiltresi}
          onChange={(olay) => setDurumFiltresi(olay.target.value)}
          className="rounded-lg border border-kenarlik bg-yuzey px-3 py-2 text-sm text-white outline-none"
        >
          {DURUM_SECENEKLERI.map((secenek) => (
            <option key={secenek.deger} value={secenek.deger}>
              {secenek.etiket}
            </option>
          ))}
        </select>
      </div>

      {hataMesaji && (
        <div className="mb-4 rounded-lg border border-red-500/40 bg-red-950/40 px-4 py-2 text-sm text-red-300">
          {hataMesaji}
        </div>
      )}

      {yukleniyorMu ? (
        <div className="flex h-32 items-center justify-center">
          <div className="h-6 w-6 animate-spin rounded-full border-2 border-vurgu border-t-transparent" />
        </div>
      ) : (
        <div className="overflow-hidden rounded-xl bg-yuzey">
          <table className="w-full text-left text-sm">
            <thead className="bg-background/50 text-xs uppercase text-sonuc">
              <tr>
                <th className="px-4 py-3">Başlık</th>
                <th className="px-4 py-3">Kategori</th>
                <th className="px-4 py-3">Durum</th>
                <th className="px-4 py-3">Ekip</th>
                <th className="px-4 py-3">SLA</th>
              </tr>
            </thead>
            <tbody>
              {gorevler.length === 0 ? (
                <tr>
                  <td colSpan={5} className="px-4 py-8 text-center text-sonuc">
                    Kayıt bulunamadı.
                  </td>
                </tr>
              ) : (
                gorevler.map((gorev) => (
                  <tr key={gorev.id} className="border-t border-kenarlik hover:bg-background/40">
                    <td className="px-4 py-3">
                      <Link href={`/gorevler/${gorev.id}`} className="font-medium text-white hover:text-vurgu">
                        {gorev.baslik}
                      </Link>
                    </td>
                    <td className="px-4 py-3 text-sonuc">{gorev.kategoriAdi}</td>
                    <td className="px-4 py-3">
                      <span className="rounded bg-blue-600/20 px-2 py-1 text-xs font-medium text-blue-300">
                        {gorev.durum}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-sonuc">{gorev.atananEkipAdi ?? "—"}</td>
                    <td className="px-4 py-3">
                      {gorev.slaIhlalEdildiMi ? (
                        <span className="text-xs font-semibold text-red-400">Aşıldı</span>
                      ) : (
                        <span className="text-xs text-emerald-400">Normal</span>
                      )}
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
