# Quy ước bản vẽ CAD của TKPCCC

File này chỉ nói quy ước nằm ở đâu và vì sao. Con số cụ thể có một chỗ duy nhất, ghi ở cột "Nguồn".

| Quy ước | Nội dung | Nguồn (sửa ở đây) |
|---|---|---|
| Đơn vị | Bản vẽ theo `INSUNITS`: mm, cm hoặc m; đơn vị khác coi là mm. Block thư viện vẽ theo mm | `HeSoMet` trong `07_CAD/module/plugin/Lenh.cs` |
| Layer thiết bị | `PCCC_<hệ>`, màu theo hệ (báo cháy, sprinkler, họng nước, bình, đèn...) | `MauHe` trong `07_CAD/module/plugin/Lenh.cs`; hệ của từng mã ở `03_DU_LIEU/vat_tu.json` gốc |
| Layer ống, dây | `PCCC_<mã vật tư>` (vd mã `ONG_DN50` ra layer `PCCC_` + `ONG_DN50`); `PCCC_BOC` tính chiều dài theo layer này | `OpOng` trong `07_CAD/module/plugin/Spec.cs` |
| Trục | Layer trục, bóng trục, kích thước trục; cỡ chữ, bán kính bóng, đoạn kéo dài | `07_CAD/data/ky_hieu/ky_hieu.json` |
| Chữ | Kiểu `PCCC_ARIAL` (TrueType, có dấu tiếng Việt); `txt.shx` không có dấu | `DamBaoKieuChu` trong `07_CAD/module/plugin/Lenh.cs` |
| Kích thước | Kiểu `PCCC_KT`: số nguyên, chữ nằm trên đường kích thước, cỡ theo `cao` | `DamBaoKieuKt` trong `07_CAD/module/plugin/Spec.cs`; mặc định ở `ky_hieu.json` |
| Thuộc tính thiết bị | Block thiết bị mang thuộc tính `MA_VT` = mã vật tư để bóc khối lượng | `ChenThietBi` trong `07_CAD/module/plugin/Spec.cs` |
| Vùng | Polyline kín trên layer `PCCC_VUNG` là vùng để đọc diện tích, sau này để rải thiết bị | `OpDoc` trong `07_CAD/module/plugin/Spec.cs` |

Bài học người dùng dạy khi vẽ: quan sát + số liệu ghi vào `07_CAD/skills/mau/` (mỗi bài một file ngắn có ngày và tên hồ sơ); luật rút ra ghi vào đúng file của `07_CAD/skills/nhan_dang/` hoặc `07_CAD/skills/ve/`.

## Layer kiến trúc (tường, vách, cửa, kích thước)
Plugin đọc tên, màu, độ dày mặc định ở nhóm `kien_truc` của `07_CAD/data/ky_hieu/ky_hieu.json` (sửa ở đó rồi `build.bat`). Bảng này chỉ ghi chuẩn.

| Layer | Màu | Nét | Ai vẽ | Ghi chú |
|---|---|---|---|---|
| `TUONG` | 7 trắng | liền | `tuong`, `PCCC_TUONG`, má cửa của `cua` | dày mặc định 200 |
| `VACH` | 8 xám | liền | `tuong` với `vach:true`, `PCCC_TUONG` tuỳ chọn Vach | dày mặc định 100 |
| `CUA` | 4 cyan | liền | cánh + cung mở của `cua`, `PCCC_CUA` | rộng mặc định 900 |
| `DIM` | 3 xanh lá | liền | `kt_chuoi` (kiểu `PCCC_KT`) | `PCCC_ADIM`, `PCCC_CDIM` dùng layer + dimstyle hiện hành |

Đổi tên thì giữ chữ nhận diện để `vung_tu_dong` dựng đúng phòng: tường có `TUONG` / `WALL` / `VACH` / `COT`, cửa có `CUA` / `DOOR`, kích thước có `DIM`
(danh sách `LOP_TUONG`, `LOP_BO` trong `module/plugin/NhanVung.cs`). Trục: nhóm `luoi_truc` của `ky_hieu.json` (`TRUC`, `KH_TRUC`, `DIM_TRUC`).

Layer khác (thiết bị, ống, dây): chưa có chuẩn layer người dùng cấp thì dùng mặc định trong `07_CAD/data/ky_hieu/ky_hieu.json` và layer do `03_DU_LIEU/vat_tu.json` đặt.
Khi tạo layer qua `cad_chay` (`net`, `dat`, `ong`...) phải theo mục này. Người dùng sẽ nạp chuẩn layer qua lệnh plugin (việc lập trình, `07_CAD/TIEN_DO.md`).

## Hình thức bản vẽ plugin tự sinh (người dùng dặn 13/11)
- Mọi bản vẽ plugin tự sinh (sơ đồ nguyên lý, bảng ký hiệu...) theo quy cách GIỐNG 100 % bản mẫu người dùng gửi: đúng bố cục, chữ, bảng ký hiệu, bảng tính, ghi chú, đường dây.
  Bản "đơn giản hoá" (vòng tròn có chữ thay ký hiệu thật, thiếu bảng) bị chê "quá đơn điệu"; bản vẽ phải nộp được.
- HÌNH ký hiệu thiết bị tuỳ người, tuỳ bản vẽ: lấy block của chính bản vẽ / thư viện người dùng (`PCCC_THEMBLOCK`), không cố định một bộ ký hiệu trong mã;
  ký hiệu lấy từ PDF mẫu chỉ là bộ mặc định. Mỗi loại dây một layer (nét, màu) theo bản vẽ người dùng; bảng ký hiệu in mẫu nét theo layer đó.
- Trước khi vẽ một loại bản vẽ mới: render đúng trang mẫu (`07_CAD/skills/nhan_dang/pdf.md`), liệt kê từng thành phần, lấy hình ký hiệu từ nét vector PDF mẫu,
  so ảnh kết quả cạnh ảnh mẫu trước khi báo xong.
