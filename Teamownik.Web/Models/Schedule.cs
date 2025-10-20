using System.ComponentModel.DataAnnotations.Schema;

[Table("harmonogram")]
public class Schedule
{
    public int Id { get; set; }
    public int GrupaId { get; set; }
    public int DzienTygodnia { get; set; }
    public TimeSpan GodzinaStart { get; set; }
    public TimeSpan GodzinaKoniec { get; set; }
    public string? Miejsce { get; set; }
    public bool Aktywny { get; set; } = true;
    public DateTime DataUtworzenia { get; set; } = DateTime.Now;
}