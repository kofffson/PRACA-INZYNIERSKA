using System.ComponentModel.DataAnnotations;

namespace Teamownik.Web.Models;

public class CreateGameViewModel
{
    [Required(ErrorMessage = "Nazwa rozgrywki jest wymagana")]
    [StringLength(200, ErrorMessage = "Nazwa może mieć maksymalnie 200 znaków")]
    [Display(Name = "Nazwa rozgrywki")]
    public string GameName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Data jest wymagana")]
    [Display(Name = "Data")]
    public DateTime GameDate { get; set; } = DateTime.Now.AddDays(1);

    [Required(ErrorMessage = "Godzina rozpoczęcia jest wymagana")]
    [Display(Name = "Godzina rozpoczęcia")]
    public TimeSpan StartTime { get; set; } = new TimeSpan(18, 0, 0);

    [Required(ErrorMessage = "Godzina zakończenia jest wymagana")]
    [Display(Name = "Godzina zakończenia")]
    public TimeSpan EndTime { get; set; } = new TimeSpan(19, 30, 0);

    [Required(ErrorMessage = "Miejsce jest wymagane")]
    [StringLength(300, ErrorMessage = "Miejsce może mieć maksymalnie 300 znaków")]
    [Display(Name = "Miejsce")]
    public string Location { get; set; } = string.Empty;

    [Display(Name = "Płatne")]
    public bool IsPaid { get; set; } = false;

    [Range(0, 1000, ErrorMessage = "Kwota musi być między 0 a 1000")]
    [Display(Name = "Kwota")]
    public decimal Cost { get; set; } = 0;

    [Required(ErrorMessage = "Limit miejsc jest wymagany")]
    [Range(2, 100, ErrorMessage = "Limit miejsc musi być między 2 a 100")]
    [Display(Name = "Limit miejsc")]
    public int MaxParticipants { get; set; } = 10;

    [Display(Name = "Tylko dla grupy")]
    public bool OnlyForGroup { get; set; } = false;

    public int? GroupId { get; set; }

    [Display(Name = "Publiczne")]
    public bool IsPublic { get; set; } = true;

    [Display(Name = "Cykliczne")]
    public bool IsRecurring { get; set; } = false;

    [Display(Name = "Wzorzec powtarzania")]
    public string? RecurrencePattern { get; set; }
}