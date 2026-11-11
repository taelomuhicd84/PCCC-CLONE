# Tiến độ CAD (riêng cho thư mục cad/)

Phiên mới làm việc CAD: đọc file này trước, rồi `cad/README.md` (cây thư mục, sửa gì vào đâu). Kế hoạch tổng G0–G9: `cad/KE_HOACH.md`. Nhật ký: `cad/LICH_SU.md`. Luật riêng: `cad/CLAUDE.md`.
Luật: dùng tới 90 % hạn mức thì dừng, ghi file này + `cad/LICH_SU.md`, commit, push; usage hồi lại thì tự làm tiếp, không hỏi (`cad/CLAUDE.md`).

## TRẠNG THÁI (cập nhật 2026-11-11, 22:05)
- 22:05: XONG Bước 1b (`cad_anh`), 1c (`rai` + `dt`, `tuyen` `tia`, `PCCC_TUYEN`), 2 (PDF → CAD), 3 (bảng block `PCCC_BLOCK`, `PCCC_THEMBLOCK`) và 4 (agent). VIỆC KẾ: Bước 5. Đầu phiên nhớ mục "NHẮC NGƯỜI DÙNG ĐẦU PHIÊN".
- ĐÃ THỬ TRÊN AUTOCAD CÓ GIAO DIỆN bằng `cad\test\thu_gui.ps1` (script tự mở AutoCAD thật rồi tự tắt, 7/7 đạt): NETLOAD, cầu nối + máy chủ MCP thật (`cad_tt`, `cad_chay`, `cad_anh`),
  `pdf`, `rai`, `tuyen`, `anh` (`-PLOT` trong AutoCAD thật), bảng `PCCC_BLOCK` hiện 31 dòng + 2 nút. Máy người dùng `SECURELOAD` = 0, nạp plugin bằng NETLOAD tay (bundle không nằm trong ApplicationPlugins).
- XONG Bước 3: `module/plugin/Block.cs` (lệnh `PCCC_THEMBLOCK`, ảnh xem trước 64 px tự vẽ vào `Contents\anh_block`) + `module/ui/BangBlock.cs` → `TKPCCC.UI.dll` (lệnh `PCCC_BLOCK`).
  NETLOAD một file `TKPCCC.Cad.dll` là đủ: nó tự nạp `TKPCCC.UI.dll` khi chạy trong `acad.exe`. `chay_thu.ps1` 33/33, `thu_mcp.ps1` 9/9.
- XONG Bước 4 (22:15): 4 agent trong `cad/.claude/agents/` (`cad-thiet-ke`, `cad-ve`, `cad-kiem`, `cad-lap-trinh`), bảng việc ở `cad/CLAUDE.md`. CHƯA chạy thử agent nào trên việc thật;
  chưa kiểm Claude Code có nạp agent của thư mục con khi mở ở gốc repo không (README cũ ghi chỉ nạp khi mở tại `cad/`).
- Hạn mức: người dùng tự reset khoảng 21:40 (mọi cửa sổ về 0 %) và bảo làm tiếp. 22:20 chốt ở mốc Bước 4: cửa sổ 5 giờ 72 % (reset 02:30 ngày 2026-11-12), tuần Fable 20 %, không đủ chỗ cho Bước 5.
  Đã hẹn `CronCreate` một lần lúc 02:33 ngày 2026-11-12 trong phiên đang mở để tự làm Bước 5 (đóng ứng dụng thì mất hẹn: phiên mới đọc file này rồi làm).
  Phiên 11/11 đã dài 520 nghìn token ngữ cảnh nên mỗi lượt gọi tốn khoảng 1,2 % cửa sổ 5 giờ; phiên mới rẻ hơn nhiều lần.
  22:35 làm thêm bước con 1 của Bước 5 rồi dừng hẳn ở khoảng 85 % cửa sổ 5 giờ.
- Người dùng dặn 22:25: "Dùng Fable 5.2 tối đa". Claude hiểu: giai đoạn dựng khung này làm trực tiếp bằng Fable 5.2 ở phiên chính, chưa giao việc cho agent opus / sonnet.
  Đã hỏi lại câu đó có gồm đặt effort của phiên lên max và đổi model 4 agent sang Fable không; CHƯA có trả lời, nên chưa đổi gì.
- 5 commit (`f3d354f` 1b, `eee2b1f` 1c, `79b9fe8` 2, `b5104b2` 3, `5f8a738` 4) và commit chốt mốc CHƯA lên GitHub lúc 22:20: `git push` ở tab Terminal chờ Git Credential Manager từ 19:30
  (người dùng đăng nhập ở cửa sổ nó bật lên; máy không có `gh`). Xong thì chạy lại `git push origin main`.
- XONG Bước 2: thao tác spec `pdf` (`Spec.cs`, hàm `NhapPdf`, lệnh `-PDFIMPORT`) + `cad/.claude/skills/tkpccc-ve-cad/scripts/pdf_ti_le.py` (tính `tl` từ chuỗi kích thước).
  `chay_thu.ps1` 29/29 (PDF thử 1:200: `tl` 200,0, trục 1–7 đo lại 41000,0), `thu_mcp.ps1` 9/9. PDF mẫu thật `ket_qua/cad_mau/test_HA2.pdf` trang 3: `tl` 285,0, nhập 39 056 đối tượng trong 8 giây,
  trục 1–7 lệch −0,09 % (đạt ± 1 %). Tỉ lệ 4 trang: 260,3 / 285,2 / 285,0 / 249,6.
- XONG Bước 1c: `rai` nhận `dt` (m² tối đa một thiết bị) và trả `m2`; `tuyen` kiểu `tia`; lệnh `PCCC_TUYEN` (chọn đầu phun, chọn ống chính, bấm vị trí ống góp); `PCCC_RAI` hỏi thêm diện tích bảo vệ.
  `chay_thu.ps1` 27/27, `thu_mcp.ps1` 9/9, bundle build lại 20:05. CHƯA thử trên AutoCAD có giao diện: `PCCC_TUYEN` (chọn bằng chuột, bấm điểm) và dấu nhắc mới của `PCCC_RAI`.
- XONG Bước 1b: thao tác spec `anh` (`module/plugin/Spec.cs`, hàm `InAnh`) + công cụ MCP `cad_anh` (`module/mcp/Mcp.cs`). In Model ra PNG bằng `-PLOT` qua `Editor.Command`,
  chạy SAU khi commit gói nên vẽ + chụp trong một lần gọi được. `chay_thu.ps1` 24/24, `thu_mcp.ps1` 9/9; bundle đã build lại 19:45 (tắt mở AutoCAD và Claude Code để nạp).
  Trên AutoCAD có giao diện: đạt (PNG 800 và 640 px có nét).
- XONG: MCP CAD (G0–G2), `cad/` thành dự án con, thử thật với AutoCAD đang mở (`luoi_truc`, `dat`, `ong`, `chu`, `dem`, `doc` đều đúng).
  Lưu ý khi vẽ thật: `cad_chay` vẽ vào bản vẽ đang hiện (đổi tab giữa chừng là vẽ sang bản khác); `doc` `vung` chỉ đọc layer `PCCC_VUNG`; `hop` chỉ lấy đối tượng nằm trọn trong hộp.
- XONG Bước 1 (G3): thao tác `rai`, `tuyen` (3 kiểu `noi_tiep`, `xuong_ca`, `nhanh`), lệnh `PCCC_RAI`. Mã ở `module/plugin/Rai.cs`, ví dụ `skills/vi_du/rai.json`,
  từ vựng trong `skills/TU_VUNG.md`. `chay_thu.ps1` 21/21, `thu_mcp.ps1` 7/7. CHƯA thử trên AutoCAD có giao diện: nhánh bấm điểm trong phòng của `PCCC_RAI`
  (`Editor.TraceBoundary`) chỉ chạy được ở đó; DLL trong bundle chưa build lại (chạy `cad\build.bat`, tắt mở AutoCAD).
- Thư mục `cad/layer/` chờ người dùng thả file `.md` chuẩn layer.
- **ĐANG LÀM (người dùng giao 2026-11-11, giai đoạn dựng khung làm bằng Fable): vẽ được bản vẽ PCCC như bản mẫu.** Việc kế: Bước 5 dưới đây.
- Chỉ MỘT phiên được sửa mã trong `cad/` tại một thời điểm (14:29 ngày 2026-11-11 hai phiên ghi đè nhau).
- Hẹn giờ: người dùng bảo "xoá hết mọi hẹn giờ, tự động chạy lại tại phiên này" (2026-11-11 15:20), scheduled task `tkpccc-cad-lam-tiep` đã xoá.
  Claude hiểu là: hẹn làm tiếp bằng `CronCreate` một lần ngay trong phiên đang làm, không tạo scheduled task mở phiên mới (chưa hỏi lại người dùng có áp dụng cho mọi lần không).
- Push: shell của Claude không lấy được đăng nhập Git thì chạy `git push origin main` trong tab Terminal của người dùng.

## VIỆC LÀM TIẾP, THEO THỨ TỰ (tự làm, không cần hỏi người dùng)
Mỗi bước xong: `cad\test\chay_thu.ps1` + `cad\test\thu_mcp.ps1` ra `TAT CA DAT`, ghi file này + `cad/LICH_SU.md`, commit, push. Kiểm `get_usage` giữa các bước.

### Bước 1b. XONG 2026-11-11 (khoá `ban_ve` 15:20, `cad_anh` 19:45; chi tiết ở `cad/LICH_SU.md`)
Skill `tkpccc-ve-cad` đã nhắc ghi `ban_ve` khi vẽ nhiều lượt và cách thêm `anh` vào cuối gói.

### Bước 1c. XONG 2026-11-11 20:00 (sửa theo trả lời của người dùng 11/11; chi tiết ở `cad/LICH_SU.md`)
Quy trình vùng (vẽ polyline `PCCC_VUNG` trước, người dùng kiểm, rồi `rai` theo `l`) đã ghi vào skill `tkpccc-ve-cad`; luật chia zone, đi dây, bước rải ở `rule/LUAT.md` (BC-04, BC-05, SP-02).
Còn lại của các ý này: agent `cad-thiet-ke` (Bước 4) và `so_do` hệ địa chỉ (G5) dùng các luật đó. `knowledge/` gốc chưa có bản nguyên văn bảng khoảng cách đầu phun của TCVN 7336.

### Bước 2. PDF → CAD (G4). XONG 2026-11-11 21:50 (chi tiết ở `cad/LICH_SU.md`; cách dùng ở skill `tkpccc-ve-cad`, mục "Nền kiến trúc từ PDF")
Còn lại của G4:
- Chữ font VNI trong PDF nhập vào bị sai dấu (vd "TAÛI TROÏNG"): chưa chuyển mã. `thamdinh/pccc_checker/pdf/` có sẵn `vni_to_unicode`, `is_vni_font` để tham khảo khi làm.
- Nền nhập vào giữ màu của PDF; bản mẫu muốn nền kiến trúc xám nhạt, nét mảnh: làm cùng lúc có file chuẩn layer.
- Nhận phòng tự động chưa có: AI xem ảnh (`anh`) rồi đưa hộp từng phòng lên `PCCC_VUNG`, hoặc người dùng bấm phòng bằng `PCCC_RAI`.
- Tờ PDF chỉ có ảnh quét thì `pdf` báo lỗi: khi gặp mới làm đường `PDFATTACH` (gắn làm nền, không thành nét).

### Bước 3. Bảng block trong AutoCAD. XONG 2026-11-11 22:05 (chi tiết ở `cad/LICH_SU.md`)
Người dùng cần tự xem và báo lại (máy không kiểm được):
- Hình thức bảng `PCCC_BLOCK`: cỡ cột, ảnh 40 px, chữ; bấm đúp một dòng có ra `PCCC_DAT <mã>` và con trỏ về vùng vẽ không; nút "Thêm từ bản vẽ" (gọi `PCCC_THEMBLOCK`, xong thì bảng tự nạp lại).
- `PCCC_THEMBLOCK` với block ĐỘNG thật (accoreconsole chỉ thử được block thường): chèn ra bằng `PCCC_DAT`, đổi trạng thái, rồi `PCCC_BOC` có đếm không.
Giới hạn bản này (ghi `shortcut:` trong mã):
- Chỉ thêm mã MỚI. Đổi hình cho mã đã có: sửa trong `PCCC_Blocks.dwg` hoặc `PCCC_GAN`. Block phải vẽ theo mm như thư viện.
- Ảnh xem trước do plugin tự vẽ từ nét (không hatch, không xoay chữ, block động theo trạng thái đang lưu). Ảnh cũ hơn `PCCC_Blocks.dwg` thì `PCCC_LIB`, `PCCC_BLOCK` tự vẽ lại.
- Mã mới ghi vào `vat_tu.json` cạnh DLL và vào `data/vat_tu.json` của repo (đường dẫn do `build.bat` để ở `Contents\nguon_vat_tu.txt`); bundle chép đi máy khác không có file đó thì chỉ ghi bản cạnh DLL và báo.

### Bước 4. Agent, model, effort. XONG 2026-11-11 22:15 (file ở `cad/.claude/agents/`, bảng việc ở `cad/CLAUDE.md`)
Còn lại: chạy thử cả chuỗi `cad-thiet-ke` → `cad-ve` → `cad-kiem` trên một tầng của bản mẫu (gộp vào Bước 5), rồi sửa lời giao việc theo chỗ agent làm sai.
Người dùng dặn 2026-11-11: lúc dựng khung và viết thuật toán thì dùng Fable; về sau chỉ dùng Opus trở xuống. Bảng lúc lập kế hoạch:
| Agent | Việc | Quyền | Model, effort |
|---|---|---|---|
| `cad-thiet-ke` | Đọc nền kiến trúc, lấy trị số theo `rule/LUAT.md`, chia zone, chọn vị trí ống chính, lập spec cho cả tầng | đọc file, ghi spec vào thư mục dự án; không gọi `cad_chay` | opus, high |
| `cad-ve` | Chạy spec đã có qua `cad_chay`, đọc kết quả, sửa lỗi nhỏ của spec | `cad_tt`, `cad_chay`, đọc file | sonnet, low |
| `cad-kiem` | Soát bản vẽ sau khi vẽ: đếm, vùng hở, đối chiếu luật | `cad_chay` chỉ với `doc`, `dem`; đọc file | sonnet, medium |
| `cad-lap-trinh` | Sửa plugin C#, thêm thao tác spec, chạy bộ test | sửa `cad/module`, `cad/test`; Bash | opus, high |
Skill `tkpccc-ve-cad` giữ sonnet/low (vẽ theo spec có sẵn).

### Bước 5. Vẽ lại một tầng của bản mẫu, đo token (VIỆC KẾ)
- XONG từ các bước trước: dòng README cho `mau-ban-ve-pccc.md`, `rule/LUAT.md`, ví dụ `rai` + `tuyen` trong skill.
- Vẽ lại tầng 1 sprinkler của bản mẫu (`ket_qua/cad_mau/test_HA2.pdf` trang 3, `tl` 285,0), không cần AutoCAD có giao diện (chạy `PCCC_SPEC` trong accoreconsole như `chay_thu.ps1`):
  1. XONG 22:35: nền đã nhập vào `ket_qua/cad_mau/cad/t1_spr.dwg` (39 056 đối tượng, `tl` 285,0); ảnh mặt bằng 1600 px ở `ket_qua/cad_mau/cad/skills/anh/t1_nen_1.png`
     (vùng [28000, 31000, 72500, 54800], 27,8 mm mỗi px), CHƯA đọc ảnh. Chạy tiếp vào đúng DWG đó bằng
     `cad\.claude\skills\tkpccc-ve-cad\scripts\chay_spec.ps1 -Spec <spec.json> -Dwg ket_qua\cad_mau\cad\t1_spr.dwg` (nhánh mở DWG có sẵn của script chưa thử).
     Điểm đặt chữ nhãn trục ở `tl` 285,0 (lệch tim trục vài trăm mm): trục 1 → 7 tại x = 29291, 36578, 43781, 50767, 57270, 64208, 70253; A → F tại y = 33429, 38327, 40114, 45530, 47314, 52213.
     Dãy phòng dưới A–B (sâu 4900), hành lang B–C, khối giữa C–D (5400), hành lang D–E, dãy phòng trên E–F (4900).
  2. Lập hộp từng phòng lên `PCCC_VUNG` (`net` `kin`:true) từ ảnh, chụp `anh` để người dùng duyệt (người dùng dặn: AI đưa hộp, họ kiểm).
  3. `rai` `SPR_DAU_DOWN` với `s` 4000, `dt` 12 (số ghi trên bảng thông số của tờ mẫu, không phải tra quy chuẩn) rồi `tuyen` `xuong_ca` / `nhanh` theo ống chính.
  4. `anh` rồi so với nền: tờ mẫu có 64 đầu phun, phòng hát lưới 2 × 2. Ghi chỗ lệch vào `cad/LICH_SU.md`. Spec để ở `ket_qua/cad_mau/cad/skills/` (không commit).
  Làm việc này qua chuỗi agent `cad-thiet-ke` → `cad-ve` → `cad-kiem` để thử luôn agent.
- Đo số token một lần vẽ (`claude -p --output-format json`), ghi vào `cad/LICH_SU.md`.
- Sau đó theo `KE_HOACH.md`: G5 `so_do`, G6 chat trong CAD (bảng `PCCC_BLOCK` là tab đầu của G6).

### Bước 6. Việc ưu tiên 2 và 3 từ `knowledge/tham-khao-mcp-hub.md` (chi tiết từng việc ở file đó)
- Ưu tiên 2, làm khi người dùng cấp file chuẩn layer: bảng layer `data/ky_hieu/layer.json` cho `DamBaoLayer` (màu, kiểu đường, độ dày; dây báo cháy ra nét đứt);
  thao tác `dan` (mũi tên + chữ); thao tác `in_pdf`, `luu` (chung đường `-PLOT` với `cad_anh`).
- Ưu tiên 3, sau Bước 2–4: `doc` thêm `gi`:"block"; `cung`, `hatch` khi vẽ lại nền cần tới; `TKPCCC.Mcp.exe` tự `NETLOAD` qua COM + thời gian chờ bằng biến môi trường;
  bộ ký hiệu van của bản mẫu khi làm `so_do`; đoạn cấu hình mẫu cho AI khác và 4–5 câu lệnh mẫu PCCC trong `cad/README.md`.
- Đưa vào skill `tkpccc-ve-cad`: vẽ theo giai đoạn (nền + trục → rải → tuyến → ghi chú), hết mỗi giai đoạn gọi `cad_anh` và báo số đếm; chỉ dừng chờ duyệt khi người dùng yêu cầu.

## NHẮC NGƯỜI DÙNG ĐẦU PHIÊN (người dùng dặn 2026-11-11: mở phiên mới thì phải nhắc)
Ba thứ người dùng CHƯA cấp, bảo Claude làm sườn trước rồi họ sửa sau: (1) file chuẩn layer trong `cad/layer/`; (2) block động của thiết bị; (3) catalog ống + phụ kiện và catalog bơm.
Đầu mỗi phiên CAD nêu một dòng nhắc. Người dùng cấp thứ nào thì gạch thứ đó; đủ cả ba thì xoá mục này.

## NGƯỜI DÙNG ĐÃ TRẢ LỜI 2026-11-11 (đầy đủ: `cad/knowledge/tu-duy-di-day-di-ong.md`)
1. Dây báo cháy hệ thường: nét thẳng, rắn bò, phòng nối tiếp phòng trong cùng zone, không rẽ chữ T, cuối zone có điện trở cuối dây. Có đèn báo phòng thì mỗi đầu báo trong phòng một nét kéo ra đèn.
2. Chia zone: theo giới hạn m² của TCVN VÀ không quá 25 đầu báo một zone (hệ thường), chạm giới hạn nào trước thì dừng. Zone là khu vực: không lấn sang khu vực khác, không để một khu vực thành hai zone lẻ tẻ. Hệ địa chỉ: ZM, CM, RM và mạch vòng, xem knowledge.
3. Ống góp sprinkler: người dùng tự bấm chọn vị trí trong CAD (lệnh của plugin).
4. Bước rải: theo quy chuẩn, tính từ diện tích bảo vệ một đầu phun (TCVN 7336: 12 m² hoặc 9 m²). Trước khi rải phải vẽ polyline phân khu vực theo tường, dầm sàn, vì kèo.
5. Nhận phòng: AI đưa hộp từng phòng, người dùng kiểm lại rồi bấm vào phòng.
6. Chuẩn layer, block động, catalog: để sau, làm sườn trước, phải nhắc (mục trên).

## CHƯA KIỂM
- Skill trong `cad/.claude/skills/` có hiện khi mở Claude Code ở gốc repo không (Claude Code nạp skill của thư mục con khi làm việc với file trong thư mục đó). Không hiện thì vẫn vẽ được nhờ hướng dẫn máy chủ MCP gửi lúc kết nối; cần thì đặt bản trỏ ở `.claude/skills/` gốc.
- `cad/.mcp.json` (mở Claude Code ngay tại `cad/`) chưa thử.
- `cad/rule/LUAT.md` lập theo tên file, chưa mở từng file căn cứ để đối chiếu phạm vi.

## VIỆC ĐỂ LẠI (thứ tự theo `KE_HOACH.md`)
- (G3, G4 đang làm, xem trên) · G5 `so_do` · G6 bảng trong CAD + chat + `cad_anh` · G7 Hub · G8 YouTube · G9 bản quyền (trước đó chuyển repo sang riêng tư).
- Dữ liệu chờ người dùng cấp (xem `cad/data/README.md`): block động co/tê, catalog ống + phụ kiện, catalog bơm, bản vẽ mẫu + khung tên.

## GHI NHỚ KỸ THUẬT
- `rai`: trường `goc` là góc xoay LƯỚI (phòng nghiêng), không phải góc xoay block; `kt` chỉ vẽ khi `goc` = 0. `le` mặc định bước / 4. Trần 5000 thiết bị một vùng.
- `rai` + `dt`: lấy số cột × số hàng ÍT thiết bị nhất mà ô lưới không vượt `dt`, hoà thì lấy ô vuông hơn (16 × 16 m, `s` 4 m, `dt` 12 ra 4 × 6 = 24 đầu, không phải 5 × 5).
  `m2` = Sx × Sy, mỗi phương S = số lớn hơn giữa bước và 2 lần cách tường; làm tròn bước theo `chan` mà đẩy `m2` vượt `dt` thì bỏ làm tròn.
- Kịch bản accoreconsole: `GetSelection` có `SingleOnly` kết thúc ngay sau `L` (không thêm dòng trống); chọn bằng `W x1,y1 x2,y2` được sau khi `_.ZOOM _E`; `GetEntity` thì không trả lời được.
- `tuyen` `noi_tiep` nối MỌI vùng của `rai` trước đó thành một đường; muốn mỗi phòng một đường thì gọi `rai` + `tuyen` từng phòng.
- Thêm thao tác spec: hàm `Op...` trong `module/plugin/Spec.cs` + 1 dòng `MotOp` + 1 dòng `skills/TU_VUNG.md` + 1 phép kiểm trong `test/chay_thu.ps1`.
- Thao tác phải chạy LỆNH AutoCAD (`anh` dùng `-PLOT`, sắp tới `pdf` dùng `-PDFIMPORT`): không đặt trong `MotOp`. `ThucHienSpec` gom chúng rồi gọi `Editor.Command` ngoài transaction
  (`anh` sau commit). `Editor.Command` chạy được trong accoreconsole. Toạ độ gõ cho lệnh thêm dấu `*` đằng trước để tính theo WCS.
- Giao diện: chỉ `module/ui/` được tham chiếu `acmgd.dll` (+ `AdWindows`, `PresentationCore`, `WindowsBase`, `System.Xaml` vì `PaletteSet` kéo theo WPF). DLL này không xử lý bản vẽ:
  gọi `Lenh.DanhMucBlock` của lõi và gửi lệnh bằng `SendStringToExecute`. Trong AutoCAD, điều khiển WinForms chỉ hiện là "Pane" với UI Automation:
  `thu_gui.ps1` tìm khung theo tên rồi đếm dòng bằng thông điệp `LVM_GETITEMCOUNT`. `thu_gui.ps1` chép DLL ra thư mục tạm rồi mới NETLOAD để không đụng thư viện block thật.
- Thứ tự dấu nhắc `-PDFIMPORT` (AutoCAD 2021): F, tên file, số trang, điểm chèn, tỉ lệ, góc xoay. Ở tỉ lệ 1 thì 1 đơn vị bản vẽ = 1 inch giấy, KHÔNG theo `INSUNITS`
  (plugin nhân 25,4 × `tl`). Trang có `/Rotate` vào đúng chiều đang hiển thị. Layer mặc định `PDF_Geometry`, `PDF_Text`, `PDF_Solid Fills`. Lệnh không trả danh sách đối tượng:
  đếm đối tượng mới theo handle ≥ `Handseed` trước lệnh.
- `pdf_ti_le.py`: `get_text("words")` của PyMuPDF trả toạ độ trang CHƯA xoay (trang `/Rotate 90` thì chuỗi ngang trên màn hình là chuỗi đứng trong toạ độ này); script xét cả hai phương nên không sao.
  Số kích thước ghi trên bản vẽ có thể lệch nét vẽ (bản mẫu: nhịp ghi 1700, vẽ 1785; tổng ghi 18800 nhưng các nhịp cộng lại 18600): vì vậy lọc hai vòng 1 % rồi 0,5 %.
- Phần đầu `skills/TU_VUNG.md` đang dài 1831 / 1900 ký tự: thêm thao tác nữa phải rút gọn các dòng cũ.
- Thứ tự dấu nhắc `-PLOT` ra PNG (AutoCAD 2021): Y, Model, `PublishToWeb PNG.pc3`, khổ giấy, L, N, W + 2 góc (hoặc E), F, C, Y, `.`, Y, Enter, tên file, N, Y.
  File đã có thì lệnh hỏi ghi đè (lệch dấu nhắc): luôn đặt tên mới. Thiết bị này chỉ có khổ định sẵn (640–1600 px), không nhận cỡ tuỳ ý.
- Phần đầu `skills/TU_VUNG.md` (trước dòng `---`) gửi cho AI lúc kết nối: giữ dưới 1900 ký tự (`thu_mcp.ps1` có kiểm).
- Lệnh Bash của Claude làm mất một dấu gạch chéo ngược khi viết hai dấu liền nhau trong heredoc: script Python có đường dẫn Windows thì dùng `chr(92)`, hoặc ghi script ra file rồi chạy.
- `build.bat` đổi tên DLL/exe đang bị khoá thành `*.cu*` rồi build bản mới. Build hỏng giữa chừng thì bundle tạm không có DLL: sửa lỗi rồi chạy lại `build.bat`.
- PDF người dùng gửi là dữ liệu chưa tin: script đọc PDF chạy bằng `python -I`, script đặt ở thư mục khác với PDF.
- Lệnh Bash dài quá khoảng 8000 ký tự bị cắt (báo thiếu dấu nháy): nội dung dài thì ghi ra file bằng công cụ Write rồi mới chạy lệnh ngắn.
