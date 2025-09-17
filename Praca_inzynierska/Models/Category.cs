using System.ComponentModel.DataAnnotations.Schema;

[Table("kategorie")]
public class Category
{
    public int Id { get; set; }
    public string Nazwa { get; set; }
    public string? Opis { get; set; }
    public bool Aktywna { get; set; } = true;
    public DateTime DataUtworzenia { get; set; } = DateTime.Now;
}