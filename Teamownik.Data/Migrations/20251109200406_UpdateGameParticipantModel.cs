using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Teamownik.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGameParticipantModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "Games",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ConfirmedAt",
                table: "GameParticipants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailSent",
                table: "GameParticipants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "GameParticipants",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "WaitlistPosition",
                table: "GameParticipants",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "ConfirmedAt",
                table: "GameParticipants");

            migrationBuilder.DropColumn(
                name: "EmailSent",
                table: "GameParticipants");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "GameParticipants");

            migrationBuilder.DropColumn(
                name: "WaitlistPosition",
                table: "GameParticipants");
        }
    }
}
