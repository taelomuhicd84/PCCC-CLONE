# Nhật ký CAD (riêng cho thư mục cad/)

## 2026-11-10 — MCP CAD (G0, G1, G2 của `KE_HOACH.md`)
Người dùng yêu cầu vẽ CAD tự động, tối ưu token, gửi ảnh tham khảo (FP AI, NĐD Tool, Claude + `execute_lisp`, MCP Hub Pro, skill vẽ cầu). Chốt giữa chừng: trước mắt chỉ làm MCP CAD, các phần khác để lại. Hỏi về ngôn ngữ: chọn C# làm chính.

Đã làm:
- Sổ sách riêng trong `cad/`: `README.md`, `KE_HOACH.md`, `TIEN_DO.md`, file này. Luật 85 % ghi vào `CLAUDE.md`.
- `cad/src/Spec.cs`: lệnh `PCCC_SPEC` đọc gói spec JSON, chạy trong một transaction (một bước Undo), ghi kết quả gọn `<tên>.kq.json`.
  10 thao tác: `luoi_truc`, `dat`, `ong`, `net`, `tron`, `chu`, `kt`, `xoa`, `doc`, `dem`. Thao tác sai vào `loi`, không làm hỏng gói.
- `cad/src/Lenh.cs`: lớp `Lenh` thành `partial`; phần chèn thiết bị của `PCCC_DAT` tách thành `ChenThietBi` dùng chung với thao tác `dat`.
- `cad/src/Cau.cs`: timer 250 ms trên luồng chính, chỉ bật trong `acad.exe`; AutoCAD rảnh (`CMDACTIVE` = 0) thì chuyển việc sang `dang\` và gọi `PCCC_SPEC`; ghi `song.json` khoảng 2 giây một lần.
- `cad/chung/HangDoi.cs`: hàng đợi file `%LOCALAPPDATA%\TKPCCC\cad` (`vao`, `dang`, `ra`), ghi qua file tạm rồi đổi tên.
- `cad/mcp/Mcp.cs` → `cad/bin/TKPCCC.Mcp.exe`: máy chủ MCP stdio viết tay, không thư viện ngoài. Công cụ `cad_tt`, `cad_chay`. Phần đầu `TU_VUNG.md` gửi cho AI lúc kết nối (1003 ký tự).
- `cad/spec/TU_VUNG.md` (từ vựng, nguồn duy nhất), `cad/spec/vi_du/thu.json`.
- Skill `tkpccc-ve-cad`, `.mcp.json` (máy chủ `cad`).
- `cad/build.bat` build cả DLL và exe; `cad/test/chay_thu.ps1` thêm 6 phép kiểm spec; `cad/test/thu_mcp.ps1` mới.

Quyết định khác với ROADMAP cũ, và lý do:
- Máy chủ MCP bằng C# thay cho Node: máy nào có Windows cũng chạy, không cài Node/Python; cùng trình biên dịch `csc.exe` với plugin.
- Hàng đợi file thay cho cổng 127.0.0.1: không mở cổng, không kẹt cổng, file việc chạy lại được trong accoreconsole để test.
- Cầu nối nằm trong DLL lõi (không tách DLL giao diện): chỉ cần `System.Windows.Forms` của .NET, không cần `acmgd`.
- `cad_anh` (ảnh chụp bản vẽ) để lại, làm cùng bảng giao diện vì cần `acmgd`.

Kiểm chứng: `chay_thu.ps1` 12/12 đạt (`ve` = 50, `xoa` = 1, `BC_KHOI` = 3, `ONG_DN50` = 15 m, 1 lỗi báo đúng vị trí); ảnh PNG in từ `spec.dwg` đúng hình (lưới trục, bóng, kích thước, chữ có dấu). `thu_mcp.ps1` 7/7 đạt (initialize, tools/list, báo AutoCAD chưa mở, việc qua hàng đợi, kết quả `ve` = 29, hàng đợi sạch).

Chưa kiểm: đường nối với AutoCAD đang mở (timer + `SendStringToExecute`), và số token thật khi AI vẽ. Xem `TIEN_DO.md`.

## 2026-11-11 — sắp `cad/` thành dự án con
Người dùng muốn `cad/` như một dự án nhỏ trong dự án lớn, cây thư mục dễ kiểm soát: `.claude` (agent, skill), `skill`, `knowledge`, `data`, `module`, `rule`.

Đã làm:
- Chuyển mã: `src/` → `module/plugin/`, `mcp/` → `module/mcp/`, `chung/` → `module/chung/`; thêm `module/lisp/`. `spec/` → `skills/` (từ vựng cho mọi AI).
- Chuyển 2 skill `tkpccc-ve-cad`, `tkpccc-cad-console` từ `.claude/skills/` gốc vào `cad/.claude/skills/`; tạo `cad/.claude/agents/` (rỗng), `cad/CLAUDE.md`, `cad/.mcp.json`.
- Thêm `data/ky_hieu/ky_hieu.json`: mặc định của `luoi_truc`, `kt`, `chu` rời khỏi mã, plugin đọc lại ở mỗi lần `PCCC_SPEC`; `build.bat` chép vào bundle. Tạo sẵn `data/block`, `ong`, `bom`, `mau` (chờ file và catalog của người dùng).
- Thêm `rule/LUAT.md` (việc CAD ↔ file quy chuẩn nguyên văn ↔ skill, agent; không chép trị số) và `knowledge/quy-uoc-ban-ve.md`.
- Sửa mọi đường dẫn trong `build.bat`, 2 bộ test, skill, `CLAUDE.md`, `STRUCTURE.md`. `README.md` của cad viết lại thành bản đồ thư mục.

Giữ nguyên, có lý do: `data/vat_tu.json` ở gốc (dùng chung với báo giá) và `TKPCCC.bundle/Contents/PCCC_Blocks.dwg` (plugin ghi cạnh DLL, người dùng sửa trực tiếp). Chép sang `cad/data` sẽ thành hai bản lệch nhau.

Kiểm chứng: `chay_thu.ps1` 12/12 (thêm phép kiểm `ky_hieu.json` đổi tên layer trục thành `TRUC_T`), `thu_mcp.ps1` 7/7. Máy chủ `cad` đã nối được trong Claude Code (đường dẫn tương đối trong `.mcp.json` chạy được).
Chưa làm được: thử với AutoCAD đang mở, vì AutoCAD của người dùng chưa nạp plugin (chưa có `song.json`). Việc kế ghi trong `TIEN_DO.md`.

## 2026-11-11 (sau) — thử thật với AutoCAD đang mở
Người dùng NETLOAD xong. `cad_tt`: ok, bản vẽ `K2-2026 - Copy.dwg`, 177 layer. `cad_chay` luoi_truc 5x9: ok true, ve = 90, hop [-2555,-2555,16955,34955]. Đường nối timer + SendStringToExecute trong Cau.cs hoạt động với AutoCAD thật.
Lưu ý: lưới vẽ vào bản vẽ thật (gốc 0,0), bỏ bằng một lần Undo. Chưa đo token.

## 2026-11-11 (trưa) — người dùng giao: vẽ được bản vẽ PCCC như bản mẫu
Giao kèm PDF mẫu 4 tờ (báo cháy, vách tường, sprinkler, EXIT-EM). Yêu cầu: viết `rai`, `tuyen`, `PCCC_RAI`; PDF → CAD; bảng block có ảnh + tên, thêm được block động từ bản vẽ; tự phân agent, model, effort (dựng khung bằng Fable, về sau Opus trở xuống). Luật mới: tới 90 % hạn mức thì lưu, push, tạm dừng; usage hồi thì tự làm tiếp, không hỏi.

Đã làm:
- Thử thật trên AutoCAD (sáng): `luoi_truc`, `dat`, `ong`, `chu`, `dem`, `doc` đều đúng. Thêm thư mục `cad/layer/`.
- Ghi luật 90 % vào `CLAUDE.md`, `cad/CLAUDE.md`, `cad/KE_HOACH.md`, `cad/TIEN_DO.md`.
- Đọc PDF mẫu (chữ vector + ảnh phóng từng vùng), ghi `cad/knowledge/mau-ban-ve-pccc.md`.
- Chốt thiết kế `rai`, `tuyen`, `pdf`, bảng block, 4 agent: ghi đủ trong `cad/TIEN_DO.md`.

Dừng 11:00 vì usage 5 giờ chạm 90 % (reset 14:20). Chưa viết mã. Đã gửi người dùng sáu câu hỏi về tư duy đi dây, đi ống, kèm giả định để làm tiếp nếu chưa có trả lời. Đã hẹn scheduled task `tkpccc-cad-lam-tiep` chạy 14:25 để tự làm tiếp.

## 2026-11-11 (chiều) — Bước 1: `rai`, `tuyen`, `PCCC_RAI`
Scheduled task `tkpccc-cad-lam-tiep` chạy 14:25 bằng `claude-fable-5-2` (usage 5 giờ 22 %). Lúc 14:28–14:29 phiên gốc "Plugin cad PCCC" cũng viết Bước 1:
hai phiên ghi đè nhau (`Rai.cs`, `rai.json`, dòng trùng trong `Lenh.cs`, `Spec.cs`, `chay_thu.ps1`, lỗi biên dịch `CS0111`). Hoà giải: giữ bản `Rai.cs` + `rai.json`
của phiên gốc, gỡ dòng trùng; phiên gốc thôi sửa mã, chỉ phiên hẹn giờ ghi file.

Đã làm:
- `module/plugin/Rai.cs`: `OpRai` (lưới theo hộp bao, bỏ điểm ngoài đa giác, tự kiểm vùng hở `ho`), `OpTuyen` (`noi_tiep`, `xuong_ca`, `nhanh`, nhãn DN),
  `RaiTay` (lệnh `PCCC_RAI`: bấm điểm trong phòng bằng `TraceBoundary` hoặc `Doituong`). Thêm trần 5000 thiết bị một vùng (chặn bước sai đơn vị).
- `Spec.cs`: 2 dòng `MotOp`, trường `KqRai`, `KqTuyen`, `DiemRai` của `GoiSpec`, nạp block trước cho `rai`, trả `kq["rai"]`, `kq["tuyen"]`.
- `Lenh.cs` (`PCCC_RAI` + `PCCC_HELP`), `PackageContents.xml`, `skills/TU_VUNG.md` (phần đầu 1440 ký tự), `skills/vi_du/rai.json`.
- `knowledge/tham-khao-mcp-hub.md` (phiên gốc viết theo việc người dùng giao 14:29): ý lấy từ tài liệu AutoCAD MCP của bên khác; đã chép việc vào `TIEN_DO.md` (Bước 1b, Bước 6).

Kiểm chứng: `chay_thu.ps1` 21/21 (phòng 6500 × 4500, `s` 4000 ra 4 thiết bị bước 3250 × 2250; `buoc` [4000, 3000] ra cách tường 1250 / 750 như bản mẫu;
`xuong_ca` 3 đoạn 12,75 m; `noi_tiep` 1 đường 11,05 m; vùng chữ L còn 3 thiết bị, không hở; thiếu `s` và `buoc` thì báo lỗi; `PCCC_RAI` nhánh `Doituong`). `thu_mcp.ps1` 7/7.
Chưa kiểm: `PCCC_RAI` nhánh bấm điểm và hình vẽ trên AutoCAD có giao diện; chưa in PNG so với PDF mẫu (chờ `cad_anh`).
Thêm 15:20: khoá `ban_ve` cấp gói (spec ghi tên bản vẽ, AutoCAD đang hiện bản khác thì từ chối cả gói); `chay_thu.ps1` 23/23, `thu_mcp.ps1` 7/7.
Dừng 15:20: usage 5 giờ 79 % (hai phiên dùng chung), việc kế `cad_anh` dễ đẩy qua 90 %. `git push` từ shell của Claude không lấy được đăng nhập
(`could not read Username`): đẩy bằng tab Terminal của người dùng, không được thì người dùng tự `git push`.
## 2026-11-11 (chiều) — phiên hẹn giờ commit Bước 1; người dùng trả lời 6 câu hỏi thiết kế
- Phiên hẹn giờ 14:25 (Fable) commit Bước 1 (`rai`, `tuyen`, `PCCC_RAI`) và khoá `ban_ve`; kết quả test do phiên đó ghi ở mục TRẠNG THÁI của `cad/TIEN_DO.md`. Ba commit của nó được phiên gốc push lúc 15:15.
- Sự cố: phiên gốc cũng viết mã cùng lúc nên hai phiên sửa trùng file; đã giao lại cho một phiên ghi. Bài học: khi phiên hẹn giờ đang chạy thì phiên khác không sửa mã.
- Người dùng gửi tài liệu hướng dẫn AutoCAD MCP của bên khác: ý lấy được ghi ở `cad/knowledge/tham-khao-mcp-hub.md`.
- Người dùng trả lời 6 câu hỏi (đi dây, chia zone, hệ địa chỉ, ống góp, bước rải, nhận phòng): ghi ở `cad/knowledge/tu-duy-di-day-di-ong.md`; việc phải sửa là Bước 1c của `cad/TIEN_DO.md`. Thêm mục nhắc ba thứ người dùng chưa cấp (chuẩn layer, block động, catalog).
- Dừng 15:15 vì usage 5 giờ gần 90 % (reset 19:20). Task hẹn giờ chạy một lần lúc 19:25 sẽ làm tiếp.
## 2026-11-11 (tối) — Bước 1b: công cụ MCP `cad_anh`
- Người dùng bảo xoá mọi hẹn giờ và làm tiếp ngay trong phiên: đã xoá scheduled task `tkpccc-cad-lam-tiep` (19:25); phiên này làm tiếp từ 19:22 sau khi usage 5 giờ reset.
- Thêm thao tác spec `anh` (`Spec.cs`, hàm `InAnh`) và công cụ MCP `cad_anh` (`Mcp.cs`): in Model ra PNG bằng lệnh `-PLOT` qua `Editor.Command`, thiết bị `PublishToWeb PNG.pc3`,
  5 khổ ngang 640–1600 px (`rong`), `hop` = cửa sổ theo WCS, bỏ `hop` = toàn bản vẽ. Chạy SAU khi commit gói nên vẽ và chụp trong một lần gọi được.
  Ảnh nằm ở `%LOCALAPPDATA%\TKPCCC\cad\anh`, tự dọn sau 1 ngày; kết quả chỉ trả đường dẫn. `Cau.cs` không phải sửa vì ảnh là một thao tác của `PCCC_SPEC`.
- Kiểm chứng: `chay_thu.ps1` 24/24 (PNG 800 px có nét của hình vẽ trong cùng gói, vùng chụp nằm xa mọi hình khác), `thu_mcp.ps1` 9/9 (`tools/list` 3 công cụ, `cad_anh` ghi đúng gói vào hàng đợi).
- Chưa kiểm: `anh` trên AutoCAD có giao diện.
- Bước 1c (20:00), theo trả lời của người dùng 11/11: `rai` thêm `dt` (m² tối đa một thiết bị; plugin lấy số cột × số hàng ít thiết bị nhất mà ô lưới không vượt, trả `m2`; đi cùng `buoc` mà vượt thì báo lỗi);
  `tuyen` kiểu `tia` (mỗi đầu báo một nét ra đèn báo phòng); lệnh `PCCC_TUYEN` (chọn đầu phun, chọn ống chính, bấm vị trí ống góp); `PCCC_RAI` hỏi thêm diện tích bảo vệ.
  `rule/LUAT.md` thêm BC-04 (chia zone), BC-05 (đi dây), SP-02 (rải đầu phun); skill `tkpccc-ve-cad` thêm quy trình phân vùng rồi mới rải.
  Kiểm chứng: `chay_thu.ps1` 27/27 (phòng 16 × 12 m, `s` 4000, `dt` 12 ra 4 × 4 bước 4000 × 3000, 12 m²; `tia` 4 nét 17,87 m; `PCCC_TUYEN` 3 đoạn 10,88 m qua điểm bấm), `thu_mcp.ps1` 9/9.
  Chưa kiểm: `PCCC_TUYEN` và dấu nhắc mới của `PCCC_RAI` trên AutoCAD có giao diện.
- Dừng 20:05: "Weekly · Fable" 92 % (reset 05:00 ngày 2026-11-14). Hẹn `CronCreate` 05:03 ngày 14/11 trong phiên. `git push` đang chờ người dùng đăng nhập Git Credential Manager.
- 21:40 người dùng tự reset hạn mức (mọi cửa sổ về 0 %) và bảo làm tiếp; xoá hẹn 05:03 ngày 14/11.
- Bước 3 (22:05), bảng block: `module/plugin/Block.cs` có lệnh `PCCC_THEMBLOCK` (chọn block trong bản vẽ, kể cả block động; nhập mã mới, tên, hệ, đơn vị; chép định nghĩa sang `PCCC_Blocks.dwg`
  bằng `WblockCloneObjects`, đổi tên thành mã, thêm thuộc tính ẩn `MA_VT`; thêm đúng một dòng vào `vat_tu.json`, tự đọc lại file mới trước khi ghi đè) và phần vẽ ảnh xem trước 64 px
  (GDI+, từ nét của block). `module/ui/BangBlock.cs` → `TKPCCC.UI.dll`: lệnh `PCCC_BLOCK` mở `PaletteSet` có ô lọc, danh sách ảnh + mã + tên + hệ, bấm đúp = `PCCC_DAT <mã>`, nút "Thêm từ bản vẽ", "Nạp lại".
  `KhoiDong` tự nạp DLL giao diện khi chạy trong `acad.exe`. `build.bat` build thêm DLL này và ghi `nguon_vat_tu.txt`.
  Kiểm chứng: `chay_thu.ps1` 34/34 (block thường → mã `VAN_KHACH`: `PCCC_DAT` đặt được, `PCCC_BOC` đếm 1; `vat_tu.json` thêm đúng 1 dòng; ảnh có nét; DLL giao diện biên dịch được).
  Thêm `cad/test/thu_gui.ps1`: tự mở AutoCAD thật, 7/7 đạt (cầu nối + MCP thật, `pdf`, `rai`, `tuyen`, `anh`, `cad_anh`, bảng `PCCC_BLOCK` 31 dòng). Nhờ đó các mục "chưa thử trên AutoCAD có giao diện" của `anh`, `pdf` đã đạt.
  Chưa kiểm: bấm đúp và nút trong bảng, `PCCC_THEMBLOCK` với block động thật, `PCCC_TUYEN` và `PCCC_RAI` bấm chuột.
- Bước 2 (21:50), PDF → CAD: thao tác spec `pdf` (`Spec.cs`, hàm `NhapPdf`) chạy `-PDFIMPORT` qua `Editor.Command` TRƯỚC transaction vẽ, nhân 25,4 × `tl` để nét vào đúng kích thước thật,
  trả `n` + layer. Script `cad/.claude/skills/tkpccc-ve-cad/scripts/pdf_ti_le.py` tính `tl` từ chuỗi kích thước: hai số liền nhau cùng hàng cách nhau (d1 + d2) / 2;
  trung vị rồi lọc hai vòng 1 % và 0,5 %, mỗi vòng lấy tổng pt / tổng mm. PDF thử `cad/test/mau_kt.pdf` (1:200) sinh bằng `cad/test/tao_pdf_mau.py`.
  Kiểm chứng: `chay_thu.ps1` 29/29 (`tl` 200,0; nhập 40 đối tượng; trục 1–7 đo lại 41000,0; khung 76905,5), `thu_mcp.ps1` 9/9.
  PDF mẫu thật trang 3 (sprinkler tầng 1): `tl` 285,0; nhập 39 056 đối tượng trong 8 giây; nhãn trục 1 và 7 cách nhau 40 961 theo `tl` này (−0,09 %). Xem ảnh `anh`: đúng chiều, đủ khung tên.
  Phát hiện trên bản mẫu: hai nhịp ghi 1700 nhưng vẽ 1785 (tổng ghi 18800, các nhịp cộng lại 18600), làm bản tính đầu (chỉ trung vị) lệch 0,3 %; chữ font VNI nhập vào sai dấu.
  Chưa kiểm: `pdf` trên AutoCAD có giao diện.
- Gỡ `cad/TKPCCC.bundle/Contents/hardcopy.log` khỏi git (log AutoCAD tự ghi, có đường dẫn trên máy người dùng) và thêm vào `.gitignore`. Các bản cũ của file vẫn còn trong lịch sử repo công khai.
