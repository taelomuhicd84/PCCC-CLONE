# Nhận dạng phòng, dựng vùng rải

Mã: `MatChon`, `CacMat`, `PhongThat`, `CumChinh`, `VungTaiDiem`, `LamGon` (`cad/module/plugin/NhanVung.cs`); `PCCC_RAI` (`cad/module/plugin/Rai.cs`); thao tác spec `vung_tu_dong` (`cad/skills/TU_VUNG.md`).
Nét nào là tường: `tuong_cot.md`. Cửa, thang, khu bỏ: `cua_thang_khu.md`. Khoang dầm, vì kèo: `dam_khoang.md`.

## Quy trình (người dùng dặn 16/11 và 19/11)
1. Vẽ vùng: polyline kín layer `PCCC_VUNG`, ghi rõ vùng nào bỏ và lý do (lõi thang, sảnh thang máy, buồng thang, WC...), in ảnh gửi người dùng.
2. Người dùng DUYỆT (hoặc sửa trong CAD) xong mới rải, đi dây, đi ống. Áp cho mọi lần thiết kế mới / thiết kế lại một tầng; không áp cho sửa plugin, chạy test.
3. Rải xong, người dùng chốt vị trí thì tự xoá vùng (`{"op":"xoa","l":"PCCC_VUNG"}`), không hỏi: vùng chỉ là nét phụ. Sửa rải sau đó thì dựng lại vùng đúng chỗ đó rồi xoá lại.
4. Sửa đầu báo đã rải: xoá theo handle (`doc` `gi:"dt"` trước), KHÔNG `xoa` theo hộp (xoá nhầm đầu báo khoang kề, 19/11).

## Luật vùng
- Vùng chạy theo mặt tường thật, kẹp sát tường, không bao cột lồi, không bao nội thất (`tuong_cot.md`). Cạnh vùng là tường thật, không lấy hộp bao.
- Chỉ trong đường trục ngoài cùng (`truc.md`).
- Phòng nhỏ có nét tường bao riêng (kể cả dải tam giác hẹp sát tường xiên, phòng 12 m² ở góc) là MỘT vùng, rải trong tường của nó, không cắt theo khoang xưởng.
- Phòng nhỏ kín (phòng 5 Quyết Thắng) vẫn có vùng và đầu báo.
- Dải không có tường ngăn với xưởng thì gộp vào khoang xưởng kề bên (cạnh theo tường xiên thật). Vách ngăn trong xưởng cắt khoang theo vách.
- Trước khi vẽ `PCCC_VUNG`: liệt kê tường kín -> phòng; phần còn lại của xưởng -> khoang vì kèo. Dò mọi nét tường trong khoang trước khi rải.
- Bản mẫu đã thẩm định chỉ là tham khảo CÁCH chia vùng (người dùng 20/11), số đầu báo vẫn theo TCVN 7568-14:2025.

## Plugin dựng phòng thế nào ("Enter mọi phòng" của `PCCC_RAI`)
1. Gom đoạn nét từ đối tượng quét chọn (`ThuNet`), tách cột, nhận trục, khép khe cửa, dựng mọi mặt kín (`CacMat`), chỉ giữ mặt có tâm trong khung chọn.
2. `PhongThat`: mặt ≥ 1 m², rộng TB (2A/P) ≥ 1 m, không chứa mặt khác bên trong.
3. Lọc lần lượt, mỗi lọc in một dòng: ô X, gạch chéo (`bo_phong=`), ngoài trục (`bo_ngoai_truc=`), WC / thang (`bo_khu_khong_bao=`), sảnh thang (`bo_sanh_thang=`),
   chữ khu (`bo_khu_ten=`), hiên (`bo_hien=`), cụm phòng nhỏ tách xa mặt bằng chính (hình chi tiết, mặt cắt: `bo_cum=`).
4. Mặt tự dựng được làm gọn (`LamGon`: bỏ bậc tường, học trần, góc cột) rồi vẽ lên `PCCC_VUNG`.
Bấm điểm trong phòng (`VungTaiDiem`): dò dần bán kính từ 10 m, lấy mặt kín nhỏ nhất chứa điểm; không áp các bộ lọc trên (bấm = cho phép).

## Xưởng
Khoang theo vì kèo / dầm: `dam_khoang.md`. Bài học xưởng Quyết Thắng (L1–L8, số liệu đối chiếu): `cad/skills/mau/phan-vung-xuong-mai-keo.md`.
