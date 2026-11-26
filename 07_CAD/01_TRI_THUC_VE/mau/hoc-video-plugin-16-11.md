# Học 3 video plugin PCCC (TikTok, người dùng gửi 16/11/2026)

Nguồn: 3 file `snaptik.vn_*.mp4` trong Downloads của người dùng (không chép vào repo). Xem bằng khung hình (ffmpeg của `imageio_ffmpeg`)
và lời thoại do Gemini bóc (skill `tkpccc-phoi-hop-model`). Hai plugin: **VPTTOOL – Thiết kế PCCC** (palette tối, 2 video) và **HTS_DESIGN / H-Tools** (palette sáng, 1 video).
Người dùng dặn: lấy tư duy hay đưa vào plugin, nhanh, tự động, không phụ thuộc nhiều vào AI; giao diện đẹp như mẫu.

## 1. Giao diện (đã làm 16/11)
| Ý của mẫu | Trong TKPCCC |
|---|---|
| Thanh dọc trái là ô biểu tượng màu, mỗi hệ một màu | `NutBen` (`05_NEN_TANG/ui/BangChinh.cs`): ô vuông chuyển sắc + emoji + nhãn ngắn |
| Tab con dạng viên thuốc, tab đang chọn tô màu nhấn | `ThanhTab` (`GiaoDien.cs`): tự xếp 2 × 2 khi hẹp; dùng ở trang Báo cháy và lưới Tiện ích / Chữa cháy / Thống kê |
| Nút hành động là hình tròn màu có biểu tượng, nhãn chữ hoa bên dưới, 3 nút một hàng | `NutIcon`, `GiaoDien.NutLuoi`, `XepLuoi` |
| Tiêu đề nhóm nhỏ chữ hoa mờ + vạch màu ("RANH GIỚI & LAYER", "RẢI & XÓA") | `TieuDeNhom` |
| Khung TRẠNG THÁI cuối bảng: ⚠ vàng việc chưa đủ, ✔ xanh việc đã xong | `KhungTrangThai`; trang Báo cháy đếm vùng PCCC_VUNG / loại trừ (`Lenh.DemVung`), thiết bị, trần, lượt rải cuối, lần kiểm vùng mù cuối |
| Nút "Tra cứu TCVN" ngay tại tab khoang dầm: bảng V1–V4 theo h, d, S | `BangTraCuu.cs` đọc mục 2–3 của `02_DIEU_KHOAN/07_BAO_CHAY/MOD_dau_bao_khoi_khoang_dam.md` (nguyên văn 5.9.1.1.6, build.bat chép cạnh DLL) |
| Trang Báo cháy chia tab RẢI ĐẦU BÁO · KHOANG DẦM · SĐNL · VẼ DÂY | `BangBoTri.cs`: RẢI ĐẦU BÁO · KHOANG DẦM · KIỂM TRA · ĐI DÂY |

## 2. Thuật toán, quy trình: đã có sẵn trong TKPCCC (không làm lại)
- Khoang dầm theo 5.9.1.1.6: `PCCC_RAI` từ khoá `Dam` đọc ký hiệu b x h, dầm ≤ 0,3 m bỏ, ô ≥ 4 / 9 m² rải từng ô (tự động hơn mẫu: mẫu bắt nhập d bằng tay).
- Vá hở nét: `NhanVung.cs` (khe cửa, chữ T, góc L hụt). Tự nhận phòng từ tường, ô X lỗ thông tầng, gạch chéo hiện hữu.
- Vẽ ống tự vào layer theo DN (`PCCC_ONG`, layer `PCCC_ONG_DNxx`), đổi DN (`PCCC_DOIDN`), bảng layer dây / ống (trang Cài đặt).
- Dim tự động sau khi rải (`KtSauRai`), preset, xem trước số lượng, vùng loại trừ, bố trí nhanh khung chữ nhật / đa giác.

## 3. Ý hay CHƯA làm (việc kế, xếp theo lợi ích; đều là thuật toán xác định, không cần AI)
1. ĐÃ LÀM 16/11 (`GhiKhoang`, phần bấm tay đặt giữa dầm chưa làm vì plugin đã tự rải khoang nhỏ ở mặt dưới dầm). **Ghi diện tích từng khoang + đánh dấu ô nhỏ** (VPTTOOL): sau khi rải khoang dầm, ghi chữ "17,3 m²" giữa mỗi khoang, ô dưới ngưỡng đánh dấu X màu hồng
   + bảng kết quả (n đầu đã rải, n khoang nhỏ, n khoang bỏ < 1 m²). Rồi chế độ bấm tay: bấm gần dầm → đặt đầu báo giữa bề rộng dầm gần nhất. Sửa ở `Rai.cs` (nhánh `khoangDam`).
2. ĐÃ LÀM 16/11 (`PCCC_TUYEN` TuDong, bảng người dùng gửi ở `03_DU_LIEU/ong_nhanh_dn.json`). **Ống nhánh tự chọn DN theo số đầu phun phía sau** (VPTTOOL "Ghi chú kích thước", bảng Đoạn 1 → DN25, 2 → DN32, 3 → DN32, 4–5 → DN40...): bảng sửa được ở trang Cài đặt,
   `PCCC_TUYEN` đếm đầu phun từ cuối nhánh về ống chính, gán layer `PCCC_ONG_DNxx` + chữ DN. Cần người dùng chốt bảng DN theo số đầu (TCVN 7336 không có bảng này, theo thói quen công ty).
3. **Bước nhảy (làm tròn bước rải tới 100 mm) và rải so le** (VPTTOOL hộp "Công cụ rải đầu phun"): thêm tuỳ chọn vào `ChiaBuoc` / `LuoiVung` (`Rai.cs`), mặc định tắt.
4. **Rải đa giác "Từng vùng"**: tách phòng chữ L, U thành các chữ nhật rồi rải riêng từng phần (ít góc chết). Hiện có "chia theo từng hàng" (`theo_hang`) gần giống.
5. **Lấy mẫu block trên bản vẽ** (VPTTOOL "Chọn mẫu / Áp dụng mẫu"): bấm ký hiệu trong bảng chú giải → plugin dùng đúng block, layer, tỉ lệ đó khi rải.
   Hiện `PCCC_GAN` chỉ gán block → mã để bóc khối lượng; cần thêm "rải bằng block mẫu".
6. **Nối nhánh 3 kiểu** Default / NearestBranch / Herringbone (VPTTOOL): `PCCC_TUYEN` có xương cá; thêm kiểu gom nhánh gần nhất.
7. **Hồ sơ dự án dùng chung cả nhóm** (HTS): xuất / nạp `cai_dat.json` + bảng layer thành file mẫu cho đồng nghiệp; tab layer theo hệ có ô màu.
8. **Xem trước rồi Áp dụng / Huỷ** trong một hộp thoại không chặn (VPTTOOL "Cập nhật"): đổi thông số là vẽ lại ngay trên layer xem trước.
