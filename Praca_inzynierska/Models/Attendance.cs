using System.ComponentModel.DataAnnotations.Schema;

[Table("obecnosci")]
public class Attendance
{
    public int Id { get; set; }
    public int UzytkownikId { get; set; }
    public int TreningId { get; set; }
    public bool Obecny { get; set; } = false;
    public int SpoznienieMinuty { get; set; } = 0;
    public string? Uwagi { get; set; }
    public DateTime DataUtworzenia { get; set; } = DateTime.Now;
}