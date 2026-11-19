using Ping.Api.Hubs;

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
