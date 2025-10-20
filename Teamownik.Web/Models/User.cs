using System.ComponentModel.DataAnnotations.Schema;

[Table("uzytkownicy")]
public class User
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string Login { get; set; }
    public string Haslo { get; set; }
    public string Imie { get; set; }
    public string Nazwisko { get; set; }
    public string? Telefon { get; set; }
    public DateTime? DataUrodzenia { get; set; }
    public string Rola { get; set; } = "zawodnik";
    public bool Aktywny { get; set; } = true;
    public DateTime DataUtworzenia { get; set; } = DateTime.Now;
    public DateTime? OstatnieLogowanie { get; set; }
}