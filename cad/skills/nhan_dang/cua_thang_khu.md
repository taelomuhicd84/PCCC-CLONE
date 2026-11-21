# Nhận dạng cửa, thang, khu không rải

Mã: `CacMat` (khép khe cửa), `GomCua`, `GomKhu`, `HinhBac`, `TachOX`, `GomChuKhu`, `LoaiKhuTen` trong `cad/module/plugin/NhanVung.cs`; các bộ lọc "Enter mọi phòng" ở `cad/module/plugin/Rai.cs`.

## Cửa (khép ô cửa để phòng kín)
- Khe giữa hai đoạn tường thẳng hàng ≤ `HO_CUA_M` (3 m): luôn khép (chỉ khe gần nhất ở mỗi đầu đoạn).
- Khe 3 m tới `HO_CUA_MAX_M` (12 m): chỉ khép khi có ký hiệu cửa NẰM TRONG khe (hộp cửa phủ ≥ 25 % chiều dài khe), hoặc tường 2 nét hở đúng chỗ đó (khe ≤ 6 m).
  Người dùng 14/11: "cứ có cửa mình khép lại 1 vùng". Khe 11 m có cung nắp bồn cầu chạm một đầu thì không khép (KOCHAM 17/11).
- Khe chữ T có cửa (cửa giữa đầu tường và tường vuông góc): đầu tường cách hộp cửa ≤ 0,5 m, chiếu tới nét gần nhất trong 12 m, tia cắt hộp cửa thì khép.
- Khe XUYÊN không gian liền thì KHÔNG khép (Karaoke ABC 21/11, `dauXuyen` trong `CacMat`): tường ngăn hai bên hành lang thẳng hàng (mặt tường cùng
  toạ độ) làm khe ngang hành lang 1,7 m trông như ô cửa, hành lang vỡ 5 vùng; mẫu đã thẩm định rải đều cả hành lang. Khe 0,7 m tới max(3 m, 6 m)
  mà ở CẢ HAI đầu có nét vuông góc chạy liền hai phía (mặt hành lang chạy tiếp qua đầu tường, chữ T) trong 0,6 m trước hoặc sau đầu khe
  (bề dày tường; đầu khe ở mặt trụ lồi) thì bỏ; phía là ô cửa (hộp ký hiệu cửa nới 0,3 m) cũng tính là chạy tiếp (má cửa phòng thẳng hàng tường ngăn).
  Có ký hiệu cửa NẰM TRONG khe thì vẫn khép (cửa ngang hành lang chia vùng). Miệng lối đi, sảnh (góc chữ L), má cửa thường (nắp đầu tường một phía) vẫn khép.
  Trên bản vẽ nhà máy thật bỏ thêm ~900 khe ≤ 6 m: đều là cầu ảo vắt qua WC, mái che, lưới ngoài nhà (đã xem ảnh 21/11).
- Block cửa (hộp ≤ 3,5 m, có cung cánh cửa bên trong) KHÔNG góp nét làm tường, kể cả khi đặt trên layer tường (Karaoke ABC 21/11, người dùng khoanh đỏ):
  khuôn / cánh cửa bên này lối thẳng hàng cánh cửa phòng bên kia thành khe ≤ 3 m bị khép ngang lối, lối 1,5 m mất dải 1 m cuối; cánh cửa mở ra hành lang
  cắt tiền sảnh 1 m khỏi vùng hành lang. Chỉ gom ký hiệu cửa (cờ `TrongCua` trong `ThuNet`). Ô cửa vẫn khép nhờ khe giữa hai má tường.
- Block tên / layer CỬA mà hộp bao rộng CẢ HAI chiều > 3,5 m không phải một cửa (căn hộ 441 21/11: block xref gom mọi cửa của tầng, hộp = cả mặt bằng,
  làm mọi khe 3–12 m thành "có cửa trong khe", phòng vỡ vụn): không lấy hộp; cung cánh cửa bên trong vẫn được gom khi đọc lồng (`GomCua`, test E15).
- Chưa giải (căn hộ 441): cột vẽ bằng KHE HỞ tường (2 nắp đầu tường + hatch) ở hai đầu một phòng hẹp ≤ 3 m: hai nắp thẳng hàng, khe ≤ 3 m luôn khép, chẻ phòng.
  Ngoại lệ "có cửa trong khe" của khe xuyên: hộp cửa chỉ nới 50 mm (0,3 m thì cửa vách ngang cách khe 0,2 m bị tính nhầm).
- Ký hiệu cửa: cung `ARC` hoặc cung `ELLIPSE` gần tròn (tỉ lệ trục ≥ 0,8; block cửa Karaoke `A$Cf159a6b7` vẽ cánh bằng elip tỉ lệ 0,9, 95°), block / layer tên CUA, DOOR.
- `vung_tu_dong` / bấm điểm: cửa rộng hơn 3 m (cửa cuốn) thì truyền `ho`. Lệnh này không dùng ký hiệu cửa (chỉ `PCCC_RAI` dùng), nên khe xuyên có má cửa vẫn khép.

## Thang, giếng, lỗ thông tầng
- Thang bộ (L7, 20/11): block / nét có tên thang (`THANGBO`, `STAIR`...) hoặc HÌNH bậc cách đều (`HinhBac`) -> khoét hộp bao khỏi vùng (nắn sát tường ≤ 0,3 m);
  thang chiếm ≥ 50 % phòng thì bỏ cả phòng (buồng thang). Buồng thang trong sảnh: khoét như lỗ thông tầng.
- Ô có dấu X (`TachOX`) = lỗ thông tầng, giếng thang máy: không rải (`o_x=`). Block thang máy cũng là giếng.
- Sảnh thang máy (người dùng 16–17/11): phòng ≤ 30 m² sát giếng thang (cách ≤ 0,6 m) không đặt đầu báo (`bo_sanh_thang=`).

## Khu không đặt đầu báo
- Lõi thang máy, sảnh thang máy, buồng thang bộ, WC (người dùng 16/11): phòng ≤ 60 m² chứa dấu hiệu (tên block, layer, chữ phòng; block `BON_CAU`...) bị bỏ (`bo_khu_khong_bao=`).
- Chữ khu (L5, 19/11): "MÁI CHE", "KHÔNG CÓ CÔNG NĂNG", "SÂN", "HIỆN HỮU" (cả khi không gạch chéo), "PHÒNG BƠM": phòng ≤ 100 m², hoặc ≤ 500 m² hình dải
  (rộng TB 2A/P ≤ 6 m, review 21/11) chứa tâm chữ bị bỏ (`bo_khu_ten=`); phòng khác chỉ báo (`chu_khu_trong_phong_lon`): người dùng vẽ `PCCC_VUNG` tách ra.
  MaiKeo: khoang chứa chữ chỉ bỏ khi là khoang đầu / cuối theo phương chia và < 0,9 x diện tích khoang trung vị (`mai_keo bo_khoang_khu_ten=`);
  chữ ở khoang giữa (mái che dọc cạnh dài không tường ngăn) chỉ báo (`mai_keo chu_khu_khoang_giua=`).
- Phòng bơm PCCC đứng riêng, không thuộc hạng mục nào: KHÔNG lắp báo cháy. Nằm trong hạng mục có báo cháy thì xét theo hạng mục đó.
  Plugin (review 21/11): phòng có chữ PHÒNG BƠM / TRẠM BƠM / NHÀ BƠM thuộc cụm mặt bằng chính (trong nhà, tầng hầm): vẫn rải, chỉ báo (`phong_bom_trong_nha`); nhà bơm tách rời (cụm nhỏ, kể cả sát bể nước) vẫn bỏ.
- Hiên (L6, chỉ `LoaiTran MaiKeo`): dải hẹp rộng TB ≤ 3 m, ≤ 200 m² có ≥ 2 cột không thuộc phòng khác, VÀ mép trong (cạnh sát phòng thật)
  là tường nét đôi (nét song song 0,08–0,45 m phủ ≥ 50 %), mép ngoài không phải tường liền (phủ ≤ 80 %; Quyết Thắng viền mái 2 nét phủ 34–50 %) -> bỏ (`bo_hien=`).
  Bản vẽ tường 1 nét, hành lang có cột trong tường ngoài: chỉ báo (`hien_nghi=`), vẫn rải (review 21/11). Hiên không vẽ cột: người dùng tự vẽ vùng.
- L5b (MaiKeo): phòng ≤ 100 m² không có chữ tên nào (≥ 3 chữ cái: KHO 1, BẢO VỆ giữ lại, review 21/11), sát (≤ 0,3 m) khoang đã bỏ vì mái che -> bỏ theo (`mai_keo bo_ke_mai_che=`).
- Ô không cột, không vì kèo, không nhãn phòng (sân trống): HỎI người dùng, không tự vẽ vùng.
- Mọi khu bỏ đều in ở dòng lệnh; cần rải thì bấm điểm trong phòng đó.

## Hộp kỹ thuật
Điểm lên dây, ống đứng: trước mắt lấy hộp gen thông tầng trên mặt bằng; sau này người dùng vẽ tay vị trí cho plugin nhận (16/11).
