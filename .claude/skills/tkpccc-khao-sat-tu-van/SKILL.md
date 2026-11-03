---
name: tkpccc-khao-sat-tu-van
description: Lập BÁO CÁO KHẢO SÁT – TƯ VẤN PCCC khi khách gửi bản vẽ PDF hoặc thông tin bằng chữ — bảng đối chiếu KS có ô Kết luận và phương án, comment + vẽ phương án lên chính PDF (đóng kín buồng thang, cửa chống cháy, tường mới…), xuất Excel, gửi bảng đầy đủ trong tin nhắn. Dùng khi người dùng nói khảo sát, tư vấn, sơ bộ thiết kế PCCC, đánh giá nhanh công trình, hoặc gửi bản vẽ hỏi "có đạt không, sửa thế nào" mà không yêu cầu thẩm định đầy đủ.
model: opus
effort: high
---
# Khảo sát – tư vấn PCCC (điều phối)
Dùng các module: `tkpccc-bang-doi-chieu` (bảng KS, xuất), `tkpccc-ghi-chu-ban-ve` (PDF), `tkpccc-tra-quy-chuan` (trích điều khoản).
Không đọc cả `knowledge/`: mở file theo cột "Tra ở" của từng dòng. Thẩm định đầy đủ (xây dựng + hệ thống) là skill `tkpccc-tham-dinh`.

## Bài học đã train (người dùng nhắc 2026-11-01: phải áp phần đã train và GHI NOTE LÊN PDF)
- Trước khi viết: đọc `knowledge/hoc/du-an/so-bo-thiet-ke.md` (bài học: thang bậc chéo, thang ngoài nhà loại 3, sân thượng, số người…),
  `knowledge/nhan-dien-thang-thoat-nan.md` (phóng to từng thang xem chiếu nghỉ; quy trình nhận diện → đạt/không → phương án từng thang
  → kiểm khả thi → không có thì báo cần người dùng xem xét; thang loại 2 và khoảng cách 3.3.2), lọc `data/mau_giai_phap/thoat_nan.json`
  theo F, cao PCCC, số tầng, diện tích, người (lọc tay theo trường `loc`).
- Bản vẽ cải tạo (2 mặt bằng cùng tờ: trên = xin phép, dưới = cải tạo): xét theo mặt bằng CẢI TẠO phía dưới. Tầng thượng đã xây kín tính 1 tầng.
- Kết quả là MỘT bảng theo khung bảng đối chiếu chính thức (người dùng dặn 2026-11-02, gốc SKILL-PCCC-NEW `skills/SKILL_quy_trinh_tham_dinh.md`):
  TT | Nội dung đối chiếu | Nội dung thiết kế | Quy định | Khoản Điều | Kết luận (ĐẠT / CHƯA ĐẠT / CẦN XÁC NHẬN / KHÔNG ÁP DỤNG + lý do có số + giải pháp).
  Mỗi dòng là hạng mục CÓ trên bản vẽ; phương án mình đề xuất (PA1, PA2, thang ngoài nhà…) và kiến thức dùng để tư vấn (vd thang hở loại 2)
  ghi trong ô Kết luận của dòng liên quan, không thành dòng riêng. Mẫu: `ket_qua/canho/tu_van_thiet_ke.md` (bản 6, chỉ có trên máy người dùng).
- Bảng ĐẦY ĐỦ ra FILE (`bang_doi_chieu.md` + Excel), không dán vào chat (người dùng dặn 2026-11-03; trước đó 02/11 dặn gửi đủ dòng trong tin nhắn,
  vì từng thiếu dòng 1–7 thông tin công trình: file phải đủ dòng).
- Khoanh và ghi chú lên CHÍNH PDF bản vẽ, khung đánh số trùng TT. Không chỉ trả văn bản chung chung.
- Việc của Claude là BÁO CÁO (nhận diện, đánh giá, phương án, khả thi, căn cứ); chấp nhận hay không là việc người dùng: không hỏi người dùng
  chọn, kể cả câu hỏi cuối lượt; điểm luật không rõ thì ghi "cần người dùng xem xét". Thiếu kiến thức thì nhờ người dùng train.
- Chỉ CẦN XÁC NHẬN khi thiếu số liệu thật (người dùng dặn 2026-11-02). Tum đủ tiêu chí 1.4.24 (≤ 30% sàn mái, chỉ che thang) không tính tầng: ghi thông tin, không hỏi.
  Khoảng cách thoát nạn tính ra (hoặc Bảng G) vượt giới hạn ⇒ CHƯA ĐẠT + phương án, không để CẦN XÁC NHẬN.
- Không có số trên bản vẽ thì ghi "phải đảm bảo ≥ …". Chỉ ghi "cửa chống cháy" khi bản vẽ có ký hiệu.
- Thẻ bản vẽ mẫu đã thẩm duyệt (`data/ban_ve_mau`, 36 MB, repo SKILL-PCCC-NEW) chưa chép sang: clone repo đó vào thư mục tạm khi cần tra.

## Đầu vào
1. Bản vẽ PDF (mặt bằng; có thể kèm mặt đứng, mặt cắt): làm đủ quy trình dưới.
2. Chỉ thông tin bằng chữ: bỏ bước PDF; dòng cần bản vẽ (số lối, thang, khoảng cách) là CẦN XÁC NHẬN và nêu yêu cầu theo quy chuẩn.
   Chiều cao không có thì ước lượng theo `knowledge/khao-sat-bao-gia.md` mục "Ước lượng chiều cao", có nhãn "ước lượng".

## Quy trình
```
B=.claude/skills/tkpccc-bang-doi-chieu/scripts/bang_doi_chieu.py
G=.claude/skills/tkpccc-ghi-chu-ban-ve/scripts/ghi_chu_pdf.py
```
0. `ket_qua/<dự án>/` (không commit): chép PDF thành `ban_ve.pdf`. Render từng trang có lưới: `python -X utf8 $G luoi … --trang N --zoom 1.6 -o …/anh/luoi_N.png`,
   xem ảnh, ghi trang nào là mặt bằng tầng nào, mặt đứng, mặt cắt.
1. `python -X utf8 $B lamviec KS -o ket_qua/<dự án>/doi_chieu.json`, điền `cong_trinh` (mỗi số liệu kèm `nguon`; thiếu ghi vào `thieu`).
2. Điền từng dòng KS: `thiet_ke` (có số, ghi trang), `quy_dinh` + `khoan_dieu` (nguyên văn từ file ở cột Tra ở), `ket_luan`, `ly_do`,
   `phuong_an`, `khung` = [TT]. Thang: mỗi thang một dòng (11a, 11b…).
3. Ghi chú lên PDF: thêm mục vào `ghi_chu` của cùng file theo quy tắc của skill `tkpccc-ghi-chu-ban-ve` (khung số = TT; thiếu số liệu màu cam;
   phương án vẽ bằng `tuong`, `cua`, `net`; trang 1 có ô tiêu đề "TƯ VẤN SƠ BỘ PCCC…").
4. Kiểm và xuất: `kiem` (0 lỗi; xử lý cảnh báo), `xlsx`, `ve … --anh`, XEM ảnh và sửa tọa độ.
5. BÁO CÁO KHẢO SÁT theo mẫu MẶC ĐỊNH `data/mau_bao_cao/BCKS_mau.xlsx` (người dùng dặn 2026-11-02: luôn dùng mẫu này nếu không gửi mẫu
   khác; gửi mẫu khác thì `--mau`): viết khoá `bcks` trong `doi_chieu.json` (tóm tắt bảng KS theo 7 mục của mẫu, văn phong như mẫu:
   "- …", "=> CĐT cần …", "(chưa đảm bảo)"; ảnh cắt từ `ban_ve_ghi_chu.pdf` vào cột GHI CHÚ), chạy `bcks … -o ket_qua/<dự án>/BCKS_<tên>.xlsx`,
   xuất PDF bằng Excel (ExportAsFixedFormat) và XEM ảnh bản in. Cách viết khoá: skill `tkpccc-bang-doi-chieu` mục `bcks`.
6. Trả lời: gửi file `bang_doi_chieu.md` (`md --ma KS`, đủ dòng) + đường dẫn `BCKS_<tên>.xlsx`, `doi_chieu.xlsx` và `ban_ve_ghi_chu.pdf`; tóm tắt 3–5 dòng
   các điểm CHƯA ĐẠT, phương án, số liệu cần khách bổ sung.

## Sau này
- Mẫu Word hoặc mẫu Excel bố cục khác: thêm cách điền khi người dùng gửi (ROADMAP Pha 5b).
- Vẽ phương án trực tiếp trong AutoCAD qua MCP từ cùng `ghi_chu` (ROADMAP Pha 6).
