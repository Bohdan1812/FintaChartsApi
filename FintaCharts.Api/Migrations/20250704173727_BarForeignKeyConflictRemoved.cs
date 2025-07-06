using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FintaChartsApi.Migrations
{
    /// <inheritdoc />
    public partial class BarForeignKeyConflictRemoved : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "InstrumentId",
                table: "Bars",
                type: "character varying(36)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<string>(
                name: "ProviderId",
                table: "Bars",
                type: "character varying(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderId1",
                table: "Bars",
                type: "character varying(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Instruments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    Symbol = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Currency = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BaseCurrency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    TickSize = table.Column<decimal>(type: "numeric(18,9)", nullable: true),
                    Exchange = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Instruments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Providers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Providers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bars_ProviderId",
                table: "Bars",
                column: "ProviderId");

            migrationBuilder.CreateIndex(
                name: "IX_Bars_ProviderId1",
                table: "Bars",
                column: "ProviderId1");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_Id",
                table: "Instruments",
                column: "Id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_Symbol",
                table: "Instruments",
                column: "Symbol",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Providers_Id",
                table: "Providers",
                column: "Id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bars_Instruments_InstrumentId",
                table: "Bars",
                column: "InstrumentId",
                principalTable: "Instruments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Bars_Providers_ProviderId",
                table: "Bars",
                column: "ProviderId",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Bars_Providers_ProviderId1",
                table: "Bars",
                column: "ProviderId1",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bars_Instruments_InstrumentId",
                table: "Bars");

            migrationBuilder.DropForeignKey(
                name: "FK_Bars_Providers_ProviderId",
                table: "Bars");

            migrationBuilder.DropForeignKey(
                name: "FK_Bars_Providers_ProviderId1",
                table: "Bars");

            migrationBuilder.DropTable(
                name: "Instruments");

            migrationBuilder.DropTable(
                name: "Providers");

            migrationBuilder.DropIndex(
                name: "IX_Bars_ProviderId",
                table: "Bars");

            migrationBuilder.DropIndex(
                name: "IX_Bars_ProviderId1",
                table: "Bars");

            migrationBuilder.DropColumn(
                name: "ProviderId",
                table: "Bars");

            migrationBuilder.DropColumn(
                name: "ProviderId1",
                table: "Bars");

            migrationBuilder.AlterColumn<string>(
                name: "InstrumentId",
                table: "Bars",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(36)");
        }
    }
}
