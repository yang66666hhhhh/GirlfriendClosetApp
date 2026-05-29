using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClosetApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutfitWornRecordSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutfitWornRecords_Outfits_OutfitId",
                table: "OutfitWornRecords");

            migrationBuilder.AlterColumn<Guid>(
                name: "OutfitId",
                table: "OutfitWornRecords",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "OutfitNameSnapshot",
                table: "OutfitWornRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PreviewSnapshotPath",
                table: "OutfitWornRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OutfitWornRecords_Outfits_OutfitId",
                table: "OutfitWornRecords",
                column: "OutfitId",
                principalTable: "Outfits",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_OutfitWornRecords_Outfits_OutfitId",
                table: "OutfitWornRecords");

            migrationBuilder.DropColumn(
                name: "OutfitNameSnapshot",
                table: "OutfitWornRecords");

            migrationBuilder.DropColumn(
                name: "PreviewSnapshotPath",
                table: "OutfitWornRecords");

            migrationBuilder.AlterColumn<Guid>(
                name: "OutfitId",
                table: "OutfitWornRecords",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_OutfitWornRecords_Outfits_OutfitId",
                table: "OutfitWornRecords",
                column: "OutfitId",
                principalTable: "Outfits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
