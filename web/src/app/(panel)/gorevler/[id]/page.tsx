"use client";

import { useCallback, useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { EkiplerApi } from "@/lib/ekiplerApi";
import { GorevlerApi } from "@/lib/gorevlerApi";
import { ApiHatasi } from "@/tipler/api";
import type { AtamaOnerisi } from "@/tipler/atama";
import type { EkipYaniti } from "@/tipler/ekip";
import type { GorevDetayi } from "@/tipler/gorev";

/** Amir'in atama motorunun onerisini onayladigi veya manuel ekip sectigi ekran (SG-311, SG-320). */
export default function GorevDetaySayfasi() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();

  const [gorev, setGorev] = useState<GorevDetayi | null>(null);
  const [oneri, setOneri] = useState<AtamaOnerisi | null>(null);
  const [ekipler, setEkipler] = useState<EkipYaniti[]>([]);
  const [secilenEkipId, setSecilenEkipId] = useState("");
  const [islemYapiliyorMu, setIslemYapiliyorMu] = useState(false);
  const [hataMesaji, setHataMesaji] = useState<string | null>(null);

  const ATAMA_BEKLEYEN_DURUMLAR = new Set(["Acildi", "Atanamadi"]);

  const yeniden_yukle = useCallback(async () => {
    setHataMesaji(null);
    try {
      const detay = await GorevlerApi.detay(id);
      setGorev(detay);

      if (ATAMA_BEKLEYEN_DURUMLAR.has(detay.durum)) {
        const [oneriSonucu, ekiplerSonucu] = await Promise.all([
          GorevlerApi.atamaOnerisi(id).catch(() => null),
          EkiplerApi.listele(),
        ]);
        setOneri(oneriSonucu);
        setEkipler(ekiplerSonucu);
        setSecilenEkipId(oneriSonucu?.onerilenEkip?.ekipId ?? "");
      }
    } catch (hata) {
      setHataMesaji(hata instanceof ApiHatasi ? hata.message : "Görev yüklenemedi.");
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [id]);

  useEffect(() => {
    void (async () => {
      await yeniden_yukle();
    })();
  }, [yeniden_yukle]);

  async function ata(ekipId: string) {
    setIslemYapiliyorMu(true);
    setHataMesaji(null);
    try {
      await GorevlerApi.ata(id, ekipId);
      await yeniden_yukle();
    } catch (hata) {
      setHataMesaji(hata instanceof ApiHatasi ? hata.message : "Atama başarısız oldu.");
    } finally {
      setIslemYapiliyorMu(false);
    }
  }

  async function iptalEt() {
    const neden = window.prompt("İptal gerekçesini girin:");
    if (!neden) return;

    setIslemYapiliyorMu(true);
    try {
      await GorevlerApi.iptalEt(id, neden);
      router.push("/gorevler");
    } catch (hata) {
      setHataMesaji(hata instanceof ApiHatasi ? hata.message : "İptal işlemi başarısız oldu.");
      setIslemYapiliyorMu(false);
    }
  }

  if (!gorev) {
    return (
      <div className="flex h-64 items-center justify-center">
        {hataMesaji ? (
          <p className="text-sm text-red-300">{hataMesaji}</p>
        ) : (
          <div className="h-6 w-6 animate-spin rounded-full border-2 border-vurgu border-t-transparent" />
        )}
      </div>
    );
  }

  const atamaBekliyorMu = ATAMA_BEKLEYEN_DURUMLAR.has(gorev.durum);
  const iptalEdilebilirMi = gorev.durum !== "Tamamlandi" && gorev.durum !== "Dogrulandi" && gorev.durum !== "Iptal";

  return (
    <div className="mx-auto max-w-3xl p-6">
      <button onClick={() => router.push("/gorevler")} className="mb-4 text-sm text-sonuc hover:text-white">
        ‹ Görevlere dön
      </button>

      {hataMesaji && (
        <div className="mb-4 rounded-lg border border-red-500/40 bg-red-950/40 px-4 py-2 text-sm text-red-300">
          {hataMesaji}
        </div>
      )}

      <div className="rounded-xl bg-yuzey p-6">
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-xl font-bold text-white">{gorev.baslik}</h1>
            <p className="mt-1 text-sm text-sonuc">
              {gorev.kategoriAdi} {gorev.bolgeAdi && `· ${gorev.bolgeAdi}`}
            </p>
          </div>
          <span className="rounded bg-blue-600/20 px-3 py-1 text-xs font-semibold text-blue-300">{gorev.durum}</span>
        </div>

        {gorev.aciklama && <p className="mt-4 text-sm text-white">{gorev.aciklama}</p>}

        <dl className="mt-4 grid grid-cols-2 gap-3 text-sm">
          <div>
            <dt className="text-xs text-sonuc">Öncelik</dt>
            <dd className="text-white">{gorev.oncelik}</dd>
          </div>
          <div>
            <dt className="text-xs text-sonuc">Kaynak</dt>
            <dd className="text-white">{gorev.kaynak}</dd>
          </div>
          <div>
            <dt className="text-xs text-sonuc">Atanan Ekip</dt>
            <dd className="text-white">{gorev.atananEkipAdi ?? "—"}</dd>
          </div>
          <div>
            <dt className="text-xs text-sonuc">SLA Hedefi</dt>
            <dd className={gorev.slaIhlalEdildiMi ? "font-semibold text-red-400" : "text-white"}>
              {new Date(gorev.slaHedefZamaniUtc).toLocaleString("tr-TR")}
              {gorev.slaIhlalEdildiMi && " (Aşıldı)"}
            </dd>
          </div>
        </dl>

        {iptalEdilebilirMi && (
          <button
            onClick={() => void iptalEt()}
            disabled={islemYapiliyorMu}
            className="mt-4 rounded-lg border border-red-500/40 px-3 py-1.5 text-xs font-semibold text-red-300 hover:bg-red-950/40 disabled:opacity-50"
          >
            Görevi İptal Et
          </button>
        )}
      </div>

      {atamaBekliyorMu && (
        <div className="mt-6 rounded-xl bg-yuzey p-6">
          <h2 className="text-lg font-semibold text-white">Ekip Ataması</h2>

          {oneri?.onerilenEkip ? (
            <div className="mt-3 rounded-lg border border-vurgu/50 bg-background p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="font-semibold text-white">Önerilen: {oneri.onerilenEkip.ekipAdi}</p>
                  <p className="text-xs text-sonuc">Toplam skor: %{Math.round(oneri.onerilenEkip.toplamSkor * 100)}</p>
                </div>
                <button
                  onClick={() => void ata(oneri.onerilenEkip!.ekipId)}
                  disabled={islemYapiliyorMu}
                  className="rounded-lg bg-vurgu px-4 py-2 text-sm font-semibold text-white hover:brightness-110 disabled:opacity-50"
                >
                  Tek Tıkla Onayla
                </button>
              </div>

              <div className="mt-3 grid grid-cols-4 gap-2 text-center text-xs">
                <SkorRozeti etiket="Mesafe" deger={oneri.onerilenEkip.mesafeSkoru} />
                <SkorRozeti etiket="Müsaitlik" deger={oneri.onerilenEkip.musaitlikSkoru} />
                <SkorRozeti etiket="Yetkinlik" deger={oneri.onerilenEkip.yetkinlikSkoru} />
                <SkorRozeti etiket="İş Yükü" deger={oneri.onerilenEkip.isYukuSkoru} />
              </div>

              {oneri.tumAdaylar.length > 1 && (
                <details className="mt-3 text-xs text-sonuc">
                  <summary className="cursor-pointer">Diğer {oneri.tumAdaylar.length - 1} aday</summary>
                  <ul className="mt-2 space-y-1">
                    {oneri.tumAdaylar
                      .filter((aday) => aday.ekipId !== oneri.onerilenEkip!.ekipId)
                      .map((aday) => (
                        <li key={aday.ekipId}>
                          {aday.ekipAdi} — toplam skor: %{Math.round(aday.toplamSkor * 100)}
                        </li>
                      ))}
                  </ul>
                </details>
              )}
            </div>
          ) : (
            <p className="mt-3 text-sm text-sonuc">Şu an uygun (aktif üyesi olan) bir ekip önerisi bulunamadı.</p>
          )}

          <div className="mt-4 flex items-end gap-3">
            <div className="flex-1">
              <label className="text-xs text-sonuc">Manuel ekip seç</label>
              <select
                value={secilenEkipId}
                onChange={(olay) => setSecilenEkipId(olay.target.value)}
                className="mt-1 w-full rounded-lg border border-kenarlik bg-background px-3 py-2 text-sm text-white outline-none"
              >
                <option value="">Ekip seçin...</option>
                {ekipler.map((ekip) => (
                  <option key={ekip.id} value={ekip.id}>
                    {ekip.ad} ({ekip.birimAdi})
                  </option>
                ))}
              </select>
            </div>
            <button
              onClick={() => secilenEkipId && void ata(secilenEkipId)}
              disabled={!secilenEkipId || islemYapiliyorMu}
              className="rounded-lg border border-kenarlik px-4 py-2 text-sm font-semibold text-white hover:bg-background disabled:opacity-50"
            >
              Ata
            </button>
          </div>
        </div>
      )}

      <div className="mt-6 rounded-xl bg-yuzey p-6">
        <h2 className="text-lg font-semibold text-white">Kanıt Fotoğrafları ({gorev.fotograflar.length})</h2>
        {gorev.fotograflar.length === 0 ? (
          <p className="mt-2 text-sm text-sonuc">Henüz fotoğraf eklenmedi.</p>
        ) : (
          <ul className="mt-3 space-y-2 text-sm text-sonuc">
            {gorev.fotograflar.map((foto) => (
              <li key={foto.id}>
                📷 {foto.asama} — {new Date(foto.cekilmeZamaniUtc).toLocaleString("tr-TR")}
              </li>
            ))}
          </ul>
        )}
      </div>

      <div className="mt-6 rounded-xl bg-yuzey p-6">
        <h2 className="text-lg font-semibold text-white">Durum Geçmişi</h2>
        <ul className="mt-3 space-y-2 text-sm">
          {gorev.durumGecmisi.map((kayit, index) => (
            <li key={index} className="border-l-2 border-kenarlik pl-3 text-sonuc">
              <span className="font-medium text-white">{kayit.yeniDurum}</span> —{" "}
              {new Date(kayit.degisiklikZamaniUtc).toLocaleString("tr-TR")}
              {kayit.not && <span className="block text-xs">{kayit.not}</span>}
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

function SkorRozeti({ etiket, deger }: { etiket: string; deger: number }) {
  return (
    <div className="rounded bg-yuzey p-2">
      <p className="text-sonuc">{etiket}</p>
      <p className="font-semibold text-white">%{Math.round(deger * 100)}</p>
    </div>
  );
}
