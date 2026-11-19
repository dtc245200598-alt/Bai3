# RealtimeSubmissionSystem — EduSubmit realtime (Bài 3, phần 3)

Hệ thống nộp bài có **REST làm nguồn sự thật** và **SignalR chỉ báo thay đổi**.
Máy chủ trả `201` ngay khi nhận bài, sau đó bộ chấm bài chạy nền và đẩy
`processing` → `graded` cho đúng nhóm `submission:{id}`.

## 1. Kiến trúc

```text
Máy khách                                       Máy chủ
─────────                                       ───────
POST /api/assignments/1/submissions  ────────►  tạo Submission (received)
        ◄──────── 201 + Location                lưu kho (có lock)
                                                Task.Delay 1s
                                                cập nhật processing ──┐
                                                                       │
POST /hubs/submissions/negotiate     ────────►  SignalR negotiate      │
GET  /hubs/submissions (WebSocket)   ◄───────►  SubmissionHub          │
Invoke WatchSubmission(2)            ────────►  join submission:2      │
                                                cập nhật graded ──────┘
        ◄──────── SubmissionStatusChanged       Clients.Group("submission:2")
        ◄──────── SubmissionStatusChanged
GET /api/submissions/2               ────────►  đọc lại trạng thái cuối
        ◄──────── 200 { status: "graded" }
```

- **REST**: tạo và đọc tài nguyên, là nguồn sự thật.
- **SignalR**: báo thay đổi, không lưu tin, không thay thế REST.
- **Group**: mỗi lượt nộp một nhóm `submission:{id}` để không gửi nhầm người.
- **IHubContext**: cho phép tác vụ nền (không phải hub) đẩy thông báo.

## 2. Cấu trúc

```text
RealtimeSubmissionSystem/
├─ RealtimeSubmissionSystem.sln
├─ requests.http                    # 10 request REST + negotiate
└─ src/
   ├─ Submission.Api/
   │  ├─ Program.cs                 # endpoint REST + MapHub
   │  ├─ Models/                    # Assignment, Submission, SubmissionStatus
   │  ├─ Contracts/                 # DTO request/response/push message
   │  ├─ Data/SubmissionStore.cs    # kho in-memory + lock
   │  ├─ Hubs/SubmissionHub.cs      # Watch/Unwatch + OnConnected
   │  └─ Services/GradingSimulator.cs # chấm nền + IHubContext
   └─ Submission.Client/
      └─ Program.cs                 # hai kênh: SignalR + REST
```

## 3. Chạy trong VS Code

Mở thư mục `Bai3`, dùng 2 terminal hoặc **Terminal → Run Task**:

```powershell
# Terminal 1 — máy chủ, chạy trước
dotnet run --project RealtimeSubmissionSystem/src/Submission.Api
# Now listening on: http://localhost:5000

# Terminal 2 — máy khách
dotnet run --project RealtimeSubmissionSystem/src/Submission.Client -- http://localhost:5000 SV001
```

Kết quả thật của máy khách:

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

Hai dòng `Thông báo SignalR` cách nhau khoảng 2 giây và máy khách **không hề hỏi**
trạng thái trong khoảng đó.

### Các cờ mô phỏng thí nghiệm

```powershell
# Thí nghiệm 5: Watch muộn 4 giây -> lỡ thông báo
dotnet run --project RealtimeSubmissionSystem/src/Submission.Client -- http://localhost:5000 SV005 --late-watch

# Thí nghiệm 4: sai tên sự kiện On -> im lặng
dotnet run --project RealtimeSubmissionSystem/src/Submission.Client -- http://localhost:5000 SV004 --wrong-event

# Thí nghiệm 3: sai đường dẫn hub -> 404
dotnet run --project RealtimeSubmissionSystem/src/Submission.Client -- http://localhost:5000 SV003 --wrong-hub
```

## 4. Bảy yêu cầu kiểm tra

Chạy máy chủ rồi dùng `requests.http` (extension **REST Client**) hoặc các lệnh dưới.

| # | Yêu cầu | Kết quả mong đợi | Kết quả thật |
|---|---|---|---|
| 1 | `GET /api/assignments` | 200 + danh sách | 200, 2 assignment |
| 2 | `POST /api/assignments/1/submissions` | 201 + `Location` | 201, `Location: /api/submissions/1` |
| 3 | `POST` thiếu `studentCode`, `repositoryUrl` | 400 | 400 + validation problem |
| 4 | `POST /api/assignments/999/submissions` | 404 | 404 `Assignment 999 không tồn tại` |
| 5 | `POST /hubs/submissions/negotiate` | 200 + `connectionId` | 200, đủ 3 transport |
| 6 | `GET` theo `Location` sau 5 giây | 200, `status: graded` | 200, `graded` |
| 7 | `POST :5200/hubs/ping/negotiate` | 200 | 200 (solution PingSignalR) |

```powershell
# 1
curl.exe -i http://localhost:5000/api/assignments

# 2 — dùng file JSON để tránh lỗi quoting của PowerShell
$body = '{"studentCode":"SV001","repositoryUrl":"https://github.com/sv001/bai3-signalr"}'
[IO.File]::WriteAllText("$env:TEMP\sub.json", $body, [Text.UTF8Encoding]::new($false))
curl.exe -i -X POST http://localhost:5000/api/assignments/1/submissions `
  -H "Content-Type: application/json" --data-binary "@$env:TEMP\sub.json"

# 3
curl.exe -i -X POST http://localhost:5000/api/assignments/1/submissions `
  -H "Content-Type: application/json" --data "{}"

# 4
curl.exe -i -X POST http://localhost:5000/api/assignments/999/submissions `
  -H "Content-Type: application/json" --data-binary "@$env:TEMP\sub.json"

# 5
curl.exe -i -X POST "http://localhost:5000/hubs/submissions/negotiate?negotiateVersion=1"

# 6 — thay 1 bằng id trong Location, chờ 5 giây
Start-Sleep 5
curl.exe -i http://localhost:5000/api/submissions/1
```

`requests.http` có 10 request: 200, 201, 400, 404, 404, 200, 404, negotiate
SubmissionHub và negotiate PingHub.

## 5. Năm thí nghiệm (dự đoán → kết quả thật → bài học)

| # | Thí nghiệm | Dự đoán | Kết quả thật | Bài học |
|---|---|---|---|---|
| 1 | Hai máy khách cùng lúc | Mỗi người chỉ thấy bài của mình | A nhận submission 3, B nhận submission 4; không lẫn | Nhóm `submission:{id}` tách người nhận |
| 2 | Máy chủ chưa chạy | Lỗi kết nối ngay | `HttpRequestException: ... actively refused` | Lần nối đầu không tự thử lại; phải bật server trước |
| 3 | Sai `/hub/submissions` | 404 ở negotiate | `404 (Not Found)` | `WithUrl` phải khớp `MapHub`, có `/hubs/` |
| 4 | Sai tên sự kiện `On` | Không lỗi, không nhận tin | 10 giây sau: `Quá thời gian...`; REST vẫn `graded` | Sai tên chiều xuống = im lặng |
| 5 | Watch muộn 4 giây | Lỡ thông báo | Không nhận tin nào; REST đọc lại `graded` | Vào nhóm muộn là lỡ tin; REST là nguồn sự thật |

Thứ tự là điểm mấu chốt: **POST → Watch → chờ push → GET chốt**. Nếu Watch trước khi
có id thì không biết nhóm nào; nếu Watch sau khi trạng thái đổi thì đã lỡ.

## 6. Ba câu hỏi đầu giờ

### 1. Vì sao máy chủ chủ động báo tốt hơn máy khách hỏi liên tục?

Máy chủ là nơi biết sự kiện xảy ra đầu tiên. Với 100 sinh viên chờ trong 1 phút, mỗi
bài đổi trạng thái 2 lần:

- Polling mỗi 2 giây: `100 × 30 = 3.000` yêu cầu, chỉ khoảng `200` yêu cầu (≈7%) mang
  tin mới, trễ tới 2 giây.
- Push qua kết nối mở: `100 × 2 = 200` thông báo, cả 200 đều có ích, đến ngay.

Push tiết kiệm đường truyền, giảm tải máy chủ và giảm độ trễ. Đổi lại phải giữ kết nối,
xử lý mất kết nối/nối lại và không được coi thông báo là dữ liệu bền vững.

### 2. REST và SignalR chia việc thế nào?

- **REST** tạo và đọc tài nguyên: `POST` nộp bài, `GET` danh sách/chi tiết. Đây là
  **nguồn sự thật**, dữ liệu vẫn đúng khi không có ai đang kết nối.
- **SignalR** chỉ báo *đã có thay đổi*: `processing`, `graded`. Không lưu tin, không
  thay thế REST.
- Khi mất kết nối, nối lại, hoặc Watch muộn: gọi `GET /api/submissions/{id}` để đồng bộ.
- Máy chủ cập nhật kho **trước**, rồi mới đẩy thông báo; nhờ vậy client nhận tin là
  đọc REST thấy ngay trạng thái mới.

### 3. Vì sao đặt thông báo đúng lúc trạng thái đổi (báo `graded`)?

Thông báo chỉ có nghĩa khi gắn với **sự kiện thật**: `received → processing → graded`.
Đặt ở lúc `graded` giúp:

- Không báo khi mới nộp (chưa có kết quả), không báo lặp mỗi lần bị hỏi.
- Chỉ gửi cho nhóm `submission:{id}` quan tâm, không phát cho toàn bộ kết nối.
- Gói tin mang đủ `Id`, `Status`, `UpdatedAt` để client biết bài nào, trạng thái nào,
  lúc nào — tránh mơ hồ.
- Nếu lỡ thông báo (mất mạng, Watch muộn), client vẫn đọc lại REST để lấy trạng thái
  cuối; thông báo chỉ là tín hiệu, không phải nguồn sự thật.

## 7. Giới hạn đã biết (và bài sau sẽ giải quyết)

| Giới hạn hôm nay | Hệ thật làm gì | Bài sau |
|---|---|---|
| `Task.Delay` giả lập chấm bài | Bộ chấm thật chạy riêng, nhận việc qua hàng đợi | Bài 6 — hàng đợi, worker |
| Máy chủ tự gọi `UpdateStatusAsync` | Worker phát sự kiện, nhiều dịch vụ cùng nghe | Bài 7 — event-driven, pub/sub |
| Nhóm nằm trong bộ nhớ một máy chủ | Nhiều bản sao cần backplane chung (Redis) | Bài 8 — vận hành, mở rộng |
| Ai cũng `Watch` được mọi id | Phải xác thực và kiểm tra quyền trước khi vào nhóm | Bài 8–9 — kiến trúc tầng |
| Kho in-memory, mất khi restart | Cơ sở dữ liệu + transaction | Bài 4–5 |

## 8. Minh chứng cần chụp

Đặt trong `../docs/screenshots/`:

- `edusubmit-requests.png` — `requests.http` với 200/201/400/404 và negotiate.
- `edusubmit-client.png` — client in 2 dòng `Thông báo SignalR` và `graded` từ REST.
- `edusubmit-thi-nghiem.png` — bảng 5 thí nghiệm hoặc ảnh từng thí nghiệm.
- `edusubmit-hub-logs.png` — log `[Hub] ... theo dõi submission:{id}` và `[Push] ...`.

## 9. Ghi chú

- Port mặc định 5000 nằm trong `src/Submission.Api/Properties/launchSettings.json`.
- Dữ liệu chỉ nằm trong bộ nhớ; khởi động lại máy chủ là reset.
- Project đích `net10.0`; nếu lớp dùng .NET 8, đổi `TargetFramework` và version gói
  `Microsoft.AspNetCore.SignalR.Client` tương ứng.
