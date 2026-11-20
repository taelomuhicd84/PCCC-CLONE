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

## 2026-11-12 19:00 — G5: chống đếm trùng thiết bị sơ đồ + `so_do` `he:"bc"` (báo cháy hệ thường), agent `cad-lap-trinh` (Opus)
- Chống đếm trùng: block van, thiết bị của sơ đồ đặt lên layer sơ đồ (`DatSoDo` đổi layer sau `ChenThietBi`); `Duyet` (`Lenh.cs`, dùng cho `dem` và `PCCC_BOC`)
  bỏ qua mọi đối tượng trên layer bắt đầu bằng `PCCC_SODO`. Thay quyết định trước (van sơ đồ được đếm): số lượng thật chỉ lấy từ mặt bằng.
- `he:"bc"` (`SoDoBaoChay`, `VeVachTang` dùng chung với `spr`): tủ (khung + `BC_TT`) trên tầng thấp nhất, mỗi zone một dây riêng từ tủ lên tầng rồi ngang qua
  các cột thiết bị (`x<số>`), cuối dây điện trở cuối dây, dây vượt nhau nét cầu, chú thích cột dưới vạch tầng. `toi_da` (đầu báo / zone) bắt buộc, truyền từ spec;
  zone vượt thì báo lỗi. Bản mẫu không có tờ sơ đồ báo cháy: bố trí TỰ ĐẶT, bảng nguồn ở `knowledge/so-do-nguyen-ly.md`.
- Mã vật tư mới `BC_DTCD` (điện trở cuối dây, ký hiệu `R` vì txt.shx không có Ω) trong `data/vat_tu.json`; `PCCC_Blocks.dwg` tự thêm block mặc định.
- `TU_VUNG.md` (đầu 1891 / 1900 ký tự, bỏ câu "chỉ đọc ảnh khi cần" ở dòng `anh`), ví dụ `skills/vi_du/so_do_bc.json`, `rule/LUAT.md` BC-04, BC-05.
- `chay_thu.ps1`: `van.json` đặt 2 OS&Y; phép kiểm `so_do` spr đổi thành "mặt bằng 2 OS&Y + sơ đồ 2 tầng × 2 trục: `dem` OS&Y vẫn 2, không FS";
  thêm 2 phép kiểm `bc`: ví dụ 3 zone `ve` 47, `dem` không đổi; zone 26 đầu báo > `toi_da` 25 và thiếu `toi_da` báo lỗi, không vẽ.
- Thử: `ket_qua/cad_mau/cad/so_do/so_do_bc.json` (2 tầng + mái, 3 zone, SỐ ĐẾM GIẢ ĐỊNH) → `so_do_bc.dwg`, ảnh `anh/so_do_bc_1.png` đã xem
  (lần đầu nhãn zone đè chữ `x18` và zone viết tắt `dem` lấy nhầm tên tầng: đã sửa).
- Kiểm: `chay_thu.ps1` 65/65, `thu_mcp.ps1` TAT CA DAT; `build.bat` OK. Chưa chạy `thu_gui.ps1`; chưa thử qua MCP trên AutoCAD có giao diện.
- Còn của G5: báo cháy hệ địa chỉ (ZM, CM, RM, mạch vòng), đèn báo phòng, dây nét đứt; vách tường / trạm bơm; mã van xả; cao độ đọc từ mặt đứng.

## 2026-11-12 — Sửa 4 NÊN SỬA review 12/11d (`so_do`)
- `SoDo.cs`: cỡ chữ mặc định nhân `k` (bản vẽ đơn vị m); `spr` chỉ ghi mã hệ `SPR`, mã lạ hoặc số ≤ 0 báo lỗi, chữ đếm cao hơn `h` báo lỗi;
  `bc` bỏ qua mã dây/ống (`DAY_BC`) trong `dem`, zone không còn thiết bị báo lỗi; `bc` tự tăng `h` theo số zone nhiều nhất một tầng
  (dây cách nhau ≥ 700 mm, dây zone 1 không trùng cạnh khung tủ).
- `chay_thu.ps1` thêm 5 phép kiểm dùng đúng đầu vào review (`so_do_loc`, `so_do_bc2`, `so_do_m` INSUNITS 6): 70/70; `thu_mcp.ps1` 9/9; `build.bat` OK.
- Chưa làm: `MaSoDo` vẫn đưa `DAY_BC` vào danh sách nạp block (gọi `NapVaoBanVe` thừa); GHI CHÚ 1–6 để lại.

## 2026-11-12 — `so_do` bc theo bố cục hồ sơ thật (trang 11)
- `SoDo.cs`: bc bỏ vạch tầng + cao độ; tủ dưới, zone xếp hàng từ dưới lên, bó dây trái; số lượng DƯỚI ký hiệu; `zone` truyền thẳng (`kv`, `dt` → "DIỆN TÍCH BẢO VỆ"), `du_phong`, `lien_dong` (mũi tên từ đáy tủ); zone giám sát van dùng mã van trong `dem`. `tang` cũ vẫn nhận, `cot` không bắt buộc cho bc. `spr` giữ nguyên.
- Ví dụ `skills/vi_du/so_do_bc.json` đổi sang dạng mới; `chay_thu.ps1` thay 2 phép kiểm bố cục cũ bằng 3 phép mới (thứ tự zone, số dưới ký hiệu, diện tích/liên động/không cao độ): 71/71; `thu_mcp.ps1` TAT CA DAT; `build.bat` OK.
- Thử: `ket_qua/cad_mau/cad/so_do/so_do_bc_2.json` → `.dwg`, ảnh `anh/so_do_bc_2.png` đã xem.
- Còn: bảng ký hiệu riêng, bảng tính ắc quy (phụ lục C TCVN 7568-14), nguồn 220 V/24 V + tiếp địa + điện thoại, cột còi đèn riêng, liên động đi ngang như bản mẫu; số lượng ghi "2" (bản mẫu ghi "02").

## 2026-11-13 — so_do bc: nhãn cuối zone 3 dòng
Người dùng dặn chữ cuối mỗi zone (cạnh điện trở cuối dây) đúng 3 dòng: khu vực / `DIỆN TÍCH BẢO VỆ: xx m²` / `ZONE n` (vd `TẦNG 1 KARAOKE` / `DIỆN TÍCH BẢO VỆ: 88.11 m²` / `ZONE 1`).
- `module/plugin/SoDo.cs`: khu vực = `kv`; zone trong tầng có tên mà `kv` chưa chứa tên tầng thì ghép "tên tầng + kv"; không `kv` thì lấy tên tầng. Thiếu `dt` bỏ dòng diện tích. Diện tích ghi dấu chấm thập phân như bản gốc (không dùng `So` kiểu VN). Dòng zone luôn có.
- `test/chay_thu.ps1`: `so_do_bc2` đổi tầng B1 thành tầng `T1` 2 zone (`KARAOKE` dt 88.11; `T1 KHO` không dt) + 1 `doc` riêng; phép kiểm mới đọc lại đúng 3 dòng / 2 dòng; các phép kiểm cũ lọc tên zone ở bó dây theo x.
- `knowledge/so-do-nguyen-ly.md`: sửa mô tả nhãn.
Kiểm chứng: `chay_thu.ps1` TAT CA DAT (72 DAT), `thu_mcp.ps1` TAT CA DAT (9), `build.bat` OK.
Bỏ theo lệnh điều phối (sẽ làm lại toàn bộ sơ đồ bc giống 100 % trang 11, ký hiệu lấy từ nét PDF): liên động ngang, nguồn 220 V / 24 V / tiếp địa, cột còi đèn, bảng ký hiệu; chưa sửa `vi_du/so_do_bc.json`, `TU_VUNG.md`, chưa vẽ lại ảnh `so_do_bc_3.png`. Bảng tính ắc quy: việc sau (thiếu dòng tĩnh / dòng báo cháy từng thiết bị).

## 2026-11-13 — so_do bc giống quy cách trang 11 hồ sơ thật (agent cad-lap-trinh)
- `module/plugin/SoDo.cs`: viết lại `SoDoBaoChay` theo toạ độ pt của trang 11 (S = 33 mm/pt): bảng ký hiệu 2 cặp cột (chỉ mã có trên sơ đồ + các loại dây, mẫu nét in bằng chính layer dây),
  `*GHI CHÚ`, bảng tính nguồn phụ lục C TCVN 7568-14:2025 (`VeBangNguon`, mọi hệ số từ spec `nguon`, kết quả trả `nguon` trong kq) + "Kết luận"; tủ hình hộp có hàng đèn zone + "TỦ TRUNG TÂM BÁO CHÁY n ZONE",
  điện thoại, tiếp địa, nguồn 220 V / 24 V; bó dây đứng (zone 1 phải, không dây nào cắt nhau, góc lượn) + elip + chú thích; hàng zone: nút ấn, đầu báo, thiết bị chỉ thị nâng lên nối xiên (HL lệch trái),
  số DƯỚI ký hiệu, Ω + nhãn khu vực / DIỆN TÍCH BẢO VỆ / ZONE n : dt M²; zone giám sát van (OS&Y, FS, van báo động + chữ mô tả); cột còi đèn bên phải; dự phòng mũi tên trái; liên động ngang từ cạnh tủ; tiêu đề gạch chân.
  Dây ngắt qua ký hiệu. Khoá spec mới: `day_truc`, `day_coi`, `day_nguon`, zone `day`/`ghi`, `nguon`, `ghi_chu`, `ghi_tu`, `dien_thoai`, `nguon_chinh`, `nguon_du_phong`, `tieu_de`, `zone_tu`, `ten_tb`.
- `Lenh.cs`: `NapBlock` chỉ chép mã bản vẽ CHƯA có (block mang mã trong bản vẽ giữ nguyên), riêng `PCCC_LIB` vẫn cập nhật; block mặc định vẽ theo `hinh_bc.json`; thư viện còn block mặc định cũ (vòng R200 + chữ cỡ 90/140) thì vẽ lại.
  `VatTu` thêm `layer_so_do`, `net`, `mau`.
- `data/vat_tu.json`: mã mới `BC_CHI_THI`, `BC_CHI_THI_HL`, `BC_COI_DEN`, `BC_DIEN_THOAI`, `DAY_BC_10`, `DAY_BC_10_FR`, `DAY_NGUON_15`, `DAY_NGUON_15_FR` (dây có layer/nét/màu mặc định như trang 11).
- `data/ky_hieu/hinh_bc.json` sinh bằng `.claude/skills/tkpccc-cad-console/scripts/hinh_tu_pdf.py` từ nét vector bảng ký hiệu trang 11; `build.bat`, `chay_thu.ps1` chép file này.
  Đã chạy `PCCC_LIB` trên bundle: `PCCC_Blocks.dwg` thêm 4 mã mới + vẽ lại 11 block mặc định cũ (BC_NHIET, BC_KHOI, BC_NUTAN, BC_DTCD, SPR_CTDC, SPR_VAN_BD, SPR_VAN_OSY_DN*); BC_TT giữ. Bản trước: chưa lưu trong repo (git có bản cũ).
- `skills/vi_du/so_do_bc.json` dựng lại trang 11 (12 zone + dự phòng 13–15, bảng tính `sl` như trang 11 vì trang 11 tính "trên 1 tầng", không khớp số trên sơ đồ).
- `test/chay_thu.ps1`: thay các phép kiểm bc cũ; mới: bảng tính đúng trang 11 (tính tay ghi trong script), chữ số trong bảng, mỗi dây đúng layer, block BC_COI_DEN + layer dây nguồn có sẵn không bị ghi đè (LISP đọc bảng block/layer), nhãn, thứ tự zone, dự phòng, giám sát van, liên động ngang.
- Tài liệu: `knowledge/so-do-nguyen-ly.md` (mục bố cục mới, cái gì tự đặt), `knowledge/mau-so-do-that.md`, `skills/TU_VUNG.md` (đầu 1887 / 1900), `README.md`, `data/README.md`.
Kiểm chứng: `chay_thu.ps1` 75 DAT TAT CA DAT, `thu_mcp.ps1` 9 DAT TAT CA DAT, `build.bat` OK; ảnh `ket_qua/cad_mau/cad/so_do/anh/so_do_bc_3.png` đã đặt cạnh `trang11.png` so từng phần.
Chưa kiểm: `thu_gui.ps1` (không chạy theo lệnh); chưa mở trong AutoCAD có giao diện; chưa review `cad-review`.
Còn khác trang 11: màu theo layer (trang 11 đen trắng), tủ vẽ bằng nét không phải block, vị trí ký hiệu theo cột cố định, thiếu dòng "S trên trần" trong bảng ký hiệu, chú thích bó dây lấy tên dây `day_truc`.

## 2026-11-13 — so_do bc: nhãn zone đúng 3 trường; kiểm kiểu chữ (agent cad-lap-trinh)
- `SoDo.cs`: nhãn cuối zone về đúng lời dặn: khu vực (ghép tên tầng) / `DIỆN TÍCH BẢO VỆ: 88.11 M²` / `ZONE 1`; thiếu `dt` bỏ dòng giữa.
  Cỡ chữ nhãn 4.5 → 4.0, tâm lùi trái (xOm + 8): dòng diện tích dài hơn trước, ở 4.5 chạm cột còi đèn.
- `skills/vi_du/so_do_bc.json`: `kv` có tầng, lấy từ chữ "Zone n" trên mặt bằng báo cháy hồ sơ thật (`cong_trinh.pdf`, đọc chữ PyMuPDF):
  trang 4/5 (dưới trần T1) zone 1, 2, 3, 8; trang 6 (trên trần T1) zone 4; trang 7 (dưới trần T2) zone 5, 6, 7 ("với zone 8 tại tầng 1"); trang 8 (trên trần T2) zone 9.
  ⇒ `TẦNG 1 KARAOKE`: 1, 2, 3, 4, 8 (zone 8 là sảnh thông tầng, về tủ từ T1, T2 nối vào); `TẦNG 2 KARAOKE`: 5, 6, 7, 9.
- Chữ mất dấu trong ảnh: KHÔNG do kiểu chữ. Phép kiểm mới (LISP `ssget` MTEXT/TEXT layer `PCCC_SODO*`) cho 262/262 chữ dùng `PCCC_ARIAL`.
  Nguyên nhân: ảnh PNG tối đa 1600 px (`PublishToWeb PNG.pc3`), chữ cao 4–5 đơn vị chỉ vài pixel nên dấu trên chữ in hoa bị nuốt. Ảnh cắt vùng
  (`anh/so_do_bc_3_bang.png`, `anh/so_do_bc_3_zone.png`) hiện đủ dấu. Xem dấu thì in ảnh theo `hop` nhỏ.
- `test/chay_thu.ps1`: sửa phép kiểm nhãn zone; thêm phép kiểm kiểu chữ sơ đồ.
Kiểm chứng: `chay_thu.ps1` 76 DAT TAT CA DAT, `thu_mcp.ps1` TAT CA DAT, `build.bat` OK; ảnh `so_do_bc_3.png` vẽ lại (spec `ket_qua/cad_mau/cad/so_do/so_do_bc_3.json` có 3 thao tác `anh`).
Chưa kiểm: `thu_gui.ps1`; chưa đối chiếu lại chi tiết từng phần với `trang11.png` ngoài vùng nhãn zone.

## 2026-11-13 — sửa theo review 13/11 (cad-lap-trinh)
- `SoDo.cs` NÊN SỬA 1: thiết bị chỉ thị HL lệch (−25, +25,6) pt so với tâm đầu báo như trang 11 (trước −50, +18): số "20" zone 4, 9 cách dây zone 9,5 pt (trước 0,5).
  Hộp HL bỏ phần chữ "HL" lệch phải khi tính: số nằm giữa dưới khung, dây nối tới góc phải dưới khung.
- NÊN SỬA 2: `nguon` `tq`, `ta`, `fc`, `hs`, `tq_nap`, `t_nap` ≤ 0 hoặc `i_tu` < 0 báo lỗi nêu tên khoá; `tb` `iq`, `ia`, `sl` < 0 báo lỗi. Kiểm lúc đọc đầu vào, chưa vẽ gì.
- NÊN SỬA 3: `zone_tu` nhỏ hơn số zone trên sơ đồ báo lỗi (kiểm trước khi vẽ).
- Khác trang 11 mục 5, 6: `TlKyHieu` `BC_CHI_THI`, `BC_CHI_THI_HL` = 1,47 (trước 1,75); dây đầu báo → thiết bị chỉ thị vẽ trên layer dây của zone (nét đứt như dây zone), không còn trên layer chữ.
- `test/chay_thu.ps1`: +2 phép kiểm (5 lỗi đầu vào đúng như review: hs −1,25 / hs 0 / tq −24 / sl −59 / zone_tu 5 < 12; khoảng cách số HL tới dây zone 4, 9 ≥ 3 pt).
- Ảnh `ket_qua/cad_mau/cad/so_do/anh/so_do_bc_3.png`, `_bang.png`, `_zone.png` vẽ lại.
Kiểm chứng: `chay_thu.ps1` 78 DAT TAT CA DAT, `thu_mcp.ps1` TAT CA DAT, `build.bat` OK. Chưa chạy `thu_gui.ps1`. Chưa commit.
CÒN (chưa làm, hết hạn mức):
- Bảng ký hiệu thiếu dòng "ĐẦU BÁO KHÓI KIỂU ĐIỂM TRÊN TRẦN" (S khung vuông zone 4, 9): cần mã mới trong `data/vat_tu.json` + hình từ nét PDF (`hinh_tu_pdf.py`, đo khung 21 × 21 pt trang 11) + sửa `vi_du/so_do_bc.json` zone 4, 9 + `THU_TU_KY_HIEU`.
- Ảnh zone (đã xem): chữ "HL" của thiết bị chỉ thị zone 4 chạm số "32" zone 5 vì hàng zone cách đều 45,45 pt; trang 11 zone 4→5 cách xa hơn (mục Bố cục 3 của review).
## 2026-11-13 — so_do bc: giãn hàng zone theo nội dung (việc kế 2)
- `cad/module/plugin/SoDo.cs`: khoảng hàng zone không còn cách đều; zone có thiết bị chỉ thị HL (`BC_CHI_THI_HL`, lệch trái) cách hàng trên tối thiểu 60 pt, hàng khác giữ `dy`. Chữ "HL" zone 4, 9 hết chạm số "32"/số hàng trên (như trang 11).
- `cad/test/chay_thu.ps1`: phép kiểm mới "gian hang theo noi dung" (ký hiệu HL zone 4 cách đáy số hàng trên >= 2 pt; đo được 4,5 pt). Chỉ kiểm zone 4 vì `doc chu` trả tối đa 40 chữ, không tới nhãn "ZONE 10".
- Ảnh `ket_qua/cad_mau/cad/so_do/anh/so_do_bc_3.png`, `_bang.png`, `_zone.png` vẽ lại (accoreconsole, spec `so_do_bc_3.json`).
- Kiểm chứng: `chay_thu.ps1` TAT CA DAT, `thu_mcp.ps1` TAT CA DAT, `build.bat` OK.
- CHƯA LÀM (hết hạn mức): việc kế (1) dòng ký hiệu "ĐẦU BÁO KHÓI KIỂU ĐIỂM TRÊN TRẦN" (mã mới trong `data/vat_tu.json`, hình từ PDF vào `cad/data/ky_hieu/hinh_bc.json`, `vi_du/so_do_bc.json` zone 4, 9 dùng mã đó).

## 2026-11-13 — so_do bc: dòng ký hiệu "ĐẦU BÁO KHÓI KIỂU ĐIỂM TRÊN TRẦN" (việc kế 1)
- `data/vat_tu.json`: mã mới `BC_KHOI_TRAN` ("Đầu báo khói kiểu điểm trên trần", ký hiệu S, cùng kiểu dòng `BC_KHOI`).
- `hinh_tu_pdf.py`: hộp `BC_KHOI_TRAN` (55, 67, 75, 86) pt trang 11 (khung vuông 12 pt + vòng tròn + chữ S), gốc tâm vòng tròn; chạy lại ra `cad/data/ky_hieu/hinh_bc.json` (các mã cũ không đổi).
- `SoDo.cs`: `THU_TU_KY_HIEU` thêm `BC_KHOI_TRAN` sau `BC_NHIET` (thứ tự như trang 11). Tỉ lệ ký hiệu dùng mặc định 1,75 như đầu báo.
- `cad/skills/vi_du/so_do_bc.json`: zone 4, 9 dùng `BC_KHOI_TRAN` + `ten_tb`. Không dùng `PCCC_LIB`: block chưa có trong thư viện thì plugin tự tạo từ `hinh_bc.json`.
- `cad/test/chay_thu.ps1`: thêm `doc chu` cột trái bảng ký hiệu và phép kiểm "bang ky hieu co dong DAU BAO KHOI KIEU DIEM TREN TRAN".
- Ảnh `ket_qua/cad_mau/cad/so_do/anh/so_do_bc_3.png`, `_bang.png`, `_zone.png` vẽ lại (spec `so_do_bc_3.json` lấy op so_do từ ví dụ mới).
- Kiểm chứng: `build.bat` OK, `chay_thu.ps1` TAT CA DAT, `thu_mcp.ps1` TAT CA DAT. Chưa chạy `npm test` (vat_tu.json thêm 1 dòng).

- 2026-11-13: so_do bc bang tinh nguon: cot dau (THIET BI) rong 1,7 lan cot so nhu trang 11 (x1 tinh theo so thiet bi); chay_thu TAT CA DAT.

## 2026-11-13 cuối phiên — người dùng test sơ bộ trên AutoCAD, ghi nhận 4 việc
- Người dùng tự test (NETLOAD, `PCCC_RAI`, panel `PCCC_BLOCK`), gửi 4 ảnh (`ket_qua/cad_test/2026-11-13/`). Ghi 4 việc vào đầu `cad/TIEN_DO.md`: rải sai cách tường (6771 > 5250 mm)
  và tự lấy khoảng cách theo tiêu chuẩn; tự nhận diện trục/tường, click vùng để rải; làm lại giao diện panel Block; `PCCC_HELP` thành panel có sửa phím tắt.
  Dặn: chỉ ghi nhận, làm song song với việc dở ở phiên reset 5 giờ kế tiếp. Lệnh tắt máy hẹn trước đó đã huỷ để người dùng test.

## 2026-11-13 (chiều) — 4 việc người dùng test 13/11, làm song song 3 phiên
Người dùng bảo chạy song song dù hạn mức tuần 89 %. 3 phiên, mỗi phiên một worktree: việc 1 (`rai` phủ kín vùng lồi lõm, trị số TCVN 7568-14 từ `data/khoang_cach_rai.json`, dim chéo góc),
việc 2 (`NhanVung.cs`, thao tác `vung_tu_dong`, `PCCC_RAI` bấm điểm không cần polyline), việc 3+4 (panel `PCCC_BLOCK` mới, `PCCC_HELP` thành panel sửa phím tắt lưu `phim_tat.txt`).
Gộp cả 3 vào `main`: xung đột `TU_VUNG.md` (giữ dòng `rai` của việc 1, thêm dòng `vung_tu_dong`), `chay_thu.ps1` (phép kiểm việc 2 dời gốc x 2500000 → 2600000 vì trùng chỗ hình việc 1).
Sau gộp: build đạt, `chay_thu.ps1`, `thu_mcp.ps1` TAT CA DAT. Chi tiết từng việc: `cad/tien_do/viec*.md`.

## 2026-11-14 — quy trình vẽ 6 bước, palette PCCC, bản quyền, 3 giao diện, ảnh block nền đen
Người dùng giao (14/11): quy trình vẽ từ PDF Trục → Tường → Mặt bằng chi tiết → Thiết bị PCCC → Dây, ống → Sơ đồ nguyên lý; tham khảo file
`PROMPT_HOP_NHAT_PLUGIN_PCCC_CAD.md` (palette 8 phân hệ, ADIM); ảnh block nền đen đúng màu; bản quyền + app tạo key + Google Sheet tự cấp key; giao diện tối/sáng/Claude.
Kế hoạch + hợp đồng key: `cad/tien_do/viec_14_11.md`. 3 agent Opus song song trong worktree (A high, B, C medium), phiên chính gộp vào `main`.
- Gói A (`3234ad5`): `KienTruc.cs` (op `tuong`, `cua`, lệnh `PCCC_TRUC`, `PCCC_TUONG`, `PCCC_CUA`), `Dim.cs` (op `kt_chuoi`, `PCCC_ADIM`, `PCCC_CDIM`, gom điểm trùng hàng 10 mm),
  `layer/kien_truc.md`, nhóm `kien_truc` trong `ky_hieu.json`; skill `tkpccc-ve-cad` mục "Quy trình 6 bước"; agent `cad-thiet-ke`/`cad-ve`/`cad-kiem` theo từng bước. 6 phép kiểm mới.
- Gói B (`dbb2e7f`): `ui/BangChinh.cs` palette `PCCC` (Quy trình vẽ · Thư viện · Lệnh · Bot chat · Cài đặt); `PCCC_BLOCK`/`PCCC_HELP` mở palette này;
  `GiaoDien.DatChuDe` 3 kiểu lưu `%APPDATA%\TKPCCC\cai_dat.json`, đổi tại chỗ; bot chat gọi `claude -p` stream-json chạy nền, `--resume`; ảnh block nền đen, màu ByLayer/ByBlock/ACI/true color.
- Gói C (`0dc394c`): `BanQuyen.cs`, `ui/BanQuyenUi.cs` (`PCCC_BANQUYEN`), `module/banquyen/` (KeyGen.exe, Apps Script `Code.gs`, `HUONG_DAN.md`), `build.bat phat_hanh` chặn lệnh, `test/thu_ban_quyen.ps1` 16 phép.
- Sau gộp: thêm 8 dòng `DS_LENH` + 7 `<Command>` trong `PackageContents.xml`; `cad-ve` sang Haiku (phối hợp model, skill `tkpccc-phoi-hop-model` ở gốc).
  Build 4 file đạt; `chay_thu.ps1`, `thu_mcp.ps1`, `thu_ban_quyen.ps1` TAT CA DAT; pytest 13/13; `npm test` 401/401. Usage 5 giờ 57 % lúc ghi.
- 10:40: người dùng đóng AutoCAD, chạy `thu_gui.ps1` trên AutoCAD thật: 12/12 sau 2 sửa (kịch bản đếm sót lệnh `PCCC`; palette hẹp 400 px: `GiaoDien.KhungHanhDong`
  tự dời khung Hành động xuống đáy khi rộng < 640, `BangBlock` ẩn cột Đơn vị + cột SL hẹp, `BangLenh` ẩn cột nút chạy + Cách dùng). Đã xem ảnh 2 phân hệ.

## 2026-11-14 — Bảng chọn thiết bị khi gõ `?`
Người dùng (ảnh PCCC_RAI): gõ `?` thì hiện bảng danh sách, không in ra dòng lệnh.
- `plugin/Lenh.cs`: delegate `Lenh.ChonVatTu` (UI gán, cùng kiểu `MoBangLenh`); `HoiMa` gõ `?` hoặc chữ khớp nhiều mã gọi `XemDanhSach`: có UI thì cập nhật ảnh block
  rồi mở bảng (lọc sẵn chữ đã gõ), Esc thì hỏi lại; accoreconsole vẫn in dòng lệnh như cũ. `BoDau`, `ThuMucAnh` thành public cho UI dùng.
- `ui/BangChonVatTu.cs` (mới): Form modal `AcApp.ShowModalDialog`, ô tìm không dấu, cột ký hiệu nền đen (ẩn khi là ống/dây), tên đậm + mã xám, đơn vị; lên/xuống, Enter, Esc.
- `chay_thu.ps1`, `thu_mcp.ps1` TAT CA DAT; `build.bat` vào bundle đạt. Chưa chạy `thu_gui.ps1` (AutoCAD người dùng đang mở).

## 2026-11-14 chiều — PCCC_RAI treo CAD: đổi sang quét chọn mặt bằng; đổi model
- Người dùng báo (ảnh, `PCCC HOÁ CHẤT.dwg`): bấm điểm trong kho ở `PCCC_RAI` thì AutoCAD đứng. Nguyên nhân: `VungBam` đọc mọi đối tượng model space, explode mọi block,
  nới ô dò gấp đôi tới cả bản vẽ, `MatChua` O(m²), rồi rơi xuống `Editor.TraceBoundary` cả bản vẽ.
- Sửa (người dùng chốt): `PCCC_RAI` hỏi mã, trần, theo hàng, rồi QUÉT CHỌN mặt bằng; `MatChon` (`NhanVung.cs`) dựng đoạn chỉ từ đối tượng đã chọn, tính mọi mặt kín MỘT lần (`CacMat`,
  `MatChua` giờ gọi `CacMat` + `BoThangHang`); polyline kín layer `PCCC_VUNG` trong vùng chọn dùng thẳng; "Bấm điểm trong phòng cần rải, Enter = mọi phòng [TatCa]":
  Enter = mọi phòng (`PhongThat`: bỏ lòng tường 2A/P < 0,5 m hoặc < 1 m², bỏ mặt bao ngoài có > 50 % diện tích là phòng khác), bấm điểm = từng phòng; vẽ vùng `PCCC_VUNG` rồi rải.
  Bỏ `VungBam`, bỏ `TraceBoundary`, bỏ từ khoá `Doituong`. Test: bỏ đáp 'D' (4 chỗ `chay_thu.ps1`, 1 chỗ `thu_gui.ps1`); phép kiểm bấm điểm dùng bản sao mặt bằng ở gốc 2700000 + khung chọn C.
- Đổi model (người dùng dặn): `cad-ve` Haiku → Sonnet; Gemini mặc định `gemini-4-argon-medium` (bỏ 3.8 Flash), việc khó Carbon/Hydrogen (`agy models`).
- Dừng ở 90 % hạn mức 5 giờ. Kết quả `chay_thu.ps1` lần đầu sau sửa: xem `cad/TIEN_DO.md`.
- 11:10: thử bảng chọn trên AutoCAD thật bằng `thu_gui.ps1` (gửi `PCCC_RAI ?` qua COM trong job): 3 lần LOI do COM chập chờn; script gỡ lỗi riêng thấy bảng mở đúng.
  Phiên khác commit `d20e5a9` (làm lại PCCC_RAI) gộp luôn phần thử dở của `thu_gui.ps1`. Dừng ở 93 % hạn mức 5 giờ, việc kế ghi `cad/TIEN_DO.md` mục 6.
- 14/11 chiều (sau reset): `thu_gui.ps1` bỏ COM + `Start-Job`, phép kiểm bảng chọn chạy bằng kịch bản khởi động `PCCC_DAT ?` (kiểm ngay sau `song.json`, lọc không dấu, bấm Chọn, `dem` BC_KHOI = 6).
  Chưa chạy vì AutoCAD người dùng đang mở. `thu_mcp.ps1` TAT CA DAT; `chay_thu.ps1` không build do `NhanVung.cs` của phiên khác đang sửa dở.
- 14/11 sau reset: lỗi test cũ do accoreconsole không có màn hình, khung chọn `C` ra "0 found" ⇒ test `ZOOM W` trước. Người dùng gửi thêm bản vẽ cả tờ là MỘT block,
  rải tràn cả tờ: thêm bước bấm NÉT PHÂN VÙNG MẪU (`GetNestedEntity`, bấm được nét trong block; khoá layer + màu thật, ByLayer/ByBlock quy ra màu; `ThuNet` thêm `chaMau`,
  lọc theo mẫu, bỏ hatch, chỉ nét liền). `PhongThat`: ngưỡng 2A/P ≥ 1 m, bỏ mặt chứa phòng khác (thay luật > 50 %). Test thêm 2 phép kiểm; `chay_thu.ps1` 96/96,
  `thu_mcp.ps1` đạt, build vào bundle. Song song: phiên khác bỏ phép thử `PCCC_RAI` qua COM trong `thu_gui.ps1` (commit `ef58af6`).
- 14:35: `thu_gui.ps1` 19/19 trên AutoCAD thật. Hộp thoại modal mở từ kịch bản không có trong cây UIA `RootElement` (thấy qua `EnumWindows`): test tìm bảng bằng `WinU.TimCuaSo`,
  điền ô tìm `WM_SETTEXT`, bấm Chọn `BM_CLICK`. `PHX` dời cuối kịch bản (phím tắt gửi `PCCC_HELP` bất đồng bộ, bị `PCCC_DAT` chen). `chay_thu.ps1` 96/96, `thu_mcp.ps1` đạt.

## 2026-11-14 tối — Chép ý hay từ video addin mẫu ("video addin cad.mp4")
Người dùng gửi video 3 phút một addin PCCC CAD khác, dặn "cái gì hay thì copy, phát triển, kết hợp và tối ưu". Tách khung bằng ffmpeg của `imageio_ffmpeg` (máy không có ffmpeg).
Video có: lưới tiện ích nhóm 1–6 có phím tắt; trang Báo cháy "Bố trí tự động" (preset, tiêu chuẩn, mặt bằng, thiết bị SMK/HEAT/BEAM/MCP/BELL, mức tối ưu k, vật cản trần,
BỐ TRÍ, hoàn tác, vẽ loop, kiểm tra, báo cáo); Dim đối tượng 5 bước. Dim và phím tắt mình đã có (`PCCC_ADIM`, `PCCC_HELP`), nên làm:
- `Rai.cs` `RaiTay`: dấu nhắc trần thêm từ khoá `HeSo` (k 50–100 %): bước và cách tường tối đa nhân k; `KetQuaRaiCuoi` tóm tắt lượt rải cho palette.
- `module/ui/BangBoTri.cs` (mới) + `PCCC_BOTRI`: phân hệ Bố trí BC (đọc `khoang_cach_rai.json` cạnh DLL, nhớ lựa chọn trong `cai_dat.json` khoá `bt_*`).
- `BangChinh.cs`: nút dọc "Bố trí BC", thẻ bước 4 có huy hiệu mở trang; huy hiệu lệnh kèm phím tắt, tìm theo phím tắt.
- Test: `chay_thu.ps1` thêm `HeSo 50`; `thu_gui.ps1` chép `khoang_cach_rai.json`, gõ `PCCC_BOTRI`, đếm thiết bị trên trang.
- Đợt 1 tiếp (người dùng: "cứ làm hết", chỉ dùng file spec .md đã train, không tra TCVN): `PCCC_RAI` thêm `Dam`, `XemTruoc`, đầu báo tia chiếu `BC_BEAM`
  (tia song song cạnh dài, `TiaVung`, `VeTia`, layer `PCCC_BC_TIA`); `khoang_cach_rai.json` thêm `BC_BEAM`, `dam` của `BC_NHIET`, nhóm `cu_ly` (nút ấn, đèn 45 m);
  lệnh mới `PCCC_DAY`; trang Báo cháy thêm ô dầm, Xem trước, mục 5 (cự ly, dây loop, Dim, bóc KL, chèn bảng), Vẽ vùng tay. Test 102/102 + thu_mcp + thu_gui đạt.
  Danh sách việc còn lại: `cad/tien_do/viec_video_addin.md`.
- Đợt 2: trang Tiện ích (`ui/BangTienIch.cs`, `PCCC_TIENICH`): 27 thẻ theo spec, mã tắt chỉ là huy hiệu (không đăng ký lệnh để khỏi đè lệnh AutoCAD),
  thẻ gửi lệnh có sẵn hoặc lệnh AutoCAD (RENAME, LAYMCH, MEASURE, TEXTALIGN, OVERKILL, LAYER, BOUNDARY). Lệnh mới `plugin/TienIch.cs`: `PCCC_TRUNG`, `PCCC_LOPCHUAN`,
  `PCCC_DANHSO`, `PCCC_XOAY`, `PCCC_TYLE`, `PCCC_DOIBLOCK` (đổi BTR, ghi lại MA_VT), `PCCC_CANDIM`, `PCCC_DOIDN`. Thanh dọc: Quy trình · Tiện ích · Báo cháy · Thư viện · Lệnh · Bot chat · Cài đặt.
  Test 109/109 (7 ca tiện ích), thu_gui đếm 27 thẻ.
- Đợt 3: `PCCC_RAI` bỏ điểm trong polyline kín layer `PCCC_LOAITRU` (lỗ thang, giếng trời; không đưa vào dựng phòng), XemTruoc vẽ tạm điểm + vòng R trên
  `PCCC_XEM_TRUOC` (không in, lượt rải thật xoá); lệnh mới `PCCC_KT` (vùng mù: lưới mẫu 0,5 m trong phòng PCCC_VUNG, xa mọi thiết bị > R, khoanh trên `PCCC_KIEM_MU`);
  trang Báo cháy: preset Lưu/Nạp, Bố trí nhanh (RECTANG trên PCCC_VUNG rồi PCCC_RAI chọn L), Vùng loại trừ, Kiểm vùng mù; Tiện ích thêm thẻ CKO. Test 113/113
  (phòng 20 x 20 m một đầu khói giữa phòng: mù 59,5 % = 952 / 1600, đúng tính tay).
- Đợt 4: trang con ADIM (`ui/BangAdim.cs`): chọn layer (chip DIM, PCCC_DIM, 0_DIM, Hiện hành), DimStyle của bản vẽ, loại đối tượng, phương đo; BẮT ĐẦU gửi
  `-LAYER M`, `-DIMSTYLE R`, `PCCC_ADIM` + loại; phương đặt qua `Lenh.AdimPhuong` (câu hỏi phương nằm sau quét chọn, chuỗi gửi không điền được). `PCCC_ADIM` lọc thêm Mline.
  Test 114/114 (Mline 3 đỉnh ra 2 dim). Người dùng đổi ngưỡng hạn mức CAD thành 95 %, cho tự làm tiếp lúc reset 19h (ghi vào `CLAUDE.md`, `cad/CLAUDE.md`).
- Đợt 5: thanh dọc 9 phân hệ (thêm Chữa cháy, Thống kê, nút cao 40 px, cuộn khi palette thấp); `BangTienIch` thành khung lưới thẻ dùng chung (mảng thẻ, nhóm,
  nút lọc truyền vào), thẻ Chữa cháy, Thống kê ở `ui/PhanHe.cs`; ô tìm lọc cả 2 trang mới; Bố trí nhanh thêm "Đa giác đều" (POLYGON). Test 114/114, thu_gui đạt.
- Thêm: `PCCC_TACHVUNG` (TZ, cắt vùng bằng nửa mặt phẳng), `PCCC_KHUVUC` (MZ, chữ tên + m² ở điểm bấm, tổng m²); CIRCLE trên PCCC_VUNG / PCCC_LOAITRU
  thành đa giác 72 cạnh (rải, loại trừ, kiểm vùng mù); trang Báo cháy thêm Bố trí nhanh "Tròn"; Tiện ích 30 thẻ. Test 117/117 (vòng r 10 m ra 2 x 2 bước 10 m, phủ kín;
  tách 20 x 10 m tại x 8 m ra 80 + 120 m²).
- Thêm: `PCCC_ADIMMOC` (bấm trước mép tường cho ADIM, nhớ ở `Lenh.AdimMoc`, ADIM dùng xong xoá), trang ADIM 2 nút mốc hiện ✓ toạ độ + ✕;
  `PCCC_KT` ghi `KetQuaKtCuoi`, trang Báo cháy hiện ở chân (mù %, cảnh báo). Test 118/118 (2 mốc bấm trước ra cùng 4 dim như gõ trong lệnh).
- Thêm: bước "Công năng" trang Báo cháy: đọc bảng "Loại theo khu vực" của `knowledge/chon-loai-dau-bao.md` (không chép bảng vào mã), gợi ý loại + căn cứ,
  tự chọn đầu khói / nhiệt; chỉ xét phần loại trước ngoặc (karaoke ghi "Nhiệt (… khói thuốc …)" phải ra nhiệt). `build.bat` chép file .md cạnh DLL.
- Thêm: ống gió (spec "vật cản trần"): `PCCC_RAI` tách polyline kín / vòng tròn layer tên có ONG_GIO, ONG-GIO, ONGGIO, DUCT khỏi vùng chọn; đầu báo rơi vào
  ống dịch tới cạnh gần nhất + 0,1 m (còn trong phòng, không vào ống khác), không dịch được thì báo `ket`. Test 119/119 (ống 2 x 2 m trùm 1 đầu: dịch, vẫn 6 đầu).
  Hết danh sách spec làm được; còn việc của người dùng: xem trên AutoCAD thật, Fable kiểm toàn dự án.

## 2026-11-14 đêm — Rải thử bản vẽ thật xưởng bao bì Quyết Thắng (xong 19h40, chờ người dùng duyệt)
19h03 tự làm tiếp (hạn mức đã reset). In PNG 5 lần chèn block nền: tờ "THUYẾT MINH CÔNG TRÌNH" là mặt bằng sạch (rải ở đây), tờ "MẶT BẰNG TỔNG THỂ
BÁO CHÁY" là thiết kế cũ (đối chiếu). Thêm luật vào `NhanVung.cs`: ô X lỗ thông tầng (`TachOX`), hatch gạch chéo = hiện hữu, khe 3–12 m có cửa
hoặc tường 2 nét cùng hở thì khép, cửa ở khe chữ T, chữ T / góc hụt 2–10 mm; test mới trong `chay_thu.ps1` (120 đạt), `thu_mcp` đạt.
Rải: 5 bộ BC_BEAM + 27 BC_KHOI, 7 zone, `PCCC_KT` mù 0 %; `thuyet_minh.md` + ảnh gửi người dùng (hồ sơ khách ở `ket_qua/`, không commit).
Lỗi gặp: PICKBOX 13 px ≈ 1,8 m làm bấm nét mẫu trượt (đặt 2 rồi trả 13); nét mẫu bấm trúng tia beam vừa vẽ; SAVEAS trùng tên treo kịch bản.
Lần đầu (dừng 93 %):
Người dùng gửi `BEAM QUYẾT THẮNG.dwg` (cả tờ là block), chốt 5 quy tắc (ô X = lỗ thông tầng bỏ; mái che vẫn rải; công trình hiện hữu bỏ; 2 xưởng tách;
zone theo TCVN 7568-14 5.7.2.1) và yêu cầu rải + thuyết minh. Đã chép file, dump cấu trúc bằng LISP, tìm được chữ XƯỞNG 1/2 và block nền ứng viên
`A$Cbcabb6d6`; dừng ở 93 % hạn mức (chi tiết + bước kế: `cad/TIEN_DO.md`). Đặt lịch 19h03 tự làm tiếp; theo lời người dùng không push lúc dừng.

## 2026-11-14 khuya — Học bộ bản vẽ PCCC mẫu (DWG)
Người dùng đưa 5 bộ DWG mẫu (`file_cad_test/`, thư mục hồ sơ trên Drive), dặn không sửa file gốc. Chép ra `ket_qua/cad_test/hoc_mau/`, dump bằng
`dump2.lsp` (sửa: MTEXT dài nối các đoạn mã 3, trước đây mất phần đầu), gom 82 đoạn ghi chú dài, in PNG 9 tờ. Viết
`cad/knowledge/hoc-ban-ve-mau-dwg.md` và `cad/knowledge/mau-thuyet-minh-pccc.md` (văn mẫu, đã lọc tên khách, địa chỉ, số GCN). Thêm `file_cad_test/` vào `.gitignore`.
Bài học công cụ: model space xếp tờ theo hàng, tờ thường là block nên gom cụm theo điểm chèn không ra khung; tên tờ nằm DƯỚI hình; bash heredoc làm hỏng `\U` trong python (viết bằng Write).

## 2026-11-15 rạng sáng — 7 việc plugin còn thiếu so với bộ bản vẽ mẫu
Người dùng chọn làm hết 7 việc ở mục 6 `cad/knowledge/hoc-ban-ve-mau-dwg.md`. Thêm `module/plugin/BangMb.cs` (`PCCC_KYHIEU`, `PCCC_QUYDINH`, spec `bang`,
`PCCC_THUYETMINH`), dim bước tia beam trong `PCCC_RAI`, nhãn zone + diện tích + điện trở cuối dây trong `PCCC_DAY`, rải theo khoang dầm (từ khoá Dam,
5.9.1.1.6 c–f), `PCCC_THOATNAN`, vòng tủ vách tường in được. Test mới trong `chay_thu.ps1` (bảng, spec bang, dim tia kt=3, dây vòng + zone, dây hở + điện trở,
thoát nạn, vòng HVT, khoang dầm 4 lớn + 18 nhỏ, thuyết minh 3 hệ 7 mục): TAT CA DAT; `thu_mcp` đạt.
Lỗi gặp: `DAY_BC` có hệ ONG (không phải BC) nên điều kiện điện trở cuối dây xét theo mã; PowerShell 5 `@($null).Count` = 1 (kiểm `-not $x.loi`).

## 2026-11-15 — Luật dầm: xét độ sâu dầm d cùng chiều cao trần h
Người dùng bổ sung: tiêu chuẩn (5.9.1.1.6) xét cả chiều cao dầm và chiều cao trần; có mặt bằng dầm thì quét ký hiệu b x h (vd B1 (400x600)), không có thì
hỏi người dùng một cỡ cho cả mặt bằng hoặc để họ vẽ line chọn cỡ từng vị trí. Làm trong `PCCC_RAI` (Dam): bước nét DẦM mẫu, `NhanDam`/`DocBxh`, `TungDam`,
`MatChon` dựng tường không có nét dầm rồi chỉ giữ dầm sâu > 0,3 m. 3 ca test mới đạt; bản trước chỉ xét h, coi mọi nét dầm là dầm sâu.

## 2026-11-16 — 5 yêu cầu báo cháy theo bản vẽ mẫu hai trường phái
Người dùng gửi `file_cad_test\test báo cháy.dwg` (trái hệ địa chỉ, phải hệ thường; vùng xanh lá là vùng anh tự sửa) và 5 yêu cầu. Đã làm:
- Vùng: `LamGon` (`NhanVung.cs`) gộp cạnh song song lệch < 1 m về đường ngoài, thay chuỗi cạnh ngắn bằng góc giao; đa giác 44 / 53 đỉnh của mẫu ra 8 đỉnh.
  Test cũ đổi số: phòng có cột góc 0,2 x 0,2 m nay 27,84 m² (4 đỉnh), hai phòng thông nhau 55,88 m².
- Dim: `KtLuoi` gom mọi cột thành một chuỗi X, mọi hàng thành một chuỗi Y; `PCCC_RAI` tự dim theo trang Cài đặt (phòng 3 x 2 ra kt=7).
- `CaiDat.cs`: `cai_dat.json` dùng chung plugin + palette (`GiaoDien` gọi lại); biến môi trường `TKPCCC_CAI_DAT` cho test.
- `PCCC_DAYBC` (`DayBc.cs`): rắn bò + chèn rẻ nhất + 2-opt Manhattan, đoạn gãy vuông góc; hệ thường cuối ở thiết bị xa tủ nhất; chuông xương cá; hệ địa chỉ mạch vòng,
  nhóm ZM / CM / R2ML gần nhất. Lần chạy đầu trên file mẫu: dây chuông đi vòng chồng hàng đầu báo, nhóm ZM bị ép cuối ở điểm xa nhất nên đi ngược → đổi chuông sang cây
  xương cá, nhóm ZM cuối tự do (dây ZM 149 → 116 m, chuông 97 → 85 m).
- Trang Cài đặt: kiểu dim, cách dim, layer dim; bảng layer dây / ống. Trang Báo cháy mục 6: hệ, dây cho 9 loại thiết bị, nút ĐI DÂY.
- `HoiMa`: gõ đúng cả tên thì nhận mã đó (thêm BC_NHIET_DC làm "dau bao nhiet" khớp 2 mã, test PCCC_DAT hỏng).
Đã thử thêm phần đoán INSUNITS inch (file mẫu INSUNITS 1 mà vẽ mm): làm hỏng test inch, ngoài yêu cầu → gỡ lại, chỉ ghi chú cho người dùng.
Kiểm: `chay_thu.ps1` 145/145, `thu_mcp.ps1`, `npm test` 68/68. Chưa chạy `thu_gui.ps1` (AutoCAD đang mở).
## 2026-11-16 chiều — Sửa "nhận diện sai vùng, rải linh tinh"
Người dùng gửi ảnh: rải hàng nghìn đầu báo trong lòng tường, cầu thang, không rải 2 xưởng; vùng cần rải là 2 polyline xanh lá (PCCC_VUNG) trong file.
Tái hiện trên bản chép: INSUNITS 1 → 8 837 đầu / 684 "phòng"; INSUNITS 4 → 112 đầu / 57 phòng. Polyline xanh phải Closed = 0 (điểm cuối trùng điểm đầu) nên bị bỏ.
Sửa: `KiemDonVi` tự đặt lại INSUNITS khi khổ bản vẽ phi lý (có báo); `KinPl` coi polyline về đúng điểm đầu là kín, `DinhPl` bỏ đỉnh cuối trùng.
Kết quả: 2 vùng xanh × 15 đầu. Bẫy test PowerShell 5: `if` trả mảng 1 phần tử bị bỏ mảng, bọc `@( if ... )`. `chay_thu.ps1` 147/147.

## 2026-11-16 tối — Giao diện mới học từ 3 video plugin PCCC
Người dùng gửi 3 video (VPTTOOL tối 2 video, HTS_DESIGN sáng 1 video). Xem bằng khung hình tách bởi ffmpeg của `imageio_ffmpeg` (máy không có ffmpeg trên PATH)
+ lời thoại do Gemini (agy) nghe, ~25 nghìn token Gemini mỗi video, Claude chỉ đọc file .md kết quả.
Giao diện: thanh biểu tượng, nút tròn chuyển sắc, tab viên thuốc, khung TRẠNG THÁI, tab Khoang dầm + Tra cứu TCVN. Biểu tượng là emoji Unicode vẽ bằng TextRenderer (ra hình đơn sắc, không cần file ảnh).
Bẫy: RadioButton AutoSize không xuống dòng (đặt cỡ cố định); thanh cuộn dọc hiện ra không đổi Size nên không có Resize: trừ sẵn bề rộng thanh cuộn (`GiaoDien.RongDung`);
tab ẩn không nhận Resize: xếp lại khi chuyển tab. Xem thử giao diện không cần AutoCAD: exe giả lập `Autodesk.AutoCAD.ApplicationServices`, `CaiDat`, `Lenh` rồi `DrawToBitmap`.
Kiến thức + việc chưa làm: `knowledge/hoc-video-plugin-16-11.md`. `chay_thu.ps1`, `thu_mcp.ps1` đạt; `thu_gui.ps1` chưa chạy (AutoCAD đang mở).

## 2026-11-16 tối (tiếp) — Ghi m² khoang dầm, ống nhánh tự chọn DN
Người dùng chọn việc 1 (ghi m² từng khoang), giữa chừng gửi bảng số đầu phun theo DN (2 mức áp suất, thường dùng mức đến 0,5 MPa, phải chọn được mức).
`GhiKhoang` (Rai.cs) ghi m² + dấu X khoang nhỏ; `TuyenXuongCa` thêm tham số `dem`: cắt ống tại từng đầu phun / hàng, kèm số đầu phun phía sau; `PCCC_TUYEN TuDong`
chọn DN theo `data/ong_nhanh_dn.json`. Câu hỏi mã ống của PCCC_TUYEN nhận mã đúng như cũ (kịch bản test cũ không đổi), gõ khác thì về HoiMa.
Bẫy: heredoc trong công cụ Bash nuốt `\\` (thành `\`) khi chạy python: viết script vá ra file bằng Write rồi chạy.

## 2026-11-16 chiều muộn — Lỗi "bước quá nhỏ" + rải đứng máy (đang dở)
Người dùng dán lỗi PCCC_RAI (mặt 763 079 m², 533 đỉnh → lưới 60 x 139), báo mặt bằng bên phải rải đứng máy, "vừa dở vừa lag".
Đã làm: bỏ riêng vùng lưới quá lớn, hỏi khi điểm bấm rơi vào mặt bao ≥ 3 phòng, in thời gian từng pha. Đo bản chép `rai_phai.dwg`:
dựng phòng 78,7 s (37 365 mặt từ 1 block cả mặt bằng), rải 0,27 s → việc kế: tối ưu `MatChon` (cad/TIEN_DO.md).
Luật mới của người dùng: 95–98 % dừng, ghi tiến độ + lịch sử; reset thì phiên nhánh ngữ cảnh sạch làm tiếp, không hỏi.

## 2026-11-16 chiều (phiên nhánh) — Thống kê block động tách theo Visibility
Người dùng chỉ ra: thống kê block không tách block động theo trạng thái Visibility; đặt LHBBlockScheduler v9.7 Premium ở `thamkhao_plugincad/` để học tư duy (không commit).
Dò bản vẽ thật (chỉ đọc): `BEAM_Quyetthang.dwg` có BÌNH CHỮA CHÁY (Visibility1: BỘT ABC 4KG / 8KG / + CO2), ĐÈN EXIT (1 HƯỚNG, 2 HƯỚNG...), tủ 1/2 cuộn vòi;
`PCCC_TANTUONGKHAN.dwg` có bơm điện/diesel theo model. Tên tham số mặc định `Visibility`, `Visibility1`; Lookup tên tự đặt (`Size`, `Opening Angle`).
Làm: `ChungLoai`, dòng tách (mã, chủng loại) trong `KetQua`, cột Chủng loại ở `PCCC_BANG`, alias `tên|trạng thái`, `PCCC_GAN` Rieng/Chung, `dem_loai` của spec `dem`.
Bẫy: accoreconsole không có `setpropertyvalue`/`getpropertyvalue`/`dumpallproperties` (LISP) nên đặt trạng thái block động trong test bằng DLL phụ `test/ThuBlockDong.cs`;
`-INSERT` block có thuộc tính trong kịch bản phải `ATTREQ 0`. Phần đầu `TU_VUNG.md` đã 1893/1900 ký tự: mô tả `dem_loai` chỉ ghi ở bảng chi tiết.

## 2026-11-16 đêm — Rải nhanh 74 s → 3 s, bỏ phòng rác, theo vùng vẽ sẵn, dim theo mẫu
Phiên tự chạy khi hạn mức reset (người dùng cho phép). Đo từng bước `CacMat` trên bản chép `rai_phai.dwg`: chậm do cầu nối khe thẳng hàng tới mọi đoạn cùng hàng
(23 triệu điểm cắt) và HashSet<long> băm trùng; sửa chỉ nối khe gần nhất + bộ băm + lưới ô, kết quả phòng / đầu không đổi.
Ảnh cả tờ cho thấy "dở" vì: Enter mọi phòng nhận cả hình chi tiết trong block tờ, và không dùng vùng PCCC_VUNG người dùng vẽ sẵn → bỏ cụm nhỏ xa, thay phòng bằng vùng vẽ sẵn.
Dim theo ảnh mẫu người dùng (hàng trùng dim 1 lần, chỉ dim thêm thiết bị lệch, dim sát): ô chữ L rải so le của test lộ lỗi chuỗi riêng từng đầu chồng chữ → hàng lệch ≥ 2 đầu một chuỗi.
Bẫy: block tờ xoay 270° (toạ độ trong block không phải toạ độ bản vẽ); ảnh `anh` (in bằng `-PLOT`) của bản gốc không thấy các hình chi tiết đó (nghi layer không in) mà plugin vẫn dựng phòng từ chúng.

## 2026-11-16 tối — Thiết kế thử tầng 8 KOCHAM từ DWG Drive
Người dùng muốn xem Claude hiểu tới đâu trước khi train tiếp. Tải DWG, tách mặt bằng tầng 8 (block "2333"), dựng vùng, rải đầu báo khói (TCVN 7568-14) và sprinkler
nhóm 1 (TCVN 7336 Bảng 1), đi dây và ống bằng spec qua `chay_spec.ps1`, xuất PDF 8 trang so với bản gốc. Sprinkler khớp số đầu bản gốc; báo cháy ít hơn
(8 so với 18) vì chưa xét dầm. Bẫy: mặt bằng tờ PCCC không nằm ở model quanh khung tờ mà trong block chèn ở toạ độ xa; `dump` theo điểm chèn không thấy, phải `ssget "C"`.

## 2026-11-16 tối (2) — Tầng 8 KOCHAM vẽ bằng lệnh gõ tay, hướng dẫn sử dụng
Chạy lại thiết kế bằng `PCCC_RAI`, `PCCC_KT`, `PCCC_DAT`, `PCCC_DAYBC`, `PCCC_TUYEN TuDong`, `PCCC_KYHIEU`, `PCCC_QUYDINH`, `PCCC_BOC`, `PCCC_BANG` trong accoreconsole.
Bằng lệnh thì plugin tự thêm dim kiểu người dùng, EOL, dây chuông, bảng biểu, cỡ ống theo số đầu (spec JSON không có). Bẫy: có `PCCC_VUNG` trong khung chọn thì
`PCCC_RAI` không hỏi điểm phòng (kịch bản lệch dòng); `PCCC_ONG` không chạy trong .scr; chọn đầu phun bằng `WP` theo đường bao phòng, nới mép hơn bán kính ký hiệu.
Viết `cad/HUONG_DAN_SU_DUNG.md` (nguồn: `DS_LENH` trong `Lenh.cs` + dấu nhắc đọc từ mã) và báo cáo quy trình PDF.

## 2026-11-16 tối (3) — Tầng 8 KOCHAM theo dầm (trần hở)
Người dùng gửi file kết cấu, dặn: không trần = có dầm nhô; lõi thang, buồng thang, WC không đặt đầu báo; hộp kỹ thuật = hộp gen thông tầng. Mặt bằng dầm "TẦNG 7"
(tên cũ tầng 8): nhãn mặt bằng ghi h X b (`DS 400X250` = 250 x 400, mặt cắt xác nhận), dầm vẽ polyline 200 sơ đồ. Dời dầm theo mép lõi thang, vẽ `KC_DAM`.
Báo cháy 22 đầu (mỗi khoang một đầu, 5.9.1.1.6 d), dây 86,9 m; sprinkler 23 lên + 5 xuống, mỗi khoang. Plugin `Dam` không chia khoang khi vùng vẽ sẵn → vẽ tay 20 khoang.
Lỗi plugin gom 6 mục vào `cad/TIEN_DO.md`, giao phiên nhánh chạy khi hạn mức reset.

## 2026-11-16 tối (4) — Ốp mặt bằng dầm, vẽ khoang dầm
Người dùng: copy mặt bằng dầm ốp vào mặt bằng kiến trúc rồi vẽ khoang dầm. Khớp bằng trục (khoảng trục 4200/7425/3750 và 3700/3700/3650/3650/3700/3700 trùng kích thước
kiến trúc), không theo mép tường (lần trước lệch 47 mm). Bẫy console: `EXPLODE L` báo *Invalid selection* → chèn bằng `-INSERT *file` (nổ khi chèn). Đổi layer nét dầm
trước khi chèn vì cùng tên `AAP-TUONG` với tường kiến trúc (bộ dò phòng sẽ coi dầm là tường). 20 khoang, 233,5 m².

## 2026-11-16 tối (5) — Đọc nhãn dầm: số lớn là chiều cao
Người dùng chốt: quy tắc đọc dầm vẫn là b x h (rộng x cao); bản kết cấu KOCHAM ghi ngược là ngoại lệ, trước mắt lấy số lớn làm chiều cao. `DocBxh` (Rai.cs)
đổi sang min = b, max = h; test vùng C trong `chay_thu.ps1` đổi nhãn sang `DS 700X300` (trước sửa sẽ ra d = 0,3 → bỏ dầm). Dầm bẹt thật sẽ đọc sai: gặp thì thêm lựa chọn.

## 2026-11-16 tối (6) — Sửa 6 lỗi plugin lộ ra ở tầng 8 KOCHAM
Hạn mức reset, tự chạy theo lời dặn. (1) Lõi thang là block trên layer nội thất nên tường bên trong bị bỏ: nét lồng trên layer tường vẫn lấy.
(2) `PCCC_ONG` tự hỏi điểm, chạy được trong .scr. (3) `PCCC_TUYEN` lọc đầu phun theo cụm vùng + loại. (4) `Dam` chia vùng vẽ sẵn thành khoang; hai bẫy
dữ liệu thật: nét dầm sơ đồ dừng cách dầm biên 200 mm (kéo dài 0,4 m), nhãn dầm cắt ngang gần hơn (ghép theo phương chữ). (5) `PCCC_DAYBC` tìm đường
vuông góc trong hợp các vùng. (6) Dim chung cho khoang của một vùng; sprinkler có `Dam`. Bẫy script: bấm nét dầm mẫu trúng chữ nhãn → zoom trong suốt
(`'_.ZOOM W`) sát nét dầm rồi bấm; khung W phải ôm trọn nét dầm. Test 6 ca trong `chay_thu.ps1`.

## 2026-11-16 tối (7) — Tự bỏ lõi thang, buồng thang, WC; dây đi cách tường
Người dùng hỏi vì sao chưa tự bỏ lõi thang, WC và dây "chạy sát mép" là sao, rồi bảo làm cả hai. Enter = mọi phòng của đầu báo bỏ phòng ≤ 60 m² có
block/layer/chữ thang máy, thang bộ, thiết bị vệ sinh (chữ có "SẢNH" không tính). Dây đi vòng: phạt ô sát mép vùng ≤ 0,4 m. Báo cáo PDF
`BAO_CAO_TANG8_SAU_SUA_LOI.pdf` (ket_qua) giải thích 4 điểm người dùng hỏi bằng ảnh.

## 2026-11-16 tối (8) — Sảnh thang máy không đầu báo; duyệt vùng trước; học mẫu rải khoang dầm
Người dùng: sảnh thang máy cũng không cần đầu báo; xác nhận vùng rải cho duyệt rồi mới rải; "rải khoang dầm sai", gửi bản mẫu (file .bak 43 MB, DWG 2018).
Mẫu là nhà xưởng: dò tờ theo tiêu đề chữ (\U+ giải mã), đo toạ độ 118 block đầu báo: lưới 10 x 10,6 m, mỗi khoang 2 x 2 đầu cách trục 2 m, bước 6 / 6,6 m,
qua dầm 4 m, vòng R7200 ở đầu biên. Ghi ở `cad/knowledge/mau-rai-khoang-dam.md`. `GomKhu` bỏ ngoại lệ "SẢNH".

## 2026-11-17 — d = h dầm − dày sàn, rải thử tầng 8 trần 4,5 m
Người dùng: khoang dầm theo vì kèo, trừ nhà nhiều tầng sàn deck/BTCT (như KOCHAM); có dầm < 0,3 m phải xem kỹ; d phải trừ dày sàn. File kết cấu không ghi
dày sàn (mặt cắt dầm không vẽ sàn): với sàn 100–150 dầm 300/400 bỏ, 500/700/1400 giữ. Gửi đề xuất vùng 2 lần, duyệt lần 2. Thêm từ khoá `DaySan`.
Rải: báo cháy 8 đầu, sprinkler 26 + 5 đầu.

## 2026-11-17 — SuaDam: nhập tay cỡ dầm khi nhãn sai
Người dùng: nhãn KOCHAM "DS 1400X250" thật rộng 1400 cao 700, "sửa luôn". Thêm từ khoá `SuaDam` (bấm nét dầm, nhập b x h thật, thắng nhãn);
cỡ gõ tay đọc đúng rộng x cao. Test ca 9. Tầng 8 chạy lại: kết quả không đổi (dầm 0,7 vẫn sâu, nằm ở sảnh/lõi).

## 2026-11-17 — Tự nhận vùng = phòng, bỏ sảnh thang máy
Người dùng gửi ảnh vùng plugin nhận (vụn) và vùng tự vẽ (1 văn phòng chữ L). Gốc lỗi: bật `Dam` thì dầm sâu dựng phòng nên vùng bị cắt theo dầm,
WC bị cắt ra mảnh không có dấu hiệu WC; sảnh thang không ghi chữ. Sửa: vùng dựng từ tường, dầm chỉ chia khoang trong phòng; block thang máy = ô X,
phòng ≤ 30 m² sát nó = sảnh, đầu báo bỏ. Tầng 8: 4 vùng thành 1 vùng 271,7 m² trùng vùng người dùng vẽ. Test ca 10, 11.

## 2026-11-17 — Vùng trong đường trục, trục không phải tường
Người dùng: "ngoài đường trục thì không được phân vùng khi không được tôi cho phép". Trên file người dùng (bản tự lưu, thử trên bản chép): trục nét liền
layer 0 trong block cắt văn phòng, đọc cả tờ khác, cung bồn cầu và hai khe song song khép khe 11 m. Sửa: đọc trong khung chọn, nhận khung trục từ ký hiệu trục,
bỏ nét trùng trục, bỏ phòng ngoài trục, siết luật khép khe. Kết quả 1 vùng 270,8 m² như người dùng vẽ. Test ca 12.

## 2026-11-17 — Bot chat chọn Claude/Gemini, thẻ thư mục dự án
Người dùng hỏi tab YouTube, Hub cài MCP đã làm chưa (chưa: G7, G8 vẫn trong kế hoạch) và muốn khung chat như FP AI: chọn Claude hay Gemini, gắn thẻ
thư mục dự án làm ngữ cảnh. Bot chat sang `module/ui/BotChat.cs`: chọn máy AI + model trên khung, thẻ thư mục (`--add-dir`). Gemini qua `agy` chế độ plan
(chỉ đọc; `agy` không nhận MCP theo phiên, vẽ CAD vẫn dùng Claude). Thử thật: Gemini đọc đúng file trong thư mục thẻ; Claude CLI cần đăng nhập lại.

## 2026-11-17 — G7 Hub cài MCP (bản đầu)
Người dùng chọn làm Hub. `TKPCCC.Hub.exe`: một cửa sổ, mỗi mục một đèn + Cài/Gỡ (hỏi lại trước khi sửa), nhật ký. Cài plugin = liên kết bundle;
MCP cad ghi cho Claude Code, Antigravity qua CLI của chúng, cho Claude Desktop, Gemini CLI bằng sửa JSON có sao lưu. Test trên nhà giả 14/14.
Bẫy: file .ps1 lỡ có chữ Việt (công cụ Write đổi \u1ec7 thành chữ thật) thì PowerShell 5.1 đọc sai: dựng chuỗi bằng [char]0x1EC7.

## 2026-11-17 — G8 tab YouTube trong palette
AcCef của AutoCAD 2021 là Chromium 77 nên chọn WebView2 (người dùng duyệt tải gói NuGet của Microsoft). Phân hệ YouTube: link hoặc từ khoá,
lùi, trang chủ, mở ra trình duyệt, tự dừng video khi rời tab. Thử ngoài AutoCAD: YouTube chạy, tìm ra 16 video.

## 2026-11-17 — Hub bản 2: bộ cài chia theo tính năng
Người dùng gửi ảnh MCP Hub Pro, muốn gom mọi bộ cài vào Hub, phân rõ từng loại. Hub: 12 mục trong 4 nhóm (Nền tảng, MCP cad, Bot chat, YouTube),
lọc theo nhóm, chọn nhiều mục rồi cài một lần. Phần mềm hãng khác chỉ mở trang tải chính thức. Test 17/17.
Bẫy: script sửa build.bat thay '\n' bằng CRLF trên chuỗi đã có CRLF → dòng '\r\r\n'; đã sửa. build.bat đổi tên DLL cũ TRƯỚC khi biên dịch:
biên dịch hỏng thì bundle mất DLL → chép lại bản .cu mới nhất.

## 2026-11-17 — Khoang dầm: 6 trường hợp, 3 loại trần, đầu báo ở tim dầm, mép dầm là tường
Người dùng hướng dẫn quy trình rải đầu báo khi có khoang dầm (TCVN 7568-14 5.9.1.1.6) và yêu cầu vẽ mặt bằng mô tả từng trường hợp, thêm list chọn có trần / không trần / vừa có vừa không.
- `PCCC_RAI`: từ khoá `LoaiTran` [CoTran/KhongTran/HonHop]; palette tab Khoang dầm thêm ô chọn LOẠI TRẦN. HonHop dùng vùng PCCC_VUNG + line dầm vẽ tay (tim dầm tự dời ra hai mép ± b/2).
- Khoang < ngưỡng: đầu báo đặt trên tim dầm (ghép cặp mép dầm). Dim chung thêm mốc mép dầm.
- Sửa lỗi `ChiaKhoang`: ô hẹp < 1 m (vd 1,8 x 1,8 m) bị bỏ nên khoang nhỏ không chia được.
- Hình 6 trường hợp a–f do plugin tự rải, gửi người dùng xem. Test ca 13. Còn chạy thử trên file "test vùng báo cháy.dwg" (xem TIEN_DO).

## 2026-11-17 trưa — Hẹn việc palette sau reset
Tab YouTube chạy trong AutoCAD (ảnh người dùng). Người dùng muốn ẩn thanh dọc và xem YouTube bằng Brave; hạn mức 5 giờ 99 % nên ghi việc vào
`cad/TIEN_DO.md` (VIỆC KẾ), hẹn phiên nhánh ngữ cảnh sạch làm sau reset 13:40.

## 2026-11-17 (13:45) — Palette thu gọn thanh dọc + YouTube bằng Brave
Nút ☰ đầu palette: menu phân hệ + thu gọn / hiện thanh dọc (nhớ `ben_an`). Tab YouTube chọn trình xem WebView2 (trong palette) hoặc Brave
(cửa sổ app riêng áp mép phải AutoCAD, nhớ `yt_trinh_duyet`). Hub thêm mục Brave (tuỳ chọn) ở nhóm YouTube. build + chay_thu, thu_mcp, thu_hub đạt.

## 2026-11-17 — Chạy thử khoang dầm trên file test; tách 3 plugin
- File "test vùng báo cháy.dwg" (bản chép): block mặt bằng dầm chép đè khớp trục → 1 vùng 270,82 m², 2 khoang theo dầm sâu, dầm 400X250 (d 0,28) bỏ. Sửa: dầm trong block dầm chép đè nhận theo layer, mọi màu.
- Tách plugin YouTube (`TKYOUTUBE`, cửa sổ co giãn, thu nhỏ) và Bot chat (`TKBOTCHAT`, thêm chọn effort) ra bundle riêng; Hub cài / gỡ từng plugin. Thử trong AutoCAD thật đạt.

## 2026-11-18 — Bot chat như VS Code (dán ảnh, vùng chọn CAD) + Brave có tab, toàn màn hình trong khung
- Bot chat: Enter gửi / Shift+Enter xuống dòng; dán ảnh (Ctrl+V, menu Dán, kéo thả, "+ Ảnh") thành thẻ, AI xem bằng Read; thẻ vùng chọn từ đối tượng đang chọn
  hoặc lệnh mới TKBOTCHON (nút "Quét vùng CAD"): gửi kèm handle, thành phần, hộp bao.
- Spec: lọc h (handle) cho xoa, doc; doc gi dt; thao tác mới sua (tl, xoay, doi, l_moi, mau, t). TU_VUNG.md cập nhật.
- Brave: hồ sơ riêng, cửa sổ thường có tab (link sau = tab mới), lần đầu áp mép phải AutoCAD rồi Brave nhớ cỡ người dùng chỉnh;
  extension brave_khung (--load-extension) cho toàn màn hình chỉ phủ khung cửa sổ: thử Brave 1.99 headless trên YouTube đạt.
- Sửa chay_thu.ps1 (tham chiếu module/ui/webview2 cũ). chay_thu 171/171, thu_mcp TAT CA DAT. Chưa thử trong AutoCAD có giao diện.
- Lượt 2: cửa sổ Brave nhận AutoCAD làm chủ (GanBrave.cs) nên luôn hiện trên CAD khi thao tác bản vẽ; test mới thu_brave.ps1 TAT CA DAT.
- Lượt 3 (ảnh khung chat mẫu): Bot chat có ô Dự án (thư mục làm việc, skill .claude\skills), + Skill (add-dir / plugin-dir / lớp bọc junction), menu / Skill,
  Lịch sử / Link phiên Claude Code (VS Code, CLI, Desktop; --fork-session) và Antigravity (đọc thô SQLite), bong bóng tin, đồng hồ trạng thái. thu_bot.ps1 TAT CA DAT.
- Lượt 4: hộp chọn thư mục kiểu Explorer (dán đường dẫn, mở theo clipboard), thả thư mục vào nút dự án; giao diện Bot chat viết lại theo khung chat mẫu
  (thanh đầu, bong bóng, khung soạn chip + / model / gửi), menu model đủ đời Claude + Antigravity (agy models).

## 2026-11-19 — Bot chat ghi file, phím như VS Code; in ảnh bằng PlotEngine
- Bot chat Claude được Edit/Write (acceptEdits trong thư mục dự án), Enter khi đang chạy = xếp hàng, phím ô nhập không bị AutoCAD giữ (PreTranslateMessage, KeepFocus).
- Thao tác `anh` in PNG bằng PlotEngine, không còn hộp "Plot Job Progress" kẹt. chay_thu 171/171, thu_mcp, thu_bot TAT CA DAT.
- Rải đầu báo trong vùng: phiên nhánh cad-rai-trong-vung.

## 2026-11-19 — Rải không để thiết bị ngoài / sát nét vùng
- Người dùng gửi 2 ảnh đầu báo khói lệch ra ngoài / nằm trên nét vùng (dải cạnh xiên, hành lang chữ L). Nguyên nhân: lưới theo hộp bao + phủ kín lượt 2 bỏ cách tường.
- `Rai.cs`: `DoiVao` / `KepDay` dời thiết bị vào trong vùng cách cạnh ≥ `tuong_min` (theo hàng hoặc cột, được lệch hàng cột), `PhuKin` dời vị trí thử thay vì bỏ;
  mặc định `CACH_TUONG_MIN_M` 0,1 m khi mã chưa có `tuong_min`. Test `rai_hep` mới, `rai_xien` kiểm thêm cách cạnh. chay_thu 171 DAT / 0 LOI (1 BO: bản vẽ thật không có trong worktree), thu_mcp TAT CA DAT.

## 2026-11-19 — Bot chat: gõ tiếng Việt (Unikey) ra sai chữ
- Ảnh người dùng: gõ "chiếu" ra "chiẤu". Nguyên nhân: `ChuyenPhim` (BotChat.cs) khai báo `DispatchMessage` không có CharSet nên gọi DispatchMessageA,
  WM_CHAR Unicode bị đổi sang mã ANSI. Sửa: `CharSet.Unicode`. Tái hiện bằng TextBox + WM_CHAR: cũ ra 00BF, mới ra 1EBF (ế).
- chay_thu, thu_mcp, thu_gui TAT CA DAT. Chưa gõ thử bằng tay trong AutoCAD thật.

## 2026-11-19 — Bot chat: đổi tab bản vẽ vẫn làm được, không nhầm file
- Lời nhắn Bot chat gắn bản vẽ đang hiện lúc gửi (env TKPCCC_CAD_BAN_VE của máy chủ MCP, ghi đè `ban_ve` của gói); AI được báo khi đổi bản vẽ.
- Cầu nối chạy gói trên đúng bản vẽ khoá: không đang hiện thì chạy nền (LockDocument, một bước Undo), không đổi tab người dùng; pdf / anh cần bản đang hiện.
- chay_thu 172 DAT, thu_mcp, thu_bot, thu_gui (AutoCAD thật, 2 bản vẽ) TAT CA DAT.

## 2026-11-19 — Bot chat: gửi gấp Ctrl+X, nối phiên chung VS Code / Claude Code
- Ctrl+X (không bôi đen chữ) khi bot đang chạy: lời nhắn vào ngay lượt đang chạy (claude `--input-format stream-json`, stdin mở suốt lượt, đóng khi có result);
  Claude gộp với yêu cầu trước ở chỗ nghỉ kế tiếp giữa hai bước công cụ (dò bằng Haiku trên claude 2.1.326). Enter vẫn xếp hàng.
- Cài đặt "Nối phiên chung" (Không / VS Code / Claude Code): link phiên ở Lịch sử ghi thẳng vào phiên gốc (phiên vừa ghi trong 2 phút thì vẫn tách nhánh);
  mở hội thoại sang VS Code (`vscode://anthropic.claude-code/open?session=`) hoặc app Claude Code (`claude://resume?session=`).
- chay_thu, thu_mcp, thu_bot (thêm mục stream-json, model giả) TAT CA DAT; thu_gui lần 1 lỗi 2 mục do song.json trễ sau gói pdf + plot, chạy lại TAT CA DAT.
  Chưa thử bằng tay trong AutoCAD.

## 2026-11-20 — Đối chiếu phân vùng AI và plugin trên cùng một mặt bằng xưởng
- Bot chat (không có Bash): mặt bằng trên vẽ lại bộ 25 vùng đã duyệt 19/11 (đọc lại từ nhật ký phiên và `b4_vung.json`, dời theo `goc`) + rải 55 BC_KHOI;
  mặt bằng dưới là kết quả plugin người dùng chạy: 35 vùng, 69 đầu báo. `rai` nhận `vung` là toạ độ đỉnh, không phải handle (lỗi "Input string" nếu đưa handle).
- Plugin sai vì cột chữ I trên layer tên ống bị coi là tường 2 nét rồi bắc cầu thành tường ảo; thiếu luật khoang vì kèo, khu mái che / phòng bơm / hiên; bỏ sót 2 phòng văn phòng.
  Luật dạy plugin L1–L8 + test: `knowledge/hoc/phan-vung-xuong-mai-keo.md`. Chưa sửa mã (việc kế ở `TIEN_DO.md`).

## 2026-11-20 — Viết mã phân vùng như AI: cột theo hình, khoang vì kèo, khu không rải theo chữ (chưa build)
- Bot chat lượt 2 (không có shell): đọc lại cột trên "BEAM QUYẾT THẮNG.dwg": chữ I 507 x 830 mm, layer `$.2.9DN25`, 4 hàng dọc x 9 hàng ngang cách 6 m.
- `NhanVung.cs`: L1 `GhiCotUng` / `TachCot`, `CacMat` tham số `khongNoi` (nét cột không khép khe); L2 `DuongViKeo`; L5 `GomChuKhu` / `LoaiKhuTen`; L8 `CumChinh(..., giu)`.
- `Rai.cs`: `LoaiTran` thêm `MaiKeo` (không hỏi dầm, chia khoang theo hàng cột), bỏ khu theo chữ (`bo_khu_ten=`), giữ phòng có chữ "PHÒNG" ngoài trục / cụm nhỏ.
  Palette tab Khoang dầm thêm mục "Mái kèo". `chay_thu.ps1` đổi chuỗi dò thành `[CoTran/KhongTran/HonHop/MaiKeo]`.
- Chưa build, chưa chạy test, chưa commit (phiên không có shell). L6 hiên, L7 thang bộ chưa làm.

## 2026-11-20 — Build + test mã mái kèo, thử trên bản chép xưởng Quyết Thắng
- Claude Code: `build.bat` đạt (không lỗi C# 5), `chay_thu.ps1` và `thu_mcp.ps1` TAT CA DAT. Commit cùng mã Bot chat (chưa push).
- Thử `PCCC_RAI BC_KHOI LoaiTran MaiKeo 9` trong accoreconsole trên bản chép (Drive → `ket_qua/QUYET_THANG/cad/thu_maikeo/`, không commit): 68 s, 36 vùng/khoang,
  80 đầu báo; đáp án AI 25 vùng / 55. Cột chữ I, phòng bơm, mái che 187 m², 2 phòng góc phải đã đúng. Còn sai: hàng vì kèo mượn cột nhà kề (3 khoang mỏng xưởng 2),
  dải mỏng đầu hồi xưởng 1 chưa gộp, xưởng 2 nuốt dải mái che phía nam, hiên (L6). Chi tiết + thứ tự sửa: `TIEN_DO.md` mục VIỆC 20/11 lượt 3.
- Bẫy kịch bản: `(command "_.ERASE" (ssget ...) "")` khi ssget trả nil làm lệch dấu nhắc, console lặp SAVEAS tới hết giờ: bọc `(if (setq ss (ssget ...)) ...)`.

## 2026-11-20 — Sửa khoang vì kèo sau khi thử bản vẽ thật
- `NhanVung.cs` `DuongViKeo`: hàng vì kèo chỉ từ cột nằm trong bề ngang chính phòng (± 0,6 m), không có thì mới mượn cột gần 6 m (trước: cột hiên nhà văn phòng
  thành 2 hàng thừa); hàng cách biên < nửa bước vì kèo bỏ (phần lồi đầu hồi gộp vào khoang đầu).
- `Rai.cs` MaiKeo: khoang ≤ 500 m² chứa chữ khu (mái che, sân, hiện hữu, phòng bơm) bỏ sau khi chia.
- Bản chép Quyết Thắng: 80 → 68 đầu báo (đáp án 55); xưởng 2 khớp đáp án 8 khoang / 16. Còn hiên (L6), mái che tây nam, dải xiên xưởng 1. Test TAT CA DAT.

## 2026-11-20 — L6 hiên (chỉ khi LoaiTran MaiKeo)
- `Rai.cs`: dải hẹp (rộng TB 2A/P ≤ 3 m, ≤ 200 m²) có ≥ 2 cột ở trong / sát viền mà cột không sát phòng khác = hiên, bỏ (`bo_hien=`).
- Chạy mọi chế độ thì `test báo cháy.dwg` (506 cột) mất phòng duy nhất còn lại: giới hạn L6 vào MaiKeo. chay_thu, thu_mcp TAT CA DAT.
- Bản chép Quyết Thắng: 68 → 62 đầu báo (đáp án 55); nhà văn phòng 2 khớp 4 phòng. Còn mái che tây nam 63,55 m², dải xiên xưởng 1, L7.

## 2026-11-20 — Test tổng hợp mái kèo + hiên
- `chay_thu.ps1` ca 14: xưởng có cột hai tường dọc, dải mái che có chữ, phần lồi đầu hồi, nhà văn phòng có hiên cột trong 6 m quanh xưởng; kiểm 8 khoang, bỏ khoang mái che,
  bỏ hiên. Ca 13 cắt phần ca 14. TAT CA DAT.
