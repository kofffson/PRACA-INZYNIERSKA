namespace Teamownik.Data.Models;

public static class Constants
{
    public static class GameStatus
    {
        public const string Open = "open";
        public const string Full = "full";
        public const string Closed = "closed";
        public const string Cancelled = "cancelled";
        public const string Completed = "completed";
        
        public const string Default = Open;
    }
    
    public static class ParticipantStatus
    {
        public const string Confirmed = "confirmed";
        public const string Reserve = "reserve";
        
        public const string Default = Confirmed;
    }
    
    public static class SettlementStatus
    {
        public const string Pending = "pending";
        public const string Paid = "paid";
        public const string Cancelled = "cancelled";
        public const string Overdue = "overdue";
        
        public const string Default = Pending;
    }
    
    public static class RecurrencePattern
    {
        public const string Daily = "daily";
        public const string Weekly = "weekly";
        public const string Biweekly = "biweekly";
        public const string Monthly = "monthly";
    }
    
    public static class PaymentMethod
    {
        public const string BankTransfer = "bank_transfer";
        public const string ConfirmedByOrganizer = "confirmed_by_organizer";
    }
    
    public static class Defaults
    {
        public const int MinParticipants = 2;
        public const int MaxParticipants = 100;
        public const int DefaultMessageCount = 50;
        public const int RegistrationCloseMinutes = 0;
        public const int LowParticipantsThresholdDivisor = 3;
    }
    
    public static class CssClasses
    {
        public const string BorderDanger = "border-danger";
        public const string BorderWarning = "border-warning";
        public const string BorderSuccess = "border-success";
        
        public const string BadgeFull = "badge-full";
        public const string BadgeClosed = "badge-closed";
        public const string BadgeOpen = "badge-open";
        
        public const string BgSuccess = "bg-success";
        public const string BgDanger = "bg-danger";
        public const string BgWarning = "bg-warning";
        public const string BgSecondary = "bg-secondary";
    }
    
    public static class Icons
    {
        public const string HourglassSplit = "bi-hourglass-split";
        public const string CheckCircleFill = "bi-check-circle-fill";
    }
    
    public static class Colors
    {
        public const string Orange = "#ff9800";
        public const string Green = "#4caf50";
        public const string Gray = "#666";
    }
    
    public static class Labels
    {
        public const string Cancelled = "Odwołane";
        public const string RegistrationClosed = "Zapisy zamknięte";
        public const string Full = "Pełne";
        public const string Closed = "Zamknięte";
        public const string Open = "Otwarte";
        
        public const string OnWaitlist = "Lista rezerwowa";
        public const string Participating = "Uczestniczysz";
        
        public const string Daily = "Codziennie";
        public const string Weekly = "Co tydzień";
        public const string Biweekly = "Co dwa tygodnie";
        public const string Monthly = "Co miesiąc";
    }
    
    public static class DateTime
    {
        public const string PolandTimeZoneId = "Central European Standard Time";
        public const string TimeFormat = "HH:mm";
        public const string DateFormat = "dd.MM.yyyy";
        public const string DateTimeFormat = "dd.MM.yyyy • HH:mm";
    }
    public static class Shared
    {
        public const string UnknownUser = "Nieznany";
    }

    public static class Navigation
    {
        public const string TabSettings = "settings";
        public const string TabMembers = "members";
    }
}