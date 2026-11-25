# Nhận dạng ký hiệu thiết bị có sẵn trong bản vẽ

Mã: `dem` / `PCCC_BOC` (`07_CAD/module/plugin/Spec.cs`, `Lenh.cs`), nhận loại thiết bị khi đi dây (`07_CAD/module/plugin/DayBc.cs`).

- Thiết bị plugin chèn mang thuộc tính `MA_VT` = mã vật tư (`03_DU_LIEU/vat_tu.json` gốc repo, nguồn duy nhất). Đếm theo mã.
- Block của người dùng không có `MA_VT`: gán bằng `block_alias.json` (`PCCC_GAN`); block động có Visibility / Lookup gán từng trạng thái bằng khoá `"tên block|trạng thái"`.
- Đi dây nhận loại thiết bị theo thứ tự: (1) mã vật tư `BC_ZM`, `BC_CM`, `BC_R2ML`, `BC_MODULE`, `BC_TOHOP`, `BC_NUTAN*`, `BC_CHUONG*`, `BC_KHOI*`, `BC_NHIET*`, `BC_TT`;
  (2) không có mã thì theo chữ của block: tên block, chữ thuộc tính, tên block lồng bên trong ("NUT NHAN", "CHUONG", "ZM"...). Mã đuôi `_DC` hoặc chữ "DIA CHI" = thiết bị địa chỉ.
- Gốc block đặt xa hình (block `ZM`, `CM`, `RM` của bản mẫu): lấy tâm hình làm điểm nối.
- Bản vẽ mẫu thường dùng block tên vô nghĩa (`A$C...`): nhận theo hình + chữ bên trong (đầu báo nhiệt "H", khói "S", block lồng chuông...).
  Bảng tên block của từng bản mẫu: `07_CAD/skills/mau/hoc-bao-chay-ban-mau.md` (TESTBCHAY_20_11), `07_CAD/skills/mau/hoc-ban-ve-mau-dwg.md`.
