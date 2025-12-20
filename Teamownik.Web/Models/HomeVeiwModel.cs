namespace Teamownik.Web.Models
{
    public class HomeViewModel
    {
        public int ActiveGroupsCount { get; set; }
        public int UnpaidSettlementsCount { get; set; }
        public int UpcomingGamesCount { get; set; }
        public int TotalUsersInGroups { get; set; }

        public string UserFirstName { get; set; } = string.Empty;
        public string UserLastName { get; set; } = string.Empty;
        public string UserFullName => $"{UserFirstName} {UserLastName}";

        public List<GameCardViewModel> MyOrganizedGames { get; set; } = [];

        public List<GameCardViewModel> MyParticipatingGames { get; set; } = [];

        public List<GameCardViewModel> MyGroupGames { get; set; } = [];

        public List<GameCardViewModel> PublicGames { get; set; } = [];
    }
}