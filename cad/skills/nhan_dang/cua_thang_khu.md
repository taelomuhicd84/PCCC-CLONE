# Nhận dạng cửa, thang, khu không rải

Mã: `CacMat` (khép khe cửa), `GomCua`, `GomKhu`, `HinhBac`, `TachOX`, `GomChuKhu`, `LoaiKhuTen` trong `cad/module/plugin/NhanVung.cs`; các bộ lọc "Enter mọi phòng" ở `cad/module/plugin/Rai.cs`.

## Cửa (khép ô cửa để phòng kín)
- Khe giữa hai đoạn tường thẳng hàng ≤ `HO_CUA_M` (3 m): luôn khép (chỉ khe gần nhất ở mỗi đầu đoạn).
- Khe 3 m tới `HO_CUA_MAX_M` (12 m): chỉ khép khi có ký hiệu cửa NẰM TRONG khe (hộp cửa phủ ≥ 25 % chiều dài khe), hoặc tường 2 nét hở đúng chỗ đó (khe ≤ 6 m).
  Người dùng 14/11: "cứ có cửa mình khép lại 1 vùng". Khe 11 m có cung nắp bồn cầu chạm một đầu thì không khép (KOCHAM 17/11).
- Khe chữ T có cửa (cửa giữa đầu tường và tường vuông góc): đầu tường cách hộp cửa ≤ 0,5 m, chiếu tới nét gần nhất trong 12 m, tia cắt hộp cửa thì khép.
- `vung_tu_dong` / bấm điểm: cửa rộng hơn 3 m (cửa cuốn) thì truyền `ho`.

## Thang, giếng, lỗ thông tầng
- Thang bộ (L7, 20/11): block / nét có tên thang (`THANGBO`, `STAIR`...) hoặc HÌNH bậc cách đều (`HinhBac`) -> khoét hộp bao khỏi vùng (nắn sát tường ≤ 0,3 m);
  thang chiếm ≥ 50 % phòng thì bỏ cả phòng (buồng thang). Buồng thang trong sảnh: khoét như lỗ thông tầng.
- Ô có dấu X (`TachOX`) = lỗ thông tầng, giếng thang máy: không rải (`o_x=`). Block thang máy cũng là giếng.
- Sảnh thang máy (người dùng 16–17/11): phòng ≤ 30 m² sát giếng thang (cách ≤ 0,6 m) không đặt đầu báo (`bo_sanh_thang=`).

## Khu không đặt đầu báo
- Lõi thang máy, sảnh thang máy, buồng thang bộ, WC (người dùng 16/11): phòng ≤ 60 m² chứa dấu hiệu (tên block, layer, chữ phòng; block `BON_CAU`...) bị bỏ (`bo_khu_khong_bao=`).
- Chữ khu (L5, 19/11): "MÁI CHE", "KHÔNG CÓ CÔNG NĂNG", "SÂN", "HIỆN HỮU" (cả khi không gạch chéo), "PHÒNG BƠM": phòng ≤ 500 m² chứa tâm chữ bị bỏ (`bo_khu_ten=`);
  phòng lớn hơn chỉ báo (`chu_khu_trong_phong_lon`): người dùng vẽ `PCCC_VUNG` tách ra.
- Phòng bơm PCCC đứng riêng, không thuộc hạng mục nào: KHÔNG lắp báo cháy. Nằm trong hạng mục có báo cháy thì xét theo hạng mục đó.
- Hiên (L6, chỉ `LoaiTran MaiKeo`): dải hẹp rộng TB ≤ 3 m, ≤ 200 m² có ≥ 2 cột không thuộc phòng khác -> bỏ (`bo_hien=`). Hiên không vẽ cột: người dùng tự vẽ vùng.
- L5b (MaiKeo): phòng ≤ 100 m² không có chữ PHÒNG, sát (≤ 0,3 m) khoang đã bỏ vì mái che -> bỏ theo (`mai_keo bo_ke_mai_che=`).
- Ô không cột, không vì kèo, không nhãn phòng (sân trống): HỎI người dùng, không tự vẽ vùng.
- Mọi khu bỏ đều in ở dòng lệnh; cần rải thì bấm điểm trong phòng đó.

## Hộp kỹ thuật
Điểm lên dây, ống đứng: trước mắt lấy hộp gen thông tầng trên mặt bằng; sau này người dùng vẽ tay vị trí cho plugin nhận (16/11).
