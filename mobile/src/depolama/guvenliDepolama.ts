import * as SecureStore from "expo-secure-store";

/**
 * JWT erisim/yenileme tokenlarini cihazin guvenli depolamasinda (iOS Keychain,
 * Android Keystore destekli EncryptedSharedPreferences) saklar. Duz metin
 * AsyncStorage yerine bilincli olarak SecureStore kullanilir; aksi halde
 * cihaza fiziksel erisimi olan biri tokenlari kolayca okuyabilirdi.
 */

const ERISIM_TOKENI_ANAHTARI = "sahagor.erisimTokeni";
const YENILEME_TOKENI_ANAHTARI = "sahagor.yenilemeTokeni";

export const GuvenliDepolama = {
  async tokenCiftiKaydet(erisimTokeni: string, yenilemeTokeni: string): Promise<void> {
    await Promise.all([
      SecureStore.setItemAsync(ERISIM_TOKENI_ANAHTARI, erisimTokeni),
      SecureStore.setItemAsync(YENILEME_TOKENI_ANAHTARI, yenilemeTokeni),
    ]);
  },

  async erisimTokeniGetir(): Promise<string | null> {
    return SecureStore.getItemAsync(ERISIM_TOKENI_ANAHTARI);
  },

  async yenilemeTokeniGetir(): Promise<string | null> {
    return SecureStore.getItemAsync(YENILEME_TOKENI_ANAHTARI);
  },

  async tokenleriTemizle(): Promise<void> {
    await Promise.all([
      SecureStore.deleteItemAsync(ERISIM_TOKENI_ANAHTARI),
      SecureStore.deleteItemAsync(YENILEME_TOKENI_ANAHTARI),
    ]);
  },
};
