import { Directory, File, Paths } from "expo-file-system";
import type { SQLiteDatabase } from "expo-sqlite";
import { FotografDepolama } from "../depolama/fotografDepolama";
import { anlikKonumAl } from "./konumServisi";

const KANIT_FOTOGRAFLARI_KLASORU = "kanit-fotograflari";

/**
 * Kamera ile cekilen gecici fotografi kalici depolamaya tasir, cekim anindaki GPS
 * konumu ve zaman damgasiyla birlikte yerel veritabanina kaydeder (SG-210, SG-211).
 *
 * Not: Konum/zaman bilgisi, JPEG dosyasinin kendi EXIF etiketlerine gomulmek yerine
 * uygulamanin kendi veri modelinde (SQLite, ileride backend'deki GorevFotografi
 * entity'sinin ayni alanlari) yapisal olarak tutulur. Bu, EXIF'in ucuncu parti
 * araclarla kolayca silinebilir/degistirilebilir olmasina kiyasla, sahtecilige karsi
 * daha guvenilir bir kayittir; cunku veri uygulamanin kendi kontrolundeki akistan gelir.
 */
export async function kanitFotografiniKaydet(
  db: SQLiteDatabase,
  gorevId: string,
  cekilenFotograf: { uri: string },
): Promise<void> {
  // KameraEkrani, konum izni verilmeden bu fonksiyonu hic cagirmaz; null donmesi
  // beklenmeyen bir durumdur (orn. izin kullanici tarafindan cekim sirasinda geri alindi).
  const konum = await anlikKonumAl();
  if (!konum) {
    throw new Error("Konum alinamadi. Kanit fotografi konum bilgisi olmadan kaydedilemez.");
  }

  const cekilmeZamaniUtc = new Date().toISOString();

  const hedefKlasor = new Directory(Paths.document, KANIT_FOTOGRAFLARI_KLASORU);
  hedefKlasor.create({ intermediates: true, idempotent: true });

  const dosyaAdi = `${gorevId}-${Date.now()}.jpg`;
  const kaynakDosya = new File(cekilenFotograf.uri);
  const hedefDosya = new File(hedefKlasor, dosyaAdi);

  // Kameranin gecici (cache) dizinindeki dosya, uygulama tarafindan kontrol edilen
  // kalici "document" dizinine kopyalanir; aksi halde isletim sistemi depolama
  // baskisi altinda cache'i temizleyip kanit fotografini sessizce silebilir.
  await kaynakDosya.copy(hedefDosya);

  await FotografDepolama.ekle(db, {
    gorevId,
    dosyaYolu: hedefDosya.uri,
    // Su an tek bir "Fotograf Ekle" butonu oldugundan asama sabit "Sonra" (sonuc kaniti)
    // olarak kaydedilir; once/sirasinda asamalarini secebilen bir UI ileride eklenebilir.
    asama: "Sonra",
    enlem: konum.enlem,
    boylam: konum.boylam,
    cekilmeZamaniUtc,
  });
}
