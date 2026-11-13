# cad/data — dữ liệu vẽ

Dữ liệu ở đây là thứ plugin và AI ĐỌC để vẽ. Số liệu phải có nguồn (catalog, tiêu chuẩn, file của người dùng); không điền theo trí nhớ.

| Thư mục | Chứa gì | Hiện có | Còn chờ |
|---|---|---|---|
| `ky_hieu/` | Mặc định ký hiệu trục, kích thước, chữ; hình block mặc định báo cháy | `ky_hieu.json` (plugin đọc ở mỗi lần `PCCC_SPEC`), `hinh_bc.json` (nét lấy từ bảng ký hiệu trang 11 hồ sơ thật bằng `cad/.claude/skills/tkpccc-cad-console/scripts/hinh_tu_pdf.py`; `PCCC_LIB` vẽ block mặc định theo đó) | kiểu kích thước, khung tên |
| `block/` | Thư viện block; block động co, tê, van | chưa có file ở đây | file `.dwg` block động do người dùng vẽ bằng BEDIT |
| `ong/` | Kích thước ống và phụ kiện theo DN | chưa có | catalog ống, phụ kiện (đường kính ngoài, bán kính co, kích thước tê) |
| `bom/` | Kích thước bơm, cụm bơm | chưa có | catalog bơm của hãng đang dùng |
| `mau/` | Bản vẽ mẫu, khung tên | chưa có | file `.dwt` / `.dwg` mẫu của người dùng |

Hai dữ liệu đang dùng nằm NGOÀI thư mục này, cố ý không chép sang để khỏi lệch nhau:
- Danh mục vật tư: `data/vat_tu.json` ở gốc repo (nguồn duy nhất, dùng chung với báo giá).
- Thư viện block thiết bị đang chạy: `cad/TKPCCC.bundle/Contents/PCCC_Blocks.dwg` và `block_alias.json`. Plugin đọc và ghi hai file này cạnh DLL, người dùng sửa trực tiếp ở đó.

Thêm loại dữ liệu mới: tạo thư mục con, ghi 1 dòng vào bảng trên, thêm lệnh chép trong `cad/build.bat` nếu plugin cần đọc.
