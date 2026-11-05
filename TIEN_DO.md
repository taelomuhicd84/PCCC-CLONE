# Tiến độ TKPCCC

File này cập nhật sau mỗi mốc việc và trước khi hết usage. Phiên mới đọc mục "ĐANG LÀM / VIỆC TIẾP THEO" trước.

## ĐANG LÀM / VIỆC TIẾP THEO (cập nhật 2026-11-03 chiều)
**DỞ (03/11, dừng vì usage 5 giờ 99 %; reset ≈ 12:10 UTC+7, tuần 82 %): sửa Q.4 theo bài train "đường thoát nạn đo từ vị trí xa nhất
TRONG PHÒNG" (`knowledge/nhan-dien-thang-thoat-nan.md`).** File người dùng gửi: `C:\Users\HP\Downloads\ban_ve_ghi_chu.pdf` (trang 5 Lầu 2, 3 nét
xanh dương: từ góc P. ngủ 06 (757, 623) → (757, 456) → (333, 457) → (330, 339) px@1.6; dài ≈ 9,2 m). Việc làm tiếp, theo thứ tự:
1. Q.4 khảo sát (`ket_qua/q4/doi_chieu.json`) và thẩm định (`ket_qua/q4_tham_dinh/doi_chieu.json`): đổi L1 7,9 m → ≈ 9,2 m ở KS dòng 12, 15,
   KT7.1 #96, các ô Kết luận có "7,9 m"; sửa `ghi_chu` trang 5: nét "L1 ≈ 7,9 m" thành nét xanh từ góc xa trong P. ngủ 06 như người dùng vẽ
   (tọa độ trên), nhãn "L1 ≈ 9,2 m"; chạy `kiem`, `ve --anh`, XEM ảnh, `xlsx`, `md`.
2. BCKS Q.4 v2 (`ket_qua/q4/BCKS_CHDV_Doan_Nhu_Hai_Q4_v2.xlsx`): thêm ảnh ghi chú ở cột GHI CHÚ (người dùng dặn: phải có ảnh như bản cũ):
   `bcks.ghi_chu.tu_van.anh` (thang Trệt + xe máy trang 1; đường thoát nạn Lầu 2 trang 5), chạy lại `bcks`, xuất PDF bằng Excel, XEM ảnh.
3. Cửa chống cháy "ngay cửa phòng": người dùng chưa nói rõ cách áp dụng cho PA1; đang vẽ cửa ở lối vào buồng thang. Ghi "cần người dùng xem xét".
**Xong 03/11 chiều (máy D:/1_Claude/TKPCCC):**
1. ✅ Thẩm định thử Q.4 (`ket_qua/q4_tham_dinh/`, không commit) theo quy tắc mới: chưa có bản vẽ hệ thống = không có hệ thống, không lập
   bảng B. Kết quả gồm KT7.1 + A5 (B1, B12, B13 bỏ khỏi kết quả, giữ `phan/` làm tư liệu khi vẽ hệ thống; B14 không chạy).
   XÂY DỰNG (KT7.1, 107 dòng): 12 ĐẠT, 15 CHƯA ĐẠT, 27 CẦN XÁC NHẬN, 53 KHÔNG ÁP DỤNG. HỆ THỐNG (A5, 29 dòng): 8 CHƯA ĐẠT (không có báo cháy,
   đèn sự cố + EXIT, bình, dụng cụ phá dỡ + mặt nạ; dòng chi tiết ghi "như #…"), 4 CẦN XÁC NHẬN, 17 KHÔNG ÁP DỤNG. `kiem` 0 lỗi, 0 cảnh báo.
   File: `ban_ve_ghi_chu.pdf` (89 annotation, trang 1, 3, 5, 7; PA1 tím đóng kín buồng thang T1 + T3), `doi_chieu.xlsx`, `doi_chieu.json`
   (bản trước gộp: `doi_chieu_truoc_gop.json`; A5 đã đổi: `phan/A5_v2.json`). 👤 Chờ người dùng xem.
   Cần người dùng xem xét (đã ghi trong ô Kết luận): KT #96 Bảng G.1 (nhà ở, 25 m) hay G.2a (khách sạn, 20 m; Lầu 1 ≈ 19,8 m sát ngưỡng);
   #122 T3 5 bậc chéo quay 180°; #88 cửa mở vào trong khi ≈ 12 người; #111 cánh cửa PA1 quét vào chiếu thang; A5 #59 không có nơi thường trực
   để dụng cụ phá dỡ; mặt tiền Trệt có 2 ô cửa ≈ 1,0 m (ảnh `cat` xác nhận) trong khi bảng khảo sát `ket_qua/q4` ghi 1 cửa 0,9 m.
2. ✅ 3 cách giảm token (người dùng duyệt): `job` ghi toàn văn dòng + "Điều khoản nguyên văn" trích từ knowledge/, skills/ (KT Q4 tìm được
   37/49, 29/40 điều) + `quy_tac_agent.md` trích mục SKILL.md; lệnh `ghi_chu_pdf.py cat` ảnh vùng phóng 2; agent đọc 3 file một lượt.
   CHƯA ĐO LẠI token với job mới: đo ở hồ sơ thẩm định kế tiếp (script đo transcript: đếm theo id phản hồi, xem mục "Đo cỡ job").
3. ✅ Model + effort trong frontmatter (CLAUDE.md mục module): agent `pccc-xay-dung` opus/high, `pccc-he-thong` sonnet/medium; skill khảo sát,
   thẩm định, thủy lực, kiểm luật opus/high; báo giá, hướng dẫn, CAD sonnet/medium; xong-dau-muc sonnet/low; skill phụ trợ không đặt.
4. ✅ CLAUDE.md "QUY TẮC SỐ 1, ÁP DỤNG MÃI MÃI": gần hết usage thì lưu TIEN_DO, LICH_SU, commit, push.
5. ✅ Luật xét tầng thượng/tum (người dùng sửa BCKS Q.4): có công năng sử dụng không (WC/kho trong tum là phụ trợ), có thì chiếm bao nhiêu %
   sàn mái, > 30% mới tính 1 tầng. Ghi vào skill khảo sát + `KS.json` dòng 1. Ô Quy mô Excel BCKS Q.4 (vẫn ghi "4 tầng nếu có WC") chưa sửa.
6. ⏳ Phiên riêng đang làm sơ đồ node công việc (Artifact, thêm nút, nối, xem nội dung): xem phiên "Sơ đồ node công việc TKPCCC".
Việc tiếp theo:
0. (05/11) Excel thủy lực MỚI của người dùng: đã ghi tư duy vào skill + knowledge. ĐÃ đồng bộ code (o_nhap, 3 script, test, lõi thêm S_tt + cảnh báo Ω); npm test 57 đạt, xuất vi_du qua Excel COM không lỗi. Qsp = Qmintt (người dùng chốt 05/11). CÒN: xem lại ảnh Excel xuất (hàng ẩn/hiện) bằng mắt; (đã sửa N196).
1. 👤 Người dùng train skill VẼ HỆ THỐNG PCCC (sau khi kết luận "không có hệ thống"): báo cháy độc lập, đèn sự cố + EXIT, bình, dụng cụ phá dỡ,
   mặt nạ cho Q4 (tư liệu: `ket_qua/q4_tham_dinh/phan/B1.json`, `B12.json`, `B13.json`).
2. Hồ sơ thẩm định kế tiếp: chạy agent với job mới, đo lại token (so với KT 81–151: 92 lượt, 23 triệu token đọc lại ngữ cảnh).
3. Máy này thiếu Pillow: `test_bcks_dien_mau` lỗi ModuleNotFoundError PIL (không do thay đổi 03/11); cài `pip install pillow` nếu cần chạy bcks.

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
