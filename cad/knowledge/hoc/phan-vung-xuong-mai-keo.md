# Dạy plugin phân vùng như AI: xưởng mái kèo, phòng nhỏ, khu không rải (20/11/2026)

Nguồn: người dùng cho AI và plugin cùng phân vùng, rải đầu báo trên hai bản chép của một mặt bằng xưởng thật (hồ sơ khách, bảng đối chiếu ở
`ket_qua/QUYET_THANG/cad/so_sanh/`, không commit). Bộ vùng của AI là bộ người dùng đã duyệt 19/11 (luật ở bộ nhớ `phan-vung-xuong-va-phong-nho`).
Người dùng: "train cho plugin phân vùng và rải được như AI ... để không có AI mà tôi vẫn dùng được bằng plugin".

Kết quả: AI ra 25 vùng, 55 đầu báo. Plugin (tự dựng mọi phòng) ra 35 vùng, 69 đầu báo: chia xưởng sai khoang, thừa khoảng 19 đầu báo ở mái che, hiên
và phòng bơm, thiếu 2 phòng văn phòng.

## Luật AI dùng (người dùng đã duyệt) và plugin còn thiếu

| # | Luật | Plugin hiện tại | Cần làm |
|---|---|---|---|
| L1 | Cột nhận theo HÌNH, không theo tên layer: đa giác kín nhỏ (cạnh ≤ 1,2 m: chữ I 12–13 đỉnh, chữ nhật, vuông, tròn) lặp lại thẳng hàng ≥ 3 lần là cột. Bản vẽ thật để cột chữ I trên layer tên ống (`$.2.9DN25`) | Nét cột thành tường; bản cánh / bụng chữ I (2 nét song song cách 96–131 mm) bị coi là "tường 2 nét" rồi bắc cầu khe tới cột kế bên (≤ `HO_CUA_MAX_M` 12 m) thành tường ảo, ra ô giả 4,7 x 5,2 m giữa hai hàng cột | Gom ký hiệu cột trước khi dựng mặt (hộp ≤ 1,2 m, kín, lặp theo lưới); nét của cột không vào `doan`, không vào bắc cầu `SongDoi` / `GomCua`; tâm cột vào danh sách cột |
| L2 | Xưởng mái kèo (người dùng chọn loại trần "không trần" + kết cấu mái kèo, xà gồ > 0,3 m): khoang = giữa hai hàng vì kèo (tim hàng cột), chạy hết bề rộng xưởng tới tường; KHÔNG chia theo xà gồ | Không có | Hàng vì kèo = hàng cột cắt ngang nhịp (các cột cùng toạ độ theo phương dài, đứng ở hai tường biên); chia phòng xưởng bằng các đường tim đó (dùng lại `ChiaKhoang` với "dầm" là đường tim hàng cột kéo tới tường). Thêm lựa chọn kết cấu MaiKeo cạnh `LoaiTran` |
| L3 | Phòng có tường bao riêng (kể cả dải tam giác hẹp sát tường xiên, phòng 12 m² ở góc) = MỘT vùng, không cắt theo hàng vì kèo | Dải xiên, phòng nhỏ: đúng | Giữ; khi chia khoang (L2) chỉ chia phòng lớn có lưới cột bên trong, không chia phòng nhỏ không có cột ở trong |
| L4 | Vách ngăn trong xưởng cắt khoang; phần dải không có vách thì gộp vào khoang xưởng kề bên, cạnh vùng theo tường xiên thật | Đúng ở dải xiên | Giữ |
| L5 | Bỏ khu có chữ "MÁI CHE" / "KHÔNG CÓ CÔNG NĂNG", "SÂN", "HIỆN HỮU" (cả khi không gạch chéo), "PHÒNG BƠM" (phòng bơm PCCC đứng riêng, không thuộc hạng mục) | Chỉ bỏ WC, thang, thiết bị vệ sinh (`KHU_KHONG_BAO`), ô X, gạch chéo | Thêm từ khoá vào nhóm bỏ (chữ trong mặt → mặt không rải), báo ở dòng lệnh "bỏ n khu: mái che, phòng bơm..." để người dùng bấm điểm nếu muốn giữ |
| L6 | Hiên: dải giữa tường phòng và mép mái mà mép ngoài chỉ có cột (không tường) → bỏ | 4 vùng hiên quanh nhà văn phòng, khoảng 8 đầu báo | Mặt có cạnh ngoài là đường nối các cột (sau L1 không còn tường ảo) hoặc là nét viền mái không có cột / tường ở trong, rộng TB ≤ 3 m bao quanh phòng → bỏ, báo ở dòng lệnh |
| L7 | Buồng thang bộ trong phòng khoét ra như lỗ thông tầng (vuông theo hộp ký hiệu thang) | Còn đỉnh theo cung cửa và bậc thang | Block / nét thang (`THANGBO`, `STAIR`, hình bậc) → khoét hộp bao khỏi vùng; `LamGon` bỏ đỉnh theo cung cửa |
| L8 | Không bỏ sót phòng có tường kín (2 phòng văn phòng ở góc ngoài khối xưởng) | Bỏ sót | Kiểm `CumChinh` (cụm < 20 % cụm lớn nhất) và `bo_ngoai_truc`: phòng kín có tường riêng, có tên phòng (VĂN PHÒNG...) thì giữ, chỉ bỏ cụm không có chữ tên phòng |

## Đã viết mã 20/11 (phiên Bot chat, CHƯA build, CHƯA test)
- L1: `NhanVung.cs` `GhiCotUng` (ThuNet, nhánh polyline, chỉ khi `n.GomCot`) + `TachCot` (cùng cỡ ± 50 mm, ≥ 3 cái thẳng hàng, tâm lệch ≤ 0,15 m);
  `CacMat(..., khongNoi)` bỏ nét cột ở bước khép khe 1, 1b (nét cột vẫn dựng mặt, mặt cột < 1 m² PhongThat bỏ). Dòng lệnh `cot=`.
- L2: `PCCC_RAI` `LoaiTran` → `MaiKeo` (loaiTran = 3, palette mục 4 "Mái kèo"): không hỏi nét dầm, `DuongViKeo` + `ChiaKhoang` cho phòng ≥ 200 m², hai cạnh ≥ 10 m;
  hàng = toạ độ có ≥ 2 cột (mọi cột), lấy cột cách hộp bao phòng ≤ 6 m, hàng cách biên > 1 m; phương nhiều hàng hơn. Dòng lệnh `mai_keo cot= phong_chia= khoang=`.
- L5: `GomChuKhu` + `LoaiKhuTen` (`KHU_TEN`); Rai.cs khối BC: bỏ phòng ≤ 500 m² chứa tâm chữ (`bo_khu_ten=`), lớn hơn chỉ báo `chu_khu_trong_phong_lon`.
- L8: chữ có "PHONG" (không phải phòng bơm) → `TenPhong`; giữ phòng đó ở `bo_ngoai_truc` và `CumChinh(..., giu)`.
- L6 (làm 20/11 chiều, Claude Code, chỉ khi `LoaiTran MaiKeo`): `Rai.cs` sau L5, dải hẹp rộng TB ≤ 3 m có ≥ 2 cột không thuộc phòng khác = hiên, bỏ (`bo_hien=`).
- L5b (20/11, người dùng "Không rải" khu mái che tây nam): chỉ khi MaiKeo, phòng ≤ 100 m² không tên PHÒNG, không phải khoang xưởng, sát (≤ 0,3 m) khoang đã bỏ vì chữ mái che thì bỏ theo, lan dần.
- CHƯA làm: L7 (khoét thang bộ); test tổng hợp (mục Thử bên dưới). `chay_thu.ps1` đã đổi chuỗi dò `[CoTran/KhongTran/HonHop/MaiKeo]` (ca 12, 13).
- Rủi ro cần soát khi test: ca 12 (`bo_ngoai_truc=1`) đổi nếu phòng ngoài trục có chữ "PHÒNG"; cửa sổ / thiết bị kín nhỏ lặp thẳng hàng bị coi là cột (chỉ mất quyền khép khe,
  khe tường hai bên vẫn khép); chữ MÁI CHE nằm trong xưởng khi mái che không có tường ngăn (khi đó chỉ báo, không bỏ).

Rải (sau khi vùng đúng): xưởng theo h người dùng nhập (9 m), văn phòng theo trần văn phòng; mỗi khoang rải độc lập cho ra 2 đầu (khoang 20 x 6 m) / 3 đầu
(khoang 30 x 6 m), cách tường 5 m, cách vì kèo 3 m. Plugin đã làm đúng phần này khi vùng đúng (cùng hàm `LuoiVung`).

## Thử
- Test tổng hợp không cần hồ sơ khách (`test/chay_thu.ps1`): nhà giả 2 xưởng 20 x 48 m và 30 x 48 m, cột chữ I trên layer tên `$.2.9DN25` lưới 6 m ở hai tường biên, nét dầm nối cột,
  khe X giữa hai xưởng, dải mái che có chữ "MÁI CHE (KHÔNG CÓ CÔNG NĂNG)", nhà văn phòng có hiên cột bao quanh, phòng "PHÒNG BƠM PCCC" đứng riêng,
  khu gạch chéo "CÔNG TRÌNH HIỆN HỮU", 2 phòng văn phòng ở góc ngoài, dải tam giác sát tường xiên có vách. Đáp án: 8 + 8 khoang, dải xiên 1 vùng, 2 phòng góc,
  0 vùng ở mái che / hiên / phòng bơm / hiện hữu / ô X.
- Thử cục bộ trên bản vẽ thật (không commit): chạy tự dựng vùng trên mặt bằng dưới, so với đáp án `ket_qua/QUYET_THANG/cad/so_sanh/ai_vung_tren.json` (25 vùng, toạ độ mặt bằng dưới).
