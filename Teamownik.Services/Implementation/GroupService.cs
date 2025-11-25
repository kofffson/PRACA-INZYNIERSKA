using Microsoft.EntityFrameworkCore;
using Teamownik.Data;
using Teamownik.Data.Models;
using Teamownik.Services.Interfaces;

namespace Teamownik.Services.Implementation;

public class GroupService : IGroupService
{
    private readonly TeamownikDbContext _context;
    private readonly IEmailService _emailService;

    public GroupService(TeamownikDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<Group?> GetGroupByIdAsync(int groupId)
    {
        return await _context.Groups
            .AsNoTracking() 
            .Include(g => g.Creator)
            .Include(g => g.Members)
            .ThenInclude(m => m.User)
            .Include(g => g.Games)
            .ThenInclude(game => game.Organizer)
            .Include(g => g.Games)
            .ThenInclude(game => game.Participants)
            .Include(g => g.Messages)
            .ThenInclude(m => m.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(g => g.GroupId == groupId);
    }

    public async Task<IEnumerable<Group>> GetAllActiveGroupsAsync()
    {
        return await _context.Groups
            .Where(g => g.IsActive)
            .Include(g => g.Creator)
            .Include(g => g.Members)
            .OrderBy(g => g.GroupName)
            .ToListAsync();
    }

    public async Task<IEnumerable<Group>> GetUserGroupsAsync(string userId)
    {
        return await _context.Groups
            .AsNoTracking()
            .Where(g => g.IsActive && (g.Members.Any(m => m.UserId == userId) || g.CreatedBy == userId)) // DODAJ: g.IsActive &&
            .Include(g => g.Creator)
            .Include(g => g.Members)
            .Select(g => new Group
            {
                GroupId = g.GroupId,
                GroupName = g.GroupName,
                CreatedAt = g.CreatedAt,
                CreatedBy = g.CreatedBy,
                Creator = g.Creator,
                Members = g.Members,
                IsActive = g.IsActive
            })
            .OrderBy(g => g.GroupName)
            .ToListAsync();
    }

    public async Task<Group> CreateGroupAsync(Group group)
    {
        group.CreatedAt = DateTime.UtcNow;
        group.IsActive = true;
        
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        // Automatycznie dodaj twórcę jako członka
        await AddMemberAsync(group.GroupId, group.CreatedBy, isVIP: true);

        return group;
    }

    public async Task<bool> UpdateGroupAsync(Group group)
    {
        _context.Groups.Update(group);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> DeleteGroupAsync(int groupId)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group == null) return false;

        _context.Groups.Remove(group);
        return await _context.SaveChangesAsync() > 0;
    }
    // DODAJ tę metodę do IGroupService i GroupService
    public async Task<bool> CanDeactivateGroupAsync(int groupId)
    {
        var group = await _context.Groups
            .Include(g => g.Games)
            .FirstOrDefaultAsync(g => g.GroupId == groupId);
        
        if (group == null) return false;
    
        // Sprawdź czy są nadchodzące gry
        var upcomingGames = group.Games?.Any(g => 
            g.StartDateTime > DateTime.UtcNow && 
            (g.Status == "open" || g.Status == "full")) ?? false;
        
        return !upcomingGames;
    }
    // ZAKTUALIZUJ DeactivateGroupAsync
    public async Task<bool> DeactivateGroupAsync(int groupId)
    {
        try
        {
            // BEZPOŚREDNIE ZAPYTanie SQL - zawsze działa
            var result = await _context.Database
                .ExecuteSqlRawAsync(
                    "UPDATE \"Groups\" SET \"IsActive\" = false WHERE \"GroupId\" = {0}", 
                    groupId
                );
        
            return result > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> AddMemberAsync(int groupId, string userId, bool isVIP = false)
    {
        // Sprawdź czy już jest członkiem
        if (await IsUserMemberAsync(groupId, userId))
            return false;

        var member = new GroupMember
        {
            GroupId = groupId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            IsVIP = isVIP,
            GamesPlayed = 0
        };

        _context.GroupMembers.Add(member);
        var result = await _context.SaveChangesAsync() > 0;

        if (result)
        {
            var group = await GetGroupByIdAsync(groupId);
            var user = await _context.Users.FindAsync(userId);
            
            if (group != null && user != null)
            {
                await _emailService.SendGroupWelcomeAsync(
                    user.Email!,
                    user.FullName,
                    group.GroupName
                );
            }
        }

        return result;
    }

    public async Task<bool> RemoveMemberAsync(int groupId, string userId)
    {
        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

        if (member == null) return false;

        _context.GroupMembers.Remove(member);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> IsUserMemberAsync(int groupId, string userId)
    {
        return await _context.GroupMembers
            .AnyAsync(m => m.GroupId == groupId && m.UserId == userId);
    }

    public async Task<IEnumerable<GroupMember>> GetGroupMembersAsync(int groupId)
    {
        return await _context.GroupMembers
            .Where(m => m.GroupId == groupId)
            .Include(m => m.User)
            .OrderByDescending(m => m.IsVIP)
            .ThenBy(m => m.JoinedAt)
            .ToListAsync();
    }

    public async Task<int> GetMemberCountAsync(int groupId)
    {
        return await _context.GroupMembers
            .CountAsync(m => m.GroupId == groupId);
    }

    public async Task<bool> UpdateMemberVIPStatusAsync(int groupId, string userId, bool isVIP)
    {
        var member = await _context.GroupMembers
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

        if (member == null) return false;

        member.IsVIP = isVIP;
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<GroupInvitation> InviteMemberAsync(int groupId, string email, string invitedBy)
    {
        var token = Guid.NewGuid().ToString();
        
        var invitation = new GroupInvitation
        {
            GroupId = groupId,
            InvitedEmail = email,
            InvitedBy = invitedBy,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsAccepted = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.GroupInvitations.Add(invitation);
        await _context.SaveChangesAsync();

        var group = await GetGroupByIdAsync(groupId);
        var inviter = await _context.Users.FindAsync(invitedBy);
        
        if (group != null && inviter != null)
        {
            var invitationLink = $"https://yourapp.com/groups/accept-invitation?token={token}";
            await _emailService.SendGroupInvitationAsync(
                email,
                group.GroupName,
                inviter.FullName,
                invitationLink
            );
        }

        return invitation;
    }

    public async Task<bool> AcceptInvitationAsync(string token, string userId)
    {
        var invitation = await _context.GroupInvitations
            .Include(i => i.Group)
            .FirstOrDefaultAsync(i => i.Token == token && !i.IsAccepted);

        if (invitation == null) return false;
        if (invitation.ExpiresAt < DateTime.UtcNow) return false;

        var user = await _context.Users.FindAsync(userId);
        if (user == null || user.Email != invitation.InvitedEmail) return false;

        invitation.IsAccepted = true;
        await _context.SaveChangesAsync();

        await AddMemberAsync(invitation.GroupId, userId);

        return true;
    }

    public async Task<GroupInvitation?> GetInvitationByTokenAsync(string token)
    {
        return await _context.GroupInvitations
            .Include(i => i.Group)
            .Include(i => i.Inviter)
            .FirstOrDefaultAsync(i => i.Token == token);
    }

    public async Task<IEnumerable<GroupInvitation>> GetPendingInvitationsAsync(int groupId)
    {
        return await _context.GroupInvitations
            .Where(i => i.GroupId == groupId && !i.IsAccepted && i.ExpiresAt > DateTime.UtcNow)
            .Include(i => i.Inviter)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<GroupMessage> SendMessageAsync(int groupId, string userId, string message)
    {
        var groupMessage = new GroupMessage
        {
            GroupId = groupId,
            UserId = userId,
            MessageText = message,
            SentAt = DateTime.UtcNow
        };

        _context.GroupMessages.Add(groupMessage);
        await _context.SaveChangesAsync();

        return groupMessage;
    }

    public async Task<IEnumerable<GroupMessage>> GetGroupMessagesAsync(int groupId, int count = 50)
    {
        return await _context.GroupMessages
            .Where(m => m.GroupId == groupId)
            .Include(m => m.User)
            .OrderByDescending(m => m.SentAt)
            .Take(count)
            .ToListAsync();
    }
    public async Task<bool> ToggleVIPStatusAsync(int groupId, string userId, string currentUserId)
{
    var group = await _context.Groups
        .Include(g => g.Members)
        .FirstOrDefaultAsync(g => g.GroupId == groupId);
    
    if (group == null || group.CreatedBy != currentUserId) 
        return false;
    
    var member = group.Members.FirstOrDefault(m => m.UserId == userId);
    if (member == null) return false;
    
    member.IsVIP = !member.IsVIP;
    await _context.SaveChangesAsync();
    
    return true;
}
}