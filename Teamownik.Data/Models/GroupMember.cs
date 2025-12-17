namespace Teamownik.Data.Models;

public class GroupMember
{
    public int GroupMemberId { get; set; }
    public int GroupId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public bool IsVIP { get; set; }
    public int GamesPlayed { get; set; }
    public int GamesOrganized { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastGamePlayed { get; set; }
    
    public Group Group { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}