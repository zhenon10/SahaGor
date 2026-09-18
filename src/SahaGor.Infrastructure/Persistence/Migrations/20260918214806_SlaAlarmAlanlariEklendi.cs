using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SahaGor.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SlaAlarmAlanlariEklendi : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "SlaIhlalAlarmiZamaniUtc",
                table: "gorev_talepleri",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaYaklasmaAlarmiZamaniUtc",
                table: "gorev_talepleri",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SlaIhlalAlarmiZamaniUtc",
                table: "gorev_talepleri");

            migrationBuilder.DropColumn(
                name: "SlaYaklasmaAlarmiZamaniUtc",
                table: "gorev_talepleri");
        }
    }
}
