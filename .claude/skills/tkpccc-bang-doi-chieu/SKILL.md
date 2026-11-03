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
LOGIC BCKS (người dùng dặn 2026-11-03, theo BCKS kho hoá chất): 4 mục, ngắn, việc của CĐT rõ ràng.
- THÔNG TIN CÔNG TRÌNH: "Quy mô: …", "- Công năng", "- Diện tích sàn điển hình", "- Chiều cao công trình" (chưa có thì ghi "Chưa có"/ước lượng).
- HỒ SƠ PHÁP LÝ: một dòng so ngưỡng có số "=> Công trình cần / không thuộc diện thẩm định".
- HỆ THỐNG PCCC: "Các hệ thống PCCC cần trang bị:" + "- …" từng hệ thống; số liệu chính có cách tính (vd "* Bể nước PCCC 288m3 …").
- TƯ VẤN: "+ …" từng việc CĐT phải làm: hồ sơ cần cung cấp "Để đơn vị TVTK tư vấn chính xác về PCCC", hạng mục phải xây, sửa
  (thoát nạn, giáp ranh, giao thông đưa vào đây, kèm số ngắn gọn); không viết chung chung "lưu ý như trên".
- Cột GHI CHÚ PHẢI CÓ ẢNH GHI CHÚ như bản cũ (người dùng dặn 2026-11-03): ảnh cắt từ `ban_ve_ghi_chu.pdf` đúng chỗ có vấn đề
  (thang, sảnh để xe, đường thoát nạn, tum…) kèm chữ ngắn "- Tầng trệt: …", khoá `bcks.ghi_chu.<mục>.anh`; mẫu 4 mục thì gắn ảnh vào
  mục Tư vấn (hoặc Thông tin cho tum, Hệ thống). Logic 4 mục chỉ thay cột NỘI DUNG, không bỏ ảnh.
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
- Ô Kết luận khi xuất = nhãn + lý do + từng phương án một dòng. Phương án và kiến thức dùng để tư vấn (vd thang hở loại 2) nằm trong
  ô Kết luận của dòng liên quan, không thành dòng riêng.
- KS: thang thoát nạn mỗi thang một hoặc vài dòng: chép dòng TT 11, `idx` mới = lớn nhất + 1, `tt` "11a", "11b"…; dòng không có trên bản vẽ thì KHÔNG ÁP DỤNG hoặc xoá.
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
