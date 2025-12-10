namespace Teamownik.Data.Models;

public class Game
{
    public int GameId { get; set; }
    public string GameName { get; set; } = string.Empty;
    public string OrganizerId { get; set; } = string.Empty;
    public int? GroupId { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public decimal Cost { get; set; } = 0;
    public bool IsPaid { get; set; } = false;
    public int MaxParticipants { get; set; }
    public bool IsRecurring { get; set; } = false;
    public string? RecurrencePattern { get; set; } 
    
    public Guid? RecurrenceSeriesId { get; set; } 

    public string Status { get; set; } = "open"; 
    public bool IsPublic { get; set; } = true;
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public ApplicationUser Organizer { get; set; } = null!;
    public Group? Group { get; set; }
    public ICollection<GameParticipant> Participants { get; set; } = new List<GameParticipant>();
    public ICollection<Settlement> Settlements { get; set; } = new List<Settlement>();
}