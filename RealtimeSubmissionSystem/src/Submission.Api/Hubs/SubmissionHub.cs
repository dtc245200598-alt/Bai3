using Microsoft.AspNetCore.SignalR;

namespace EduSubmit.Api.Hubs;

/// <summary>
/// Hub chỉ làm một việc: đưa kết nối vào nhóm submission:{id}
/// để máy chủ chấm bài đẩy thông báo đúng người quan tâm.
/// </summary>
public sealed class SubmissionHub : Hub
{
    public async Task WatchSubmission(int submissionId)
    {
        var groupName = GroupName(submissionId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("Watching", groupName);
        Console.WriteLine($"[Hub] {Context.ConnectionId} theo dõi {groupName}");
    }

    public async Task UnwatchSubmission(int submissionId)
    {
        var groupName = GroupName(submissionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("Unwatched", groupName);
        Console.WriteLine($"[Hub] {Context.ConnectionId} bỏ theo dõi {groupName}");
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[Hub] + {Context.ConnectionId} kết nối mới");
        await Clients.Caller.SendAsync("SystemMessage", $"Đã kết nối SubmissionHub: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[Hub] - {Context.ConnectionId} ngắt kết nối");
        await base.OnDisconnectedAsync(exception);
    }

    private static string GroupName(int submissionId) => $"submission:{submissionId}";
}
