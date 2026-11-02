---
name: tkpccc-ghi-chu-ban-ve
description: Ghi comment và vẽ phương án lên PDF bản vẽ PCCC bằng annotation thật (khung, callout, leader, nét, tường mới, cửa chống cháy, ô chữ), sửa/xoá được trong Foxit, Acrobat. Dùng khi khoanh vị trí không đạt, thiếu số liệu, nghi vấn trên bản vẽ; khi vẽ phương án (đóng kín buồng thang, vị trí cửa chống cháy, tường mới, đường thoát nạn); khi đánh số khung trùng cột TT/Khung của bảng đối chiếu; hoặc người dùng nói "ghi chú lên PDF", "khoanh bản vẽ", "comment bản vẽ", "vẽ phương án".
---
# Module: ghi chú + phương án lên PDF bản vẽ
Module này chỉ VẼ. Nội dung (kết luận, lý do, phương án) lấy từ bảng đối chiếu (skill `tkpccc-bang-doi-chieu`).
Phát triển module này: chỉ sửa trong thư mục này.

| File | Việc |
|---|---|
| `scripts/ghi_chu_pdf.py` | CLI `ve` (vẽ), `luoi` (ảnh trang có lưới tọa độ), `tim` (vị trí chữ để neo) |
| `test/test_ghi_chu_pdf.py` | `python -m pytest .claude/skills/tkpccc-ghi-chu-ban-ve/test -q` |

## Lệnh
```
S=.claude/skills/tkpccc-ghi-chu-ban-ve/scripts/ghi_chu_pdf.py
python -X utf8 $S luoi ket_qua/<da>/ban_ve.pdf --trang 3 --zoom 1.6 -o ket_qua/<da>/anh/luoi_3.png
python -X utf8 $S tim  ket_qua/<da>/ban_ve.pdf "CT1" --zoom 1.6
python -X utf8 $S ve   ket_qua/<da>/ban_ve.pdf ket_qua/<da>/doi_chieu.json -o ket_qua/<da>/ban_ve_ghi_chu.pdf --anh ket_qua/<da>/anh/ra
```
`ve` đọc khoá `ghi_chu` (và `zoom`) của `doi_chieu.json`, hoặc một file chỉ có danh sách mục. Mục sai thì báo `ghi_chu[i]: …` cho từng mục và không ghi file.

## Đặc tả mục ghi chú
Tọa độ = px trên ảnh `luoi` ở `zoom` (khoá `zoom` gốc file, mặc định 1 = point PDF), đúng chiều NHÌN THẤY; trang có /Rotate tự đổi.
Trường chung: `trang` (từ 1), `kieu`, `mau`, `khung` (số = TT của bảng KS hoặc cột Khung khi thẩm định; danh sách = một ghi chú chung cho nhiều dòng), `noi_dung` (comment),
`phuong_an` ("PA1"), `nhan` (chữ ngắn hiện trên bản vẽ), `nhan_lech` [dx, dy], `zoom` (ghi đè cho riêng mục).

| kieu | Trường riêng | Vẽ ra |
|---|---|---|
| `khung` | `hop` [x0,y0,x1,y1] hoặc `neo` {"chu": "CT1", "thu": 1, "le": 6}; `net_dut`; `may` (viền mây) | khung chữ nhật + số khung (khung có `phuong_an` chỉ hiện `nhan`); comment ở khung |
| `callout` | `hop` (ô chữ), `den` [x,y] (chỗ chỉ tới), `khuyu` (tuỳ chọn), `co` (cỡ chữ) | ô chữ "[khung] PA: nội dung" + đường chỉ có mũi tên |
| `leader` | `tu` (đuôi, chỗ ghi nhãn), `den` (mũi tên) | đường dẫn mũi tên + nhãn (mặc định "[khung]"); comment ở đường |
| `net` | `diem` [[x,y],…], `net_dut`, `mui_ten`, `day` | đường nét + comment: đường thoát nạn, kích thước đo, ranh giới |
| `tuong` | `diem`, `day` (mặc định 3,5) | tường mới của phương án (nét đậm) |
| `cua` | `tam` (bản lề), `den` (mép cánh khi đóng), `chieu` (1 hoặc -1), `loai` ("EI 60") | cánh mở 90° + cung quét + nhãn loại cửa |
| `cung` | `tam` (đầu mép bậc sát giếng thang), `den` (đầu mép bậc phía tường), `chieu` (1/-1, quay về phía chiếu nghỉ), `goc` (90) | cung vế thang: 1/4 tròn tô nhạt bán kính = bề rộng vế + đường bán kính; chiếu nghỉ phải chứa trọn cung, cánh cửa không quét vào (`knowledge/nhan-dien-thang-thoat-nan.md` bước 3) |
| `chu` | `hop`, `vien` | ô chữ tự do: tiêu đề trang, ghi chú chung |

Màu `mau`: `do` = CHƯA ĐẠT · `cam` = CẦN XÁC NHẬN, thiếu số liệu, nghi vấn · `xanh` = thông tin, yêu cầu · `tim` = PA1 · `xanh_la` = PA2, giải pháp đạt · `den`.

## Quy tắc ghi (người dùng dặn 2026-11-02)
1. Mỗi dòng CHƯA ĐẠT / CẦN XÁC NHẬN của bảng có ít nhất một mục cùng số `khung` đặt đúng chỗ trên bản vẽ.
   Ghi chú đặt NGAY TẠI đối tượng nó nói tới, mũi tên trỏ đúng đối tượng đó. Chiều cao PCCC (người dùng dặn 2026-11-02): trên mặt đứng
   hoặc mặt cắt vẽ `net` ngang tại MÉP DƯỚI CỬA SỔ tầng trên cùng + nhãn cao độ đo được, callout trỏ vào nét đó; không trỏ cao độ sàn.
2. Thiếu số liệu đầu vào: callout hoặc leader màu cam, ghi "Thiếu <số liệu>: phải đảm bảo ≥ … (điều)", đặt ở chỗ lẽ ra có số liệu
   (thang, cửa, mặt đứng, bảng thống kê). Nghi vấn: khung cam, comment nêu điều nghi và cần xác nhận gì.
3. Phương án điều chỉnh: vẽ bằng kiểu có nghĩa (`tuong`, `cua`, `net` đường thoát nạn), mỗi mục có `phuong_an` + `nhan`;
   mỗi phương án một màu (PA1 tím, PA2 xanh lá); comment ghi khả thi hay không và vì sao.
   Đóng kín buồng thang (người dùng dặn 2026-11-02): vẽ buồng thang bằng `tuong` NÉT LIỀN KHÉP KÍN (cả tường có sẵn lẫn tường mới) +
   `cua` + `cung` vế thang ở mọi đầu vế giáp chiếu tầng, cho TẤT CẢ các tầng có thang (hầm, tầng 1, lửng, tầng điển hình, tầng thượng),
   không chỉ tầng điển hình.
4. Trang đầu có mục `chu`: "TƯ VẤN SƠ BỘ PCCC (ngày) – tham khảo, không phải kết luận thẩm định" hoặc "THẨM ĐỊNH SƠ BỘ PCCC (ngày)".
5. Luôn chạy `ve --anh` rồi XEM ảnh, sửa tọa độ tới khi khung trùng hạng mục và chữ không đè nét chính của bản vẽ.
6. Chỉ dùng annotation, không vẽ vào nội dung trang (draw_*, insert_text), để người dùng sửa, xoá được.

## Đường sang CAD qua MCP (sau này, ROADMAP Pha 6)
`tuong`, `cua`, `net` là ĐỐI TƯỢNG CÓ NGHĨA (loại, giới hạn chịu lửa, phương án), không phải nét trang trí. Module CAD qua MCP sẽ đọc
cùng các mục này, đổi tọa độ trang PDF sang mm bản vẽ bằng 2 điểm hiệu chỉnh, rồi vẽ bằng lệnh `PCCC_*`. Cần thêm dữ liệu cho CAD
(vd `gioi_han_chiu_lua`, `rong_mm`) thì thêm trường vào mục, không đổi kiểu sẵn có.

## Giới hạn đã biết
- Cỡ chữ ô callout ước lượng (thu tới 5 pt rồi kéo dài ô xuống): kiểm bằng ảnh.
- `neo` cần chữ vector; bản scan thì đọc tọa độ trên ảnh `luoi`.
- PyMuPDF: giữ biến doc và page khi đọc annot; đối tượng đã bị thu hồi thì Python sập (access violation).
- Viền mây làm khung to ra vài point.
