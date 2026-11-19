# Biên bản kiểm thử

- Ngày chạy: 19/11/2026
- Máy: Windows, .NET SDK `10.0.404`, runtime `Microsoft.AspNetCore.App 10.0.15`
- Lệnh build:

```powershell
dotnet build PingSignalR/PingSignalR.sln -v:minimal
dotnet build RealtimeSubmissionSystem/RealtimeSubmissionSystem.sln -v:minimal
```

Kết quả: **Build succeeded. 0 Warning(s). 0 Error(s)** cho cả hai.

## 1. EduSubmit — bảy yêu cầu kiểm tra

| # | Request | Kết quả |
|---|---|---|
| 1 | `GET http://localhost:5000/api/assignments` | `200`, 2 assignment |
| 2 | `POST /api/assignments/1/submissions` (đủ trường) | `201`, `Location: /api/submissions/1` |
| 3 | `POST /api/assignments/1/submissions` với `{}` | `400`, lỗi `studentCode`, `repositoryUrl` |
| 4 | `POST /api/assignments/999/submissions` | `404`, `Assignment 999 không tồn tại` |
| 5 | `POST /hubs/submissions/negotiate?negotiateVersion=1` | `200`, `connectionId` + 3 transport |
| 6 | `GET /api/submissions/1` sau 5 giây | `200`, `"status":"graded"` |
| 7 | `POST /hubs/ping/negotiate?negotiateVersion=1` (cổng 5200) | `200` |
| + | POST repo dùng `http://` | `201`, `"status":"failed"` — đã chấm bài |

Thực tế — hồ sơ bức xạ (có lược bỏ Date/Server/Content-Length):

```json
HTTP/1.1 201 Created
Location: /api/submissions/1

{"id":1,"assignmentId":1,"studentCode":"SV001",
 "repositoryUrl":"https://github.com/sv001/bai3-signalr",
 "status":"received", "createdAt":"2026-11-19T09:12:01.4723582+00:00", ...}
```

```json
HTTP/1.1 400 Bad Request
{"type":"https://tools.ietf.org/html/rfc9110#section-15.5.1",
 "title":"One or more validation errors occurred.","status":400,
 "errors":{"studentCode":["studentCode là bắt buộc."],
           "repositoryUrl":["repositoryUrl là bắt buộc."]}}
```

```json
HTTP/1.1 404 Not Found
{"error":"Assignment 999 không tồn tại."}
```

```json
HTTP/1.1 200 OK
{"id":1,"assignmentId":1,"studentCode":"SV001",
 "repositoryUrl":"https://github.com/sv001/bai3-signalr",
 "status":"graded", "createdAt":"...","updatedAt":"2026-11-19T09:12:04.4961099+00:00"}
```

## 2. PingSignalR

### 2.1. Negotiate thật

```json
{"negotiateVersion":1,"connectionId":"g4_5B5Qk9M0avwdAxbZLjQ",
 "availableTransports":[{"transport":"WebSockets"},
  {"transport":"ServerSentEvents"},
  {"transport":"LongPolling"}]}
```

### 2.2. Máy khách mặc định chạy đúng kịch bản slide (An / Bình)

An (ConnectionId `6rJ7vUjhy4ZMSDwXDrkYCQ`):

```text
[An] Đã kết nối SignalR. ConnectionId: 6rJ7vUjhy4ZMSDwXDrkYCQ
[Hệ thống] 6rJ7vUjhy4ZMSDwXDrkYCQ đã kết nối
[Echo] Echo: Xin chào SignalR
[Hệ thống] Bạn đã vào nhóm lop-hoc-01
[Nhóm] 6rJ7vUjhy4ZMSDwXDrkYCQ đã vào nhóm lop-hoc-01
[Broadcast] 6rJ7vUjhy4ZMSDwXDrkYCQ: An: Xin chào cả lớp
[Nhóm] [lop-hoc-01] 6rJ7vUjhy4ZMSDwXDrkYCQ: An: Tin nhắn cho nhóm lop-hoc-01
```

Bình (ConnectionId `uUxTKxuoWEfwP_H9fwhN4Q`) — thấy các tin của An nhưng không nhận
Echo của An:

```text
[Binh] Đã kết nối SignalR. ConnectionId: uUxTKxuoWEfwP_H9fwhN4Q
[Hệ thống] uUxTKxuoWEfwP_H9fwhN4Q đã kết nối
[Broadcast] 6rJ7vUjhy4ZMSDwXDrkYCQ: An: Xin chào cả lớp
[Nhóm] [lop-hoc-01] 6rJ7vUjhy4ZMSDwXDrkYCQ: An: Tin nhắn cho nhóm lop-hoc-01
[Nhóm] [lop-hoc-01] uUxTKxuoWEfwP_H9fwhN4Q: Binh: Tin nhắn cho nhóm lop-hoc-01
```

### 2.3. Tự nối lại (tắt server 2 giây, bật lại)

```text
Đang kết nối lại...
Đã kết nối lại. ConnectionId mới: IY4SGPF_3RxELEzCES9S6Q
```

Giá trị gốc: `Đang kết nối lại...` và ConnectionId mới khác với kết nối cũ.
Log server 2 ghi `[Hub] + IY4SGPF_3RxELEzCES9S6Q kết nối mới`.

### 2.4. Ba lab lỗi

```text
# lab hub
[Lỗi] Không kết nối được tới http://localhost:5200/hub/ping
       HttpRequestException: Response status code does not indicate success: 404 (Not Found).
       Bài học: WithUrl phải khớp MapHub, đúng /hubs/ping (có /hubs/).

# lab method
[Lỗi] HubException: Failed to invoke 'EchoWrong' due to an error on the server. HubException: Method does not exist.
       Bài học: tên trong InvokeAsync phải khớp tuyệt đối tên phương thức hub.

# lab event
[Lỗi] Không thấy EchoResponse: tên On(...) không khớp tên máy chủ gửi.
       Bài học: sai tên sự kiện chiều xuống = im lặng, không có exception.
```

## 3. EduSubmit — máy khách nhận

```text
Đã kết nối SignalR. ConnectionId: 88lQ99iLL1gXyWxoQ9hdIg
POST /api/assignments/1/submissions -> 201
Location: /api/submissions/3
Đã đăng ký theo dõi: submission:3
Đã gọi WatchSubmission(3)
Thông báo SignalR: submission 3 -> processing lúc 09:12:09
Thông báo SignalR: submission 3 -> graded lúc 09:12:11
Đã nhận đủ thông báo cho submission 3 (trạng thái cuối: graded).
REST GET /api/submissions/3 -> 200
Trạng thái cuối từ REST: graded (UpdatedAt: 09:12:11)
Hoàn tất: REST là nguồn sự thật, SignalR chỉ báo thay đổi.
```

## 4. EduSubmit — yêu cầu cộng điểm (repo `http://`)

```text
Đã kết nối SignalR. ConnectionId: gx29Qtlg309zvjwURUz2Fw
POST /api/assignments/1/submissions -> 201
Location: /api/submissions/4
Submission bị đánh dấu failed: repositoryUrl phải bắt đầu bằng https://.
Máy chủ không chấm bài nên không có thông báo SignalR.
REST GET /api/submissions/4 -> 200
Trạng thái cuối từ REST: failed (UpdatedAt: 09:12:11)
```

Server đã trả:
```json
{"id":4,"assignmentId":1,"studentCode":"SV006",
 "repositoryUrl":"http://github.com/sv006/bai3-signalr",
 "status":"failed", ...}
```

## 5. Thí nghiệm hai máy khách cùng lúc

```text
A: POST -> 201, Location: /api/submissions/7, REST -> graded
B: POST -> 201, Location: /api/submissions/8, REST -> graded
```

```text
[Push] submission 7 -> processing lúc 09:12:38
[Push] submission 8 -> processing lúc 09:12:38
[Push] submission 7 -> graded lúc 09:12:40
[Push] submission 8 -> graded lúc 09:12:40
```

Mỗi máy khách nhận đúng submission của mình.

## 6. Log Hub và Push của máy chủ (một máy chủ)

```text
[Hub] + shyeFzGKBOp5qX2_SAN9ZA kết nối mới
[Hub] + rl48r0cpEnLLe6xDTCplhQ kết nối mới
[Hub] shyeFzGKBOp5qX2_SAN9ZA theo doi submission:7
[Hub] rl48r0cpEnLLe6xDTCplhQ theo doi submission:8
[Push] submission 7 -> processing lúc 09:12:38
[Push] submission 8 -> processing lúc 09:12:38
[Push] submission 7 -> graded lúc 09:12:40
[Push] submission 8 -> graded lúc 09:12:40
```

## 7. Ghi chú

- Trong khi chạy tự động, client Ping được chạy bằng `--wait 30` để đủ thời gian nối
  lại khi server bị tắt/bật (tắt sau 6 giây, bật lại sau 3 giây). Trong terminal thật
  client chờ Enter.
- Client connect vào cổng ping hoặc 5000, cả hai được cấu hình như `launchSettings.json`.
- Trước khi chạy, đã dừng tiỿn trình Ping.Api cũ đang chiếm cổng 5200 của `C:\code\Bài 3`.
