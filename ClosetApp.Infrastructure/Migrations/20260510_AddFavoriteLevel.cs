using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ClosetApp.Infrastructure.Data;

#nullable disable

namespace ClosetApp.Infrastructure.Migrations
{
    [DbContextAttribute(typeof(ClosetDbContext))]
    [Migration("20260510_AddFavoriteLevel")]
    public partial class AddFavoriteLevel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FavoriteLevel",
                table: "Clothes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FavoriteLevel",
                table: "Clothes");
        }
    }
}
