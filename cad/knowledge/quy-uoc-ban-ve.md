# Quy ước bản vẽ CAD của TKPCCC

File này chỉ nói quy ước nằm ở đâu và vì sao. Con số cụ thể có một chỗ duy nhất, ghi ở cột "Nguồn".

| Quy ước | Nội dung | Nguồn (sửa ở đây) |
|---|---|---|
| Đơn vị | Bản vẽ theo `INSUNITS`: mm, cm hoặc m; đơn vị khác coi là mm. Block thư viện vẽ theo mm | `HeSoMet` trong `cad/module/plugin/Lenh.cs` |
| Layer thiết bị | `PCCC_<hệ>`, màu theo hệ (báo cháy, sprinkler, họng nước, bình, đèn...) | `MauHe` trong `cad/module/plugin/Lenh.cs`; hệ của từng mã ở `data/vat_tu.json` gốc |
| Layer ống, dây | `PCCC_<mã vật tư>` (vd `PCCC_ONG_DN50`); `PCCC_BOC` tính chiều dài theo layer này | `OpOng` trong `cad/module/plugin/Spec.cs` |
| Trục | Layer trục, bóng trục, kích thước trục; cỡ chữ, bán kính bóng, đoạn kéo dài | `cad/data/ky_hieu/ky_hieu.json` |
| Chữ | Kiểu `PCCC_ARIAL` (TrueType, có dấu tiếng Việt); `txt.shx` không có dấu | `DamBaoKieuChu` trong `cad/module/plugin/Lenh.cs` |
| Kích thước | Kiểu `PCCC_KT`: số nguyên, chữ nằm trên đường kích thước, cỡ theo `cao` | `DamBaoKieuKt` trong `cad/module/plugin/Spec.cs`; mặc định ở `ky_hieu.json` |
| Thuộc tính thiết bị | Block thiết bị mang thuộc tính `MA_VT` = mã vật tư để bóc khối lượng | `ChenThietBi` trong `cad/module/plugin/Spec.cs` |
| Vùng | Polyline kín trên layer `PCCC_VUNG` là vùng để đọc diện tích, sau này để rải thiết bị | `OpDoc` trong `cad/module/plugin/Spec.cs` |

Bài học người dùng dạy khi vẽ (đặt thiết bị ở đâu, đi ống thế nào, trình bày ra sao) ghi vào `cad/knowledge/hoc/`, mỗi bài một file ngắn có ngày và tên hồ sơ.
