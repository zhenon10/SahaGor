import { useEffect, useRef, useState } from "react";
import { ActivityIndicator, Alert, Pressable, StyleSheet, Text, View } from "react-native";
import { CameraView, useCameraPermissions } from "expo-camera";
import * as Location from "expo-location";
import { useSQLiteContext } from "expo-sqlite";
import type { NativeStackScreenProps } from "@react-navigation/native-stack";
import type { KokYiginParametreleri } from "../navigasyon/KokNavigasyon";
import { kanitFotografiniKaydet } from "../servisler/fotografServisi";

type Props = NativeStackScreenProps<KokYiginParametreleri, "Kamera">;

type KonumIzniDurumu = "kontrolEdiliyor" | "verildi" | "reddedildi";

/**
 * Uygulama ici, tek amacli kanit fotografi kamerasi (SG-210). Bilerek expo-image-picker
 * KULLANILMAZ ve projeye eklenmez; boylece galeriden fotograf secme/yukleme hicbir
 * kod yolunda mumkun degildir - tek giris noktasi canli kamera onizlemesidir.
 *
 * Konum izni verilmeden kamera hic gosterilmez (AC: "GPS izni reddedilirse fotograf
 * cekimi engellenir ve kullaniciya Turkce uyari gosterilir").
 */
export function KameraEkrani({ route, navigation }: Props) {
  const { gorevId } = route.params;
  const db = useSQLiteContext();
  const kameraRef = useRef<CameraView>(null);

  const [kameraIzni, kameraIzniIste] = useCameraPermissions();
  const [konumIzniDurumu, setKonumIzniDurumu] = useState<KonumIzniDurumu>("kontrolEdiliyor");
  const [cekimYapiliyorMu, setCekimYapiliyorMu] = useState(false);

  useEffect(() => {
    let iptalEdildiMi = false;

    void (async () => {
      const { status } = await Location.requestForegroundPermissionsAsync();
      if (!iptalEdildiMi) {
        setKonumIzniDurumu(status === "granted" ? "verildi" : "reddedildi");
      }
    })();

    return () => {
      iptalEdildiMi = true;
    };
  }, []);

  async function fotografCek() {
    if (!kameraRef.current || cekimYapiliyorMu) {
      return;
    }

    setCekimYapiliyorMu(true);
    try {
      const cekim = await kameraRef.current.takePictureAsync({ quality: 0.7 });
      if (!cekim) {
        throw new Error("Kamera bir fotograf dondurmedi.");
      }

      await kanitFotografiniKaydet(db, gorevId, cekim);
      navigation.goBack();
    } catch {
      Alert.alert("Fotoğraf kaydedilemedi", "Bir sorun oluştu. Lütfen tekrar deneyin.");
    } finally {
      setCekimYapiliyorMu(false);
    }
  }

  if (!kameraIzni || konumIzniDurumu === "kontrolEdiliyor") {
    return (
      <View style={stiller.ortaliKapsayici}>
        <ActivityIndicator size="large" color="#2563eb" />
      </View>
    );
  }

  if (!kameraIzni.granted) {
    return (
      <View style={stiller.ortaliKapsayici}>
        <Text style={stiller.izinMetni}>
          Kanıt fotoğrafı çekebilmek için kamera erişimine izin vermeniz gerekiyor.
        </Text>
        <Pressable style={stiller.izinButonu} onPress={() => void kameraIzniIste()}>
          <Text style={stiller.izinButonuMetni}>Kameraya İzin Ver</Text>
        </Pressable>
        <Pressable onPress={() => navigation.goBack()}>
          <Text style={stiller.vazgecMetni}>Vazgeç</Text>
        </Pressable>
      </View>
    );
  }

  if (konumIzniDurumu === "reddedildi") {
    return (
      <View style={stiller.ortaliKapsayici}>
        <Text style={stiller.izinMetni}>
          Konum izni olmadan kanıt fotoğrafı çekilemez. Fotoğrafın çekildiği yerin doğrulanabilmesi
          için konum bilgisi zorunludur. Lütfen cihaz ayarlarından SahaGör için konum iznini açın.
        </Text>
        <Pressable onPress={() => navigation.goBack()}>
          <Text style={stiller.vazgecMetni}>Geri Dön</Text>
        </Pressable>
      </View>
    );
  }

  return (
    <View style={stiller.kapsayici}>
      <CameraView ref={kameraRef} style={stiller.kamera} facing="back" />

      <View style={stiller.altBar}>
        <Pressable onPress={() => navigation.goBack()} disabled={cekimYapiliyorMu}>
          <Text style={stiller.iptalMetni}>İptal</Text>
        </Pressable>

        <Pressable
          style={[stiller.cekimButonu, cekimYapiliyorMu && stiller.cekimButonuPasif]}
          onPress={() => void fotografCek()}
          disabled={cekimYapiliyorMu}
        >
          {cekimYapiliyorMu && <ActivityIndicator color="#0f172a" />}
        </Pressable>

        <View style={{ width: 48 }} />
      </View>
    </View>
  );
}

const stiller = StyleSheet.create({
  kapsayici: {
    flex: 1,
    backgroundColor: "#000000",
  },
  kamera: {
    flex: 1,
  },
  altBar: {
    flexDirection: "row",
    alignItems: "center",
    justifyContent: "space-between",
    paddingHorizontal: 32,
    paddingVertical: 24,
    backgroundColor: "#000000",
  },
  iptalMetni: {
    color: "#ffffff",
    fontSize: 15,
    width: 48,
  },
  cekimButonu: {
    width: 72,
    height: 72,
    borderRadius: 36,
    backgroundColor: "#ffffff",
    alignItems: "center",
    justifyContent: "center",
    borderWidth: 4,
    borderColor: "#64748b",
  },
  cekimButonuPasif: {
    opacity: 0.6,
  },
  ortaliKapsayici: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    backgroundColor: "#0f172a",
    padding: 32,
    gap: 16,
  },
  izinMetni: {
    color: "#e2e8f0",
    fontSize: 15,
    textAlign: "center",
    lineHeight: 22,
  },
  izinButonu: {
    backgroundColor: "#2563eb",
    borderRadius: 10,
    paddingHorizontal: 24,
    paddingVertical: 12,
  },
  izinButonuMetni: {
    color: "#ffffff",
    fontWeight: "600",
  },
  vazgecMetni: {
    color: "#94a3b8",
    fontSize: 14,
  },
});
