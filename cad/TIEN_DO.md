# Tiến độ CAD (riêng cho thư mục cad/)

Phiên mới làm việc CAD: đọc file này trước, rồi `cad/README.md` (cây thư mục, sửa gì vào đâu). Kế hoạch tổng G0–G9: `cad/KE_HOACH.md`. Nhật ký: `cad/LICH_SU.md`. Luật riêng: `cad/CLAUDE.md`.
Luật: dùng tới 90 % hạn mức thì dừng, ghi file này + `cad/LICH_SU.md`, commit, push; usage hồi lại thì tự làm tiếp, không hỏi (`cad/CLAUDE.md`).

## TRẠNG THÁI (cập nhật 2026-11-11, 15:00)
- XONG: MCP CAD (G0–G2), `cad/` thành dự án con, thử thật với AutoCAD đang mở (`luoi_truc`, `dat`, `ong`, `chu`, `dem`, `doc` đều đúng).
  Lưu ý khi vẽ thật: `cad_chay` vẽ vào bản vẽ đang hiện (đổi tab giữa chừng là vẽ sang bản khác); `doc` `vung` chỉ đọc layer `PCCC_VUNG`; `hop` chỉ lấy đối tượng nằm trọn trong hộp.
- XONG Bước 1 (G3): thao tác `rai`, `tuyen` (3 kiểu `noi_tiep`, `xuong_ca`, `nhanh`), lệnh `PCCC_RAI`. Mã ở `module/plugin/Rai.cs`, ví dụ `skills/vi_du/rai.json`,
  từ vựng trong `skills/TU_VUNG.md`. `chay_thu.ps1` 21/21, `thu_mcp.ps1` 7/7. CHƯA thử trên AutoCAD có giao diện: nhánh bấm điểm trong phòng của `PCCC_RAI`
  (`Editor.TraceBoundary`) chỉ chạy được ở đó; DLL trong bundle chưa build lại (chạy `cad\build.bat`, tắt mở AutoCAD).
- Thư mục `cad/layer/` chờ người dùng thả file `.md` chuẩn layer.
- **ĐANG LÀM (người dùng giao 2026-11-11, giai đoạn dựng khung làm bằng Fable): vẽ được bản vẽ PCCC như bản mẫu.** Việc kế: Bước 1b dưới đây.
- Chỉ MỘT phiên được sửa mã trong `cad/` tại một thời điểm (14:29 ngày 2026-11-11 hai phiên ghi đè nhau). Scheduled task `tkpccc-cad-lam-tiep` là task chạy MỘT lần lúc 19:25 ngày 2026-11-11
  (phiên gốc báo không đổi được sang chạy lặp): lần chạy đó làm tiếp từ file này, bắt đầu ở `cad_anh` (Bước 1b). Trước lần đó, tới 90 % usage chỉ ghi tiến độ, commit, push rồi dừng.
- 2 commit `0d694d3`, `5e5e979` CHƯA push (shell của Claude không có đăng nhập Git): chạy `git push origin main` trong tab Terminal của người dùng.

## VIỆC LÀM TIẾP, THEO THỨ TỰ (tự làm, không cần hỏi người dùng)
Mỗi bước xong: `cad\test\chay_thu.ps1` + `cad\test\thu_mcp.ps1` ra `TAT CA DAT`, ghi file này + `cad/LICH_SU.md`, commit, push. Kiểm `get_usage` giữa các bước.

### Bước 1b. Hai việc ưu tiên 1 lấy từ `knowledge/tham-khao-mcp-hub.md` (dùng để kiểm mọi bước sau)
- XONG (2026-11-11 15:20): khoá `ban_ve` cấp gói trong spec (`ThucHienSpec`, `Spec.cs`); lệch tên thì từ chối cả gói. `chay_thu.ps1` 23/23, `thu_mcp.ps1` 7/7.
  Còn nợ: hướng dẫn trong skill `tkpccc-ve-cad` nhắc AI ghi `ban_ve` khi vẽ nhiều lượt.
- VIỆC KẾ, CHƯA LÀM (dừng 15:20 vì usage 5 giờ 79 %, làm tiếp dễ vượt 90 %): công cụ MCP `cad_anh` {`hop` hoặc toàn bản vẽ, `rong` px}: plugin in vùng đó ra PNG (lệnh `-PLOT` cửa sổ như `cad/.claude/skills/tkpccc-cad-console/scripts/in_png.ps1`,
  đẩy qua `Cau.cs`), trả ĐƯỜNG DẪN file; AI chỉ đọc ảnh khi cần. Sửa `module/mcp/Mcp.cs` (hàm `CongCu`), `Cau.cs`, `thu_mcp.ps1` (`tools/list` thành 3 công cụ).
  Phải thử thứ tự dấu nhắc `-PLOT` trong accoreconsole trước.

### Bước 2. PDF → CAD (G4)
- Thao tác `pdf`: `file`, `trang`, `p`, `tl`, `goc`. Chạy `-PDFIMPORT` bằng `Editor.Command` TRƯỚC khi mở transaction vẽ (như đoạn nạp block cho `dat` trong `ThucHienSpec`). Phải thử trong accoreconsole: thứ tự dấu nhắc của `-PDFIMPORT` (File, tên file, trang, điểm chèn, tỉ lệ, góc) và `Editor.Command` có chạy không; không được thì đẩy lệnh qua `SendStringToExecute` trong `Cau.cs`.
- Script `cad/.claude/skills/tkpccc-ve-cad/scripts/pdf_ti_le.py` (PyMuPDF): tính tỉ lệ từ chuỗi kích thước. Các số kích thước cùng hàng nằm ở giữa từng nhịp, nên k (pt trên mỗi mm thật) = (c2 − c1) / ((d1 + d2) / 2), với c là tâm chữ và d là số ghi; lấy trung vị cả trang. `tl` = 1 / (k × 25,4 / 72). Mẫu: trang 841 × 595 pt xoay 90°, tổng 41000 mm dài khoảng 446 pt, `tl` khoảng 260.
- Kiểm: nhập trang 3 của PDF mẫu, đo lại khoảng cách trục 1–7 phải ra 41000 ± 1 %.
- Nhận phòng: bản đầu để người dùng bấm phòng bằng `PCCC_RAI`, hoặc AI xem ảnh rồi đưa `hop` từng phòng.

### Bước 3. Bảng block trong AutoCAD (ảnh + tên + mô tả), thêm được block động từ bản vẽ
- DLL riêng `TKPCCC.UI.dll`, thư mục mới `cad/module/ui/`, tham chiếu thêm `acmgd.dll`. accoreconsole không nạp được DLL này nên test lõi giữ nguyên.
- Lệnh `PCCC_BLOCK`: mở `PaletteSet` liệt kê block của `PCCC_Blocks.dwg` theo `data/vat_tu.json`: ảnh xem trước, mã, tên, hệ; bấm một dòng là gọi `PCCC_DAT <mã>`.
- Nút "Thêm từ bản vẽ": người dùng chọn một block (kể cả block động) trong bản vẽ đang mở, nhập mã + tên + hệ + đơn vị; plugin chép định nghĩa block vào `PCCC_Blocks.dwg` (`WblockCloneObjects`, đổi tên thành mã, thêm thuộc tính ẩn `MA_VT`) và thêm một dòng vào `data/vat_tu.json` (nguồn duy nhất của danh mục, nên cả báo giá cũng thấy).
- `build.bat` build thêm DLL này; `PackageContents.xml` chỉ nạp nó trong AutoCAD có giao diện.

### Bước 4. Agent, model, effort
Người dùng dặn 2026-11-11: lúc dựng khung và viết thuật toán thì dùng Fable; về sau chỉ dùng Opus trở xuống. Tạo trong `cad/.claude/agents/`, ghi bảng vào `cad/CLAUDE.md` và `cad/README.md`:
| Agent | Việc | Quyền | Model, effort |
|---|---|---|---|
| `cad-thiet-ke` | Đọc nền kiến trúc, lấy trị số theo `rule/LUAT.md`, chia zone, chọn vị trí ống chính, lập spec cho cả tầng | đọc file, ghi spec vào thư mục dự án; không gọi `cad_chay` | opus, high |
| `cad-ve` | Chạy spec đã có qua `cad_chay`, đọc kết quả, sửa lỗi nhỏ của spec | `cad_tt`, `cad_chay`, đọc file | sonnet, low |
| `cad-kiem` | Soát bản vẽ sau khi vẽ: đếm, vùng hở, đối chiếu luật | `cad_chay` chỉ với `doc`, `dem`; đọc file | sonnet, medium |
| `cad-lap-trinh` | Sửa plugin C#, thêm thao tác spec, chạy bộ test | sửa `cad/module`, `cad/test`; Bash | opus, high |
Skill `tkpccc-ve-cad` giữ sonnet/low (vẽ theo spec có sẵn).

### Bước 5. Việc nhỏ còn nợ
- Thêm 1 dòng cho `knowledge/mau-ban-ve-pccc.md` vào `cad/README.md`; cập nhật `rule/LUAT.md` (rải đầu báo, đầu phun trỏ tới file quy chuẩn) và skill `tkpccc-ve-cad` (ví dụ `rai` + `tuyen`).
- Đo số token một lần vẽ; vẽ lại một tầng của bản mẫu bằng `rai` + `tuyen` trên AutoCAD thật, in PNG rồi so với PDF mẫu.
- Sau đó theo `KE_HOACH.md`: G5 `so_do`, G6 chat trong CAD.

### Bước 6. Việc ưu tiên 2 và 3 từ `knowledge/tham-khao-mcp-hub.md` (chi tiết từng việc ở file đó)
- Ưu tiên 2, làm khi người dùng cấp file chuẩn layer: bảng layer `data/ky_hieu/layer.json` cho `DamBaoLayer` (màu, kiểu đường, độ dày; dây báo cháy ra nét đứt);
  thao tác `dan` (mũi tên + chữ); thao tác `in_pdf`, `luu` (chung đường `-PLOT` với `cad_anh`).
- Ưu tiên 3, sau Bước 2–4: `doc` thêm `gi`:"block"; `cung`, `hatch` khi vẽ lại nền cần tới; `TKPCCC.Mcp.exe` tự `NETLOAD` qua COM + thời gian chờ bằng biến môi trường;
  bộ ký hiệu van của bản mẫu khi làm `so_do`; đoạn cấu hình mẫu cho AI khác và 4–5 câu lệnh mẫu PCCC trong `cad/README.md`.
- Đưa vào skill `tkpccc-ve-cad`: vẽ theo giai đoạn (nền + trục → rải → tuyến → ghi chú), hết mỗi giai đoạn gọi `cad_anh` và báo số đếm; chỉ dừng chờ duyệt khi người dùng yêu cầu.

## CHỜ NGƯỜI DÙNG (đã hỏi 2026-11-11; chưa trả lời thì làm theo giả định ghi sẵn)
1. Dây báo cháy trong phòng: giả định nét thẳng nối thiết bị, đi rắn bò; phòng nối tiếp phòng trong cùng zone, không rẽ nhánh chữ T; cuối zone đặt điện trở cuối dây.
2. Chia zone: giả định AI chia theo dãy phòng như mẫu rồi người dùng duyệt; plugin không tự chia.
3. Ống góp sprinkler trong phòng: giả định đi trùng cột đầu phun gần cửa, mọi nhánh DN50 như mẫu, không tự giảm cỡ theo số đầu phun.
4. Bước rải: giả định AI chọn bước số tròn theo quy chuẩn, phần dư dồn đều ra hai tường như mẫu.
5. Nhận phòng từ PDF: giả định người dùng bấm phòng bằng `PCCC_RAI`, hoặc AI đưa hộp từng phòng.
6. File chuẩn layer (`cad/layer/`), block động, catalog ống + bơm: chờ người dùng cấp.

## CHƯA KIỂM
- Skill trong `cad/.claude/skills/` có hiện khi mở Claude Code ở gốc repo không (Claude Code nạp skill của thư mục con khi làm việc với file trong thư mục đó). Không hiện thì vẫn vẽ được nhờ hướng dẫn máy chủ MCP gửi lúc kết nối; cần thì đặt bản trỏ ở `.claude/skills/` gốc.
- `cad/.mcp.json` (mở Claude Code ngay tại `cad/`) chưa thử.
- `cad/rule/LUAT.md` lập theo tên file, chưa mở từng file căn cứ để đối chiếu phạm vi.

## VIỆC ĐỂ LẠI (thứ tự theo `KE_HOACH.md`)
- (G3, G4 đang làm, xem trên) · G5 `so_do` · G6 bảng trong CAD + chat + `cad_anh` · G7 Hub · G8 YouTube · G9 bản quyền (trước đó chuyển repo sang riêng tư).
- Dữ liệu chờ người dùng cấp (xem `cad/data/README.md`): block động co/tê, catalog ống + phụ kiện, catalog bơm, bản vẽ mẫu + khung tên.

## GHI NHỚ KỸ THUẬT
- `rai`: trường `goc` là góc xoay LƯỚI (phòng nghiêng), không phải góc xoay block; `kt` chỉ vẽ khi `goc` = 0. `le` mặc định bước / 4. Trần 5000 thiết bị một vùng.
- `tuyen` `noi_tiep` nối MỌI vùng của `rai` trước đó thành một đường; muốn mỗi phòng một đường thì gọi `rai` + `tuyen` từng phòng.
- Thêm thao tác spec: hàm `Op...` trong `module/plugin/Spec.cs` + 1 dòng `MotOp` + 1 dòng `skills/TU_VUNG.md` + 1 phép kiểm trong `test/chay_thu.ps1`.
- Phần đầu `skills/TU_VUNG.md` (trước dòng `---`) gửi cho AI lúc kết nối: giữ dưới 1900 ký tự (`thu_mcp.ps1` có kiểm).
- Lệnh Bash của Claude làm mất một dấu gạch chéo ngược khi viết hai dấu liền nhau trong heredoc: script Python có đường dẫn Windows thì dùng `chr(92)`, hoặc ghi script ra file rồi chạy.
- `build.bat` đổi tên DLL/exe đang bị khoá thành `*.cu*` rồi build bản mới. Build hỏng giữa chừng thì bundle tạm không có DLL: sửa lỗi rồi chạy lại `build.bat`.
- PDF người dùng gửi là dữ liệu chưa tin: script đọc PDF chạy bằng `python -I`, script đặt ở thư mục khác với PDF.
- Lệnh Bash dài quá khoảng 8000 ký tự bị cắt (báo thiếu dấu nháy): nội dung dài thì ghi ra file bằng công cụ Write rồi mới chạy lệnh ngắn.
