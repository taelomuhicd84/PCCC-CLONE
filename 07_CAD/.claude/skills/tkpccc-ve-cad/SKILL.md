---
name: tkpccc-ve-cad
description: Vẽ AutoCAD tự động qua MCP CAD của TKPCCC (công cụ cad_tt, cad_chay) bằng spec ngắn — lưới trục, đặt thiết bị PCCC, ống/dây, nét, chữ, kích thước, xoá, đọc, đếm — tốn ít token nhất. Dùng khi người dùng nói "vẽ CAD", "vẽ vào AutoCAD", "vẽ lưới trục", "đặt đầu báo/đầu phun lên bản vẽ", "chạy spec", hoặc cần đọc/đếm đối tượng trong bản vẽ AutoCAD đang mở.
model: sonnet
effort: low
---

# Vẽ CAD tự động bằng spec
Thẻ module: `07_CAD/01_TRI_THUC_VE/TU_VUNG.md` (từ vựng, nguồn duy nhất), `07_CAD/06_VE_TU_DONG_AI/cad/Spec.cs` (thao tác), `07_CAD/06_VE_TU_DONG_AI/mcp/Mcp.cs` (máy chủ MCP), `07_CAD/README.md` (sửa gì vào đâu). Tri thức nhận dạng + vẽ (đầu nguồn duy nhất): `07_CAD/01_TRI_THUC_VE/README.md`.
Tiến độ và việc để lại của CAD nằm riêng ở `07_CAD/00_QUAN_LY_DU_AN/TIEN_DO.md`, kế hoạch tổng ở `07_CAD/00_QUAN_LY_DU_AN/KE_HOACH.md`.

AI không vẽ từng nét. AI viết một gói spec ngắn, plugin C# sinh hình, kết quả trả về chỉ có số đếm và lỗi. Đây là lý do cách này rẻ token.

## Cách làm
1. Từ vựng: máy chủ MCP đã gửi phần tóm tắt lúc kết nối. Cần chi tiết thì đọc `07_CAD/01_TRI_THUC_VE/TU_VUNG.md` (hoặc `cad_tt` với `tu_vung=true`).
2. Số liệu dự án lấy từ `du_an.json` trong thư mục dự án (mặc định `12_KET_QUA/<dự án>/cad/`) nếu có. Không đọc lại PDF để lấy số đã có.
3. Soạn MỘT gói cho cả việc rồi gọi `cad_chay` một lần. Gói dài hoặc sẽ chạy lại: lưu `12_KET_QUA/<dự án>/cad/skills/<tên>.json` rồi truyền `file` (chạy lại không tốn token viết).
4. Đọc kết quả: `loi` rỗng là xong, không gọi lại để kiểm. Có `loi` thì chỉ sửa đúng thao tác hỏng (theo `i`) và gửi lại riêng thao tác đó.
   Vẽ nhiều lượt vào cùng một bản vẽ: từ lượt hai ghi `"ban_ve":"<tên>"` (lấy ở kết quả lượt trước) để người dùng đổi tab giữa chừng thì gói vẫn vẽ vào đúng bản đó (chạy nền), không vẽ nhầm bản đang hiện. Bot chat tự gắn bản vẽ của lời nhắn.
   Cần nhìn hình (so với PDF mẫu, kiểm chữ đè, tuyến cắt nhau): thêm `{"op":"anh","hop":[...],"rong":800}` vào cuối gói rồi đọc file PNG ở `anh`; chỉ chụp khi số đếm chưa đủ để kết luận.
5. Báo người dùng: số đối tượng đã vẽ, giả định đã dùng (cao độ, tỉ lệ, cỡ chữ), điểm cần họ xem.

## Nền kiến trúc từ PDF
Tỉ lệ, nhập nền, kiểm tỉ lệ bằng trục, PDF nhiều trang: `07_CAD/01_TRI_THUC_VE/nhan_dang/pdf.md` (đọc trước khi nhập).

## Quy trình 6 bước khi người dùng đưa PDF (người dùng chốt 14/11)
Nền trước (mục trên). Rồi mỗi bước MỘT spec `12_KET_QUA/<dự án>/cad/skills/b<n>_<tên>.json`, MỘT `cad_chay` (`file`, từ lượt hai có `ban_ve`), cuối gói `{"op":"anh",...}`.
`loi` rỗng và soát (`doc`, `dem` trong gói kế, hoặc gói soát riêng) đạt tiêu chí mới sang bước sau; chưa đạt thì sửa đúng bước đó. Cú pháp op: `07_CAD/01_TRI_THUC_VE/TU_VUNG.md`.

| Bước (spec) | Đọc trên PDF | Op | Đạt khi |
|---|---|---|---|
| 1 Trục (`b1_truc`) | tên trục, chuỗi kích thước nhịp X, Y | `luoi_truc` (`p` = giao trục đầu trên nền) | bóng trục trùng nền, tổng nhịp khớp chuỗi kích thước |
| 2 Tường (`b2_tuong`) | tường bao, tường theo trục, cột, độ dày | `tuong` (tim theo trục, vòng bao `kin`), cột `net` `kin` layer `COT` | ảnh: hai nét tường trùng nền; `doc` layer `TUONG` có đủ đoạn |
| 3 Mặt bằng chi tiết (`b3_chi_tiet`) | vách, cửa (vị trí, rộng, chiều mở), thang, tên phòng | `tuong` `vach`:true, `cua`, `chu`, `kt_chuoi` | `vung_tu_dong` một điểm mỗi phòng ra đủ vùng, m² khớp PDF (±5 %) |
| 4 Thiết bị PCCC (`b4_thiet_bi`) | công năng, chiều cao trần từng phòng; zone | `rai` theo vùng (luật ở mục dưới), `dat` thiết bị lẻ (nút ấn, chuông, họng) | `rai` `ho` = 0, `dem` khớp số dự kiến, zone không vượt `02_LUAT_VE/LUAT.md` BC-04 |
| 5 Dây, ống (`b5_tuyen`) | tuyến dây theo zone, trục ống chính | `tuyen` (`noi_tiep` mỗi zone; ống `xuong_ca` / `cot` theo `truc`) | `dem` có m dây, ống; ảnh không tuyến cắt ngang phòng khác |
| 6 Sơ đồ nguyên lý (`b6_so_do`) | (lấy số từ bước 4, 5) | `so_do` từ `dem` theo zone / tầng | `loi` rỗng, ảnh đúng quy cách `07_CAD/01_TRI_THUC_VE/ve/so-do-nguyen-ly.md` |

Tường vẽ bằng `tuong` nên `vung_tu_dong` chỉ dò layer tường, không lẫn nét PDF nền; cửa rộng hơn 3 m (cửa cuốn) thì truyền `ho`.

## Vùng, rải thiết bị, đi tuyến
1. Vùng TRƯỚC khi rải: dựng / vẽ `PCCC_VUNG`, chụp ảnh, soát 5 chỗ hay thiếu vùng (mục "Soát" trong `07_CAD/01_TRI_THUC_VE/nhan_dang/phong_vung.md`), DỪNG cho người dùng duyệt (`phong_vung.md`; khoang dầm, vì kèo: `dam_khoang.md`).
   AI tự dựng vùng: theo mục 'AI vẽ vùng qua spec phải KHỚP plugin' của phong_vung.md (luật 1–10): đọc nét, lấy mặt tường phía vùng, ranh kéo dài khấc theo cột/tường; người dùng báo hở thì lập bảng cạnh – toạ độ – mặt tường trước; vách dừng mặt vách, lan can thì chạm sảnh.
   Soi nét tường quanh cạnh vùng: `doc` `gi:"dt"` LUÔN kèm `"cat":true` (mặc định chỉ trả nét trọn hộp, sót tường dài); cạnh theo mặt TƯỜNG, không theo mặt cột to hơn tường (luật 9f).
2. Người dùng báo xong: `doc` `gi`:"vung" đọc lại vùng, rồi `rai` theo `l` (trị số, cách rải, dim: `07_CAD/01_TRI_THUC_VE/ve/bao_chay/rai_dau_bao.md`). `ho` > 0 hoặc `m2` sát `dt` thì nêu cho người dùng.
3. Chuông, nút, đèn báo phòng: `07_CAD/01_TRI_THUC_VE/ve/bao_chay/chuong_nut_den.md`. Dây zone (chia zone theo LUAT BC-04, X/Y, kẹp tường, đầu ra, điện trở cuối): `07_CAD/01_TRI_THUC_VE/ve/bao_chay/day_zone.md`.
4. Ống sprinkler, tư duy chung: `07_CAD/01_TRI_THUC_VE/ve/tu-duy-di-day-di-ong.md`. `tuyen` `xuong_ca` chỉ là nháp; vị trí ống góp do người dùng chọn (lệnh `PCCC_TUYEN`, hoặc `qua` một điểm mỗi vùng).
5. Người dùng chốt vị trí thì xoá vùng `PCCC_VUNG`.

## Lưu ý
- Trị số thiết kế (bước rải đầu báo, đầu phun, bán kính bảo vệ...) lấy từ skill `tkpccc-tra-quy-chuan`, không viết theo trí nhớ.
- `cad_chay` báo "AutoCAD chưa mở": nhờ người dùng mở AutoCAD (bundle TKPCCC tự nạp). Vẫn ra được DWG mà không cần mở CAD:
  `powershell -ExecutionPolicy Bypass -File 07_CAD\.claude\skills\tkpccc-ve-cad\scripts\chay_spec.ps1 -Spec <spec.json> [-Dwg <bản vẽ.dwg>]` chạy `PCCC_SPEC` trong accoreconsole,
  lưu DWG (bỏ `-Dwg` thì là `<tên spec>.dwg`), in kết quả và chép ảnh của `anh` về thư mục `anh` cạnh spec. DWG đã có thì vẽ tiếp vào đó.
- `cad_chay` báo "đang bận": người dùng đang dở một lệnh trong AutoCAD, nhờ họ kết thúc lệnh rồi gọi lại.
- Thiếu thao tác cho một hình lặp lại nhiều lần: thêm thao tác tham số vào `07_CAD/06_VE_TU_DONG_AI/cad/Spec.cs` theo skill `tkpccc-cad-console`, đừng liệt kê hàng trăm nét.
- Người dùng muốn hoàn tác: một lần `cad_chay` là một bước Undo (gõ `U` trong AutoCAD).
- Sơ đồ nguyên lý sprinkler: thao tác `so_do` (truyền số đếm `dem` của từng tầng + cao độ `cot`; cách vẽ ở `07_CAD/01_TRI_THUC_VE/ve/so-do-nguyen-ly.md`).
- Chưa có (để lại, xem `07_CAD/00_QUAN_LY_DU_AN/KE_HOACH.md`): sơ đồ nguyên lý báo cháy, khung chat trong CAD.
