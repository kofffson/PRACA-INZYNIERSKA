using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Teamownik.Services.Interfaces;
using Teamownik.Web.Models;

namespace Teamownik.Web.Controllers;

[Authorize]
public class SettlementsController : Controller
{
    private readonly ISettlementService _settlementService;
    private readonly ILogger<SettlementsController> _logger;

    public SettlementsController(
        ISettlementService settlementService, 
        ILogger<SettlementsController> logger)
    {
        _settlementService = settlementService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> MyPayments()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) 
            return RedirectToPage("/Identity/Account/Login");

        var mySettlements = await _settlementService.GetUserPaymentsAsync(userId);
        var toPay = await _settlementService.GetTotalToPayAsync(userId);
        var paidThisMonth = await _settlementService.GetPaidThisMonthAsync(userId);

        var model = new SettlementsViewModel
        {
            CurrentMonthName = DateTime.Now.ToString("MMMM", new System.Globalization.CultureInfo("pl-PL")),
            TotalToPay = toPay,
            TotalPaidThisMonth = paidThisMonth,
            
            PaymentsToMake = mySettlements
                .Where(s => !s.IsPaid && s.Status == "pending")
                .Select(MapToViewModel)
                .ToList(),
                
            PaidHistory = mySettlements
                .Where(s => s.IsPaid)
                .OrderByDescending(s => s.PaidAt)
                .Take(10)
                .Select(MapToViewModel)
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> MyReceivables()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) 
            return RedirectToPage("/Identity/Account/Login");

        var receivables = await _settlementService.GetUserReceivablesAsync(userId);
        var totalToReceive = await _settlementService.GetTotalToReceiveAsync(userId);

        var model = new ReceivablesViewModel
        {
            TotalToReceive = totalToReceive,
            Receivables = receivables.Select(MapToViewModel).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAsPaid(int id, string paymentMethod = "bank_transfer")
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var settlement = await _settlementService.GetSettlementByIdAsync(id);

        if (settlement == null || settlement.PayerId != userId)
        {
            TempData["Error"] = "Nie znaleziono płatności lub brak uprawnień.";
            return RedirectToAction(nameof(MyPayments));
        }

        var result = await _settlementService.MarkAsPaidAsync(id, paymentMethod);
        
        if (result)
        {
            TempData["Success"] = "Płatność oznaczona jako zrealizowana.";
        }
        else
        {
            TempData["Error"] = "Nie udało się oznaczyć płatności.";
        }

        return RedirectToAction(nameof(MyPayments));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendReminder(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var settlement = await _settlementService.GetSettlementByIdAsync(id);

        if (settlement == null || settlement.RecipientId != userId)
        {
            TempData["Error"] = "Brak uprawnień.";
            return RedirectToAction(nameof(MyReceivables));
        }

        var result = await _settlementService.SendReminderToUserAsync(id);
        
        TempData[result ? "Success" : "Error"] = result 
            ? "Przypomnienie wysłane." 
            : "Nie udało się wysłać przypomnienia.";

        return RedirectToAction(nameof(MyReceivables));
    }

    private SettlementViewModel MapToViewModel(Data.Models.Settlement s)
    {
        return new SettlementViewModel
        {
            SettlementId = s.SettlementId,
            Title = s.Game?.GameName ?? "Rozliczenie",
            Amount = s.Amount,
            Date = s.Game?.StartDateTime ?? s.DueDate,
            RecipientName = s.Recipient?.FullName ?? "Nieznany",
            BankAccountNumber = s.BankAccountNumber ?? "Brak numeru konta",
            ParticipantsCount = s.Game?.Participants?.Count ?? 0,
            IsPaid = s.IsPaid,
            Status = s.Status,
            PaymentMethod = s.PaymentMethod,
            DueDate = s.DueDate,
            PaidAt = s.PaidAt
        };
    }
}