namespace Teamownik.Data.Models;

public class GroupInvitation
{
    public int InvitationId { get; set; }
    public int GroupId { get; set; }
    public string InvitedEmail { get; set; } = string.Empty;
    public string InvitedBy { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsAccepted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public Group Group { get; set; } = null!;
    public ApplicationUser Inviter { get; set; } = null!;
}