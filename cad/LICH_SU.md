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
