using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FintaChartsApi.Migrations
{
    /// <inheritdoc />
    public partial class AddedInstrumantPricesTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Providers_Id",
                table: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_Instruments_Id",
                table: "Instruments");

            migrationBuilder.DropColumn(
                name: "Exchange",
                table: "Instruments");

            migrationBuilder.CreateTable(
                name: "InstrumentPrices",
                columns: table => new
                {
                    InstrumentId = table.Column<string>(type: "character varying(36)", nullable: false),
                    ProviderId = table.Column<string>(type: "character varying(50)", nullable: false),
                    Ask = table.Column<decimal>(type: "numeric(18,8)", nullable: true),
                    Bid = table.Column<decimal>(type: "numeric(18,8)", nullable: true),
                    Last = table.Column<decimal>(type: "numeric(18,8)", nullable: true),
                    Volume = table.Column<long>(type: "bigint", nullable: true),
                    LastUpdated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InstrumentPrices", x => new { x.InstrumentId, x.ProviderId });
                    table.ForeignKey(
                        name: "FK_InstrumentPrices_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InstrumentPrices_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InstrumentPrices_ProviderId",
                table: "InstrumentPrices",
                column: "ProviderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InstrumentPrices");

            migrationBuilder.AddColumn<string>(
                name: "Exchange",
                table: "Instruments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Id",
                table: "Providers",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_Id",
                table: "Instruments",
                column: "Id",
                unique: true);
        }
    }
}
