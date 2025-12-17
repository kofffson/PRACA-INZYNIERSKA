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

    // GET: /Settlements/MyPayments
    // GET: /Settlements/MyPayments?year=2024&month=12
    [HttpGet]
    public async Task<IActionResult> MyPayments(int? year, int? month)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) 
            return RedirectToPage("/Identity/Account/Login");

        // Pobierz wszystkie płatności użytkownika
        var mySettlements = await _settlementService.GetUserPaymentsAsync(userId);
        
        // Ustal miesiąc do wyświetlenia (domyślnie bieżący)
        var targetYear = year ?? DateTime.Now.Year;
        var targetMonth = month ?? DateTime.Now.Month;
        
        // Pobierz podsumowanie miesięczne
        var monthlySummary = await _settlementService.GetMonthlyPaymentSummaryAsync(userId, targetYear, targetMonth);
        
        // Filtruj płatności dla wybranego miesiąca
        var startOfMonth = new DateTime(targetYear, targetMonth, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfMonth = startOfMonth.AddMonths(1);
        
        var monthPayments = mySettlements.Where(s => 
            s.Game != null && 
            s.Game.StartDateTime >= startOfMonth && 
            s.Game.StartDateTime < endOfMonth
        ).ToList();
        
        // Pobierz dane dla bieżącego miesiąca (górne kafelki)
        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var currentMonthEnd = currentMonthStart.AddMonths(1);
        
        var toPay = await _settlementService.GetTotalToPayAsync(userId);
        var paidThisMonth = mySettlements
            .Where(s => s.IsPaid && s.PaidAt >= currentMonthStart && s.PaidAt < currentMonthEnd)
            .Sum(s => s.Amount);

        var model = new SettlementsViewModel
        {
            CurrentMonthName = new DateTime(targetYear, targetMonth, 1).ToString("MMMM yyyy", new System.Globalization.CultureInfo("pl-PL")),
            TotalToPay = toPay,
            TotalPaidThisMonth = paidThisMonth,
            
            // Podsumowanie wybranego miesiąca
            MonthlyBreakdown = new MonthlyBreakdown
            {
                TotalAmount = monthlySummary["Total"],
                PaidAmount = monthlySummary["Paid"],
                UnpaidAmount = monthlySummary["Unpaid"],
                PaymentsCount = (int)monthlySummary["Count"]
            },
            
            // Płatności do wykonania (wszystkie niezapłacone)
            PaymentsToMake = mySettlements
                .Where(s => !s.IsPaid && s.Status == "pending")
                .Select(MapToViewModel)
                .OrderBy(s => s.DueDate)
                .ToList(),
                
            // Historia płatności (ostatnie 10 zapłaconych)
            PaidHistory = mySettlements
                .Where(s => s.IsPaid)
                .OrderByDescending(s => s.PaidAt)
                .Take(10)
                .Select(MapToViewModel)
                .ToList()
        };

        return View(model);
    }

    // GET: /Settlements/MyReceivables
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

    // GET: /Settlements/GameSettlements/5
    [HttpGet]
    public async Task<IActionResult> GameSettlements(int gameId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId)) 
            return RedirectToPage("/Identity/Account/Login");

        var summary = await _settlementService.GetGameSettlementSummaryAsync(gameId);
        
        // Sprawdź czy użytkownik jest organizatorem
        if (summary.Settlements.Any())
        {
            var firstSettlement = await _settlementService.GetSettlementByIdAsync(summary.Settlements.First().SettlementId);
            if (firstSettlement?.Game?.OrganizerId != userId)
            {
                TempData["Error"] = "Nie masz uprawnień do przeglądania rozliczeń tej gry";
                return RedirectToAction(nameof(MyPayments));
            }
        }

        var model = new GameSettlementsViewModel
        {
            GameId = summary.GameId,
            GameName = summary.GameName,
            TotalAmount = summary.TotalAmount,
            TotalParticipants = summary.TotalParticipants,
            PaidCount = summary.PaidCount,
            UnpaidCount = summary.UnpaidCount,
            TotalCollected = summary.TotalCollected,
            TotalOutstanding = summary.TotalOutstanding,
            Settlements = summary.Settlements.Select(s => new GameSettlementDetailViewModel
            {
                SettlementId = s.SettlementId,
                PayerName = s.PayerName,
                PayerEmail = s.PayerEmail,
                Amount = s.Amount,
                IsPaid = s.IsPaid,
                PaidAt = s.PaidAt,
                Status = s.Status,
                DueDate = s.DueDate
            }).ToList()
        };

        return View(model);
    }

    // POST: /Settlements/MarkAsPaid/5
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

    // POST: /Settlements/ConfirmPaymentByOrganizer
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmPaymentByOrganizer(int settlementId, int gameId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        var result = await _settlementService.MarkAsPaidByOrganizerAsync(settlementId, userId);
        
        if (result)
        {
            TempData["Success"] = "Płatność potwierdzona.";
        }
        else
        {
            TempData["Error"] = "Nie udało się potwierdzić płatności.";
        }

        return RedirectToAction(nameof(GameSettlements), new { gameId });
    }

    // POST: /Settlements/SendReminder/5
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
