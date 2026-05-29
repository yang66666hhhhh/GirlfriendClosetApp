using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClosetApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOutfitOriginalClothingCountAndClothingDetailsSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClothingDetailsSnapshot",
                table: "OutfitWornRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginalClothingCount",
                table: "Outfits",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClothingDetailsSnapshot",
                table: "OutfitWornRecords");

            migrationBuilder.DropColumn(
                name: "OriginalClothingCount",
                table: "Outfits");
        }
    }
}
