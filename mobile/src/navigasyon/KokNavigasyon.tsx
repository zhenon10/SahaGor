import { NavigationContainer } from "@react-navigation/native";
import { createNativeStackNavigator } from "@react-navigation/native-stack";
import { ActivityIndicator, View } from "react-native";
import { useKimlik } from "../baglam/KimlikBaglami";
import { GirisEkrani } from "../ekranlar/GirisEkrani";
import { GorevDetayEkrani } from "../ekranlar/GorevDetayEkrani";
import { GorevListesiEkrani } from "../ekranlar/GorevListesiEkrani";
import { KameraEkrani } from "../ekranlar/KameraEkrani";

export type KokYiginParametreleri = {
  Giris: undefined;
  GorevListesi: undefined;
  GorevDetay: { gorevId: string };
  Kamera: { gorevId: string };
};

const Yigin = createNativeStackNavigator<KokYiginParametreleri>();

/**
 * Oturum durumuna gore Giris/Ana ekranlari arasinda gecis yapan kok navigasyon.
 * Kimlik dogrulama tamamlanana kadar (SecureStore okunurken) bir yukleme gostergesi gosterilir;
 * boylece kisa bir an icin yanlislikla Giris ekrani gorunup hemen Ana ekrana atlamaz (flicker).
 */
export function KokNavigasyon() {
  const { kullanici, baslangicKontroluTamamlandiMi } = useKimlik();

  if (!baslangicKontroluTamamlandiMi) {
    return (
      <View style={{ flex: 1, alignItems: "center", justifyContent: "center", backgroundColor: "#0f172a" }}>
        <ActivityIndicator size="large" color="#2563eb" />
      </View>
    );
  }

  return (
    <NavigationContainer>
      <Yigin.Navigator screenOptions={{ headerShown: false }}>
        {kullanici ? (
          <>
            <Yigin.Screen name="GorevListesi" component={GorevListesiEkrani} />
            <Yigin.Screen name="GorevDetay" component={GorevDetayEkrani} />
            <Yigin.Screen name="Kamera" component={KameraEkrani} options={{ presentation: "fullScreenModal" }} />
          </>
        ) : (
          <Yigin.Screen name="Giris" component={GirisEkrani} />
        )}
      </Yigin.Navigator>
    </NavigationContainer>
  );
}
