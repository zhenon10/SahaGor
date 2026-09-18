"use client";

import { useEffect } from "react";
import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useKimlik } from "@/baglam/KimlikBaglami";

const ROL_ETIKETLERI: Record<string, string> = {
  Operator: "Operatör",
  SahaPersoneli: "Saha Personeli",
  Amir: "Amir",
  SistemYoneticisi: "Sistem Yöneticisi",
};

const NAVIGASYON_OGELERI = [
  { href: "/", etiket: "Genel Bakış" },
  { href: "/harita", etiket: "Canlı Harita" },
  { href: "/gorevler", etiket: "Görevler" },
  { href: "/ekipler", etiket: "Ekipler" },
  { href: "/personel", etiket: "Personel" },
];

/**
 * Komuta panelinin tum sayfalarini saran korumali kabuk. Oturum kontrolu istemci
 * tarafinda yapilir (bkz. KimlikBaglami); henuz dogrulama tamamlanmadan bir
 * yukleme gostergesi gosterilir, aksi halde bir an icin yanlislikla korumali
 * icerik gorunup hemen /giris'e atlanmaz (flicker).
 */
export default function PanelYerlesimi({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const { kullanici, baslangicKontroluTamamlandiMi, cikisYap } = useKimlik();

  useEffect(() => {
    if (baslangicKontroluTamamlandiMi && !kullanici) {
      router.replace("/giris");
    }
  }, [baslangicKontroluTamamlandiMi, kullanici, router]);

  if (!baslangicKontroluTamamlandiMi || !kullanici) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="h-8 w-8 animate-spin rounded-full border-2 border-vurgu border-t-transparent" />
      </div>
    );
  }

  return (
    <div className="flex min-h-screen flex-col">
      <header className="flex items-center justify-between border-b border-kenarlik bg-yuzey px-6 py-3">
        <div className="flex items-center gap-6">
          <span className="text-lg font-bold text-white">SahaGör</span>
          <nav className="flex items-center gap-1">
            {NAVIGASYON_OGELERI.map((oge) => {
              const aktifMi = pathname === oge.href;
              return (
                <Link
                  key={oge.href}
                  href={oge.href}
                  className={`rounded-md px-3 py-1.5 text-sm font-medium transition ${
                    aktifMi ? "bg-vurgu text-white" : "text-sonuc hover:bg-background hover:text-white"
                  }`}
                >
                  {oge.etiket}
                </Link>
              );
            })}
          </nav>
        </div>

        <div className="flex items-center gap-4">
          <div className="text-right">
            <p className="text-sm font-medium text-white">{kullanici.adSoyad}</p>
            <p className="text-xs text-sonuc">{ROL_ETIKETLERI[kullanici.rol] ?? kullanici.rol}</p>
          </div>
          <button
            onClick={() => void cikisYap().then(() => router.replace("/giris"))}
            className="rounded-lg border border-red-500/40 px-3 py-1.5 text-sm text-red-300 transition hover:bg-red-950/40"
          >
            Çıkış Yap
          </button>
        </div>
      </header>

      <main className="flex-1">{children}</main>
    </div>
  );
}
