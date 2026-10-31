# Tiến độ TKPCCC

File này cập nhật sau mỗi mốc việc và trước khi hết usage. Phiên mới đọc mục "ĐANG LÀM / VIỆC TIẾP THEO" trước.

## ĐANG LÀM / VIỆC TIẾP THEO (cập nhật 2026-10-31)
**Xong Pha 1 (bóc KL + báo giá) và Pha 2 (hướng dẫn B0–B7, khảo sát, báo giá sơ bộ).** Đã kiểm: 35 test (`shared/test`, `runner/test`),
6 test plugin trong accoreconsole, chạy thử web trên trình duyệt (tạo dự án, khảo sát, ước KL, báo giá, Word, màn hình điện thoại).
**Đã sắp xếp lại thư mục theo khung PCCC_he_thong** (`shared/`, `runner/src`+`test`, `skills/`, `ket_qua/`…), mô tả ở `STRUCTURE.md`.
**Code ở GitHub:** https://github.com/taelomuhicd84/PCCC-CLONE (công khai).

Việc người dùng cần làm:
- 👤 Tắt hẳn AutoCAD rồi mở lại để nạp plugin mới (DLL đã build; bản cũ đổi tên `TKPCCC.Cad.dll.cu*`).
- 👤 Đăng nhập lại Claude Code trên máy (phiên OAuth hết hạn): mở terminal gõ `claude` rồi `/login`. Chưa đăng nhập thì nút "Claude gợi ý"/"Hỏi" báo lỗi.
- 👤 Clone `taelomuhicd84/PcccVipPro` (private) về máy và báo đường dẫn để tham khảo.
- 👤 Gửi file đơn giá mẫu của bạn (khi có) để tool đọc đúng cột.

Việc tiếp theo (đề xuất):
1. Pha 3: đọc PDF + thẩm định — ghép lõi Python `D:\1_Claude\TestPCCC\PCCC` (copy module cần thiết vào thư mục mới `thamdinh/`, ghi vào `STRUCTURE.md`), hiện kết quả trên web.
2. Pha 4: `PCCC_RAI` rải đầu phun/đầu báo theo lưới trong polyline phòng; thủy lực TCVN 7336 (port `shared/src/thuy_luc.ts` của PCCC_he_thong).
3. Hỏi người dùng: kích thước/ký hiệu block chuẩn họ muốn (hiện là vòng tròn R200 + chữ), quy tắc ước khối lượng theo kinh nghiệm của họ.

Vấn đề còn mở / giới hạn đã biết:
- Plugin chỉ cho AutoCAD 2021–2024 (.NET Framework 4.8); 2025+ cần build .NET 8.
- Ray-casting khu vực bỏ qua cung (bulge) của polyline; ống trong block đặt ở layer 0 không đếm; VUNGBV chỉ xét thiết bị ở model space cấp 1.
- Báo giá sơ bộ dùng quy tắc mặc định của `knowledge/bao-gia-pccc.md` (DN ống chưa đúng thiết kế, ghi "cỡ ống chờ thiết kế").
- `PCCC_ONG` dùng lệnh PLINE thường (không kiểm được trong accoreconsole, chỉ kiểm trong AutoCAD thật).
