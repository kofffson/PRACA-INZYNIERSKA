using System.ComponentModel.DataAnnotations;
using Teamownik.Data.Models;

namespace Teamownik.Web.Models;

public class GroupsIndexViewModel
{
    public List<GroupCardViewModel> UserGroups { get; set; } = new();
}

public class GroupCardViewModel
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string CreatorName { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public bool IsCreator { get; set; }
    public DateTime CreatedAt { get; set; }
}


public class GroupDetailsViewModel
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string CreatorName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsCreator { get; set; }
    public int MemberCount { get; set; }
    public string CurrentUserId { get; set; } = string.Empty;
    
    public List<GroupMemberViewModel> Members { get; set; } = new();
    public List<Game> UpcomingGames { get; set; } = new();
    public List<GroupMessage> RecentMessages { get; set; } = new();
    
    public int TotalGamesPlayed => Members.Sum(m => m.GamesPlayed);
    public int TotalGamesOrganized => Members.Sum(m => m.GamesOrganized);
    public int VIPCount => Members.Count(m => m.IsVIP);
}

public class GroupMemberViewModel
{
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public bool IsVIP { get; set; }
    public DateTime JoinedAt { get; set; }
    public int GamesPlayed { get; set; }
    
    public int GamesOrganized { get; set; }
    
    public string JoinedAgo 
    {
        get
        {
            var days = (DateTime.Now - JoinedAt).TotalDays;
            if (days < 1) return "Dziś";
            if (days < 30) return $"{(int)days}d temu";
            if (days < 365) return $"{(int)(days / 30)}m temu";
            return $"{(int)(days / 365)}r temu";
        }
    }
    
    public string StatsSummary 
    {
        get
        {
            return $"{GamesPlayed} rozegranych • {GamesOrganized} zorganizowanych";
        }
    }
    
    public bool CanToggleVIP { get; set; }
}

public class CreateGroupViewModel
{
    [Required(ErrorMessage = "Nazwa grupy jest wymagana")]
    [StringLength(100, ErrorMessage = "Nazwa może mieć maksymalnie 100 znaków")]
    [Display(Name = "Nazwa grupy")]
    public string GroupName { get; set; } = string.Empty;
}
