using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Security.Claims;
using Teamownik.Data;
using Teamownik.Data.Models;
using Teamownik.Services.Interfaces;
using Teamownik.Web.Models;

namespace Teamownik.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IGameService _gameService;
    private readonly IGroupService _groupService;
    private readonly TeamownikDbContext _context;

    public HomeController(
        ILogger<HomeController> logger,
        UserManager<ApplicationUser> userManager,
        IGameService gameService,
        IGroupService groupService,
        TeamownikDbContext context)
    {
        _logger = logger;
        _userManager = userManager;
        _gameService = gameService;
        _groupService = groupService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Identity/Account/Login");
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return RedirectToPage("/Identity/Account/Login");
        }

        var model = new HomeViewModel
        {
            UserFirstName = user.FirstName,
            UserLastName = user.LastName
        };

        await LoadUserStatistics(model, userId);

        await LoadUserGames(model, userId);

        return View(model);
    }

    private async Task LoadUserStatistics(HomeViewModel model, string userId)
    {
        try
        {
            var userGroups = await _groupService.GetUserGroupsAsync(userId);
            model.ActiveGroupsCount = userGroups.Count();

            model.UnpaidSettlementsCount = await _context.Settlements
                .Where(s => s.PayerId == userId && !s.IsPaid)
                .CountAsync();

            var now = DateTime.UtcNow.AddMinutes(-5); // Tolerancja 5 minut
            
            _logger.LogInformation($"[LoadUserStatistics] Czas UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}, Czas z tolerancją: {now:yyyy-MM-dd HH:mm:ss}");
            
            model.UpcomingGamesCount = await _context.GameParticipants
                .Where(gp => gp.UserId == userId && gp.Game.StartDateTime > now)
                .CountAsync();

            var groupIds = userGroups.Select(g => g.GroupId).ToList();
            model.TotalUsersInGroups = await _context.GroupMembers
                .Where(gm => groupIds.Contains(gm.GroupId))
                .Select(gm => gm.UserId)
                .Distinct()
                .CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania statystyk użytkownika");
            model.ActiveGroupsCount = 0;
            model.UnpaidSettlementsCount = 0;
            model.UpcomingGamesCount = 0;
            model.TotalUsersInGroups = 0;
        }
    }

    private async Task LoadUserGames(HomeViewModel model, string userId)
{
    try
    {
        var now = DateTime.UtcNow.AddMinutes(-5); // Tolerancja 5 minut
        
        _logger.LogInformation($"[LoadUserGames] ===== ROZPOCZYNAM ŁADOWANIE GIER =====");
        _logger.LogInformation($"[LoadUserGames] Użytkownik: {userId}");
        _logger.LogInformation($"[LoadUserGames] Czas UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        _logger.LogInformation($"[LoadUserGames] Czas z tolerancją (-5 min): {now:yyyy-MM-dd HH:mm:ss}");

        var organizedGames = await _gameService.GetGamesByOrganizerAsync(userId);
        _logger.LogInformation($"[LoadUserGames] Znaleziono {organizedGames.Count()} gier organizowanych (PRZED filtrem)");
        
        var upcomingOrganized = organizedGames
            .Where(g => g.StartDateTime > now && g.Status != "cancelled")
            .OrderBy(g => g.StartDateTime)
            .Take(5)
            .ToList();

        _logger.LogInformation($"[LoadUserGames] Po filtrze: {upcomingOrganized.Count} organizowanych gier");
        
        foreach (var game in upcomingOrganized)
        {
            _logger.LogInformation($"[LoadUserGames] ✓ Dodaję organizowaną grę: {game.GameName}, Start: {game.StartDateTime:yyyy-MM-dd HH:mm:ss}, Status: {game.Status}");
            model.MyOrganizedGames.Add(await MapToGameCardViewModel(game, userId));
        }

        var participatingGames = await _gameService.GetGamesByParticipantAsync(userId);
        _logger.LogInformation($"[LoadUserGames] Znaleziono {participatingGames.Count()} gier uczestnictwa (PRZED filtrem)");
        
        var upcomingParticipating = participatingGames
            .Where(g => g.OrganizerId != userId && g.StartDateTime > now && g.Status != "cancelled")
            .OrderBy(g => g.StartDateTime)
            .Take(5)
            .ToList();

        _logger.LogInformation($"[LoadUserGames] Po filtrze: {upcomingParticipating.Count} gier uczestnictwa");
        
        foreach (var game in upcomingParticipating)
        {
            _logger.LogInformation($"[LoadUserGames] ✓ Dodaję grę uczestnictwa: {game.GameName}, Start: {game.StartDateTime:yyyy-MM-dd HH:mm:ss}");
            model.MyParticipatingGames.Add(await MapToGameCardViewModel(game, userId));
        }

        var userGroups = await _groupService.GetUserGroupsAsync(userId);
        var groupIds = userGroups.Select(g => g.GroupId).ToList();

        var groupGames = await _context.Games
            .Include(g => g.Organizer)
            .Include(g => g.Participants)
            .Include(g => g.Group)
            .Where(g => groupIds.Contains(g.GroupId ?? 0) 
                && g.StartDateTime > now 
                && (g.Status == "open" || g.Status == "full")
                && g.OrganizerId != userId
                && !g.Participants.Any(p => p.UserId == userId))
            .OrderBy(g => g.StartDateTime)
            .Take(5)
            .ToListAsync();

        _logger.LogInformation($"[LoadUserGames] Znaleziono {groupGames.Count} gier grupowych");

        foreach (var game in groupGames)
        {
            var cardModel = await MapToGameCardViewModel(game, userId);
            cardModel.IsFromUserGroup = true;
            model.MyGroupGames.Add(cardModel);
        }

        var publicGames = await _context.Games
            .Include(g => g.Organizer)
            .Include(g => g.Participants)
            .Where(g => g.IsPublic 
                && g.StartDateTime > now 
                && (g.Status == "open" || g.Status == "full")
                && g.OrganizerId != userId
                && !groupIds.Contains(g.GroupId ?? 0)
                && !g.Participants.Any(p => p.UserId == userId))
            .OrderBy(g => g.StartDateTime)
            .Take(3)
            .ToListAsync();

        _logger.LogInformation($"[LoadUserGames] Znaleziono {publicGames.Count} publicznych gier");
        
        foreach (var game in publicGames)
        {
            model.PublicGames.Add(await MapToGameCardViewModel(game, userId));
        }
        
        _logger.LogInformation($"[LoadUserGames] ===== PODSUMOWANIE =====");
        _logger.LogInformation($"[LoadUserGames] Organizowane: {model.MyOrganizedGames.Count}");
        _logger.LogInformation($"[LoadUserGames] Uczestniczące: {model.MyParticipatingGames.Count}");
        _logger.LogInformation($"[LoadUserGames] Grupowe: {model.MyGroupGames.Count}");
        _logger.LogInformation($"[LoadUserGames] Publiczne: {model.PublicGames.Count}");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Błąd podczas ładowania gier użytkownika");
    }
}

    private async Task<GameCardViewModel> MapToGameCardViewModel(Game game, string userId)
    {
        var confirmedCount = await _gameService.GetConfirmedParticipantsCountAsync(game.GameId);
        var waitlist = await _gameService.GetWaitlistAsync(game.GameId);
        var isUserParticipant = await _gameService.IsUserParticipantAsync(game.GameId, userId);

        return new GameCardViewModel
        {
            GameId = game.GameId,
            GameName = game.GameName,
            OrganizerName = game.Organizer.FullName,
            Location = game.Location,
            StartDateTime = game.StartDateTime,
            EndDateTime = game.EndDateTime,
            Cost = game.Cost,
            CurrentParticipants = confirmedCount,
            MaxParticipants = game.MaxParticipants,
            WaitlistCount = waitlist.Count(),
            Status = game.Status,
            IsRecurring = game.IsRecurring,
            RecurrencePattern = game.RecurrencePattern,
            IsOrganizedByUser = game.OrganizerId == userId,
            IsUserParticipant = isUserParticipant,
            GroupId = game.GroupId,
            GroupName = game.Group?.GroupName
        };
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel 
        { 
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
        });
    }
}
