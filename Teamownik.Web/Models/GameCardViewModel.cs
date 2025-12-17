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
        public string? ParticipantStatus { get; set; }
        public int? GroupId { get; set; }
        public string? GroupName { get; set; }
        
        public int AvailableSpots => MaxParticipants - CurrentParticipants;
        public bool IsFull => CurrentParticipants >= MaxParticipants;
        public bool HasWaitlist => WaitlistCount > 0;
        public bool IsCancelled => Status == "cancelled";
        
        public bool IsRegistrationClosed
        {
            get
            {
                var timeUntilStart = StartDateTime - DateTime.UtcNow;
                // return timeUntilStart.TotalMinutes < 30;
                return timeUntilStart.TotalMinutes < 0;
            }
        }
        
        public bool CanJoin => !IsCancelled && !IsUserParticipant && !IsRegistrationClosed;
        
        public string BorderColorClass
        {
            get
            {
                if (Status == "cancelled")
                    return "border-danger";
                if (IsRegistrationClosed)
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
                if (IsRegistrationClosed)
                    return "badge-closed";
                    
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
                if (IsRegistrationClosed)
                    return "Zapisy zamknięte";
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
                var polandZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                var localStart = TimeZoneInfo.ConvertTimeFromUtc(StartDateTime, polandZone);
                var localEnd = TimeZoneInfo.ConvertTimeFromUtc(EndDateTime, polandZone);
                
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
                    return $"{pattern} • {localStart:HH:mm}-{localEnd:HH:mm}";
                }
                return $"{localStart:dd.MM.yyyy} • {localStart:HH:mm}-{localEnd:HH:mm}";
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
        
        public bool IsOnWaitlist => ParticipantStatus == "reserve";
        
        public string UserParticipationStatus 
        {
            get
            {
                if (IsOnWaitlist)
                    return "Lista rezerwowa";
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
