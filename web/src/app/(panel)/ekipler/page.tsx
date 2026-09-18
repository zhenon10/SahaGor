"use client";

import { useCallback, useEffect, useState } from "react";
import { BirimlerApi } from "@/lib/birimlerApi";
import { EkiplerApi } from "@/lib/ekiplerApi";
import { GorevKategorileriApi } from "@/lib/gorevKategorileriApi";
import { PersonellerApi } from "@/lib/personellerApi";
import { ApiHatasi } from "@/tipler/api";
import type { BirimYaniti } from "@/tipler/birim";
import type { EkipYaniti } from "@/tipler/ekip";
import type { GorevKategorisiYaniti } from "@/tipler/gorevKategorisi";
import type { PersonelYaniti } from "@/tipler/personel";

/** Ekip yonetimi: olusturma, uye ve uzmanlik alani atama (SG-320). */
export default function EkiplerSayfasi() {
  const [ekipler, setEkipler] = useState<EkipYaniti[]>([]);
  const [birimler, setBirimler] = useState<BirimYaniti[]>([]);
  const [kategoriler, setKategoriler] = useState<GorevKategorisiYaniti[]>([]);
  const [personeller, setPersoneller] = useState<PersonelYaniti[]>([]);
  const [acikEkipId, setAcikEkipId] = useState<string | null>(null);
  const [hataMesaji, setHataMesaji] = useState<string | null>(null);

  const [yeniEkipAdi, setYeniEkipAdi] = useState("");
  const [yeniEkipBirimId, setYeniEkipBirimId] = useState("");

  const yeniden_yukle = useCallback(async () => {
    try {
      const [ekipSonucu, birimSonucu, kategoriSonucu, personelSonucu] = await Promise.all([
        EkiplerApi.listele(),
        BirimlerApi.listele(),
        GorevKategorileriApi.listele(),
        PersonellerApi.listele(),
      ]);
      setEkipler(ekipSonucu);
      setBirimler(birimSonucu);
      setKategoriler(kategoriSonucu);
      setPersoneller(personelSonucu);
      if (birimSonucu.length > 0 && !yeniEkipBirimId) setYeniEkipBirimId(birimSonucu[0].id);
    } catch (hata) {
      setHataMesaji(hata instanceof ApiHatasi ? hata.message : "Veriler yüklenemedi.");
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    void (async () => {
      await yeniden_yukle();
    })();
  }, [yeniden_yukle]);

  async function ekipOlustur() {
    if (!yeniEkipAdi.trim() || !yeniEkipBirimId) return;
    try {
      await EkiplerApi.olustur(yeniEkipBirimId, yeniEkipAdi.trim());
      setYeniEkipAdi("");
      await yeniden_yukle();
    } catch (hata) {
      setHataMesaji(hata instanceof ApiHatasi ? hata.message : "Ekip oluşturulamadı.");
    }
  }

  return (
    <div className="mx-auto max-w-4xl p-6">
      <h1 className="mb-4 text-xl font-bold text-white">Ekipler</h1>

      {hataMesaji && (
        <div className="mb-4 rounded-lg border border-red-500/40 bg-red-950/40 px-4 py-2 text-sm text-red-300">
          {hataMesaji}
        </div>
      )}

      <div className="mb-6 flex items-end gap-3 rounded-xl bg-yuzey p-4">
        <div className="flex-1">
          <label className="text-xs text-sonuc">Ekip Adı</label>
          <input
            value={yeniEkipAdi}
            onChange={(olay) => setYeniEkipAdi(olay.target.value)}
            className="mt-1 w-full rounded-lg border border-kenarlik bg-background px-3 py-2 text-sm text-white outline-none"
            placeholder="Örn. Ekip-3"
          />
        </div>
        <div>
          <label className="text-xs text-sonuc">Birim</label>
          <select
            value={yeniEkipBirimId}
            onChange={(olay) => setYeniEkipBirimId(olay.target.value)}
            className="mt-1 rounded-lg border border-kenarlik bg-background px-3 py-2 text-sm text-white outline-none"
          >
            {birimler.map((birim) => (
              <option key={birim.id} value={birim.id}>
                {birim.ad}
              </option>
            ))}
          </select>
        </div>
        <button
          onClick={() => void ekipOlustur()}
          className="rounded-lg bg-vurgu px-4 py-2 text-sm font-semibold text-white hover:brightness-110"
        >
          Ekip Oluştur
        </button>
      </div>

      <div className="space-y-3">
        {ekipler.map((ekip) => (
          <EkipKarti
            key={ekip.id}
            ekip={ekip}
            acikMi={acikEkipId === ekip.id}
            personeller={personeller.filter((p) => p.birimId === ekip.birimId && p.rol === "SahaPersoneli")}
            kategoriler={kategoriler}
            onTikla={() => setAcikEkipId(acikEkipId === ekip.id ? null : ekip.id)}
            onGuncellendi={yeniden_yukle}
            onHata={setHataMesaji}
          />
        ))}
      </div>
    </div>
  );
}

function EkipKarti({
  ekip,
  acikMi,
  personeller,
  kategoriler,
  onTikla,
  onGuncellendi,
  onHata,
}: {
  ekip: EkipYaniti;
  acikMi: boolean;
  personeller: PersonelYaniti[];
  kategoriler: GorevKategorisiYaniti[];
  onTikla: () => void;
  onGuncellendi: () => Promise<void>;
  onHata: (mesaj: string) => void;
}) {
  const [secilenPersonelId, setSecilenPersonelId] = useState("");
  const [secilenKategoriId, setSecilenKategoriId] = useState("");

  async function calistir(islem: () => Promise<unknown>) {
    try {
      await islem();
      await onGuncellendi();
    } catch (hata) {
      onHata(hata instanceof ApiHatasi ? hata.message : "İşlem başarısız oldu.");
    }
  }

  return (
    <div className="rounded-xl bg-yuzey">
      <button onClick={onTikla} className="flex w-full items-center justify-between px-4 py-3 text-left">
        <div>
          <p className="font-semibold text-white">{ekip.ad}</p>
          <p className="text-xs text-sonuc">
            {ekip.birimAdi} · {ekip.uyeler.length} üye · {ekip.uzmanlikAlanlari.length} yetkinlik
          </p>
        </div>
        <div className="flex items-center gap-3">
          <span className={`text-xs font-semibold ${ekip.aktifMi ? "text-emerald-400" : "text-sonuc"}`}>
            {ekip.aktifMi ? "Aktif" : "Pasif"}
          </span>
          <button
            onClick={(olay) => {
              olay.stopPropagation();
              void calistir(() => EkiplerApi.durumGuncelle(ekip.id, !ekip.aktifMi));
            }}
            className="rounded border border-kenarlik px-2 py-1 text-xs text-white hover:bg-background"
          >
            {ekip.aktifMi ? "Pasifleştir" : "Aktifleştir"}
          </button>
        </div>
      </button>

      {acikMi && (
        <div className="border-t border-kenarlik px-4 py-4 text-sm">
          <div className="grid grid-cols-2 gap-4">
            <div>
              <p className="mb-2 font-medium text-white">Üyeler</p>
              <ul className="space-y-1">
                {ekip.uyeler.map((uye) => (
                  <li key={uye.personelId} className="flex items-center justify-between text-sonuc">
                    <span>{uye.adSoyad}</span>
                    <button
                      onClick={() => void calistir(() => EkiplerApi.uyeCikar(ekip.id, uye.personelId))}
                      className="text-xs text-red-400 hover:underline"
                    >
                      Çıkar
                    </button>
                  </li>
                ))}
                {ekip.uyeler.length === 0 && <li className="text-sonuc">Henüz üye yok.</li>}
              </ul>
              <div className="mt-2 flex gap-2">
                <select
                  value={secilenPersonelId}
                  onChange={(olay) => setSecilenPersonelId(olay.target.value)}
                  className="flex-1 rounded border border-kenarlik bg-background px-2 py-1 text-xs text-white"
                >
                  <option value="">Personel seç...</option>
                  {personeller
                    .filter((p) => !ekip.uyeler.some((u) => u.personelId === p.id))
                    .map((p) => (
                      <option key={p.id} value={p.id}>
                        {p.adSoyad}
                      </option>
                    ))}
                </select>
                <button
                  onClick={() =>
                    secilenPersonelId &&
                    void calistir(() => EkiplerApi.uyeEkle(ekip.id, secilenPersonelId)).then(() => setSecilenPersonelId(""))
                  }
                  className="rounded border border-kenarlik px-2 py-1 text-xs text-white hover:bg-background"
                >
                  Ekle
                </button>
              </div>
            </div>

            <div>
              <p className="mb-2 font-medium text-white">Yetkinlik Alanları</p>
              <ul className="space-y-1">
                {ekip.uzmanlikAlanlari.map((alan) => (
                  <li key={alan.gorevKategorisiId} className="flex items-center justify-between text-sonuc">
                    <span>{alan.ad}</span>
                    <button
                      onClick={() => void calistir(() => EkiplerApi.uzmanlikCikar(ekip.id, alan.gorevKategorisiId))}
                      className="text-xs text-red-400 hover:underline"
                    >
                      Çıkar
                    </button>
                  </li>
                ))}
                {ekip.uzmanlikAlanlari.length === 0 && <li className="text-sonuc">Henüz yetkinlik eklenmedi.</li>}
              </ul>
              <div className="mt-2 flex gap-2">
                <select
                  value={secilenKategoriId}
                  onChange={(olay) => setSecilenKategoriId(olay.target.value)}
                  className="flex-1 rounded border border-kenarlik bg-background px-2 py-1 text-xs text-white"
                >
                  <option value="">Kategori seç...</option>
                  {kategoriler
                    .filter((k) => !ekip.uzmanlikAlanlari.some((a) => a.gorevKategorisiId === k.id))
                    .map((k) => (
                      <option key={k.id} value={k.id}>
                        {k.ad}
                      </option>
                    ))}
                </select>
                <button
                  onClick={() =>
                    secilenKategoriId &&
                    void calistir(() => EkiplerApi.uzmanlikEkle(ekip.id, secilenKategoriId)).then(() =>
                      setSecilenKategoriId(""),
                    )
                  }
                  className="rounded border border-kenarlik px-2 py-1 text-xs text-white hover:bg-background"
                >
                  Ekle
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
