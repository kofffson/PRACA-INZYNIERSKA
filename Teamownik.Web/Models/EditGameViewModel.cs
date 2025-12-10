using System;
using System.ComponentModel.DataAnnotations;

namespace Teamownik.Web.Models
{
    public class EditGameViewModel
    {
        public int GameId { get; set; }

        [Required(ErrorMessage = "Nazwa jest wymagana")]
        [Display(Name = "Nazwa rozgrywki")]
        public string GameName { get; set; }

        [Required(ErrorMessage = "Data jest wymagana")]
        [DataType(DataType.Date)]
        public DateTime GameDate { get; set; }

        [Required(ErrorMessage = "Czas rozpoczęcia jest wymagany")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Czas zakończenia jest wymagany")]
        [DataType(DataType.Time)]
        public TimeSpan EndTime { get; set; }

        [Required(ErrorMessage = "Miejsce jest wymagane")]
        public string Location { get; set; }

        [Range(0, 10000, ErrorMessage = "Koszt musi być liczbą dodatnią")]
        public decimal Cost { get; set; }

        public bool IsPaid { get; set; }

        [Range(2, 100, ErrorMessage = "Liczba uczestników musi być między 2 a 100")]
        public int MaxParticipants { get; set; }

        public bool OnlyForGroup { get; set; }

        public int? GroupId { get; set; }

        public bool IsPublic { get; set; }
    }
}