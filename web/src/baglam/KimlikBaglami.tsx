"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { ErisimTokeniDeposu } from "@/lib/erisimTokeniDeposu";
import { ApiHatasi } from "@/tipler/api";
import type { GirisIstegi, OturumBilgisi } from "@/tipler/kimlik";

interface KimlikBaglamiDegeri {
  kullanici: OturumBilgisi | null;
  baslangicKontroluTamamlandiMi: boolean;
  girisYapiliyorMu: boolean;
  girisYap: (istek: GirisIstegi) => Promise<void>;
  cikisYap: () => Promise<void>;
}

const KimlikBaglami = createContext<KimlikBaglamiDegeri | undefined>(undefined);

async function oturumuGetir(): Promise<OturumBilgisi | null> {
  const yaniti = await fetch("/api/auth/session");
  if (!yaniti.ok) {
    return null;
  }
  return (await yaniti.json()) as OturumBilgisi;
}

export function KimlikSaglayici({ children }: { children: ReactNode }) {
  const [kullanici, setKullanici] = useState<OturumBilgisi | null>(null);
  const [baslangicKontroluTamamlandiMi, setBaslangicKontroluTamamlandiMi] = useState(false);
  const [girisYapiliyorMu, setGirisYapiliyorMu] = useState(false);

  // Sayfa ilk yuklendiginde, httpOnly cookie'deki yenileme tokeni gecerliyse
  // kullaniciyi tekrar giris yaptirmadan oturumu sessizce geri yukler.
  useEffect(() => {
    let iptalEdildiMi = false;

    void (async () => {
      try {
        const oturum = await oturumuGetir();
        if (!iptalEdildiMi && oturum) {
          ErisimTokeniDeposu.ayarla(oturum.erisimTokeni);
          setKullanici(oturum);
        }
      } finally {
        if (!iptalEdildiMi) {
          setBaslangicKontroluTamamlandiMi(true);
        }
      }
    })();

    return () => {
      iptalEdildiMi = true;
    };
  }, []);

  // Erisim tokeninin suresi dolmadan (yaklasik) 1 dakika once sessizce yenilenir;
  // boylece komuta panelinde uzun sure acik kalan bir sekme asla aniden oturumu
  // dusurmez (Amir, bir gorevi inceleme ortasindayken 401 ile karsilasmaz).
  useEffect(() => {
    if (!kullanici) {
      return;
    }

    const sonKullanmaMs = new Date(kullanici.erisimTokeniSonKullanmaZamaniUtc).getTime();
    const gecikmeMs = Math.max(sonKullanmaMs - Date.now() - 60_000, 5_000);

    const zamanlayici = setTimeout(() => {
      void (async () => {
        const oturum = await oturumuGetir();
        if (oturum) {
          ErisimTokeniDeposu.ayarla(oturum.erisimTokeni);
          setKullanici(oturum);
        } else {
          ErisimTokeniDeposu.ayarla(null);
          setKullanici(null);
        }
      })();
    }, gecikmeMs);

    return () => clearTimeout(zamanlayici);
  }, [kullanici]);

  const girisYap = useCallback(async (istek: GirisIstegi) => {
    setGirisYapiliyorMu(true);
    try {
      const yaniti = await fetch("/api/auth/login", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(istek),
      });

      const govde = await yaniti.json();

      if (!yaniti.ok) {
        throw new ApiHatasi(typeof govde?.mesaj === "string" ? govde.mesaj : "Giriş başarısız.", yaniti.status);
      }

      const oturum = govde as OturumBilgisi;
      ErisimTokeniDeposu.ayarla(oturum.erisimTokeni);
      setKullanici(oturum);
    } finally {
      setGirisYapiliyorMu(false);
    }
  }, []);

  const cikisYap = useCallback(async () => {
    await fetch("/api/auth/logout", { method: "POST" });
    ErisimTokeniDeposu.ayarla(null);
    setKullanici(null);
  }, []);

  const deger = useMemo<KimlikBaglamiDegeri>(
    () => ({ kullanici, baslangicKontroluTamamlandiMi, girisYapiliyorMu, girisYap, cikisYap }),
    [kullanici, baslangicKontroluTamamlandiMi, girisYapiliyorMu, girisYap, cikisYap],
  );

  return <KimlikBaglami.Provider value={deger}>{children}</KimlikBaglami.Provider>;
}

export function useKimlik(): KimlikBaglamiDegeri {
  const baglam = useContext(KimlikBaglami);
  if (!baglam) {
    throw new Error("useKimlik(), KimlikSaglayici icinde kullanilmalidir.");
  }
  return baglam;
}
