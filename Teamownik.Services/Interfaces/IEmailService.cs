namespace Teamownik.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
    
    Task SendGameConfirmationAsync(string email, string userName, string gameName, DateTime gameDateTime, string location);
    Task SendWaitlistNotificationAsync(string email, string userName, string gameName, int position);
    Task SendPromotedFromWaitlistAsync(string email, string userName, string gameName, DateTime gameDateTime);
    Task SendGameReminderAsync(string email, string userName, string gameName, DateTime gameDateTime, string location);
    Task SendGameCancellationAsync(string email, string userName, string gameName, DateTime gameDateTime, string reason);
    Task SendGameUpdatedAsync(string email, string userName, string gameName, DateTime newDateTime, string newLocation);
    
    Task SendGroupInvitationAsync(string email, string groupName, string inviterName, string invitationLink);
    Task SendGroupWelcomeAsync(string email, string userName, string groupName);
}