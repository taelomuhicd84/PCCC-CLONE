# Rải đầu báo cháy (khói, nhiệt) trong vùng

Mã: `LuoiVung`, `DoiVao`, `PhuKin`, `KtLuoi` (`cad/module/plugin/Rai.cs`); thao tác `rai` (`cad/skills/TU_VUNG.md`), lệnh `PCCC_RAI`. Vùng: `cad/skills/nhan_dang/phong_vung.md`.

## Trị số
- Đầu báo khói, nhiệt (`BC_KHOI`, `BC_KHOI_TRAN`, `BC_NHIET`): theo TCVN 7568-14:2025, trị số + nguyên văn ở `data/khoang_cach_rai.json` gốc repo (một chỗ duy nhất).
  KHÔNG hỏi người dùng khoảng cách hay m², bỏ `s` để plugin tự lấy; có chiều cao trần thì truyền `h` (m). Khói kiểu điểm dùng tới trần 15 m (5.9.1.1.1; 9 m vẫn dùng khói).
- Người dùng chốt 20/11: GIỮ TCVN 7568-14:2025. Bản mẫu đã thẩm định chỉ tham khảo CÁCH rải (vị trí, hàng, lề) và cách chia vùng; mẫu dày hơn (kiểu TCVN 5738) thì không theo.
- Mã khác (sprinkler...): `s`, `dt` tra `cad/rule/LUAT.md` (BC-02, SP-02), ghi căn cứ khi báo.

## Cách rải trong vùng
- Lưới chia đều theo hộp vùng, cách tường ≤ S/2; điểm cách cạnh < `tuong_min` dời vào trong (kẹp theo hàng hoặc cột, khe hẹp thì đặt giữa khe) TRƯỚC khi phủ kín;
  phủ kín vùng lồi lõm, cạnh xiên bằng cách thêm thiết bị chỗ hở (`ho` > 0 thì nêu cho người dùng). `theo_hang` / câu hỏi Co/Khong: mỗi hàng chia lại theo bề rộng trống thật.
- Kết quả `rai` có `m2`, `ho`, `cheo`; `kt`:true vẽ chuỗi kích thước bước, cách tường như lệnh PCCC_RAI (theo Cài đặt, mọi mã kể cả đầu phun), `kt`:"cheo" thêm chéo góc như TCVN 7568-14 Hình 2. Kiểm phủ kiểu bản mẫu: vòng R7200 ở đầu báo hàng biên.
- Đầu phun dùng ké vùng báo cháy (người dùng 21/11): một lệnh `rai` `ma` SPR_..., `l` PCCC_VUNG + `hop`, `kt`:true. Phòng bị bỏ khi rải báo cháy
  (WC, thang, sảnh thang) KHÔNG cần vùng đầu phun (người dùng chốt 21/11): không dựng thêm vùng cho chúng.
- Xưởng mái kèo: mỗi khoang rải độc lập (khoang 20 x 6 m ra 2 đầu, 30 x 6 m ra 3 đầu, cách tường 5 m, cách vì kèo 3 m với h 9 m).

## Khoang dầm (trần hở)
- Loại trần, cách đọc dầm: `cad/skills/nhan_dang/dam_khoang.md`.
- Một LƯỚI CHUNG cho cả tầng, lề cố định tới dầm, số đầu mỗi khoang theo 7,2 m / 10,2 m (khói); hàng, cột thẳng hàng xuyên tầng. Mẫu nhà xưởng: khoang 10 x 10,6 m,
  2 x 2 đầu cách trục dầm 2 m (`cad/skills/mau/mau-rai-khoang-dam.md`). Không rải mỗi khoang một đầu ở tâm độc lập (người dùng chê 16/11).
- Khoang phải rải trong ô (≥ 4 m² với trần 2–4 m, ≥ 9 m² với trần ≥ 4 m): mép dầm chính coi như tường, dim từ mép dầm. Đầu báo lắp mặt dưới dầm (khoản c, e) đặt đúng giữa bề rộng dầm.

## Dim vị trí thiết bị (người dùng 16/11, mã `KtLuoi`)
- Ngang: MỘT chuỗi cho các thiết bị chung tập cột x (tường → TB → … → tường); hàng khác cùng các x đó không dim lại. Dọc: MỘT chuỗi trên một cột.
- Đường dim đặt gần hàng / cột thiết bị (ngay trên hàng đầu tiên, sát cột), không đẩy ra xa ngoài mép công trình.
- Chỉ thiết bị lệch lưới mới dim thêm: tới tường gần nhất và tới cột thiết bị gần nhất. Áp cả `kt_chuoi`, `PCCC_ADIM`.
- NGƯỜI DÙNG 23/11 (Karaoke 123 tầng 2, ảnh `ket_qua/HOC_BAO_CHAY/anh/k4t2_nguoidung_23_11.png`: dấu X xanh lá = bác bỏ, dấu ✓ = đúng):
  hành lang hẹp vừa đủ MỘT hàng đầu báo thì đặt hàng đó ĐÚNG GIỮA bề rộng hành lang; dim ngang (tường – đầu báo – tường, vd 900 | 900) CHỈ MỘT LẦN cho
  cả hành lang (✓ ở hành lang trên), không lặp ở từng đầu báo; dọc hành lang giữ một chuỗi. Không để số dim đè lên nhau; không đặt đường dim / số dim
  đè lên tường, cột, vật cản làm khó đọc (bác bỏ: chuỗi 900 dọc ở ô giao, các dim 900 ở hành lang dưới sát tường / cột, dim dọc cắt qua cột).
  Plugin ĐÃ LÀM 23/11 (`KtLuoi`): vùng một hàng ≥ 2 thiết bị, bề rộng ≤ 3 m (hành lang) thì chuỗi dọc hành lang lệch khỏi hàng 1/4 bề rộng (tối đa
  2 x cỡ chữ; hành lang 1,8 m lệch 0,45 m), không sát tường; tương tự hành lang dọc một cột. NGƯỜI DÙNG 23/11 chiều: đường dim hành lang KHÔNG nằm
  ngang bằng đầu báo / đầu phun, phải trên hoặc dưới một khoảng vừa phải (bản trước đặt chuỗi ngay trên hàng, lệch 0: bác bỏ). Chuỗi ngang (tường – đầu báo – tường) vẫn một lần, đặt ở cột thiết bị cùng tập
  hàng gần giữa vùng mà đường dim (lệch ±2 x cỡ chữ) không qua cột (tâm cột cách đường < 0,5 m; cột từ `TachCot`); chuỗi chính ngang cũng đổi phía
  khi phía trên cắt cột. Ô giao đã gộp vào hành lang (`GopMatNho`) nên hết chuỗi 900 riêng ở ô giao. Test E25.

## Còn tồn (đầu `cad/TIEN_DO.md` có phần CÒN)
- Mã chưa khai `tuong_min` (vd đầu phun): tạm 0,1 m (`CACH_TUONG_MIN_M`), KHÔNG phải trị số quy chuẩn; có số thật thì khai vào `data/khoang_cach_rai.json`.
- (Đã hết 23/11) Hành lang hẹp đặt sát 0,5 m tới một tường: Karaoke 123 T2 hai hành lang ra đúng giữa (970 | 970, 900 | 900), E25 kiểm hàng y = giữa.
  Hành lang lồi lõm (chữ L, ô giao lệch) chưa có ca thử riêng.
