namespace Teamownik.Data.Models;

public class GameSettlementSummary
{
    public int GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int TotalParticipants { get; set; }
    public int PaidCount { get; set; }
    public int UnpaidCount { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalOutstanding { get; set; }
    public List<SettlementDetail> Settlements { get; set; } = [];
}