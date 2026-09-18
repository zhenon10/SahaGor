"use client";

import dynamic from "next/dynamic";

// Leaflet, modul yuklenirken "window"a erisir; bu yuzden sunucu tarafinda (SSR)
// render edilemez. "ssr: false" ile bu bilesen SADECE tarayicida yuklenir.
const CanliHarita = dynamic(() => import("@/bilesenler/CanliHarita").then((mod) => mod.CanliHarita), {
  ssr: false,
  loading: () => (
    <div className="flex h-full w-full items-center justify-center">
      <div className="h-8 w-8 animate-spin rounded-full border-2 border-vurgu border-t-transparent" />
    </div>
  ),
});

export default function HaritaSayfasi() {
  return (
    <div className="h-[calc(100vh-57px)] w-full">
      <CanliHarita />
    </div>
  );
}
