using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Teamownik.Data.Models;
using Teamownik.Services.Interfaces;
using Teamownik.Web.Models;

namespace Teamownik.Web.Controllers;

[Authorize]
public class GamesController : Controller
{
    private readonly ILogger<GamesController> _logger;
    private readonly IGameService _gameService;
    private readonly IGroupService _groupService;

    public GamesController(
        ILogger<GamesController> logger,
        IGameService gameService,
        IGroupService groupService)
    {
        _logger = logger;
        _gameService = gameService;
        _groupService = groupService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Identity/Account/Login");
            }

            var games = await _gameService.GetUpcomingGamesAsync();
            return View(games);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania gier");
            TempData["Error"] = "Wystąpił błąd podczas ładowania spotkań";
            return RedirectToAction("Index", "Home");
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania szczegółów gry");
            TempData["Error"] = "Wystąpił błąd podczas ładowania szczegółów spotkania";
            return RedirectToAction("Index", "Home");
        }
    }

    public async Task<IActionResult> Create()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Create GET: User not authenticated");
                return RedirectToPage("/Identity/Account/Login");
            }

            _logger.LogInformation($"Create GET: Loading form for user {userId}");
            
            var userGroups = await _groupService.GetUserGroupsAsync(userId);
            _logger.LogInformation($"Create GET: Found {userGroups.Count()} groups for user");
            
            ViewBag.UserGroups = userGroups;
            return View();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania formularza tworzenia gry");
            TempData["Error"] = "Wystąpił błąd podczas ładowania formularza";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateGameViewModel model)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("Create POST: User not authenticated");
                return RedirectToPage("/Identity/Account/Login");
            }

            if (!ModelState.IsValid)
            {
                var userGroups = await _groupService.GetUserGroupsAsync(userId);
                ViewBag.UserGroups = userGroups;
                return View(model);
            }

            var localStartDateTime = model.GameDate.Date + model.StartTime;
            var localEndDateTime = model.GameDate.Date + model.EndTime;
            
            TimeZoneInfo polandTimeZone;
            try
            {
                polandTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw");
            }
            catch
            {
                try
                {
                    polandTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
                }
                catch
                {
                    polandTimeZone = TimeZoneInfo.Local;
                    _logger.LogWarning("Nie można znaleźć strefy czasowej Europe/Warsaw, używam lokalnej strefy czasowej");
                }
            }
            
            var startDateTime = TimeZoneInfo.ConvertTimeToUtc(localStartDateTime, polandTimeZone);
            var endDateTime = TimeZoneInfo.ConvertTimeToUtc(localEndDateTime, polandTimeZone);
            
            _logger.LogInformation($"Tworzenie gry: Czas lokalny start: {localStartDateTime:yyyy-MM-dd HH:mm:ss}, UTC: {startDateTime:yyyy-MM-dd HH:mm:ss}");

            var game = new Game
            {
                GameName = model.GameName,
                OrganizerId = userId,
                Location = model.Location,
                StartDateTime = startDateTime,
                EndDateTime = endDateTime,
                Cost = model.IsPaid ? model.Cost : 0,
                IsPaid = model.IsPaid,
                MaxParticipants = model.MaxParticipants,
                GroupId = model.OnlyForGroup ? model.GroupId : null,
                IsPublic = model.IsPublic,
                IsRecurring = model.IsRecurring,
                RecurrencePattern = model.RecurrencePattern,
                Status = "open",
                CreatedAt = DateTime.UtcNow
            };

            var createdGame = await _gameService.CreateGameAsync(game);
            await _gameService.JoinGameAsync(createdGame.GameId, userId);

            TempData["Success"] = $"Spotkanie '{createdGame.GameName}' zostało utworzone!";
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas tworzenia gry");
            ModelState.AddModelError("", $"Wystąpił błąd podczas tworzenia spotkania: {ex.Message}");
            
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var userGroups = await _groupService.GetUserGroupsAsync(userId);
                    ViewBag.UserGroups = userGroups;
                }
            }
            catch (Exception groupEx)
            {
                _logger.LogError(groupEx, "Błąd podczas ładowania grup");
            }
            
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Identity/Account/Login");
            }

            var result = await _gameService.JoinGameAsync(id, userId);
            
            if (result)
            {
                var game = await _gameService.GetGameByIdAsync(id);
                if (game != null && game.Status == "full")
                {
                    TempData["Success"] = "Zostałeś dodany do listy rezerwowej!";
                }
                else
                {
                    TempData["Success"] = "Pomyślnie zapisano na spotkanie!";
                }
            }
            else
            {
                TempData["Error"] = "Nie udało się zapisać na spotkanie. Możliwe, że jesteś już zapisany.";
            }
            
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas zapisywania na grę");
            TempData["Error"] = "Wystąpił błąd podczas zapisywania na spotkanie";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Identity/Account/Login");
            }

            var result = await _gameService.LeaveGameAsync(id, userId);
            
            if (result)
            {
                TempData["Success"] = "Pomyślnie wypisano ze spotkania!";
            }
            else
            {
                TempData["Error"] = "Nie udało się wypisać ze spotkania.";
            }
            
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas wypisywania z gry");
            TempData["Error"] = "Wystąpił błąd podczas wypisywania ze spotkania";
            return RedirectToAction("Index", "Home");
        }
    }

    public async Task<IActionResult> Manage(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var game = await _gameService.GetGameByIdAsync(id);
            
            if (game == null)
            {
                return NotFound();
            }
            
            if (game.OrganizerId != userId)
            {
                return Forbid();
            }
            
            return View(game);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania strony zarządzania");
            TempData["Error"] = "Wystąpił błąd podczas ładowania strony zarządzania";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id, string? reason)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var game = await _gameService.GetGameByIdAsync(id);
            
            if (game == null)
            {
                return NotFound();
            }
            
            if (game.OrganizerId != userId)
            {
                return Forbid();
            }
            
            var result = await _gameService.CancelGameAsync(id, reason ?? "Brak powodu");
            
            if (result)
            {
                TempData["Success"] = "Spotkanie zostało odwołane";
            }
            else
            {
                TempData["Error"] = "Nie udało się odwołać spotkania";
            }
            
            return RedirectToAction(nameof(Manage), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas odwoływania gry");
            TempData["Error"] = "Wystąpił błąd podczas odwoływania spotkania";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var game = await _gameService.GetGameByIdAsync(id);
            
            if (game == null)
            {
                return NotFound();
            }
            
            if (game.OrganizerId != userId)
            {
                return Forbid();
            }
            
            var result = await _gameService.DeleteGameAsync(id);
            
            if (result)
            {
                TempData["Success"] = "Spotkanie zostało usunięte";
            }
            else
            {
                TempData["Error"] = "Nie udało się usunąć spotkania";
            }
            
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania gry");
            TempData["Error"] = "Wystąpił błąd podczas usuwania spotkania";
            return RedirectToAction(nameof(Index));
        }
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveParticipant(int id, string userId)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var game = await _gameService.GetGameByIdAsync(id);
    
            if (game == null)
            {
                return NotFound();
            }
    
            if (game.OrganizerId != currentUserId)
            {
                return Forbid();
            }
    
            if (userId == game.OrganizerId)
            {
                TempData["Error"] = "Nie możesz usunąć organizatora ze spotkania";
                return RedirectToAction(nameof(Manage), new { id });
            }
    
            var result = await _gameService.LeaveGameAsync(id, userId);
    
            if (result)
            {
                TempData["Success"] = "Uczestnik został usunięty";
            }
            else
            {
                TempData["Error"] = "Nie udało się usunąć uczestnika";
            }
    
            return RedirectToAction(nameof(Manage), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania uczestnika");
            TempData["Error"] = "Wystąpił błąd podczas usuwania uczestnika";
            return RedirectToAction(nameof(Index));
        }
    }
}
