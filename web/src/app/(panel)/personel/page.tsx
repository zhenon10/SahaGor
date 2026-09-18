"use client";

import { useCallback, useEffect, useState } from "react";
import { BirimlerApi } from "@/lib/birimlerApi";
import { PersonellerApi } from "@/lib/personellerApi";
import { ApiHatasi } from "@/tipler/api";
import type { BirimYaniti } from "@/tipler/birim";
import { PERSONEL_ROLLERI, type PersonelYaniti } from "@/tipler/personel";

const ROL_ETIKETLERI: Record<string, string> = {
  Operator: "Operatör",
  SahaPersoneli: "Saha Personeli",
  Amir: "Amir",
  SistemYoneticisi: "Sistem Yöneticisi",
};

/** Personel yonetimi: hesap olusturma, aktif/pasif, sifre sifirlama (SG-320). */
export default function PersonelSayfasi() {
  const [personeller, setPersoneller] = useState<PersonelYaniti[]>([]);
  const [birimler, setBirimler] = useState<BirimYaniti[]>([]);
  const [hataMesaji, setHataMesaji] = useState<string | null>(null);
  const [formAcikMi, setFormAcikMi] = useState(false);

  const [form, setForm] = useState({
    birimId: "",
    adSoyad: "",
    kullaniciAdi: "",
    telefon: "",
    sifre: "",
    rol: "SahaPersoneli",
  });

  const yeniden_yukle = useCallback(async () => {
    try {
      const [personelSonucu, birimSonucu] = await Promise.all([PersonellerApi.listele(), BirimlerApi.listele()]);
      setPersoneller(personelSonucu);
      setBirimler(birimSonucu);
      setForm((onceki) => ({ ...onceki, birimId: onceki.birimId || birimSonucu[0]?.id || "" }));
    } catch (hata) {
      setHataMesaji(hata instanceof ApiHatasi ? hata.message : "Veriler yüklenemedi.");
    }
  }, []);

  useEffect(() => {
    void (async () => {
      await yeniden_yukle();
    })();
  }, [yeniden_yukle]);

  async function personelOlustur() {
    setHataMesaji(null);
    try {
      await PersonellerApi.olustur({
        birimId: form.birimId,
        adSoyad: form.adSoyad.trim(),
        kullaniciAdi: form.kullaniciAdi.trim(),
        telefon: form.telefon.trim(),
        sifre: form.sifre,
        rol: form.rol,
      });
      setForm((onceki) => ({ ...onceki, adSoyad: "", kullaniciAdi: "", telefon: "", sifre: "" }));
      setFormAcikMi(false);
      await yeniden_yukle();
    } catch (hata) {
      setHataMesaji(hata instanceof ApiHatasi ? hata.message : "Personel oluşturulamadı.");
    }
  }

  async function durumDegistir(personel: PersonelYaniti) {
    try {
      await PersonellerApi.durumGuncelle(personel.id, !personel.aktifMi);
      await yeniden_yukle();
    } catch (hata) {
      setHataMesaji(hata instanceof ApiHatasi ? hata.message : "Durum güncellenemedi.");
    }
  }

  async function sifreSifirla(personel: PersonelYaniti) {
    const yeniSifre = window.prompt(`${personel.adSoyad} için yeni şifre (en az 8 karakter):`);
    if (!yeniSifre) return;

    try {
      await PersonellerApi.sifreSifirla(personel.id, yeniSifre);
      window.alert("Şifre başarıyla sıfırlandı.");
    } catch (hata) {
      setHataMesaji(hata instanceof ApiHatasi ? hata.message : "Şifre sıfırlanamadı.");
    }
  }

  return (
    <div className="mx-auto max-w-4xl p-6">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-bold text-white">Personel</h1>
        <button
          onClick={() => setFormAcikMi(!formAcikMi)}
          className="rounded-lg bg-vurgu px-4 py-2 text-sm font-semibold text-white hover:brightness-110"
        >
          {formAcikMi ? "Vazgeç" : "Yeni Personel"}
        </button>
      </div>

      {hataMesaji && (
        <div className="mb-4 rounded-lg border border-red-500/40 bg-red-950/40 px-4 py-2 text-sm text-red-300">
          {hataMesaji}
        </div>
      )}

      {formAcikMi && (
        <div className="mb-6 grid grid-cols-2 gap-3 rounded-xl bg-yuzey p-4">
          <AlanGirdisi etiket="Ad Soyad" deger={form.adSoyad} onDegisti={(v) => setForm({ ...form, adSoyad: v })} />
          <AlanGirdisi
            etiket="Kullanıcı Adı"
            deger={form.kullaniciAdi}
            onDegisti={(v) => setForm({ ...form, kullaniciAdi: v })}
          />
          <AlanGirdisi etiket="Telefon" deger={form.telefon} onDegisti={(v) => setForm({ ...form, telefon: v })} />
          <AlanGirdisi
            etiket="Geçici Şifre"
            deger={form.sifre}
            tip="password"
            onDegisti={(v) => setForm({ ...form, sifre: v })}
          />

          <div>
            <label className="text-xs text-sonuc">Birim</label>
            <select
              value={form.birimId}
              onChange={(olay) => setForm({ ...form, birimId: olay.target.value })}
              className="mt-1 w-full rounded-lg border border-kenarlik bg-background px-3 py-2 text-sm text-white outline-none"
            >
              {birimler.map((birim) => (
                <option key={birim.id} value={birim.id}>
                  {birim.ad}
                </option>
              ))}
            </select>
          </div>

          <div>
            <label className="text-xs text-sonuc">Rol</label>
            <select
              value={form.rol}
              onChange={(olay) => setForm({ ...form, rol: olay.target.value })}
              className="mt-1 w-full rounded-lg border border-kenarlik bg-background px-3 py-2 text-sm text-white outline-none"
            >
              {PERSONEL_ROLLERI.map((rol) => (
                <option key={rol} value={rol}>
                  {ROL_ETIKETLERI[rol]}
                </option>
              ))}
            </select>
          </div>

          <div className="col-span-2">
            <button
              onClick={() => void personelOlustur()}
              className="rounded-lg bg-vurgu px-4 py-2 text-sm font-semibold text-white hover:brightness-110"
            >
              Oluştur
            </button>
          </div>
        </div>
      )}

      <div className="overflow-hidden rounded-xl bg-yuzey">
        <table className="w-full text-left text-sm">
          <thead className="bg-background/50 text-xs uppercase text-sonuc">
            <tr>
              <th className="px-4 py-3">Ad Soyad</th>
              <th className="px-4 py-3">Rol</th>
              <th className="px-4 py-3">Birim</th>
              <th className="px-4 py-3">Durum</th>
              <th className="px-4 py-3 text-right">İşlemler</th>
            </tr>
          </thead>
          <tbody>
            {personeller.map((personel) => (
              <tr key={personel.id} className="border-t border-kenarlik">
                <td className="px-4 py-3 text-white">
                  {personel.adSoyad}
                  {personel.kilitliMi && <span className="ml-2 text-xs text-red-400">(kilitli)</span>}
                </td>
                <td className="px-4 py-3 text-sonuc">{ROL_ETIKETLERI[personel.rol] ?? personel.rol}</td>
                <td className="px-4 py-3 text-sonuc">{personel.birimAdi}</td>
                <td className="px-4 py-3">
                  <span className={personel.aktifMi ? "text-xs text-emerald-400" : "text-xs text-sonuc"}>
                    {personel.aktifMi ? "Aktif" : "Pasif"}
                  </span>
                </td>
                <td className="px-4 py-3 text-right">
                  <button
                    onClick={() => void sifreSifirla(personel)}
                    className="mr-2 text-xs text-sonuc hover:text-white hover:underline"
                  >
                    Şifre Sıfırla
                  </button>
                  <button
                    onClick={() => void durumDegistir(personel)}
                    className="text-xs text-sonuc hover:text-white hover:underline"
                  >
                    {personel.aktifMi ? "Pasifleştir" : "Aktifleştir"}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}

function AlanGirdisi({
  etiket,
  deger,
  onDegisti,
  tip = "text",
}: {
  etiket: string;
  deger: string;
  onDegisti: (deger: string) => void;
  tip?: string;
}) {
  return (
    <div>
      <label className="text-xs text-sonuc">{etiket}</label>
      <input
        type={tip}
        value={deger}
        onChange={(olay) => onDegisti(olay.target.value)}
        className="mt-1 w-full rounded-lg border border-kenarlik bg-background px-3 py-2 text-sm text-white outline-none"
      />
    </div>
  );
}
