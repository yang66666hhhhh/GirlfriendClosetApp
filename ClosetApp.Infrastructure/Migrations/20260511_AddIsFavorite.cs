using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ClosetApp.Infrastructure.Data;

#nullable disable

namespace ClosetApp.Infrastructure.Migrations
{
    [DbContextAttribute(typeof(ClosetDbContext))]
    [Migration("20260511_AddIsFavorite")]
    public partial class AddIsFavorite : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsFavorite",
                table: "Clothes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsFavorite",
                table: "Clothes");
        }
    }
}
