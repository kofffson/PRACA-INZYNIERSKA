using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Teamownik.Data.Models;
using Teamownik.Services.Interfaces;
using Teamownik.Web.Models;
using Teamownik.Data;
using Microsoft.EntityFrameworkCore;

namespace Teamownik.Web.Controllers;

[Authorize]
public class GroupsController : Controller
{
    private readonly ILogger<GroupsController> _logger;
    private readonly IGroupService _groupService;
    private readonly TeamownikDbContext _context;

    public GroupsController(
        ILogger<GroupsController> logger,
        IGroupService groupService,
        TeamownikDbContext context)
    {
        _logger = logger;
        _groupService = groupService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var userId = GetUserId();
            if (userId == null) return RedirectToLogin();

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
            if (group == null) return NotFound();

            var currentUserId = GetUserId();
            var members = await _groupService.GetGroupMembersAsync(id);
            var recentMessages = await _groupService.GetGroupMessagesAsync(id, count: 50);

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
                }).ToList(),
                RecentMessages = recentMessages.ToList() 
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

    public IActionResult Create() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateGroupViewModel model)
    {
        try
        {
            if (!ModelState.IsValid) return View(model);

            var userId = GetUserId();
            if (userId == null) return RedirectToLogin();
            
            var groupExists = await _context.Groups
                .AnyAsync(g => g.GroupName.ToLower() == model.GroupName.ToLower() && g.IsActive);

            if (groupExists)
            {
                ModelState.AddModelError("GroupName", "Grupa o tej nazwie już istnieje. Wybierz inną nazwę.");
                return View(model);
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
            ModelState.AddModelError("", "Wystąpił błąd podczas tworzenia grupy.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            var userId = GetUserId();
            var group = await _groupService.GetGroupByIdAsync(id);
            
            if (group == null)
            {
                TempData["Error"] = "Grupa nie została znaleziona";
                return RedirectToAction(nameof(Index));
            }
            
            if (group.CreatedBy != userId)
            {
                TempData["Error"] = "Nie masz uprawnień do dezaktywacji tej grupy";
                return RedirectToAction(nameof(Index));
            }

            var result = await _groupService.DeactivateGroupAsync(id);
            TempData[result ? "Success" : "Error"] = result 
                ? "Grupa została dezaktywowana" 
                : "Nie udało się dezaktywować grupy";
            
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas dezaktywacji grupy ID: {GroupId}", id);
            TempData["Error"] = "Wystąpił błąd podczas dezaktywacji grupy";
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
            var group = await _groupService.GetGroupByIdAsync(id);
            
            if (group == null) return NotFound();
            if (group.CreatedBy != userId) return Forbid();

            var result = await _groupService.DeleteGroupAsync(id);
            TempData[result ? "Success" : "Error"] = result 
                ? "Grupa została usunięta" 
                : "Nie udało się usunąć grupy";
            
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
            var currentUserId = GetUserId();
            var result = await _groupService.ToggleVIPStatusAsync(groupId, userId, currentUserId);
            
            TempData[result ? "Success" : "Error"] = result 
                ? "Status VIP został zmieniony" 
                : "Nie udało się zmienić statusu VIP";
            
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
    public async Task<IActionResult> AddMemberByUsername(int groupId, string username)
    {
        try
        {
            var currentUserId = GetUserId();
            var group = await _groupService.GetGroupByIdAsync(groupId);
        
            if (group == null || group.CreatedBy != currentUserId)
            {
                TempData["Error"] = "Nie masz uprawnień do zarządzania tą grupą";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            var userToAdd = await _context.Users.FirstOrDefaultAsync(u => u.Email == username);
        
            if (userToAdd == null)
            {
                TempData["Error"] = $"Nie znaleziono użytkownika: {username}";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            if (await _groupService.IsUserMemberAsync(groupId, userToAdd.Id))
            {
                TempData["Error"] = "Ten użytkownik już jest członkiem grupy";
                return RedirectToAction(nameof(Details), new { id = groupId });
            }

            var result = await _groupService.AddMemberAsync(groupId, userToAdd.Id, isVIP: false);
            TempData[result ? "Success" : "Error"] = result 
                ? $"Użytkownik {userToAdd.FullName} został dodany do grupy!" 
                : "Nie udało się dodać użytkownika";
        
            return RedirectToAction("Details", new { id = groupId, activeTab = "settings" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas dodawania członka do grupy");
            TempData["Error"] = "Wystąpił błąd podczas dodawania członka";
            return RedirectToAction("Details", new { id = groupId, activeTab = "settings" });
        }
    }
    
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int groupId, string userId)
    {
        try
        {
            var currentUserId = GetUserId();
            var group = await _groupService.GetGroupByIdAsync(groupId);
        
            if (group == null) return NotFound();
            if (group.CreatedBy != currentUserId || userId == currentUserId) return Forbid();

            var result = await _groupService.RemoveMemberAsync(groupId, userId);
            TempData[result ? "Success" : "Error"] = result 
                ? "Członek został usunięty z grupy" 
                : "Nie udało się usunąć członka z grupy";
        
            return RedirectToAction("Details", new { id = groupId, activeTab = "members" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania członka z grupy");
            TempData["Error"] = "Wystąpił błąd podczas usuwania członka z grupy";
            return RedirectToAction("Details", new { id = groupId, activeTab = "members" });
        }
    }

    private string? GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private IActionResult RedirectToLogin() => RedirectToPage("/Identity/Account/Login");
}