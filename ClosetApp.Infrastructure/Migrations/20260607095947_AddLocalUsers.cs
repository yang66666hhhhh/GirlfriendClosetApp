using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClosetApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "LocalUserId",
                table: "Tags",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocalUserId",
                table: "PersonalProfiles",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocalUserId",
                table: "OutfitWornRecords",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocalUserId",
                table: "Outfits",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocalUserId",
                table: "OutfitGeneratedImages",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocalUserId",
                table: "Favorites",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LocalUserId",
                table: "Clothes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "LocalUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    AvatarPhotoPath = table.Column<string>(type: "TEXT", nullable: true),
                    Role = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    LinkedAccountId = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LocalUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_LocalUserId",
                table: "Tags",
                column: "LocalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonalProfiles_LocalUserId",
                table: "PersonalProfiles",
                column: "LocalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OutfitWornRecords_LocalUserId",
                table: "OutfitWornRecords",
                column: "LocalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Outfits_LocalUserId",
                table: "Outfits",
                column: "LocalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OutfitGeneratedImages_LocalUserId",
                table: "OutfitGeneratedImages",
                column: "LocalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_LocalUserId",
                table: "Favorites",
                column: "LocalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Clothes_LocalUserId",
                table: "Clothes",
                column: "LocalUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LocalUsers_Role",
                table: "LocalUsers",
                column: "Role");

            migrationBuilder.AddForeignKey(
                name: "FK_Clothes_LocalUsers_LocalUserId",
                table: "Clothes",
                column: "LocalUserId",
                principalTable: "LocalUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Favorites_LocalUsers_LocalUserId",
                table: "Favorites",
                column: "LocalUserId",
                principalTable: "LocalUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OutfitGeneratedImages_LocalUsers_LocalUserId",
                table: "OutfitGeneratedImages",
                column: "LocalUserId",
                principalTable: "LocalUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Outfits_LocalUsers_LocalUserId",
                table: "Outfits",
                column: "LocalUserId",
                principalTable: "LocalUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OutfitWornRecords_LocalUsers_LocalUserId",
                table: "OutfitWornRecords",
                column: "LocalUserId",
                principalTable: "LocalUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PersonalProfiles_LocalUsers_LocalUserId",
                table: "PersonalProfiles",
                column: "LocalUserId",
                principalTable: "LocalUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_LocalUsers_LocalUserId",
                table: "Tags",
                column: "LocalUserId",
                principalTable: "LocalUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clothes_LocalUsers_LocalUserId",
                table: "Clothes");

            migrationBuilder.DropForeignKey(
                name: "FK_Favorites_LocalUsers_LocalUserId",
                table: "Favorites");

            migrationBuilder.DropForeignKey(
                name: "FK_OutfitGeneratedImages_LocalUsers_LocalUserId",
                table: "OutfitGeneratedImages");

            migrationBuilder.DropForeignKey(
                name: "FK_Outfits_LocalUsers_LocalUserId",
                table: "Outfits");

            migrationBuilder.DropForeignKey(
                name: "FK_OutfitWornRecords_LocalUsers_LocalUserId",
                table: "OutfitWornRecords");

            migrationBuilder.DropForeignKey(
                name: "FK_PersonalProfiles_LocalUsers_LocalUserId",
                table: "PersonalProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_Tags_LocalUsers_LocalUserId",
                table: "Tags");

            migrationBuilder.DropTable(
                name: "LocalUsers");

            migrationBuilder.DropIndex(
                name: "IX_Tags_LocalUserId",
                table: "Tags");

            migrationBuilder.DropIndex(
                name: "IX_PersonalProfiles_LocalUserId",
                table: "PersonalProfiles");

            migrationBuilder.DropIndex(
                name: "IX_OutfitWornRecords_LocalUserId",
                table: "OutfitWornRecords");

            migrationBuilder.DropIndex(
                name: "IX_Outfits_LocalUserId",
                table: "Outfits");

            migrationBuilder.DropIndex(
                name: "IX_OutfitGeneratedImages_LocalUserId",
                table: "OutfitGeneratedImages");

            migrationBuilder.DropIndex(
                name: "IX_Favorites_LocalUserId",
                table: "Favorites");

            migrationBuilder.DropIndex(
                name: "IX_Clothes_LocalUserId",
                table: "Clothes");

            migrationBuilder.DropColumn(
                name: "LocalUserId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "LocalUserId",
                table: "PersonalProfiles");

            migrationBuilder.DropColumn(
                name: "LocalUserId",
                table: "OutfitWornRecords");

            migrationBuilder.DropColumn(
                name: "LocalUserId",
                table: "Outfits");

            migrationBuilder.DropColumn(
                name: "LocalUserId",
                table: "OutfitGeneratedImages");

            migrationBuilder.DropColumn(
                name: "LocalUserId",
                table: "Favorites");

            migrationBuilder.DropColumn(
                name: "LocalUserId",
                table: "Clothes");
        }
    }
}
