using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FintaChartsApi.Migrations
{
    /// <inheritdoc />
    public partial class removedBarTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bars");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bars",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InstrumentId = table.Column<string>(type: "character varying(36)", nullable: false),
                    ProviderId = table.Column<string>(type: "character varying(50)", nullable: false),
                    C = table.Column<decimal>(type: "numeric(18,9)", nullable: false),
                    H = table.Column<decimal>(type: "numeric(18,9)", nullable: false),
                    L = table.Column<decimal>(type: "numeric(18,9)", nullable: false),
                    O = table.Column<decimal>(type: "numeric(18,9)", nullable: false),
                    T = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    V = table.Column<decimal>(type: "numeric(18,9)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bars", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bars_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bars_Providers_ProviderId",
                        column: x => x.ProviderId,
                        principalTable: "Providers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bars_InstrumentId",
                table: "Bars",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_Bars_ProviderId",
                table: "Bars",
                column: "ProviderId");
        }
    }
}
