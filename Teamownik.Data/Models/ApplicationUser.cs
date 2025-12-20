using Microsoft.AspNetCore.Identity;

namespace Teamownik.Data.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<GroupMember> GroupMemberships { get; set; } = [];
    public ICollection<Game> OrganizedGames { get; set; } = [];
    public ICollection<GameParticipant> GameParticipations { get; set; } = [];
    public ICollection<Settlement> PaymentsToMake { get; set; } = [];
    public ICollection<Settlement> PaymentsToReceive { get; set; } = [];
    public ICollection<GroupMessage> Messages { get; set; } = [];
    public string FullName => $"{FirstName} {LastName}";
}