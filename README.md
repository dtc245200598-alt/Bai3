# Bài 3 — Giao tiếp thời gian thực với SignalR

> **MSSV:** `<MSSV>` — **Họ và tên:** `<Họ và tên>`
>
> Thay hai chỗ trên bằng thông tin của bạn trước khi nộp.

Thư mục này gồm **hai solution** đúng yêu cầu bài nộp:

| Solution | Nội dung | Cổng |
|---|---|---|
| [`PingSignalR/`](PingSignalR/README.md) | Hub 4 phương thức, client console, tự nối lại, 3 lỗi cố ý | 5200 |
| [`RealtimeSubmissionSystem/`](RealtimeSubmissionSystem/README.md) | EduSubmit REST + `SubmissionHub` + chấm nền + client hai kênh | 5000 |

## 1. Yêu cầu môi trường

- .NET SDK **10.0** (đã kiểm tra với `10.0.404`, runtime `10.0.15`).
- VS Code + extension **C# Dev Kit** (`ms-dotnettools.csdevkit`) và **REST Client**
  (`humao.rest-client`).
- Không cần cài SQL Server/Redis: dữ liệu demo nằm trong bộ nhớ.

Mở **thư mục `Bai3`** trong VS Code (File → Open Folder) để các task và launch
configuration bên dưới hoạt động đúng đường dẫn.

## 2. Chạy nhanh

### Cách A — Terminal

```powershell
# ---- PingSignalR ----
dotnet run --project PingSignalR/src/Ping.Api
# Terminal khác:
dotnet run --project PingSignalR/src/Ping.Client -- http://localhost:5200 An
# Terminal thứ ba:
dotnet run --project PingSignalR/src/Ping.Client -- http://localhost:5200 Binh

# ---- EduSubmit ----
dotnet run --project RealtimeSubmissionSystem/src/Submission.Api
# Terminal khác:
dotnet run --project RealtimeSubmissionSystem/src/Submission.Client -- http://localhost:5000 SV001
```

### Cách B — VS Code Task

`Terminal → Run Task…` rồi chọn:

- `Build: cả hai solution`
- `Ping: máy chủ (5200)`
- `Ping: An`, `Ping: Bình`, `Ping: demo An + Bình`
- `Edu: máy chủ (5000)`
- `Edu: máy khách`, `Edu: khách Watch muộn`, `Edu: khách sai sự kiện`, `Edu: khách sai hub`

### Cách C — F5

`.vscode/launch.json` có sẵn cấu hình cho máy chủ và máy khách. Thứ tự:

1. F5 `Ping.Api` (hoặc `Submission.Api`), chờ log `Now listening on`.
2. F5 `Ping.Client - An` / `Ping.Client - Bình` (hoặc `Submission.Client`).
3. VS Code cho phép nhiều phiên debug song song; chọn phiên ở thanh Debug.

## 3. Đối chiếu thang điểm (10 điểm)

| Tiêu chí | Điểm | Minh chứng trong thư mục |
|---|---:|---|
| 1. Chạy được | 4 | Build 0 lỗi; 7 yêu cầu kiểm tra đạt; client Ping nhận Echo; client Edu nhận `graded` |
| 2. `requests.http` | 2 | 10 request: 200, 201, 400, 404, negotiate cả hai hub |
| 3. Giải thích | 2 | README trả lời 3 câu: push vs polling, REST/SignalR chia việc, báo đúng lúc `graded` |
| 4. Thí nghiệm | 1 | Bảng 5 thí nghiệm với dự đoán → kết quả thật → bài học |
| 5. Git & PR | 1 | Repo có commit theo bước, `.gitignore` loại `bin/`, `obj/`; PR theo mẫu |

Phần cộng điểm (`failed`, theo dõi 2 lượt nộp cùng lúc) **không làm** — bài đã đủ 10 điểm
theo các tiêu chí bắt buộc.

## 4. Minh chứng / ảnh cần chụp

Thư mục [`docs/screenshots/`](docs/screenshots/README.md) liệt kê tên tệp và nội dung.
Tóm tắt:

| Nhóm | Tệp |
|---|---|
| Ping bước 1–7 | `buoc1-build.png`, `buoc2-negotiate.png`, `buoc4-echo.png`, `buoc5-nhom.png`, `buoc6-noilai.png`, `buoc7-loi1.png`, `buoc7-loi2.png`, `buoc7-loi3.png` |
| EduSubmit | `edusubmit-requests.png`, `edusubmit-client.png`, `edusubmit-thi-nghiem.png`, `edusubmit-hub-logs.png` |

Mẹo chụp: dùng Windows Terminal, phóng to chữ, chụp đúng vùng terminal (Win + Shift + S).
Mỗi ảnh nên thấy cả **lệnh đã chạy** và **kết quả** để người chấm đối chiếu.

## 5. Nộp bài

```powershell
cd Bai3
git status                 # phải không thấy bin/, obj/
git log --oneline          # các commit theo bước
```

- Tạo PR vào repo lớp với tiêu đề: `Bai3 - <MSSV> - <Họ và tên>`.
- Chỉ sửa trong thư mục bài của mình, không commit `bin/`, `obj/`, `.vs/`.
- Nếu chưa cấu hình git:

```powershell
git config user.name  "<Họ và tên>"
git config user.email "<email>"
```

## 6. Ba câu trả lời ngắn (chi tiết trong README từng solution)

1. **Máy chủ chủ động báo** tốt hơn polling vì máy chủ biết sự kiện đầu tiên: 100 người
   chờ 1 phút chỉ cần 200 thông báo thay vì 3.000 yêu cầu; 100% thông báo có ích và
   không trễ 2 giây.
2. **REST** tạo/đọc tài nguyên và là nguồn sự thật; **SignalR** chỉ báo thay đổi, không
   lưu tin. Mất kết nối/Watch muộn thì gọi lại REST.
3. **Báo đúng lúc `graded`** (và `processing`) để thông báo gắn với thay đổi thật, gửi
   đúng nhóm `submission:{id}`, mang đủ id/trạng thái/thời điểm; không báo lặp, không
   phát cho người không liên quan.

## 7. Cấu trúc tổng thể

```text
Bai3/
├─ .vscode/                     # task + launch + extension gợi ý
├─ docs/screenshots/            # nơi đặt ảnh minh chứng
├─ PingSignalR/
│  ├─ PingSignalR.sln
│  ├─ README.md
│  └─ src/Ping.Api, src/Ping.Client
├─ RealtimeSubmissionSystem/
│  ├─ RealtimeSubmissionSystem.sln
│  ├─ requests.http
│  ├─ README.md
│  └─ src/Submission.Api, src/Submission.Client
└─ README.md                    # tệp này
```
