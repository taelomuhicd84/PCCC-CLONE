---
name: tkpccc-tham-dinh
description: THẨM ĐỊNH thiết kế PCCC từ bản vẽ PDF (kiến trúc chỉ có mặt bằng, có mặt đứng/mặt cắt, hoặc hồ sơ thiết kế gần đầy đủ), tách 2 phần XÂY DỰNG (bảng KT) và HỆ THỐNG PCCC (bảng A + B) — kết luận từng dòng trong ô Kết luận của bảng đối chiếu, comment callout/leader/khung/nét cho thiếu số liệu và nghi vấn, phương án điều chỉnh vẽ lên PDF, xuất PDF ghi chú + Excel. Dùng khi người dùng nói thẩm định, thẩm duyệt, đối chiếu hồ sơ PCCC, kiểm tra hồ sơ thiết kế PCCC.
model: opus
effort: high
---
# Thẩm định PCCC: xây dựng + hệ thống (điều phối)
Module dùng: `tkpccc-bang-doi-chieu`, `tkpccc-ghi-chu-ban-ve`, `tkpccc-tra-quy-chuan`, `tkpccc-kiem-luat-pdf` (máy đọc chữ + 26 luật thoát nạn,
dùng làm bước sàng lọc). Agent: `pccc-xay-dung` (bảng KT), `pccc-he-thong` (bảng A, B). Khảo sát – tư vấn nhanh là skill `tkpccc-khao-sat-tu-van`.

## Nguyên tắc
1. Thông tin trước, kết luận sau: điền `cong_trinh` một lần, mỗi số liệu kèm `nguon` ("trang 5 mặt cắt A-A", "người dùng", "ước lượng");
   thiếu thì ghi `cong_trinh.thieu` VÀ comment cam trên bản vẽ ở chỗ lẽ ra có số liệu.
2. Bốn kết luận: ĐẠT · CHƯA ĐẠT · CẦN XÁC NHẬN · KHÔNG ÁP DỤNG, lý do có số. Thiếu dữ liệu thì CẦN XÁC NHẬN, nêu cần gì; không đoán.
3. CHƯA ĐẠT phải có phương án điều chỉnh: ưu tiên ít thay đổi kiến trúc; nêu khả thi hay không trên mặt bằng, ảnh hưởng hệ thống kèm theo
   (tăng áp, sprinkler…); vẽ phương án lên PDF. Thang thoát nạn làm đủ quy trình `knowledge/nhan-dien-thang-thoat-nan.md`.
4. Chỉ trích điều khoản có trong `knowledge/`, `skills/` hoặc cột Quy định của bảng chính thức; văn bản hết hiệu lực không làm căn cứ.
5. Chỉ báo cáo; không hỏi người dùng chọn phương án. Điểm luật không rõ: "cần người dùng xem xét" kèm căn cứ. Thiếu kiến thức: nhờ người dùng train.
6. Hồ sơ khách nằm trong `ket_qua/<dự án>/`, không commit.

## Ba loại đầu vào
1. Kiến trúc chỉ có mặt bằng: chiều cao PCCC ước lượng (`knowledge/khao-sat-bao-gia.md`); mọi kết luận phụ thuộc chiều cao nằm hai bên ngưỡng
   (15, 21, 28, 50 m) là CẦN XÁC NHẬN; comment cam "thiếu mặt đứng, mặt cắt".
2. Có mặt đứng, mặt cắt: đọc cao độ, số tầng, chiều cao PCCC (ghi phép cộng).
3. Hồ sơ gần đầy đủ (có bản vẽ hệ thống): phần hệ thống xét đủ bảng B theo thiết kế.
   CHƯA CÓ BẢN VẼ HỆ THỐNG = KHÔNG CÓ HỆ THỐNG (người dùng dặn 2026-11-03): dòng đối tượng PHẢI CÓ của bảng A là CHƯA ĐẠT, `ly_do`
   "không có <hệ thống>; phải trang bị (căn cứ có số)", `phuong_an` "thiết kế <hệ thống>" (bước sau: Claude vẽ hệ thống PCCC, skill người
   dùng sẽ train). KHÔNG lập bảng B cho hệ thống không có bản vẽ: bảng B chỉ đối chiếu hệ thống có trên bản vẽ (hoặc do Claude vẽ).
   Giáp ranh không ghi khoảng cách PCCC: CHƯA ĐẠT (`knowledge/giap-ranh-khoang-cach-pccc.md`, Bài học 30; khảo sát sơ bộ thì chỉ khoanh + hướng dẫn).

## Không trùng lắp, không làm thừa (người dùng dặn 2026-11-03)
- Một số liệu một chỗ: thông tin công trình nằm trong `cong_trinh` của `doi_chieu.json`, lệnh `job` ghi ra `cong_trinh.md` một file, job chỉ
  link tới. Số sai thì sửa `cong_trinh` rồi chạy lại `job`, không sửa từng file job.
- Chỉ giao agent dòng cần suy luận. Không lập job cho bảng hoặc dòng có kết luận biết trước (hệ thống không có bản vẽ, hạng mục không có
  trên bản vẽ như hầm, gara, thang máy chữa cháy): phiên chính điền hàng loạt bằng script, kèm căn cứ.
- Mỗi kết luận ghi một lần ở dòng gốc (vd đối tượng trang bị ở bảng A); dòng khác cùng ý ghi ngắn "như A5 #…", không chép lại lý do.
- Agent không vẽ thử, không chạy gop/kiem/ve (phiên chính làm một lần); phiên chính không đọc lại cả file phần, chỉ kiểm bằng script.

## Quy trình
```
B=.claude/skills/tkpccc-bang-doi-chieu/scripts/bang_doi_chieu.py
G=.claude/skills/tkpccc-ghi-chu-ban-ve/scripts/ghi_chu_pdf.py
```
0. Chuẩn bị `ket_qua/<dự án>/`: `ban_ve.pdf`; `luoi` từng trang (zoom 1.6) vào `anh/luoi_<trang>.png`; phân loại trang (mặt bằng tầng,
   mặt đứng, mặt cắt, bảng thống kê cửa/thang, ghi chú chung, bản vẽ hệ thống). Tuỳ chọn: chạy máy kiểm luật (skill `tkpccc-kiem-luat-pdf`)
   lấy `findings.jsonl` làm gợi ý chỗ cần xem.
1. Thông tin công trình → `cong_trinh` (công năng + nhóm F, số tầng, chiều cao PCCC, diện tích, khối tích, bậc chịu lửa, cờ hệ thống).
2. `python -X utf8 $B chon --cong-nang "…" --cao … --co …` rồi `lamviec <chuỗi in ra> -o ket_qua/<dự án>/doi_chieu.json`; bỏ bảng B
   của hệ thống không có bản vẽ (mục "Ba loại đầu vào").
3. Giao việc: `job` cho từng bảng (bảng dài thì chia khoảng idx, ≤ 55 dòng một job) → subagent `pccc-xay-dung` cho KT, `pccc-he-thong`
   cho A, B: "Đọc <jobs/…md>, làm theo, ghi JSON vào <phan/…json>". Phần đã có file phan hợp lệ thì bỏ qua. Trước khi giao, thêm
   `cong_trinh.da_biet` (danh sách câu: số liệu đã biết, vd từ khảo sát; `job` ghi vào `cong_trinh.md`) để các agent không lệch số. Lời giao ghi thêm:
   vùng ô chữ chung RIÊNG cho từng job (nhiều job vẽ chung một PDF, vd mỗi bảng B một dải trên của một trang), job nào vẽ phương án
   dùng chung (vd đóng kín buồng thang), "không chạy gop/kiem/ve". Gói Pro: tối đa 2 agent một lượt, kiểm `get_usage` giữa các lượt
   (≥ 85 % thì dừng, lưu, push); agent chết vì hết hạn mức (429) thì `SendMessage` cho nó làm tiếp, không mở agent mới.
4. `gop` rồi `kiem` (0 lỗi; xử lý cảnh báo: CHƯA ĐẠT thiếu phương án, khung chưa vẽ).
5. Rà soát chéo: số liệu lệch giữa các bảng (chiều cao, diện tích, số tầng), kết luận mâu thuẫn giữa KT và A/B, comment trùng chỗ.
6. Trang 1 thêm ô `chu` "THẨM ĐỊNH SƠ BỘ PCCC (ngày) – phần xây dựng + hệ thống; tham khảo"; `ve … --anh`, XEM ảnh, sửa tọa độ.
7. KẾT QUẢ ĐỐI CHIẾU LÀ BẢNG ĐỐI CHIẾU (người dùng dặn 2026-11-03), không phải bản tóm tắt: `xlsx` + `md > bang_doi_chieu.md` (đủ mọi dòng:
   TT | Nội dung đối chiếu | Nội dung thiết kế | Quy định | Khoản Điều | Kết luận), gửi hai file bằng SendUserFile. KHÔNG dán bảng vào chat
   (tốn token output). Chat chỉ: đường dẫn, đếm theo kết luận từng phần (XÂY DỰNG, HỆ THỐNG), điểm cần người dùng xem xét, số liệu cần bổ sung.

## File phần (agent ghi, lệnh `gop` đọc)
```json
{"ma": "KT7.1",
 "cong_trinh": {"bac_chiu_lua": "II"},
 "dong": [{"idx": 85, "thiet_ke": "Cửa buồng thang D1 0,9 m, không ký hiệu chống cháy (trang 3)", "ket_luan": "CHUA_DAT",
           "ly_do": "cửa buồng thang phải là cửa ngăn cháy (…)", "phuong_an": ["PA1: thay FD EI 60 tự đóng, chèn kín khe"],
           "khung": [1], "nguon": "trang 3"}],
 "ghi_chu": [{"khung": 1, "trang": 3, "kieu": "khung", "hop": [775, 985, 870, 1110], "mau": "do", "noi_dung": "…"},
             {"khung": 1, "trang": 3, "kieu": "cua", "tam": [800, 990], "den": [830, 990], "loai": "EI 60", "mau": "tim",
              "phuong_an": "PA1", "noi_dung": "Thay cửa ngăn cháy tự đóng"}]}
```
- Chỉ ghi dòng đã xét (theo `idx` của job). `cong_trinh` chỉ ghi trường mới tìm được. Tọa độ px theo zoom của ảnh `luoi` (1.6).
- Số khung đánh từ 1 trong mỗi file phần; `gop` đánh lại nối tiếp toàn hồ sơ.

## Đo cỡ job (hồ sơ Q.4 F1.2 4 tầng, đầu vào loại 1, agent sonnet, 02–03/11/2026)
| Job | Dòng | Thời gian | Token | Lệnh | Đủ dòng | Ghi chú |
|---|---|---|---|---|---|---|
| A5 | 29 | 16 phút | 324 nghìn | 61 | 29/29 | tự tạo `phan/` (nay `job` tạo); không có Edit nên sửa JSON bằng Python |
| KT7.1 idx 6–80 | 55 | 31 phút | 536 nghìn | 104 | 55/55 | lệnh bash quá dài lỗi ENAMETOOLONG → ghi script ra file rồi chạy |
| KT7.1 idx 81–151 | 52 | 34 phút | 478 nghìn | 100 | 52/52 | lần đầu chết giữa job (429), chạy lại từ đầu; vẽ PA1 đóng kín buồng thang 4 trang (36 ghi chú) |
| B1 | 48 | 18 phút | 298 nghìn | 59 | 48/48 | |
| B12 + B13 (1 agent) | 24 + 18 | 14 phút | 250 nghìn | 56 | 42/42 | gộp 2 job ngắn được: đọc thông tin công trình một lần |
| B14 | 44 | không chạy | | | | quy tắc 03/11: không lập bảng B khi hồ sơ không có bản vẽ hệ thống |
- Đo transcript (03/11, lọc trùng theo id phản hồi): KT 81–151 92 lượt, đọc lại ngữ cảnh 23 triệu token (≈ 258 nghìn/lượt), viết ra 281 nghìn.
  Tốn nhất là SỐ LƯỢT × CỠ NGỮ CẢNH, không phải tra cứu. Vì vậy (03/11): `job` gắn toàn văn dòng + điều khoản nguyên văn (KT Q4 tìm được
  37/49 và 29/40 điều), `quy_tac_agent.md` thay 3 SKILL.md (≈ 9,5 nghìn ký tự thay ≈ 60 nghìn), xem ảnh bằng `cat` vùng; chưa đo lại.
- KT ≈ 9–10 nghìn token, ≈ 35–40 giây một dòng; A5 ≈ 11 nghìn; bảng B ≈ 6 nghìn token, ≈ 20 giây một dòng. Agent xét đủ dòng, JSON đúng
  định dạng (agent tự kiểm: idx đủ, khung hai chiều, tọa độ trong trang).
- Gói Pro: lượt 3 agent song song (≈ 1,2 triệu token) đưa cửa sổ 5 giờ từ 42 % lên hết, tuần +8 %. 03/11: KT 81–151 + B1 + B12/B13
  (≈ 1 triệu token, phiên chính Opus) đưa cửa sổ 5 giờ 28 % → 85 %, tuần 60 % → 67 %. Một cửa sổ 5 giờ chạy được ≈ 1–1,2 triệu token agent:
  cả hồ sơ (≈ 270 dòng ≈ 2,1 triệu token) chia 2 cửa sổ, mỗi lượt ≤ 2 agent, mở agent tiếp ngay khi một agent xong nếu usage còn dưới ≈ 60 %.

## Sau này
Web: tab thẩm định chạy quy trình này qua runner (`claude -p`), xuất Word theo mẫu bảng A/B chính thức (ROADMAP Pha 5b). Vẽ phương án vào
AutoCAD qua MCP từ cùng `ghi_chu` (Pha 6).
