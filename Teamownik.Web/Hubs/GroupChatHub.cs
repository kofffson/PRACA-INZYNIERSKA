using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using Teamownik.Services.Interfaces;

namespace Teamownik.Web.Hubs;

public class GroupChatHub : Hub
{
    private readonly IGroupService _groupService;

    public GroupChatHub(IGroupService groupService)
    {
        _groupService = groupService;
    }
    
    public async Task JoinGroup(string groupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupId);
    
    }


    public async Task SendMessageToGroup(int groupId, string messageText)
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var userName = Context.User?.Identity?.Name ?? "Anonim";
        
        if (string.IsNullOrEmpty(userId) || string.IsNullOrWhiteSpace(messageText))
            return;

        var message = await _groupService.SendMessageAsync(groupId, userId, messageText);
        
        var senderName = message.User?.FullName ?? message.User?.UserName ?? userName; 
        
        await Clients.Group(groupId.ToString()).SendAsync(
            "ReceiveMessage",
            senderName,
            message.MessageText,
            message.SentAt.ToString("HH:mm"),
            message.UserId 
        );
    }
}