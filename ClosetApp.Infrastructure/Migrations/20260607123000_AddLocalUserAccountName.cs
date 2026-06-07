using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClosetApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalUserAccountName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccountName",
                table: "LocalUsers",
                type: "TEXT",
                nullable: false,
                defaultValue: "admin");

            migrationBuilder.CreateIndex(
                name: "IX_LocalUsers_AccountName",
                table: "LocalUsers",
                column: "AccountName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LocalUsers_AccountName",
                table: "LocalUsers");

            migrationBuilder.DropColumn(
                name: "AccountName",
                table: "LocalUsers");
        }
    }
}
