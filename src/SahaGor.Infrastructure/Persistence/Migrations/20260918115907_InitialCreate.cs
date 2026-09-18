using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace SahaGor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "gorev_kategorileri",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    SlaYanitSuresiDakika = table.Column<int>(type: "integer", nullable: false),
                    SlaCozumSuresiDakika = table.Column<int>(type: "integer", nullable: false),
                    AktifMi = table.Column<bool>(type: "boolean", nullable: false),
                    OlusturmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OlusturanKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuncellemeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuncelleyenKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    SilindiMi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SilinmeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gorev_kategorileri", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "kurumlar",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Adres = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IletisimTelefonu = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AktifMi = table.Column<bool>(type: "boolean", nullable: false),
                    OlusturmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OlusturanKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuncellemeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuncelleyenKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    SilindiMi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SilinmeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kurumlar", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "birimler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KurumId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AktifMi = table.Column<bool>(type: "boolean", nullable: false),
                    OlusturmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OlusturanKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuncellemeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuncelleyenKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    SilindiMi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SilinmeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_birimler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_birimler_kurumlar_KurumId",
                        column: x => x.KurumId,
                        principalTable: "kurumlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "bolgeler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KurumId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SinirPolygonu = table.Column<Polygon>(type: "geography (Polygon,4326)", nullable: false),
                    SorumluBirimId = table.Column<Guid>(type: "uuid", nullable: true),
                    OlusturmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OlusturanKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuncellemeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuncelleyenKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    SilindiMi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SilinmeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bolgeler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_bolgeler_birimler_SorumluBirimId",
                        column: x => x.SorumluBirimId,
                        principalTable: "birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_bolgeler_kurumlar_KurumId",
                        column: x => x.KurumId,
                        principalTable: "kurumlar",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ekipler",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BirimId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GuncelKonum = table.Column<Point>(type: "geography (Point,4326)", nullable: true),
                    KonumGuncellenmeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AktifMi = table.Column<bool>(type: "boolean", nullable: false),
                    OlusturmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OlusturanKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuncellemeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuncelleyenKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    SilindiMi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SilinmeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ekipler", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ekipler_birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ekip_uzmanlik_alanlari",
                columns: table => new
                {
                    EkipId = table.Column<Guid>(type: "uuid", nullable: false),
                    UzmanlikAlanlariId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ekip_uzmanlik_alanlari", x => new { x.EkipId, x.UzmanlikAlanlariId });
                    table.ForeignKey(
                        name: "FK_ekip_uzmanlik_alanlari_ekipler_EkipId",
                        column: x => x.EkipId,
                        principalTable: "ekipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ekip_uzmanlik_alanlari_gorev_kategorileri_UzmanlikAlanlariId",
                        column: x => x.UzmanlikAlanlariId,
                        principalTable: "gorev_kategorileri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "personeller",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BirimId = table.Column<Guid>(type: "uuid", nullable: false),
                    EkipId = table.Column<Guid>(type: "uuid", nullable: true),
                    AdSoyad = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    KullaniciAdi = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SifreHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Telefon = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Eposta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Rol = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    AktifMi = table.Column<bool>(type: "boolean", nullable: false),
                    MusaitMi = table.Column<bool>(type: "boolean", nullable: false),
                    OlusturmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OlusturanKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuncellemeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuncelleyenKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    SilindiMi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SilinmeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_personeller", x => x.Id);
                    table.ForeignKey(
                        name: "FK_personeller_birimler_BirimId",
                        column: x => x.BirimId,
                        principalTable: "birimler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_personeller_ekipler_EkipId",
                        column: x => x.EkipId,
                        principalTable: "ekipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "gorev_talepleri",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Baslik = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Aciklama = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    KategoriId = table.Column<Guid>(type: "uuid", nullable: false),
                    Konum = table.Column<Point>(type: "geography (Point,4326)", nullable: false),
                    BolgeId = table.Column<Guid>(type: "uuid", nullable: true),
                    Durum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Oncelik = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Kaynak = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    DisKaynakReferansNo = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BildirenTelefonNumarasi = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    AtananEkipId = table.Column<Guid>(type: "uuid", nullable: true),
                    AtananPersonelId = table.Column<Guid>(type: "uuid", nullable: true),
                    OlusturulmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SlaHedefZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AtanmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    YolaCikmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BaslamaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TamamlanmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DogrulanmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IptalZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OlusturmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OlusturanKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuncellemeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuncelleyenKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    SilindiMi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SilinmeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gorev_talepleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gorev_talepleri_bolgeler_BolgeId",
                        column: x => x.BolgeId,
                        principalTable: "bolgeler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_gorev_talepleri_ekipler_AtananEkipId",
                        column: x => x.AtananEkipId,
                        principalTable: "ekipler",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_gorev_talepleri_gorev_kategorileri_KategoriId",
                        column: x => x.KategoriId,
                        principalTable: "gorev_kategorileri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gorev_talepleri_personeller_AtananPersonelId",
                        column: x => x.AtananPersonelId,
                        principalTable: "personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "gorev_durum_gecmisleri",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GorevTalebiId = table.Column<Guid>(type: "uuid", nullable: false),
                    OncekiDurum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    YeniDurum = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    IslemiYapanPersonelId = table.Column<Guid>(type: "uuid", nullable: true),
                    Not = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DegisiklikZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OlusturmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OlusturanKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuncellemeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuncelleyenKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    SilindiMi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SilinmeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gorev_durum_gecmisleri", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gorev_durum_gecmisleri_gorev_talepleri_GorevTalebiId",
                        column: x => x.GorevTalebiId,
                        principalTable: "gorev_talepleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gorev_durum_gecmisleri_personeller_IslemiYapanPersonelId",
                        column: x => x.IslemiYapanPersonelId,
                        principalTable: "personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "gorev_fotograflari",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GorevTalebiId = table.Column<Guid>(type: "uuid", nullable: false),
                    DosyaYolu = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Asama = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CekildigiKonum = table.Column<Point>(type: "geography (Point,4326)", nullable: false),
                    CekilmeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SunucuyaUlasmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CekenPersonelId = table.Column<Guid>(type: "uuid", nullable: false),
                    OlusturmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OlusturanKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    GuncellemeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GuncelleyenKullaniciId = table.Column<Guid>(type: "uuid", nullable: true),
                    SilindiMi = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    SilinmeZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gorev_fotograflari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gorev_fotograflari_gorev_talepleri_GorevTalebiId",
                        column: x => x.GorevTalebiId,
                        principalTable: "gorev_talepleri",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gorev_fotograflari_personeller_CekenPersonelId",
                        column: x => x.CekenPersonelId,
                        principalTable: "personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_birimler_KurumId_Ad",
                table: "birimler",
                columns: new[] { "KurumId", "Ad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_birimler_SilindiMi",
                table: "birimler",
                column: "SilindiMi");

            migrationBuilder.CreateIndex(
                name: "IX_bolgeler_KurumId",
                table: "bolgeler",
                column: "KurumId");

            migrationBuilder.CreateIndex(
                name: "IX_bolgeler_SilindiMi",
                table: "bolgeler",
                column: "SilindiMi");

            migrationBuilder.CreateIndex(
                name: "IX_bolgeler_SinirPolygonu",
                table: "bolgeler",
                column: "SinirPolygonu")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_bolgeler_SorumluBirimId",
                table: "bolgeler",
                column: "SorumluBirimId");

            migrationBuilder.CreateIndex(
                name: "IX_ekip_uzmanlik_alanlari_UzmanlikAlanlariId",
                table: "ekip_uzmanlik_alanlari",
                column: "UzmanlikAlanlariId");

            migrationBuilder.CreateIndex(
                name: "IX_ekipler_BirimId_Ad",
                table: "ekipler",
                columns: new[] { "BirimId", "Ad" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ekipler_GuncelKonum",
                table: "ekipler",
                column: "GuncelKonum")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_ekipler_SilindiMi",
                table: "ekipler",
                column: "SilindiMi");

            migrationBuilder.CreateIndex(
                name: "IX_gorev_durum_gecmisleri_GorevTalebiId",
                table: "gorev_durum_gecmisleri",
                column: "GorevTalebiId");

            migrationBuilder.CreateIndex(
                name: "IX_gorev_durum_gecmisleri_IslemiYapanPersonelId",
                table: "gorev_durum_gecmisleri",
                column: "IslemiYapanPersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_gorev_durum_gecmisleri_SilindiMi",
                table: "gorev_durum_gecmisleri",
                column: "SilindiMi");

            migrationBuilder.CreateIndex(
                name: "IX_gorev_fotograflari_CekenPersonelId",
                table: "gorev_fotograflari",
                column: "CekenPersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_gorev_fotograflari_GorevTalebiId",
                table: "gorev_fotograflari",
                column: "GorevTalebiId");

            migrationBuilder.CreateIndex(
                name: "IX_gorev_fotograflari_SilindiMi",
                table: "gorev_fotograflari",
                column: "SilindiMi");

            migrationBuilder.CreateIndex(
                name: "IX_gorev_kategorileri_Ad",
                table: "gorev_kategorileri",
                column: "Ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gorev_kategorileri_SilindiMi",
                table: "gorev_kategorileri",
                column: "SilindiMi");

            migrationBuilder.CreateIndex(
                name: "IX_gorev_talepleri_AtananEkipId",
                table: "gorev_talepleri",
                column: "AtananEkipId");

            migrationBuilder.CreateIndex(
                name: "IX_gorev_talepleri_AtananPersonelId",
                table: "gorev_talepleri",
                column: "AtananPersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_gorev_talepleri_BolgeId",
                table: "gorev_talepleri",
                column: "BolgeId");

            migrationBuilder.CreateIndex(
                name: "IX_gorev_talepleri_Durum",
                table: "gorev_talepleri",
                column: "Durum");

            migrationBuilder.CreateIndex(
                name: "IX_gorev_talepleri_KategoriId",
                table: "gorev_talepleri",
                column: "KategoriId");

            migrationBuilder.CreateIndex(
                name: "IX_gorev_talepleri_Konum",
                table: "gorev_talepleri",
                column: "Konum")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_gorev_talepleri_SilindiMi",
                table: "gorev_talepleri",
                column: "SilindiMi");

            migrationBuilder.CreateIndex(
                name: "IX_gorev_talepleri_SlaHedefZamaniUtc",
                table: "gorev_talepleri",
                column: "SlaHedefZamaniUtc");

            migrationBuilder.CreateIndex(
                name: "IX_kurumlar_Ad",
                table: "kurumlar",
                column: "Ad",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_kurumlar_SilindiMi",
                table: "kurumlar",
                column: "SilindiMi");

            migrationBuilder.CreateIndex(
                name: "IX_personeller_BirimId",
                table: "personeller",
                column: "BirimId");

            migrationBuilder.CreateIndex(
                name: "IX_personeller_EkipId",
                table: "personeller",
                column: "EkipId");

            migrationBuilder.CreateIndex(
                name: "IX_personeller_KullaniciAdi",
                table: "personeller",
                column: "KullaniciAdi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_personeller_SilindiMi",
                table: "personeller",
                column: "SilindiMi");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ekip_uzmanlik_alanlari");

            migrationBuilder.DropTable(
                name: "gorev_durum_gecmisleri");

            migrationBuilder.DropTable(
                name: "gorev_fotograflari");

            migrationBuilder.DropTable(
                name: "gorev_talepleri");

            migrationBuilder.DropTable(
                name: "bolgeler");

            migrationBuilder.DropTable(
                name: "gorev_kategorileri");

            migrationBuilder.DropTable(
                name: "personeller");

            migrationBuilder.DropTable(
                name: "ekipler");

            migrationBuilder.DropTable(
                name: "birimler");

            migrationBuilder.DropTable(
                name: "kurumlar");
        }
    }
}
