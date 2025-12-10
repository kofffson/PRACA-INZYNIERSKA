using Microsoft.EntityFrameworkCore;
using Teamownik.Data;
using Teamownik.Data.Models;
using Teamownik.Services.Interfaces;

namespace Teamownik.Services.Implementation;

public class GroupService : IGroupService
{
    private readonly TeamownikDbContext _context;

    public GroupService(TeamownikDbContext context)
    {
        _context = context;
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
            .AsNoTracking()
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
            .Where(g => g.IsActive && (g.Members.Any(m => m.UserId == userId) || g.CreatedBy == userId))
            .Include(g => g.Creator)
            .Include(g => g.Members)
            .OrderBy(g => g.GroupName)
            .ToListAsync();
    }

    public async Task<Group> CreateGroupAsync(Group group)
    {
        group.CreatedAt = DateTime.UtcNow;
        group.IsActive = true;
        
        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

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
    
    public async Task<bool> DeactivateGroupAsync(int groupId)
    {
        try
        {
            var result = await _context.Database
                .ExecuteSqlInterpolatedAsync(
                    $"UPDATE \"Groups\" SET \"IsActive\" = false WHERE \"GroupId\" = {groupId}"
                );
        
            return result > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> AddMemberAsync(int groupId, string userId, bool isVIP = false)
    {
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
        return await _context.SaveChangesAsync() > 0;
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
            .AsNoTracking()
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
            .AsNoTracking()
            .Where(m => m.GroupId == groupId)
            .Include(m => m.User)
            .OrderByDescending(m => m.SentAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<bool> ToggleVIPStatusAsync(int groupId, string userId, string currentUserId)
    {
        var member = await _context.GroupMembers
            .Include(m => m.Group)
            .FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);
        
        if (member == null || member.Group.CreatedBy != currentUserId) 
            return false;
        
        member.IsVIP = !member.IsVIP;
        await _context.SaveChangesAsync();
        
        return true;
    }
}