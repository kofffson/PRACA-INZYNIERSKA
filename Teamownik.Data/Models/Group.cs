namespace Teamownik.Data.Models;

public class Group
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
    public ApplicationUser Creator { get; set; } = null!;
    public ICollection<GroupMember> Members { get; set; } = new List<GroupMember>();
    public ICollection<Game> Games { get; set; } = new List<Game>();
    public ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();
}