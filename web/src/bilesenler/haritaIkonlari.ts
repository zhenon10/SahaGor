import L from "leaflet";
import type { GorevOzeti } from "@/tipler/gorev";

/**
 * Leaflet'in varsayilan pin resimleri, bundler'larda kirilan bir dosya yolu bekler
 * (klasik react-leaflet tuzagi). Bunun yerine inline stilli, tamamen bizim kontrolumuzde
 * olan basit divIcon'lar kullanilir; harici resim dosyasi yuklemesi gerekmez.
 */

export function gorevIkonuOlustur(gorev: GorevOzeti): L.DivIcon {
  const renk = gorev.slaIhlalEdildiMi ? "#ef4444" : "#2563eb";

  return L.divIcon({
    className: "",
    html: `<div style="width:16px;height:16px;border-radius:50%;background:${renk};border:2px solid #ffffff;box-shadow:0 0 6px rgba(0,0,0,0.6);"></div>`,
    iconSize: [16, 16],
    iconAnchor: [8, 8],
    popupAnchor: [0, -8],
  });
}

export function ekipIkonuOlustur(): L.DivIcon {
  return L.divIcon({
    className: "",
    html: `<div style="font-size:22px;line-height:22px;filter:drop-shadow(0 0 3px rgba(0,0,0,0.8));">🚐</div>`,
    iconSize: [24, 24],
    iconAnchor: [12, 12],
    popupAnchor: [0, -12],
  });
}
