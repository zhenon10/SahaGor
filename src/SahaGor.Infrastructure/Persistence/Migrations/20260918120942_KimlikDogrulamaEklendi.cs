using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SahaGor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class KimlikDogrulamaEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BasarisizGirisSayisi",
                table: "personeller",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "KilitlenmeBitisZamaniUtc",
                table: "personeller",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "yenileme_jetonlari",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PersonelId = table.Column<Guid>(type: "uuid", nullable: false),
                    Token = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SonKullanmaZamaniUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IptalEdildiMi = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_yenileme_jetonlari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_yenileme_jetonlari_personeller_PersonelId",
                        column: x => x.PersonelId,
                        principalTable: "personeller",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_yenileme_jetonlari_PersonelId",
                table: "yenileme_jetonlari",
                column: "PersonelId");

            migrationBuilder.CreateIndex(
                name: "IX_yenileme_jetonlari_SilindiMi",
                table: "yenileme_jetonlari",
                column: "SilindiMi");

            migrationBuilder.CreateIndex(
                name: "IX_yenileme_jetonlari_Token",
                table: "yenileme_jetonlari",
                column: "Token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "yenileme_jetonlari");

            migrationBuilder.DropColumn(
                name: "BasarisizGirisSayisi",
                table: "personeller");

            migrationBuilder.DropColumn(
                name: "KilitlenmeBitisZamaniUtc",
                table: "personeller");
        }
    }
}
