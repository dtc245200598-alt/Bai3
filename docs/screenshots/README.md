# Ảnh minh chứng

Ảnh PNG chụp thực tế từ log đã chạy của bài này nằm trong thư mục [`../anh/`](../anh/).
Tài liệu README ở [`../README.md`](../README.md) đã nhúng chúng ở mục "Minh chứng".

- `buoc1-build.png` — `dotnet build PingSignalR.sln -v:minimal`: 0 Warning(s), 0 Error(s).
- `buoc2-negotiate.png` — `curl.exe -i -X POST "http://localhost:5200/hubs/ping/negotiate?negotiateVersion=1"` trả 200 + `connectionId`.
- `buoc4-echo.png` — Client An in `[Echo] Echo: Xin chào SignalR`.
- `buoc5-nhom.png` — Hai terminal An + Bình: thấy `Broadcast`/`GroupMessage`, Echo chỉ ở người gửi.
- `buoc6-noilai.png` — Client in `Đang kết nối lại...` rồi `Đã kết nối lại. ConnectionId mới: ...`.
- `buoc7-loi1.png` — `--lab hub`: 404 Not Found.
- `buoc7-loi2.png` — `--lab method`: `HubException: Method does not exist`.
- `buoc7-loi3.png` — `--lab event`: im lặng rồi dòng `[Lỗi] Không thấy EchoResponse...`.
