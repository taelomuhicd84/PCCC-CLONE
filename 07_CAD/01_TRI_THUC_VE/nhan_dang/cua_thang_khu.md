# Nhận dạng cửa, thang, khu không rải

Mã: `CacMat` (khép khe cửa), `GomCua`, `GomKhu`, `HinhBac`, `TachOX`, `GomChuKhu`, `LoaiKhuTen` trong `07_CAD/07_KIEN_TRUC/cad/NhanVung.cs`; các bộ lọc "Enter mọi phòng" ở `07_CAD/09_PCCC/01_RAI_THIET_BI/cad/Rai.cs`.

## Cửa (khép ô cửa để phòng kín)
- Khe giữa hai đoạn tường thẳng hàng ≤ `HO_CUA_M` (3 m): luôn khép (chỉ khe gần nhất ở mỗi đầu đoạn).
- Khe 3 m tới `HO_CUA_MAX_M` (12 m): chỉ khép khi có ký hiệu cửa NẰM TRONG khe (hộp cửa phủ ≥ 25 % chiều dài khe), hoặc tường 2 nét hở đúng chỗ đó (khe ≤ 6 m).
  Người dùng 14/11: "cứ có cửa mình khép lại 1 vùng". Khe 11 m có cung nắp bồn cầu chạm một đầu thì không khép (KOCHAM 17/11).
- Khe chữ T có cửa (cửa giữa đầu tường và tường vuông góc): đầu tường cách hộp cửa ≤ 0,5 m, chiếu tới nét gần nhất trong 12 m, tia cắt hộp cửa thì khép.
  Plugin ĐÃ LÀM 28/11 (TESTVUNGBCHAY lửng, WC sau khu rửa P.M03 bị nuốt): đầu tường 2 nét có NẮP (polyline kín) cũng là đầu tường: điểm đầu chỉ chạm một
  nét nắp vuông góc 50–600 mm, đầu kia của nắp nối mặt tường bên kia đi lùi vào thân (không đi tiếp qua nắp: bậc đổi bề dày tường không phải đầu tường).
  Cầu từ đầu có nắp chỉ tới 3 m (trường học khung 6: nắp bậc tường cho cầu 5,5–8,5 m dọc lớp học). `CacMat` bước 1b `chiNap`, test E40.
- NẮP ĐẦU TƯỜNG không làm "tường thẳng hàng" (người dùng 28/11, lửng trục 4/A–B: nắp tường ngoài trục A và cạnh 0,66 m đầu tường ngang thẳng hàng cách
  2,9 m bị khép, phòng trống chẻ đôi). Plugin ĐÃ LÀM 28/11 (`CacMat` `napDau`): khe ≥ 0,6 m mà CẢ HAI đầu là nắp (đoạn ≤ 1 m, hai đầu nối nét vuông góc đi
  cùng phía, thân dài ≥ 1,5 lần nắp và > 1,2 m) thì không khép. Một đầu là mặt tường / mặt cột vẫn khép (cột 200 x 500 không phải thân tường: trường học
  khung 6 cầu mặt trong hai cột qua dải cửa sổ giữ nguyên). Test E41.
  Cũng không khép khe ≥ 0,6 m có một đầu là nắp, đầu kia nằm GIỮA một mặt tường vuông góc khe (mặt chạy tiếp ≥ 50 mm cả hai phía): khe xuyên lòng phòng
  (TESTVUNGBCHAY tầng 1 P.104: cột trục 3 vẽ trên layer trục, tường hở chỗ cột, nắp tường ngoài thẳng hàng mép hộp tường chồng trong tường hành lang
  cách 2,8 m; `giuaMat`, test E48).
- Khe XUYÊN không gian liền thì KHÔNG khép (Karaoke ABC 21/11, `dauXuyen` trong `CacMat`): tường ngăn hai bên hành lang thẳng hàng (mặt tường cùng
  toạ độ) làm khe ngang hành lang 1,7 m trông như ô cửa, hành lang vỡ 5 vùng; mẫu đã thẩm định rải đều cả hành lang. Khe 0,7 m tới max(3 m, 6 m)
  mà ở CẢ HAI đầu có nét vuông góc chạy liền hai phía (mặt hành lang chạy tiếp qua đầu tường, chữ T) trong 0,6 m trước hoặc sau đầu khe
  (bề dày tường; đầu khe ở mặt trụ lồi) thì bỏ; phía là ô cửa (hộp ký hiệu cửa nới 0,3 m) cũng tính là chạy tiếp (má cửa phòng thẳng hàng tường ngăn).
  Có ký hiệu cửa NẰM TRONG khe thì vẫn khép (cửa ngang hành lang chia vùng). Miệng lối đi, sảnh (góc chữ L), má cửa thường (nắp đầu tường một phía) vẫn khép.
  Trên bản vẽ nhà máy thật bỏ thêm ~900 khe ≤ 6 m: đều là cầu ảo vắt qua WC, mái che, lưới ngoài nhà (đã xem ảnh 21/11).
  Nét chữ T phải chạy ≥ 0,15 m MỖI phía đầu khe (23/11, Quyết Thắng): vạch đầu tường 0,2 m (±0,1 m) ở hai đầu ô cửa 5,6 m tường 1 nét xưởng 2 bị tính
  là chữ T, khe không khép, xưởng 2 không thành mặt, cả cụm phía đông bị `bo_cum` bỏ: 25 vùng / 57 ĐB còn 12 / 20 (lỗi từ 1bd93ea, 21/11).
  Ngưỡng 0,25–0,5 m thì Karaoke 123, trường học vỡ vùng lại (mặt hành lang sau trụ lồi ngắn); 0,15 m giữ nguyên số vùng các khung 3–6.
  Kiểm: `chay_thu.ps1` phép "Ban chep Quyet Thang" (chạy khi máy có `12_KET_QUA/QUYET_THANG/cad/thu_maikeo/qt_maikeo.dwg`).
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
- NGƯỜI DÙNG 23/11 (Karaoke 123 tầng 2, ảnh `12_KET_QUA/HOC_BAO_CHAY/anh/k4t2_nguoidung_23_11.png`; THAY dòng trên cho sảnh / lỗ thông tầng, giếng
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
- NGƯỜI DÙNG 25/11 (Karaoke 123, quét tầng 1 + tầng 2 một lần; "tại trục (A-B)/4 tầng 1 chưa bao đúng vùng rải"): các tầng trên có dấu X lỗ thông tầng
  mà tầng dưới KHÔNG có dấu X ở đó (tầng dưới là sàn đáy sảnh) thì VÙNG RẢI của tầng dưới ở chỗ đó NHƯ CÁC TẦNG TRÊN: tách riêng đúng đường bao vùng
  ô X tầng trên (đầu báo / đầu phun lắp ở đỉnh sảnh); phần còn lại của phòng tầng dưới là vùng riêng, lắp ở trần tầng đó. Tầng dưới không có tường ngăn
  sảnh với hành lang nên tự dựng phòng ra một mặt chung (Karaoke tầng 1: hành lang B–C + khoảng trái thang + sảnh = 122,22 m²); chỉ nhìn các tầng
  trên mới biết ranh. Chỉ tầng NGAY DƯỚI (tầng trên gần nhất khớp được không có X ở đó thì thôi; tầng dưới cũng có X = giữa sảnh nhiều tầng, không
  chiếu). Phần còn lại nằm dưới lỗ thang tầng trên (≤ 15 m², < 1/2 diện tích có phòng tầng trên phủ) hoặc chứa hộp thang ≥ 1/3 mảnh = vế thang cạnh
  sảnh: bỏ, như tầng trên không có vùng ở lỗ thang.
  Plugin ĐÃ LÀM 25/11 (`ChieuThongTang`, in `mat_bang_tang=1,2` và `chieu_thong_tang=N tang a duoi b m2=cũ->sảnh+còn lại bo_ve_thang=..`):
  tầng nhận theo TÊN MẶT BẰNG "MẶT BẰNG / MB ... TẦNG n" (chữ, MText hoặc thuộc tính block khung tên, vd `NAMECHITIET` DONG2; `GomTenTang`, `SoTang`:
  TẦNG 2-5 = 2, HẦM 1 = -1, TRỆT = 0,9, LỬNG = 1,5, LẦU 1 = 1,9, MÁI / TUM = 1000); cụm phòng (hộp bao cách ≤ 2 m) nhận tên gần nhất ≤ 15 m;
  dời giữa hai tầng = dời được nhiều cặp phòng cùng diện tích + cùng cỡ hộp nhất (≥ 3 phòng, ≥ 25 %); tách phòng bằng Region AutoCAD SAU `LamGon`
  (phòng chưa làm gọn tự chạm qua khe cửa 100 mm kho gầm thang, Region không dựng). Karaoke tầng 1: 122,22 → 61,77 (= ô X tầng 2) + 49,88, bỏ 10,6.
  Người dùng sửa tay 25/11 "hành lang phải bao kẹp sát tường": mảnh sau khi tách KHÔNG làm gọn lại (LamGon gộp bậc 0,2 m mặt tường thang ra cạnh cắt
  sảnh, hành lang lấn vào tường thang 1,7 m: 50,22 m²); nay giữ bậc, 49,88 m² đúng như vùng người dùng sửa. Hành lang chữ L này rải theo mảnh
  (`07_CAD/01_TRI_THUC_VE/ve/bao_chay/rai_dau_bao.md`, gạch 25/11).
  Cũng 25/11: quét nhiều tầng (≥ 2 tên tầng) thì khung trục chỉ áp cho cụm phòng có phòng trong khung (trước: trục nhận ở tầng 1, bỏ hết 16 phòng tầng 2
  `bo_ngoai_truc`). Test E31 + phép kiểm bản chép Karaoke tầng 1 + 2 trong `chay_thu.ps1`.
  shortcut: không có tên mặt bằng ghi số tầng, mặt bằng đặt xoay / lật, hoặc quét từng tầng riêng thì không chiếu: vẽ polyline `PCCC_VUNG` theo ô X tầng trên.
  CÒN: thang tầng 1 Karaoke (nét rời, vế cong) chưa nhận là thang bộ (không `khoet_thang`); mảnh vế thang chỉ bỏ nhờ luật "dưới lỗ tầng trên".
- NGƯỜI DÙNG 28/11 (`07_CAD/04_KIEM_THU/file_cad_test/TESTVUNGBCHAY.dwg`, tầng 1 + tầng lửng; THAY phần "lắp ở đâu" của gạch 23/11 và 25/11 trên):
  plugin TỰ đọc các tầng người dùng quét (tên mặt bằng), "quét tới đâu tính tới đó", không hỏi. Ô X cùng vị trí ở các tầng liền nhau = một cột lỗ thông tầng;
  gọi k là tầng CAO NHẤT có X của cột lỗ.
  (a) Trên k còn tầng đã quét KHÔNG có X ở đó, hoặc k là tầng cao nhất đã quét → lỗ DỪNG ở k: ô X tầng k là vùng riêng, rải đầu báo ở đó.
  (b) X lên tới tầng trên cùng của nhà (MÁI / TUM) → thông toàn bộ: không vùng, không đầu báo ở mọi tầng của cột lỗ.
  Các tầng có X thấp hơn k: không vùng. Tầng sàn lỗ (ngay dưới cột lỗ, không có X): chỉ lắp ở tầng k, nên phần nằm dưới ô X được tách khỏi phòng, không vùng
  (cách hiểu của Claude, người dùng: "tại vị trí tầng cao nhất có dấu X đó thì rải vùng báo cháy rồi rải đầu báo").
  Ô X nhỏ (cỡ giếng) mà tầng ngay dưới cùng vị trí là phòng có cửa, không có X là LỖ THÔNG TẦNG, không phải giếng / hộp kỹ thuật (TESTVUNGBCHAY lửng
  trục 3–4/A–B: ô X 2,3 x 1,1 m dưới khối WC; tầng 1 ở đó là phòng nhỏ có cửa). Giếng thang máy, hộp kỹ thuật (X ở mọi tầng, block / cabin thang máy) vẫn không rải.
  Ô X không bao giờ gộp với phòng kề (lửng: ô thông tầng có thang trục 6–7/B–C dính vào P.101 qua vách 1 nét).
  Plugin ĐÃ LÀM 28/11: (1) dấu X vẽ NÉT KHUẤT (line / polyline 2 đỉnh kiểu nét đứt, mọi layer) nhận bằng `OXKhuat` (hai nét xiên dài lệch ≤ 10 %, cắt nhau
  gần trung điểm ≤ 5 %, bốn đầu ở bốn góc hộp; không đòi đầu chạm tường: ô trục 6–7 vẽ lệch 100 mm). Cỡ giếng vào `OXNho` (không rải, KHÔNG làm giếng
  thang máy cho luật sảnh thang: phòng sát hộp kỹ thuật không bị bỏ), lớn vào thông tầng; X nằm trong ram dốc là của ram. In `o_x_net_khuat=`.
  X vẽ LỆCH (lửng trục 6–7/A–B: nét 6 m và 4,8 m cắt nhau ở 40 % / 58 %, một đầu giữa cạnh) nhận theo MẶT (`OXMat`): mặt kín nhỏ nhất ≥ 1 m² chứa
  giao điểm, bốn đầu nét cách viền ≤ 0,3 m, mỗi nét ≥ 1/2 đường chéo hộp mặt; ô = chính mặt đó (lửng: thông tầng 21,8 m², test E47). X vẽ trùng
  trên hai xref chỉ nhận một lần.
  (2) `KhoetOX` sau `LamGon`: hộp ô X (và hộp ram) khoét khỏi phòng kề khi phần chung ≥ 0,2 m² và ≥ 1/4 ô (`tach_o_x=` cũ->mới): P.101 16,02 → 9,02,
  hộp kỹ thuật trục 4 khoét khỏi phòng trống. Phòng rộng hơn 1,3 lần ô mà có điểm trong nằm trong ô không bị `MatKhongRai` bỏ cả phòng. Test E42.
  (3) Quét nhiều tầng (`PhanTangX`, in `o_x_nhieu_tang= vung lo_nho duoi_lo mai_ram gieng ghep_tang`): cột lỗ theo dời tầng `DoiTang`; k là tầng cao nhất
  có X: tầng dưới k không vùng; k = MÁI / TUM hoặc cột lên tới ram: không vùng; ô lớn ở k: vùng; ô cỡ giếng ở k: X ở mọi tầng = giếng, tầng ngay dưới
  đáy cột là phòng có cửa (mặt kín ≥ 1 m² chứa tâm, có hộp cửa trong / cách ≤ 0,5 m) = lỗ thông tầng nhỏ → vùng (mặt kín quanh tâm ô). Tầng sàn lỗ:
  `ChieuThongTang` tách phần dưới ô và BỎ (thay 25/11), in `chieu_thong_tang=... m2=cũ->còn lại bo_duoi_lo=`. Dải ≤ 0,2 m giữa mép ô và mép phòng dưới
  (tường hai tầng lệch 0,1 m) chỉ trừ thêm khi mảnh còn lại bị `PhongThat` bỏ (`DaiKhe`). `DoiTang`: dời lấy TRUNG VỊ (một cặp lệch 50 mm kéo cả tầng
  5 mm), thiếu 3 cặp phòng khớp thì nhận cặp đưa hộp tầng phủ ≥ 80 % hộp tầng kia (TESTVUNGBCHAY: chỉ P.M02 trùng). TESTVUNGBCHAY 2 tầng: ô lửng trục 6–7
  vùng 7 m² (sau khoét thang), ô lửng trục 3–4 vùng 2,48 m², tầng 1 dưới ô trục 6–7: 17,83 → 8,3 (bỏ 9,52). Karaoke 123 T1+T2: 122,22 → 49,88
  (bỏ 61,77 dưới ô X, vế thang 10,6), 30 vùng (trước 31). Tầng 1 dưới ô X lệch trục 6–7/A–B: phòng 10,79 m² bỏ, hành lang 12,54 → 7,64.
  Test E31 (đổi kỳ vọng), E44, bản chép TESTVUNGBCHAY + Karaoke trong `chay_thu.ps1`.
  shortcut: tầng nhận theo tên mặt bằng; không ghép được tầng (một tầng, thiếu tên) thì ô cỡ giếng không vùng, ô lớn rải như vùng thường (như cũ).
- Sảnh thang máy (người dùng 16–17/11): phòng ≤ 30 m² sát giếng thang (cách ≤ 0,6 m) không đặt đầu báo (`bo_sanh_thang=`).
  NGƯỜI DÙNG 21/11 (căn hộ 441, mẫu có đầu báo ở sảnh trước thang máy): chỉ bỏ khi sảnh nằm trong BUỒNG ĐỆM (≤ 2 ký hiệu cửa có tâm trong phòng hoặc
  cách biên ≤ 1 m) hoặc trong BUỒNG THANG (phòng chứa tâm hộp thang bộ). Sảnh tiếp tân, thông tầng, hành lang chung nhiều cửa phòng: RẢI theo mẫu (test E16).
  NGƯỜI DÙNG 28/11 (phong_vung.md 28/11 (4)): sảnh trước THANG BỘ ở đầu hành lang là vùng riêng (không bỏ như sảnh thang máy, không coi là WC riêng).
  Người dùng nhắc lại 21/11: "sảnh thang máy thì không, sảnh tiếp tân hoặc sảnh thì có". Chữ trong phòng thắng phép đoán theo hình: có chữ SẢNH (không kèm
  THANG), TIẾP TÂN, LỄ TÂN thì luôn rải (`Sanh` trong `GomKhu`, test E20); chữ "SẢNH THANG MÁY" chứa THANG MÁY nên bỏ theo `bo_khu_khong_bao`.

## Khu không đặt đầu báo
- Lõi thang máy, sảnh thang máy, buồng thang bộ, WC (người dùng 16/11): phòng ≤ 60 m² chứa dấu hiệu (tên block, layer, chữ phòng; block `BON_CAU`...) bị bỏ (`bo_khu_khong_bao=`).
- Chữ khu (L5, 19/11): "MÁI CHE", "KHÔNG CÓ CÔNG NĂNG", "SÂN", "HIỆN HỮU" (cả khi không gạch chéo), "PHÒNG BƠM" (KHÔNG khớp "PHÒNG BỘ MÔN": bỏ dấu, bỏ cách cũng ra PHONGBOM…, trường học 21/11, test E21): phòng ≤ 100 m², hoặc ≤ 500 m² hình dải
  (rộng TB 2A/P ≤ 6 m, review 21/11) chứa tâm chữ bị bỏ (`bo_khu_ten=`); phòng khác chỉ báo (`chu_khu_trong_phong_lon`): người dùng vẽ `PCCC_VUNG` tách ra.
  MaiKeo: khoang chứa chữ chỉ bỏ khi là khoang đầu / cuối theo phương chia và < 0,9 x diện tích khoang trung vị (`mai_keo bo_khoang_khu_ten=`);
  chữ ở khoang giữa (mái che dọc cạnh dài không tường ngăn) chỉ báo (`mai_keo chu_khu_khoang_giua=`).
- RAM DỐC (người dùng 28/11, "ghi nhớ luôn"): không vùng, không đầu báo, không gộp vào vùng kề. Hình nhận: dải nét xiên ngắn song song xếp thành hàng
  mũi tên (chevron) lặp đều, thường là hatch / block ẩn danh màu xanh trên layer HATCH, kèm nét tâm dọc và dấu X nét khuất. TESTVUNGBCHAY lửng trục 7–8/A–B:
  block `*U126` layer `xref_FRASER_ARC_TL$0$NA_08. HATCH` hộp 76935–81847 x 55145–58345; X trên layer `...04. NET KHUAT`, nét tâm `...03. NET MANH` y 56746.
  Chữ RAM / DỐC trong vùng cũng là ram dốc.
  Plugin ĐÃ LÀM 28/11 (`RamDoc`, `KhoetOX`, in `bo_ram_doc=N m2=..`): ≥ 6 chữ V (hai nét xiên cùng dài 0,3–3 m chung đỉnh, đầu kia tự do: không chạm nét
  CÙNG cỡ) cùng hướng mũi, đỉnh thẳng hàng theo hướng mũi, cách đều 0,1–0,6 m → hộp ram; vùng chung ≥ 1/2 với hộp ram: bỏ cả vùng, ít hơn: khoét hộp ram
  (lửng: ram 13,36 m² bỏ, phòng trục 6–7/A–B 22 → 19,68 vì chevron lấn qua trục 7). Chữ: nhãn ≤ 12 ký tự bắt đầu RAM / DỐC (vùng ≤ 150 m²). Test E43.
  shortcut: hatch mẫu chevron không nổ chưa nhận; ram dốc chỉ áp cho báo cháy (như các khu không đặt đầu báo khác).
- Phòng bơm PCCC đứng riêng, không thuộc hạng mục nào: KHÔNG lắp báo cháy. Nằm trong hạng mục có báo cháy thì xét theo hạng mục đó.
  Plugin (review 21/11): phòng có chữ PHÒNG BƠM / TRẠM BƠM / NHÀ BƠM thuộc cụm mặt bằng chính (trong nhà, tầng hầm): vẫn rải, chỉ báo (`phong_bom_trong_nha`); nhà bơm tách rời (cụm nhỏ, kể cả sát bể nước) vẫn bỏ.
- Hiên (L6, chỉ `LoaiTran MaiKeo`): dải hẹp rộng TB ≤ 3 m, ≤ 200 m² có ≥ 2 cột không thuộc phòng khác, VÀ mép trong (cạnh sát phòng thật)
  là tường nét đôi (nét song song 0,08–0,45 m phủ ≥ 50 %), mép ngoài không phải tường liền (phủ ≤ 80 %; Quyết Thắng viền mái 2 nét phủ 34–50 %) -> bỏ (`bo_hien=`).
  Bản vẽ tường 1 nét, hành lang có cột trong tường ngoài: chỉ báo (`hien_nghi=`), vẫn rải (review 21/11). Hiên không vẽ cột: người dùng tự vẽ vùng.
- NGƯỜI DÙNG 23/11 (trường học khung 6, bản chép `hoc_bao_chay.dwg`: "hành lang bên làm gì rải đầu báo"): HÀNH LANG BÊN không đặt đầu báo
  (QCVN 10:2025 1.5.11, nguyên văn `01_TRI_THUC/01_CHUNG_THUAT_NGU/qcvn10-2025-chung.md`), với MỌI loại trần, không chỉ `MaiKeo` như L6 Hiên ở trên. Dấu hiệu: dải rộng ≤ 3 m,
  dài ≥ 4 lần rộng; một cạnh dài là tường có cửa phòng, cạnh dài kia là hàng cột + lan can / tường lửng / nét mảnh mở ra SÂN TRONG hoặc ra ngoài nhà
  (bên kia không có phòng). Có chữ "HÀNH LANG", "HÀNH LANG THOÁT NẠN" mà mép ngoài hở thì vẫn bỏ. Phần nở rộng của chính hành lang đó (ô 4,1 m trước
  lối xuống sân) cũng bỏ. Dải 1,65 m ngoài cửa sổ phòng (lan can mép ngoài, lam che nắng ngoài nữa) là hành lang ngoài / lô gia: bỏ.
  Ca gốc: hành lang 2,3 m bao 3 phía sân chào cờ + dải 1,65 m ngoài dãy văn phòng; plugin rải 45 đầu báo ở đó, người dùng bắt bỏ hết. Hành lang
  trong nhà (hai bên đều là phòng) vẫn rải.
  PLUGIN ĐÃ LÀM 23/11 (`Rai.cs` `HanhLangBen`, in `bo_hanh_lang_ben=`, test E28, E29): dải rộng 0,8–3 m theo phương cạnh dài nhất; điểm thử dời ra
  ngoài mép 0,4 m không rơi vào / sát (≤ 0,25 m) phòng nào trên ≥ 60 % một mép dọc, mép kia có phòng ≥ 40 %; không chữ "PHÒNG". Nhận khi mép ngoài
  có lan can (`LanCan` phủ ≥ 50 %), hoặc cùng chuỗi (cùng đường mép ngoài lệch ≤ 0,15 m, hở ≤ 6 m) với dải đã nhận, hoặc chuỗi ≥ 3 dải dài ≥ 12 m
  mà ≥ 2/3 dải không có cửa. Bản chép trường học: nhận đủ 15 dải (5 đoạn hành lang + 10 ô dải 1,65 m).
- Cùng ca 23/11: buồng thang hở dính hành lang bên + phòng nhỏ dưới gầm thang (P. THIẾT BỊ, KHO 10,57 m²) thành MỘT vùng 35 m² nên luật thang ≥ 50 %
  không bắt. Đúng: tách phòng nhỏ (có chữ tên, cửa riêng) thành vùng riêng và rải; phần thang + chiếu nghỉ + hành lang bên thì bỏ.
  WC có chữ VỆ SINH / WC > 60 m² (79–80 m²) cũng bỏ: chữ thắng ngưỡng diện tích. Các mặt con của khối WC không có chữ (khu rửa tay, khu tiểu
  8–17 m²) kề mặt có chữ VỆ SINH, không có cửa phòng khác: bỏ theo.
  PLUGIN ĐÃ LÀM 23/11: chữ WC / VỆ SINH / TOILET / nhãn ngắn bắt đầu "VS" ("VS-TĐ HS NỮ", "VS GV NAM") bỏ phòng tới 150 m² (test E29);
  mặt ≤ 20 m², dài ≤ 3 lần rộng, không chữ tên, kề phòng WC đã bỏ -> `bo_mat_con_wc=` (xét sau hành lang bên). Thang vẽ NÉT RỜI (≥ 6 bậc
  0,6–2,5 m cách đều 0,2–0,4 m, `ThangRoi`) nay là thang bộ như block (test E30). CHƯA LÀM: buồng thang hở gộp hành lang + phòng gầm thang
  (bản vẽ chỉ vẽ vế dưới, vế trên trên mặt cắt không vẽ): chỉ khoét được vế 3 m², phần chiếu nghỉ + P. THIẾT BỊ / KHO vẫn một vùng.
- L5b (MaiKeo): phòng ≤ 100 m² không có chữ tên nào (≥ 3 chữ cái: KHO 1, BẢO VỆ giữ lại, review 21/11), sát (≤ 0,3 m) khoang đã bỏ vì mái che -> bỏ theo (`mai_keo bo_ke_mai_che=`).
- Ô không cột, không vì kèo, không nhãn phòng (sân trống): HỎI người dùng, không tự vẽ vùng.
- Mọi khu bỏ đều in ở dòng lệnh; cần rải thì bấm điểm trong phòng đó.

## Hộp kỹ thuật
Điểm lên dây, ống đứng: trước mắt lấy hộp gen thông tầng trên mặt bằng; sau này người dùng vẽ tay vị trí cho plugin nhận (16/11).
