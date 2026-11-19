# PingSignalR — Bài 3, phần 2

Solution nhỏ nhất để thấy SignalR hoạt động: một hub 4 phương thức, một client console
có tự động kết nối lại, và ba bài lab lỗi cố ý.

## 1. Cấu trúc

```text
PingSignalR/
├─ PingSignalR.sln
└─ src/
   ├─ Ping.Api/                 # ASP.NET Core + SignalR hub
   │  ├─ Program.cs             # AddSignalR + MapHub
   │  └─ Hubs/PingHub.cs        # Echo, Broadcast, JoinGroup, SendToGroup
   └─ Ping.Client/              # Console client dùng Microsoft.AspNetCore.SignalR.Client
      └─ Program.cs             # On(...), InvokeAsync(...), WithAutomaticReconnect(...)
```

## 2. Chạy nhanh trong VS Code

Mở thư mục `Bai3` bằng VS Code, mở 3 terminal (`` Ctrl+` `` rồi bấm dấu `+`):

```powershell
# Terminal 1 — máy chủ, chạy trước
dotnet run --project PingSignalR/src/Ping.Api
# log: Now listening on: http://localhost:5200

# Terminal 2 — An
dotnet run --project PingSignalR/src/Ping.Client -- http://localhost:5200 An

# Terminal 3 — Bình
dotnet run --project PingSignalR/src/Ping.Client -- http://localhost:5200 Binh
```

Hoặc dùng **Terminal → Run Task** và chọn các task `Ping: ...` đã cấu hình sẵn
trong `.vscode/tasks.json`. Có thể bấm **F5** với cấu hình `Ping.Api` / `Ping.Client`
trong `.vscode/launch.json`.

### Lệnh tương tác ở máy khách

```text
echo <tin>            -> chỉ mình bạn nhận EchoResponse (Clients.Caller)
broadcast <tin>       -> mọi kết nối nhận BroadcastMessage (Clients.All)
join <nhóm>           -> vào nhóm, cả nhóm nhận GroupMessage
group <nhóm> <tin>    -> gửi tin cho thành viên nhóm (Clients.Group)
help / exit
```

Ví dụ demo nhanh (không cần gõ tay):

```powershell
# Terminal 2
dotnet run --project PingSignalR/src/Ping.Client -- http://localhost:5200 An --demo

# Terminal 3 (chạy sau khoảng 1–2 giây)
dotnet run --project PingSignalR/src/Ping.Client -- http://localhost:5200 Binh --demo
```

## 3. Bốn phạm vi gửi của hub

| Phương thức client gọi | Tên sự kiện server gửi | Phạm vi | Ai nhận |
|---|---|---|---|
| `Echo(message)` | `EchoResponse` | `Clients.Caller` | Chỉ người vừa gọi |
| `Broadcast(message)` | `BroadcastMessage` | `Clients.All` | Mọi kết nối đang mở |
| `JoinGroup(groupName)` | `GroupMessage` | `Clients.Group` | Thành viên nhóm |
| `SendToGroup(group, msg)` | `GroupMessage` | `Clients.Group` | Thành viên nhóm |
| (tự động) | `SystemMessage` | `Clients.All` | Mọi kết nối |

Tên ở hai chiều phải khớp tuyệt đối:

- `SendAsync("EchoResponse", ...)` ở server ↔ `On("EchoResponse", ...)` ở client.
- `InvokeAsync("Echo", ...)` ở client ↔ `Task Echo(...)` ở hub.

## 4. Bảy bước và minh chứng

| Bước | Việc làm | Kết quả phải thấy | Ảnh |
|---|---|---|---|
| 1 | Tạo solution + 2 project, thêm gói SignalR.Client, build | `Build succeeded. 0 Error(s)` | `buoc1-build.png` |
| 2 | Server chỉ thêm `AddSignalR()` và `MapHub<PingHub>("/hubs/ping")` | `POST .../negotiate` trả 200 + `connectionId` | `buoc2-negotiate.png` |
| 3–4 | `Echo` trả riêng người gọi; client `On` rồi `InvokeAsync` | `[Echo] Echo: Xin chào SignalR` | `buoc4-echo.png` |
| 5 | Thêm Broadcast, JoinGroup, SendToGroup; chạy An + Bình | Hai máy khách thấy tin của nhau; Echo chỉ về người gửi | `buoc5-nhom.png` |
| 6 | Tắt server, chờ, bật lại | `Đang kết nối lại...` rồi `Đã kết nối lại. ConnectionId mới: ...` | `buoc6-noilai.png` |
| 7 | Ba lỗi cố ý (bên dưới) | Mỗi lỗi một kiểu: 404 / HubException / im lặng | `buoc7-loi1.png` … `loi3.png` |

### Kiểm tra bước 2 bằng curl

```powershell
curl.exe -i -X POST "http://localhost:5200/hubs/ping/negotiate?negotiateVersion=1"
```

Kết quả thật:

```text
HTTP/1.1 200 OK
Content-Type: application/json

{"negotiateVersion":1,"connectionId":"...","connectionToken":"...",
 "availableTransports":[
   {"transport":"WebSockets","transferFormats":["Text","Binary"]},
   {"transport":"ServerSentEvents","transferFormats":["Text"]},
   {"transport":"LongPolling","transferFormats":["Text","Binary"]}]}
```

### Kiểm tra bước 5

Ở An gõ:

```text
join lop-hoc-01
broadcast An: Xin chào cả lớp
group lop-hoc-01 An: Tin nhắn nhóm
```

Ở Bình cũng `join lop-hoc-01`. Kết quả thật (đã rút gọn ConnectionId):

```text
[An] Đã kết nối SignalR. ConnectionId: kPjz...
[Hệ thống] kPjz... đã kết nối
[Echo] Echo: Xin chào SignalR
[Hệ thống] Bạn đã vào nhóm lop-hoc-01
[Nhóm] kPjz... đã vào nhóm lop-hoc-01
[Broadcast] kPjz...: An: Xin chào cả lớp
[Nhóm] [lop-hoc-01] kPjz...: An: Tin nhắn cho nhóm lop-hoc-01
```

Bình nhận được `Broadcast`/`GroupMessage` của An nhưng **không** nhận `[Echo]` của An.

### Kiểm tra bước 6 — mất kết nối và tự nối lại

1. Giữ terminal An đang chạy.
2. Terminal 1: `Ctrl+C` để tắt server.
3. Chờ vài giây rồi chạy lại `dotnet run --project PingSignalR/src/Ping.Api`.
4. Quan sát An:

```text
Đang kết nối lại...
Đã kết nối lại. ConnectionId mới: cPVL4WbaxlZw2gTOVFboFg
[Hệ thống] cPVL4WbaxlZw2gTOVFboFg đã kết nối
```

Lịch thử lại được khai báo trong client: `0s, 2s, 10s, 30s`. ConnectionId **đổi** sau
mỗi lần nối lại — vì vậy nhóm cũ không còn hiệu lực, phải `JoinGroup` lại nếu cần.

## 5. Ba bài lab lỗi cố ý

Client có cờ `--lab` để tái hiện lỗi mà không phải sửa code:

```powershell
# Lỗi 1: sai đường dẫn hub -> 404 ở negotiate
dotnet run --project PingSignalR/src/Ping.Client -- http://localhost:5200 An --lab hub

# Lỗi 2: sai tên phương thức -> HubException: Method does not exist
dotnet run --project PingSignalR/src/Ping.Client -- http://localhost:5200 An --lab method

# Lỗi 3: sai tên sự kiện On -> im lặng, không exception
dotnet run --project PingSignalR/src/Ping.Client -- http://localhost:5200 An --lab event
```

Kết quả thật:

```text
# lab hub
[Lỗi] Không kết nối được tới http://localhost:5200/hub/ping
       HttpRequestException: Response status code does not indicate success: 404 (Not Found).
       Bài học: WithUrl phải khớp MapHub, đúng /hubs/ping (có /hubs/).

# lab method
[Lỗi] HubException: Failed to invoke 'EchoWrong' due to an error on the server.
       HubException: Method does not exist.
       Bài học: tên trong InvokeAsync phải khớp tuyệt đối tên phương thức hub.

# lab event
[Lỗi] Không thấy EchoResponse: tên On(...) không khớp tên máy chủ gửi.
       Bài học: sai tên sự kiện chiều xuống = im lặng, không có exception.
```

## 6. Vì sao chọn mô hình đẩy?

Với 100 sinh viên cùng chờ trong 1 phút, mỗi bài đổi trạng thái 2 lần:

| Cách | Thông điệp/phút | Có ích | Nhận xét |
|---|---:|---:|---|
| Polling mỗi 2 giây | 100 × 30 = 3.000 yêu cầu | ≈ 200 (7%) | Tốn đường truyền, trễ tới 2 giây |
| Server push | 100 × 2 = 200 thông báo | 200 (100%) | Đến ngay, đúng người |

Máy chủ là nơi **biết sự kiện xảy ra đầu tiên**, nên để máy chủ chủ động báo là hợp lý.
SignalR không lưu tin: mất kết nối/vào nhóm muộn thì phải đọc lại REST.

## 7. Ghi chú

- Port mặc định 5200 nằm trong `src/Ping.Api/Properties/launchSettings.json`.
- Nếu 5200 đang bị chiếm, đổi `applicationUrl` và URL truyền cho client.
- Ảnh minh chứng đặt trong `../docs/screenshots/` theo tên ở bảng bước 4.
