# Tiến độ CAD (riêng cho thư mục cad/)

Phiên mới làm việc CAD: đọc file này trước, rồi `cad/README.md` (sửa gì vào đâu) và `cad/KE_HOACH.md` (kế hoạch tổng G0–G9). Nhật ký: `cad/LICH_SU.md`.
Luật: dùng tới 85 % hạn mức thì dừng, ghi file này + `cad/LICH_SU.md`, commit, push, rồi hỏi người dùng.

## TRẠNG THÁI (cập nhật 2026-11-10)
**DỪNG 2026-11-10 khoảng 15:40: hạn mức 5 giờ đã 87 % (đặt lại khoảng 19:30 giờ Việt Nam); tuần 19 %, tuần Fable 23 %.** `claude mcp list` báo máy chủ `cad` đang chờ duyệt (Pending approval): đường dẫn tương đối trong `.mcp.json` chưa thử kết nối thật. `npm test` chưa đọc được kết quả ở lần chạy cuối (đầu ra bị cắt), pytest 13 đạt.
**XONG G0, G1, G2 = MCP CAD.** Người dùng chốt 10/11: trước mắt chỉ làm MCP CAD, các phần khác để lại.
- G1 lệnh `PCCC_SPEC` (`cad/src/Spec.cs`), 10 thao tác: `luoi_truc`, `dat`, `ong`, `net`, `tron`, `chu`, `kt`, `xoa`, `doc`, `dem`. `chay_thu.ps1` 12/12 đạt.
- G2 cầu nối `cad/src/Cau.cs` + `cad/bin/TKPCCC.Mcp.exe` (công cụ `cad_tt`, `cad_chay`) + skill `tkpccc-ve-cad` + `.mcp.json`. `thu_mcp.ps1` 7/7 đạt.

## CHƯA KIỂM, CẦN NGƯỜI DÙNG
1. 👤 Chạy thật với AutoCAD đang mở (phần timer + `SendStringToExecute` trong `Cau.cs` chưa chạy lần nào, test không mở CAD không phủ tới):
   tắt hẳn AutoCAD, chạy `cad\build.bat`, mở lại AutoCAD (bundle `cad\TKPCCC.bundle` phải nằm trong `%APPDATA%\Autodesk\ApplicationPlugins`, hoặc gõ `NETLOAD` chọn `cad\TKPCCC.bundle\Contents\TKPCCC.Cad.dll`).
   Sau vài giây phải có file `%LOCALAPPDATA%\TKPCCC\cad\song.json`. Rồi trong Claude Code gõ: "vẽ lưới trục A–J × 1–5 bước 3600".
2. Đo token của lần vẽ ở mục 1 (mục tiêu: 1 lần gọi `cad_chay`) và ghi vào `cad/LICH_SU.md`.
3. Lần đầu mở Claude Code sau khi có `.mcp.json`, ứng dụng hỏi cho phép máy chủ `cad`: bấm đồng ý.

## VIỆC KẾ (để lại, thứ tự theo `KE_HOACH.md`)
- G3 thuật toán PCCC: `rai` (rải đầu phun, đầu báo, đèn exit/EM, bình theo vùng), `tuyen` (đi ống xương cá, đi dây nối tiếp), lệnh `PCCC_RAI`.
- G4 PDF → CAD: thao tác `pdf` (PDFIMPORT + tỉ lệ 2 điểm), phương án ghi chú PDF → spec.
- G5 sơ đồ nguyên lý `so_do` từ số đếm theo tầng + cao độ tầng.
- G6 bảng trong CAD: tab Công cụ (0 token), tab Chat (chọn máy AI, model, effort, thư mục dự án), `cad_anh`.
- G7 Hub: cài bundle, ghi cấu hình MCP cho các AI trên máy (có sao lưu), chẩn đoán.
- G8 tab YouTube (WebView2). G9 bản quyền + bảo mật (trước đó phải chuyển repo sang riêng tư).

## GHI NHỚ KỸ THUẬT
- Thêm thao tác spec: hàm `Op...` trong `Spec.cs` + 1 dòng `MotOp` + 1 dòng `TU_VUNG.md` + 1 phép kiểm trong `chay_thu.ps1`.
- Phần đầu `TU_VUNG.md` (trước dòng `---`) là hướng dẫn gửi cho AI lúc kết nối: giữ dưới 1900 ký tự, `thu_mcp.ps1` có kiểm.
- Lưới trục mặc định: chữ 350, bóng bán kính 1,15 × cao, trục kéo dài 5 × cao; kích thước tổng nằm sát bóng trục, muốn thoáng thì tăng `du`.
- Đi tuyến, rải thiết bị chưa có: hiện AI phải tự tính toạ độ rồi dùng `dat`, `ong`.
