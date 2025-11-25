namespace Teamownik.Data.Models;

public class GroupMember
{
    public int GroupMemberId { get; set; }
    public int GroupId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsVIP { get; set; } = false;
    public int GamesPlayed { get; set; } = 0;
    public int GamesOrganized { get; set; } = 0;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastGamePlayed { get; set; }
    
    public Group Group { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}