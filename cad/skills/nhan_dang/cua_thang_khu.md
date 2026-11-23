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
  Nét chữ T phải chạy ≥ 0,15 m MỖI phía đầu khe (23/11, Quyết Thắng): vạch đầu tường 0,2 m (±0,1 m) ở hai đầu ô cửa 5,6 m tường 1 nét xưởng 2 bị tính
  là chữ T, khe không khép, xưởng 2 không thành mặt, cả cụm phía đông bị `bo_cum` bỏ: 25 vùng / 57 ĐB còn 12 / 20 (lỗi từ 1bd93ea, 21/11).
  Ngưỡng 0,25–0,5 m thì Karaoke 123, trường học vỡ vùng lại (mặt hành lang sau trụ lồi ngắn); 0,15 m giữ nguyên số vùng các khung 3–6.
  Kiểm: `chay_thu.ps1` phép "Ban chep Quyet Thang" (chạy khi máy có `ket_qua/QUYET_THANG/cad/thu_maikeo/qt_maikeo.dwg`).
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
- Ô có dấu X (`TachOX`) = giếng thang máy: không rải (`o_x=`). Block thang máy cũng là giếng. (Lỗ thông tầng: xem dòng 23/11 dưới.)
- NGƯỜI DÙNG 23/11 (Karaoke 123 tầng 2, ảnh `ket_qua/HOC_BAO_CHAY/anh/k4t2_nguoidung_23_11.png`; THAY dòng trên cho sảnh / lỗ thông tầng, giếng
  thang máy vẫn không rải): sảnh thông tầng / lỗ thông tầng phải TÁCH THÀNH VÙNG RẢI RIÊNG — đầu báo, đầu phun của khu đó lắp trên ĐỈNH sảnh thông tầng;
  khu tiếp giáp (hành lang giáp sảnh, ô cạnh thang hở) vẫn là vùng riêng, rải trên trần tầng đó. Ranh giới hai vùng: đường kẻ thẳng theo mép sàn / mặt
  tường (đường xanh dương người dùng vẽ ở x ≈ 356515, tim trục giữa dãy phòng giữa và ô thang), KHÔNG gộp hành lang vào vùng thông tầng.
  Ô thang hở: đường bao bám mép bậc thang (đường đỏ người dùng: đi theo mép thang, khấc theo vế thang), khép kín, không kẻ thẳng băng ngang thang.
  Plugin ĐÃ LÀM 23/11 tối (in `thong_tang=N m2=..(dài x rộng)@tâm`): dấu X nhận cả trên layer bỏ (trục: Karaoke vẽ X trên `19- TIM TRUC`) và layer thường
  ở lượt chỉ-tường (`NetBo`, nét xiên). Ô X KHÔNG rải (`o_x=`, `OXKhongRai`) khi cỡ giếng thang máy (mọi cạnh ≤ 4 m và ≤ 12 m²) hoặc dải dài hẹp
  (cạnh dài ≥ 4 lần cạnh ngắn = khe giữa hai khối: Quyết Thắng khe 41,4 x 5 m giữa 2 xưởng, đáp án bỏ) hoặc > 300 m² (trường học khung 6: ô 19 x 32 m,
  mẫu không có thiết bị = sân / khoảng trống); còn lại là sảnh / lỗ thông tầng: rải như vùng thường. Dấu X từ nguồn mới (layer bỏ, layer thường lượt
  chỉ-tường) CHỈ nhận khi là thông tầng (trường học có 40 block "ống thoát nước" vẽ dấu X ~1 m: nhận làm giếng thì bỏ nhầm 5 phòng sát nó). Ô thông tầng không thành mặt kín (Karaoke: cạnh trên là thang hở, góc cửa kính hở) thì dựng mặt từ khung X + nét trong ô (mép thang, lan can),
  lấy mặt lớn nhất (`MatThongTang`): Karaoke T2 61,78 m² khấc theo vế thang. Test E24. shortcut: lỗ thông tầng nhỏ cỡ giếng hay dài hẹp vẫn bỏ (bấm điểm).
- Sảnh thang máy (người dùng 16–17/11): phòng ≤ 30 m² sát giếng thang (cách ≤ 0,6 m) không đặt đầu báo (`bo_sanh_thang=`).
  NGƯỜI DÙNG 21/11 (căn hộ 441, mẫu có đầu báo ở sảnh trước thang máy): chỉ bỏ khi sảnh nằm trong BUỒNG ĐỆM (≤ 2 ký hiệu cửa có tâm trong phòng hoặc
  cách biên ≤ 1 m) hoặc trong BUỒNG THANG (phòng chứa tâm hộp thang bộ). Sảnh tiếp tân, thông tầng, hành lang chung nhiều cửa phòng: RẢI theo mẫu (test E16).
  Người dùng nhắc lại 21/11: "sảnh thang máy thì không, sảnh tiếp tân hoặc sảnh thì có". Chữ trong phòng thắng phép đoán theo hình: có chữ SẢNH (không kèm
  THANG), TIẾP TÂN, LỄ TÂN thì luôn rải (`Sanh` trong `GomKhu`, test E20); chữ "SẢNH THANG MÁY" chứa THANG MÁY nên bỏ theo `bo_khu_khong_bao`.

## Khu không đặt đầu báo
- Lõi thang máy, sảnh thang máy, buồng thang bộ, WC (người dùng 16/11): phòng ≤ 60 m² chứa dấu hiệu (tên block, layer, chữ phòng; block `BON_CAU`...) bị bỏ (`bo_khu_khong_bao=`).
- Chữ khu (L5, 19/11): "MÁI CHE", "KHÔNG CÓ CÔNG NĂNG", "SÂN", "HIỆN HỮU" (cả khi không gạch chéo), "PHÒNG BƠM" (KHÔNG khớp "PHÒNG BỘ MÔN": bỏ dấu, bỏ cách cũng ra PHONGBOM…, trường học 21/11, test E21): phòng ≤ 100 m², hoặc ≤ 500 m² hình dải
  (rộng TB 2A/P ≤ 6 m, review 21/11) chứa tâm chữ bị bỏ (`bo_khu_ten=`); phòng khác chỉ báo (`chu_khu_trong_phong_lon`): người dùng vẽ `PCCC_VUNG` tách ra.
  MaiKeo: khoang chứa chữ chỉ bỏ khi là khoang đầu / cuối theo phương chia và < 0,9 x diện tích khoang trung vị (`mai_keo bo_khoang_khu_ten=`);
  chữ ở khoang giữa (mái che dọc cạnh dài không tường ngăn) chỉ báo (`mai_keo chu_khu_khoang_giua=`).
- Phòng bơm PCCC đứng riêng, không thuộc hạng mục nào: KHÔNG lắp báo cháy. Nằm trong hạng mục có báo cháy thì xét theo hạng mục đó.
  Plugin (review 21/11): phòng có chữ PHÒNG BƠM / TRẠM BƠM / NHÀ BƠM thuộc cụm mặt bằng chính (trong nhà, tầng hầm): vẫn rải, chỉ báo (`phong_bom_trong_nha`); nhà bơm tách rời (cụm nhỏ, kể cả sát bể nước) vẫn bỏ.
- Hiên (L6, chỉ `LoaiTran MaiKeo`): dải hẹp rộng TB ≤ 3 m, ≤ 200 m² có ≥ 2 cột không thuộc phòng khác, VÀ mép trong (cạnh sát phòng thật)
  là tường nét đôi (nét song song 0,08–0,45 m phủ ≥ 50 %), mép ngoài không phải tường liền (phủ ≤ 80 %; Quyết Thắng viền mái 2 nét phủ 34–50 %) -> bỏ (`bo_hien=`).
  Bản vẽ tường 1 nét, hành lang có cột trong tường ngoài: chỉ báo (`hien_nghi=`), vẫn rải (review 21/11). Hiên không vẽ cột: người dùng tự vẽ vùng.
- NGƯỜI DÙNG 23/11 (trường học khung 6, bản chép `hoc_bao_chay.dwg`: "hành lang bên làm gì rải đầu báo"): HÀNH LANG BÊN không đặt đầu báo
  (QCVN 10:2025 1.5.11, nguyên văn `knowledge/qcvn10-2025-chung.md`), với MỌI loại trần, không chỉ `MaiKeo` như L6 Hiên ở trên. Dấu hiệu: dải rộng ≤ 3 m,
  dài ≥ 4 lần rộng; một cạnh dài là tường có cửa phòng, cạnh dài kia là hàng cột + lan can / tường lửng / nét mảnh mở ra SÂN TRONG hoặc ra ngoài nhà
  (bên kia không có phòng). Có chữ "HÀNH LANG", "HÀNH LANG THOÁT NẠN" mà mép ngoài hở thì vẫn bỏ. Phần nở rộng của chính hành lang đó (ô 4,1 m trước
  lối xuống sân) cũng bỏ. Dải 1,65 m ngoài cửa sổ phòng (lan can mép ngoài, lam che nắng ngoài nữa) là hành lang ngoài / lô gia: bỏ.
  Ca gốc: hành lang 2,3 m bao 3 phía sân chào cờ + dải 1,65 m ngoài dãy văn phòng; plugin rải 45 đầu báo ở đó, người dùng bắt bỏ hết. Hành lang
  trong nhà (hai bên đều là phòng) vẫn rải.
- Cùng ca 23/11: buồng thang hở dính hành lang bên + phòng nhỏ dưới gầm thang (P. THIẾT BỊ, KHO 10,57 m²) thành MỘT vùng 35 m² nên luật thang ≥ 50 %
  không bắt. Đúng: tách phòng nhỏ (có chữ tên, cửa riêng) thành vùng riêng và rải; phần thang + chiếu nghỉ + hành lang bên thì bỏ.
  WC có chữ VỆ SINH / WC > 60 m² (79–80 m²) cũng bỏ: chữ thắng ngưỡng diện tích. Các mặt con của khối WC không có chữ (khu rửa tay, khu tiểu
  8–17 m²) kề mặt có chữ VỆ SINH, không có cửa phòng khác: bỏ theo.
- L5b (MaiKeo): phòng ≤ 100 m² không có chữ tên nào (≥ 3 chữ cái: KHO 1, BẢO VỆ giữ lại, review 21/11), sát (≤ 0,3 m) khoang đã bỏ vì mái che -> bỏ theo (`mai_keo bo_ke_mai_che=`).
- Ô không cột, không vì kèo, không nhãn phòng (sân trống): HỎI người dùng, không tự vẽ vùng.
- Mọi khu bỏ đều in ở dòng lệnh; cần rải thì bấm điểm trong phòng đó.

## Hộp kỹ thuật
Điểm lên dây, ống đứng: trước mắt lấy hộp gen thông tầng trên mặt bằng; sau này người dùng vẽ tay vị trí cho plugin nhận (16/11).
