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
        var userId = GetUserId();
        if (userId == null) return RedirectToLogin();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return RedirectToLogin();

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
            var now = DateTime.UtcNow.AddMinutes(-5);
            
            var userGroups = await _groupService.GetUserGroupsAsync(userId);
            model.ActiveGroupsCount = userGroups.Count();

            model.UnpaidSettlementsCount = await _context.Settlements
                .CountAsync(s => s.PayerId == userId && !s.IsPaid);

            model.UpcomingGamesCount = await _context.GameParticipants
                .CountAsync(gp => gp.UserId == userId && gp.Game.StartDateTime > now);

            model.TotalUsersInGroups = await _context.Users.CountAsync();
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
            var now = DateTime.UtcNow.AddMinutes(-5);

            var organizedGames = await _gameService.GetGamesByOrganizerAsync(userId);
            var upcomingOrganized = organizedGames
                .Where(g => g.StartDateTime > now && g.Status != "cancelled")
                .OrderBy(g => g.StartDateTime);

            foreach (var game in upcomingOrganized)
            {
                model.MyOrganizedGames.Add(await MapToGameCardViewModel(game, userId));
            }

            var participatingGames = await _gameService.GetGamesByParticipantAsync(userId);
            var upcomingParticipating = participatingGames
                .Where(g => g.OrganizerId != userId && g.StartDateTime > now && g.Status != "cancelled")
                .OrderBy(g => g.StartDateTime);

            foreach (var game in upcomingParticipating)
            {
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
                .ToListAsync();

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
                .ToListAsync();

            foreach (var game in publicGames)
            {
                model.PublicGames.Add(await MapToGameCardViewModel(game, userId));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania gier użytkownika");
        }
    }

    private async Task<GameCardViewModel> MapToGameCardViewModel(Game game, string userId)
    {
        var totalSlotsOccupied = await _gameService.GetTotalSlotsOccupiedAsync(game.GameId);
        var waitlist = await _gameService.GetWaitlistAsync(game.GameId);
        
        var participant = game.Participants?.FirstOrDefault(p => p.UserId == userId);
        var isUserParticipant = participant != null;

        return new GameCardViewModel
        {
            GameId = game.GameId,
            GameName = game.GameName,
            OrganizerName = game.Organizer.FullName,
            Location = game.Location,
            StartDateTime = game.StartDateTime,
            EndDateTime = game.EndDateTime,
            Cost = game.Cost,
            CurrentParticipants = totalSlotsOccupied,
            MaxParticipants = game.MaxParticipants,
            WaitlistCount = waitlist.Count(),
            Status = game.Status,
            IsRecurring = game.IsRecurring,
            RecurrencePattern = game.RecurrencePattern,
            IsOrganizedByUser = game.OrganizerId == userId,
            IsUserParticipant = isUserParticipant,
            ParticipantStatus = participant?.Status,
            GroupId = game.GroupId,
            GroupName = game.Group?.GroupName
        };
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel 
        { 
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier 
        });
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private IActionResult RedirectToLogin() => RedirectToPage("/Identity/Account/Login");
}