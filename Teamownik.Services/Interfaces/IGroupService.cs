using Teamownik.Data.Models;

namespace Teamownik.Services.Interfaces;

public interface IGroupService
{
    // Podstawowe operacje CRUD
    Task<Group?> GetGroupByIdAsync(int groupId);
    Task<IEnumerable<Group>> GetAllActiveGroupsAsync();
    Task<IEnumerable<Group>> GetUserGroupsAsync(string userId);
    Task<Group> CreateGroupAsync(Group group);
    Task<bool> UpdateGroupAsync(Group group);
    Task<bool> DeleteGroupAsync(int groupId);
    Task<bool> DeactivateGroupAsync(int groupId);
    
    Task<bool> AddMemberAsync(int groupId, string userId, bool isVIP = false);
    Task<bool> RemoveMemberAsync(int groupId, string userId);
    Task<bool> IsUserMemberAsync(int groupId, string userId);
    Task<IEnumerable<GroupMember>> GetGroupMembersAsync(int groupId);
    Task<int> GetMemberCountAsync(int groupId);
    Task<bool> UpdateMemberVIPStatusAsync(int groupId, string userId, bool isVIP);
    
    Task<GroupInvitation> InviteMemberAsync(int groupId, string email, string invitedBy);
    Task<bool> AcceptInvitationAsync(string token, string userId);
    Task<GroupInvitation?> GetInvitationByTokenAsync(string token);
    Task<IEnumerable<GroupInvitation>> GetPendingInvitationsAsync(int groupId);
    
    Task<GroupMessage> SendMessageAsync(int groupId, string userId, string message);
    Task<IEnumerable<GroupMessage>> GetGroupMessagesAsync(int groupId, int count = 50);
    Task<bool> ToggleVIPStatusAsync(int groupId, string userId, string currentUserId);
}