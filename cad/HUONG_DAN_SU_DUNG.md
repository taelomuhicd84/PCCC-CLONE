# HƯỚNG DẪN SỬ DỤNG PLUGIN TKPCCC CHO AUTOCAD

Plugin thiết kế PCCC cho AutoCAD 2021 trở lên: rải đầu báo cháy, đầu phun sprinkler theo tiêu chuẩn, đi dây, đi ống, dim, kiểm tra vùng mù, bóc khối lượng, chèn bảng ký hiệu và bảng thống kê. Mọi lệnh bắt đầu bằng `PCCC_`. Bảng lệnh đầy đủ nằm trong mảng `DS_LENH` (`cad/module/plugin/Lenh.cs`); gõ `PCCC_HELP` trong AutoCAD để xem bản mới nhất.

## 1. Cài đặt

1. Yêu cầu: AutoCAD 2021 (2021–2024 đều được), Windows.
2. Build plugin một lần: chạy `cad\build.bat`. Kết quả: `cad\TKPCCC.bundle\Contents\TKPCCC.Cad.dll` và `TKPCCC.UI.dll`.
3. Cài bằng Hub (khuyên dùng): mở `cad\bin\TKPCCC.Hub.exe`, bấm Cài ở "Plugin tự nạp" và ở từng AI muốn dùng MCP cad (Claude Code, Claude Desktop, Gemini CLI, Antigravity). Mỗi lần bấm đều hỏi lại; file cấu hình được sao lưu trước khi sửa; nhật ký ở `%APPDATA%\TKPCCC\hub.log`.
   Hoặc cách cũ: chạy `cad\cai_dat.bat`. File này tạo liên kết `TKPCCC.bundle` trong `%APPDATA%\Autodesk\ApplicationPlugins`. Gỡ: `cad\cai_dat.bat /go`.
4. Tắt hẳn AutoCAD rồi mở lại. Dòng lệnh báo `[TKPCCC] Đã nạp plugin PCCC`. Không tự nạp thì gõ `NETLOAD` và chọn `TKPCCC.Cad.dll`.
5. Bản quyền: gõ `PCCC_BANQUYEN`, nhập email, bấm Đăng ký (hoặc dán key) rồi Kích hoạt.
6. Sau mỗi lần build lại: tắt hẳn AutoCAD rồi mở lại để nạp bản mới.

## 2. Bắt đầu

- `PCCC`: mở palette chính. Thanh dọc trái chọn phân hệ: Quy trình vẽ, Tiện ích, Báo cháy, Chữa cháy, Thống kê, Thư viện, Lệnh, Bot chat, YouTube, Cài đặt.
- YouTube: xem video hướng dẫn ngay trong palette (Edge WebView2). Dán link hoặc gõ từ khoá rồi Enter; "Mở bằng trình duyệt" mở video đang xem ra ngoài; rời phân hệ thì video tự dừng. Lần sau mở lại đúng trang cuối.
- Bot chat: đầu khung chọn máy AI (Claude: đọc + vẽ CAD qua MCP; Gemini: chỉ đọc + trả lời) và model; "+ Thư mục dự án" gắn thẻ thư mục cho AI đọc hồ sơ (bấm thẻ để bỏ). Claude cần `claude` đăng nhập (`claude` rồi `/login`), Gemini cần Antigravity CLI `agy` đã đăng nhập.
- `PCCC_HELP`: bảng mọi lệnh. Gõ chữ vào ô tìm để lọc. Sửa ô Phím tắt rồi Enter là lưu (file `phim_tat.txt`, mỗi dòng `PHIMTAT=LENH`).
- Khi lệnh hỏi mã thiết bị: gõ `?` để mở bảng chọn, hoặc gõ một phần tên hay mã để tìm.
- Đơn vị bản vẽ: plugin đọc `INSUNITS`. Bản vẽ mm (khuyên dùng) thì nhập khoảng cách theo m khi lệnh hỏi m.
- Mọi lệnh có thể dừng bằng Esc; lệnh dài dừng giữa chừng thì bản vẽ không đổi.

## 3. Quy trình thiết kế trên mặt bằng kiến trúc có sẵn

Đây là quy trình đã chạy thật cho tầng 8 văn phòng KOCHAM (16/11/2026).

| Bước | Việc | Lệnh |
|---|---|---|
| 1 | Chép bản làm, không sửa file gốc. Xoá thiết bị PCCC cũ (theo layer hoặc theo block) | lệnh AutoCAD |
| 2 | Phân vùng: để plugin tự dò tường, hoặc vẽ tay polyline kín layer `PCCC_VUNG` cho phòng dò sai | `PCCC_RAI` (tự dò), `PLINE`, `PCCC_TACHVUNG`, `PCCC_KHUVUC` |
| 3 | Rải đầu báo cháy theo TCVN 7568-14:2025 | `PCCC_RAI` mã `BC_KHOI` / `BC_NHIET` |
| 4 | Kiểm vùng mù | `PCCC_KT` |
| 5 | Đặt nút ấn, chuông, tổ hợp, module | `PCCC_DAT` |
| 6 | Đi dây báo cháy (hệ thường hoặc địa chỉ) | `PCCC_DAYBC` (hoặc `PCCC_DAY`) |
| 7 | Rải đầu phun sprinkler (S, m² theo nhóm nguy cơ, TCVN 7336) | `PCCC_RAI` mã `SPR_DAU_UP` / `SPR_DAU_DOWN` |
| 8 | Vẽ ống chính, ống cấp | `PCCC_ONG` |
| 9 | Ống nhánh xương cá, cỡ ống tự động theo số đầu phun | `PCCC_TUYEN` (`TuDong`) |
| 10 | Đặt van, công tắc dòng chảy, van kiểm tra | `PCCC_DAT` |
| 11 | Dim bổ sung, dóng hàng dim | `PCCC_ADIM`, `PCCC_CDIM`, `PCCC_CANDIM` |
| 12 | Bảng ký hiệu, quy định lắp đặt, thống kê, thuyết minh | `PCCC_KYHIEU`, `PCCC_QUYDINH`, `PCCC_BOC` + `PCCC_BANG`, `PCCC_THUYETMINH` |

### 3.1. Phân vùng (bước 2)

- `PCCC_RAI` tự dựng phòng từ nét tường trong khung chọn (bỏ trục, nét khuất, cửa, nội thất, chữ, dim). Khe cửa nhỏ hơn 3 m được nối.
- Tường nằm trong block (vd block lõi thang đặt trên layer nội thất) vẫn được nhận nếu nét bên trong nằm trên layer tường (`TUONG`, `WALL`...).
- Phòng dò sai (gộp lõi thang, gộp buồng thang, phòng hở): vẽ tay polyline kín trên layer `PCCC_VUNG` rồi chạy lại. Có polyline `PCCC_VUNG` kín trong khung chọn thì `PCCC_RAI` dùng luôn các vùng đó, không dò tường và không hỏi điểm phòng.
- Vùng vẽ sẵn nằm trong một phòng tự dựng (diện tích không quá 1,3 lần phòng đó) thì thay cho phòng đó, không vẽ chồng.
- Phòng chữ L rải sprinkler: nên chia 2 vùng chữ nhật (`PCCC_TACHVUNG` hoặc vẽ 2 polyline). Chia thì đầu phun không bị thêm sát tường ở góc trong.
- Nét phân vùng màu (bản vẽ cả tờ là block, vùng kẻ nét xanh, đỏ): khi `PCCC_RAI` hỏi "Bấm một nét phân vùng mẫu", bấm một nét mỗi màu rồi Enter. Plugin rải theo mọi vùng cùng màu đó.

### 3.2. Rải đầu báo cháy (bước 3)

Dấu nhắc của `PCCC_RAI` với đầu báo khói, nhiệt:

1. Mã thiết bị: `BC_KHOI` (khói điểm), `BC_KHOI_TRAN`, `BC_NHIET`, `BC_BEAM` (tia chiếu: vẽ tia nối hai tường).
2. Chiều cao trần (m), Enter = không xét. Từ khoá: `HeSo` = mức tối ưu k 50–100 % (rải dày hơn tiêu chuẩn); `Dam` = trần có dầm sâu hơn 0,3 m (xét từng khoang dầm; `DaySan` = nhập chiều dày sàn mm, độ sâu dầm d = h dầm − dày sàn, dầm còn d ≤ 0,3 m thì bỏ; đọc cỡ `b x h` = rộng x cao trên mặt bằng dầm hoặc nhập cỡ; nhãn ghi ngược như `DS 400X250` vẫn đúng vì plugin lấy số lớn làm chiều cao; nhãn sai hẳn, vd dầm bẹt ghi `DS 1400X250` mà thật rộng 1400 cao 700: ở dấu nhắc "Bấm nét dầm màu khác" gõ `SuaDam`, bấm từng nét dầm đó rồi nhập cỡ thật `1400x700`, cỡ nhập tay thắng nhãn; cỡ gõ tay luôn đọc đúng thứ tự rộng x cao); `XemTruoc` = chỉ đếm, không vẽ.
3. Mỗi hàng chia lại theo bề rộng thật của hàng? Enter = Không (Co cho phòng chữ L có WC).
4. Quét chọn mặt bằng (tường các phòng hoặc các vùng `PCCC_VUNG`), Enter.
5. Bấm nét phân vùng mẫu, Enter = tự nhận từ tường.
6. Enter = mọi phòng, hoặc bấm điểm trong từng phòng rồi Enter. Enter = mọi phòng với đầu báo: plugin tự bỏ phòng ≤ 60 m² có block hoặc layer thang máy, thang bộ, thiết bị vệ sinh (`THANG MAY`, `LIFT`, `THANG BO`, `STAIR`, `TOILET`, `LAVABO`, `BON CAU`, `XI BET`...) hoặc chữ tên phòng `WC`, `VỆ SINH`, `THANG MÁY`, `THANG BỘ` (kể cả "SẢNH THANG MÁY": sảnh thang máy không đặt đầu báo), in `bo_khu_khong_bao= m2=`. Phòng nằm ngoài đường trục ngoài cùng (nhận trục từ block ký hiệu trục hoặc vòng tròn có chữ) không phân vùng, in `bo_ngoai_truc=`; nét liền trùng đường trục không coi là tường (`bo_net_truc=`); quét khung thì chỉ đọc nét trong khung. Bản vẽ không ghi chữ sảnh: phòng ≤ 30 m² sát giếng thang máy (block thang máy, ô có dấu X, cách ≤ 0,6 m) coi là sảnh thang, bỏ, in `bo_sanh_thang= m2=`. Phòng bị bỏ nhầm thì bấm điểm trong phòng đó.

Không cần nhập khoảng cách: bước tối đa, cách tường, chéo góc lấy từ `data/khoang_cach_rai.json` (chép nguyên văn TCVN 7568-14:2025; khói điểm: bước 10,2 m, cách tường 5,1 m, bán kính 7,2 m). Dòng lệnh in kết quả từng vùng: `n`, lưới, bước, m², số dim.

Sau khi rải, plugin tự dim theo trang Cài đặt (mặc định "chuỗi"): một chuỗi ngang dọc hàng đông nhất, một chuỗi đứng dọc cột đông nhất, đặt sát thiết bị. Hàng trùng toạ độ chỉ dim một lần, thiết bị lệch lưới mới dim thêm.

Trần hở có dầm (`Dam`), vùng tự dựng: vùng vẽ lên `PCCC_VUNG` là PHÒNG dựng từ tường (dầm không cắt phòng), dầm sâu chỉ chia khoang bên trong như dưới. Vùng `PCCC_VUNG` vẽ sẵn (vd vẽ một đường bao văn phòng): plugin tự chia vùng theo các dầm sâu hơn 0,3 m thành khoang (`chia_khoang=`), vẽ nét khoang và m² trên layer `PCCC_KHOANG_M2`, rải từng khoang, dim chung trên cả vùng (`kt_gom`). Nét dầm sơ đồ dừng cách dầm biên vài trăm mm vẫn khép khoang (plugin nối dài nét dầm 0,4 m). Bấm nét dầm mẫu đúng nét dầm, không bấm vào chữ nhãn (phóng to sát nét dầm trước khi bấm).

### 3.3. Đi dây báo cháy (bước 6)

`PCCC_DAYBC`: chọn `Thuong` (hệ thường) hoặc `DiaChi` (hệ địa chỉ, gồm module ZM, CM, R2ML). Quét chọn thiết bị của MỘT zone (đầu báo, nút ấn, chuông; có tủ thì chọn luôn), Enter, rồi bấm vị trí tủ trung tâm hoặc điểm dây đi về tủ. Plugin đi dây vuông góc, nối tiếp không rẽ chữ T, tự thêm điện trở cuối dây và dây nguồn chuông, layer theo trang Báo cháy. Có vùng `PCCC_VUNG` thì dây đi trong các vùng (qua tường ở khe giữa hai vùng kề nhau); đoạn nào cắt ra ngoài vùng (lõi thang máy, buồng thang) thì tự đi vòng (`ne_loi=` số đoạn đã đi vòng; đoạn đi vòng tránh bám mép vùng, cách tường khoảng 0,4–0,5 m). Kết quả in số tín hiệu, mét dây.

`PCCC_DAY` (đi tay hơn): mã dây, chọn thiết bị, bấm điểm đầu (tủ), điểm cuối (Enter = dừng ở thiết bị cuối, `Vong` = về điểm đầu), tên zone.

Mỗi zone một lần `PCCC_DAYBC`. Hệ thường: zone không quá 25 đầu báo (người dùng dặn) và không lấn sang khu vực khác.

### 3.4. Rải đầu phun sprinkler (bước 7)

`PCCC_RAI` với `SPR_DAU_UP` (hướng lên, trần hở), `SPR_DAU_DOWN` (hướng xuống, trần giả), `SPR_DAU_SIDE` (gắn tường):

1. Bước tối đa S (m) theo tiêu chuẩn áp dụng. TCVN 7336:2021 Bảng 1, nhóm 1 đến 4.1: 4 m. Từ khoá `Dam` (gõ `Dam` rồi mới nhập S): trần hở, dầm cao hơn 300 mm thì đầu phun đặt trong từng khoang dầm; chia khoang như mục 3.2 (bấm nét dầm mẫu sau bước chọn).
2. Diện tích bảo vệ tối đa một đầu (m²): nhóm 1–3: 12; nhóm 4–7: 9. Enter = không xét.
3. Khoảng cách tới tường (m), Enter = chia đều (tối đa S/2).
4. Chia lại theo hàng (Enter = Không).
5. Quét chọn, nét mẫu, phòng: như mục 3.2.

Plugin chọn số cột × số hàng ÍT đầu nhất mà ô lưới không vượt m² cho phép (vd phòng 16 × 12 m, S 4 m, 12 m² ra 4 × 4, không phải 4 × 3), bước làm tròn xuống 50 mm.

### 3.5. Ống sprinkler (bước 8, 9)

- `PCCC_ONG`: nhập mã ống (`ONG_DN65`...), bấm điểm đầu rồi các điểm tiếp theo, Enter = xong; plugin vẽ polyline trên layer `PCCC_ONG_DN65` (đặt luôn layer hiện hành) và in số đoạn, mét. Chạy được cả trong file kịch bản `.scr`. Ống, dây đếm khối lượng theo layer `PCCC_<mã>`.
- `PCCC_TUYEN`: mã ống nhánh, hoặc `TuDong` (rồi `Den05` = đầu phun áp đến 0,5 MPa, `Tren05`) để cỡ ống theo số đầu phun phía sau (`data/ong_nhanh_dn.json`: DN25 ≤ 2 đầu, DN32 ≤ 3, DN40 ≤ 5, DN50 ≤ 10, DN65 ≤ 20...). Chọn các đầu phun của MỘT phòng, chọn ống chính, bấm vị trí ống góp (Enter = cột đầu phun gần giữa phòng). Kết quả: ống góp vuông góc ống chính tới hàng xa nhất (cả hai phía), mỗi hàng một nhánh.
- Chọn dính đầu phun phòng khác: plugin nhóm đầu phun theo (cụm vùng `PCCC_VUNG` kề nhau, loại đầu phun) và chỉ nối nhóm đông nhất, in `tuyen_loc giu= bo=`. Đầu phun hướng xuống ở WC, hành lang lọt vào khung chọn văn phòng sẽ bị bỏ. Phòng bên cạnh cùng loại đầu phun và vùng sát nhau (qua một tường) vẫn bị gộp: khi đó chọn bằng `WP` theo đường bao phòng, chừa mép rộng hơn bán kính ký hiệu.
- `PCCC_DOIDN`: đổi cỡ một đoạn ống đã vẽ.

### 3.6. Thống kê và hồ sơ (bước 12)

- `PCCC_KYHIEU`: bảng ký hiệu chỉ gồm thiết bị, ống, dây có trên tờ. Chọn mặt bằng (Enter = cả bản vẽ), bấm góc trên trái, cao chữ.
- `PCCC_QUYDINH`: bảng quy định lắp đặt đầu báo theo TCVN 7568-14:2025.
- `PCCC_BOC`: bóc khối lượng (chọn vùng, Enter = cả bản vẽ; tách theo tầng, khu nếu muốn), gửi web báo giá. Rồi `PCCC_BANG`: chèn bảng thống kê (bấm điểm, cao chữ). Block động có Visibility tách dòng theo chủng loại.
- `PCCC_THUYETMINH`: chèn thuyết minh văn mẫu theo các hệ có trên bản vẽ.

## 4. Danh sách lệnh

### 4.1. Vẽ nền kiến trúc

| Lệnh | Việc | Cách dùng |
|---|---|---|
| `PCCC_TRUC` | Vẽ lưới trục | Nhập nhịp X (vd 6000,6000,4500), nhịp Y, bấm điểm gốc |
| `PCCC_TUONG` | Vẽ tường theo tim, vách ngăn | Nhập dày, bấm chuỗi điểm như PLINE; Vach = vách ngăn, Dong = khép kín |
| `PCCC_CUA` | Cửa đi trên tường đã vẽ | Bấm tâm cửa trên tường, nhập rộng, bấm phía cánh mở vào |
| `PCCC_THOATNAN` | Hatch đường thoát nạn | Chọn polyline kín các lối thoát nạn (layer `PCCC_THOAT_NAN`) |

### 4.2. Vùng, rải, đặt thiết bị

| Lệnh | Việc | Cách dùng |
|---|---|---|
| `PCCC_RAI` | Rải thiết bị theo lưới trong phòng | Mục 3.2, 3.4 |
| `PCCC_BOTRI` | Bố trí báo cháy từ palette (chọn thiết bị theo TCVN, trần, k) | Phân hệ Báo cháy; nút BỐ TRÍ gửi `PCCC_RAI` điền sẵn |
| `PCCC_DAT` | Đặt thiết bị theo mã vật tư | Nhập mã, bấm các điểm đặt, Enter = xong |
| `PCCC_TACHVUNG` | Tách một vùng `PCCC_VUNG` thành 2 | Chọn vùng, bấm 2 điểm đường tách |
| `PCCC_KHUVUC` | Đặt tên vùng, ghi m² | Bấm điểm trong vùng, gõ tên, Enter = xong |
| `PCCC_VUNGBV` | Vẽ vùng bảo vệ bán kính R | Nhập mã, R; báo thiết bị đứng lẻ |
| `PCCC_BLOCK` | Bảng block PCCC (chỉ AutoCAD có giao diện) | Bấm đúp hoặc nút xanh để đặt, nút cam để rải |

### 4.3. Dây, ống

| Lệnh | Việc | Cách dùng |
|---|---|---|
| `PCCC_DAYBC` | Đi dây báo cháy tự động (thường / địa chỉ) | Mục 3.3 |
| `PCCC_DAY` | Dây nối tiếp thiết bị | Mục 3.3 |
| `PCCC_ONG` | Vẽ ống, dây trên layer `PCCC_<mã>` | Nhập mã, bấm các điểm |
| `PCCC_TUYEN` | Ống nhánh sprinkler xương cá | Mục 3.5 |
| `PCCC_DOIDN` | Đổi cỡ ống | Chọn đoạn ống, nhập mã mới |

### 4.4. Dim

| Lệnh | Việc | Cách dùng |
|---|---|---|
| `PCCC_ADIM` | Dim tự động cả lưới thiết bị | Lọc Block / Line / Pline, quét cửa sổ, 2 mốc tường (Enter bỏ qua), bấm điểm đặt đường dim |
| `PCCC_CDIM` | Dim tiếp chuỗi ADIM vừa tạo | Bấm các điểm tiếp theo |
| `PCCC_ADIMMOC` | Bấm trước mép tường / trục cho ADIM | Nhập 1 (đầu) hoặc 2 (cuối), bấm điểm |
| `PCCC_CANDIM` | Dóng hàng đường kích thước | Chọn các dim, bấm điểm đường dim đi qua |

### 4.5. Kiểm tra, hiệu chỉnh

| Lệnh | Việc | Cách dùng |
|---|---|---|
| `PCCC_KT` | Kiểm vùng mù đầu báo | Quét phòng + thiết bị (Enter = cả bản vẽ): % mù từng phòng, khoanh đỏ điểm mù trên `PCCC_KIEM_MU` |
| `PCCC_TRUNG` | Thiết bị đặt trùng | Quét vùng: cùng mã cách ≤ 10 mm thì khoanh đỏ |
| `PCCC_LOPCHUAN` | Kiểm, áp layer chuẩn thiết bị | Quét vùng; Co = chuyển về `PCCC_<hệ>`, Khong = khoanh đỏ |
| `PCCC_DANHSO` | Đánh số thiết bị | Chọn thiết bị, tiền tố, số bắt đầu, thứ tự XY hoặc YX |
| `PCCC_XOAY` | Xoay từng block quanh điểm chèn | Chọn block, nhập góc thêm |
| `PCCC_TYLE` | Tỉ lệ từng block | Chọn block, nhập hệ số |
| `PCCC_DOIBLOCK` | Đổi block, giữ điểm, góc, tỉ lệ | Chọn block, nhập mã mới |
| `PCCC_TIENICH` | 27 tiện ích trong 6 nhóm | Phân hệ Tiện ích của palette |

### 4.6. Thống kê, hồ sơ

| Lệnh | Việc | Cách dùng |
|---|---|---|
| `PCCC_KYHIEU` | Bảng ký hiệu thiết bị có trên tờ | Chọn mặt bằng, bấm góc trên trái, cao chữ |
| `PCCC_QUYDINH` | Bảng quy định lắp đặt đầu báo | Bấm góc trên trái, cao chữ |
| `PCCC_BOC` | Bóc khối lượng, gửi web báo giá | Chọn vùng (Enter = cả bản vẽ) |
| `PCCC_BANG` | Chèn bảng thống kê (theo lần `PCCC_BOC` gần nhất) | Bấm điểm chèn, cao chữ |
| `PCCC_THUYETMINH` | Thuyết minh văn mẫu theo hệ | Bấm góc trên trái, cao chữ |

### 4.7. Thư viện, cài đặt, tự động hoá

| Lệnh | Việc | Cách dùng |
|---|---|---|
| `PCCC_LIB` | Nạp, cập nhật block từ `PCCC_Blocks.dwg` | Gõ lệnh; Taomoi = tạo lại thư viện mặc định |
| `PCCC_THEMBLOCK` | Thêm block của bạn (kể cả block động) vào thư viện | Chọn block, nhập mã, tên, hệ, đơn vị |
| `PCCC_GAN` | Gán block có sẵn trong bản vẽ vào mã vật tư | Chọn block, nhập mã (Rieng / Chung cho từng trạng thái Visibility) |
| `PCCC_SPEC` | Chạy file spec JSON (như Claude gọi `cad_chay`) | Nhập đường dẫn .json; kết quả `<tên>.kq.json` |
| `PCCC_BANQUYEN` | Bản quyền | Email, Đăng ký hoặc dán key, Kích hoạt |

## 5. Mã vật tư thường dùng

Nguồn duy nhất: `data/vat_tu.json`.

| Hệ | Mã |
|---|---|
| Báo cháy thường | `BC_KHOI`, `BC_NHIET`, `BC_BEAM`, `BC_TOHOP` (nút ấn + chuông + đèn), `BC_NUTAN`, `BC_CHUONG`, `BC_DEN`, `BC_DTCD` (điện trở cuối dây), `BC_TT` (tủ) |
| Báo cháy địa chỉ | `BC_KHOI_DC`, `BC_NHIET_DC`, `BC_NUTAN_DC`, `BC_ZM`, `BC_CM`, `BC_R2ML` |
| Sprinkler | `SPR_DAU_UP`, `SPR_DAU_DOWN`, `SPR_DAU_SIDE`, `SPR_VAN_OSY_DN65`…`DN150`, `SPR_CTDC`, `SPR_VAN_KT`, `SPR_VAN_XK`, `SPR_VAN_BD` |
| Họng nước | `HVT_TU`, `HVT_VAN_CHAN`, `HVT_VAN_1C` |
| Ống | `ONG_DN25` … `ONG_DN150` |
| Dây | `DAY_BC` (2x1,5 chống cháy), `DAY_BC_10` (2x1,0), `DAY_BC_10_FR`, `DAY_NGUON_15`, `DAY_NGUON_15_FR`, `ONG_LUON` |
| Thoát nạn | `DEN_EXIT`, `DEN_SUCO` |

Block của bạn: `PCCC_THEMBLOCK` (thêm mã mới) hoặc `PCCC_GAN` (gán block có sẵn vào mã), lưu trong `block_alias.json`.

## 6. Layer plugin tạo

| Layer | Nội dung |
|---|---|
| `PCCC_VUNG` | Vùng (phòng) để rải, polyline kín |
| `PCCC_BC`, `PCCC_SPR`... | Thiết bị theo hệ |
| `PCCC_ONG_DN25` … | Ống theo cỡ; dây `PCCC_DAY_BC`... (đổi được ở trang Cài đặt) |
| `PCCC_KT` | Dim sau khi rải (đổi ở trang Cài đặt) |
| `PCCC_KIEM`, `PCCC_KIEM_MU` | Khoanh đỏ của lệnh kiểm tra |
| `PCCC_NHAN` | Chữ DN, nhãn |

## 7. Lưu ý đã gặp khi thiết kế thật

1. Tự dò tường có thể gộp lõi thang máy, buồng thang vào phòng bên cạnh (cửa sát lõi, tường lõi vẽ trên layer không phải layer tường). Đã sửa trường hợp tường lõi nằm trong block đặt trên layer nội thất. Xem vùng `PCCC_VUNG` vẽ ra; sai thì xoá, vẽ tay polyline kín rồi rải lại. Mép trong tường vẽ trên layer `0` thì vùng có thể lấy mép ngoài tường (lệch bằng bề dày tường).
2. Có sẵn vùng `PCCC_VUNG` trong khung chọn thì `PCCC_RAI` không hỏi điểm phòng: muốn dò tường cho phòng khác thì chọn khung không chứa vùng có sẵn.
3. Phòng chữ L rải sprinkler: chia 2 vùng chữ nhật, nếu không có thể có đầu phun thêm sát tường ở góc trong.
4. `PCCC_TUYEN`: plugin bỏ đầu phun khác loại hoặc ở vùng tách rời (mục 3.5); phòng cùng loại đầu phun nằm sát nhau vẫn phải chọn bằng `WP`.
5. Chưa tự xét dầm khi không bật `Dam`; trần có dầm sâu hơn 0,3 m thì bật `Dam` (đầu báo và sprinkler) và cho cỡ dầm. Nhãn dầm được ghép với nét dầm cùng phương chữ trước. Nét dầm sơ đồ hẹp hơn dầm thật (vd vẽ 200, thật 250) thì khoang đo được lớn hơn thật: khoang sát 12 m² có thể thành 2 đầu phun.
6. Cỡ ống `TuDong` là theo số đầu, chưa phải tính thủy lực: kiểm lại bằng module thủy lực (web TKPCCC, tab Thủy lực).
7. Hình dim, chữ có thể đè nhau ở chỗ hai chuỗi cắt nhau: dời bằng `PCCC_CANDIM` hoặc kéo tay.
8. Khung chọn `W` chỉ lấy đối tượng nằm trọn trong khung: nét dầm thò ra ngoài khung bị bỏ, khoang không khép. Quét khung ôm trọn nét dầm.

## 8. Dùng cùng Claude (MCP)

Khi AutoCAD đang mở, Claude gọi được `cad_tt` (trạng thái bản vẽ), `cad_chay` (chạy gói spec: vẽ, rải, đi tuyến, đếm, chụp ảnh), `cad_anh` (in PNG). Mỗi lần `cad_chay` là một bước Undo (gõ `U` để hoàn tác). Từ vựng spec: `cad/skills/TU_VUNG.md`.
