using System.Text;
using Ping.Api.Hubs;

// Ghi UTF-8 kể cả khi stdout bị chuyển hướng (chạy tự động, chụp minh chứng).
try
{
    Console.OutputEncoding = new UTF8Encoding(false);
}
catch
{
    // Không có console thật; SetOut bên dưới vẫn bảo đảm UTF-8.
}

Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), new UTF8Encoding(false)) { AutoFlush = true });
Console.SetError(new StreamWriter(Console.OpenStandardError(), new UTF8Encoding(false)) { AutoFlush = true });

var builder = WebApplication.CreateBuilder(args);

// Một dòng bắt buộc để SignalR có thể được inject vào ứng dụng.
builder.Services.AddSignalR();

var app = builder.Build();

app.MapGet("/", () => Results.Text(
    "PingSignalR đang chạy.\n" +
    "Hub: /hubs/ping\n" +
    "Kiểm tra bắt tay: POST /hubs/ping/negotiate?negotiateVersion=1"));

// Một dòng thứ hai: ánh xạ hub ra URL. Tên URL này phải khớp WithUrl ở máy khách.
app.MapHub<PingHub>("/hubs/ping");

app.Run();
