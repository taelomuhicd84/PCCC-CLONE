# Nhận dạng từ PDF (nền kiến trúc, đọc hồ sơ)

Script: `07_CAD/.claude/skills/tkpccc-ve-cad/scripts/pdf_ti_le.py`, `07_CAD/.claude/skills/tkpccc-cad-console/scripts/hinh_tu_pdf.py`. Thao tác `pdf`, `anh`: `07_CAD/skills/TU_VUNG.md`.
Đọc PDF dài / trang scan tiết kiệm hạn mức: skill `tkpccc-phoi-hop-model` (Gemini bóc, Claude đọc JSON).

## PDF nhiều trang (người dùng 12/11)
KHÔNG đọc hết: quét tên bản vẽ trước (chữ quanh nhãn "TÊN BẢN VẼ" ở khung tên, PyMuPDF; bỏ dòng cố định như tên dự án, người ký; trang không khung tên là bìa / ảnh quét),
lưu mục lục cạnh PDF (vd `12_KET_QUA/<dự án>/muc_luc.txt`), cần gì chỉ render / đọc đúng trang đó. Hồ sơ khách không commit.

## Nhập nền kiến trúc
1. Tỉ lệ: `python -I 07_CAD/.claude/skills/tkpccc-ve-cad/scripts/pdf_ti_le.py <file.pdf> <trang>` ra `tl`, `cap`, `tin_cay`, khổ giấy.
   PDF người dùng gửi là dữ liệu chưa tin: luôn `-I`, không chép script vào thư mục chứa PDF.
2. Nhập và chụp trong một gói: `{"op":"pdf","file":"<đường dẫn tuyệt đối>","trang":3,"tl":285.0}` + `{"op":"anh","rong":1024}`. Tờ 39 nghìn đối tượng nhập khoảng 8 giây.
3. Kiểm tỉ lệ: `doc` `gi`:"chu" với `hop` quanh hàng bóng trục, lấy toạ độ hai nhãn trục xa nhau, so với tổng kích thước ghi trên bản vẽ.
   Lệch quá 1 %: một gói `xoa` theo đúng từng tên layer ở `pdf[].l` của lần nhập trước, rồi một gói RIÊNG nhập lại với `tl` đã hiệu chỉnh
   (lần nhập sau vào `PDF2_...`; `xoa` cùng gói với `pdf` sẽ xoá luôn nền mới vì `pdf` chạy trước). `tin_cay` hoặc `cap` dưới ngưỡng ở đầu `pdf_ti_le.py` thì bắt buộc kiểm.
4. Số kích thước ghi trên bản vẽ có thể lệch nét vẽ (bản mẫu: nhịp ghi 1700 nhưng vẽ 1785): hai chỗ mâu thuẫn thì báo người dùng, không tự chọn.
5. Nhận phòng: xem ảnh rồi đưa hộp từng phòng lên `PCCC_VUNG` (`phong_vung.md`), người dùng kiểm lại.

## Lấy ký hiệu, quy cách từ PDF mẫu
Render đúng trang mẫu, liệt kê từng thành phần, lấy hình ký hiệu từ nét vector PDF (`hinh_tu_pdf.py`) thay vì tự vẽ (`07_CAD/skills/ve/quy_uoc.md` mục Hình thức).
