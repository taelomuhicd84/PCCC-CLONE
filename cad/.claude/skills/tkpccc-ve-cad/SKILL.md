---
name: tkpccc-ve-cad
description: Vẽ AutoCAD tự động qua MCP CAD của TKPCCC (công cụ cad_tt, cad_chay) bằng spec ngắn — lưới trục, đặt thiết bị PCCC, ống/dây, nét, chữ, kích thước, xoá, đọc, đếm — tốn ít token nhất. Dùng khi người dùng nói "vẽ CAD", "vẽ vào AutoCAD", "vẽ lưới trục", "đặt đầu báo/đầu phun lên bản vẽ", "chạy spec", hoặc cần đọc/đếm đối tượng trong bản vẽ AutoCAD đang mở.
model: sonnet
effort: low
---

# Vẽ CAD tự động bằng spec
Thẻ module: `cad/skills/TU_VUNG.md` (từ vựng, nguồn duy nhất), `cad/module/plugin/Spec.cs` (thao tác), `cad/module/mcp/Mcp.cs` (máy chủ MCP), `cad/README.md` (sửa gì vào đâu).
Tiến độ và việc để lại của CAD nằm riêng ở `cad/TIEN_DO.md`, kế hoạch tổng ở `cad/KE_HOACH.md`.

AI không vẽ từng nét. AI viết một gói spec ngắn, plugin C# sinh hình, kết quả trả về chỉ có số đếm và lỗi. Đây là lý do cách này rẻ token.

## Cách làm
1. Từ vựng: máy chủ MCP đã gửi phần tóm tắt lúc kết nối. Cần chi tiết thì đọc `cad/skills/TU_VUNG.md` (hoặc `cad_tt` với `tu_vung=true`).
2. Số liệu dự án lấy từ `du_an.json` trong thư mục dự án (mặc định `ket_qua/<dự án>/cad/`) nếu có. Không đọc lại PDF để lấy số đã có.
3. Soạn MỘT gói cho cả việc rồi gọi `cad_chay` một lần. Gói dài hoặc sẽ chạy lại: lưu `ket_qua/<dự án>/cad/skills/<tên>.json` rồi truyền `file` (chạy lại không tốn token viết).
4. Đọc kết quả: `loi` rỗng là xong, không gọi lại để kiểm. Có `loi` thì chỉ sửa đúng thao tác hỏng (theo `i`) và gửi lại riêng thao tác đó.
   Vẽ nhiều lượt vào cùng một bản vẽ: từ lượt hai ghi `"ban_ve":"<tên>"` (lấy ở kết quả lượt trước) để người dùng đổi tab giữa chừng thì gói bị từ chối, không vẽ nhầm bản khác.
   Cần nhìn hình (so với PDF mẫu, kiểm chữ đè, tuyến cắt nhau): thêm `{"op":"anh","hop":[...],"rong":800}` vào cuối gói rồi đọc file PNG ở `anh`; chỉ chụp khi số đếm chưa đủ để kết luận.
5. Báo người dùng: số đối tượng đã vẽ, giả định đã dùng (cao độ, tỉ lệ, cỡ chữ), điểm cần họ xem.

## Lưu ý
- Trị số thiết kế (bước rải đầu báo, đầu phun, bán kính bảo vệ...) lấy từ skill `tkpccc-tra-quy-chuan`, không viết theo trí nhớ.
- `cad_chay` báo "AutoCAD chưa mở": nhờ người dùng mở AutoCAD (bundle TKPCCC tự nạp). Vẫn ra được DWG mà không cần mở CAD bằng `PCCC_SPEC` trong accoreconsole, cách chạy xem `cad/test/chay_thu.ps1`.
- `cad_chay` báo "đang bận": người dùng đang dở một lệnh trong AutoCAD, nhờ họ kết thúc lệnh rồi gọi lại.
- Thiếu thao tác cho một hình lặp lại nhiều lần: thêm thao tác tham số vào `cad/module/plugin/Spec.cs` theo skill `tkpccc-cad-console`, đừng liệt kê hàng trăm nét.
- Người dùng muốn hoàn tác: một lần `cad_chay` là một bước Undo (gõ `U` trong AutoCAD).
- Chưa có (để lại, xem `cad/KE_HOACH.md`): sơ đồ nguyên lý, PDF → CAD, khung chat trong CAD.
