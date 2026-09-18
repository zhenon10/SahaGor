import { useState } from "react";
import {
  ActivityIndicator,
  Alert,
  KeyboardAvoidingView,
  Platform,
  Pressable,
  StyleSheet,
  Text,
  TextInput,
  View,
} from "react-native";
import { useKimlik } from "../baglam/KimlikBaglami";
import { ApiHatasi } from "../tipler/api";

export function GirisEkrani() {
  const { girisYap, girisYapiliyorMu } = useKimlik();
  const [kullaniciAdi, setKullaniciAdi] = useState("");
  const [sifre, setSifre] = useState("");

  async function girisDenemesiYap() {
    if (!kullaniciAdi.trim() || !sifre) {
      Alert.alert("Eksik bilgi", "Kullanici adi ve sifre alanlari zorunludur.");
      return;
    }

    try {
      await girisYap(kullaniciAdi.trim(), sifre);
    } catch (hata) {
      const mesaj = hata instanceof ApiHatasi ? hata.message : "Beklenmeyen bir hata olustu.";
      Alert.alert("Giris basarisiz", mesaj);
    }
  }

  return (
    <KeyboardAvoidingView
      style={stiller.kapsayici}
      behavior={Platform.OS === "ios" ? "padding" : undefined}
    >
      <View style={stiller.form}>
        <Text style={stiller.baslik}>SahaGör</Text>
        <Text style={stiller.altBaslik}>Saha personeli girişi</Text>

        <TextInput
          style={stiller.girdi}
          placeholder="Kullanıcı adı"
          autoCapitalize="none"
          autoCorrect={false}
          value={kullaniciAdi}
          onChangeText={setKullaniciAdi}
          editable={!girisYapiliyorMu}
        />

        <TextInput
          style={stiller.girdi}
          placeholder="Şifre"
          secureTextEntry
          value={sifre}
          onChangeText={setSifre}
          editable={!girisYapiliyorMu}
          onSubmitEditing={girisDenemesiYap}
        />

        <Pressable
          style={({ pressed }) => [stiller.buton, pressed && stiller.butonBasili]}
          onPress={girisDenemesiYap}
          disabled={girisYapiliyorMu}
        >
          {girisYapiliyorMu ? (
            <ActivityIndicator color="#ffffff" />
          ) : (
            <Text style={stiller.butonMetni}>Giriş Yap</Text>
          )}
        </Pressable>
      </View>
    </KeyboardAvoidingView>
  );
}

const stiller = StyleSheet.create({
  kapsayici: {
    flex: 1,
    backgroundColor: "#0f172a",
    justifyContent: "center",
  },
  form: {
    paddingHorizontal: 24,
    gap: 12,
  },
  baslik: {
    fontSize: 32,
    fontWeight: "700",
    color: "#ffffff",
    textAlign: "center",
  },
  altBaslik: {
    fontSize: 15,
    color: "#94a3b8",
    textAlign: "center",
    marginBottom: 24,
  },
  girdi: {
    backgroundColor: "#1e293b",
    color: "#ffffff",
    borderRadius: 10,
    paddingHorizontal: 16,
    paddingVertical: 14,
    fontSize: 16,
  },
  buton: {
    backgroundColor: "#2563eb",
    borderRadius: 10,
    paddingVertical: 14,
    alignItems: "center",
    marginTop: 8,
  },
  butonBasili: {
    opacity: 0.8,
  },
  butonMetni: {
    color: "#ffffff",
    fontSize: 16,
    fontWeight: "600",
  },
});
