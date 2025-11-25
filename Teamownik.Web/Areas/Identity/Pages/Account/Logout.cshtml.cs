#nullable disable

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Teamownik.Data.Models;

namespace Teamownik.Web.Areas.Identity.Pages.Account
{
    public class LogoutModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(SignInManager<ApplicationUser> signInManager, ILogger<LogoutModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        public async Task<IActionResult> OnPost(string returnUrl = null)
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            
            // Przekieruj bezpośrednio na stronę logowania
            return RedirectToPage("./Login");
        }

        // GET nie jest potrzebny - jeśli ktoś wejdzie na /Logout przez GET, przekieruj na Login
        public IActionResult OnGet()
        {
            return RedirectToPage("./Login");
        }
    }
}