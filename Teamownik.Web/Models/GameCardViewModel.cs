using System;

namespace Teamownik.Web.Models
{
    public class GameCardViewModel
    {
        public int GameId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public string OrganizerName { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public decimal Cost { get; set; }
        public int CurrentParticipants { get; set; }
        public int MaxParticipants { get; set; }
        public int WaitlistCount { get; set; }
        public string Status { get; set; } = string.Empty; 
        public bool IsRecurring { get; set; }
        public string? RecurrencePattern { get; set; }
        public bool IsFromUserGroup { get; set; }
        public bool IsOrganizedByUser { get; set; }
        public bool IsUserParticipant { get; set; }
        public int? GroupId { get; set; }
        public string? GroupName { get; set; }
        
        public int AvailableSpots => MaxParticipants - CurrentParticipants;
        public bool IsFull => CurrentParticipants >= MaxParticipants;
        public bool HasWaitlist => WaitlistCount > 0;
        public bool IsCancelled => Status == "cancelled";
        public bool CanJoin => !IsCancelled && !IsUserParticipant;
        
        public string BorderColorClass
        {
            get
            {
                if (Status == "cancelled")
                    return "border-danger";
                if (IsFull)
                    return "border-warning";
                if (IsOrganizedByUser && CurrentParticipants < MaxParticipants / 3)
                    return "border-warning";
                return "border-success";
            }
        }
        
        public string StatusBadgeClass
        {
            get
            {
                return Status switch
                {
                    "full" => "badge-full",
                    "closed" => "badge-closed",
                    "cancelled" => "badge-closed",
                    "open" => "badge-open",
                    _ => "badge-open"
                };
            }
        }
        
        public string StatusBadgeText
        {
            get
            {
                if (Status == "cancelled")
                    return "Odwołane";
                if (IsFull)
                    return "Pełne";
                if (Status == "closed")
                    return "Zamknięte";
                if (AvailableSpots > 0)
                    return $"Wolne: {AvailableSpots}";
                return "Otwarte";
            }
        }
        
        public bool ShowLowParticipantsWarning
        {
            get
            {
                if (!IsOrganizedByUser) return false;
                var threshold = MaxParticipants / 3;
                return CurrentParticipants < threshold && Status == "open";
            }
        }
        
        public string FormattedDateTime
        {
            get
            {
                if (IsRecurring && !string.IsNullOrEmpty(RecurrencePattern))
                {
                    var pattern = RecurrencePattern switch
                    {
                        "daily" => "Codziennie",
                        "weekly" => "Co tydzień",
                        "biweekly" => "Co dwa tygodnie",
                        "monthly" => "Co miesiąc",
                        _ => ""
                    };
                    return $"{pattern} • {StartDateTime:HH:mm}-{EndDateTime:HH:mm}";
                }
                return $"{StartDateTime:dd.MM.yyyy} • {StartDateTime:HH:mm}-{EndDateTime:HH:mm}";
            }
        }
        
        public string ParticipantsText
        {
            get
            {
                var text = $"{CurrentParticipants}/{MaxParticipants} zapisanych";
                if (WaitlistCount > 0)
                    text += $" (lista rezerwowa: {WaitlistCount})";
                return text;
            }
        }
        
        public bool IsOnWaitlist => Status == "reserve" || (IsFull && IsUserParticipant);
        
        public string UserParticipationStatus 
        {
            get
            {
                if (IsOnWaitlist)
                    return "Jesteś na liście rezerwowej";
                else if (IsUserParticipant)
                    return "Uczestniczysz";
                else
                    return "";
            }
        }
    
        public string UserParticipationIcon 
        {
            get
            {
                if (IsOnWaitlist)
                    return "bi-hourglass-split";
                else if (IsUserParticipant)
                    return "bi-check-circle-fill";
                else
                    return "";
            }
        }
    
        public string UserParticipationColor 
        {
            get
            {
                if (IsOnWaitlist)
                    return "#ff9800";
                else if (IsUserParticipant)
                    return "#4caf50"; 
                else
                    return "#666"; 
            }
        }
    }
}
