using System.ComponentModel.DataAnnotations.Schema;

[Table("treningi")]
public class Training
{
    public int Id { get; set; }
    public int GrupaId { get; set; }
    public string? Nazwa { get; set; }
    public string? Opis { get; set; }
    public DateTime DataTreningu { get; set; }
    public TimeSpan GodzinaStart { get; set; }
    public TimeSpan GodzinaKoniec { get; set; }
    public string? Miejsce { get; set; }
    public int? MaxUczestnikow { get; set; }
    public int ObecnychUczestnikow { get; set; } = 0;
    public bool Odwolany { get; set; } = false;
    public string? PowodOdwolania { get; set; }
    public DateTime DataUtworzenia { get; set; } = DateTime.Now;
}