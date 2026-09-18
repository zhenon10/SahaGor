import { createContext, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { KimlikApi } from "../api/kimlikApi";
import { PersonellerApi } from "../api/personellerApi";
import { GuvenliDepolama } from "../depolama/guvenliDepolama";
import { OturumDepolama, type OturumKullanicisi } from "../depolama/oturumDepolama";
import { ApiHatasi } from "../tipler/api";

interface KimlikBaglamiDegeri {
  /** null: giris yapilmamis. undefined: uygulama acilista henuz kontrol ediyor. */
  kullanici: OturumKullanicisi | null;
  baslangicKontroluTamamlandiMi: boolean;
  girisYapiliyorMu: boolean;
  girisYap: (kullaniciAdi: string, sifre: string) => Promise<void>;
  cikisYap: () => Promise<void>;
}

const KimlikBaglami = createContext<KimlikBaglamiDegeri | undefined>(undefined);

export function KimlikSaglayici({ children }: { children: ReactNode }) {
  const [kullanici, setKullanici] = useState<OturumKullanicisi | null>(null);
  const [baslangicKontroluTamamlandiMi, setBaslangicKontroluTamamlandiMi] = useState(false);
  const [girisYapiliyorMu, setGirisYapiliyorMu] = useState(false);

  useEffect(() => {
    let iptalEdildiMi = false;

    async function oturumuGeriYukle() {
      const [erisimTokeni, kayitliKullanici] = await Promise.all([
        GuvenliDepolama.erisimTokeniGetir(),
        OturumDepolama.getir(),
      ]);

      if (!iptalEdildiMi && erisimTokeni && kayitliKullanici) {
        setKullanici(kayitliKullanici);
      }

      if (!iptalEdildiMi) {
        setBaslangicKontroluTamamlandiMi(true);
      }
    }

    void oturumuGeriYukle();

    return () => {
      iptalEdildiMi = true;
    };
  }, []);

  const deger = useMemo<KimlikBaglamiDegeri>(
    () => ({
      kullanici,
      baslangicKontroluTamamlandiMi,
      girisYapiliyorMu,

      async girisYap(kullaniciAdi: string, sifre: string) {
        setGirisYapiliyorMu(true);
        try {
          const yanit = await KimlikApi.girisYap({ kullaniciAdi, sifre });

          // Tokenlar, personel detayi cagrisindan ONCE kaydedilmeli; aksi halde
          // apiIstemcisi bu istege Authorization header'i ekleyemez ve 401 doner.
          await GuvenliDepolama.tokenCiftiKaydet(yanit.erisimTokeni, yanit.yenilemeTokeni);

          // GirisYaniti icinde EkipId yer almaz (SG-120 sozlesmesi minimaldir);
          // gorev listesini "atananEkipId" ile filtreleyebilmek icin ayrica sorgulanir.
          const personelDetayi = await PersonellerApi.benimBilgilerimGetir(yanit.personelId);

          const yeniKullanici: OturumKullanicisi = {
            personelId: yanit.personelId,
            adSoyad: yanit.adSoyad,
            rol: yanit.rol,
            ekipId: personelDetayi.ekipId,
          };
          await OturumDepolama.kaydet(yeniKullanici);

          setKullanici(yeniKullanici);
        } catch (hata) {
          if (hata instanceof ApiHatasi) {
            throw hata;
          }
          throw new ApiHatasi("Sunucuya baglanilamadi. Internet baglantinizi kontrol edin.", 0);
        } finally {
          setGirisYapiliyorMu(false);
        }
      },

      async cikisYap() {
        await Promise.all([GuvenliDepolama.tokenleriTemizle(), OturumDepolama.temizle()]);
        setKullanici(null);
      },
    }),
    [kullanici, baslangicKontroluTamamlandiMi, girisYapiliyorMu],
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
