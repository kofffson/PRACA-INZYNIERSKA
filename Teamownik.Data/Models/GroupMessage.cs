namespace Teamownik.Data.Models;

public class GroupMessage
{
    public int MessageId { get; set; }
    public int GroupId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string MessageText { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    public Group Group { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}