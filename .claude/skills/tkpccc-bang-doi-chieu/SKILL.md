---
name: tkpccc-bang-doi-chieu
description: Bảng đối chiếu PCCC chính thức (A1–A26 hệ thống theo công năng, B1–B14 theo hệ thống, KT kiến trúc theo công năng + chiều cao, KS khảo sát – tư vấn) — chọn bảng, đọc từng dòng không nạp cả file, lập file kết quả doi_chieu.json, giao job cho agent, gộp, kiểm, xuất Excel và markdown. Dùng mỗi khi lập hoặc điền bảng đối chiếu cho khảo sát, tư vấn hay thẩm định, khi cần biết công trình dùng bảng nào, hoặc người dùng nói "bảng đối chiếu", "xuất bảng", "ô Kết luận".
---
# Module: bảng đối chiếu
Phát triển module này: chỉ sửa trong thư mục này.

| File | Việc |
|---|---|
| `data/A*.json`, `data/B*.json`, `data/KT*.json`, `data/index.json` | bảng gốc chép từ SKILL-PCCC-NEW 24743f9 (`data/bang`): A, B theo mẫu chính thức Đồng Nai; KT dựng từ bộ bảng cũ theo công năng + chiều cao |
| `data/KS.json` | khung khảo sát – tư vấn của TKPCCC (người dùng chốt 2026-11-02) |
| `scripts/bang_doi_chieu.py` | CLI: danhmuc, chon, khung, dong, tim, lamviec, job, gop, kiem, xlsx, md, bcks |
| `data/mau_bao_cao/BCKS_mau.xlsx` (gốc repo) | mẫu báo cáo khảo sát MẶC ĐỊNH (từ BCKS kho hoá chất, chốt 2026-11-03): 4 mục THÔNG TIN, PHÁP LÝ, HỆ THỐNG, TƯ VẤN; `BCKS_mau_7muc.xlsx` mẫu cũ 7 mục |
| `test/test_bang_doi_chieu.py` | `python -m pytest .claude/skills/tkpccc-bang-doi-chieu/test -q` |

## Chọn bảng
- Khảo sát – tư vấn: `KS`.
- Thẩm định XÂY DỰNG: một bảng `KT` theo công năng + chiều cao PCCC. Thẩm định HỆ THỐNG PCCC: một bảng `A` theo công năng +
  các bảng `B` theo cờ hệ thống (B12 bình + dụng cụ, B13 đèn sự cố + loa, B14 điện PCCC luôn xét; B1 báo cháy, B5 họng nước, B6 sprinkler xét cả khi chưa rõ có hay không).
- `chon --cong-nang "<công năng thực tế + nhóm F>" --cao <m> --co bao_chay=1,sprinkler=0,…` in bảng + lý do + chuỗi cho `lamviec`.
  Cờ: bao_chay, bao_chay_dia_chi, tram_bom, ngoai_nha, hong_nuoc, sprinkler, bot_co_dinh, khi_hoa_long, khi_nen, khi, sol_khi (1 có, 0 không, bỏ trống = chưa rõ).

## Lệnh
```
B=.claude/skills/tkpccc-bang-doi-chieu/scripts/bang_doi_chieu.py
python -X utf8 $B khung KT7.1 --tu 60 --den 90          # danh sách dòng ngắn: #idx, TT, nội dung, khoản điều, tra ở
python -X utf8 $B dong KT7.1 "#85"                      # toàn văn một dòng (Quy định, gợi ý, tra ở)
python -X utf8 $B tim KT7.1 "hành lang"
python -X utf8 $B lamviec KT7.1,A5,B1,B12 -o ket_qua/<da>/doi_chieu.json     # tạo/bổ sung file kết quả
python -X utf8 $B job ket_qua/<da>/doi_chieu.json KT7.1 --tu 0 --den 70      # -> jobs/KT7.1_0-70.md (toàn văn dòng + điều khoản nguyên văn trích từ knowledge/, skills/) + cong_trinh.md, quy_tac_agent.md chung
python -X utf8 $B gop ket_qua/<da>/doi_chieu.json ket_qua/<da>/phan/*.json   # số khung đánh lại nối tiếp; KS dùng --giu-so
python -X utf8 $B kiem ket_qua/<da>/doi_chieu.json      # lỗi (thoát 1) + cảnh báo + đếm theo kết luận
python -X utf8 $B xlsx ket_qua/<da>/doi_chieu.json      # -> doi_chieu.xlsx: sheet Tóm tắt + mỗi bảng một sheet
python -X utf8 $B md ket_qua/<da>/doi_chieu.json --ma KS [--chi-loi] [--cat 300]   # bảng markdown ra file, gửi file (không dán vào chat)
python -X utf8 $B bcks ket_qua/<da>/doi_chieu.json -o ket_qua/<da>/BCKS_<tên>.xlsx [--mau <mẫu khác>.xlsx]   # báo cáo khảo sát theo mẫu
```

## Báo cáo khảo sát theo mẫu (`bcks`)
Mặc định điền `data/mau_bao_cao/BCKS_mau.xlsx`; người dùng gửi mẫu khác thì truyền `--mau`. Hàng của từng mục đọc theo nhãn cột A của mẫu
(`BCKS_NHAN`); mục có nội dung mà mẫu không có thì lệnh trả về `muc_mau_khong_co` (đưa ý đó vào Tư vấn).
LOGIC BCKS (người dùng sửa 2026-11-06, thay logic 4 mục 2026-11-03): mẫu MẶC ĐỊNH `BCKS_mau.xlsx` có 8 hạng mục (THÔNG TIN, HỒ SƠ PHÁP LÝ,
KHOẢNG CÁCH PCCC, GIAO THÔNG, THOÁT NẠN, NGĂN CHÁY, HỆ THỐNG PCCC, TƯ VẤN; mẫu 4 mục cũ: `BCKS_mau_4muc.xlsx`). BCKS là bản TÓM GỌN: mỗi mục 1–3 câu,
KẾT LUẬN TRƯỚC, không dẫn điều khoản, không giải thích dài; nội dung đầy đủ (lý do, số đo, phương án, căn cứ) để ở GHI CHÚ TRÊN BẢN VẼ và bảng đối chiếu.
- THÔNG TIN CÔNG TRÌNH: "Quy mô: …", "- Công năng", "- Diện tích sàn điển hình", "- Chiều cao PCCC", "- Chiều cao". Tên khách "Quý khách", địa điểm chung
  ("TP.HCM") khi người dùng chưa cung cấp; không ghi địa chỉ, tên chủ trên bản vẽ.
- HỒ SƠ PHÁP LÝ: một dòng so ngưỡng có số "=> … thuộc / không thuộc diện thẩm định".
- (người dùng dặn 2026-11-06, áp mãi mãi) Công trình KHÔNG thuộc diện thẩm định ⇒ KHOẢNG CÁCH PCCC và GIAO THÔNG ghi thẳng "-Đảm bảo" (dòng KS 7, 8 kết luận ĐẠT). Mọi ô BCKS cùng MỘT font, cỡ như ô mẫu (chỉ dòng bể nước thêm đỏ đậm), không lẫn cỡ chữ.
- KHOẢNG CÁCH PCCC: "- Các mặt công trình giáp với ranh đất <3m cần xây tường cao tới mái và không trổ ô cửa sổ".
- GIAO THÔNG: "-Đảm bảo" (hoặc việc phải làm, ngắn).
- THOÁT NẠN: "Công trình có N thang. Tuy nhiên <lỗi chính> => Chưa đảm bảo làm thang thoát nạn. Cần cải tạo lại thang (<số tối thiểu>), thang phải được đóng kín bằng tường gạch và cửa chống cháy." Ví dụ lỗi chính: thang rẽ quạt.
- NGĂN CHÁY (có sảnh thông tầng / thang hở nối tầng nổi: áp `knowledge/hut-khoi-thong-tang.md`, thêm "nếu không đóng kín phải có màn ngăn khói E45 và hút khói đồng thời các tầng thông nhau"): "Công trình có <lỗ giếng trời lấy sáng (lỗ thông tầng)> => Cần đóng kín bằng tường hoặc kính chống cháy hoặc kính cường lực dày 6mm có trang bị đai Sprinkler".
- HỆ THỐNG PCCC: "Các hệ thống PCCC cần trang bị:" + "- …" chỉ tên hệ thống, không ngoặc, không số căn cứ. Người dùng dặn 2026-11-06 (áp mãi mãi):
  (a) "họng nước chữa cháy" mặc định gọi là "Hệ thống chữa cháy vách tường (+ bơm)"; (b) sprinkler tầng hầm ghi "tầng hầm để xe" khi là hầm xe;
  (c) CÓ chữa cháy vách tường hoặc sprinkler thì BẮT BUỘC thêm dòng bể nước, chữ ĐỎ ĐẬM: `!- Bể nước PCCC: 126m3 (hầm để xe)` hoặc `9m3 (hầm kỹ thuật)`
  (dòng bắt đầu bằng "!" được `bcks` ghi đỏ đậm, hàm `_rich`); không có hai hệ thống đó thì KHÔNG ghi bể.
- (người dùng dặn 2026-11-09, khách sạn Duy Anh, áp mãi mãi) CÓ TẦNG HẦM thì phần thoát nạn (bảng KS, ghi chú PDF trang hầm) PHẢI xét hầm: số thang/lối ra, quãng đường từ điểm xa nhất tới cửa thang, phân tán hai lối (3.2.8, ⅓D nếu sprinkler), lối lên tầng 1, ram xe không tính lối thoát người. Nhưng Ở BCKS mục THOÁT NẠN chỉ ghi dòng "!=> Chưa đảm bảo …" ngắn cho phần CHƯA ĐẠT; phần đã đạt (thang thẳng, không rẽ quạt, không bậc chéo, quây kín, khoang đệm) và số đo khoảng cách hai lối KHÔNG ghi vào BCKS (số đo để ở bảng KS và ghi chú PDF).
- (2026-11-10, người dùng chỉnh tiếp) BCKS THOÁT NẠN: dòng đầu "Công trình có N thang bộ thoát nạn, mỗi tầng có M lối", rồi MỖI nhóm tầng một dòng "+ Tầng …: <lỗi ngắn>" (vd "+ Tầng hầm 1, hầm 2: hai cửa lối ra thang quá gần nhau"), cuối "!=> Chưa đảm bảo …"; KHÔNG ghi chung "tất cả các tầng" (khó hiểu), không số đo. KHOẢNG CÁCH PCCC đạt thì chỉ ghi "- Đảm bảo".
- (cùng ngày) GIAO THÔNG khi cao PCCC > 15 m: ghi "Cần bố trí bãi đỗ xe chữa cháy: rộng ≥ 6 m, dài ≥ … m" (Bảng 14 rộng; Bảng 15 dài theo diện tích sàn tổng và chu vi: 2.000–4.000 m² chưa sprinkler toàn nhà = 1/4 chu vi, có sprinkler = 1/6 chu vi, ≥ 15 m); khoảng lùi đủ chỗ thì không ghi "chưa đảm bảo" chung chung.
- (cùng ngày) KHOẢNG CÁCH PCCC: "khoảng lùi 6 m" là khoảng từ tường nhà tới RANH ĐẤT; công trình lân cận ít nhất sát ranh nên cách tường nhà ≥ khoảng lùi ⇒ so Bảng E.3 (≥ 3 m theo ranh) rồi KẾT LUẬN đạt/không, không ghi "chưa có khoảng cách tới công trình lân cận".
- (cùng ngày) NGĂN CHÁY: sảnh thông tầng đã ngăn bằng kính cường lực chống cháy (hoặc đóng kín) ⇒ KHÔNG cần hút khói đồng thời các tầng thông nhau; chỉ ghi hút khói cho hành lang > 15 m, hầm.
- TƯ VẤN: "+ …" 4–6 việc ngắn của CĐT (đóng kín buồng thang, cửa chống cháy các gian phòng tiếp giáp sảnh, số người ≤ 20/tầng nếu 1 thang, lắp hệ thống như đã yêu cầu, tường giáp ranh).
- Cột GHI CHÚ có ảnh cắt từ `ban_ve_ghi_chu.pdf` ở hàng liên quan (thoát nạn, ngăn cháy) như bản cũ (người dùng dặn 2026-11-03); khoá `bcks.ghi_chu.<mục>.anh`.
Nội dung lấy từ khoá `bcks` của `doi_chieu.json`, viết theo văn phong mẫu (gạch đầu dòng "- …", kết luận "=> …", số có đơn vị):
```json
"bcks": {"tieu_de": {"ten_cong_trinh": "", "khach_hang": "", "dia_diem": ""},
         "muc": {"thong_tin": "Quy mô: … \n- Công năng: …", "phap_ly": "", "khoang_cach": "", "giao_thong": "", "thoat_nan": "",
                 "he_thong": "", "tu_van": ""},
         "ghi_chu": {"thoat_nan": {"chu": "- Tầng trệt: …", "anh": [{"trang": 1, "hop": [x0, y0, x1, y1], "rong": 620}]}}}
```
- `ghi_chu.<mục>.anh`: vùng cắt từ `ban_ve_ghi_chu.pdf` (chưa có thì `ban_ve.pdf`), tọa độ như mục ghi chú PDF (px ảnh lưới ở `zoom`);
  `rong` px (mặc định 620 khi 1 ảnh, 305 khi nhiều ảnh). Ảnh tự thu nhỏ cho vừa ô (Excel tối đa 409 pt mỗi hàng).
- Chiều cao hàng ước theo số dòng chữ; luôn xuất PDF bằng Excel (ExportAsFixedFormat) rồi XEM ảnh trước khi gửi.
Không mở cả file JSON bảng gốc (tới 160 KB): đọc bằng `khung`, `dong`, `tim`.

## doi_chieu.json (file kết quả, `ket_qua/<dự án>/`, không commit)
```json
{"loai": "khao_sat|tham_dinh", "ban_ve": "ban_ve.pdf", "zoom": 1.6,
 "cong_trinh": {"ten": "", "cong_nang": "", "nhom_f": "", "so_tang_noi": null, "so_tang_ham": null, "chieu_cao_pccc_m": null,
                "dien_tich_san_m2": null, "khoi_tich_m3": null, "bac_chiu_lua": "", "cap_s": "", "he_thong": {},
                "nguon": {"chieu_cao_pccc_m": "trang 5 mặt cắt A-A"}, "thieu": ["mặt đứng", "cao độ đường"]},
 "bang": [{"ma": "KS", "ten": "…", "phan": "khao_sat|xay_dung|he_thong", "dong": [
   {"idx": 13, "tt": "11", "noi_dung": "…", "quy_dinh": "…", "khoan_dieu": "…", "tra_o": "…", "nhom": false, "xet": true,
    "thiet_ke": "Rẽ quạt, chưa đóng kín (trang 3)", "ket_luan": "CHUA_DAT", "ly_do": "nhà cao 18,4 m > 15 m (3.4.4)",
    "phuong_an": ["PA1 (nét tím) …: KHÔNG KHẢ THI (…)", "PA2 …: cần người dùng xem xét"], "khung": [11], "nguon": "trang 3"}]}],
 "ghi_chu": [ … mục của skill tkpccc-ghi-chu-ban-ve … ]}
```
- `ket_luan`: `DAT` · `CHUA_DAT` · `CAN_XAC_NHAN` · `KHONG_AP_DUNG` (rỗng = chưa xét). Khác ĐẠT phải có `ly_do` có số; CHƯA ĐẠT phải có `phuong_an`;
  CHƯA ĐẠT, CẦN XÁC NHẬN phải có `khung` và mục `ghi_chu` cùng số (lệnh `kiem` kiểm cả hai chiều).
- NGẮN GỌN (người dùng dặn 2026-11-06): bảng đối chiếu chỉ đối chiếu những mục CẦN, mỗi ô Kết luận một câu ngắn đủ hiểu, kiểu "không đảm bảo
  <mục abc>" hoặc "thang rẽ quạt, chưa đóng kín"; `ly_do` ≤ 1 câu, không chép điều khoản, không số đo dài. TOÀN BỘ nội dung chi tiết (số đo, lý do đầy đủ,
  phương án PA1/PA2, khả thi, căn cứ, kiến thức tư vấn như thang hở loại 2) ghi thành NOTE TRÊN BẢN VẼ (mục `ghi_chu`, khung số = TT), không nhồi vào ô Kết luận.
  `phuong_an` trong JSON để rỗng hoặc chỉ "xem ghi chú khung N"; dòng CHƯA ĐẠT/CẦN XÁC NHẬN vẫn phải có `khung` và mục `ghi_chu` cùng số (`kiem` kiểm hai chiều).
  Ô Kết luận khi xuất = nhãn + lý do ngắn. Phương án, kiến thức tư vấn không thành dòng riêng.
- Dòng thông tin (`thong_tin`: số tầng, chiều cao PCCC, công năng, khối tích, diện thẩm định của KS): ghi giá trị vào `ly_do`, để trống
  `ket_luan` (ô Kết luận hiện giá trị, `kiem` đếm là "xac_dinh"); số liệu chưa chắc thì dùng CẦN XÁC NHẬN.
- Ghi chú chung cho nhiều dòng (vd hệ thống chưa thể hiện trên bản vẽ): `"khung": [19, 20, 21]` trong một mục `ghi_chu`.

## Cột "Tra ở" → file TKPCCC
| Ghi trong bảng | Mở |
|---|---|
| `MOD_<tên>` (vd MOD_loi_ra_va_cua, MOD_hanh_lang) | `skills/MOD_<tên>*.md` |
| `SKILL_ngan_chay`, `SKILL_thoat_nan` | `skills/SKILL_ngan_chay_cong_nang.md`, `skills/SKILL_thoat_nan.md` |
| `chưa nạp: đọc cột quy định của bảng`, `bảng hệ thống B34–B50` | cột Quy định của chính dòng; kết luận theo đó, nguồn ghi "bảng đối chiếu chính thức" |
| `knowledge/…`, `skills/…` (bảng KS) | đúng file đó; chọn file quy chuẩn theo skill `tkpccc-tra-quy-chuan` |
Đối tượng trang bị hệ thống (báo cháy, chữa cháy…): TKPCCC đã có QCVN 10:2025 (`knowledge/qcvn10-2025-*.md`), dùng thay tiêu chuẩn trang bị cũ.

## Lưu ý dữ liệu
- Bảng KT dựng từ bộ bảng cũ: đầu bảng còn liệt kê NĐ 136/2020, TCVN cũ. Khi trích căn cứ chỉ dùng văn bản hiện hành (skill `tkpccc-tra-quy-chuan`).
- A5 (thấy khi thẩm định Q.4, 02/11): cột Tra ở lệch nội dung ở dòng idx 15, 21, 38 (ghi MOD_loi_ra_va_cua) và 27 (MOD_vat_lieu); cột Quy định
  còn ngưỡng khối tích cũ (báo cháy 2.500/5.000 m³) khác QCVN 10:2025 Bảng A.1 mục 18 (≥ 3 tầng hoặc ≥ 700 m² sàn): xét theo QCVN 10:2025.
- Sửa bảng gốc: sửa `data/<mã>.json`, giữ nguyên `idx`, ghi `LICH_SU.md`; muốn đồng bộ về SKILL-PCCC-NEW thì báo người dùng.
- Mẫu Word chính thức của bảng A, B (`data/mau/*.docx` ở SKILL-PCCC-NEW) chưa chép: xuất Word theo mẫu là việc sau (ROADMAP Pha 5b).
