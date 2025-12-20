using Teamownik.Data.Models;

namespace Teamownik.Web.Models;

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
    public bool IsCancelled => Status == Constants.GameStatus.Cancelled;

    public bool IsRegistrationClosed
    {
        get
        {
            var timeUntilStart = StartDateTime - System.DateTime.UtcNow;
            return timeUntilStart.TotalMinutes < Constants.Defaults.RegistrationCloseMinutes;
        }
    }

    public bool CanJoin => !IsCancelled && !IsUserParticipant && !IsRegistrationClosed;

    public string BorderColorClass
    {
        get
        {
            if (Status == Constants.GameStatus.Cancelled)
                return Constants.CssClasses.BorderDanger;
            if (IsRegistrationClosed)
                return Constants.CssClasses.BorderDanger;
            if (IsFull)
                return Constants.CssClasses.BorderWarning;
            if (IsOrganizedByUser && CurrentParticipants < MaxParticipants / Constants.Defaults.LowParticipantsThresholdDivisor)
                return Constants.CssClasses.BorderWarning;
            return Constants.CssClasses.BorderSuccess;
        }
    }

    public string StatusBadgeClass
    {
        get
        {
            if (IsRegistrationClosed)
                return Constants.CssClasses.BadgeClosed;

            return Status switch
            {
                Constants.GameStatus.Full => Constants.CssClasses.BadgeFull,
                Constants.GameStatus.Closed => Constants.CssClasses.BadgeClosed,
                Constants.GameStatus.Cancelled => Constants.CssClasses.BadgeClosed,
                Constants.GameStatus.Open => Constants.CssClasses.BadgeOpen,
                _ => Constants.CssClasses.BadgeOpen
            };
        }
    }

    public string StatusBadgeText
    {
        get
        {
            if (Status == Constants.GameStatus.Cancelled)
                return Constants.Labels.Cancelled;
            if (IsRegistrationClosed)
                return Constants.Labels.RegistrationClosed;
            if (IsFull)
                return Constants.Labels.Full;
            if (Status == Constants.GameStatus.Closed)
                return Constants.Labels.Closed;
            if (AvailableSpots > 0)
                return $"Wolne: {AvailableSpots}";
            return Constants.Labels.Open;
        }
    }

    public bool ShowLowParticipantsWarning
    {
        get
        {
            if (!IsOrganizedByUser) return false;
            var threshold = MaxParticipants / Constants.Defaults.LowParticipantsThresholdDivisor;
            return CurrentParticipants < threshold && Status == Constants.GameStatus.Open;
        }
    }

    public string FormattedDateTime
    {
        get
        {
            var polandZone = TimeZoneInfo.FindSystemTimeZoneById(Constants.DateTime.PolandTimeZoneId);
            var localStart = TimeZoneInfo.ConvertTimeFromUtc(StartDateTime, polandZone);
            var localEnd = TimeZoneInfo.ConvertTimeFromUtc(EndDateTime, polandZone);

            if (IsRecurring && !string.IsNullOrEmpty(RecurrencePattern))
            {
                var pattern = RecurrencePattern switch
                {
                    Constants.RecurrencePattern.Daily => Constants.Labels.Daily,
                    Constants.RecurrencePattern.Weekly => Constants.Labels.Weekly,
                    Constants.RecurrencePattern.Biweekly => Constants.Labels.Biweekly,
                    Constants.RecurrencePattern.Monthly => Constants.Labels.Monthly,
                    _ => ""
                };
                return $"{pattern} • {localStart.ToString(Constants.DateTime.TimeFormat)}-{localEnd.ToString(Constants.DateTime.TimeFormat)}";
            }
            return $"{localStart.ToString(Constants.DateTime.DateFormat)} • {localStart.ToString(Constants.DateTime.TimeFormat)}-{localEnd.ToString(Constants.DateTime.TimeFormat)}";
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

    public bool IsOnWaitlist => ParticipantStatus == Constants.ParticipantStatus.Reserve;

    public string UserParticipationStatus
    {
        get
        {
            if (IsOnWaitlist)
                return Constants.Labels.OnWaitlist;
            if (IsUserParticipant)
                return Constants.Labels.Participating;
            return "";
        }
    }

    public string UserParticipationIcon
    {
        get
        {
            if (IsOnWaitlist)
                return Constants.Icons.HourglassSplit;
            if (IsUserParticipant)
                return Constants.Icons.CheckCircleFill;
            return "";
        }
    }

    public string UserParticipationColor
    {
        get
        {
            if (IsOnWaitlist)
                return Constants.Colors.Orange;
            if (IsUserParticipant)
                return Constants.Colors.Green;
            return Constants.Colors.Gray;
        }
    }
}