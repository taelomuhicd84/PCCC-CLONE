# Tiến độ TKPCCC

File này cập nhật sau mỗi mốc việc và trước khi hết usage. Phiên mới đọc mục "ĐANG LÀM / VIỆC TIẾP THEO" trước.

## ĐANG LÀM / VIỆC TIẾP THEO (cập nhật 2026-11-03 trưa)
**ĐANG DỞ (03/11 trưa, máy D:/1_Claude/TKPCCC): thử trọn luồng THẨM ĐỊNH trên hồ sơ Q.4 (việc 2 bên dưới), đo cỡ job. Dừng 10:40 vì usage
5 giờ chạm 85 % (reset 12:10 ngày 03/11).**
Hồ sơ Q.4 KHÔNG có trong repo (repo công khai): `ket_qua/q4/` (khảo sát) và `ket_qua/q4_tham_dinh/` (`ban_ve.pdf` 7 trang vector không có lớp chữ,
`anh/luoi_1..7.png` zoom 1.6, `doi_chieu.json`, `jobs/`, `phan/`) nằm trên máy D:/1_Claude/TKPCCC (người dùng chép từ máy PCCCVIP sáng 03/11).
Trang: 1 nội thất Trệt, 2 tường xây Trệt, 3 nội thất Lầu 1, 4 tường Lầu 1, 5 nội thất Lầu 2, 6 tường Lầu 2, 7 tum.
- File phần xong, đã kiểm đủ dòng (6/7 job):
  `A5` 29/29 (12 CẦN XÁC NHẬN, 17 KHÔNG ÁP DỤNG) · `KT7.1_6-80` 55/55 (6 CHƯA ĐẠT #8 #9 #45 #50 #61 #76, 16 CXN, 4 ĐẠT, 29 KAD)
  · `KT7.1_81-151` 52/52 (9 CHƯA ĐẠT #82 #87 #96 #99 #107 #116 #117 #118 #124, 11 CXN, 8 ĐẠT, 24 KAD; 36 ghi chú: PA1 tím đóng kín buồng
  thang T1 trang 1, 3, 5 + buồng T3 và cửa ra sân thượng trang 7; ô chữ chung trang 3 y 40–238)
  · `B1` 48/48 (37 CXN, 11 KAD; ô chữ trang 2; khung cam bếp tr.1, tr.5, cửa ra tr.1, lối đi tr.3)
  · `B12` 24/24 (16 CXN, 8 KAD) · `B13` 18/18 (13 CXN, 5 KAD) (hai ô chữ trang 4).
- Việc còn lại, theo thứ tự (gói Pro: kiểm `get_usage` giữa các lượt; ≥ 85 % thì dừng, lưu, push):
  1. Theo quy tắc mới (người dùng dặn 03/11 trưa: chưa có bản vẽ hệ thống = không có hệ thống; không lập bảng B cho hệ thống không có
     bản vẽ; không trùng lắp): KHÔNG chạy B14; B1, B12, B13 KHÔNG gộp vào kết quả (giữ `phan/` làm tư liệu cho bước vẽ hệ thống sau này: số
     bình, mặt nạ, loại đầu báo…); A5 dòng PHẢI CÓ (#6, #44, #52, #58 …) đổi CẦN XÁC NHẬN → CHƯA ĐẠT "không có <hệ thống>" + phương án
     "thiết kế <hệ thống>" bằng script trước khi `gop`. Chỉ làm tiếp khi người dùng nói "làm tiếp Q4". (Lời giao agent mẫu, khi cần:)
     Lời giao mẫu: "Đọc <job>, làm theo, ghi ĐÚNG MỘT JSON vào <phan/….json>. Ngày thẩm định …. Chỉ xét các dòng trong job, mọi dòng có
     ket_luan; số liệu theo `da_biet`; dòng PHẢI CÓ khi chưa có bản vẽ hệ thống …; ô chữ chung chỉ trong vùng …; không chạy gop/kiem/ve,
     không sửa file khác; lệnh bash dài thì ghi script ra file; trả lời ngắn: số dòng đã xét, đếm theo kết luận, điểm cần người dùng xem xét,
     lỗi định dạng gặp phải."
  2. `gop` → `kiem` (0 lỗi, xử lý cảnh báo) → rà chéo (mục dưới) với nhau và với bảng KS (`ket_qua/q4/doi_chieu.json`) → trang 1 ô `chu`
     "THẨM ĐỊNH SƠ BỘ PCCC (ngày) – phần xây dựng + hệ thống; tham khảo" ở [60, 40, 980, 72] → `ve --anh`, XEM ảnh, sửa tọa độ → `xlsx` →
     báo cáo người dùng (đếm XÂY DỰNG / HỆ THỐNG, `md --chi-loi`, phương án chính, số liệu cần bổ sung, đường dẫn PDF + Excel, kết quả đo job).
  3. Điền hàng B14 của bảng "Đo cỡ job" (`.claude/skills/tkpccc-tham-dinh/SKILL.md`); TIEN_DO, LICH_SU; commit, push (không đưa `ket_qua/`).
- Rà chéo (agent báo, xử lý ở bước 2):
  - Trang 1 cửa ra mặt tiền: khung cam B1 [1262, 440, 1420, 612] trùm 2 ô cửa, chồng PA1 (cửa mở ra ngoài) của KT 81–151: thu nhỏ hoặc dời.
  - Mặt tiền Trệt có 2 ô cửa ≈ 0,9–1,0 m (một vào sảnh T1; một vào P.khách qua ô đệm ≈ 1,3 m, ngăn bằng vách kính trượt), cả hai mở vào
    trong; `da_biet`, KS dòng 9, 13 ghi 1 cửa 0,9 m. KT #84 ĐẠT (số lối ra của nhà ≥ số lối ra mỗi tầng); ô thứ hai không bù cho tầng trên (#82).
  - Số bình chữa cháy: B12 #6 ≥ 2 bình/tầng (tầng ≈ 102 m² ≥ 100 m², đo sơ) + tum 1 = 7 bình; A5 #53 ghi ≥ 1 bình/tầng, A5 #54 dự trữ theo
    4–5 bình: thống nhất. B12 #3 mức nguy hiểm Thấp/Trung bình chưa chốt (≈ 310 m² sát 300 m², ≈ 12 người sát 15).
  - KT #124 lối ra mái CHƯA ĐẠT (sân thượng +10.400 ≥ 10 m, 6.6) còn KS ghi CXN; KT #107 mặt bậc T1 ≈ 235 mm < 250 mm CHƯA ĐẠT (sai số ± 5 mm).
  - KT #96 dùng Bảng G.1 (Lầu 2 ≈ 33 m > 25 m); nếu xếp khách sạn Bảng G.2a cột 4 (20 m) thì Lầu 1 ≈ 19,8 m sát ngưỡng.
  - Đã thấy trước: KT #8 #9 CHƯA ĐẠT còn KS dòng 6 CXN (đúng Bài học 30). A5 #6, #44, #52, #58 PHẢI CÓ ghi CXN còn KS 20, 24, 25 CHƯA ĐẠT
    (nhà hiện trạng chưa lắp). A5 #1: không thuộc diện thẩm định (4 tầng < 7, ≈ 310 m² < 3.000 m², Phụ lục III NĐ 105/2025).
- Điểm cần người dùng xem xét (đưa vào báo cáo, không hỏi chọn): kiểu báo cháy trung tâm hay độc lập, nhà không có phòng trực 24/24 (B1 #17);
  bếp mở thông phòng ăn, khách: loại đầu báo, ranh giới (B1 #6, #7); phòng ngủ cho thuê có là "phần nhà sở hữu riêng" (Đ 5.8.2.9.2; B1 #13,
  #58); PA1 Lầu 1, Lầu 2: cánh cửa quét vào cung chiếu thang (3.4.3), tường mới ở mép ô thông tầng Lầu 1 cần dầm đỡ, sảnh Lầu 2 lấy chỗ P.ăn;
  T3 5 bậc chéo quay 180° (#122, 3.4.4); cửa mở vào trong khi ≈ 12 người ≤ 15 (#88, ngoại lệ 3.2.10); buồng thang T1 sát tường giáp ranh nên
  không có lỗ lấy sáng ≥ 1,2 m²/tầng (#114, #115); B13 #1 F1.2 hay F1.4 (cột Quy định loại trừ nhà ở riêng lẻ).
- Thiếu tri thức (nhờ người dùng nạp): TCVN 13456:2022 (B13 #8, #19), TCVN 7435-1 (B12), Bảng 1 Đ 5.9.1.1.5 TCVN 7568-14 (B1); dự trữ 10 %
  số bình (B12 #13) không có trong QCVN 10 mục 2.6 đã nạp. Cột "Tra ở" trỏ file không liên quan: B1 (MOD_loi_ra_va_cua, MOD_cau_thang,
  SKILL_ngan_chay), B12 #3 #7 #15 #17, B13 #3: ghi vào skill `tkpccc-bang-doi-chieu`.
- ✅ Người dùng trả lời 03/11: chưa có bản vẽ hệ thống thì kết luận KHÔNG CÓ (CHƯA ĐẠT + phương án thiết kế), rồi Claude vẽ hệ thống PCCC
  (👤 người dùng sẽ train skill vẽ hệ thống sau). Đã ghi vào skill `tkpccc-tham-dinh`, agent `pccc-he-thong`.

**Phiên 2026-11-02 chiều: Pha 5a xong (người dùng giao Claude tự quyết).** Hai luồng: KHẢO SÁT – TƯ VẤN (skill `tkpccc-khao-sat-tu-van`)
và THẨM ĐỊNH xây dựng + hệ thống (skill `tkpccc-tham-dinh`, agent `pccc-xay-dung`, `pccc-he-thong`). Nền: module `tkpccc-bang-doi-chieu`
(bảng A/B/KT chép SKILL-PCCC-NEW 24743f9 + khung KS) và `tkpccc-ghi-chu-ban-ve` (annotation PDF thật: khung, callout, leader, nét, tường,
cửa, chữ). Sắp xếp lại: mỗi module một skill (bảng module ↔ skill ở `CLAUDE.md`; quy tắc tư vấn chuyển từ CLAUDE.md vào skill khảo sát).
Chừa đường thiết kế CAD qua MCP (ROADMAP Pha 6, chưa làm). Đã kiểm: npm test 57, pytest 9 (thamdinh 2 + ghi chú 4 + bảng 3),
CAD TAT CA DAT; chạy thử ghi chú trang 3 hồ sơ `ket_qua/canho` (trang xoay 270°): khung, callout đúng chỗ.
Việc tiếp theo của luồng mới:
1. ✅ 02/11 chiều: chạy trọn luồng khảo sát trên `ket_qua/canho` (Lưu Xuân Lộc) → bản 7: `doi_chieu.json` khung KS (26 dòng + 11a–11e;
   thang hở loại 2 đưa vào Kết luận dòng 11c kèm 3.4.16, 3.3.2), `ban_ve_ghi_chu.pdf` (68 annotation, PA1 tím, PA2 xanh lá, PA3 xanh),
   `doi_chieu.xlsx`, `bang_doi_chieu_ban7.md` (không commit). Điểm mới so bản 6: vế thang giữa đo sơ ≈ 0,84–0,89 m (< 0,9 m?);
   bãi đỗ xe được miễn theo chú thích 1) Bảng 14 nếu họng tiếp nước cách đường ≤ 18 m; thang máy xuống hầm cần khoang đệm ngăn cháy
   loại 1 áp suất dương (4.28); thang máy chữa cháy không áp dụng (6.13); hầm gara kín phải thoát khói (D.2h); lối ra mái (6.6, 6.7).
   Đã vẽ buồng thang nét liền khép kín + cung vế ở mọi tầng, chiều cao PCCC ghi ngay tại mép dưới cửa sổ tầng thượng:
   `ket_qua/canho/ban_ve_ghi_chu_moi_tang_2.pdf` (167 annotation, bản mới nhất).
   CHỜ người dùng xem bản 7 và sửa.
1b. ✅ 02/11 khuya (máy F:\…\PCCCVIP): MẪU BÁO CÁO KHẢO SÁT MẶC ĐỊNH = BCKS Excel của người dùng (người dùng dặn: luôn dùng nếu không gửi mẫu
   khác). Mẫu trắng `data/mau_bao_cao/BCKS_mau.xlsx` (giữ logo, tiêu đề công ty; xoá nội dung Lưu Xuân Lộc; sửa chữ "HỒ HƠ PHÁP LÍ").
   Lệnh mới `bang_doi_chieu.py bcks` điền khoá `bcks` của `doi_chieu.json` + ảnh cắt từ `ban_ve_ghi_chu.pdf` vào cột GHI CHÚ (tự thu nhỏ
   ảnh cho vừa 409 pt/hàng; Excel in hàng thấp hơn ảnh ≈ 7 % nên tính 0,81 pt/px). Đã kiểm bản in bằng Excel (ExportAsFixedFormat): 1 trang.
   Làm lại khảo sát Q.4 trên máy này (`ket_qua/q4/`, không commit): 7 CHƯA ĐẠT, 8 CẦN XÁC NHẬN, 9 KHÔNG ÁP DỤNG; tum có WC-07 nên tính
   1 tầng (QCVN 10 1.4.24 CT1) → 4 tầng, cao ≈ 11,6 m; thang hở L ≈ 33 m > 25 m; xe máy trong sảnh chân thang; mặt bậc đo sơ ≈ 235 mm.
   👤 Chờ người dùng xem `BCKS_CHDV_Doan_Nhu_Hai_Q4.xlsx/.pdf`, `ban_ve_ghi_chu.pdf` (66 annotation, PA1 tím) và cho biết có coi WC nhỏ trên tum
   là tum không tính tầng không (nếu có: 3 tầng, cao ≈ 8,2 m, nhưng vẫn phải đóng kín thang vì L ≈ 33 m).
2. ⏳ ĐANG DỞ (03/11): thử luồng thẩm định trên hồ sơ Q.4 (KT7.1 + A5 + B1, B12, B13, B14): xong 6/7 job, còn B14 rồi gộp, vẽ, báo cáo; chi tiết ở khối đầu mục này.
3. Pha 5b (ROADMAP): web chạy luồng, Word theo mẫu A/B, điền mẫu báo cáo khảo sát của người dùng (chờ người dùng gửi mẫu).

**Đọc thêm `LICH_SU.md` mục 2026-11-02 "Lưu ý cho phiên sau" trước khi làm.**
**NHẮC NGƯỜI DÙNG ĐẦU PHIÊN (người dùng dặn):** thang hở (loại 2) chỉ là kiến thức dùng để tư vấn, không phải dòng đối chiếu: bảng
Lưu Xuân Lộc bản 6 còn dòng 14 "Thang hở (loại 2)" phải bỏ, ghi vào cột Kết luận của dòng thang; vẫn chờ người dùng train điều kiện thang loại 2.
**Phiên 2026-11-02: tư vấn thử hồ sơ thật, người dùng đang train phần thang thoát nạn.**
- Đã ghi tri thức: quy trình đánh giá thang thoát nạn trong nhà (`knowledge/nhan-dien-thang-thoat-nan.md`, mục cuối): nhận diện từng thang →
  đạt/không → phương án từng thang → kiểm khả thi (chặn cửa căn hộ, mép bậc tới tường ≥ bản thang 3.4.3, cung mở cửa, cửa giếng thang máy
  E 30 khi thang máy ngoài buồng 4.23, tách thang hầm 3.2.1) → thang ngoài nhà → không có thì báo người dùng xem xét. Ô lấy sáng không phải
  lý do loại thang (3.4.8b chiếu sáng nhân tạo). Đã sửa cho khớp: bài học (1b), (2) `knowledge/hoc/du-an/so-bo-thiet-ke.md`,
  `data/mau_giai_phap/thoat_nan.json`, `CLAUDE.md`. Trước đó: bản vẽ cải tạo xét theo mặt bằng cải tạo; tầng thượng đã xây kín tính 1 tầng.
- Kết quả thử (KHÔNG commit): `ket_qua/luu_xuan_loc_thu/` (`tu_van_thiet_ke.md` dạng bảng, `tu_van_khoanh.pdf` vẽ bằng `ve_khoanh.py`).
  **Mục 4 Thoát nạn và PDF còn ghi "chọn thang trái cải tạo": SAI theo bài học mới.**

Việc tiếp theo:
1. Xong 02/11: `tu_van_thiet_ke.md` bản 6 theo khung bảng đối chiếu chính thức (23 dòng; phương án nằm trong cột Kết luận),
   chữ thang trên PDF đã sửa. Đánh số khung trùng TT: nay làm bằng luồng mới (việc 1 ở đầu mục này).
2. 👤 Chờ người dùng: (a) train tiếp điều kiện thang loại 2 (3.2.6, 3.4.15, 3.4.16; đoạn 3.2.6 trong bản hợp nhất bị lỗi chữ); đã train 02/11 phần
   đường thoát nạn 3.3.2: L1 + 3H + L3 ≤ Phụ lục G (`skills/MOD_tinh_toan_khoang_cach.md`);
   (b) căn hộ mở cửa thẳng vào buồng thang: chỉ báo cáo kèm căn cứ 3.2.1b, người dùng tự quyết (dặn 2026-11-02: không hỏi chọn);
   (c) duyệt bài học "Đối chiếu BCKS Lưu Xuân Lộc" (đang `cho_nguoi_dung_duyet`).
3. Hỏi người dùng có muốn chép ngược thay đổi `thoat_nan.json`, `nhan-dien-thang-thoat-nan.md` sang SKILL-PCCC-NEW không.

## Trạng thái các pha (cập nhật 2026-11-01 tối)
**Xong Pha 1 (bóc KL + báo giá), Pha 2 (hướng dẫn B0–B7, khảo sát, báo giá sơ bộ), Pha 3 (thẩm định PDF), Pha 4a (thủy lực TCVN 7336).**
Máy đang làm: `F:\1_ToolMMo\2_VibeCode\PCCCVIP` (Python 3.13 + pymupdf 1.28.2 + pyyaml, Node 24, Claude CLI đã đăng nhập, có Excel, KHÔNG có AutoCAD).
Đã kiểm 2026-11-01: `npm test` 56 test, `python -m pytest thamdinh/test -q` 2 test, trình duyệt (tab Thẩm định PDF, tab Thủy lực: tính sơ bộ,
tính theo số liệu, xuất Excel qua API có Excel COM hoàn thiện; số Excel tự tính khớp web: Q sprinkler 76,2816 / 76,2801).
CHƯA chạy `cad\test\chay_thu.ps1` (máy không có AutoCAD; phiên này không sửa `cad/`). Chưa bấm nút tải Excel trong trình duyệt (chỉ gọi API).

Pha 4a đã làm:
- `shared/src/thuy_luc.ts` + 2 file test chép nguyên từ SKILL-PCCC-NEW (24743f9): 14 test khớp Excel người dùng. `thuy_luc.py` của PcccVipPro
  dùng Hazen–Williams (gần đúng), không theo PL B → không dùng.
- `bang_tinh/thuy_luc/` (Excel mẫu, `o_nhap.json`, `vi_du/`), `scripts/xuat_thuy_luc_excel.py` + 2 `.ps1` (Excel COM) chép từ SKILL-PCCC-NEW.
  Sửa thêm: cờ `--khong-hoan-thien`; hoàn thiện lỗi chỉ cảnh báo; **F106 dùng áp nút b (N103)** theo B.2.10.12 (mẫu gốc dùng L101 = nút a).
- `runner/src/thuy_luc.ts` (chuẩn hoá ô sơ bộ, số liệu → ô Excel: chặn vượt khung 5/4/7 dòng, đổi K → K_gpm, báo khóa không có ô như `P_van`,
  đối xứng thì chép nhánh trái sang nhánh phải); route `GET /api/thuy-luc`, `POST /api/thuy-luc/so-bo`, `POST /api/thuy-luc/tinh`, `GET /api/thuy-luc.xlsx`.
- Web tab "Thủy lực"; `data/huong_dan.json` B4, B5a trỏ tới tab; B5b thêm `knowledge/tcvn-4513-1988-muc-6-19.md` (chép từ SKILL-PCCC-NEW).

Cần người dùng trả lời (thủy lực):
- Lỗi F106 (lưu lượng hàng II dùng áp nút a) và lỗi bảng nhánh phải giữ số cũ khi đối xứng cũng có trong SKILL-PCCC-NEW:
  **người dùng tự sửa bên đó** (trả lời 2026-11-01), TKPCCC không đụng repo đó.
- Họng (người dùng chốt 2026-11-01): H.5/H.6 (2,5 hoặc 5 l/s/tia) chỉ là yêu cầu tối thiểu (QBmin); tính toán ống vòng, bơm, bể theo lăng H.7
  tra theo chiều cao tia đặc. Đã sửa: Excel xuất ghi đè D184 dùng I162 (H.7); lõi QBmin mặc định 2,5, cảnh báo khi lăng H.7 < tối thiểu; web
  thêm ô "Tối thiểu 1 tia (H.5/H.6)". Excel và web khớp: Q bơm 81,4816 / 81,4801, PB 0,978 (Excel làm tròn lên) / 0,9773.
  SKILL-PCCC-NEW (D184, `q_tia_h6 ?? q_tia`) người dùng tự sửa.

Pha 3 đã làm (chi tiết, phần đã cắt/sửa: `thamdinh/README.md`):
- `thamdinh/`: lõi Python chép từ repo `taelomuhicd84/PCCC` (TestPCCC) commit `ab546c7` (thư mục `D:\1_Claude\TestPCCC` không có trên máy này).
  CLI mới `python -m pccc_checker run <pdf> --out <dir> [--pages auto|all] [--set k=v]` → `findings.jsonl` D/KD/XM + PDF ghi chú.
- Thêm dòng Đạt; dòng "không đọc được số liệu" xếp XM, không phải KD (luật người dùng trong SKILL-PCCC-NEW: thiếu dữ liệu thì CẦN XÁC NHẬN);
  thiếu nhóm F/chiều cao PCCC/bậc chịu lửa thì từng luật phụ thuộc ra XM thay vì im lặng. Sửa lỗi gốc: chiều cao ≥ 20 m bị gắn "ước lượng".
- Runner `runner/src/thamdinh.ts`: `POST /api/tham-dinh` (thân PDF ≤ 200 MB; rỗng = chạy lại file cũ), `GET /api/tham-dinh`,
  `GET /api/tham-dinh/pdf`, `POST /api/tham-dinh/xet` (Claude xét 1 dòng XM, lưu `claude_xet.json`).
- Web tab "Thẩm định PDF": 9 ô thông tin công trình (→ `--set`), bảng KD đỏ / XM cam nghiêng / Đạt xanh, cột # khớp nhãn PDF.

Việc người dùng cần làm:
- 👤 Đưa PDF hồ sơ thật (vd KS Hoàng Hùng 18 trang) vào máy này để thử tab Thẩm định PDF (kết quả nằm trong `ket_qua/`, không commit).
- 👤 Lưu ý: repo `SKILL-PCCC-NEW` đã công khai cùng 284 file `ket_qua/` hồ sơ khách (Hoàng Hùng, Hoàng Anh, Galaxy…) và PDF TCVN trong
  `8_TAILIEUHOCTAP/`. Muốn ẩn phải xoá khỏi cả lịch sử git hoặc chuyển repo về private. `PcccVipPro` sạch.
- 👤 Máy có AutoCAD: tắt hẳn AutoCAD rồi mở lại để nạp plugin mới.
- 👤 Gửi file đơn giá mẫu của bạn (khi có) để tool đọc đúng cột.

- Thẩm định thử KS Hoàng Hùng (người dùng giao 2026-11-01, PDF kiến trúc 10/27 tờ, `Downloads\23-10-2026-...kien truc KS Hoàng Hùng 25-06.pdf`): báo cáo `ket_qua/ks_hoang_hung_thu/bao_cao.md` (không commit). Máy 0 Đạt/0 KD/13 XM; đọc bằng mắt: nghi KD buồng thang N1 (3.4.13, nhà ~34 m), lối ra BTB1 qua bếp (3.4.7), sảnh ngăn khói hầm; thiếu mặt bằng T3–10 và bản vẽ PCCC. Chờ người dùng bổ sung hồ sơ.
- Người dùng đổi hồ sơ thử sang Căn hộ DV Lưu Xuân Lộc (Huy Phát, 6 tờ kiến trúc): báo cáo `ket_qua/luu_xuan_loc_thu/bao_cao.md` (không commit). Nghi KD: 2 thang cách ≈ 9,7 m < ½ đường chéo ≈ 10,8 m (bản điều chỉnh); cao PCCC sát 15 m; hầm xe chỉ 1 thang.
- (bản 3, 2026-11-02) Đối chiếu BCKS thật của người dùng (xlsx, 6 tầng + hầm, cao PCCC 18,4 m, KHÔNG thuộc diện thẩm định): sửa tư vấn thành bảng (`ket_qua/luu_xuan_loc_thu/tu_van_thiet_ke.md`), PDF khoanh làm lại. Bài học mới ghi vào `knowledge/hoc/du-an/so-bo-thiet-ke.md` (trạng thái cho_nguoi_dung_duyet); form tư vấn dạng BẢNG ghi ở `knowledge/khao-sat-bao-gia.md`.
- (bản 2, 2026-11-01 tối) Người dùng nhắc: phải áp phần đã train và ghi note lên PDF. Đã chép `data/mau_giai_phap/thoat_nan.json`, `knowledge/hoc/du-an/so-bo-thiet-ke.md`, `knowledge/nhan-dien-thang-thoat-nan.md`; viết lại `ket_qua/luu_xuan_loc_thu/tu_van_thiet_ke.md` và khoanh PDF `tu_van_khoanh.pdf` (`ve_khoanh.py`). Bắt được lỗi bản 1: cả 2 thang có bậc chéo (3.4.4), ≤ 15 người/tầng. CÒN: thẻ `data/ban_ve_mau` (36 MB) chưa chép; chưa tích hợp tạo khoanh PDF vào tab web.
- Tư vấn thiết kế Lưu Xuân Lộc bản 1 (đã thay bằng bản 2): `ket_qua/luu_xuan_loc_thu/tu_van_thiet_ke.md` (form 7 mục; KHÔNG thuộc diện thẩm định; 2 lối: buồng thang L1 trái + thang ngoài phải; báo cháy + họng + bể ≥ 12 m³, không sprinkler). Sửa nhận định cũ: hầm 160 m² chỉ cần 1 lối (3.2.6.1e).

Việc tiếp theo (đề xuất):
1. Thử thẩm định trên hồ sơ thật khi có PDF; so với lần chạy CLI gốc 31/10 trên Hoàng Hùng (Đạt 0, KD 3 đều "không đọc được số liệu", XM 3 →
   nay 3 dòng KD đó phải thành XM).
2. Thủy lực: làm theo câu trả lời 2 câu hỏi trên; thử trên mặt bằng sprinkler thật; mở rộng (hỏi trước): kho nhóm 5–7 (Bảng 2), ô HCB2 cụm van
   trong Excel mẫu, nút "chọn số đầu C07" (`chonSoDau` đã có trong lõi).
3. Pha 3+ (hỏi người dùng trước): đọc bảng cửa/bảng thang để lấy kích thước (bớt XM); nối bảng đối chiếu A/B/KT như `SKILL-PCCC-NEW`
   (`data/bang/*.json`, `shared/src/chon_bang.ts`); xuất Word đối chiếu.
4. Pha 4b: `PCCC_RAI` rải đầu phun/đầu báo theo lưới trong polyline phòng (cần máy có AutoCAD để kiểm).
5. Hỏi người dùng: kích thước/ký hiệu block chuẩn họ muốn (hiện là vòng tròn R200 + chữ), quy tắc ước khối lượng theo kinh nghiệm của họ.

Vấn đề còn mở / giới hạn đã biết:
- Plugin chỉ cho AutoCAD 2021–2024 (.NET Framework 4.8); 2025+ cần build .NET 8.
- Ray-casting khu vực bỏ qua cung (bulge) của polyline; ống trong block đặt ở layer 0 không đếm; VUNGBV chỉ xét thiết bị ở model space cấp 1.
- Báo giá sơ bộ dùng quy tắc mặc định của `knowledge/bao-gia-pccc.md` (DN ống chưa đúng thiết kế, ghi "cỡ ống chờ thiết kế").
- `PCCC_ONG` dùng lệnh PLINE thường (không kiểm được trong accoreconsole, chỉ kiểm trong AutoCAD thật).
- Thẩm định PDF: bản scan (không có lớp chữ) không đọc được; mới có 26 luật thoát nạn QCVN 06; thang bộ được tính là 1 lối ra khi xét
  phân tán 3.2.8 (PDF mẫu có 2 cửa sát nhau nhưng không báo). Mã dòng PC-001… đánh lại mỗi lần chạy nên chạy lại thì xoá "Claude xét" cũ.

## Ghi chú kỹ thuật Pha 3
- Repo tham khảo (đều công khai từ 2026-11-01): `taelomuhicd84/PCCC` (lõi Python gốc), `taelomuhicd84/SKILL-PCCC-NEW` (web thẩm định
  React + runner + 58 bảng đối chiếu + agent m01–m18), `taelomuhicd84/PcccVipPro` (module `pccc` cho Claude Code). Clone vào thư mục tạm, không sửa.
- Bash của Claude bị bộ lọc chặn chạy code của repo tải về; muốn so với bản gốc thì chạy trên bản đã chép trong `thamdinh/`.
- Lõi gốc còn chưa chép (để khi cần): Gemini đọc bản scan, `project_info.py` (đọc thông tin công trình), bảng đối chiếu `rules/checklists/`
  B4–B50, xuất DXF/Word.
- Hoàng Hùng (CLI gốc 31/10): KD là STAIR-DATA BTB1/BTB2 tr 4, EXIT-DOOR-DATA D01 tr 6; thiếu `height_pccc_m`, bậc chịu lửa, tỉ lệ bản vẽ;
  không nhận diện được lối ra tr 17.
