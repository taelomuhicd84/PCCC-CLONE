# Lịch sử TKPCCC

## 2026-10-31
- Lập kế hoạch 4 pha (plan `C:\Users\HP\.claude\plans\t-i-mu-n-thi-t-k-eager-giraffe.md`). Người dùng chốt: AutoCAD .NET, web nội bộ/LAN,
  MVP bóc KL + báo giá, block do tool tạo (attribute MA_VT, sửa hình được), đơn giá: tool tạo mẫu, gọi Claude không dùng API key.
- Pha 1: `data/vat_tu.json` (51 mã), plugin `PCCC_LIB/DAT/ONG/BOC`, runner (`/api/khoi-luong`, đơn giá mẫu Excel, báo giá Excel có công thức),
  web tab Khối lượng & Báo giá, `/api/claude` qua `claude -p`.
- Pha 2: tab Hướng dẫn B0–B7 (theo quy trình người dùng: B0, B1, B4, B5a, B5b, B6a, B6b, B6c, B7) đọc nguyên văn `knowledge/*.md`,
  hỏi Claude theo tài liệu của bước; tab Khảo sát 8 mục (Claude gợi ý, xuất Word); báo giá sơ bộ theo m².
- Tham khảo LHB Block Scheduler (chỉ tính năng, DLL có khoá bản quyền nên không dịch ngược): thêm không đếm trùng, block lồng/MINSERT,
  bỏ XREF, tách theo tầng/khu (`PCCC_BOC` → sheet `TheoKhuVuc`), `PCCC_GAN` (gán block có sẵn → mã), `PCCC_BANG`, `PCCC_VUNGBV`.
- Rà lại với effort cao, sửa: runner sập khi lỗi sau khi gửi header, EPIPE stdin của claude; `eWrongDatabase` khi tạo thư viện;
  ký hiệu lệch tâm (DBText → MText); bảng thống kê bị đếm như thiết bị (Table kế thừa BlockReference); tỉ lệ block theo INSUNITS;
  ô số của trình duyệt hiểu "1.200" là 1,2. Thêm `cad/test/chay_thu.ps1` (kiểm plugin trong accoreconsole), `docSoVN`, chuẩn hoá khảo sát.
- Tạo lại `PCCC_Blocks.dwg` (bản do plugin cũ tạo bị lệch tâm, cất ở `PCCC_Blocks_cu_20261031_151400.dwg`).
- git init (identity trantrongtien198); đẩy lên https://github.com/taelomuhicd84/PCCC-CLONE (nhánh `main`).
- Sắp xếp lại thư mục theo khung dự án mẫu PCCC_he_thong: `shared/src` (bao_gia, khao_sat) + `shared/test`, `runner/src`
  (server, xuat_xlsx, xuat_docx, engines/claude) + `runner/test`, `cad/src/Lenh.cs` + `cad/test`, `skills/` (MOD_*, SKILL_*, PHU_LUC_G
  chuyển từ knowledge/), `du_an/` → `ket_qua/`; thêm `STRUCTURE.md`, `ROADMAP.md`, `chay.bat`, `.claude/launch.json`.
- Gom quy trình hay thành 3 skill dự án: `tkpccc-cad-console` (+ `scripts/in_png.ps1` in DWG ra PNG, đã chạy thử),
  `tkpccc-xong-dau-muc`, `tkpccc-tra-quy-chuan`. Người dùng dặn: xong mỗi đầu mục thì lưu tiến độ, push, xoá ngữ cảnh rồi làm tiếp.
- Pha 3 (bắt đầu): khảo sát lõi Python `D:\1_Claude\TestPCCC\PCCC` (chỉ đọc). Chốt đường chạy offline không API key: extract vector →
  rule engine YAML (26 luật QCVN 06:2022 + SĐ1:2023) → D/KD/XM (`consistency.py`) → PDF ghi chú; bỏ `info`/Gemini/DXF/Word.
  Danh sách file cần chép + kế hoạch 6 bước ghi ở cuối `TIEN_DO.md`. Dừng trước khi viết code vì usage 5 giờ chạm 85%.
- Pha 3: chạy thử CLI gốc TestPCCC (offline, vector) trên 1 bản vẽ PDF thật → ra report + PDF ghi chú (để trong `ket_qua/`).
  Xác nhận đường chạy dùng được; chưa chép code vào TKPCCC, web chưa có tab Thẩm định.
- Pha 3: chạy thử lõi TestPCCC (bản sao thư mục tạm) trên PDF mẫu `make_sample_pdf.py` (4 KD + 1 XM đúng lỗi cài sẵn, PDF ghi chú đúng vị trí)
  và hồ sơ KS Hoàng Hùng 18 trang (0 Đạt, 3 KD "không đọc được số liệu", 3 XM). Đề xuất hạ KD "không đọc được" xuống XM khi chép vào `thamdinh/`.

## 2026-11-01
- Clone dự án về máy mới `F:\1_ToolMMo\2_VibeCode\PCCCVIP`. Không có `D:\1_Claude\TestPCCC` → lấy lõi từ repo `taelomuhicd84/PCCC` (đúng commit ab546c7).
- Người dùng đưa thêm repo tham khảo `SKILL-PCCC-NEW`, `PcccVipPro` rồi mở công khai cả ba (đã báo: SKILL-PCCC-NEW có hồ sơ khách trong `ket_qua/`).
- Pha 3 xong: `thamdinh/` (lõi Python, CLI `run`, dòng Đạt, "không đọc được" → XM, thiếu thông tin công trình → XM theo luật, sửa lỗi chiều cao
  ≥ 20 m bị coi là ước lượng, pytest), `runner/src/thamdinh.ts` + 4 route, web tab "Thẩm định PDF", "Claude xét" dòng XM (chạy thật 27 giây).
  Kiểm: npm test 39, pytest 2, trình duyệt (cả màn hình điện thoại). Chưa thử hồ sơ thật trên máy này; chưa chạy test CAD (máy không có AutoCAD).
- Người dùng dặn lại (ghi vào `CLAUDE.md` + bộ nhớ): gần hết usage thì dừng ngay, ghi `TIEN_DO.md` + `LICH_SU.md`, push — luôn tự làm.
- Pha 4a xong (người dùng chọn: tab web + xuất Excel mẫu): chép lõi thủy lực TCVN 7336 + test + Excel mẫu + script xuất từ SKILL-PCCC-NEW;
  `runner/src/thuy_luc.ts`, 4 route, tab web "Thủy lực". Đối chiếu Excel (Excel COM tính lại) với web phát hiện 2 lỗi của Excel mẫu/xuất:
  F106 lưu lượng hàng II dùng áp nút a (TCVN 7336 B.2.10.12 là nút b) và bảng nhánh phải giữ số cũ khi đối xứng → sửa lúc xuất; sau sửa
  Q sprinkler Excel 76,2816 / web 76,2801. Ghi lỗi 13 vào `knowledge/kiem-bang-tinh-thuy-luc-7336.md`. Kiểm: npm test 56, pytest 2.
- Người dùng: tự sửa 2 lỗi Excel bên SKILL-PCCC-NEW; chốt luật họng "tối thiểu 2,5 (H.5/H.6), tính toán theo H.7 tra chiều cao tia đặc".
  Áp vào TKPCCC: Excel xuất D184 dùng I162 (H.7) thay H18; lõi `thuy_luc.ts` QBmin mặc định 2,5 (trước lấy lăng H.7) + cảnh báo lăng < tối thiểu;
  web thêm ô tối thiểu 1 tia; lỗi 14 trong sổ tri thức. Excel và web khớp (Q bơm 81,4816 / 81,4801). npm test 57.

## 2026-11-02
- Tư vấn thử hồ sơ thật (kết quả trong `ket_qua/`, không commit): KS Hoàng Hùng (thiếu tờ), rồi Căn hộ DV Lưu Xuân Lộc (kiến trúc + BCKS).
  Người dùng chỉ ra lỗi: tư vấn chung chung, không ghi note lên PDF, cao PCCC sai (14,7 → 18,4 m), coi vùng cơi nới là 2 phương án, bỏ sót
  chống sét, tủ phá dỡ, mặt nạ. Đổi form kết quả sang bảng. Người dùng dạy: tầng thượng đã xây kín tính 1 tầng; bản vẽ cải tạo (2 mặt bằng
  cùng tờ) xét theo mặt bằng cải tạo phía dưới; khảo sát ghi "1 thang" khi chưa thang nào đạt.
- Người dùng train quy trình đánh giá thang thoát nạn trong nhà (PDF có nét tím = PA1, xanh lá = PA2 đóng kín thang giữa, xanh dương = khoảng
  mép bậc tới tường; ảnh mẫu cung chiếu nghỉ CT1/FD1). Tra luật theo yêu cầu: 3.4.8b cho chiếu sáng nhân tạo khi không trổ được ô lấy sáng;
  3.4.3 chiếu ≥ bản thang, cánh cửa không làm giảm bề rộng tính toán; 4.23 + bảng cửa: cửa giếng thang máy E 30; 3.2.1b liệt kê "trực tiếp
  vào buồng thang bộ" (người dùng muốn được hỏi); thang loại 2 chờ người dùng train. Ghi vào `knowledge/nhan-dien-thang-thoat-nan.md`.
- Người dùng dặn (ghi vào `CLAUDE.md`): việc của Claude là báo cáo, chấp nhận hay không là việc người dùng (không hỏi chọn); kết quả tư vấn
  theo khung bảng đối chiếu chính thức (TT, Nội dung đối chiếu, Nội dung thiết kế, Quy định, Khoản Điều, Kết luận). `tu_van_thiet_ke.md` bản 5
  (27 dòng) và PDF sửa chữ thang đã gửi; khung PDF chưa đánh số theo TT. Dừng ở usage 5 giờ 86%.
- Người dùng sửa: thang ngoài nhà ở mặt tiền là PHƯƠNG ÁN 3 do mình đề xuất, không phải hạng mục đối chiếu. `tu_van_thiet_ke.md` bản 6
  (23 dòng): PA1, PA2, PA3 chuyển vào cột Kết luận; luật ghi vào `CLAUDE.md` và `knowledge/nhan-dien-thang-thoat-nan.md`.
- Người dùng train thang loại 2, phần đường thoát nạn: 3.3.2 bản sửa đổi (chép nguyên văn từ ảnh bản hợp nhất) đo trên tầng tới mép
  bậc đầu tiên của thang loại 2, đoạn trên thang lấy 3H. Người dùng xác nhận PHẢI CỘNG L1 + 3H + L3 rồi so Phụ lục G (ví dụ vẽ mặt bằng
  + mặt cắt: 19,0 + 10,8 + 3,0 = 32,8 m > 30 m). Ghi vào `skills/MOD_tinh_toan_khoang_cach.md`, trỏ từ `knowledge/nhan-dien-thang-thoat-nan.md`
  bước 5 và `skills/MOD_cau_thang_buong_thang.md`.
- Pha 5a (người dùng giao Claude tự quyết; dặn thêm giữa chừng: chừa đường thiết kế CAD qua MCP, mỗi module một skill để sửa đỡ tốn token):
  module `tkpccc-bang-doi-chieu` (chép 76 bảng A/B/KT từ SKILL-PCCC-NEW 24743f9 + khung KS; CLI chon, khung, dong, tim, lamviec, job, gop,
  kiem, xlsx, md), `tkpccc-ghi-chu-ban-ve` (annotation PDF thật: khung, callout, leader, nét, tường, cửa, chữ; trang xoay; neo theo chữ;
  ảnh lưới tọa độ), skill điều phối `tkpccc-khao-sat-tu-van`, `tkpccc-tham-dinh`, agent `pccc-xay-dung`, `pccc-he-thong`; thẻ skill cho
  module cũ (`tkpccc-thuy-luc`, `tkpccc-kiem-luat-pdf`, `tkpccc-bao-gia`, `tkpccc-huong-dan`, CAD trong `tkpccc-cad-console`); CLAUDE.md
  gọn lại (bảng module ↔ skill; quy tắc tư vấn chuyển vào skill). ROADMAP thêm Pha 5a, 5b, 6 (CAD qua MCP). Cài pytest trên máy này.
  Kiểm: npm test 57, pytest 9, CAD TAT CA DAT.
- Chạy trọn luồng khảo sát – tư vấn mới trên hồ sơ Lưu Xuân Lộc (bản 7, kết quả trong `ket_qua/canho`, không commit): bảng KS 26 dòng
  + 11a–11e, 68 annotation trên 5 trang, Excel. Sửa công cụ: dòng thông tin (`thong_tin`) lấy giá trị làm kết luận; một ghi chú cho nhiều
  dòng (`khung` là danh sách); khung phương án chỉ hiện nhãn PA. Tra thêm 4.28, 6.13, Bảng 14 chú thích 1), 3.3.6, 6.6, 6.7, D.2.
- Người dùng dặn: vẽ buồng thang bằng NÉT LIỀN KHÉP KÍN và vẽ ở TẤT CẢ các tầng (không chỉ tầng điển hình); hỏi lại cung vế thang (đã train,
  lần trước bỏ sót). Thêm kiểu `cung` vào `tkpccc-ghi-chu-ban-ve` (1/4 tròn tô nhạt, tâm đầu mép bậc sát giếng thang, R = bề rộng vế);
  ghi quy tắc vào SKILL.md và `knowledge/nhan-dien-thang-thoat-nan.md`. Hồ sơ Lưu Xuân Lộc: vẽ PA1/PA2 thang giữa + cung ở tầng 1, lửng,
  2–4, thượng; thang trái hiện có + cung; hầm: đóng kín buồng thang hầm + khoang đệm thang máy (4.28). Phát hiện thêm: cửa thang trái mở ra
  hành lang, ngược chiều thoát (3.2.10), đổi chiều thì quét vào cung vế; tầng 1 PA2 phải có lối ra theo 3.4.7. File mới
  `ket_qua/canho/ban_ve_ghi_chu_moi_tang.pdf` (165 annotation; file cũ đang mở nên không ghi đè được).
- Người dùng sửa: ghi chú chiều cao PCCC phải đặt NGAY TẠI mép dưới cửa sổ tầng trên cùng (không trỏ cao độ sàn). Ghi quy tắc vào
  `tkpccc-ghi-chu-ban-ve` (quy tắc 1). Hồ sơ Lưu Xuân Lộc: nét xanh tại mép dưới cửa sổ tầng thượng (đo vector ≈ +18.200 = sàn
  +17.200 + 1,0 m), giữ 18,4 m theo BCKS; `ket_qua/canho/ban_ve_ghi_chu_moi_tang_2.pdf` (167 annotation, file trước đang mở).
- Khảo sát thử BV layout Q.4 (căn hộ dịch vụ F1.2, 3 tầng + tum, bản scan không có chữ): bảng KS 33 dòng, 46 annotation (`ket_qua/q4/`, không commit),
  điền thử form BCKS của người dùng (Hoàng Quân Phát; sheet NOTE F10–F16, I10–I16). Kết quả: 1 CHƯA ĐẠT, 16 CẦN XÁC NHẬN, 8 KHÔNG ÁP DỤNG.
- Người dùng sửa: (a) tum đủ tiêu chí 1.4.24 (≤ 30% sàn mái, chỉ che thang) thì KHÔNG tính tầng, không hỏi xác nhận; chỉ hỏi khi thiếu số liệu thật;
  (b) khoảng cách thoát nạn L = L1 + 3H + L3 vượt Bảng G.1 (hoặc tính toán) ⇒ CHƯA ĐẠT (không đảm bảo), kèm phương án; Q.4 ≈ 34 m > 25 m.
  Đã ghi hai quy tắc vào skill `tkpccc-khao-sat-tu-van`.
- Còn thiếu: lệnh điền mẫu BCKS vào `tkpccc-bang-doi-chieu` (ROADMAP 5b; hiện dùng script tạm); chưa kiểm bản in form (máy không có LibreOffice).
- (khuya, máy PCCCVIP) Kéo các skill mới từ GitHub. Người dùng gửi BCKS Lưu Xuân Lộc làm MẪU MẶC ĐỊNH cho báo cáo khảo sát ("sau này cứ mặc định
  cái này nếu không có up mẫu khác"): lưu mẫu trắng `data/mau_bao_cao/BCKS_mau.xlsx` (giữ logo + tiêu đề công ty, xoá nội dung và ảnh của
  khách, sửa "HỒ HƠ PHÁP LÍ" → "HỒ SƠ PHÁP LÝ"); thêm lệnh `bcks` (+ test) vào `tkpccc-bang-doi-chieu`, bước 5 trong `tkpccc-khao-sat-tu-van`.
  Bản in kiểm bằng Excel COM: Excel in hàng thấp hơn ≈ 7 % còn ảnh giữ cỡ, nên xếp ảnh theo 0,81 pt/px và tự thu nhỏ cho vừa 409 pt/hàng.
- Làm lại khảo sát Q.4 trên máy này (kết quả lần trước ở máy khác, không có ở đây): theo chữ QCVN 10:2025 1.4.24 CT1, tum có WC-07 không chỉ bao
  che thang nên tính 1 tầng (4 tầng, cao ≈ 11,6 m ước lượng) → nhà 1 lối (3.2.6.2a) phải dùng buồng thang; nếu bỏ WC tum (3 tầng, ≈ 8,2 m) được
  thang hở nhưng L ≈ 33 m > 25 m vẫn không đạt. Thang T1 một vế mỗi tầng, mặt bậc đo sơ ≈ 235 mm < 250; thang xoắn T2 không tính; T3 lên tum
  5 bậc chéo. Hệ thống: báo cháy (cho phép độc lập, Bảng A.1 mục 18), EX–EM, bình + phá dỡ + mặt nạ (Phụ lục E, F), chống sét; không bắt buộc
  họng nước, bể, sprinkler (Bảng B.1 mục 1.2).

### Lưu ý cho phiên sau (người dùng dặn ghi, 2026-11-02)
1. Kết quả tư vấn: MỘT bảng theo khung bảng đối chiếu chính thức (TT | Nội dung đối chiếu | Nội dung thiết kế | Quy định | Khoản Điều |
   Kết luận), gửi ĐẦY ĐỦ trong tin nhắn, không cắt dòng (người dùng phàn nàn khi thiếu dòng 1–7 thông tin công trình). Mẫu:
   `ket_qua/luu_xuan_loc_thu/tu_van_thiet_ke.md` (chỉ có trên máy này, không commit).
2. Mỗi dòng là hạng mục CÓ trên bản vẽ. Phương án mình đề xuất (PA1, PA2, PA3 thang ngoài nhà…) ghi trong cột Kết luận, không thành dòng.
   Thang hở (loại 2) cũng vậy: chỉ là kiến thức dùng để tư vấn (thang hiện có đủ điều kiện loại 2 thì ghi "được dùng thang hở theo điều …"
   trong Kết luận của dòng thang), không thành dòng riêng. Bảng bản 6 còn dòng 14 "Thang hở (loại 2)": phải bỏ.
3. Chỉ báo cáo; chấp nhận hay không là việc người dùng: không hỏi chọn, không đặt nút "chấp nhận / không". Thiếu kiến thức thì nhờ train.
4. Thang thoát nạn: làm đủ quy trình `knowledge/nhan-dien-thang-thoat-nan.md` (nhận diện từng thang → đạt/không → phương án từng thang →
   kiểm khả thi trên mặt bằng → thang ngoài nhà → không có thì báo người dùng xem xét). Không loại thang vì thiếu ô lấy sáng (3.4.8b).
   Không có số trên bản vẽ thì ghi "phải đảm bảo ≥ …". Chỉ ghi "cửa chống cháy" khi bản vẽ có ký hiệu.
5. Bản vẽ cải tạo (2 mặt bằng cùng tờ) xét theo mặt bằng cải tạo phía dưới; tầng thượng xây kín hoặc vượt diện tích tum thì tính 1 tầng.
6. Đang chờ người dùng: train điều kiện thang loại 2 (3.2.6, 3.4.15, 3.4.16); duyệt bài học "Đối chiếu BCKS Lưu Xuân Lộc".
7. Việc kế: đánh số khung PDF trùng TT (`ve_khoanh.py`), gửi lại; dòng 16 (khoang đệm thang máy xuống hầm) còn "cần tra" điều khoản.
8. Máy này: `git push` chạy được ngay từ Bash (không cần tab Terminal). Kiểm `get_usage` sau mỗi việc lớn; ≥ 85% thì dừng, lưu, push.
9. NHẮC NGƯỜI DÙNG đầu phiên sau (người dùng dặn): thang hở (loại 2) chỉ là kiến thức tư vấn, không phải dòng đối chiếu (bỏ dòng 14 bảng
   bản 6); vẫn chờ người dùng train điều kiện thang loại 2.
10. Cửa thang máy E 30 CHỈ khi thang máy nằm NGOÀI buồng thang (PA1). Mở rộng buồng thang ra sảnh thang máy (PA2) thì thang máy nằm TRONG
   buồng thang: xét 3.4.6 (≤ 2 thang chở người, chỉ xuống tới tầng 1; thang máy Lưu Xuân Lộc xuống hầm), không xét E 30 (người dùng sửa
   2026-11-02; đã sửa bảng bản 6 dòng 11, PDF, `knowledge/nhan-dien-thang-thoat-nan.md`).

## 2026-11-03
- (02/11 khuya → 03/11 sáng, máy PCCCVIP) Thử luồng THẨM ĐỊNH (skill `tkpccc-tham-dinh`) trên hồ sơ Q.4 để đo cỡ job: `cong_trinh` chép từ
  khảo sát + khoá mới `da_biet` (tóm tắt số liệu đã biết; `job` in kèm vào file giao việc nên các agent không lệch số với khảo sát); 7 job.
  Lượt 1 chạy 3 agent song song: A5 xong 29/29 dòng (16 phút, ≈ 324 nghìn token, 61 lệnh), KT7.1 idx 6–80 xong 55/55 (31 phút, ≈ 536 nghìn
  token, 104 lệnh), KT7.1 idx 81–151 chết giữa job vì hết hạn mức 5 giờ (gói Pro: 42 % → 100 %; tuần 48 % → 56 %). Sáng 03/11 cho agent đó
  làm tiếp (SendMessage) và giao B1, rồi người dùng dừng để làm tiếp ở máy khác: cả hai chưa ghi file phần. Kết quả dở ở `ket_qua/q4_tham_dinh/`.
- Người dùng cho phép đưa `ket_qua/q4`, `ket_qua/q4_tham_dinh` lên repo (công khai) để làm tiếp ở máy khác ("up gì thì up, miễn sao chạy
  tiếp"), nhưng bộ lọc an toàn của Claude Code chặn lệnh đẩy hồ sơ khách lên repo công khai: chỉ đẩy skill, agent, tri thức, tiến độ;
  hai thư mục hồ sơ người dùng tự mang sang máy khác hoặc tự `git add -f` rồi đẩy.
- Ghi tư duy (người dùng dặn "update tư duy vào skill, agent, knowledge"): skill `tkpccc-tham-dinh` thêm bảng "Đo cỡ job", nhịp gói Pro
  (≤ 2 agent một lượt, kiểm usage giữa lượt, agent chết vì 429 thì SendMessage làm tiếp), ≤ 55 dòng một job, `da_biet`, chia vùng ô chữ
  chung theo job, quy tắc PHẢI CÓ khi chưa có bản vẽ hệ thống (CẦN XÁC NHẬN, chờ người dùng xác nhận), giáp ranh theo Bài học 30;
  agent `pccc-xay-dung`, `pccc-he-thong` thêm cách tiết kiệm token (đọc 8–10 dòng một lệnh, ghi JSON một lần bằng script); lệnh `job` tự tạo
  thư mục `phan/`; sửa `knowledge/chon-loai-dau-bao.md` (trần giả, sàn nâng là Bảng A.2 mục 7 QCVN 10:2025, không phải A.3); ghi lỗi cột
  Tra ở và ngưỡng khối tích cũ của bảng A5 vào skill `tkpccc-bang-doi-chieu`.
- (03/11 sáng, máy D:/1_Claude/TKPCCC, người dùng chép `ket_qua/q4`, `ket_qua/q4_tham_dinh` sang) Làm tiếp thẩm định Q.4: chạy lại KT7.1 idx
  81–151 từ đầu (52/52, 34 phút, 478 nghìn token, 100 lệnh; 9 CHƯA ĐẠT; vẽ PA1 đóng kín buồng thang T1 ở Trệt, Lầu 1, Lầu 2 và buồng T3 + cửa
  ra sân thượng ở tum; thấy mặt tiền Trệt có 2 ô cửa chứ không phải 1), B1 (48/48, 18 phút, 298 nghìn), B12 + B13 gộp một agent (42/42,
  14 phút, 250 nghìn). Cửa sổ 5 giờ 28 % → 85 % nên dừng, lưu, push; B14 và bước gộp, vẽ, báo cáo làm sau khi reset. Bảng "Đo cỡ job" của
  skill `tkpccc-tham-dinh` đã điền (bảng B ≈ 6 nghìn token một dòng, KT ≈ 9–10 nghìn).
- Người dùng train (03/11 trưa): (1) trỏ đúng việc yêu cầu, có hệ thống mới tra chi tiết; thông tin công trình một file `.md` rồi link tới
  (lệnh `job` nay ghi `cong_trinh.md` chung, job chỉ link; tác dụng chính là sửa một chỗ, agent nhiều job đọc một lần); (2) chưa có bản vẽ
  hệ thống = không có hệ thống: dòng PHẢI CÓ là CHƯA ĐẠT + phương án thiết kế, không lập bảng B cho hệ thống không có bản vẽ, rồi Claude vẽ
  hệ thống PCCC (skill vẽ người dùng train sau); (3) không trùng lắp, không làm việc thừa. Ghi vào `CLAUDE.md`, skill `tkpccc-tham-dinh`,
  agent `pccc-he-thong`, `pccc-xay-dung`. "Làm tiếp Q4" = làm tiếp thẩm định Q.4, chỉ chạy khi người dùng nói (bỏ hẹn giờ 12:10).
