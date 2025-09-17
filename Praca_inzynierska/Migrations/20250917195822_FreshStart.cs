using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Praca_inzynierska.Migrations
{
    /// <inheritdoc />
    public partial class FreshStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "grupy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nazwa = table.Column<string>(type: "text", nullable: false),
                    Opis = table.Column<string>(type: "text", nullable: true),
                    KategoriaId = table.Column<int>(type: "integer", nullable: true),
                    TrenerId = table.Column<int>(type: "integer", nullable: false),
                    MinZawodnikow = table.Column<int>(type: "integer", nullable: false),
                    MaxZawodnikow = table.Column<int>(type: "integer", nullable: false),
                    ObecnychZawodnikow = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DataRozpoczecia = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DataZakonczenia = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Miejsce = table.Column<string>(type: "text", nullable: true),
                    Adres = table.Column<string>(type: "text", nullable: true),
                    DataUtworzenia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grupy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "harmonogram",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GrupaId = table.Column<int>(type: "integer", nullable: false),
                    DzienTygodnia = table.Column<int>(type: "integer", nullable: false),
                    GodzinaStart = table.Column<TimeSpan>(type: "interval", nullable: false),
                    GodzinaKoniec = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Miejsce = table.Column<string>(type: "text", nullable: true),
                    Aktywny = table.Column<bool>(type: "boolean", nullable: false),
                    DataUtworzenia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_harmonogram", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "kategorie",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nazwa = table.Column<string>(type: "text", nullable: false),
                    Opis = table.Column<string>(type: "text", nullable: true),
                    Aktywna = table.Column<bool>(type: "boolean", nullable: false),
                    DataUtworzenia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kategorie", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "obecnosci",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UzytkownikId = table.Column<int>(type: "integer", nullable: false),
                    TreningId = table.Column<int>(type: "integer", nullable: false),
                    Obecny = table.Column<bool>(type: "boolean", nullable: false),
                    SpoznienieMinuty = table.Column<int>(type: "integer", nullable: false),
                    Uwagi = table.Column<string>(type: "text", nullable: true),
                    DataUtworzenia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_obecnosci", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "treningi",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GrupaId = table.Column<int>(type: "integer", nullable: false),
                    Nazwa = table.Column<string>(type: "text", nullable: true),
                    Opis = table.Column<string>(type: "text", nullable: true),
                    DataTreningu = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GodzinaStart = table.Column<TimeSpan>(type: "interval", nullable: false),
                    GodzinaKoniec = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Miejsce = table.Column<string>(type: "text", nullable: true),
                    MaxUczestnikow = table.Column<int>(type: "integer", nullable: true),
                    ObecnychUczestnikow = table.Column<int>(type: "integer", nullable: false),
                    Odwolany = table.Column<bool>(type: "boolean", nullable: false),
                    PowodOdwolania = table.Column<string>(type: "text", nullable: true),
                    DataUtworzenia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_treningi", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "uzytkownicy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    Login = table.Column<string>(type: "text", nullable: false),
                    Haslo = table.Column<string>(type: "text", nullable: false),
                    Imie = table.Column<string>(type: "text", nullable: false),
                    Nazwisko = table.Column<string>(type: "text", nullable: false),
                    Telefon = table.Column<string>(type: "text", nullable: true),
                    DataUrodzenia = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Rola = table.Column<string>(type: "text", nullable: false),
                    Aktywny = table.Column<bool>(type: "boolean", nullable: false),
                    DataUtworzenia = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OstatnieLogowanie = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_uzytkownicy", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "zapisy",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UzytkownikId = table.Column<int>(type: "integer", nullable: false),
                    GrupaId = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    DataZapisu = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataPotwierdzenia = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PozycjaListaOczekujacych = table.Column<int>(type: "integer", nullable: true),
                    Uwagi = table.Column<string>(type: "text", nullable: true),
                    KontaktAwaryjny = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_zapisy", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "grupy");

            migrationBuilder.DropTable(
                name: "harmonogram");

            migrationBuilder.DropTable(
                name: "kategorie");

            migrationBuilder.DropTable(
                name: "obecnosci");

            migrationBuilder.DropTable(
                name: "treningi");

            migrationBuilder.DropTable(
                name: "uzytkownicy");

            migrationBuilder.DropTable(
                name: "zapisy");
        }
    }
}
