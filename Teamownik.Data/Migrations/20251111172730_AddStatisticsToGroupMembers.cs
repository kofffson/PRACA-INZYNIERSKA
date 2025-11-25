using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Teamownik.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStatisticsToGroupMembers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GamesOrganized",
                table: "GroupMembers",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastGamePlayed",
                table: "GroupMembers",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GamesOrganized",
                table: "GroupMembers");

            migrationBuilder.DropColumn(
                name: "LastGamePlayed",
                table: "GroupMembers");
        }
    }
}
