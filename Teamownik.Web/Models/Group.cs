using System.ComponentModel.DataAnnotations.Schema;

[Table("grupy")]
public class Group
{
    public int Id { get; set; }
    public string Nazwa { get; set; }
    public string? Opis { get; set; }
    public int? KategoriaId { get; set; }
    public int TrenerId { get; set; }
    public int MinZawodnikow { get; set; } = 1;
    public int MaxZawodnikow { get; set; }
    public int ObecnychZawodnikow { get; set; } = 0;
    public string Status { get; set; } = "aktywna";
    public DateTime? DataRozpoczecia { get; set; }
    public DateTime? DataZakonczenia { get; set; }
    public string? Miejsce { get; set; }
    public string? Adres { get; set; }
    public DateTime DataUtworzenia { get; set; } = DateTime.Now;
}