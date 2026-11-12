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
- Khoảng 23:00 người dùng chốt lại cách dùng model: viết code bằng Opus, review và kiểm tra bằng Fable effort max, effort còn lại Claude chọn. Thêm agent `cad-review` (fable, max),
  sửa `cad/CLAUDE.md`, đặt lại hẹn 02:33 theo cách này (việc đầu: review mã tối 11/11).
- 22:45 người dùng giải thích: "Fable 5.2 tối đa" = dùng Fable khi tư duy và dựng code cho dự án; chạy dự án thật thì chia agent theo model hợp việc (ghi vào `cad/CLAUDE.md`).
  Lần chạy 02:33 ngày 12/11: chạy hết usage 5 giờ, tự ghi tiến độ + lịch sử, không push; người dùng tự push tay. Đã đặt lại hẹn 02:33 với lời dặn này.
- 22:35 Bước 5, bước con 1: thêm `cad/.claude/skills/tkpccc-ve-cad/scripts/chay_spec.ps1` (chạy một spec trong accoreconsole, lưu DWG, chép ảnh về cạnh spec); dùng nó nhập trang 3 PDF mẫu
  vào `ket_qua/cad_mau/cad/t1_spr.dwg` (39 056 đối tượng) và chụp ảnh mặt bằng 1600 px. Chưa đọc ảnh, chưa lập hộp phòng. Dừng vì cửa sổ 5 giờ 81 %.
  Người dùng dặn "Dùng Fable 5.2 tối đa" (Claude hiểu: làm trực tiếp bằng Fable ở phiên chính; đã hỏi lại về effort và model của agent).
- 22:20 chốt mốc: cửa sổ 5 giờ 72 % (reset 02:30 ngày 12/11), hẹn `CronCreate` 02:33 trong phiên để làm Bước 5. 5 commit của buổi tối chưa lên GitHub (chờ đăng nhập Git Credential Manager).
- Bước 4 (22:15), agent: tạo `cad-thiet-ke` (opus, high; lập spec, không có công cụ vẽ), `cad-ve` (sonnet, low; chỉ chạy spec), `cad-kiem` (sonnet, medium; chỉ `doc`, `dem`, `anh`),
  `cad-lap-trinh` (opus, high; sửa plugin + test) trong `cad/.claude/agents/`; bảng việc ở `cad/CLAUDE.md`. Chưa chạy thử agent nào.
- Bước 3 (22:05), bảng block: `module/plugin/Block.cs` có lệnh `PCCC_THEMBLOCK` (chọn block trong bản vẽ, kể cả block động; nhập mã mới, tên, hệ, đơn vị; chép định nghĩa sang `PCCC_Blocks.dwg`
  bằng `WblockCloneObjects`, đổi tên thành mã, thêm thuộc tính ẩn `MA_VT`; thêm đúng một dòng vào `vat_tu.json`, tự đọc lại file mới trước khi ghi đè) và phần vẽ ảnh xem trước 64 px
  (GDI+, từ nét của block). `module/ui/BangBlock.cs` → `TKPCCC.UI.dll`: lệnh `PCCC_BLOCK` mở `PaletteSet` có ô lọc, danh sách ảnh + mã + tên + hệ, bấm đúp = `PCCC_DAT <mã>`, nút "Thêm từ bản vẽ", "Nạp lại".
  `KhoiDong` tự nạp DLL giao diện khi chạy trong `acad.exe`. `build.bat` build thêm DLL này và ghi `nguon_vat_tu.txt`.
  Kiểm chứng: `chay_thu.ps1` 33/33 (block thường → mã `VAN_KHACH`: `PCCC_DAT` đặt được, `PCCC_BOC` đếm 1; `vat_tu.json` thêm đúng 1 dòng; ảnh có nét; DLL giao diện biên dịch được).
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
## 2026-11-12 (đêm) — lần chạy hẹn giờ 02:33: review bằng Fable max, Bước 5 bằng Opus
- 02:35 bắt đầu. Lượt gọi đầu của phiên chính tốn 36 % cửa sổ 5 giờ và 14 % Fable tuần vì ngữ cảnh 580 nghìn token phải nạp lại sau 4 giờ nghỉ (bộ đệm hết hạn):
  bài học, việc hẹn giờ nên chạy ở phiên mới ngữ cảnh sạch.
- Giao 2 subagent song song (không ai sửa mã): review Fable effort max cho `git diff 24109da..HEAD -- cad`; Opus effort high làm Bước 5 theo `cad-thiet-ke.md`.
- 02:47 Bước 5 xong (Opus, 11,6 phút, 235 nghìn token): 15 vùng, 62 đầu phun so với 64 của mẫu, 11 phòng khớp số lượng, hành lang thiếu 2. Tim trục và tường lấy từ nét vector PDF nên hộp khớp ngay.
  Tìm ra 1 lỗi không báo của `tuyen` `xuong_ca` (ống góp P01 hở 1521 mm vì chân rơi ngoài đoạn ống chính) và 5 chỗ plugin vẽ khác kỹ sư (chọn cột khi hoà, ống chính sát phòng,
  cách tường khi rải theo `s`, hành lang, chữ DN trùng): ghi ở mục "Bước 5" của `cad/TIEN_DO.md`. `chay_spec.ps1` nhánh "DWG đã có" chạy được.
  Cách làm đáng giữ: agent thiết kế tự chạy spec để nhìn ảnh rồi chỉnh (không tách `cad-ve` ra), và lấy hình học từ vector của PDF thay vì ước theo ảnh.

## 2026-11-12 — sửa theo review 11/11 + Bước 5 mục a, b, f (agent `cad-lap-trinh`, Opus)
Bài giao: sửa 1 CHẶN + 12 NÊN SỬA của `ket_qua/cad_review/review_2026-11-11.md`, mục a, b, f của Bước 5, GHI CHÚ loại tài liệu cũ / sửa 1–2 dòng. Không làm c, d, e.

Đã sửa (mã):
- CHẶN `Spec.cs` `NhapPdf`: đọc số trang bằng `UnderlayHost.PdfHost.GetFile(f, "").Items.Count` (chạy được trong accoreconsole), `trang` < 1 hoặc > số trang thì báo lỗi, không gọi `-PDFIMPORT`.
  Không đọc được số trang thì chỉ chặn `trang` < 1 (`shortcut:` trong mã). `InAnh` kiểm `PublishToWeb PNG.pc3` có trong `PlotSettingsValidator.GetPlotDeviceList()` trước `-PLOT`.
- `Spec.cs`: `loi` xếp theo `i` trước khi trả.
- `Block.cs` `PCCC_THEMBLOCK`: (1) block tên trùng mã (block `VAN_KHACH`, mã `van_khach`) không còn bị báo "thư viện đã có block"; (2) `DanhMucThem` dựng + tự kiểm nội dung mới của CẢ HAI file danh mục,
  báo lỗi khi mã đã có trong file gốc, file chỉ đọc hoặc đang khoá, TRƯỚC khi lưu `PCCC_Blocks.dwg`; ghi danh mục sau cùng.
- `Rai.cs` `OpRai`: tính lưới mọi vùng trước, đạt hết mới chèn (vùng sau lỗi thì không để lại thiết bị vùng trước).
- `Rai.cs` `TuyenXuongCa` (NÊN SỬA `Rai.cs:313-326` = Bước 5 a, b): chỉ chọn cột có chân trong đoạn ống chính; không cột nào thì cột chân gần đoạn nhất; hoà thì cột có x nhỏ hơn rồi y nhỏ hơn (không phụ thuộc chiều `truc`);
  chân (kể cả theo `qua`) ngoài đoạn thì nối thêm đoạn dọc trục về đầu mút gần nhất. `qua` nhận nhiều điểm, mỗi điểm dùng cho vùng của `rai` chứa nó (`GoiSpec.VungRai` giữ đa giác từng vùng).
- `Rai.cs` `nhan` (Bước 5 f): bỏ chữ DN khi trong bán kính cỡ chữ × số ký tự đã có chữ cùng nội dung (kể cả chữ của nền PDF).
- `Rai.cs` `PCCC_TUYEN`: chỉ nhận block có mã thiết bị (`MaThietBi`).
- `pdf_ti_le.py`: bỏ số bằng 0 (chuỗi `000` của "1 000 000" gây chia cho 0); `trang` ngoài khoảng thì in lỗi, mã thoát 2.
- `chay_spec.ps1`: `-Dwg` tương đối tính theo `$PWD`; làm trên bản chép spec + DWG trong thư mục tạm tên ASCII rồi chép về (đường dẫn có dấu không còn hỏng; DWG đang mở ở AutoCAD khác thì báo lỗi lúc chép về);
  biến môi trường `TKPCCC_CAD_DLL` để test dùng DLL tạm.
- `test/tao_pdf_mau.py` + `mau_kt.pdf`: một nhịp ghi 6650 vẽ 7000, một số không kề nhau cùng hàng với số tổng, chữ "1 000 000".
Đã sửa (tài liệu): `TU_VUNG.md` (phần chi tiết: `rai`, `tuyen` `xuong_ca`/`qua`/`nhan`, `pdf` số trang và nhập lần hai vào `PDF2_*`), skill `tkpccc-ve-cad` (xoá nền theo tên layer ở gói riêng, ngưỡng `tin_cay` trỏ về `pdf_ti_le.py`,
bỏ "chưa có bảng block", con số 25 đầu báo trỏ về LUAT BC-04), `cad-thiet-ke.md` (trỏ BC-04), `rule/LUAT.md` (bỏ "(chưa tạo)", SP-01), `README.md` (thêm `cad-review`), `cad-review.md` (thêm Write), `TIEN_DO.md` ghi nhớ `-PDFIMPORT`.

Kiểm chứng: `chay_thu.ps1` 42/42 (thêm 9 phép, sửa 4 phép theo review: `PCCC_RAI` phòng 16 × 12 m ra 4 × 4; `PCCC_TUYEN` bấm ngoài các cột x 65000 ra 63,5 m (Enter ra 59,5); `pdf_ti_le.py` trên PDF có cặp số bẩn đòi 200 ± 0,1 %
(thử đảo dấu lọc ra 208,8, bản cũ văng ZeroDivisionError); `vat_tu.json` gốc qua `nguon_vat_tu.txt`); `thu_mcp.ps1` 9/9; `build.bat` xong.
Chạy lại `t1_spr.json` trên bản chép nền (`ket_qua/cad_mau/cad/lan2/`, từ `t1_spr.bak`): `ok`, 62 đầu phun, DN100 56,91 m, DN80 20,54 m, DN50 130,79 m (như lần trước: P01 đổi sang cột 34894, cùng chiều dài),
`ve` 131 (lần trước 153: 22 chữ DN50 trùng bị bỏ). Ảnh `lan2/anh/t1_spr_2.png`: ống góp P01 nối vào DN100, hết hở.
Chưa kiểm: nhánh thiếu thiết bị `PublishToWeb PNG.pc3` (máy có sẵn, không gỡ được để thử); `-PDFIMPORT` với trang sai trong `acad.exe` (đã chặn trước lệnh nên không còn gọi); `thu_gui.ps1` không chạy.
GHI CHÚ của review để nguyên (không phải tài liệu cũ, không sửa trong 1–2 dòng): `Block.cs:248` lưu đè thư viện không cất bản cũ; `NapBlock` `Replace` block lồng trùng tên;
`anh` luôn in Model; `anh` vùng trống ra PNG trắng mà `ok`; `pdf_ti_le.py:34` phương chữ đoán theo hộp chữ; `thu_mcp.ps1:45` (máy chủ tự cắt 1900) và `:84` (kiểm luôn đúng);
`chay_thu.ps1` chưa có `anh` vùng trống, chưa so vị trí `p` của `pdf`, chưa đếm block thư viện trước/sau; `thu_gui.ps1` thử bản build trong bundle; `build.bat` ghi `nguon_vat_tu.txt` theo bảng mã OEM.
(`chay_spec.ps1` không thấy QSAVE hỏng: nay chép DWG về sau cùng, DWG đang mở thì báo lỗi lúc chép.)

## 2026-11-12 14:00 — Bước 5 mục c, d, e (agent `cad-lap-trinh`, Opus)
Đã sửa `module/plugin/Rai.cs`:
- c. `tuyen` kiểu `cot` (hàm `TuyenCot`): mỗi cột thiết bị (cùng vị trí dọc ống chính, gom theo `sai_so`) một nhánh vuông góc ống chính tới thiết bị xa nhất của cột, không có nhánh song song;
  ống chính xuyên phòng thì nhánh đi cả hai phía; chân ngoài đoạn ống chính thì nối thêm dọc trục như `xuong_ca`.
- d. `rai` + `s` nhận `le` (số hoặc [x, y]): giữ số thiết bị, bước = min(`s`, (W − 2·`le`) / (n − 1)) (`ChiaBuoc`), `le` quá `s` / 2 thì lấy `s` / 2. Vượt `dt` thì bỏ làm tròn, rồi bỏ `le` (chia đều như cũ).
  `theo_hang`:true (`KhoangNgang`): mỗi hàng chia lại theo bề rộng trống thật của hàng (giao đường ngang với đa giác), bước không quá bước của lưới. Không truyền `le`/`theo_hang` thì như cũ.
- e. `rai` theo `tim` (`RaiTim`, `DauMut`): đỉnh giữa của tim là chỗ bắt buộc có đầu, giữa hai đỉnh chia đều bước ≤ `s`; hai đầu tim là tường mút (thêm ít đầu nhất để bước ≤ `s`, cách tường nửa bước hoặc gần `le`);
  tim khép kín thì mọi đỉnh bắt buộc; không xét `dt`. Kết quả `rai`: `n`, `bx` (bước lớn nhất), `tuong` [đầu, cuối].
Tài liệu: `skills/TU_VUNG.md` (phần đầu 1893 / 1900 ký tự, đã rút gọn câu đầu; bảng chi tiết `rai`, `tuyen`), `.claude/agents/cad-thiet-ke.md` (khi nào dùng `cot`, `tim`, `theo_hang`).
Kiểm chứng: `chay_thu.ps1` 47/47 (thêm 5 phép, số tính tay ghi ở chú thích `rai_moi.json`: `le` [650,1100] ra 4000 × 2300 cách tường 1150 / 1100; `theo_hang` phòng chữ L 10,25 m
(lưới chung cột thì 10,3); `cot` 13,5 m 4 đoạn; `tim` 6 đầu, bước lớn nhất 3600, cách tường 1500 / 1800, nhánh 16,8 m; `tim` + `le` 600 cách tường 1000; tim kín 8 đầu); `thu_mcp.ps1` 9/9; `build.bat` xong (AutoCAD người dùng đang mở: tắt mở lại để nạp).
Vẽ lại tầng 1 trên bản chép mới của nền (`ket_qua/cad_mau/cad/lan3/`, từ `t1_spr.bak`; spec `lan3/t1_spr.json` = `lan2` + `le` [650,1100] và `theo_hang` cho 11 phòng, P06 `tuyen` `cot`,
HL1/HL2 `rai` `tim` qua 4 góc giao của mẫu): `ok`, 63 đầu phun (mẫu 64): 11 phòng × 4, HL1 9/10, HL2 6/6, HL3 2/2, HL4 2/2. Ống: DN100 56,91 m, DN80 20,54 m, DN50 146,12 m (lan2 130,79: hàng lệch cột nên nhánh hàng dài hơn).
Lệch vị trí so với mẫu (đầu phun mẫu đọc lại từ vòng tròn PDF, ghép cặp gần nhất): trung bình 546 → 412 mm; HL1 1027 → 567, HL2 953 → 539, P04 509 → 275 (hàng trên 52804 / 56704, mẫu 52763 / 56660), P07 667 → 559;
P09 175 → 501, P01 370 → 449 (mẫu P01 hàng dưới dồn trái, hàng trên lại hẹp: không theo một luật đối xứng). P06: 2 nhánh đứng x 64970 / 68470, không còn nhánh ngang (ảnh `lan3/anh/t1_spr_5.png`).
HL1 vẫn thiếu 1: đoạn 49759 → tường 63200 dài 13441 mm, `s` 4000 cho 3 đầu (bước 3840); mẫu đặt 4 (bước khoảng 3000–3500). Truyền `s` 3500 cho HL1 thì ra 10 (tính tay, chưa vẽ).
Chưa kiểm: `PCCC_RAI` chưa có `le`/`theo_hang` (chỉ qua spec); `tim` không làm tròn bước theo `chan`; `thu_gui.ps1` không chạy.

## 2026-11-12 (chiều) — sửa review 12/11 (agent `cad-lap-trinh`, Opus)
Theo `ket_qua/cad_review/review_2026-11-12.md`: 1 CHẶN + 5 NÊN SỬA; 5 GHI CHÚ để nguyên (tim [A, B, A]; `nx`/`bx`/`tuong` của `theo_hang` vẫn của lưới chung; `PCCC_TUYEN` nhận thiết bị khác SPR; `chay_spec.ps1` tên có `[ ]`, tên có dấu + `ban_ve`, DWG chỉ đọc; phép kiểm `pdf_ti_le.py` chưa bắt bỏ vòng lọc).

Đã làm:
- `module/plugin/Spec.cs` (`NhapPdf`): `SoTrangPdf` < 1 thì ném lỗi "không đọc được PDF …: có bảo mật/mật khẩu hoặc hỏng; in lại ra PDF không bảo mật rồi nhập", không gọi `-PDFIMPORT`; bỏ dòng `shortcut:`.
- `module/plugin/Rai.cs`: hàm `ChanTruc` (điểm gần nhất trên cả polyline). `TuyenXuongCa`: `ngoai` tính tới cả polyline, đoạn nối tới `ChanTruc`. `TuyenCot`: như vậy + gom đoạn nối cùng điểm nối cùng phía, giữ đoạn từ chân xa nhất.
  `LuoiVung` `theo_hang`: theo `s` mỗi hàng `nMin` = cột lưới × bề rộng hàng / W, ba lần thử như vòng `lan`, thêm thiết bị tới khi ô hàng ≤ `dt`; theo `buoc` giữ bước đúng; `k.O` (= `m2`) là ô lớn nhất của các hàng nên `rai` báo lỗi khi vượt `dt`.
- `test/tao_pdf_mau.py`: tham số thư mục thì tạo `khoa_sua.pdf` (chỉ mật khẩu chủ, AES-128, chỉ cho in), `mat_khau.pdf` (có mật khẩu mở), `hong.pdf` (không phải PDF).
- `test/chay_thu.ps1`: 7 phép kiểm mới, đầu vào đúng như review: `pdf_mk.json` (3 file, gói sau vẫn chạy), `rv_tuyen.json` (`cot` 2 cột cùng phía 6 m hai chiều `truc`; `xuong_ca` `truc` thẳng hàng 6 m 3 đoạn; `cot` `truc` thẳng hàng 2 đoạn 3 m),
  `hoa.json` + `hoa_doc.json` (2 cột đối xứng, `truc` hai chiều, `doc` hộp hẹp quanh cột trái ra 1 cả hai lần), `rv_hang.json` (`theo_hang` 10 × 6 m ra 6 như lưới chung; `buoc` + `dt` 12 báo lỗi, không `dt` thì `m2` 17,7).

Kiểm chứng: `chay_thu.ps1` 54/54 `TAT CA DAT`, `thu_mcp.ps1` 9/9 `TAT CA DAT`, `build.bat` OK (bundle 14:45). Không chạy `thu_gui.ps1` (AutoCAD người dùng đang mở).
Chưa kiểm: các phép kiểm mới chưa chạy đột biến ngược trên mã cũ (số "trước" lấy từ review đã chạy); `theo_hang` mới chưa dò vét cạn bằng Python như review (phòng chữ L/U/T, `chan`/`le` lớn); PDF mã hoá trên AutoCAD có giao diện.

## 2026-11-12 15:00 — chốt mốc Bước 5 (a–f), review bằng Opus max thay Fable
- Người dùng dặn 12/11: review, kiểm tra CAD dùng Opus effort max, không dùng Fable nữa (đã sửa `cad-review.md`, `cad/CLAUDE.md`). Duyệt 15 vùng tầng 1. Chuẩn layer, block động, catalog ống/bơm sẽ nạp qua lệnh plugin.
- Chuỗi: sửa review 11/11 (`7408077`) → c, d, e (`a2b65c0`) → review Opus max 1 CHẶN 5 NÊN SỬA → sửa (`1c20265`) → review lại hẹp 0 CHẶN 2 NÊN SỬA (`review_2026-11-12b.md`).
- Kiểm chứng: `chay_thu.ps1` 54/54, `thu_mcp.ps1` 9/9 (agent review chạy). Chưa kiểm: `thu_gui.ps1`, `pdf` trong acad.exe có giao diện, bản vẽ đơn vị m và UCS khác World sau lần sửa này.
- Hạn mức: 5 giờ 10 % → 82 % cho 5 lượt agent (sửa ~18 %, c/d/e ~13 %, review rộng ~26 %, sửa ~6 %, review hẹp ~9 %).

## 2026-11-12 — Sửa 2 NÊN SỬA + GHI CHÚ của review 12/11b
- `module/plugin/Rai.cs` `LuoiVung` (`theo_hang` theo `s`): hàng rộng bằng vùng lấy thẳng lưới chung (`k.Nx`, `k.Bx`, `k.Tx`, đúng cả vị trí); hàng hẹp bắt đầu từ 1 thiết bị, vòng `dt` tự thêm. Phòng 10 × 6 m khoét WC 3 × 3 m, `dt` 12: 5 đầu (trước 6).
- `Rai.cs` `TuyenCot`: chỉ gộp đoạn nối khi điểm nối T nằm trên đường trục (|fd(T)| ≤ 1e-6); ống chính bậc chữ Z: 6 đoạn 12,77 m (trước 5 đoạn 9,61 m, nhánh x 9000 treo); chữ L vẫn 5 đoạn 11 m.
- `skills/TU_VUNG.md` (bảng, phần đầu giữ 1893 ký tự): `theo_hang`, đoạn nối của `xuong_ca`, `cot`.
- Kiểm: 2 phép mới (`rv2`) trong `test/chay_thu.ps1` với đúng đầu vào review; `chay_thu.ps1`, `thu_mcp.ps1` TAT CA DAT; `build.bat` được.
- Chưa kiểm: không chạy mã cũ với phép kiểm mới (dựa số của review); GHI CHÚ vị trí phòng chữ nhật + `le` 650 chưa có phép kiểm riêng; `thu_gui.ps1` không chạy.

## 2026-11-12 (17:05) — `PCCC_RAI` hỏi `le`, `theo_hang`; thử HL1 `s` 3500
- `module/plugin/Rai.cs` (`RaiTay`): sau diện tích bảo vệ, `PCCC_RAI` hỏi thêm khoảng cách tới tường (m, Enter = mặc định cũ, chia đều tối đa S/2) và "Mỗi hàng chia lại..." [Co/Khong] (mặc định Khong);
  truyền xuống `LuoiVung` như thao tác spec `rai` (`le` cả hai phương, `theo_hang`).
- `test/chay_thu.ps1`: kịch bản `PCCC_RAI` cũ thêm hai Enter; thêm `PCCC_RAI` phòng 14000 × 10000, S 4, không dt, `le` 1,5: tính tay x n = 4, (14 − 3) / 3 = 3,667 làm tròn 3,65; y n = 3, (10 − 3) / 2 = 3,5
  ra `n=12 luoi=4x3 buoc=3650x3500` (không `le` thì 3500 × 3300).
- Thử HL1 `s` 3500: `ket_qua/cad_mau/cad/lan4/` (chép `t1_spr.bak`, spec = `lan3` chỉ đổi `s` HL1): `ok`, HL1 10 đầu (mẫu 10), bước lớn nhất 3416,8, cách tường 1599 / 1493; HL2 6, tổng 64 (mẫu 64);
  DN50 146,54 m. Ảnh `lan4/anh/t1_spr_2.png` (nửa trái HL1), đã xem: đầu phun đều dọc tim, không đè.
- Kiểm chứng: `chay_thu.ps1` TAT CA DAT (57 dòng DAT), `thu_mcp.ps1` TAT CA DAT, `build.bat` xong. Chưa kiểm: `thu_gui.ps1` (không chạy theo bài giao); lệch vị trí HL1 so với mẫu chưa đo lại.

## 2026-11-12 (17:45) — Bước 5: van OS&Y, công tắc dòng chảy, trục xả DN50, van xả khí (agent `cad-lap-trinh`)
- `data/vat_tu.json`: thêm `SPR_VAN_OSY_DN65`…`DN150` (theo DN ống chính, ký hiệu `OSY`) và `SPR_VAN_XK` (van xả khí, `AAV`). Công tắc dòng chảy dùng `SPR_CTDC` có sẵn,
  ống trục xả dùng `ONG_DN50` có sẵn. Block mặc định do plugin tự sinh vào `TKPCCC.bundle/Contents/PCCC_Blocks.dwg` (thêm 6 block) khi spec chạy lần đầu.
- Vị trí đọc từ bản mẫu `test_HA2.pdf` trang 3 (ảnh cắt vector PDF + chữ): 2 van OS&Y DN100 trên 2 trục đứng x 43184 / 43558, y 34409 (`goc` 90); 2 FS cạnh trục (42940 / 43805, y 34585);
  ống xả DN50 rẽ từ trục trái y 34798 qua x 42413 xuống y 34105 tới điểm trục xả (42890, 34105); AAV ở hai góc mạch vòng: (31990, 40124) và (48462, 48365).
  TỰ ĐẶT: ống tới AAV vẽ DN25 (mẫu không ghi DN); kéo dài 2 trục DN100 xuống y 32700 (chỗ "KẾT NỐI TRẠM BƠM"); điểm trục xả là vòng tròn r 60 trên `PCCC_NHAN`; nhãn chữ không dấu.
  Chưa vẽ: van xả trên ống xả và các van, đồng hồ ở cụm AAV (mẫu có, danh mục chưa có mã).
- Spec `ket_qua/cad_mau/cad/lan4/t1_van.json` chạy vào `lan4/t1_spr.dwg`: `ok`, vẽ 14, `dem` ONG_DN100 60,51 (+3,6), ONG_DN50 148,48 (+1,94), ONG_DN25 1,88, SPR_VAN_OSY_DN100 2,
  SPR_CTDC 2, SPR_VAN_XK 2, SPR_DAU_DOWN 64. Ảnh `lan4/anh/t1_van_1..3.png` đã xem: block trùng ký hiệu mẫu.
- `skills/TU_VUNG.md` (phần sau `---`, dòng `dat`): mã van sprinkler. `test/chay_thu.ps1`: `dat` `SPR_VAN_OSY_DN100` + `SPR_VAN_XK` ra mỗi mã 1 block.
- Kiểm chứng: `chay_thu.ps1` TAT CA DAT (58 dòng DAT), `thu_mcp.ps1` TAT CA DAT (9), `npm test` 57/57, `build.bat` xong. Chưa kiểm: `thu_gui.ps1`; block mặc định chưa vẽ hình van thật.

## 2026-11-12 18:00 — đo token một lần vẽ (Bước 5)
- `claude -p --output-format json` từ Bash của Claude: lỗi `Failed to authenticate: OAuth session expired and could not be refreshed` (đăng nhập CLI hết hạn trong shell này; runner web gọi `claude -p` có thể cũng hỏng tới khi người dùng đăng nhập lại `claude` trong Terminal).
- Đo thay bằng subagent Sonnet effort low, cùng việc người dùng hay giao: phòng 8000 × 6000, polyline `PCCC_VUNG`, `rai` SPR_DAU_DOWN `s` 4000 `dt` 12, `tuyen` `xuong_ca` ONG_DN100 có `nhan`, `dem`;
  ghi spec, chạy `chay_spec.ps1` (accoreconsole). Kết quả 4 đầu phun (2 × 2, bước 4000 × 3000), ống 12,5 m, đúng ngay lần đầu, không sửa spec.
- Số đo: 70 140 token (phần lớn là lời nhắc hệ thống + công cụ của subagent), 5 lượt công cụ, 19 giây. Cửa sổ 5 giờ Pro tăng khoảng 1–2 % (gộp cả lượt điều phối của phiên chính).
  Chưa đo qua MCP với AutoCAD có giao diện (agent `cad-ve`), vì `cad_chay` vẽ vào bản vẽ người dùng đang mở.


## 2026-11-12 — Sửa GHI CHÚ nhỏ của review 12/11c
- `Rai.cs` `TuyenCot`: điểm nối coi là trên đường trục khi lệch ≤ `saiSo` (trước 1e-6). Ống chính bậc 0,5 mm (vẽ tay lệch) nay gộp đoạn nối: 5 đoạn 8 m, không còn vẽ chồng (trước 6 đoạn 9 m).
- `chay_thu.ps1` thêm 3 phép kiểm: `cot` ống chính bậc 0,5 mm (5 đoạn 8 m); `theo_hang` hàng rộng bằng vùng (phòng 6 × 6 m, `le` 650, `dt` 12: 4 đầu, `m2` 9; bỏ dòng `w >= W` ra `m2` 12);
  `PCCC_RAI` trả lời `Co` (phòng chữ L 12 × 8 m khoét 3 × 3 m: `n=6 HO=1.1%`).
- Kiểm: `chay_thu.ps1` 61/61, `thu_mcp.ps1` 9/9 `TAT CA DAT`; `build.bat` OK. Chưa chạy `thu_gui.ps1`. Chưa thử phép kiểm mới trên DLL cũ (dựa theo số của review).
- Chưa sửa (chỉ ghi): `cot` nối hình quạt dư ống khi nhiều cột vượt đầu ống chính có bậc; hở sát góc khoét của `theo_hang` (`ho` = 0 chưa chắc phủ kín); DN trên block OS&Y; tài liệu cũ ở `TIEN_DO.md:172-174`, `Rai.cs:142`, `PCCC_HELP`.

## 2026-11-12 18:30 — G5 bước đầu: thao tác `so_do` (sơ đồ nguyên lý sprinkler), agent `cad-lap-trinh` (Opus)
- Bản mẫu `test_HA2.pdf` không có tờ sơ đồ nguyên lý, không có mặt đứng: lấy cụm van từ khung "CHI TIẾT: VAN CỔNG OS&Y + CÔNG TẮC DÒNG CHẢY" của tờ CC-07
  (2 trục DN100 + trục xả DN50 bên trái, nhánh OS&Y → FS → mũi tên, ống xả có van về trục xả, nét cầu chỗ ống vượt nhau); vạch tầng, cao độ, AAV đầu trục,
  van xả hình nơ, khoảng cách là TỰ ĐẶT. Bảng nguồn từng chi tiết: `knowledge/so-do-nguyen-ly.md`.
- Mã: `module/plugin/SoDo.cs` (`OpSoDo`, `MaSoDo`, `OngCau`, `ChuCaoDo`), 1 dòng `MotOp`; `ThucHienSpec` nạp block thư viện cho `so_do` như `dat`/`rai` (bản vẽ mới chưa có block thì trước đây báo "Thư viện không có block" và vẽ dở 6 đối tượng).
  Kiểm hết đầu vào (mã ống, van OS&Y cùng DN, block có trong bản vẽ) trước khi vẽ: lỗi thì không để lại hình dở.
  Ống + chữ sơ đồ ở layer `PCCC_SODO`, vạch tầng `PCCC_SODO_TANG`: `dem`/`PCCC_BOC` không cộng chiều dài ống sơ đồ; van có `MA_VT` nên vẫn đếm.
- `TU_VUNG.md`: dòng `so_do` ở phần đầu (rút gọn dòng `luoi_truc`, `tuyen`, `anh`, `pdf` để còn 1895 / 1900 ký tự) + dòng chi tiết; ví dụ `skills/vi_du/so_do.json`; README 3 chỗ; skill `tkpccc-ve-cad` 1 dòng.
- `chay_thu.ps1` thêm 2 phép kiểm: `so_do` 2 trục, 3 vạch tầng (2 tầng có `dem`) ra 4 OS&Y DN100 + 4 FS + 2 AAV, `ve` 48, chiều dài ống DN50 không đổi;
  chữ cao độ `±0.000`, `+4.200`, `+8.400` + 3 tên tầng; trục `ONG_DN25` (không có van OS&Y DN25) báo lỗi, không vẽ.
- Thử: `ket_qua/cad_mau/cad/so_do/so_do_spr.json` (tầng 1 64 đầu phun theo `lan4`; tầng 2 GIẢ ĐỊNH 64 như tầng 1, cao độ 4,2 / 8,4 m TỰ ĐẶT vì mẫu không có) →
  `so_do_spr.dwg`, ảnh `anh/so_do_spr_1.png` đã xem: trục, nhánh, van, nét cầu, chữ đúng chỗ.
- Kiểm: `chay_thu.ps1` 63/63, `thu_mcp.ps1` TAT CA DAT; `build.bat` OK. Chưa chạy `thu_gui.ps1`; chưa thử qua MCP trên AutoCAD có giao diện.
- Còn của G5: sơ đồ báo cháy (`he:"bc"`), vách tường / trạm bơm; mã van xả; cao độ đọc tự động từ mặt đứng.
