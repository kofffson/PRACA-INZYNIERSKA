namespace Teamownik.Web.Models;

public class ProfileViewModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public int TotalGamesPlayed { get; set; }
    public int TotalGamesOrganized { get; set; }
    public int ActiveGroupsCount { get; set; }
    
    public string FullName => $"{FirstName} {LastName}";
}