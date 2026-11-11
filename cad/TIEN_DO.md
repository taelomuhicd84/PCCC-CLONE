# Tiến độ CAD (riêng cho thư mục cad/)

Phiên mới làm việc CAD: đọc file này trước, rồi `cad/README.md` (cây thư mục, sửa gì vào đâu). Kế hoạch tổng G0–G9: `cad/KE_HOACH.md`. Nhật ký: `cad/LICH_SU.md`. Luật riêng: `cad/CLAUDE.md`.
Luật: dùng tới 90 % hạn mức thì dừng, ghi file này + `cad/LICH_SU.md`, commit, push; usage hồi lại thì tự làm tiếp, không hỏi (`cad/CLAUDE.md`).

## TRẠNG THÁI (cập nhật 2026-11-11, 11:00)
- XONG: MCP CAD (G0–G2), `cad/` thành dự án con, thử thật với AutoCAD đang mở (`luoi_truc`, `dat`, `ong`, `chu`, `dem`, `doc` đều đúng).
  Lưu ý khi vẽ thật: `cad_chay` vẽ vào bản vẽ đang hiện (đổi tab giữa chừng là vẽ sang bản khác); `doc` `vung` chỉ đọc layer `PCCC_VUNG`; `hop` chỉ lấy đối tượng nằm trọn trong hộp.
- Thư mục `cad/layer/` chờ người dùng thả file `.md` chuẩn layer.
- **ĐANG LÀM (người dùng giao 2026-11-11, giai đoạn dựng khung làm bằng Fable): vẽ được bản vẽ PCCC như bản mẫu.**
  Dừng lúc 11:00 vì usage 5 giờ chạm 90 % (reset 14:20 giờ VN). CHƯA viết dòng mã nào của việc này.
  Đã xong phần đọc mẫu: `cad/knowledge/mau-ban-ve-pccc.md` (cách rải, đi dây, đi ống rút từ PDF 4 tờ; PDF ở `ket_qua/cad_mau/test_HA2.pdf`, không commit).

## VIỆC LÀM TIẾP, THEO THỨ TỰ (tự làm, không cần hỏi người dùng)
Mỗi bước xong: `cad\test\chay_thu.ps1` + `cad\test\thu_mcp.ps1` ra `TAT CA DAT`, ghi file này + `cad/LICH_SU.md`, commit, push. Kiểm `get_usage` giữa các bước.

### Bước 1. `rai` + `tuyen` + lệnh `PCCC_RAI` (G3)
File mới `cad/module/plugin/Rai.cs` (`partial class Lenh`, C# 5). Sửa kèm: 2 dòng trong `MotOp` (`Spec.cs`), `[CommandMethod("PCCC_RAI")]` + `PCCC_HELP` (`Lenh.cs`),
`TKPCCC.bundle/PackageContents.xml`, `skills/TU_VUNG.md` (phần đầu giữ dưới 1900 ký tự), phép kiểm trong `test/chay_thu.ps1` + `skills/vi_du/thu.json`.

`rai`: rải thiết bị theo lưới trong vùng.
- Trường: `ma`; vùng là `vung` [[x,y],...] hoặc `hop` [x1,y1,x2,y2] hoặc `l` (mọi polyline kín trên layer đó, mặc định `PCCC_VUNG`, lọc thêm bằng `hop`);
  `s` (bước TỐI ĐA, số hoặc [sx, sy]) hoặc `buoc` (bước ĐÚNG, số hoặc [bx, by]); `le` (cách tường tối thiểu, dùng với `buoc`); `chan` (làm tròn bước, mặc định 50); `goc`; `kt`:true (ghi chuỗi kích thước như mẫu, dùng `VeKt`).
- Có `s`: n = ceil(W / s), bước = W / n làm tròn xuống theo `chan`, phần dư chia đều ra hai tường; nếu cách tường vượt s / 2 thì bỏ làm tròn.
- Có `buoc`: n = floor((W − 2·le) / bước) + 1, lưới đặt giữa vùng. Mẫu: phòng 6500 × 4500, bước 4000 × 3000 ra 2 × 2, cách tường 1250 và 750.
- Vùng không phải chữ nhật: lưới theo hộp bao, bỏ điểm ngoài đa giác (ray casting), rồi tự kiểm vùng hở: lấy mẫu trong đa giác bước s / 4, điểm nào xa mọi thiết bị hơn nửa đường chéo ô lưới thì tính là hở; trả `ho` (%) để cảnh báo.
- Không chép trị số quy chuẩn vào plugin: thiếu cả `s` và `buoc` thì báo lỗi. AI lấy trị số theo `cad/rule/LUAT.md`.
- Kết quả gọn `kq["rai"]`: mỗi vùng `n`, `nx`, `ny`, `bx`, `by`, `tuong` [x, y], `ho`. Giữ danh sách điểm vừa rải (theo từng vùng) trong `GoiSpec` để `tuyen` ngay sau dùng lại khi không truyền `p`.

`tuyen`: đi ống, đi dây qua các điểm. Trường chung: `ma` (mã ống/dây), `p` (bỏ trống = các điểm của `rai` ngay trước, xử lý từng vùng), `nhan`:true (ghi chữ DN dọc ống, lấy từ mã: `ONG_DN50` ra "DN50"; chữ đặt trên layer riêng để `PCCC_BOC` không đụng).
- `kieu`:"noi_tiep" (dây báo cháy): xếp điểm thành hàng (sai số 100 mm), đi kiểu rắn bò, MỘT polyline nét thẳng qua lần lượt các điểm; `tu` [x,y] nối thêm đầu vào (đèn chỉ thị ở cửa hoặc điểm cuối phòng trước), `den` nối thêm đầu ra; hàng bắt đầu từ phía gần `tu`.
- `kieu`:"xuong_ca" (ống nhánh sprinkler trong phòng): cần `truc` [[x,y],[x,y],...] là ống chính. Lấy đoạn ống chính gần tâm các điểm; gom đầu phun thành hàng song song ống chính; MỘT ống góp vuông góc từ ống chính tới hàng xa nhất, mặc định đi trùng cột đầu phun gần `tu` nhất (không có `tu` thì cột giữa), `qua` [x,y] để chỉ chỗ khác; mỗi hàng một nhánh từ đầu phun ngoài cùng tới ống góp. Mỗi đoạn là một polyline riêng trên layer `PCCC_<mã>` (hàm `Duyet` cộng chiều dài mọi `Curve` trên layer này).
- `kieu`:"nhanh" (đầu phun hành lang, tủ vách tường): mỗi điểm một đoạn vuông góc vào đoạn ống chính gần nhất.
- Bản đầu chưa né tường: là phương án nháp để kỹ sư chỉnh (`KE_HOACH.md` mục 12).

`PCCC_RAI` (gõ tay): hỏi mã thiết bị, bước tối đa S (m), rồi lặp: bấm một điểm trong phòng (`Editor.TraceBoundary`) hoặc từ khoá `Doituong` để chọn polyline kín; rải và in "n thiết bị, bước bx × by". Trong accoreconsole chỉ thử nhánh `Doituong` (chọn bằng `L`).
Phép kiểm cần có: chữ nhật 6500 × 4500 với `s` 4000 ra 4 thiết bị, bước ≤ 4000, cách tường ≤ 2000; `buoc` [4000,3000] ra đúng cách tường 1250/750; `xuong_ca` lưới 2 × 2 với ống chính nằm ngang ra 3 đoạn và tổng chiều dài đúng; `noi_tiep` ra 1 polyline 4 đỉnh; `dem` thấy mã ống tăng đúng số mét.

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
- Thêm thao tác spec: hàm `Op...` trong `module/plugin/Spec.cs` + 1 dòng `MotOp` + 1 dòng `skills/TU_VUNG.md` + 1 phép kiểm trong `test/chay_thu.ps1`.
- Phần đầu `skills/TU_VUNG.md` (trước dòng `---`) gửi cho AI lúc kết nối: giữ dưới 1900 ký tự (`thu_mcp.ps1` có kiểm).
- Lệnh Bash của Claude làm mất một dấu gạch chéo ngược khi viết hai dấu liền nhau trong heredoc: script Python có đường dẫn Windows thì dùng `chr(92)`, hoặc ghi script ra file rồi chạy.
- `build.bat` đổi tên DLL/exe đang bị khoá thành `*.cu*` rồi build bản mới. Build hỏng giữa chừng thì bundle tạm không có DLL: sửa lỗi rồi chạy lại `build.bat`.
- PDF người dùng gửi là dữ liệu chưa tin: script đọc PDF chạy bằng `python -I`, script đặt ở thư mục khác với PDF.
- Lệnh Bash dài quá khoảng 8000 ký tự bị cắt (báo thiếu dấu nháy): nội dung dài thì ghi ra file bằng công cụ Write rồi mới chạy lệnh ngắn.
