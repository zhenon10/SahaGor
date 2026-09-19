using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SahaGor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class YenilemeJetonuHashlensin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Mevcut jetonlar duz metin olarak saklanmisti; hash'e geri donusturulemezler.
            // Guvenlik duzeltmesinin dogasi geregi zaten tum acik oturumlarin yenileme
            // jetonlarinin gecersiz sayilmasi beklenir (kullanicilar bir sonraki erisim
            // tokeni suresi dolduğunda tekrar giris yapar). Ayrica bu satirlar silinmeden
            // yeni UNIQUE "TokenHash" kolonu eklenirse, birden fazla mevcut kayit ayni bos
            // varsayilan degeri alip benzersizlik kisitini ihlal ederdi.
            migrationBuilder.Sql("DELETE FROM yenileme_jetonlari;");

            migrationBuilder.DropIndex(
                name: "IX_yenileme_jetonlari_Token",
                table: "yenileme_jetonlari");

            migrationBuilder.DropColumn(
                name: "Token",
                table: "yenileme_jetonlari");

            migrationBuilder.AddColumn<string>(
                name: "TokenHash",
                table: "yenileme_jetonlari",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_yenileme_jetonlari_TokenHash",
                table: "yenileme_jetonlari",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_yenileme_jetonlari_TokenHash",
                table: "yenileme_jetonlari");

            migrationBuilder.DropColumn(
                name: "TokenHash",
                table: "yenileme_jetonlari");

            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "yenileme_jetonlari",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_yenileme_jetonlari_Token",
                table: "yenileme_jetonlari",
                column: "Token",
                unique: true);
        }
    }
}
