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
Còn lệch: vùng đông plugin 434170..444420 (17,4 m², 2 đầu), mẫu 433070..444520 (3 đầu); tiền sảnh 1 m sau cửa vách ngang (cánh cửa mở vào) thành mặt
riêng nhỏ bị bỏ ở bước sau khép khe (chưa tìm bước nào cắt; bước khép khe đã bỏ khe x 434170). Không thiếu bảo vệ theo 7568 (đầu gần nhất cách vách 3,55 m). Chuông: mẫu có chuông ở từng phòng hát
(22 block chuông, 20 đèn báo phòng ngoài cửa), nút ấn 3 cái ở hành lang, 3 zone, mỗi zone 1 điện trở cuối. Chưa đối chiếu chuông, nút, dây.
