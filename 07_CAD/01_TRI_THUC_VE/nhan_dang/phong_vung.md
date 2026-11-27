# Nhận dạng phòng, dựng vùng rải

Mã: `MatChon`, `CacMat`, `PhongThat`, `CumChinh`, `VungTaiDiem`, `LamGon` (`07_CAD/07_KIEN_TRUC/cad/NhanVung.cs`); `PCCC_RAI` (`07_CAD/09_PCCC/01_RAI_THIET_BI/cad/Rai.cs`); thao tác spec `vung_tu_dong` (`07_CAD/01_TRI_THUC_VE/TU_VUNG.md`).
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
- NGƯỜI DÙNG 26/11 (`07_CAD/04_KIEM_THU/file_cad_test/TESTBCHAY_20_11.dwg`, vùng tay ở `12_KET_QUA/TESTBCHAY_20_11/cad/skills/b4a_vung.json`): CỬA chia vùng.
  (1) Sảnh vào (trục 1–2) mở thông ra hành lang vẫn là vùng RIÊNG, ranh theo mặt tường hành lang kéo dài (miệng chữ L), không gộp với hành lang.
  (2) Hành lang có cửa đi ngang (tường + ô cửa cắt ngang hành lang, trục 7) thì tách hai vùng ở tường đó.
  (3) Ô nhỏ thông với phòng qua Ô CỬA CÓ CÁNH (tường hai bên, cánh phủ kín ô) KHÔNG gộp vào phòng ("không, phải tách riêng biệt").
  (4) Cùng ngày, nói rõ (3): "vùng wc nhỏ trong phòng này, không được bao phòng này; các phòng có wc nhỏ cũng tương tự" = ô nhỏ trong phòng đó là WC riêng
  của phòng: KHÔNG VÙNG, KHÔNG ĐẦU BÁO; vùng phòng chủ chạy theo tường ngoài của WC (chữ L, khấc), không bao WC. Áp cho mọi phòng có WC nhỏ: phòng
  ≤ 5 m², không chữ tên, có ký hiệu cửa, cửa chỉ mở sang phòng (không sang dải hành lang). Kho nhỏ có cửa ra hành lang vẫn rải.
  Ở TESTBCHAY: ô 2,5 x 1,1 m trong phòng 6–7, 3 ô 2,1 x 1,5 m góc trên phòng 7–9, 6 ô 1,4 x 1,85 m của 3 cột WC dãy dưới.
  (5) NGƯỜI DÙNG 26/11 chiều (sửa (4)): ô 2,5 x 2,75 m = 6,9 m² trong phòng 8–9 TESTBCHAY (trục 8, dãy dưới) "khá to, có thể là kho nên phải bao
  vùng": ô trong phòng > 5 m² là KHO, có vùng riêng (theo mặt tường của ô) và đầu báo; vùng phòng chủ vẫn chừa ô đó (khấc). Ngưỡng 8 → 5 m²
  (`Rai.cs`, test E36).
  (6) NGƯỜI DÙNG 26/11 chiều, chốt cách nhận WC: phòng là WC (không vùng, không đầu báo) khi MỘT trong ba: (a) RẤT NHỎ (≤ 5 m², cửa chỉ mở vào phòng,
  `bo_wc_trong_phong=`); (b) GHI CHỮ WC / VỆ SINH / TOILET (`bo_khu_khong_bao=`, tới 150 m²); (c) CÓ THIẾT BỊ WC bên trong (bồn cầu, lavabo, tiểu
  nam; block thiết bị → `bo_khu_khong_bao=`, nét nổ rời hình kín 0,25–0,9 m trong phòng ≤ 8 m² không chữ tên → `bo_wc_thiet_bi=`, test E36).
  Ô trống không chữ, không thiết bị, lớn hơn 5 m² là KHO: có vùng.
  (7) NGƯỜI DÙNG 26/11: nét vùng `PCCC_VUNG` mặc định màu HỒNG (ACI 230, `MAU_VUNG` ở `NhanVung.cs`; `vung_tu_dong`, `PCCC_RAI`, op `net` layer này). Chỉ áp
  khi tạo layer: layer đã có giữ màu; plugin đang chạy chưa build lại thì spec truyền `"mau":230`.
  (8) NGƯỜI DÙNG 26/11: nét vùng luôn nằm LỚP TRÊN CÙNG (thứ tự vẽ): `VungLenTren` (`NhanVung.cs`) gọi sau mỗi polyline vùng (`vung_tu_dong`, `PCCC_RAI`, `net`
  layer `PCCC_VUNG`), `DrawOrderTable.MoveToTop` + đặt layer `PCCC_VUNG` hồng kể cả khi layer có sẵn màu trắng (thay "chỉ áp khi tạo layer" ở (7)).
  Chỉ sảnh WC MỞ (không cánh, hoặc cánh chỉ phủ một phần chỗ thông) mới gộp vào phòng như mục 6 dưới.
  Plugin: (1), (2) đã đúng sẵn (`CacMat` khép miệng chữ L; khe có ký hiệu cửa thì khép); (3) `GopMatNho` (`o_nho_qua_cua=`, test E34);
  (4) lọc `bo_wc_trong_phong=` trong `PCCC_RAI` (`Rai.cs`; bên kia cửa = mặt gần tâm hộp cửa nhất; dải hành lang = rộng TB ≤ 2,5 m, dài ≥ 3 lần rộng;
  test E35). `CungCua` nhận thêm cánh cửa vẽ LWPOLYLINE hở 1–2 đoạn có một cung 60–120°.
  shortcut: kho nhỏ ≤ 5 m² không ghi chữ mà cửa mở vào phòng cũng bị coi là WC: cần rải thì bấm điểm trong phòng đó.

## Soát chỗ hay thiếu vùng sau khi dựng (người dùng khoanh đỏ 21/11, Karaoke 123 tầng 2; AI vẽ bằng chatbot làm y như plugin)
Dựng xong (`PCCC_RAI` Enter mọi phòng, hoặc `vung_tu_dong`), chụp ảnh và dò từng chỗ sau. Mọi khoảng sàn đi lại được trong phạm vi đều phải nằm trong một vùng,
trừ khu được bỏ có lý do (WC, lõi / sảnh thang máy, buồng thang, ô thông tầng).
1. Ô giao hành lang với lối dọc / ngách nhỏ ≤ 2 x 2 m ở đầu hành lang: dễ bị bỏ như dải tường. Thiếu thì `vung_tu_dong` `p` = điểm giữa ô.
2. Hành lang chạy ngang qua buồng thang hở / ô thông tầng: mép sàn, lan can 2 nét (layer nét mảnh, không phải layer tường) là CẠNH vùng; vùng hành lang
   chạy liền qua đó. Thiếu thì vẽ polyline `PCCC_VUNG` theo mặt tường bên kia + mép lan can bên này.
3. Góc phòng bị cắt chéo, vùng có cạnh xiên vô lý: nét hatch tường đã nổ (nét 45° ngắn dày đặc trong dải tường) bị lấy làm tường. Plugin đã bỏ;
   tự vẽ thì không bám các nét đó.
4. Phòng có chữ tên mà không có vùng: đọc dòng `bo_...` (chữ khu khớp nhầm, vd "PHÒNG BỘ MÔN" từng khớp "phòng bơm"); đúng là phòng thì bấm điểm trong phòng.
5. Cả cụm phòng mất (nhà thi đấu, khối tách bởi sân) trong lưới trục: không phải hình chi tiết, phải có vùng.
Mọi chỗ bổ sung tay ghi lại vào file `07_CAD/01_TRI_THUC_VE/mau/` của công trình đó để sửa plugin.
6. NGƯỜI DÙNG 21/11 tối (tự sửa vùng tầng 2 Karaoke 123, ảnh `images/3.png` của phiên): vùng phải KẸP BO theo tường / vách thật, không kẻ thẳng qua
   chỗ hở. Phòng có WC riêng: vùng phòng BAO LUÔN khoảng sảnh nhỏ trước cửa WC (khoảng hở giữa cửa phòng / cửa buồng xí), CHỈ CHỪA buồng xí (có bồn cầu);
   cạnh vùng đi vòng theo vách buồng xí (vùng chữ L, có khấc), không cắt thẳng ở mặt khối WC. Ô giao đầu hành lang (≤ 2 x 2 m) GỘP vào hành lang
   (không thành vùng riêng). Plugin ĐÃ LÀM 23/11 (`GopMatNho`, bước 2b dưới); lối dọc vuông góc hành lang vẫn là vùng riêng như người dùng vẽ.
7. NGƯỜI DÙNG 23/11 (đường đỏ trên ảnh `12_KET_QUA/HOC_BAO_CHAY/anh/k4t2_nguoidung_23_11.png`, phòng thứ 2 từ phải dãy trên): đó là đường bao CHUẨN cho
   mọi phòng công năng và ô thang hở — khép kín, nối liền, bám mặt tường / vách / mép thang, khấc vào ôm sảnh trước WC, chừa buồng xí; KHÔNG băng ngang
   vô lý. Hành lang tại trục (1''–1')/(E–D) (ô giao đầu hành lang trên) phải liền 1 vùng với hành lang, trừ khi có dầm chặn (`GopMatNho` đã làm; bản
   người dùng chụp còn chạy plugin cũ). Sảnh / lỗ thông tầng: vùng riêng (luật ở `cua_thang_khu.md` mục Thang, giếng, lỗ thông tầng; plugin làm 23/11 tối).
   Soát lại 23/11 tối trên bản chép T2: mọi phòng dãy trên, dãy giữa, dãy dưới đã bám tường, ôm sảnh WC, chừa buồng xí; không còn cạnh băng ngang.
8. NGƯỜI DÙNG 23/11 ("hơi lố, đường line đầu tiên của lan can hay tường chứ"; hành lang dưới Karaoke 123 T2 kéo tới nét NGOÀI lan can 60 mm):
   cạnh vùng dừng ở nét ĐẦU TIÊN tính từ phía vùng của lan can / tường / vách (mặt phía mình), không lấy nét ngoài, không lấy mặt cột bên cạnh. Bề dày
   lan can không thuộc vùng nào; vùng bên kia (sảnh thông tầng) dừng ở nét phía nó. Sửa tay 23/11: vùng hành lang x 336915..356655 (19,74 x 1,8 m),
   rải lại 2 ĐB 4945 | 9850 | 4945. Plugin làm 23/11 (`LanCanKin` + `GopMatNho` c, test E27): xem `tuong_cot.md` mục lan can.

## AI vẽ vùng qua spec phải KHỚP plugin (người dùng 27/11, TESTBCHAY T1 + T2: "plugin đã làm đúng, AI thì sai")
`vung_tu_dong` (= `VungTaiDiem`) chỉ lấy mặt kín nhỏ nhất quanh điểm, KHÔNG chạy bước 2b `GopMatNho` và các lọc ở mục dưới. Vì vậy AI phải tự làm cho
ra đúng kết quả "Enter mọi phòng" của `PCCC_RAI`. Đối chiếu plugin với AI trên TESTBCHAY (toạ độ mm), từng luật:
1. HÀNH LANG: mỗi đoạn thẳng là MỘT vùng chữ nhật riêng, KHÔNG gộp thành chữ U / L. Đoạn chính đi trọn từ tường mút này tới tường mút kia, kể cả ô giao
   ở đầu đoạn (T1 E–D: x 282094..313894, 57,2 m²). Đoạn nối dọc giữa hai hành lang là vùng riêng (T1 và T2 đều 1,8 x 5,4 m = 9,72 m²).
   AI sai: vẽ một vùng chữ U 97,9 m².
2. HÀNH LANG THÔNG RA SẢNH: ranh là mặt tường của phòng kéo dài ở phía XA nhất (T1: mặt ngoài tường phải phòng góc dãy dưới, x 301894). Khoảng hở
   giữa phòng dãy giữa và khối thang (2,5 m) thuộc vùng hành lang: vùng C–B có thêm nhánh lên tới mặt dưới hành lang E–D.
   AI sai: cắt hành lang ở mặt tường phòng dãy giữa (x 299194).
3. SẢNH: là vùng riêng. Cạnh trên theo đáy khối thang (T1 -50208). Chừa thang và thang xoắn (bậc tới -51558 ở x 306354..308194). AI sai: chừa cả bề rộng
   thang 2.
4. SẢNH WC CHUNG nằm giữa hai phòng (khối WC giữa phòng, cửa vào từ hành lang): CHIA ĐÔI theo vách giữa, mỗi nửa gộp vào phòng kề. Phòng thành chữ L
   (30,5 → 32 m²). Buồng xí vẫn chừa. AI sai: bỏ cả sảnh.
5. TIỀN PHÒNG WC kề phòng (ô 1,3 x 2,55 m, chỉ có buồng xí phía trên, T1 trục 6–7) gộp vào phòng kề (37,05 → 40,36 m²). AI sai: để vùng riêng 3,18 m².
6. KHOẢNG TRỐNG giữa phòng và lỗ thông tầng (T2: 2,7 x 5,4 m, x 354015..356715) có vùng riêng. AI sai: bỏ sót.
7. PHÒNG CÓ WC RIÊNG: vùng chữ L ôm sảnh trước WC, chừa buồng xí. AI đã đúng, trùng plugin tới mm.
Cách làm khi AI dựng vùng: `vung_tu_dong` một điểm trong mỗi mặt (phòng, từng ô sảnh nhỏ, từng đoạn hành lang). Rồi áp luật 1–6 để gộp hoặc cắt.
Gộp bằng `net` `PCCC_VUNG` `kin`, xoá mảnh theo handle. Xong thì đếm số vùng của từng tầng: T1 15 vùng, T2 16 vùng.
8. MÉP VÙNG KẸP SÁT TƯỜNG (người dùng 27/11, phòng T1 trục 2–3 hở 50 mm): đầu tường cụt và má cửa thẳng hàng mà mặt tường thật chạy song song ngay cạnh
   (≤ 150 mm, phủ kín khe) thì KHÔNG phải ô cửa: plugin bỏ cầu khép khe đó (`CacMat`, test E37), vùng ra 28,2 m² (trước 28,13). AI tự vẽ thì kiểm
   mọi cạnh vùng trùng mặt tường thật, không dừng ở đầu tường cụt.
Mép vùng theo đáy dầm, theo mép sảnh thông tầng: người dùng sẽ train thêm, có mặt cắt (cao trần, b x h dầm) và mặt bằng dầm để đối chiếu.
Khi đó xem `dam_khoang.md`.

## Plugin dựng phòng thế nào ("Enter mọi phòng" của `PCCC_RAI`)
1. Gom đoạn nét từ đối tượng quét chọn (`ThuNet`), tách cột, nhận trục, khép khe cửa, dựng mọi mặt kín (`CacMat`), chỉ giữ mặt có tâm trong khung chọn.
2. `PhongThat`: mặt ≥ 1 m², rộng TB (2A/P) ≥ 1 m, không chứa mặt khác bên trong. 2A/P chỉ bằng nửa cạnh ô vuông: ô ≥ 3 m² có diện tích / cạnh dài
   hộp bao ≥ 1,5 m cũng giữ (Karaoke 123 21/11: ô giao hành lang 2,0 x 1,8 m = 3,6 m², 2A/P = 0,95, và 1,8 x 1,8 m viền khấc cột, bị bỏ im lặng
   như dải tường; mẫu có đầu báo ở đó. Test E18). Mặt bị bỏ ở bước này KHÔNG in dòng lệnh; bấm điểm trong ô thì vẫn rải.
2b. `GopMatNho` (người dùng 21/11 tối, in `gop_sanh= gop_o_nho=`): hai mặt mà chỗ chung CHỈ là cầu khép khe (không nét thật nào song song cách ≤ 10 mm
   ở 1/4, 1/2, 3/4 cạnh chung) là cùng không gian. (a) Mặt bị `PhongThat` bỏ, 0,8–6 m², 2A/P ≥ 0,5 m, không chứa dấu hiệu WC (`GomKhu`) hay hình kín cỡ
   bồn cầu / lavabo (`GomVatDung`: polyline kín, elip, tròn, cạnh 0,25–0,9 m, không thuộc tường / cửa / cột) → gộp vào phòng gốc kề lớn nhất (một lượt:
   buồng xí chỉ kề sảnh không bị kéo theo). (b) Phòng ≤ 4 m² (ô giao) → phòng kề lớn nhất; hai dải hành lang (2A/P ≤ 2,5 m, cạnh dài ≥ 2,5 lần) CÙNG
   PHƯƠNG, cầu chung dài ≥ 0,8 bề rộng (cắt ngang trọn hành lang; cầu ô cửa 0,9 m không tính) → một. Test E18 (ô giao vào hành lang 18 m²), E22, E22B, E23.
   NGƯỜI DÙNG 25/11 (Karaoke 123 tầng 1, phòng (A-B)/3'-4: "chưa bao quanh tường WC trong phòng"): vùng phòng phải ÔM sảnh trước cửa WC, đường bao đi vòng
   theo mặt ngoài tường WC (phòng 25,65 → 27,3 m², như các phòng tầng 2). Lỗi: cánh cửa WC vẽ polyline kín 40 x 690 mm trên LAYER TƯỜNG, dựng như tường
   khoét vào sảnh 1,1 x 1,5 m, chu vi tăng 1,4 m, 2A/P còn 0,49 < 0,5 nên (a) không gộp. Sửa: bề rộng của (a) lấy thêm theo chu vi HỘP BAO (khấc, gai
   mỏng không tính). Test E33.
   NGƯỜI DÙNG 26/11: (a), (b) KHÔNG gộp khi cầu chung nằm ở ô cửa có cung cánh (hộp cung chạm cầu, phủ ≥ 80 % chiều dài cầu): ô đó thành vùng riêng
   (`o_nho_qua_cua=`). Test E34. Karaoke 123 T2: cánh 0,89 m đứng tự do trên chỗ thông 1,5 m (phía trên hở) = sảnh mở, vẫn gộp (59 %; bản chép giữ 16 vùng).
3. Lọc lần lượt, mỗi lọc in một dòng: ô X, gạch chéo (`bo_phong=`), ngoài trục (`bo_ngoai_truc=`), WC / thang (`bo_khu_khong_bao=`), sảnh thang (`bo_sanh_thang=`),
   chữ khu (`bo_khu_ten=`), hiên (`bo_hien=`), cụm phòng nhỏ tách xa mặt bằng chính (hình chi tiết, mặt cắt: `bo_cum=`).
   `bo_cum` không xét phòng có tâm trong khung trục (trường học khung 6, 21/11: 2 tầng chọn một lần, nhà thi đấu cách dãy học một sân > 2 m
   thành cụm < 20 % cả tầng 2, bị bỏ như hình chi tiết; mẫu có 15 đầu báo). Hình chi tiết ngoài trục đã lọc ở `bo_ngoai_truc`.
4. Mặt tự dựng được làm gọn (`LamGon`: bỏ bậc tường, học trần, góc cột) rồi vẽ lên `PCCC_VUNG`.
Bấm điểm trong phòng (`VungTaiDiem`): dò dần bán kính từ 10 m, lấy mặt kín nhỏ nhất chứa điểm; không áp các bộ lọc trên (bấm = cho phép).

## Xưởng
Khoang theo vì kèo / dầm: `dam_khoang.md`. Bài học xưởng Quyết Thắng (L1–L8, số liệu đối chiếu): `07_CAD/01_TRI_THUC_VE/mau/phan-vung-xuong-mai-keo.md`.
