import { useCallback, useEffect, useState } from "react";
import { ActivityIndicator, FlatList, Pressable, RefreshControl, StyleSheet, Text, View } from "react-native";
import { useSQLiteContext } from "expo-sqlite";
import NetInfo from "@react-native-community/netinfo";
import type { NativeStackScreenProps } from "@react-navigation/native-stack";
import { useKimlik } from "../baglam/KimlikBaglami";
import { GorevDepolama } from "../depolama/gorevDepolama";
import { SenkronKuyruguDepolama } from "../depolama/senkronKuyruguDepolama";
import type { KokYiginParametreleri } from "../navigasyon/KokNavigasyon";
import { ekipKonumunuBildir } from "../servisler/ekipKonumServisi";
import { bekleyenFotograflariYukle } from "../servisler/fotografYuklemeServisi";
import { atanmisGorevleriYukle } from "../servisler/gorevServisi";
import { gorevIslemiBaslat, kuyruguIsle } from "../servisler/senkronMotoru";
import type { GorevOzeti } from "../tipler/gorev";
import type { GorevIslemTuru } from "../tipler/senkron";

type Props = NativeStackScreenProps<KokYiginParametreleri, "GorevListesi">;

/**
 * Saha personelinin atanmis gorevlerini gosteren ana ekran (SG-201, SG-202, SG-203).
 * Gorevler once yerel SQLite onbelleginden gosterilir, arka planda sunucudan tazelenir;
 * durum degisiklikleri (yola cik/basla) offline dahi calisir ve bir senkron kuyruguna girer.
 */
export function GorevListesiEkrani({ navigation }: Props) {
  const db = useSQLiteContext();
  const { kullanici, cikisYap } = useKimlik();

  const [gorevler, setGorevler] = useState<GorevOzeti[]>([]);
  const [bekleyenGorevIdleri, setBekleyenGorevIdleri] = useState<Set<string>>(new Set());
  const [yukleniyorMu, setYukleniyorMu] = useState(true);
  const [yenileniyorMu, setYenileniyorMu] = useState(false);
  const [cevrimici, setCevrimici] = useState(true);

  /** Ag cagrisi yapmadan, sadece yerel SQLite'tan okuyup ekrani gunceller (islem sonrasi aninda yansima icin). */
  const yerelListeyiTazele = useCallback(async () => {
    const kuyrukOgeleri = await SenkronKuyruguDepolama.hepsiniGetir(db);
    setBekleyenGorevIdleri(new Set(kuyrukOgeleri.map((oge) => oge.gorevId)));
    setGorevler(await GorevDepolama.hepsiniGetir(db));
  }, [db]);

  const listeyiYukle = useCallback(
    async (gosterge: (yukleniyor: boolean) => void) => {
      if (!kullanici?.ekipId) {
        setYukleniyorMu(false);
        return;
      }

      gosterge(true);
      try {
        // Once bekleyen offline islemleri ve fotograflari gondermeyi dene; boylece
        // sunucudan cekilecek "guncel" liste, bu cihazdan az once yapilan degisiklikleri
        // de yansitir (SG-203, SG-212).
        await kuyruguIsle(db);
        await bekleyenFotograflariYukle(db);

        const sonuc = await atanmisGorevleriYukle(db, kullanici.ekipId);
        setGorevler(sonuc.gorevler);
        setCevrimici(sonuc.kaynakCevrimici);

        const kuyrukOgeleri = await SenkronKuyruguDepolama.hepsiniGetir(db);
        setBekleyenGorevIdleri(new Set(kuyrukOgeleri.map((oge) => oge.gorevId)));
      } finally {
        gosterge(false);
      }
    },
    [db, kullanici?.ekipId],
  );

  useEffect(() => {
    void listeyiYukle(setYukleniyorMu);
  }, [listeyiYukle]);

  // Baglanti geri geldiginde bekleyen kuyrugu otomatik olarak islemeye calis (SG-203).
  useEffect(() => {
    const kaydi = NetInfo.addEventListener((durum) => {
      if (durum.isConnected) {
        void (async () => {
          await kuyruguIsle(db);
          await bekleyenFotograflariYukle(db);
          await yerelListeyiTazele();
        })();
      }
    });

    return () => kaydi();
  }, [db, yerelListeyiTazele]);

  const islemBaslat = useCallback(
    async (gorevId: string, islemTuru: GorevIslemTuru) => {
      await gorevIslemiBaslat(db, gorevId, islemTuru);
      await yerelListeyiTazele();

      // "Yola cik", ekibin fiilen harekete gectigi andir; bu bilgiyi komuta panelinin
      // canli haritasina yansitmak icin ekip konumu da bildirilir (SG-301, SG-302).
      // Best-effort: basarisiz olsa dahi gorev akisini engellemez.
      if (islemTuru === "YOLA_CIK" && kullanici?.ekipId) {
        void ekipKonumunuBildir(kullanici.ekipId);
      }
    },
    [db, yerelListeyiTazele, kullanici?.ekipId],
  );

  if (!kullanici?.ekipId) {
    return (
      <View style={stiller.bosKapsayici}>
        <Text style={stiller.bosMetin}>Henüz bir ekibe atanmamışsınız. Gösterilecek görev bulunmuyor.</Text>
        <Pressable style={stiller.cikisButonu} onPress={() => void cikisYap()}>
          <Text style={stiller.cikisMetni}>Çıkış Yap</Text>
        </Pressable>
      </View>
    );
  }

  return (
    <View style={stiller.kapsayici}>
      <View style={stiller.ustBilgi}>
        <View>
          <Text style={stiller.hosGeldin}>{kullanici.adSoyad}</Text>
          <Text style={stiller.gorevSayisi}>{gorevler.length} görev</Text>
        </View>
        <Pressable onPress={() => void cikisYap()}>
          <Text style={stiller.cikisLinki}>Çıkış</Text>
        </Pressable>
      </View>

      {!cevrimici && (
        <View style={stiller.cevrimdisiBanner}>
          <Text style={stiller.cevrimdisiMetni}>Çevrimdışı — son bilinen görevler gösteriliyor</Text>
        </View>
      )}

      {yukleniyorMu ? (
        <View style={stiller.bosKapsayici}>
          <Text style={stiller.bosMetin}>Görevler yükleniyor...</Text>
        </View>
      ) : (
        <FlatList
          data={gorevler}
          keyExtractor={(gorev) => gorev.id}
          contentContainerStyle={stiller.liste}
          refreshControl={
            <RefreshControl refreshing={yenileniyorMu} onRefresh={() => void listeyiYukle(setYenileniyorMu)} />
          }
          ListEmptyComponent={
            <View style={stiller.bosKapsayici}>
              <Text style={stiller.bosMetin}>Şu an size atanmış görev bulunmuyor.</Text>
            </View>
          }
          renderItem={({ item }) => (
            <GorevKarti
              gorev={item}
              senkronBekliyorMu={bekleyenGorevIdleri.has(item.id)}
              onIslemBaslat={(tur) => void islemBaslat(item.id, tur)}
              onKartaTikla={() => navigation.navigate("GorevDetay", { gorevId: item.id })}
            />
          )}
        />
      )}
    </View>
  );
}

function GorevKarti({
  gorev,
  senkronBekliyorMu,
  onIslemBaslat,
  onKartaTikla,
}: {
  gorev: GorevOzeti;
  senkronBekliyorMu: boolean;
  onIslemBaslat: (islemTuru: GorevIslemTuru) => void;
  onKartaTikla: () => void;
}) {
  return (
    <Pressable style={stiller.kart} onPress={onKartaTikla}>
      <View style={stiller.kartUstSatir}>
        <Text style={stiller.kartBaslik} numberOfLines={2}>
          {gorev.baslik}
        </Text>
        {gorev.slaIhlalEdildiMi && (
          <View style={stiller.slaRozeti}>
            <Text style={stiller.slaRozetiMetni}>SLA AŞILDI</Text>
          </View>
        )}
      </View>
      <Text style={stiller.kartAltMetin}>{gorev.kategoriAdi}</Text>
      {gorev.bolgeAdi && <Text style={stiller.kartAltMetin}>{gorev.bolgeAdi}</Text>}

      <View style={stiller.altSatir}>
        <View style={stiller.durumRozeti}>
          <Text style={stiller.durumRozetiMetni}>{gorev.durum}</Text>
        </View>
        {senkronBekliyorMu && (
          <View style={stiller.senkronRozeti}>
            <ActivityIndicator size={10} color="#fef08a" />
            <Text style={stiller.senkronRozetiMetni}>Senkronize bekliyor</Text>
          </View>
        )}
      </View>

      {gorev.durum === "Atandi" && (
        <Pressable style={stiller.islemButonu} onPress={() => onIslemBaslat("YOLA_CIK")}>
          <Text style={stiller.islemButonuMetni}>Yola Çık</Text>
        </Pressable>
      )}
      {gorev.durum === "YolaCikildi" && (
        <Pressable style={stiller.islemButonu} onPress={() => onIslemBaslat("BASLA")}>
          <Text style={stiller.islemButonuMetni}>İşleme Başla</Text>
        </Pressable>
      )}
    </Pressable>
  );
}

const stiller = StyleSheet.create({
  kapsayici: {
    flex: 1,
    backgroundColor: "#0f172a",
  },
  ustBilgi: {
    flexDirection: "row",
    justifyContent: "space-between",
    alignItems: "center",
    paddingHorizontal: 20,
    paddingTop: 16,
    paddingBottom: 12,
  },
  hosGeldin: {
    color: "#ffffff",
    fontSize: 18,
    fontWeight: "700",
  },
  gorevSayisi: {
    color: "#94a3b8",
    fontSize: 13,
    marginTop: 2,
  },
  cikisLinki: {
    color: "#ef4444",
    fontWeight: "600",
  },
  cevrimdisiBanner: {
    backgroundColor: "#78350f",
    paddingVertical: 8,
    paddingHorizontal: 20,
  },
  cevrimdisiMetni: {
    color: "#fef3c7",
    fontSize: 13,
    textAlign: "center",
  },
  liste: {
    padding: 16,
    gap: 12,
  },
  kart: {
    backgroundColor: "#1e293b",
    borderRadius: 12,
    padding: 16,
    gap: 6,
  },
  kartUstSatir: {
    flexDirection: "row",
    justifyContent: "space-between",
    gap: 8,
  },
  kartBaslik: {
    color: "#ffffff",
    fontSize: 16,
    fontWeight: "600",
    flex: 1,
  },
  kartAltMetin: {
    color: "#94a3b8",
    fontSize: 13,
  },
  slaRozeti: {
    backgroundColor: "#7f1d1d",
    borderRadius: 6,
    paddingHorizontal: 8,
    paddingVertical: 3,
    alignSelf: "flex-start",
  },
  slaRozetiMetni: {
    color: "#fecaca",
    fontSize: 10,
    fontWeight: "700",
  },
  altSatir: {
    flexDirection: "row",
    alignItems: "center",
    gap: 8,
    marginTop: 4,
  },
  durumRozeti: {
    backgroundColor: "#1d4ed8",
    borderRadius: 6,
    paddingHorizontal: 8,
    paddingVertical: 3,
    alignSelf: "flex-start",
  },
  durumRozetiMetni: {
    color: "#dbeafe",
    fontSize: 11,
    fontWeight: "600",
  },
  senkronRozeti: {
    flexDirection: "row",
    alignItems: "center",
    gap: 4,
    backgroundColor: "#78350f",
    borderRadius: 6,
    paddingHorizontal: 8,
    paddingVertical: 3,
  },
  senkronRozetiMetni: {
    color: "#fef08a",
    fontSize: 10,
    fontWeight: "600",
  },
  islemButonu: {
    backgroundColor: "#2563eb",
    borderRadius: 8,
    paddingVertical: 10,
    alignItems: "center",
    marginTop: 8,
  },
  islemButonuMetni: {
    color: "#ffffff",
    fontWeight: "600",
    fontSize: 14,
  },
  bosKapsayici: {
    flex: 1,
    alignItems: "center",
    justifyContent: "center",
    padding: 32,
    gap: 16,
  },
  bosMetin: {
    color: "#94a3b8",
    textAlign: "center",
    fontSize: 14,
  },
  cikisButonu: {
    borderWidth: 1,
    borderColor: "#ef4444",
    borderRadius: 10,
    paddingHorizontal: 24,
    paddingVertical: 12,
  },
  cikisMetni: {
    color: "#ef4444",
    fontWeight: "600",
  },
});
