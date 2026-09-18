import AsyncStorage from "@react-native-async-storage/async-storage";

/**
 * Giris yapmis kullanicinin gizli olmayan profil bilgileri (SecureStore'daki tokenlarin
 * aksine). Uygulama yeniden acildiginda "hos geldiniz" bilgisini aginca beklemeden
 * gosterebilmek icin AsyncStorage'da tutulur.
 */
export interface OturumKullanicisi {
  personelId: string;
  adSoyad: string;
  rol: string;
  ekipId: string | null;
}

const OTURUM_KULLANICISI_ANAHTARI = "sahagor.oturumKullanicisi";

export const OturumDepolama = {
  async kaydet(kullanici: OturumKullanicisi): Promise<void> {
    await AsyncStorage.setItem(OTURUM_KULLANICISI_ANAHTARI, JSON.stringify(kullanici));
  },

  async getir(): Promise<OturumKullanicisi | null> {
    const deger = await AsyncStorage.getItem(OTURUM_KULLANICISI_ANAHTARI);
    return deger ? (JSON.parse(deger) as OturumKullanicisi) : null;
  },

  async temizle(): Promise<void> {
    await AsyncStorage.removeItem(OTURUM_KULLANICISI_ANAHTARI);
  },
};
