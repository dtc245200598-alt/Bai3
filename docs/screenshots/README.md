# Ảnh minh chứng

Đặt ảnh PNG chụp terminal vào chính thư mục này. Người chấm cần thấy **cả lệnh và kết quả**,
không cần thấy toàn màn hình.

## PingSignalR

| Tệp | Chụp gì |
|---|---|
| `buoc1-build.png` | Terminal chạy `dotnet build PingSignalR/PingSignalR.sln` với `Build succeeded. 0 Error(s)` |
| `buoc2-negotiate.png` | `curl.exe -i -X POST "http://localhost:5200/hubs/ping/negotiate?negotiateVersion=1"` trả 200 + `connectionId` |
| `buoc4-echo.png` | Client An in `[Echo] Echo: Xin chào SignalR` |
| `buoc5-nhom.png` | Hai terminal An + Bình, cả hai thấy `Broadcast`/`GroupMessage`, Echo chỉ ở người gửi |
| `buoc6-noilai.png` | Terminal client in `Đang kết nối lại...` và `Đã kết nối lại. ConnectionId mới: ...` |
| `buoc7-loi1.png` | `--lab hub`: 404 Not Found |
| `buoc7-loi2.png` | `--lab method`: `HubException: Method does not exist` |
| `buoc7-loi3.png` | `--lab event`: im lặng rồi dòng `[Lỗi] Không thấy EchoResponse...` |

## RealtimeSubmissionSystem

| Tệp | Chụp gì |
|---|---|
| `edusubmit-requests.png` | `requests.http` hoặc curl: 200, 201 + Location, 400, 404, negotiate 200 |
| `edusubmit-client.png` | Client in `Thông báo SignalR` cho `processing` và `graded`, sau đó `REST GET ... -> 200`, `graded` |
| `edusubmit-thi-nghiem.png` | Bảng 5 thí nghiệm trong README hoặc ảnh kết quả từng thí nghiệm |
| `edusubmit-hub-logs.png` | Log máy chủ: `[Hub] ... theo dõi submission:{id}` và `[Push] submission ... -> graded` |

## Mẹo

- Dùng Windows Terminal, theme tối, cỡ chữ 14–16, cửa sổ đủ rộng để không xuống dòng.
- Chụp bằng `Win + Shift + S` → chọn vùng terminal.
- Với bước 6, chụp client sau khi server đã được bật lại, không chụp lúc đang chờ.
- Không chỉnh sửa nội dung ảnh; chỉ cắt vùng terminal.
