using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClosetApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsSnapshotCompleteToOutfitWornRecord : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSnapshotComplete",
                table: "OutfitWornRecords",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSnapshotComplete",
                table: "OutfitWornRecords");
        }
    }
}
