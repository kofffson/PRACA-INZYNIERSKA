namespace Teamownik.Data.Models;

public class GameParticipant
{
    public int ParticipantId { get; set; }
    public int GameId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsReserve { get; set; } = false;
    public string Status { get; set; } = "confirmed";
    public int? WaitlistPosition { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAt { get; set; }
    public bool EmailSent { get; set; } = false;
    
    public Game Game { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}