-- SahaGor veritabani icin gerekli PostGIS uzantilarini aktif eder.
-- postgis/postgis imaji "template_postgis" adinda hazir bir sablon icerir fakat
-- POSTGRES_DB ortam degiskeni ile olusturulan veritabani "template1" tabanli oldugundan
-- uzantilar bu script ile acikca etkinlestirilir.

-- Temel PostGIS fonksiyonlari (geometry/geography tipleri, ST_* fonksiyonlari).
CREATE EXTENSION IF NOT EXISTS postgis;

-- Adres/rota gibi topolojik iliskiler icin (ileride kullanilabilir, simdiden hazir bulunsun).
CREATE EXTENSION IF NOT EXISTS postgis_topology;

-- UUID tabanli birincil anahtar uretimi icin (gen_random_uuid).
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- Kurulumun basarili oldugunu build/CI loglarinda dogrulamak icin bilgi mesaji.
DO $$
BEGIN
    RAISE NOTICE 'SahaGor: PostGIS surumu -> %', PostGIS_version();
END $$;
