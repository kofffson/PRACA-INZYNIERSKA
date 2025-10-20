using System.ComponentModel.DataAnnotations.Schema;

[Table("zapisy")]
public class Enrollment
{
    public int Id { get; set; }
    public int UzytkownikId { get; set; }
    public int GrupaId { get; set; }
    public string Status { get; set; } = "oczekujacy";
    public DateTime DataZapisu { get; set; } = DateTime.Now;
    public DateTime? DataPotwierdzenia { get; set; }
    public int? PozycjaListaOczekujacych { get; set; }
    public string? Uwagi { get; set; }
    public string? KontaktAwaryjny { get; set; }
}