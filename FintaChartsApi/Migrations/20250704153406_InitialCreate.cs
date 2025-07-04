using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FintaChartsApi.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bars",
                columns: table => new
                {
                    InstrumentId = table.Column<string>(type: "text", nullable: false),
                    Resolution = table.Column<string>(type: "text", nullable: false),
                    T = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    O = table.Column<decimal>(type: "numeric(18,9)", nullable: false),
                    H = table.Column<decimal>(type: "numeric(18,9)", nullable: false),
                    L = table.Column<decimal>(type: "numeric(18,9)", nullable: false),
                    C = table.Column<decimal>(type: "numeric(18,9)", nullable: false),
                    V = table.Column<decimal>(type: "numeric(18,9)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bars", x => new { x.InstrumentId, x.Resolution, x.T });
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bars");
        }
    }
}
