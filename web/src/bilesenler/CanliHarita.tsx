"use client";

import { useEffect, useState } from "react";
import { MapContainer, Marker, Popup, TileLayer } from "react-leaflet";
import "leaflet/dist/leaflet.css";
import { EkiplerApi } from "@/lib/ekiplerApi";
import { GorevlerApi } from "@/lib/gorevlerApi";
import { signalRBaglantisiOlustur } from "@/lib/signalRBaglantisi";
import { KAPALI_GOREV_DURUMLARI, type GorevBildirimi, type GorevOzeti } from "@/tipler/gorev";
import type { EkipKonumBildirimi, EkipYaniti } from "@/tipler/ekip";
import { ekipIkonuOlustur, gorevIkonuOlustur } from "./haritaIkonlari";

const VARSAYILAN_MERKEZ: [number, number] = [41.0082, 28.9784];

type BaglantiDurumu = "baglaniyor" | "bagli" | "koptu";

/**
 * Komuta panelinin canli haritasi (SG-301, SG-302). Ilk acilista mevcut acik gorevleri
 * ve ekip konumlarini REST'ten ceker; ardindan SignalR'a abone olup her degisikligi
 * sayfa yenilenmeden (WebSocket, polling YOK) yansitir.
 */
export function CanliHarita() {
  const [gorevler, setGorevler] = useState<GorevOzeti[]>([]);
  const [ekipler, setEkipler] = useState<EkipYaniti[]>([]);
  const [baglantiDurumu, setBaglantiDurumu] = useState<BaglantiDurumu>("baglaniyor");

  useEffect(() => {
    let iptalEdildiMi = false;

    void (async () => {
      const [gorevSonucu, ekipSonucu] = await Promise.all([GorevlerApi.listele(), EkiplerApi.listele()]);
      if (iptalEdildiMi) return;

      setGorevler(gorevSonucu.kayitlar.filter((g) => !KAPALI_GOREV_DURUMLARI.has(g.durum)));
      setEkipler(ekipSonucu);
    })();

    return () => {
      iptalEdildiMi = true;
    };
  }, []);

  useEffect(() => {
    const baglanti = signalRBaglantisiOlustur();

    baglanti.on("gorevGuncellendi", (bildirim: GorevBildirimi) => {
      const kapandiMi = KAPALI_GOREV_DURUMLARI.has(bildirim.durum);

      setGorevler((oncekiler) => {
        if (kapandiMi) {
          return oncekiler.filter((g) => g.id !== bildirim.gorevId);
        }

        const zatenListedeMi = oncekiler.some((g) => g.id === bildirim.gorevId);
        if (zatenListedeMi) {
          return oncekiler.map((g) => (g.id === bildirim.gorevId ? { ...g, durum: bildirim.durum } : g));
        }

        // Yeni acilan bir gorev: konum/kategori gibi tam bilgisi bildirimde yok,
        // en guncel listeyi yeniden cekmek en basit ve tutarli cozumdur.
        void GorevlerApi.listele().then((sonuc) =>
          setGorevler(sonuc.kayitlar.filter((g) => !KAPALI_GOREV_DURUMLARI.has(g.durum))),
        );
        return oncekiler;
      });
    });

    baglanti.on("ekipKonumuGuncellendi", (bildirim: EkipKonumBildirimi) => {
      setEkipler((oncekiler) =>
        oncekiler.map((ekip) =>
          ekip.id === bildirim.ekipId
            ? {
                ...ekip,
                guncelKonumEnlem: bildirim.enlem,
                guncelKonumBoylam: bildirim.boylam,
                konumGuncellenmeZamaniUtc: bildirim.zamanUtc,
              }
            : ekip,
        ),
      );
    });

    baglanti.onreconnecting(() => setBaglantiDurumu("koptu"));
    baglanti.onreconnected(() => {
      setBaglantiDurumu("bagli");
      void baglanti.invoke("AmirPanelineKatil");
    });
    baglanti.onclose(() => setBaglantiDurumu("koptu"));

    baglanti
      .start()
      .then(() => {
        setBaglantiDurumu("bagli");
        return baglanti.invoke("AmirPanelineKatil");
      })
      .catch(() => setBaglantiDurumu("koptu"));

    return () => {
      void baglanti.stop();
    };
  }, []);

  return (
    <div className="relative h-full w-full">
      <BaglantiRozeti durum={baglantiDurumu} />

      <MapContainer center={VARSAYILAN_MERKEZ} zoom={13} className="h-full w-full">
        <TileLayer
          url="https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png"
          attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>, &copy; <a href="https://carto.com/attributions">CARTO</a>'
        />

        {gorevler.map((gorev) => (
          <Marker key={gorev.id} position={[gorev.enlem, gorev.boylam]} icon={gorevIkonuOlustur(gorev)}>
            <Popup>
              <strong>{gorev.baslik}</strong>
              <br />
              {gorev.kategoriAdi}
              <br />
              Durum: {gorev.durum}
              {gorev.bolgeAdi && (
                <>
                  <br />
                  Bölge: {gorev.bolgeAdi}
                </>
              )}
              {gorev.slaIhlalEdildiMi && (
                <>
                  <br />
                  <span style={{ color: "#ef4444", fontWeight: 600 }}>SLA aşıldı</span>
                </>
              )}
            </Popup>
          </Marker>
        ))}

        {ekipler
          .filter((ekip): ekip is EkipYaniti & { guncelKonumEnlem: number; guncelKonumBoylam: number } =>
            ekip.guncelKonumEnlem != null && ekip.guncelKonumBoylam != null,
          )
          .map((ekip) => (
            <Marker key={ekip.id} position={[ekip.guncelKonumEnlem, ekip.guncelKonumBoylam]} icon={ekipIkonuOlustur()}>
              <Popup>
                <strong>{ekip.ad}</strong>
                <br />
                {ekip.birimAdi}
                <br />
                {ekip.uyeler.length} üye
              </Popup>
            </Marker>
          ))}
      </MapContainer>
    </div>
  );
}

function BaglantiRozeti({ durum }: { durum: BaglantiDurumu }) {
  const metinler: Record<BaglantiDurumu, string> = {
    baglaniyor: "Bağlanıyor...",
    bagli: "Canlı",
    koptu: "Bağlantı koptu — yeniden deneniyor",
  };

  const renkler: Record<BaglantiDurumu, string> = {
    baglaniyor: "bg-slate-600 text-slate-200",
    bagli: "bg-emerald-600/90 text-white",
    koptu: "bg-red-600/90 text-white",
  };

  return (
    <div className={`absolute right-3 top-3 z-[1000] rounded-full px-3 py-1 text-xs font-semibold shadow ${renkler[durum]}`}>
      {metinler[durum]}
    </div>
  );
}
