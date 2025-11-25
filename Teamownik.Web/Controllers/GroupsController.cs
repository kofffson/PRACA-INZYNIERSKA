using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Teamownik.Data.Models;
using Teamownik.Services.Interfaces;
using Teamownik.Web.Models;

namespace Teamownik.Web.Controllers;

[Authorize]
public class GroupsController : Controller
{
    private readonly ILogger<GroupsController> _logger;
    private readonly IGroupService _groupService;

    public GroupsController(
        ILogger<GroupsController> logger,
        IGroupService groupService)
    {
        _logger = logger;
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

            var userGroups = await _groupService.GetUserGroupsAsync(userId);
            
            var viewModel = new GroupsIndexViewModel
            {
                UserGroups = userGroups.Select(g => new GroupCardViewModel
                {
                    GroupId = g.GroupId,
                    GroupName = g.GroupName,
                    CreatorName = g.Creator?.FullName ?? g.Creator?.UserName ?? "Nieznany",
                    MemberCount = g.Members.Count,
                    IsCreator = g.CreatedBy == userId,
                    CreatedAt = g.CreatedAt
                }).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania grup");
            TempData["Error"] = "Wystąpił błąd podczas ładowania grup";
            return RedirectToAction("Index", "Home");
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        try
        {
            var group = await _groupService.GetGroupByIdAsync(id);
            if (group == null)
            {
                return NotFound();
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var members = await _groupService.GetGroupMembersAsync(id);

            var viewModel = new GroupDetailsViewModel
            {
                GroupId = group.GroupId,
                GroupName = group.GroupName,
                CreatorName = group.Creator?.FullName ?? group.Creator?.UserName ?? "Nieznany",
                CreatedAt = group.CreatedAt,
                IsCreator = group.CreatedBy == currentUserId,
                MemberCount = members.Count(),
                CurrentUserId = currentUserId ?? "",
                Members = members.Select(m => new GroupMemberViewModel
                {
                    UserId = m.UserId,
                    UserName = m.User?.FullName ?? m.User?.UserName ?? "Nieznany",
                    IsVIP = m.IsVIP,
                    JoinedAt = m.JoinedAt,
                    GamesPlayed = m.GamesPlayed,
                    GamesOrganized = m.GamesOrganized,
                    CanToggleVIP = group.CreatedBy == currentUserId && m.UserId != currentUserId
                }).ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania szczegółów grupy");
            TempData["Error"] = "Wystąpił błąd podczas ładowania szczegółów grupy";
            return RedirectToAction(nameof(Index));
        }
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateGroupViewModel model)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Identity/Account/Login");
            }

            var group = new Group
            {
                GroupName = model.GroupName,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _groupService.CreateGroupAsync(group);

            TempData["Success"] = $"Grupa '{group.GroupName}' została utworzona!";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas tworzenia grupy");
            ModelState.AddModelError("", $"Wystąpił błąd podczas tworzenia grupy: {ex.Message}");
            return View(model);
        }
    }

    [HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Deactivate(int id)
{
    try
    {
        _logger.LogInformation($"=== DEZAKTYWACJA - KROK 1: Sprawdzanie użytkownika ===");
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        _logger.LogInformation($"Użytkownik: {userId}");
        
        _logger.LogInformation($"=== DEZAKTYWACJA - KROK 2: Pobieranie grupy ID: {id} ===");
        var group = await _groupService.GetGroupByIdAsync(id);
        
        if (group == null)
        {
            _logger.LogWarning($"Grupa ID: {id} nie znaleziona");
            TempData["Error"] = "Grupa nie została znaleziona";
            return RedirectToAction(nameof(Index));
        }
        
        _logger.LogInformation($"Znaleziono grupę: {group.GroupName}, Twórca: {group.CreatedBy}");
        
        _logger.LogInformation($"=== DEZAKTYWACJA - KROK 3: Sprawdzanie uprawnień ===");
        if (group.CreatedBy != userId)
        {
            _logger.LogWarning($"Brak uprawnień: Użytkownik {userId} nie jest twórcą grupy");
            TempData["Error"] = "Nie masz uprawnień do dezaktywacji tej grupy";
            return RedirectToAction(nameof(Index));
        }

        _logger.LogInformation($"=== DEZAKTYWACJA - KROK 4: Wywołanie Service ===");
        var result = await _groupService.DeactivateGroupAsync(id);
        
        _logger.LogInformation($"=== DEZAKTYWACJA - KROK 5: Wynik Service: {result} ===");
        
        if (result)
        {
            TempData["Success"] = "Grupa została dezaktywowana";
            _logger.LogInformation($"=== GRUPA ID: {id} POMYŚLNIE DEZAKTYWOWANA ===");
        }
        else
        {
            TempData["Error"] = "Nie udało się dezaktywować grupy (Service zwrócił false)";
            _logger.LogWarning($"=== SERVICE ZWRÓCIŁ FALSE DLA GRUPY ID: {id} ===");
        }
        
        return RedirectToAction(nameof(Index));
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, $"BŁĄD podczas dezaktywacji grupy ID: {id}");
        TempData["Error"] = $"Wystąpił błąd podczas dezaktywacji grupy: {ex.Message}";
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
            var group = await _groupService.GetGroupByIdAsync(id);
            
            if (group == null)
            {
                return NotFound();
            }
            
            if (group.CreatedBy != userId)
            {
                return Forbid();
            }

            var result = await _groupService.DeleteGroupAsync(id);
            
            if (result)
            {
                TempData["Success"] = "Grupa została usunięta";
            }
            else
            {
                TempData["Error"] = "Nie udało się usunąć grupy";
            }
            
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania grupy");
            TempData["Error"] = "Wystąpił błąd podczas usuwania grupy";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleVIP(int groupId, string userId)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var result = await _groupService.ToggleVIPStatusAsync(groupId, userId, currentUserId);
            
            if (result)
            {
                TempData["Success"] = "Status VIP został zmieniony";
            }
            else
            {
                TempData["Error"] = "Nie udało się zmienić statusu VIP";
            }
            
            return RedirectToAction(nameof(Details), new { id = groupId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas zmiany statusu VIP");
            TempData["Error"] = "Wystąpił błąd podczas zmiany statusu VIP";
            return RedirectToAction(nameof(Details), new { id = groupId });
        }
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int groupId, string userId)
    {
        try
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var group = await _groupService.GetGroupByIdAsync(groupId);
        
            if (group == null)
            {
                return NotFound();
            }
        
            if (group.CreatedBy != currentUserId || userId == currentUserId)
            {
                return Forbid();
            }

            var result = await _groupService.RemoveMemberAsync(groupId, userId);
        
            if (result)
            {
                TempData["Success"] = "Członek został usunięty z grupy";
            }
            else
            {
                TempData["Error"] = "Nie udało się usunąć członka z grupy";
            }
        
            return RedirectToAction(nameof(Details), new { id = groupId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania członka z grupy");
            TempData["Error"] = "Wystąpił błąd podczas usuwania członka z grupy";
            return RedirectToAction(nameof(Details), new { id = groupId });
        }
    }
}