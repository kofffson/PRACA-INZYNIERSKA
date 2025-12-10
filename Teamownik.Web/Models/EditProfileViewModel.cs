using System.ComponentModel.DataAnnotations;

namespace Teamownik.Web.Models;

public class EditProfileViewModel
{
    [Required(ErrorMessage = "Imię jest wymagane")]
    [StringLength(50, ErrorMessage = "Imię nie może być dłuższe niż 50 znaków")]
    [Display(Name = "Imię")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nazwisko jest wymagane")]
    [StringLength(50, ErrorMessage = "Nazwisko nie może być dłuższe niż 50 znaków")]
    [Display(Name = "Nazwisko")]
    public string LastName { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Nieprawidłowy numer telefonu")]
    [Display(Name = "Numer telefonu")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "E-mail")]
    public string Email { get; set; } = string.Empty;
}