# Biên bản kiểm thử

- Ngày chạy: 19/11/2026
- Máy: Windows, .NET SDK `10.0.404`, runtime `Microsoft.AspNetCore.App 10.0.15`
- Lệnh build:

```powershell
dotnet build PingSignalR/PingSignalR.sln -v:minimal
dotnet build RealtimeSubmissionSystem/RealtimeSubmissionSystem.sln -v:minimal
```

Kết quả:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## 1. Bảy yêu cầu kiểm tra

| # | Request | Kết quả |
|---|---|---|
| 1 | `GET http://localhost:5000/api/assignments` | `200`, 2 assignment |
| 2 | `POST /api/assignments/1/submissions` (đủ trường) | `201`, `Location: /api/submissions/1` |
| 3 | `POST /api/assignments/1/submissions` với `{}` | `400`, lỗi `studentCode`, `repositoryUrl` |
| 4 | `POST /api/assignments/999/submissions` | `404`, `Assignment 999 không tồn tại` |
| 5 | `POST /hubs/submissions/negotiate?negotiateVersion=1` | `200`, `connectionId`, 3 transport |
| 6 | `GET /api/submissions/1` sau 5 giây | `200`, `"status":"graded"` |
| 7 | `POST http://localhost:5200/hubs/ping/negotiate?negotiateVersion=1` | `200` |

Ghi chú: lúc kiểm thử, cổng 5200 đang bị một tiến trình `Ping.Api.exe` khác chiếm,
nên PingSignalR được chạy ở **5210** để kiểm tra. Cấu hình mặc định của bài vẫn là
**5200** (`launchSettings.json` và README). Trước khi chấm, tắt tiến trình cũ đó.

Kết quả thật của POST + GET:

```json
HTTP/1.1 201 Created
Location: /api/submissions/1

{"id":1,"assignmentId":1,"studentCode":"SV001",
 "repositoryUrl":"https://github.com/sv001/bai3-signalr",
 "status":"received", ...}
```

```json
HTTP/1.1 200 OK

{"id":1,"assignmentId":1,"studentCode":"SV001",
 "repositoryUrl":"https://github.com/sv001/bai3-signalr",
 "status":"graded", "updatedAt":"2026-11-19T08:37:48.5285656+00:00"}
```

## 2. PingSignalR

Hai máy khách An/Bình chạy `--demo`:

```text
[An] Đã kết nối SignalR. ConnectionId: kPjz6pj8dPs1zUbvnxZ8Yg
[Echo] Echo: Xin chào SignalR
[Hệ thống] Bạn đã vào nhóm lop-hoc-01
[Nhóm] kPjz6pj8dPs1zUbvnxZ8Yg đã vào nhóm lop-hoc-01
[Broadcast] kPjz6pj8dPs1zUbvnxZ8Yg: An: Xin chào cả lớp
[Nhóm] [lop-hoc-01] kPjz6pj8dPs1zUbvnxZ8Yg: An: Tin nhắn cho nhóm lop-hoc-01
```

```text
[Binh] Đã kết nối SignalR. ConnectionId: GTMosZwhMCrTZX5bND4ZLA
[Echo] Echo: Xin chào SignalR
[Nhóm] kPjz6pj8dPs1zUbvnxZ8Yg đã vào nhóm lop-hoc-01
[Broadcast] kPjz6pj8dPs1zUbvnxZ8Yg: An: Xin chào cả lớp
[Nhóm] [lop-hoc-01] GTMosZwhMCrTZX5bND4ZLA: Binh: Tin nhắn cho nhóm lop-hoc-01
```

Bình nhận tin broadcast/nhóm của An, nhưng không nhận `[Echo]` của An.

Tự nối lại (tắt server khoảng 2 giây rồi bật lại):

```text
Đang kết nối lại...
Đã kết nối lại. ConnectionId mới: cPVL4WbaxlZw2gTOVFboFg
[Hệ thống] cPVL4WbaxlZw2gTOVFboFg đã kết nối
```

Ba lab lỗi:

```text
# --lab hub
HttpRequestException: ... 404 (Not Found).

# --lab method
HubException: Failed to invoke 'EchoWrong' ... Method does not exist.

# --lab event
[Lỗi] Không thấy EchoResponse: tên On(...) không khớp tên máy chủ gửi.
```

## 3. EduSubmit

Máy khách chuẩn:

```text
Đã kết nối SignalR. ConnectionId: 5PIMDu-m1tGDtFGcwfO4PA
POST /api/assignments/1/submissions -> 201
Location: /api/submissions/1
Đã đăng ký theo dõi: submission:1
Đã gọi WatchSubmission(1)
Thông báo SignalR: submission 1 -> processing lúc 08:40:08
Thông báo SignalR: submission 1 -> graded lúc 08:40:10
Đã nhận đủ thông báo cho submission 1.
REST GET /api/submissions/1 -> 200
Trạng thái cuối từ REST: graded (UpdatedAt: 08:40:10)
Hoàn tất: REST là nguồn sự thật, SignalR chỉ báo thay đổi.
```

Hai thông báo cách nhau 2 giây; máy khách không gọi REST trong khoảng đó.

Watch muộn 4 giây:

```text
Thí nghiệm 5: chờ 4 giây mới Watch (sẽ lỡ thông báo)...
Đã đăng ký theo dõi: submission:2
Quá thời gian chờ thông báo graded.
REST GET /api/submissions/2 -> 200
Trạng thái cuối từ REST: graded (UpdatedAt: 08:40:13)
```

Hai máy khách cùng lúc: A nhận submission 3, B nhận submission 4, không lẫn.

Sai hub:

```text
HttpRequestException: ... 404 (Not Found).
Bài học: lỗi 404 ở negotiate — WithUrl phải khớp MapHub, đúng /hubs/submissions.
```

Sai tên sự kiện:

```text
Quá thời gian chờ thông báo graded.
Trạng thái cuối từ REST: graded
```

Server không chạy:

```text
HttpRequestException: No connection could be made because the target machine actively refused it.
```
