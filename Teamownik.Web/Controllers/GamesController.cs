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
            var userId = GetUserId();
            if (userId == null) return RedirectToLogin();

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

    public IActionResult Details(int id) => RedirectToAction(nameof(Manage), new { id });

    public async Task<IActionResult> Create()
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToLogin();

            var userGroups = await _groupService.GetUserGroupsAsync(userId);
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
            var userId = GetUserId();
            if (userId == null) return RedirectToLogin();

            if (!ModelState.IsValid)
            {
                ViewBag.UserGroups = await _groupService.GetUserGroupsAsync(userId);
                return View(model);
            }

            var polandTimeZone = GetPolandTimeZone();
            var startDateTime = TimeZoneInfo.ConvertTimeToUtc(model.GameDate.Date + model.StartTime, polandTimeZone);
            var endDateTime = TimeZoneInfo.ConvertTimeToUtc(model.GameDate.Date + model.EndTime, polandTimeZone);

            if ((startDateTime - DateTime.UtcNow).TotalMinutes < 60)
            {
                ModelState.AddModelError("", "Nie można utworzyć spotkania na mniej niż 1 godzinę przed jego rozpoczęciem.");
                ViewBag.UserGroups = await _groupService.GetUserGroupsAsync(userId);
                return View(model);
            }

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

            IEnumerable<Game> createdGames;
            if (model.IsRecurring && !string.IsNullOrEmpty(model.RecurrencePattern))
            {
                createdGames = await _gameService.CreateRecurringGamesAsync(game);
            }
            else
            {
                createdGames = new[] { await _gameService.CreateGameAsync(game) };
            }

            foreach (var createdGame in createdGames)
            {
                await _gameService.JoinGameAsync(createdGame.GameId, userId, 0);
            }

            var firstGame = createdGames.First();
            TempData["Success"] = model.IsRecurring 
                ? $"Utworzono serię {createdGames.Count()} cyklicznych spotkań '{firstGame.GameName}'!"
                : $"Spotkanie '{firstGame.GameName}' zostało utworzone!";
            
            return RedirectToAction("Index", "Home");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas tworzenia gry");
            ModelState.AddModelError("", "Wystąpił błąd podczas tworzenia spotkania.");
            
            var userId = GetUserId();
            if (userId != null)
            {
                ViewBag.UserGroups = await _groupService.GetUserGroupsAsync(userId);
            }
            
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToLogin();

            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                TempData["Error"] = "Nie znaleziono spotkania";
                return RedirectToAction("Index", "Home");
            }

            if (game.OrganizerId != userId)
            {
                TempData["Error"] = "Nie masz uprawnień do edycji tego spotkania";
                return RedirectToAction(nameof(Manage), new { id });
            }

            var polandTimeZone = GetPolandTimeZone();
            var localStart = TimeZoneInfo.ConvertTimeFromUtc(game.StartDateTime, polandTimeZone);
            var localEnd = TimeZoneInfo.ConvertTimeFromUtc(game.EndDateTime, polandTimeZone);

            var model = new EditGameViewModel
            {
                GameId = game.GameId,
                GameName = game.GameName,
                GameDate = localStart.Date,
                StartTime = localStart.TimeOfDay,
                EndTime = localEnd.TimeOfDay,
                Location = game.Location,
                Cost = game.Cost,
                IsPaid = game.IsPaid,
                MaxParticipants = game.MaxParticipants,
                OnlyForGroup = game.GroupId.HasValue,
                GroupId = game.GroupId,
                IsPublic = game.IsPublic
            };

            ViewBag.UserGroups = await _groupService.GetUserGroupsAsync(userId);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania formularza edycji");
            TempData["Error"] = "Wystąpił błąd podczas ładowania formularza edycji";
            return RedirectToAction("Index", "Home");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, EditGameViewModel model)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToLogin();

            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                TempData["Error"] = "Nie znaleziono spotkania";
                return RedirectToAction("Index", "Home");
            }

            if (game.OrganizerId != userId)
            {
                TempData["Error"] = "Nie masz uprawnień do edycji tego spotkania";
                return RedirectToAction(nameof(Manage), new { id });
            }

            if (!ModelState.IsValid)
            {
                ViewBag.UserGroups = await _groupService.GetUserGroupsAsync(userId);
                return View(model);
            }

            var polandTimeZone = GetPolandTimeZone();
            var startDateTime = TimeZoneInfo.ConvertTimeToUtc(model.GameDate.Date + model.StartTime, polandTimeZone);
            var endDateTime = TimeZoneInfo.ConvertTimeToUtc(model.GameDate.Date + model.EndTime, polandTimeZone);

            if ((startDateTime - DateTime.UtcNow).TotalMinutes < 60)
            {
                ModelState.AddModelError("", "Nie można edytować spotkania na czas mniejszy niż 1 godzina przed jego rozpoczęciem.");
                ViewBag.UserGroups = await _groupService.GetUserGroupsAsync(userId);
                return View(model);
            }

            game.GameName = model.GameName;
            game.Location = model.Location;
            game.StartDateTime = startDateTime;
            game.EndDateTime = endDateTime;
            game.Cost = model.IsPaid ? model.Cost : 0;
            game.IsPaid = model.IsPaid;
            game.MaxParticipants = model.MaxParticipants;
            game.GroupId = model.OnlyForGroup ? model.GroupId : null;
            game.IsPublic = model.IsPublic;

            var result = await _gameService.UpdateGameAsync(game);
            TempData[result ? "Success" : "Error"] = result 
                ? "Spotkanie zostało zaktualizowane" 
                : "Nie udało się zaktualizować spotkania";

            return RedirectToAction(nameof(Manage), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas aktualizacji gry");
            TempData["Error"] = "Wystąpił błąd podczas aktualizacji spotkania";
            
            var userId = GetUserId();
            if (userId != null)
            {
                ViewBag.UserGroups = await _groupService.GetUserGroupsAsync(userId);
            }
            
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Join(int id, int guestsCount = 0)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToLogin();

            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null)
            {
                TempData["Error"] = "Nie znaleziono spotkania.";
                return RedirectToAction("Index", "Home");
            }

            if (await _gameService.IsUserParticipantAsync(id, userId))
            {
                TempData["Error"] = "Jesteś już zapisany na to spotkanie.";
                return RedirectToAction("Index", "Home");
            }

            if ((game.StartDateTime - DateTime.UtcNow).TotalMinutes < 30)
            {
                TempData["Error"] = "Zapisy są zamknięte. Zapisy zamykają się pół godziny przed rozpoczęciem spotkania.";
                return RedirectToAction("Index", "Home");
            }

            if (game.Status == "cancelled")
            {
                TempData["Error"] = "To spotkanie zostało odwołane.";
                return RedirectToAction("Index", "Home");
            }

            var availableSpots = await _gameService.GetAvailableSpotsAsync(id);
            var totalSlotsNeeded = 1 + guestsCount;
            var result = await _gameService.JoinGameAsync(id, userId, guestsCount);
            
            if (result)
            {
                var isReserve = availableSpots < totalSlotsNeeded;
                TempData["Success"] = isReserve
                    ? (guestsCount > 0 ? $"Ty i {guestsCount} gość(ci) zostaliście dodani do listy rezerwowej!" : "Zostałeś dodany do listy rezerwowej!")
                    : (guestsCount > 0 ? $"Pomyślnie zapisano na spotkanie! (ty + {guestsCount} gość/gości)" : "Pomyślnie zapisano na spotkanie!");
            }
            else
            {
                TempData["Error"] = "Nie udało się zapisać na spotkanie. Spróbuj ponownie.";
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
    public async Task<IActionResult> UpdateGuests(int id, int guestsCount)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToLogin();

            if (guestsCount < 0)
            {
                TempData["Error"] = "Liczba gości nie może być ujemna.";
                return RedirectToAction(nameof(Manage), new { id });
            }

            var result = await _gameService.UpdateGuestsCountAsync(id, userId, guestsCount);
            TempData[result ? "Success" : "Error"] = result
                ? (guestsCount > 0 ? $"Zaktualizowano liczbę gości: {guestsCount}" : "Usunięto gości z zapisu")
                : "Nie można zaktualizować liczby gości - za mało wolnych miejsc";
        
            return RedirectToAction(nameof(Manage), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas aktualizacji gości");
            TempData["Error"] = "Wystąpił błąd podczas aktualizacji";
            return RedirectToAction(nameof(Manage), new { id });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Leave(int id)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToLogin();

            var result = await _gameService.LeaveGameAsync(id, userId);
            TempData[result ? "Success" : "Error"] = result 
                ? "Pomyślnie wypisano ze spotkania!" 
                : "Nie udało się wypisać ze spotkania.";
            
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
            var game = await _gameService.GetGameByIdAsync(id);
            if (game == null) return NotFound();
            
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
            var userId = GetUserId();
            var game = await _gameService.GetGameByIdAsync(id);
            
            if (game == null) return NotFound();
            if (game.OrganizerId != userId) return Forbid();
            
            var result = await _gameService.CancelGameAsync(id, reason ?? "Brak powodu");
            TempData[result ? "Success" : "Error"] = result 
                ? "Spotkanie zostało odwołane" 
                : "Nie udało się odwołać spotkania";
            
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
            var userId = GetUserId();
            var game = await _gameService.GetGameByIdAsync(id);
            
            if (game == null) return NotFound();
            if (game.OrganizerId != userId) return Forbid();
            
            var result = await _gameService.DeleteGameAsync(id);
            TempData[result ? "Success" : "Error"] = result 
                ? "Spotkanie zostało usunięte" 
                : "Nie udało się usunąć spotkania";
            
            return RedirectToAction("Index", "Home");
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
            var currentUserId = GetUserId();
            var game = await _gameService.GetGameByIdAsync(id);
    
            if (game == null) return NotFound();
            if (game.OrganizerId != currentUserId) return Forbid();
    
            if (userId == game.OrganizerId)
            {
                TempData["Error"] = "Nie możesz usunąć organizatora ze spotkania";
                return RedirectToAction(nameof(Manage), new { id });
            }
    
            var result = await _gameService.LeaveGameAsync(id, userId);
            TempData[result ? "Success" : "Error"] = result 
                ? "Uczestnik został usunięty" 
                : "Nie udało się usunąć uczestnika";
    
            return RedirectToAction(nameof(Manage), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania uczestnika");
            TempData["Error"] = "Wystąpił błąd podczas usuwania uczestnika";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MakePublic(int id)
    {
        try
        {
            var userId = GetUserId();
            var game = await _gameService.GetGameByIdAsync(id);

            if (game == null) return NotFound();
            if (game.OrganizerId != userId) return Forbid();

            game.IsPublic = true;
            await _gameService.UpdateGameAsync(game);

            TempData["Success"] = "Spotkanie zostało upublicznione! Teraz widzą je wszyscy.";
            return RedirectToAction(nameof(Manage), new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas upubliczniania gry");
            TempData["Error"] = "Wystąpił błąd podczas zmiany statusu.";
            return RedirectToAction(nameof(Manage), new { id });
        }
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private IActionResult RedirectToLogin() => RedirectToPage("/Identity/Account/Login");
    
    private TimeZoneInfo GetPolandTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Warsaw"); }
        catch
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"); }
            catch { return TimeZoneInfo.Local; }
        }
    }
}