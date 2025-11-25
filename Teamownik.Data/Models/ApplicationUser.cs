using Microsoft.AspNetCore.Identity;

namespace Teamownik.Data.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public ICollection<GroupMember> GroupMemberships { get; set; } = new List<GroupMember>();
    public ICollection<Game> OrganizedGames { get; set; } = new List<Game>();
    public ICollection<GameParticipant> GameParticipations { get; set; } = new List<GameParticipant>();
    public ICollection<Settlement> PaymentsToMake { get; set; } = new List<Settlement>();
    public ICollection<Settlement> PaymentsToReceive { get; set; } = new List<Settlement>();
    public ICollection<GroupInvitation> SentInvitations { get; set; } = new List<GroupInvitation>();
    public ICollection<GroupMessage> Messages { get; set; } = new List<GroupMessage>();
    
    public string FullName => $"{FirstName} {LastName}";
}