using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Teamownik.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OstatecznaNaprawa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GroupInvitations");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Settlements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "Settlements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentProviderId",
                table: "Settlements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentReference",
                table: "Settlements",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Settlements",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "RecurrenceSeriesId",
                table: "Games",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GuestsCount",
                table: "GameParticipants",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "PaymentProviderId",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "PaymentReference",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "RecurrenceSeriesId",
                table: "Games");

            migrationBuilder.DropColumn(
                name: "GuestsCount",
                table: "GameParticipants");

            migrationBuilder.CreateTable(
                name: "GroupInvitations",
                columns: table => new
                {
                    InvitationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GroupId = table.Column<int>(type: "integer", nullable: false),
                    InvitedBy = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InvitedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    IsAccepted = table.Column<bool>(type: "boolean", nullable: false),
                    Token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupInvitations", x => x.InvitationId);
                    table.ForeignKey(
                        name: "FK_GroupInvitations_AspNetUsers_InvitedBy",
                        column: x => x.InvitedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GroupInvitations_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "GroupId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GroupInvitations_GroupId",
                table: "GroupInvitations",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_GroupInvitations_InvitedBy",
                table: "GroupInvitations",
                column: "InvitedBy");

            migrationBuilder.CreateIndex(
                name: "IX_GroupInvitations_Token",
                table: "GroupInvitations",
                column: "Token",
                unique: true);
        }
    }
}
