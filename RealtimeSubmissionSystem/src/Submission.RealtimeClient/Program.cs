using System.Net.Http.Json;
using System.Text;
using Microsoft.AspNetCore.SignalR.Client;

try
{
    Console.OutputEncoding = Encoding.UTF8;
}
catch
{
    // Bỏ qua nếu terminal không cho đổi encoding.
}

// Khi stdout bị chuyển hướng, Console.OutputEncoding có thể không đặt được.
// Ghi thẳng UTF-8 để log tiếng Việt không bị mất dấu.
Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true });
Console.SetError(new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false)) { AutoFlush = true });

// Cách dùng:
//   dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 SV001
//   dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 SV002 --late-watch
//   dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 SV003 --wrong-event
//   dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 SV004 --wrong-hub
//   dotnet run --project src/Submission.RealtimeClient -- http://localhost:5000 SV006 --http-repo
var baseUrl = args.Length > 0 && !args[0].StartsWith("--")
    ? args[0].TrimEnd('/')
    : "http://localhost:5000";

var studentCode = args.Length > 1 && !args[1].StartsWith("--")
    ? args[1]
    : "SV001";

var lateWatch = args.Contains("--late-watch");   // Thí nghiệm 5: vào nhóm muộn
var wrongEvent = args.Contains("--wrong-event"); // Thí nghiệm 4: sai tên sự kiện On
var wrongHub = args.Contains("--wrong-hub");     // Thí nghiệm 3: sai đường dẫn hub
var httpRepo = args.Contains("--http-repo");     // Yêu cầu cộng điểm: repo http:// -> failed

var hubUrl = wrongHub
    ? $"{baseUrl}/hub/submissions"
    : $"{baseUrl}/hubs/submissions";

var connection = new HubConnectionBuilder()
    .WithUrl(hubUrl)
    .WithAutomaticReconnect(new[]
    {
        TimeSpan.Zero,
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(10),
        TimeSpan.FromSeconds(30)
    })
    .Build();

var isStopping = false;

// graded hoặc failed đều là trạng thái kết thúc.
var finished = new TaskCompletionSource<SubmissionStatusMessage>(
    TaskCreationOptions.RunContinuationsAsynchronously);

var eventName = wrongEvent ? "SubmissionStatusChangedWrong" : "SubmissionStatusChanged";

connection.On<SubmissionStatusMessage>(eventName, message =>
{
    Console.WriteLine($"Thông báo SignalR: submission {message.Id} -> {message.Status} lúc {message.UpdatedAt:HH:mm:ss}");

    if (message.Status.Equals("graded", StringComparison.OrdinalIgnoreCase)
        || message.Status.Equals("failed", StringComparison.OrdinalIgnoreCase))
    {
        finished.TrySetResult(message);
    }
});

connection.On<string>("Watching", groupName =>
    Console.WriteLine($"Đã đăng ký theo dõi: {groupName}"));

connection.Reconnecting += _ =>
{
    Console.WriteLine("Đang kết nối lại...");
    return Task.CompletedTask;
};

connection.Reconnected += connectionId =>
{
    Console.WriteLine($"Đã kết nối lại. ConnectionId mới: {connectionId}");
    return Task.CompletedTask;
};

connection.Closed += error =>
{
    if (!isStopping)
    {
        Console.WriteLine($"Đã mất kết nối: {error?.Message ?? "không rõ nguyên nhân"}");
    }

    return Task.CompletedTask;
};

// ---------- Kênh 1: SignalR ----------
try
{
    await connection.StartAsync();
}
catch (Exception exception)
{
    Console.WriteLine($"Không kết nối được tới {hubUrl}");
    Console.WriteLine($"{exception.GetType().Name}: {exception.Message}");

    if (wrongHub)
    {
        Console.WriteLine("Bài học: lỗi 404 ở negotiate — WithUrl phải khớp MapHub, đúng /hubs/submissions.");
    }
    else
    {
        Console.WriteLine("Bài học: lần nối đầu không được tự thử lại — hãy chạy máy chủ trước.");
    }

    return;
}

Console.WriteLine($"Đã kết nối SignalR. ConnectionId: {connection.ConnectionId}");

// ---------- Kênh 2: REST ----------
using var http = new HttpClient { BaseAddress = new Uri($"{baseUrl}/") };

SubmissionDto? created = null;
string? location = null;

try
{
    var payload = new
    {
        studentCode,
        repositoryUrl = httpRepo
            ? $"http://github.com/{studentCode.ToLowerInvariant()}/bai3-demo"
            : $"https://github.com/{studentCode.ToLowerInvariant()}/bai3-demo"
    };

    var response = await http.PostAsJsonAsync("api/assignments/1/submissions", payload);
    Console.WriteLine($"POST /api/assignments/1/submissions -> {(int)response.StatusCode}");

    if (!response.IsSuccessStatusCode)
    {
        Console.WriteLine(await response.Content.ReadAsStringAsync());
        return;
    }

    created = await response.Content.ReadFromJsonAsync<SubmissionDto>();
    if (created is null)
    {
        Console.WriteLine("Không đọc được phản hồi POST.");
        return;
    }

    location = response.Headers.Location is { } header
        ? (header.IsAbsoluteUri ? header.PathAndQuery : header.ToString())
        : $"/api/submissions/{created.Id}";

    Console.WriteLine($"Location: {location}");
}
catch (HttpRequestException exception)
{
    Console.WriteLine($"HttpRequestException: {exception.Message}");
    Console.WriteLine("Bài học: REST không chạy thì không có gì để theo dõi.");
    return;
}

// Yêu cầu cộng điểm: repo không bắt đầu bằng https:// bị đánh dấu failed ngay từ POST.
// Khi đó máy chủ không chấm bài nên không có thông báo SignalR; chỉ cần đọc lại REST.
var failedFromPost = created.Status.Equals("failed", StringComparison.OrdinalIgnoreCase);

if (failedFromPost)
{
    Console.WriteLine("Submission bị đánh dấu failed: repositoryUrl phải bắt đầu bằng https://.");
    Console.WriteLine("Máy chủ không chấm bài nên không có thông báo SignalR.");
}
else
{
    // Thí nghiệm 5: cố ý Watch muộn 4 giây -> lỡ mọi thông báo, phải đọc lại REST.
    if (lateWatch)
    {
        Console.WriteLine("Thí nghiệm 5: chờ 4 giây mới Watch (sẽ lỡ thông báo)...");
        await Task.Delay(TimeSpan.FromSeconds(4));
    }

    await connection.InvokeAsync("WatchSubmission", created.Id);
    Console.WriteLine($"Đã gọi WatchSubmission({created.Id})");

    try
    {
        var message = await finished.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Console.WriteLine($"Đã nhận đủ thông báo cho submission {message.Id} (trạng thái cuối: {message.Status}).");
    }
    catch (TimeoutException)
    {
        Console.WriteLine("Quá thời gian chờ thông báo graded.");
    }
}

// Chốt lại bằng REST: SignalR không lưu tin, REST là nguồn sự thật.
try
{
    var response = await http.GetAsync(location);
    Console.WriteLine($"REST GET {location} -> {(int)response.StatusCode}");

    if (response.IsSuccessStatusCode)
    {
        var finalState = await response.Content.ReadFromJsonAsync<SubmissionDto>();
        Console.WriteLine($"Trạng thái cuối từ REST: {finalState?.Status} (UpdatedAt: {finalState?.UpdatedAt:HH:mm:ss})");
    }
}
catch (HttpRequestException exception)
{
    Console.WriteLine($"HttpRequestException khi đọc lại REST: {exception.Message}");
}

Console.WriteLine("Hoàn tất: REST là nguồn sự thật, SignalR chỉ báo thay đổi.");
isStopping = true;
await connection.DisposeAsync();

internal sealed record SubmissionDto(
    int Id,
    int AssignmentId,
    string StudentCode,
    string RepositoryUrl,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

internal sealed record SubmissionStatusMessage(
    int Id,
    string Status,
    DateTimeOffset UpdatedAt);
