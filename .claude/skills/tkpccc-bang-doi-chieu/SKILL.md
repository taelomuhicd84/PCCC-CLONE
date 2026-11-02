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
| `scripts/bang_doi_chieu.py` | CLI: danhmuc, chon, khung, dong, tim, lamviec, job, gop, kiem, xlsx, md |
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
python -X utf8 $B job ket_qua/<da>/doi_chieu.json KT7.1 --tu 0 --den 70      # -> jobs/KT7.1_0-70.md + đường file phần
python -X utf8 $B gop ket_qua/<da>/doi_chieu.json ket_qua/<da>/phan/*.json   # số khung đánh lại nối tiếp; KS dùng --giu-so
python -X utf8 $B kiem ket_qua/<da>/doi_chieu.json      # lỗi (thoát 1) + cảnh báo + đếm theo kết luận
python -X utf8 $B xlsx ket_qua/<da>/doi_chieu.json      # -> doi_chieu.xlsx: sheet Tóm tắt + mỗi bảng một sheet
python -X utf8 $B md ket_qua/<da>/doi_chieu.json --ma KS [--chi-loi] [--cat 300]   # bảng markdown để gửi trong tin nhắn
```
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
- KS: thang thoát nạn mỗi thang một dòng: chép dòng TT 11, `idx` mới = lớn nhất + 1, `tt` "11a", "11b"…; dòng không có trên bản vẽ thì KHÔNG ÁP DỤNG hoặc xoá.

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
- Sửa bảng gốc: sửa `data/<mã>.json`, giữ nguyên `idx`, ghi `LICH_SU.md`; muốn đồng bộ về SKILL-PCCC-NEW thì báo người dùng.
- Mẫu Word chính thức của bảng A, B (`data/mau/*.docx` ở SKILL-PCCC-NEW) chưa chép: xuất Word theo mẫu là việc sau (ROADMAP Pha 5b).
