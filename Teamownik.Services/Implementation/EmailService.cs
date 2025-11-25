using Microsoft.Extensions.Logging;
using Teamownik.Services.Interfaces;

namespace Teamownik.Services.Implementation;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation($"Email sent to {to}: {subject}");
        await Task.CompletedTask;
    }

    public async Task SendGameConfirmationAsync(string email, string userName, string gameName, DateTime gameDateTime, string location)
    {
        var subject = $"Potwierdzenie zapisu - {gameName}";
        var body = $@"
            Cześć {userName}!
            
            Twój zapis na trening ""{gameName}"" został potwierdzony.
            
            Szczegóły:
            - Data: {gameDateTime:dd.MM.yyyy HH:mm}
            - Miejsce: {location}
            
            Do zobaczenia!
        ";
        
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendWaitlistNotificationAsync(string email, string userName, string gameName, int position)
    {
        var subject = $"Lista oczekujących - {gameName}";
        var body = $@"
            Cześć {userName}!
            
            Trening ""{gameName}"" jest już pełny, ale zostałeś dodany na listę oczekujących.
            
            Twoja pozycja: {position}
            
            Powiadomimy Cię, gdy zwolni się miejsce!
        ";
        
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendPromotedFromWaitlistAsync(string email, string userName, string gameName, DateTime gameDateTime)
    {
        var subject = $"Zwolniło się miejsce! - {gameName}";
        var body = $@"
            Świetne wieści, {userName}!
            
            Zwolniło się miejsce na treningu ""{gameName}"".
            Zostałeś automatycznie przeniesiony z listy oczekujących!
            
            Data treningu: {gameDateTime:dd.MM.yyyy HH:mm}
            
            Do zobaczenia!
        ";
        
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendGameReminderAsync(string email, string userName, string gameName, DateTime gameDateTime, string location)
    {
        var subject = $"Przypomnienie - {gameName}";
        var body = $@"
            Cześć {userName}!
            
            Przypominamy o treningu ""{gameName}"" jutro o {gameDateTime:HH:mm}.
            
            Miejsce: {location}
            
            Do zobaczenia!
        ";
        
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendGameCancellationAsync(string email, string userName, string gameName, DateTime gameDateTime, string reason)
    {
        var subject = $"ODWOŁANIE - {gameName}";
        var body = $@"
            Cześć {userName}!
            
            Niestety musimy odwołać trening ""{gameName}"" zaplanowany na {gameDateTime:dd.MM.yyyy HH:mm}.
            
            Powód: {reason}
            
            Przepraszamy za niedogodności!
        ";
        
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendGameUpdatedAsync(string email, string userName, string gameName, DateTime newDateTime, string newLocation)
    {
        var subject = $"ZMIANA - {gameName}";
        var body = $@"
            Cześć {userName}!
            
            Informujemy o zmianie szczegółów treningu ""{gameName}"":
            
            Nowa data: {newDateTime:dd.MM.yyyy HH:mm}
            Nowe miejsce: {newLocation}
            
            Do zobaczenia!
        ";
        
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendGroupInvitationAsync(string email, string groupName, string inviterName, string invitationLink)
    {
        var subject = $"Zaproszenie do grupy - {groupName}";
        var body = $@"
            Cześć!
            
            {inviterName} zaprasza Cię do grupy ""{groupName}""!
            
            Kliknij w link, aby zaakceptować zaproszenie:
            {invitationLink}
            
            Zaproszenie ważne przez 7 dni.
        ";
        
        await SendEmailAsync(email, subject, body);
    }

    public async Task SendGroupWelcomeAsync(string email, string userName, string groupName)
    {
        var subject = $"Witamy w grupie - {groupName}";
        var body = $@"
            Cześć {userName}!
            
            Witamy w grupie ""{groupName}""!
            
            Teraz możesz przeglądać treningi grupowe i zapisywać się na nie.
            
            Miłego trenowania!
        ";
        
        await SendEmailAsync(email, subject, body);
    }
}