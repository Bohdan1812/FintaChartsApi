using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FintaChartsApi.Migrations
{
    /// <inheritdoc />
    public partial class removeBarsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bars_Providers_ProviderId1",
                table: "Bars");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Bars",
                table: "Bars");

            migrationBuilder.DropIndex(
                name: "IX_Bars_ProviderId1",
                table: "Bars");

            migrationBuilder.DropColumn(
                name: "Resolution",
                table: "Bars");

            migrationBuilder.DropColumn(
                name: "ProviderId1",
                table: "Bars");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Bars",
                table: "Bars",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Bars_InstrumentId",
                table: "Bars",
                column: "InstrumentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Bars",
                table: "Bars");

            migrationBuilder.DropIndex(
                name: "IX_Bars_InstrumentId",
                table: "Bars");

            migrationBuilder.AddColumn<string>(
                name: "Resolution",
                table: "Bars",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ProviderId1",
                table: "Bars",
                type: "character varying(50)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Bars",
                table: "Bars",
                columns: new[] { "InstrumentId", "Resolution", "T" });

            migrationBuilder.CreateIndex(
                name: "IX_Bars_ProviderId1",
                table: "Bars",
                column: "ProviderId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Bars_Providers_ProviderId1",
                table: "Bars",
                column: "ProviderId1",
                principalTable: "Providers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
