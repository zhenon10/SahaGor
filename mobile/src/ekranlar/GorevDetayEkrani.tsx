import { useCallback, useState } from "react";
import { FlatList, Image, Pressable, StyleSheet, Text, View } from "react-native";
import { useFocusEffect } from "@react-navigation/native";
import { useSQLiteContext } from "expo-sqlite";
import type { NativeStackScreenProps } from "@react-navigation/native-stack";
import type { KokYiginParametreleri } from "../navigasyon/KokNavigasyon";
import { GorevDepolama } from "../depolama/gorevDepolama";
import { FotografDepolama } from "../depolama/fotografDepolama";
import { bekleyenFotograflariYukle } from "../servisler/fotografYuklemeServisi";
import type { GorevOzeti } from "../tipler/gorev";
import type { YerelFotograf } from "../tipler/fotograf";

type Props = NativeStackScreenProps<KokYiginParametreleri, "GorevDetay">;

/** Kanit fotografi eklenebilecek durumlar: gorev uzerinde fiilen calisiliyor olmali. */
const FOTOGRAF_EKLENEBILIR_DURUMLAR = new Set(["YolaCikildi", "Baslandi"]);

export function GorevDetayEkrani({ route, navigation }: Props) {
  const { gorevId } = route.params;
  const db = useSQLiteContext();

  const [gorev, setGorev] = useState<GorevOzeti | null>(null);
  const [fotograflar, setFotograflar] = useState<YerelFotograf[]>([]);

  // Kamera ekranindan "goBack" ile donuldugunde yeni cekilen fotografin listede
  // gorunmesi icin, ekran her odaklandiginda (focus) yerel veriler yeniden okunur.
  // Ayrica cihaz online ise bekleyen fotograflarin yuklenmesi hemen denenir (SG-212);
  // boylece kullanici uygulamayi kapatip actiginda dahi yukleme otomatik ilerler.
  useFocusEffect(
    useCallback(() => {
      void (async () => {
        await bekleyenFotograflariYukle(db);
        setGorev(await GorevDepolama.idIleGetir(db, gorevId));
        setFotograflar(await FotografDepolama.gorevIcinGetir(db, gorevId));
      })();
    }, [db, gorevId]),
  );

  if (!gorev) {
    return (
      <View style={stiller.kapsayici}>
        <Text style={stiller.bosMetin}>Görev bulunamadı.</Text>
      </View>
    );
  }

  const fotografEklenebilirMi = FOTOGRAF_EKLENEBILIR_DURUMLAR.has(gorev.durum);

  return (
    <View style={stiller.kapsayici}>
      <View style={stiller.ustBar}>
        <Pressable onPress={() => navigation.goBack()}>
          <Text style={stiller.geriMetni}>‹ Geri</Text>
        </Pressable>
      </View>

      <View style={stiller.icerik}>
        <Text style={stiller.baslik}>{gorev.baslik}</Text>
        <Text style={stiller.altMetin}>{gorev.kategoriAdi}</Text>
        {gorev.bolgeAdi && <Text style={stiller.altMetin}>{gorev.bolgeAdi}</Text>}

        <View style={stiller.durumRozeti}>
          <Text style={stiller.durumRozetiMetni}>{gorev.durum}</Text>
        </View>

        <Text style={stiller.bolumBasligi}>Kanıt Fotoğrafları ({fotograflar.length})</Text>

        {fotograflar.length === 0 ? (
          <Text style={stiller.bosMetin}>Henüz fotoğraf eklenmedi.</Text>
        ) : (
          <FlatList
            data={fotograflar}
            horizontal
            keyExtractor={(fotograf) => String(fotograf.id)}
            contentContainerStyle={stiller.fotografListesi}
            renderItem={({ item }) => (
              <View style={stiller.fotografKapsayici}>
                <Image source={{ uri: item.dosyaYolu }} style={stiller.fotografKucukResmi} />
                {!item.yuklendiMi && (
                  <View style={stiller.yuklemeBekliyorRozeti}>
                    <Text style={stiller.yuklemeBekliyorMetni}>Yüklenmedi</Text>
                  </View>
                )}
              </View>
            )}
          />
        )}

        {fotografEklenebilirMi ? (
          <Pressable
            style={stiller.fotografEkleButonu}
            onPress={() => navigation.navigate("Kamera", { gorevId })}
          >
            <Text style={stiller.fotografEkleButonuMetni}>📷 Fotoğraf Ekle</Text>
          </Pressable>
        ) : (
          <Text style={stiller.bosMetin}>
            Fotoğraf eklemek için önce "Yola Çık" ve "İşleme Başla" adımlarını tamamlayın.
          </Text>
        )}
      </View>
    </View>
  );
}

const stiller = StyleSheet.create({
  kapsayici: {
    flex: 1,
    backgroundColor: "#0f172a",
  },
  ustBar: {
    paddingHorizontal: 20,
    paddingTop: 16,
    paddingBottom: 8,
  },
  geriMetni: {
    color: "#94a3b8",
    fontSize: 15,
  },
  icerik: {
    paddingHorizontal: 20,
    gap: 8,
  },
  baslik: {
    color: "#ffffff",
    fontSize: 22,
    fontWeight: "700",
  },
  altMetin: {
    color: "#94a3b8",
    fontSize: 14,
  },
  durumRozeti: {
    backgroundColor: "#1d4ed8",
    borderRadius: 6,
    paddingHorizontal: 10,
    paddingVertical: 4,
    alignSelf: "flex-start",
    marginTop: 4,
  },
  durumRozetiMetni: {
    color: "#dbeafe",
    fontSize: 12,
    fontWeight: "600",
  },
  bolumBasligi: {
    color: "#ffffff",
    fontSize: 16,
    fontWeight: "600",
    marginTop: 20,
  },
  fotografListesi: {
    gap: 10,
    paddingVertical: 8,
  },
  fotografKapsayici: {
    position: "relative",
  },
  fotografKucukResmi: {
    width: 96,
    height: 96,
    borderRadius: 10,
    backgroundColor: "#1e293b",
  },
  yuklemeBekliyorRozeti: {
    position: "absolute",
    bottom: 4,
    left: 4,
    right: 4,
    backgroundColor: "rgba(120, 53, 15, 0.9)",
    borderRadius: 4,
    paddingVertical: 2,
  },
  yuklemeBekliyorMetni: {
    color: "#fef3c7",
    fontSize: 9,
    textAlign: "center",
    fontWeight: "600",
  },
  fotografEkleButonu: {
    backgroundColor: "#2563eb",
    borderRadius: 10,
    paddingVertical: 14,
    alignItems: "center",
    marginTop: 24,
  },
  fotografEkleButonuMetni: {
    color: "#ffffff",
    fontWeight: "600",
    fontSize: 15,
  },
  bosMetin: {
    color: "#94a3b8",
    fontSize: 13,
    marginTop: 8,
  },
});
