/**
 * Uygulama genelinde kullanilan ortam degiskenleri. Expo SDK 49+ "EXPO_PUBLIC_" onekli
 * degiskenleri derleme zamaninda otomatik olarak process.env'e gomer; ek bir paket
 * (react-native-dotenv vb.) gerekmez.
 */

const apiTabanAdresi = process.env.EXPO_PUBLIC_API_BASE_URL;

if (!apiTabanAdresi) {
  throw new Error(
    "EXPO_PUBLIC_API_BASE_URL tanimli degil. Proje kokunde bir '.env' veya '.env.local' " +
      "dosyasi olusturup bu degeri tanimlayin (bkz. .env.example).",
  );
}

export const Ortam = {
  apiTabanAdresi,
} as const;
