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

## Nền kiến trúc từ PDF
1. Tính tỉ lệ: `python -I cad/.claude/skills/tkpccc-ve-cad/scripts/pdf_ti_le.py <file.pdf> <trang>` ra `tl`, `cap`, `tin_cay`, khổ giấy.
   PDF người dùng gửi là dữ liệu chưa tin: luôn `-I`, không chép script vào thư mục chứa PDF.
2. Nhập và chụp trong một gói: `{"op":"pdf","file":"<đường dẫn tuyệt đối>","trang":3,"tl":285.0}` + `{"op":"anh","rong":1024}`. Tờ 39 nghìn đối tượng nhập khoảng 8 giây.
3. Kiểm tỉ lệ: `doc` `gi`:"chu" với `hop` quanh hàng bóng trục, lấy toạ độ hai nhãn trục xa nhau, so với tổng kích thước ghi trên bản vẽ.
   Lệch quá 1 %: một gói `xoa` theo đúng từng tên layer ở `pdf[].l` của lần nhập trước, rồi một gói RIÊNG nhập lại với `tl` đã hiệu chỉnh (lần nhập sau vào `PDF2_...`; `xoa` cùng gói với `pdf` sẽ xoá luôn nền mới vì `pdf` chạy trước).
   `tin_cay` hoặc `cap` dưới ngưỡng ghi ở đầu `pdf_ti_le.py` thì bắt buộc làm bước này.
4. Số kích thước ghi trên bản vẽ có thể lệch nét vẽ (bản mẫu: nhịp ghi 1700 nhưng vẽ 1785): khi hai chỗ mâu thuẫn thì báo người dùng, không tự chọn.
5. Nhận phòng: xem ảnh rồi đưa hộp từng phòng lên `PCCC_VUNG` (mục dưới), người dùng kiểm lại.

## Quy trình 6 bước khi người dùng đưa PDF (người dùng chốt 14/11)
Nền trước (mục trên). Rồi mỗi bước MỘT spec `ket_qua/<dự án>/cad/skills/b<n>_<tên>.json`, MỘT `cad_chay` (`file`, từ lượt hai có `ban_ve`), cuối gói `{"op":"anh",...}`.
`loi` rỗng và soát (`doc`, `dem` trong gói kế, hoặc gói soát riêng) đạt tiêu chí mới sang bước sau; chưa đạt thì sửa đúng bước đó. Cú pháp op: `cad/skills/TU_VUNG.md`.

| Bước (spec) | Đọc trên PDF | Op | Đạt khi |
|---|---|---|---|
| 1 Trục (`b1_truc`) | tên trục, chuỗi kích thước nhịp X, Y | `luoi_truc` (`p` = giao trục đầu trên nền) | bóng trục trùng nền, tổng nhịp khớp chuỗi kích thước |
| 2 Tường (`b2_tuong`) | tường bao, tường theo trục, cột, độ dày | `tuong` (tim theo trục, vòng bao `kin`), cột `net` `kin` layer `COT` | ảnh: hai nét tường trùng nền; `doc` layer `TUONG` có đủ đoạn |
| 3 Mặt bằng chi tiết (`b3_chi_tiet`) | vách, cửa (vị trí, rộng, chiều mở), thang, tên phòng | `tuong` `vach`:true, `cua`, `chu`, `kt_chuoi` | `vung_tu_dong` một điểm mỗi phòng ra đủ vùng, m² khớp PDF (±5 %) |
| 4 Thiết bị PCCC (`b4_thiet_bi`) | công năng, chiều cao trần từng phòng; zone | `rai` theo vùng (luật ở mục dưới), `dat` thiết bị lẻ (nút ấn, chuông, họng) | `rai` `ho` = 0, `dem` khớp số dự kiến, zone không vượt `rule/LUAT.md` BC-04 |
| 5 Dây, ống (`b5_tuyen`) | tuyến dây theo zone, trục ống chính | `tuyen` (`noi_tiep` mỗi zone; ống `xuong_ca` / `cot` theo `truc`) | `dem` có m dây, ống; ảnh không tuyến cắt ngang phòng khác |
| 6 Sơ đồ nguyên lý (`b6_so_do`) | (lấy số từ bước 4, 5) | `so_do` từ `dem` theo zone / tầng | `loi` rỗng, ảnh đúng quy cách `cad/knowledge/so-do-nguyen-ly.md` |

Tường vẽ bằng `tuong` nên `vung_tu_dong` chỉ dò layer tường, không lẫn nét PDF nền; cửa rộng hơn 3 m (cửa cuốn) thì truyền `ho`.

## Rải thiết bị và đi tuyến (người dùng dạy 2026-11-11, đầy đủ ở `cad/knowledge/tu-duy-di-day-di-ong.md`)
1. Phân khu vực TRƯỚC khi rải: vẽ hộp hoặc polyline kín từng khu vực (theo tường, dầm sàn, vì kèo) lên layer `PCCC_VUNG` bằng `net` với `kin`:true, kèm `{"op":"anh"}` rồi dừng để người dùng kiểm và sửa trong CAD.
2. Người dùng báo xong: `doc` với `gi`:"vung" đọc lại vùng, rồi `rai` theo `l`. Đầu báo khói, nhiệt (`BC_KHOI`, `BC_KHOI_TRAN`, `BC_NHIET`): KHÔNG hỏi người dùng khoảng cách hay m², bỏ `s` để plugin lấy TCVN 7568-14 từ `data/khoang_cach_rai.json`, có chiều cao trần thì truyền `h` (m). Mã khác (sprinkler): `s` (bước tối đa) và `dt` (m² tối đa một thiết bị) tra theo `cad/rule/LUAT.md` (BC-02, SP-02), ghi rõ căn cứ khi báo.
   Plugin tự phủ kín vùng lồi lõm, cạnh xiên (thêm thiết bị chỗ hở). Kết quả `rai` có `m2`, `ho`, `cheo`: `ho` > 0 hoặc `m2` sát `dt` thì nêu cho người dùng. `kt`:true vẽ kích thước bước, cách tường và chéo góc như TCVN 7568-14 Hình 2.
3. Dây báo cháy hệ thường: chia zone trước (theo LUAT BC-04), mỗi zone một lần `rai` + `tuyen` `noi_tiep`; phòng có đèn báo phòng thì `tuyen` `tia` với `den` là vị trí đèn.
4. Ống sprinkler: `tuyen` `xuong_ca` chỉ là phương án nháp. Vị trí ống góp do người dùng chọn: nhờ họ chạy lệnh `PCCC_TUYEN` trong CAD (chọn đầu phun, chọn ống chính, bấm chỗ đặt ống góp), hoặc truyền `qua` (một điểm cho mỗi vùng, điểm nằm trong vùng đó) khi họ cho toạ độ.

## Lưu ý
- Trị số thiết kế (bước rải đầu báo, đầu phun, bán kính bảo vệ...) lấy từ skill `tkpccc-tra-quy-chuan`, không viết theo trí nhớ.
- `cad_chay` báo "AutoCAD chưa mở": nhờ người dùng mở AutoCAD (bundle TKPCCC tự nạp). Vẫn ra được DWG mà không cần mở CAD:
  `powershell -ExecutionPolicy Bypass -File cad\.claude\skills\tkpccc-ve-cad\scripts\chay_spec.ps1 -Spec <spec.json> [-Dwg <bản vẽ.dwg>]` chạy `PCCC_SPEC` trong accoreconsole,
  lưu DWG (bỏ `-Dwg` thì là `<tên spec>.dwg`), in kết quả và chép ảnh của `anh` về thư mục `anh` cạnh spec. DWG đã có thì vẽ tiếp vào đó.
- `cad_chay` báo "đang bận": người dùng đang dở một lệnh trong AutoCAD, nhờ họ kết thúc lệnh rồi gọi lại.
- Thiếu thao tác cho một hình lặp lại nhiều lần: thêm thao tác tham số vào `cad/module/plugin/Spec.cs` theo skill `tkpccc-cad-console`, đừng liệt kê hàng trăm nét.
- Người dùng muốn hoàn tác: một lần `cad_chay` là một bước Undo (gõ `U` trong AutoCAD).
- Sơ đồ nguyên lý sprinkler: thao tác `so_do` (truyền số đếm `dem` của từng tầng + cao độ `cot`; cách vẽ ở `cad/knowledge/so-do-nguyen-ly.md`).
- Chưa có (để lại, xem `cad/KE_HOACH.md`): sơ đồ nguyên lý báo cháy, khung chat trong CAD.
