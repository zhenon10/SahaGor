"use client";

import { useEffect, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { useKimlik } from "@/baglam/KimlikBaglami";
import { ApiHatasi } from "@/tipler/api";

export default function GirisSayfasi() {
  const router = useRouter();
  const { kullanici, baslangicKontroluTamamlandiMi, girisYap, girisYapiliyorMu } = useKimlik();

  const [kullaniciAdi, setKullaniciAdi] = useState("");
  const [sifre, setSifre] = useState("");
  const [hataMesaji, setHataMesaji] = useState<string | null>(null);

  // Zaten gecerli bir oturumu olan kullanici giris ekranini gormemeli.
  useEffect(() => {
    if (baslangicKontroluTamamlandiMi && kullanici) {
      router.replace("/");
    }
  }, [baslangicKontroluTamamlandiMi, kullanici, router]);

  async function gonder(olay: FormEvent) {
    olay.preventDefault();
    setHataMesaji(null);

    if (!kullaniciAdi.trim() || !sifre) {
      setHataMesaji("Kullanıcı adı ve şifre alanları zorunludur.");
      return;
    }

    try {
      await girisYap({ kullaniciAdi: kullaniciAdi.trim(), sifre });
      router.replace("/");
    } catch (hata) {
      setHataMesaji(hata instanceof ApiHatasi ? hata.message : "Beklenmeyen bir hata oluştu.");
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center px-4">
      <form onSubmit={gonder} className="w-full max-w-sm space-y-4 rounded-xl bg-yuzey p-8">
        <div className="mb-6 text-center">
          <h1 className="text-2xl font-bold text-white">SahaGör</h1>
          <p className="mt-1 text-sm text-sonuc">Komuta Paneli Girişi</p>
        </div>

        {hataMesaji && (
          <div className="rounded-lg border border-red-500/40 bg-red-950/40 px-4 py-2 text-sm text-red-300">
            {hataMesaji}
          </div>
        )}

        <div className="space-y-1">
          <label htmlFor="kullaniciAdi" className="text-sm text-sonuc">
            Kullanıcı Adı
          </label>
          <input
            id="kullaniciAdi"
            type="text"
            autoComplete="username"
            value={kullaniciAdi}
            onChange={(olay) => setKullaniciAdi(olay.target.value)}
            disabled={girisYapiliyorMu}
            className="w-full rounded-lg border border-kenarlik bg-background px-3 py-2 text-white outline-none focus:border-vurgu"
          />
        </div>

        <div className="space-y-1">
          <label htmlFor="sifre" className="text-sm text-sonuc">
            Şifre
          </label>
          <input
            id="sifre"
            type="password"
            autoComplete="current-password"
            value={sifre}
            onChange={(olay) => setSifre(olay.target.value)}
            disabled={girisYapiliyorMu}
            className="w-full rounded-lg border border-kenarlik bg-background px-3 py-2 text-white outline-none focus:border-vurgu"
          />
        </div>

        <button
          type="submit"
          disabled={girisYapiliyorMu}
          className="w-full rounded-lg bg-vurgu py-2.5 font-semibold text-white transition hover:brightness-110 disabled:opacity-60"
        >
          {girisYapiliyorMu ? "Giriş yapılıyor..." : "Giriş Yap"}
        </button>
      </form>
    </main>
  );
}
