using Microsoft.AspNetCore.SignalR;

namespace Ping.Api.Hubs;

/// <summary>
/// Hub nhỏ nhất minh họa 4 phạm vi gửi của SignalR:
/// Caller, All, Group và OnConnected/OnDisconnected.
/// </summary>
public sealed class PingHub : Hub
{
    /// <summary>Trả lời riêng người vừa gọi (Clients.Caller).</summary>
    public async Task Echo(string message)
    {
        await Clients.Caller.SendAsync("EchoResponse", $"Echo: {message}");
    }

    /// <summary>Phát cho mọi kết nối đang mở (Clients.All).</summary>
    public async Task Broadcast(string message)
    {
        await Clients.All.SendAsync("BroadcastMessage", $"{Context.ConnectionId}: {message}");
    }

    /// <summary>Đưa kết nối hiện tại vào nhóm và báo cho cả nhóm (Clients.Group).</summary>
    public async Task JoinGroup(string groupName)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("SystemMessage", $"Bạn đã vào nhóm {groupName}");
        await Clients.Group(groupName).SendAsync("GroupMessage", $"{Context.ConnectionId} đã vào nhóm {groupName}");
    }

    /// <summary>Gửi tin cho toàn bộ thành viên của một nhóm (Clients.Group).</summary>
    public async Task SendToGroup(string groupName, string message)
    {
        await Clients.Group(groupName).SendAsync("GroupMessage", $"[{groupName}] {Context.ConnectionId}: {message}");
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[Hub] + {Context.ConnectionId} kết nối mới");
        await Clients.All.SendAsync("SystemMessage", $"{Context.ConnectionId} đã kết nối");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[Hub] - {Context.ConnectionId} ngắt kết nối");
        await Clients.All.SendAsync("SystemMessage", $"{Context.ConnectionId} đã ngắt kết nối");
        await base.OnDisconnectedAsync(exception);
    }
}
