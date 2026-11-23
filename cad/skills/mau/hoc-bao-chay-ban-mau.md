# Học rải báo cháy theo bản mẫu đã thẩm định (file_cad_test/TESTBCHAY_20_11.dwg)

Người dùng 20/11: mỗi khung rectang trong bản vẽ là một công trình mẫu đã thẩm định, là CHUẨN. Việc: chép block mặt bằng, explode, vẽ vùng
kẹp sát mặt tường (không bao phần cột lồi, không bao nội thất), rải đầu báo / chuông / nút, đối chiếu mẫu, rút logic, sửa plugin; xong mới tới dây
(điện trở cuối đặt xa tủ TTBC). Tiến độ: `cad/TIEN_DO.md` mục VIỆC 20/11 (2).

## Bản vẽ (toạ độ gốc, khung layer 0, đáy y = -17533)
| Khung | x | Nội dung | Block mặt bằng |
|---|---|---|---|
| 1 | 28063..49595 | "CÁC BLOCK MẪU CẦN ĐƯA VÀO BLOCK MẪU" (chuông, nút, đèn EM): không phải công trình | — |
| 2 | 53820..164835 | Xưởng Quyết Thắng (đã học 20/11, `phan-vung-xuong-mai-keo.md`) | `mb_hientrang` |
| 3 | 173108..243353 | Mặt bằng căn hộ 441 (2 tầng), đầu báo khói | `T1`, `T2` |
| 4 | 252521..381242 | 2 tầng, có bảng ký hiệu (khói trên trần, nhiệt dưới trần, đèn chỉ thị) | `232`, `tang 2` |
| 5 | 386638..449075 | Karaoke ABC 46,45 x 17,6 m, 1 tầng, đầu báo NHIỆT, 3 zone | `A$C52273371` (handle 9426) |
| 6 | 461967..747884 | Trường học Tân, tầng 1 + 2 (lớn) | `A$Cc52e29cf`, `A$C4bb18544` |
| 7 | 764067..889565 | Nhà ở xã hội, 2 tầng | `MB TẦNG 1`, `A$Cbe9093c8` |
| 8 | 899276..1002776 | Karaoke 991: tầng hầm + tầng 1, TTBC 10 zone | `A$C1b64d7a5`, `A$C473f3ffb` |

Ký hiệu trong bản vẽ (tên block, không có mã MA_VT): nhiệt "H" = `A$C04a8c6c3`, `A$Cfb28fa55`; khói "S" = `A$C1f830c1d`, `ĐẦU BÁO CHÁY KHÓI`, `SMOKE`;
chuông = block chứa `A$C3C813C48` (`Chuông báo cháy`, `A$Cdca0159f`, `A$C373f2228`, `A$Cdebcf022`); nút = `Nút nhấn khẩn`, `15NUT NHAN KHAN20`,
tổ hợp `FDS`; đèn báo phòng = `Đèn báo phòng`; TTBC = `A$C81605030`; điện trở cuối khung 5 = `A$Ccacc4903` (3 cái = 3 zone), nơi khác `DT`, `DIEN TRO CUOI NGUON`.

## Cách làm (đã chạy được 20/11)
- Làm trên bản chép `ket_qua/HOC_BAO_CHAY/cad/hoc_bao_chay.dwg` (không commit). AutoCAD mở từ Claude: plugin KHÔNG tự nạp vì junction
  `%APPDATA%\Autodesk\ApplicationPlugins\TKPCCC.bundle` báo lỗi 0x80070289 (mount point) -> NETLOAD thẳng `D:/1_Claude/TKPCCC/cad/TKPCCC.bundle/Contents/TKPCCC.Cad.dll`
  qua COM (`SendCommand "(command \"_.NETLOAD\" \"...\") "`). Mở AutoCAD từ shell có sandbox thì chạy hẳn ngoài sandbox.
- Chép + explode 1 cấp: `ket_qua/HOC_BAO_CHAY/cad/chep_no.ps1 -H <handle> -DY -50000` (COM Copy, Move, Explode, xoá bản chép block). Thiết bị, dây của mẫu
  nằm NGOÀI block mặt bằng nên bản chép sạch; thiết bị mẫu so được bằng cách dời -50 m.
- `PCCC_RAI` qua COM: ngăn các câu trả lời bằng `\r` (dấu cách bị nuốt vì ô mã thiết bị nhận cả dấu cách). Thứ tự với BC_NHIET:
  mã, Enter (trần), Khong, C, góc 1, góc 2, Enter (hết chọn), Enter (nét mẫu), Enter (mọi phòng).
- Thử mã mới không cần tắt AutoCAD: build ra thư mục tạm (csc như `build.bat`, chép data từ `TKPCCC.bundle\Contents`), chạy accoreconsole trên bản lưu, DXFOUT, so bằng Python.
  21/11: dùng luôn thư mục tạm của `chay_thu.ps1` (có DLL + data), kịch bản `NETLOAD <dll>`, `_.ZOOM W 386000,-49000 450000,-25000`,
  `PCCC_RAI BC_NHIET "" Khong C 388000,-47500 448000,-27000 "" "" ""`, `_.QUIT _Y`; log in "Vùng tự dựng m2=" từng vùng. Xoá vùng PCCC_* cũ trước
  (PCCC_RAI dùng thẳng polyline PCCC_VUNG có sẵn trong khung chọn, không dựng lại). Chạy PCCC_RAI qua COM thì ngăn trả lời bằng `\r` như trên.
- Sau mỗi lần build bundle: tắt AutoCAD (COM `Quit`), `build.bat`, mở lại, NETLOAD qua COM; lệnh MCP đầu tiên hay quá giờ, gửi NETLOAD lại khi AutoCAD rảnh.

## Khung 5 Karaoke ABC
LỖI 1 (ĐÃ SỬA 20/11, `NhanVung.cs` MatChon, test `chay_thu.ps1` ca 15): bản vẽ có layer `tuong` nhưng layer đó chỉ chứa block cửa, tường vẽ trên layer `0`.
Lượt "chỉ layer tường" dựng 178 mặt vụn (lớn nhất 3,3 m²) và MatChon dừng ở đó -> "Enter mọi phòng" ra 0 vùng (bấm điểm thì đúng vì VungTaiDiem lùi về mọi nét).
Sửa: lượt chỉ-tường không ra phòng thật nào (PhongThat rỗng) thì bỏ, dựng lại bằng mọi nét liền. Sau sửa: 30 vùng, 50 đầu báo.

Đối chiếu (mẫu 62 đầu báo nhiệt trong mặt bằng; plugin 50): 21 / 30 vùng khớp số. Lệch (mẫu > plugin), vị trí mẫu tính từ góc trái dưới vùng (m):
| Vùng (m) | Mẫu | Plugin | Vị trí mẫu |
|---|---|---|---|
| 6,0 x 2,9 (3 phòng) | 2 | 1 | (0,79; 0,74) (4,79; 0,74) |
| 4,0 x 6,1 | 2 | 1 | (1,79; 1,79) (1,79; 5,39) |
| 3,35 x 6,1 | 2 | 1 | (1,47; 1,79) (1,47; 5,39) |
| hành lang 10,25 x 1,7 | 3 | 2 | x 0,29; 4,79; 8,79 |
| sảnh trái 4,0 x 14,4 | 4 | 2 | y 1,29; 4,69; 8,69; 12,69 |
| 4,2 x 7,9 (chữ L) | 4 | 2 | lưới 2 x 2 (x 0,54; 3,24 / y 1,29; 5,79) |
| 7,24 x 7,9 (chữ L) | 5 | 3 | |
| lối 1,6 m giữa 2 phòng phía nam | 1 | 0 | plugin không dựng vùng (hở) |
Nhận xét: mẫu giữ bước giữa hai đầu báo nhiệt ≈ 4–4,5 m, cách tường ≤ ≈ 2,5 m, phòng 6 x 2,9 m vẫn 2 đầu. Plugin theo TCVN 7568-14:2025
5.9.2.1.2 / 5.9.2.1.4 (bước 7,2 m, cách tường 3,6 m, `data/khoang_cach_rai.json`). Mẫu khớp kiểu bảng đầu báo nhiệt cũ (≈ 5 m / 2,5 m / 25 m²) nhưng
NGƯỜI DÙNG CHỐT 20/11: GIỮ TCVN 7568-14:2025, không rải theo mật độ 5738; bản mẫu chỉ để học CÁCH rải (vị trí, hàng, lề) và CÁCH chia vùng.
Vậy khi đối chiếu: lệch SỐ LƯỢNG do mẫu dày hơn 7568 thì không sửa; chỉ sửa khi vùng sai, sót vùng, hoặc 7568 đòi nhiều hơn plugin rải.
Khác: hành lang bị cắt thành 5 đoạn vùng; mẫu rải đều cả hành lang như một vùng.
LỖI 2 (ĐÃ SỬA 21/11, `NhanVung.cs` `CacMat` khe xuyên + `GomCua` elip, test `chay_thu.ps1` E12, E13; luật ở `cad/skills/nhan_dang/cua_thang_khu.md`):
nguyên nhân KHÔNG phải nét `xref` mà là khe ngang hành lang giữa mặt tường ngăn hai bên thẳng hàng (trục 3, 5), má cửa phòng thẳng hàng tường ngăn
(x 420972, 428570; cánh cửa vẽ bằng ELIP nên trước đó không nhận là cửa). Sau sửa (PCCC_RAI Enter mọi phòng, 26 vùng, 50 đầu báo):
hành lang tây 401672..432970 một vùng 53,2 m² (mẫu cũng một chuỗi đầu báo cách 4,5 m suốt đoạn này, dim mẫu chạy liền tới vách cửa 2 cánh x 433070);
hai lối 1,5–1,6 m phía nam có vùng (10,71 / 12,96 m², 1 / 2 đầu). Vách ngang x 432970–433170 có cửa đi = ranh vùng đúng (mẫu cũng tách).
LỖI 3 (ĐÃ SỬA 21/11, người dùng khoanh đỏ 2 chỗ thiếu góc; `ThuNet` cờ `TrongCua`, test E14): nét trong block cửa (khuôn, cánh) làm tường:
lối 1,5 m trục 3 mất dải 1 m cuối (khuôn cửa phòng đông thẳng hàng cánh cửa phòng tây, khe ≤ 3 m khép ngang lối); hành lang đông mất tiền sảnh 1 m
(cánh cửa vách ngang mở ra hành lang + hộp cửa nới 0,3 m chạm khe x 434170). Sau sửa: lối trục 3 12,15 m² (đủ 1,5 x 8,1), hành lang đông 19,13 m²
(433170..444420, như mẫu), mặt dựng 543 -> 234, 26 vùng, 51 đầu báo. Chuông: mẫu có chuông ở từng phòng hát
(22 block chuông, 20 đèn báo phòng ngoài cửa), nút ấn 3 cái ở hành lang, 3 zone, mỗi zone 1 điện trở cuối. Chưa đối chiếu dây.

### Chuông, nút, đèn khung 5 (21/11, đối chiếu 26 vùng plugin; toạ độ bản chép y −50 m)
Đếm: nhiệt `A$Cfb28fa55` 63, chuông `A$Cdca0159f` 22, đèn `Đèn báo phòng` 20, nút `A$C9835e11b` 3, điện trở cuối `A$Ccacc4903` 3, TTBC `A$C81605030` 1.
- Chuông 19 / 22 nằm trong phòng, mỗi phòng một: 10 phòng hát bắc 3,4–4,0 x 7,6 m (y −36135: sát tường hành lang, cạnh cửa), phòng nam 6,0 x 3,9 và
  7,6 x 3,8 (x 401935 / 411435 / 421035: cách tường trái ≈ 0,26 m), phòng lớn 4,2 / 3,9 / 7,2 x 7,9 m. 3 chuông còn lại đi cặp nút (cách nút ≈ 0,4 m).
- Nút + chuông: sảnh trái (398644, −36788) cạnh lối vào; giữa hành lang (420971, −36808); lối ra đông (444474, −37178, ngoài vùng 0,8 m = ô cửa ra).
- Đèn báo phòng: hành lang y −36655 (sát tường bắc, ngoài cửa phòng bắc) và y −37984 (sát tường nam); lối đi 1,5–1,6 m (x 411083, 420683) ngoài cửa
  phòng nam; sảnh trái 2 đèn (x 401583) ngoài cửa 2 phòng tây nam; 1 đèn trong phòng 7,2 x 7,9 (cửa phòng con bên trong).
- Phòng không chuông, không đèn: WC, kho 2,5 x 2,8 (có điện trở cuối), phòng TTBC 4,0 x 3,0, lối đi.
- Điện trở cuối: (402407, −44885) phòng 6,0 x 3,9 tây nam; (426005, −29407) phòng hát bắc trục 4–5 sát tường xa hành lang; (437960, −45380) kho 2,5 x 2,8.

## Khung 3 Căn hộ 441, tầng 1 (21/11; bản chép y −50 m, T1 x 176200..207200, y −53700..−45600)
Mặt bằng: block `T1` (handle 50D0, 48 đối tượng: kiến trúc là block xref lồng `A$Ca48bff2d` 509 nét, phải nổ thêm), tầng lửng block `T2` (50D1).
Tường 2 nét layer `xref_FRASER_ARC_TUONG$0$NA_01.TUONG`, cột = KHE HỞ tường giữa 2 nắp đầu tường (tô hatch, không có polyline cột), cửa sổ block `S23_plan`.
Thiết bị mẫu cả khung (2 tầng): khói `ĐẦU BÁO CHÁY KHÓI` 19 (tầng 1: 10), `Đèn báo phòng` 9, điện trở cuối `A$Ccacc4903` 2, `A$C8b7f0307` 1; KHÔNG có chuông, nút ấn.
LỖI 4 (ĐÃ SỬA, `GomCua`, test E15): block xref gom mọi cửa của tầng (layer `...NA_09. CUA`) có hộp bao cả mặt bằng (176244..203244 x −53733..−45733) bị coi
là một cửa -> mọi khe 3–12 m "có cửa trong khe": khép 8,2 m ngang P.103 từ đầu cửa sổ, 7,8 m dọc trục 3... ra 8 vùng vụn (P.103, P.104 chỉ còn một mẩu
vì `PhongThat` bỏ mặt chứa mặt khác). Sau sửa: 12 vùng, 13 đầu báo; mẫu 10.
Đối chiếu tầng 1 (plugin / mẫu): hành lang trái 2 / 2; P.103 1 / 1; phòng 1,7 x 3,5 m 1 / 1; P.101 1 / 1; phòng phải P.102 1 / 1; P.102 + bếp mở 1 / 1; khu thang phải 1 / 1.
Lệch (chưa sửa):
- P.104 bị chẻ 2 vùng (2 / 1): khe 2,8 m giữa nắp tường mặt ngoài và nắp trụ 105 mm phía hành lang ở cột trục 3 (x 182644) thẳng hàng, ≤ 3 m nên luôn khép.
  Thử luật "khe đi vuông góc ra khỏi nắp đầu tường" (nắp ≤ 0,45 m cùng phương khe + mặt tường dài vuông góc): phía hành lang chỉ có trụ 105 mm nên không bắt;
  nhìn cục bộ thì nắp đầu tường và trụ nhỏ giữa hai cửa đối xứng nhau, chưa có dấu hiệu phân biệt chắc. Thừa 1 đầu báo, không thiếu bảo vệ.
- Phòng tắm (bồn cầu, bồn tắm là block xref tên vô nghĩa) không nhận là WC: rải 1 (mẫu 0).
- Ô X thông tầng phía phải (x ≈ 203200..207200, y ≈ −52900..−50300) dính dải bồn cây thành một mặt, `TachOX` không tách: rải 1 (mẫu 0).
- Hành lang giữa (sảnh thang máy + đoạn trước cửa P.101, P.102, 9 m²) bị bỏ như sảnh thang (`bo_sanh_thang`); mẫu CÓ 1 đầu báo ở đoạn trước cửa P.101,
  cách giếng thang ≈ 2,2 m. Luật người dùng 16–17/11 "sảnh thang máy không đặt đầu báo" chưa phân biệt sảnh thang thuần với hành lang chung có cửa phòng: HỎI người dùng.
Đèn báo phòng tầng 1: ngoài cửa từng phòng ở (P.101–P.104, P.102), trên hành lang sát tường có cửa — khớp quan sát khung 5.

### Khung 3 tầng lửng (T2, bản chép x 211600..242600; 21/11)
Plugin 11 vùng / 11 đầu báo; mẫu 9 (P.M03 1, hành lang trái 1, P.M02 1, phòng 1,7 m 1, SẢNH THANG MÁY 1, P.M01 1, P.101 1, khu thang + thông tầng phải 2),
đèn báo phòng 4, chữ "Về tủ TTBC Zone 3". THIẾU: `bo_sanh_thang=1 m2=21.1` bỏ cả sảnh trước giếng thang + P.M01 (phòng có tên, sát giếng); mẫu đặt 1 đầu báo
ngay sảnh thang (cách cửa thang ≈ 1,5 m) và 1 trong P.M01. Thừa: các ô X thông tầng (`TachOX` không nhận) được rải.
=> Hai tầng khung 3 đều mâu thuẫn luật "sảnh thang máy không đặt đầu báo" (người dùng 16–17/11, KOCHAM): cần người dùng chốt (bỏ luật, hay chỉ bỏ sảnh
thuần không có cửa phòng / không có chữ tên phòng).

## Khung 4 Karaoke 123 (21/11, XONG)
Block `232` (890E, 5602 nét, tầng 1) và `tang 2` (8931) chèn ở toạ độ xa, hình rơi vào khung; chép + nổ 1 cấp. Tường 2 nét layer `01 - TUONG`,
hatch gạch chéo tường ĐÃ NỔ thành nét 45° dài ≈ 0,28 m cách 90 mm (layer `02 - TUONG HOAN THIEN`, 3409 nét tầng 1), cột layer `04 - COT` (cot=60 nhận được).
Bản chép: tầng 1 ở y −50 m đã bị xoá layer hatch nổ (phiên trước); bản chép tầng 1 NGUYÊN (còn nét hatch nổ) ở y −100 m (x 279000..324000, y −112500..−91500);
tầng 2 ở y −50 m (x 334000..379000, y −62500..−41500; hatch tầng 2 là hatch thật, `gach_cheo=73`).
- LỖI 5 (đã sửa, E17): nét hatch nổ làm tường, mẩu cùng đường gạch ở hai tường kế nhau bị khép ngang góc phòng ⇒ vùng cạnh chéo 4–6 m, mat=6001.
  Ca tổng hợp cũ (2 dải) không tái hiện vì cần ĐỦ 4 dải tường (đường gạch đi từ tường trái sang tường trên qua góc). `BoHatchNo`: mat 6001 → 512.
- LỖI 6 (đã sửa, E18): ô giao hành lang trên với lối dọc trái (1,8 x 1,8 m tầng 1; 2,0 x 1,8 m tầng 2) bị `PhongThat` bỏ im lặng (2A/P < 1); mẫu có 1 đầu báo.
- LỖI 7 (đã sửa, E19): tầng 2 đoạn hành lang 6,3 m trên buồng thang hở: mép sàn 2 nét layer `06 - THAY 1` không phải layer tường ⇒ hành lang thông
  xuống thang + ô thông tầng ra ngoài nhà, không thành phòng; mẫu có đầu báo + đèn. `LanCan` lấy 2 nét song song nối 2 cột.
- Kết quả (BC_KHOI để so vùng): tầng 1 15 vùng / 20 đầu báo, tầng 2 17 vùng / 20. Mọi đầu báo nhiệt + đèn báo phòng của mẫu nằm trong vùng plugin;
  ngoài vùng chỉ còn `A$Ced08533f` (layer `0.10-140`, 1 cái ngoài cửa mỗi phòng, gắn trên mặt tường hành lang: nghi đèn chỉ thị đầu báo trên trần, chưa rõ).
- Mật độ mẫu: phòng karaoke 20–30 m² đặt 4 đầu báo NHIỆT (`A$C04a8c6c3`, 112 cái 2 tầng), hành lang 1 dãy; dày hơn TCVN 7568-14 (người dùng chốt giữ TCVN).
  Mỗi phòng 1 chuông (`A$C373f2228`), 1 đèn báo phòng ngoài cửa (`Đèn báo phòng`), 11 phòng / tầng.
- Đầu phun dùng ké (1 lần gọi `rai SPR_DAU_DOWN l PCCC_VUNG hop ... s 4000 dt 12 kt:true`, s/dt chỉ để thử cơ chế): tầng 1 69 đầu / 15 vùng 320 ms,
  tầng 2 63 đầu / 17 vùng 314 ms, dim theo Cài đặt.
- Đối chiếu bằng accoreconsole (không khoá AutoCAD): LISP xuất block mẫu + polyline `PCCC_VUNG` ra txt, Python khớp điểm trong vùng
  (`ket_qua/HOC_BAO_CHAY/cad/xuat.ps1 -Dwg <x.ra.dwg> -Ra <txt>` + `xuat.lsp`; `khop.py` sửa hộp khung / độ dời y trong file). `go_khung.ps1 -Diem 'x,y;x,y'` bấm điểm trong phòng để hỏi vì sao thiếu vùng.

## Khung 6 Trường học (21/11, XONG phần vùng; còn câu hỏi phạm vi)
Khung y −17533..129887 (cao hơn khung khác). Block `A$Cc52e29cf` (EFC3, 438 đối tượng) + `A$C4bb18544` (EFDC, 2150) chèn y −4351, hộp bao kéo tới
x 1,42 triệu, y 109 m (nội dung xa ngoài khung): chép −50 m sẽ ĐÈ vùng khung 7–8 (đã làm nhầm, khôi phục `.bak`, bản sai lưu `hoc_bao_chay.k6sai.dwg`).
Bản chép đúng ở y −200 m; chọn rải x 461967..747884, y −218000..−90000 (2 tầng cạnh nhau, khung trục x 490741..719595).
- LỖI 8 (E21): chữ "PHÒNG BỘ MÔN" bỏ dấu, bỏ cách = PHONGBOMON chứa PHONGBOM ⇒ 3 phòng học 79–90 m² bị bỏ như phòng bơm.
- LỖI 9: nhà thi đấu 618 m² + 7 phòng cạnh nó cách dãy học một sân > 2 m ⇒ `CumChinh` coi là cụm hình chi tiết (< 20 % cả tầng 2) và bỏ. Nay phòng có tâm
  trong khung trục không xét cụm. (Chưa có ca thử riêng; gặp lại thì dựng ca 2 cụm trong 1 lưới trục.)
- LỖI 10: lan can hành lang thoáng dọc thư viện vẽ polyline hở (layer `N_THAY 1` của xref): `LanCan` chỉ nhận LINE ⇒ thêm polyline hở.
- Kết quả: 110 vùng; cả 120 đầu báo nhiệt + 8 điện trở cuối (`A$Ccacc4903`, 8 zone) của mẫu nằm trong vùng plugin; 35 đèn báo phòng đều trên tường cạnh cửa.
  Mẫu có 20 `ZONE MONITOR` (layer `PC - THIET BI BAO CHAY`).
- NGƯỜI DÙNG TRẢ LỜI 21/11: NGOÀI PHẠM VI thiết kế (không sửa mã: plugin rải đúng phần người dùng quét chọn; đối chiếu khung 6 chỉ tính y < 60 m).
  Mẫu KHÔNG đặt thiết bị nào ở phần y 60–102 m (dãy phòng học loại 2 phía trên, 2 khối WC học sinh 79 m², sảnh...), cả 2 tầng;
  plugin vẫn rải ở đó (~40 vùng). Không thấy chữ phạm vi / hiện hữu; tên xref "mat bang cai tao tret - mai" (cải tạo).
- WC học sinh 79 m² có chữ "VỆ SINH" nhưng > 60 m² nên không bị `bo_khu_khong_bao` bỏ (luật ≤ 60 m²).
- NGƯỜI DÙNG 23/11 soát bản chép (x 495–590 m, y −205..−95 m): bỏ hành lang bên quanh sân chào cờ, dải 1,65 m ngoài dãy văn phòng, 4 buồng thang,
  6 mặt WC (27 vùng, 47 đầu báo; spec `ket_qua/HOC_BAO_CHAY/cad/skills/bo_hl_ben_thang_wc.json`); P. thiết bị, Kho dưới gầm thang tách vùng riêng,
  1 đầu báo mỗi phòng. Luật: `nhan_dang/cua_thang_khu.md` mục "Khu không đặt đầu báo" (23/11). Plugin CHƯA sửa (xem `cad/TIEN_DO.md`).
- Đầu phun 1 lần gọi: 891 đầu / 110 vùng, 1,5 s (kết quả spec chỉ liệt kê 40 vùng đầu, `DOC_TOI_DA`; đếm bằng `dem`).
- `xuat.ps1` nay nhận `XUAT_X0/X1/Y0/Y1` (biến môi trường), `khop.py file,x0,x1,dy,y0,y1`.
