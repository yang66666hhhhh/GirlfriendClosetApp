using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClosetApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutfitWornRecordClothingSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ClothingCountSnapshot",
                table: "OutfitWornRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "OutfitClothingIdsSnapshot",
                table: "OutfitWornRecords",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClothingCountSnapshot",
                table: "OutfitWornRecords");

            migrationBuilder.DropColumn(
                name: "OutfitClothingIdsSnapshot",
                table: "OutfitWornRecords");
        }
    }
}
