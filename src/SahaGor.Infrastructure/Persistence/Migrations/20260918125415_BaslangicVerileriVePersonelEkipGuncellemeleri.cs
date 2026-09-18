using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SahaGor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class BaslangicVerileriVePersonelEkipGuncellemeleri : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "gorev_kategorileri",
                columns: new[] { "Id", "Aciklama", "Ad", "AktifMi", "GuncellemeZamaniUtc", "GuncelleyenKullaniciId", "OlusturanKullaniciId", "OlusturmaZamaniUtc", "SilinmeZamaniUtc", "SlaCozumSuresiDakika", "SlaYanitSuresiDakika" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0a01-000000000001"), "Kaldırım, yol veya bordür hasarları.", "Kırık Kaldırım / Yol Bozukluğu", true, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 4320, 60 },
                    { new Guid("00000000-0000-0000-0a01-000000000002"), "Aydınlatma direği veya lamba arızaları.", "Sokak Lambası Arızası", true, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1440, 30 },
                    { new Guid("00000000-0000-0000-0a01-000000000003"), "Zamanında toplanmayan çöp/atık bildirimleri.", "Çöp Toplama Aksaklığı", true, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 480, 30 },
                    { new Guid("00000000-0000-0000-0a01-000000000004"), "Sokak hayvanı ile ilgili şikayet/yardım talebi.", "Başıboş Hayvan İhbarı", true, null, null, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 720, 30 }
                });

            migrationBuilder.InsertData(
                table: "kurumlar",
                columns: new[] { "Id", "Ad", "Adres", "AktifMi", "GuncellemeZamaniUtc", "GuncelleyenKullaniciId", "IletisimTelefonu", "OlusturanKullaniciId", "OlusturmaZamaniUtc", "SilinmeZamaniUtc" },
                values: new object[] { new Guid("00000000-0000-0000-0a02-000000000001"), "Örnek Belediyesi", "Örnek Mah. Belediye Cad. No:1", true, null, null, "0212 000 00 00", null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.InsertData(
                table: "birimler",
                columns: new[] { "Id", "Aciklama", "Ad", "AktifMi", "GuncellemeZamaniUtc", "GuncelleyenKullaniciId", "KurumId", "OlusturanKullaniciId", "OlusturmaZamaniUtc", "SilinmeZamaniUtc" },
                values: new object[] { new Guid("00000000-0000-0000-0a02-000000000002"), "Kurum, birim ve kullanici yonetimi.", "Sistem Yönetimi", true, null, null, new Guid("00000000-0000-0000-0a02-000000000001"), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.InsertData(
                table: "personeller",
                columns: new[] { "Id", "AdSoyad", "AktifMi", "BasarisizGirisSayisi", "BirimId", "EkipId", "Eposta", "GuncellemeZamaniUtc", "GuncelleyenKullaniciId", "KilitlenmeBitisZamaniUtc", "KullaniciAdi", "MusaitMi", "OlusturanKullaniciId", "OlusturmaZamaniUtc", "Rol", "SifreHash", "SilinmeZamaniUtc", "Telefon" },
                values: new object[] { new Guid("00000000-0000-0000-0a02-000000000003"), "Sistem Yöneticisi", true, 0, new Guid("00000000-0000-0000-0a02-000000000002"), null, null, null, null, null, "sistem.yoneticisi", true, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "SistemYoneticisi", "$2a$12$nAwKPKrugSehsxpFuJIIMuhqRs9dlar/YXWPs5twVFGfXBUkRYjIq", null, "0000000000" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "gorev_kategorileri",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a01-000000000001"));

            migrationBuilder.DeleteData(
                table: "gorev_kategorileri",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a01-000000000002"));

            migrationBuilder.DeleteData(
                table: "gorev_kategorileri",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a01-000000000003"));

            migrationBuilder.DeleteData(
                table: "gorev_kategorileri",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a01-000000000004"));

            migrationBuilder.DeleteData(
                table: "personeller",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000003"));

            migrationBuilder.DeleteData(
                table: "birimler",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000002"));

            migrationBuilder.DeleteData(
                table: "kurumlar",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0a02-000000000001"));
        }
    }
}
