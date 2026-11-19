using System.Text;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

try
{
    Console.OutputEncoding = Encoding.UTF8;
}
catch
{
    // Một số terminal không cho đổi encoding; bỏ qua để chương trình vẫn chạy.
}

// Khi stdout bị chuyển hướng, Console.OutputEncoding có thể không đặt được.
// Ghi thẳng UTF-8 để log tiếng Việt không bị mất dấu.
Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true });
Console.SetError(new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false)) { AutoFlush = true });

// Cách dùng:
//   dotnet run --project src/Ping.Client -- http://localhost:5200 An
//   dotnet run --project src/Ping.Client -- http://localhost:5200 Binh
//   dotnet run --project src/Ping.Client -- http://localhost:5200 An --interactive
//   dotnet run --project src/Ping.Client -- http://localhost:5200 An --lab hub|method|event
//   dotnet run --project src/Ping.Client -- http://localhost:5200 An --wait 20
var serverUrl = args.Length > 0 && !args[0].StartsWith("--")
    ? args[0].TrimEnd('/')
    : "http://localhost:5200";

var name = args.Length > 1 && !args[1].StartsWith("--")
    ? args[1]
    : "An";

var lab = GetOption(args, "--lab");

// Mặc định chạy đúng kịch bản trong slide: Echo -> JoinGroup -> Broadcast -> SendToGroup.
// Thêm --interactive nếu muốn tự gõ lệnh.
var interactive = args.Contains("--interactive");

// Chỉ dùng khi stdin bị chuyển hướng (chạy tự động/chụp minh chứng):
// giữ kết nối thêm N giây thay vì chờ Enter.
var waitSeconds = GetIntOption(args, "--wait", 8);

// Bài lab 1: cố ý dùng sai đường dẫn hub để nhận 404 ở bước negotiate.
var hubUrl = lab == "hub"
    ? $"{serverUrl}/hub/ping"
    : $"{serverUrl}/hubs/ping";

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

// Bài lab 3: cố ý đăng ký sai tên sự kiện -> máy chủ vẫn gửi nhưng máy khách im lặng.
var echoEventName = lab == "event" ? "EchoResponseWrong" : "EchoResponse";

connection.On<string>(echoEventName, message =>
    Console.WriteLine($"[Echo] {message}"));
connection.On<string>("BroadcastMessage", message =>
    Console.WriteLine($"[Broadcast] {message}"));
connection.On<string>("GroupMessage", message =>
    Console.WriteLine($"[Nhóm] {message}"));
connection.On<string>("SystemMessage", message =>
    Console.WriteLine($"[Hệ thống] {message}"));

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

try
{
    await connection.StartAsync();
}
catch (Exception ex)
{
    Console.WriteLine($"[Lỗi] Không kết nối được tới {hubUrl}");
    Console.WriteLine($"       {ex.GetType().Name}: {ex.Message}");
    if (lab == "hub")
    {
        Console.WriteLine("       Bài học: WithUrl phải khớp MapHub, đúng /hubs/ping (có /hubs/).");
    }

    return;
}

Console.WriteLine($"[{name}] Đã kết nối SignalR. ConnectionId: {connection.ConnectionId}");

// Bài lab 2: gọi sai tên phương thức -> HubException "Method does not exist".
if (lab == "method")
{
    try
    {
        await connection.InvokeAsync("EchoWrong", "Xin chào");
    }
    catch (HubException ex)
    {
        Console.WriteLine($"[Lỗi] HubException: {ex.Message}");
        Console.WriteLine("       Bài học: tên trong InvokeAsync phải khớp tuyệt đối tên phương thức hub.");
    }

    isStopping = true;
    await connection.DisposeAsync();
    return;
}

// Bài lab 3 (tiếp): gửi Echo nhưng không có handler đúng tên -> không thấy phản hồi.
if (lab == "event")
{
    await connection.InvokeAsync("Echo", "Xin chào SignalR");
    await Task.Delay(TimeSpan.FromSeconds(2));
    Console.WriteLine("[Lỗi] Không thấy EchoResponse: tên On(...) không khớp tên máy chủ gửi.");
    Console.WriteLine("       Bài học: sai tên sự kiện chiều xuống = im lặng, không có exception.");
    isStopping = true;
    await connection.DisposeAsync();
    return;
}

// Gửi thử một Echo ngay sau khi kết nối để thấy đúng phạm vi Caller.
await connection.InvokeAsync("Echo", "Xin chào SignalR");

if (!interactive)
{
    const string group = "lop-hoc-01";

    try
    {
        // Chờ một chút để máy khách thứ hai kịp kết nối và vào nhóm.
        await Task.Delay(TimeSpan.FromSeconds(2));
        await connection.InvokeAsync("JoinGroup", group);

        await Task.Delay(TimeSpan.FromSeconds(1));
        await connection.InvokeAsync("Broadcast", $"{name}: Xin chào cả lớp");

        await Task.Delay(TimeSpan.FromSeconds(1));
        await connection.InvokeAsync("SendToGroup", group, $"{name}: Tin nhắn cho nhóm {group}");

        Console.WriteLine("Đã gửi đủ Echo, JoinGroup, Broadcast, SendToGroup.");
        if (Console.IsInputRedirected)
        {
            await Task.Delay(TimeSpan.FromSeconds(waitSeconds));
        }
        else
        {
            Console.WriteLine("Nhấn Enter để thoát...");
            Console.ReadLine();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Lỗi] {ex.GetType().Name}: {ex.Message}");
        Console.WriteLine("       Kiểm tra máy chủ đang chạy đúng project Ping.Api của bài này.");
    }

    isStopping = true;
    await connection.DisposeAsync();
    return;
}

Console.WriteLine("Chế độ tương tác. Lệnh: echo <tin> | broadcast <tin> | join <nhóm> | group <nhóm> <tin> | help | exit");

while (true)
{
    var line = Console.ReadLine();
    if (line is null)
    {
        break;
    }

    line = line.Trim();
    if (line.Length == 0)
    {
        continue;
    }

    if (line.Equals("exit", StringComparison.OrdinalIgnoreCase) ||
        line.Equals("quit", StringComparison.OrdinalIgnoreCase))
    {
        break;
    }

    if (line.Equals("help", StringComparison.OrdinalIgnoreCase))
    {
        PrintHelp();
        continue;
    }

    var spaceIndex = line.IndexOf(' ');
    var command = (spaceIndex < 0 ? line : line[..spaceIndex]).ToLowerInvariant();
    var rest = spaceIndex < 0 ? string.Empty : line[(spaceIndex + 1)..].Trim();

    try
    {
        switch (command)
        {
            case "echo" when rest.Length > 0:
                await connection.InvokeAsync("Echo", rest);
                break;

            case "broadcast" when rest.Length > 0:
                await connection.InvokeAsync("Broadcast", $"{name}: {rest}");
                break;

            case "join" when rest.Length > 0:
                await connection.InvokeAsync("JoinGroup", rest);
                break;

            case "group" when rest.Length > 0:
                var groupSeparator = rest.IndexOf(' ');
                if (groupSeparator < 0)
                {
                    Console.WriteLine("Cú pháp: group <nhóm> <tin nhắn>");
                }
                else
                {
                    var groupName = rest[..groupSeparator];
                    var message = rest[(groupSeparator + 1)..];
                    await connection.InvokeAsync("SendToGroup", groupName, $"{name}: {message}");
                }

                break;

            default:
                Console.WriteLine("Lệnh không hợp lệ. Gõ 'help' để xem hướng dẫn.");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Lỗi] {ex.GetType().Name}: {ex.Message}");
    }
}

isStopping = true;
await connection.DisposeAsync();

static void PrintHelp()
{
    Console.WriteLine("""
        echo <tin>            -> chỉ mình bạn nhận EchoResponse (Clients.Caller)
        broadcast <tin>       -> mọi kết nối nhận BroadcastMessage (Clients.All)
        join <nhóm>           -> vào nhóm, cả nhóm nhận GroupMessage
        group <nhóm> <tin>    -> gửi tin cho thành viên nhóm (Clients.Group)
        exit                  -> thoát
        """);
}

static string? GetOption(string[] arguments, string optionName)
{
    var index = Array.IndexOf(arguments, optionName);
    return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
}

static int GetIntOption(string[] arguments, string optionName, int fallback)
{
    var value = GetOption(arguments, optionName);
    return int.TryParse(value, out var number) && number > 0 ? number : fallback;
}
