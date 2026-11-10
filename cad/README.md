# cad/ — AutoCAD của TKPCCC

Plugin AutoCAD 2021 và đường vẽ tự động qua MCP. Phiên mới đọc `TIEN_DO.md` trước; kế hoạch tổng ở `KE_HOACH.md`; nhật ký ở `LICH_SU.md`.

## Vẽ tự động chạy thế nào
```
AI (Claude Code, VS Code, Antigravity...) -> bin\TKPCCC.Mcp.exe -> hàng đợi file -> lệnh PCCC_SPEC trong AutoCAD -> bản vẽ
```
AI chỉ viết một gói thao tác ngắn (spec JSON), plugin sinh hình. Một gói = một bước Undo. Kết quả trả về chỉ gồm số đếm và lỗi.

## Sửa gì thì vào đâu
| Muốn | Sửa file |
|---|---|
| Thêm hoặc đổi một thao tác vẽ (`luoi_truc`, `dat`, `ong`...) | `src/Spec.cs` (hàm `Op...` + 1 dòng trong `MotOp`) và `spec/TU_VUNG.md` |
| Đổi câu hướng dẫn AI nhận lúc kết nối | phần đầu `spec/TU_VUNG.md`, trước dòng `---` (giữ dưới 1900 ký tự) |
| Đổi tên, mô tả công cụ MCP | `mcp/Mcp.cs`, hàm `CongCu` |
| Đổi nhịp xem hàng đợi, điều kiện "AutoCAD đang rảnh" | `src/Cau.cs` |
| Đổi chỗ đặt hàng đợi | `chung/HangDoi.cs` hoặc biến môi trường `TKPCCC_CAD_HANG_DOI` |
| Lệnh gõ tay `PCCC_*` | `src/Lenh.cs` (xem skill `tkpccc-cad-console`) |

## Build và kiểm thử
- `cad\build.bat`: ra `TKPCCC.bundle\Contents\TKPCCC.Cad.dll` và `bin\TKPCCC.Mcp.exe`. AutoCAD đang mở thì tắt hẳn rồi mở lại để nạp bản mới.
- `powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1`: plugin + `PCCC_SPEC`, không cần mở AutoCAD.
- `powershell -ExecutionPolicy Bypass -File cad\test\thu_mcp.ps1`: máy chủ MCP + hàng đợi + plugin, không cần mở AutoCAD.
- Cả hai phải ra `TAT CA DAT`.

## Dùng
1. Chạy `cad\build.bat`, mở AutoCAD (bundle tự nạp, hoặc `NETLOAD` file `TKPCCC.Cad.dll`).
2. Mở Claude Code ở thư mục gốc repo: file `.mcp.json` khai máy chủ `cad`. AI khác thì khai lệnh chạy là đường dẫn tới `cad\bin\TKPCCC.Mcp.exe`.
3. Gõ yêu cầu vẽ. Chạy tay một spec: trong AutoCAD gõ `PCCC_SPEC` rồi dán đường dẫn file `.json`; kết quả nằm ở `<tên>.kq.json`.
