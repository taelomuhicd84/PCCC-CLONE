# Layer kiến trúc (tường, vách, cửa, kích thước)

Plugin đọc tên, màu, độ dày mặc định ở nhóm `kien_truc` của `cad/data/ky_hieu/ky_hieu.json` (sửa ở đó rồi `build.bat`). Bảng này chỉ ghi chuẩn.

| Layer | Màu | Nét | Ai vẽ | Ghi chú |
|---|---|---|---|---|
| `TUONG` | 7 trắng | liền | `tuong`, `PCCC_TUONG`, má cửa của `cua` | dày mặc định 200 |
| `VACH` | 8 xám | liền | `tuong` với `vach:true`, `PCCC_TUONG` tuỳ chọn Vach | dày mặc định 100 |
| `CUA` | 4 cyan | liền | cánh + cung mở của `cua`, `PCCC_CUA` | rộng mặc định 900 |
| `DIM` | 3 xanh lá | liền | `kt_chuoi` (kiểu `PCCC_KT`) | `PCCC_ADIM`, `PCCC_CDIM` dùng layer + dimstyle hiện hành |

Đổi tên thì giữ chữ nhận diện để `vung_tu_dong` dựng đúng phòng: tường có `TUONG` / `WALL` / `VACH` / `COT`, cửa có `CUA` / `DOOR`, kích thước có `DIM`
(danh sách `LOP_TUONG`, `LOP_BO` trong `module/plugin/NhanVung.cs`). Trục: nhóm `luoi_truc` của `ky_hieu.json` (`TRUC`, `KH_TRUC`, `DIM_TRUC`).
