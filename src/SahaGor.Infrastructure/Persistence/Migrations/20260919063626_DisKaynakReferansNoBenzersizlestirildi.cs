using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SahaGor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DisKaynakReferansNoBenzersizlestirildi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_gorev_talepleri_DisKaynakReferansNo",
                table: "gorev_talepleri",
                column: "DisKaynakReferansNo",
                unique: true,
                filter: "\"DisKaynakReferansNo\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_gorev_talepleri_DisKaynakReferansNo",
                table: "gorev_talepleri");
        }
    }
}
