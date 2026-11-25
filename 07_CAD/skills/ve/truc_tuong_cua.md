# Vẽ trục, tường, cột, cửa (dựng lại mặt bằng kiến trúc)

Thao tác spec (cú pháp đủ ở `07_CAD/skills/TU_VUNG.md`): `luoi_truc`, `tuong`, `cua`, `net`, `chu`, `kt_chuoi`. Mặc định cỡ, layer, màu: nhóm `luoi_truc`, `kien_truc`, `chu`, `kt`
của `07_CAD/data/ky_hieu/ky_hieu.json` (sửa ở đó rồi `build.bat`). Quy ước layer: `quy_uoc.md`. Nhận dạng từ bản gốc: `07_CAD/skills/nhan_dang/`.

| Đối tượng | Vẽ bằng | Đạt khi |
|---|---|---|
| Trục | `luoi_truc` (`p` = giao trục đầu trên nền; nhịp X, Y lấy từ chuỗi kích thước) | bóng trục trùng nền, tổng nhịp khớp chuỗi kích thước |
| Tường bao, tường theo trục | `tuong` (đường tim theo trục, vòng bao `kin`) | ảnh: hai nét tường trùng nền; `doc` layer `TUONG` đủ đoạn |
| Vách | `tuong` `vach`:true | |
| Cột | `net` `kin` layer `COT` | |
| Cửa | `cua` (tâm trên tường, rộng, phía cánh mở) | `vung_tu_dong` một điểm mỗi phòng ra đủ vùng, m² khớp bản gốc ± 5 % |
| Tên phòng, kích thước | `chu`, `kt_chuoi` | |

- Tường vẽ bằng `tuong` nên `vung_tu_dong` chỉ dò layer tường, không lẫn nét PDF nền.
- Cửa rộng hơn 3 m (cửa cuốn) thì truyền `ho` khi dựng vùng.
- Mặc định trục 20/11: layer `TRUC` nét CENTER màu 1, bóng `KH_TRUC` màu 3, kích thước `DIM_TRUC` màu 5, chữ cao 350 (`ky_hieu.json`).
