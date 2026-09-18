import type { SQLiteDatabase } from "expo-sqlite";

/**
 * Uygulama ilk acildiginda (SQLiteProvider'in onInit'i araciligiyla) bir kez calisir.
 * "CREATE TABLE IF NOT EXISTS" kullanildigi icin tekrar tekrar cagrilmasi guvenlidir.
 *
 * Not: SQLite'ta yerlesik bir BOOLEAN tipi yoktur; slaIhlalEdildiMi 0/1 INTEGER olarak saklanir.
 */
export async function veritabaniSemasiniOlustur(db: SQLiteDatabase): Promise<void> {
  await db.execAsync(`
    PRAGMA journal_mode = WAL;

    CREATE TABLE IF NOT EXISTS gorevler (
      id TEXT PRIMARY KEY NOT NULL,
      baslik TEXT NOT NULL,
      kategoriAdi TEXT NOT NULL,
      durum TEXT NOT NULL,
      oncelik TEXT NOT NULL,
      enlem REAL NOT NULL,
      boylam REAL NOT NULL,
      bolgeAdi TEXT,
      atananEkipAdi TEXT,
      olusturulmaZamaniUtc TEXT NOT NULL,
      slaHedefZamaniUtc TEXT NOT NULL,
      slaIhlalEdildiMi INTEGER NOT NULL
    );

    -- Offline durum degisikliklerinin "cikis kutusu" (outbox). AUTOINCREMENT ile artan
    -- birincil anahtar, ekleme sirasini garanti eder; senkron motoru bu sirayla (FIFO)
    -- isler (SG-203 kabul kriteri).
    CREATE TABLE IF NOT EXISTS senkron_kuyrugu (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      gorevId TEXT NOT NULL,
      islemTuru TEXT NOT NULL,
      olusturulmaZamaniUtc TEXT NOT NULL,
      denemeSayisi INTEGER NOT NULL DEFAULT 0,
      sonHataMesaji TEXT
    );

    -- Uygulama ici kamera ile cekilmis kanit fotograflari (SG-210, SG-211). "yuklendiMi"
    -- alani, baglanti geldiginde arka planda sunucuya gonderilmeyi bekleyen fotograflari
    -- isaretler (SG-212); basariyla yuklenen bir fotograf bu tabloda kalir (yerel gecmis
    -- olarak), sadece bayragi 1'e cekilir.
    CREATE TABLE IF NOT EXISTS yerel_fotograflar (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      gorevId TEXT NOT NULL,
      dosyaYolu TEXT NOT NULL,
      asama TEXT NOT NULL DEFAULT 'Sonra',
      enlem REAL NOT NULL,
      boylam REAL NOT NULL,
      cekilmeZamaniUtc TEXT NOT NULL,
      yuklendiMi INTEGER NOT NULL DEFAULT 0
    );
  `);
}
