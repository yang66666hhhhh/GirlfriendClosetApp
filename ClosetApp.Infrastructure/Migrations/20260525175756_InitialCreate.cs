using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClosetApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clothes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    GarmentType = table.Column<int>(type: "INTEGER", nullable: true),
                    ImagePath = table.Column<string>(type: "TEXT", nullable: true),
                    Color = table.Column<string>(type: "TEXT", nullable: true),
                    Brand = table.Column<string>(type: "TEXT", nullable: true),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    Season = table.Column<int>(type: "INTEGER", nullable: false),
                    FavoriteLevel = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clothes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Outfits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Scene = table.Column<int>(type: "INTEGER", nullable: false),
                    Season = table.Column<int>(type: "INTEGER", nullable: false),
                    Rating = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", nullable: true),
                    WornDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    WearCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Outfits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "TEXT", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Favorites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OutfitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favorites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Favorites_Outfits_OutfitId",
                        column: x => x.OutfitId,
                        principalTable: "Outfits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutfitClothes",
                columns: table => new
                {
                    OutfitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClothingId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutfitClothes", x => new { x.OutfitId, x.ClothingId });
                    table.ForeignKey(
                        name: "FK_OutfitClothes_Clothes_ClothingId",
                        column: x => x.ClothingId,
                        principalTable: "Clothes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OutfitClothes_Outfits_OutfitId",
                        column: x => x.OutfitId,
                        principalTable: "Outfits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutfitWornRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OutfitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WornDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutfitWornRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutfitWornRecords_Outfits_OutfitId",
                        column: x => x.OutfitId,
                        principalTable: "Outfits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClothingTags",
                columns: table => new
                {
                    ClothingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TagId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClothingTags", x => new { x.ClothingId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ClothingTags_Clothes_ClothingId",
                        column: x => x.ClothingId,
                        principalTable: "Clothes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClothingTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Tags",
                columns: new[] { "Id", "Category", "Color", "CreatedAt", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000001"), 0, "#D8B7A3", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "韩系", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000002"), 0, "#C8C2B8", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "极简", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000003"), 0, "#B7C4B2", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "通勤", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000004"), 0, "#E7C7C0", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "甜妹", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000005"), 0, "#C89B7B", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "美式", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000006"), 0, "#C4A98F", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "复古", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000007"), 0, "#A8B5A0", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Clean Fit", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000008"), 0, "#D4A5C9", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Y2K", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000009"), 1, "#E88D8D", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "约会", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000010"), 1, "#8BA8D9", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "上班", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000011"), 1, "#A8D4A8", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "出游", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000012"), 1, "#D4A5E8", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc), "派对", new DateTime(2026, 5, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClothingTags_TagId",
                table: "ClothingTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_Favorites_OutfitId",
                table: "Favorites",
                column: "OutfitId");

            migrationBuilder.CreateIndex(
                name: "IX_OutfitClothes_ClothingId",
                table: "OutfitClothes",
                column: "ClothingId");

            migrationBuilder.CreateIndex(
                name: "IX_OutfitWornRecords_OutfitId",
                table: "OutfitWornRecords",
                column: "OutfitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClothingTags");

            migrationBuilder.DropTable(
                name: "Favorites");

            migrationBuilder.DropTable(
                name: "OutfitClothes");

            migrationBuilder.DropTable(
                name: "OutfitWornRecords");

            migrationBuilder.DropTable(
                name: "Tags");

            migrationBuilder.DropTable(
                name: "Clothes");

            migrationBuilder.DropTable(
                name: "Outfits");
        }
    }
}
