using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClosetApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiImageGenerationSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OutfitGeneratedImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OutfitId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProviderKind = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    PromptSnapshot = table.Column<string>(type: "TEXT", nullable: false),
                    ProfileSnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    OutfitSnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    OptionSnapshotJson = table.Column<string>(type: "TEXT", nullable: false),
                    ResultImagePath = table.Column<string>(type: "TEXT", nullable: true),
                    IsPrimary = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    FailureReason = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutfitGeneratedImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OutfitGeneratedImages_Outfits_OutfitId",
                        column: x => x.OutfitId,
                        principalTable: "Outfits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PersonalProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DisplayName = table.Column<string>(type: "TEXT", nullable: false),
                    HeightCm = table.Column<int>(type: "INTEGER", nullable: true),
                    BodyShape = table.Column<string>(type: "TEXT", nullable: false),
                    SkinTone = table.Column<string>(type: "TEXT", nullable: false),
                    HairLength = table.Column<string>(type: "TEXT", nullable: false),
                    HairColor = table.Column<string>(type: "TEXT", nullable: false),
                    FaceFeaturesSummary = table.Column<string>(type: "TEXT", nullable: false),
                    StyleKeywords = table.Column<string>(type: "TEXT", nullable: false),
                    AvoidKeywords = table.Column<string>(type: "TEXT", nullable: false),
                    AvatarPhotoPath = table.Column<string>(type: "TEXT", nullable: true),
                    FullBodyPhotoPath = table.Column<string>(type: "TEXT", nullable: true),
                    CloudUploadConsentAcceptedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonalProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OutfitGeneratedImages_OutfitId",
                table: "OutfitGeneratedImages",
                column: "OutfitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OutfitGeneratedImages");

            migrationBuilder.DropTable(
                name: "PersonalProfiles");
        }
    }
}
