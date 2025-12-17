using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Teamownik.Data.Models;
using Teamownik.Web.Models;

namespace Teamownik.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        UserManager<ApplicationUser> userManager,
        ILogger<ProfileController> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToLogin();

        var model = new ProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? string.Empty,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            
            TotalGamesPlayed = user.GameParticipations?.Count ?? 0,
            TotalGamesOrganized = user.OrganizedGames?.Count ?? 0,
            ActiveGroupsCount = user.GroupMemberships?.Count ?? 0
        };

        return View(model);
    }

    public async Task<IActionResult> Edit()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToLogin();

        var model = new EditProfileViewModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Email = user.Email ?? string.Empty
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        try
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return RedirectToLogin();

            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                TempData["Success"] = "Profil został zaktualizowany";
                return RedirectToAction(nameof(Index));
            }

            TempData["Error"] = "Nie udało się zaktualizować profilu";
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas aktualizacji profilu");
            TempData["Error"] = "Wystąpił błąd podczas aktualizacji profilu";
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Cancel() => RedirectToAction(nameof(Index));

    private async Task<ApplicationUser?> GetCurrentUserAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return string.IsNullOrEmpty(userId) ? null : await _userManager.FindByIdAsync(userId);
    }

    private IActionResult RedirectToLogin() => RedirectToPage("/Identity/Account/Login");
}