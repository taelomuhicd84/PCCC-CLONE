# Tiến độ CAD (riêng cho thư mục cad/)

Phiên mới làm việc CAD: đọc file này trước, rồi `cad/README.md` (cây thư mục, sửa gì vào đâu). Kế hoạch tổng G0–G9: `cad/KE_HOACH.md`. Nhật ký: `cad/LICH_SU.md`. Luật riêng: `cad/CLAUDE.md`.
Luật: dùng tới 85 % hạn mức thì dừng, ghi file này + `cad/LICH_SU.md`, commit, push.

## TRẠNG THÁI (cập nhật 2026-11-11)
- **XONG MCP CAD (G0, G1, G2)**, đã push 10/11: lệnh `PCCC_SPEC` 10 thao tác, cầu nối, `cad/bin/TKPCCC.Mcp.exe` (công cụ `cad_tt`, `cad_chay`).
- **XONG 11/11: sắp `cad/` thành dự án con** theo ý người dùng: `.claude/` (skills, agents), `skills/`, `knowledge/`, `data/`, `module/`, `rule/`. Mã nằm trong `module/` (plugin, mcp, chung, lisp); 2 skill CAD nằm trong `cad/.claude/skills/`; từ vựng spec ở `cad/skills/TU_VUNG.md`.
- **Thêm 11/11:** `cad/data/ky_hieu/ky_hieu.json` (mặc định ký hiệu trục, kích thước, chữ; plugin đọc lại ở mỗi lần `PCCC_SPEC`), `cad/rule/LUAT.md` (9 dòng nối việc CAD ↔ file quy chuẩn ↔ skill, agent), `cad/knowledge/quy-uoc-ban-ve.md`.
- Kiểm 11/11 sau khi chuyển: `chay_thu.ps1` 12/12, `thu_mcp.ps1` 7/7. Máy chủ `cad` trong `.mcp.json` gốc đã được duyệt và nối được (Claude Code thấy công cụ `mcp__cad__cad_tt`, `mcp__cad__cad_chay`).

## XONG 11/11: thử thật với AutoCAD đang mở
Người dùng NETLOAD `TKPCCC.Cad.dll`; `cad_tt` ra tên bản vẽ `K2-2026 - Copy.dwg` (177 layer); `cad_chay` lưới trục 5×9 → `ok` true, `ve` = 90, hộp `[-2555,-2555,16955,34955]`. Đường nối timer + `SendStringToExecute` chạy được.
Lưới nằm trong bản vẽ thật của người dùng (gốc 0,0): 👤 Ctrl+Z một lần để bỏ (một lần gọi = một bước Undo).

## VIỆC KẾ
- Chưa đo số token của lần vẽ; chưa thử `dat`, `ong`, `doc`, `dem` trên bản vẽ thật (đã có test accoreconsole).
- Sau đó theo `KE_HOACH.md`: G3 `rai`, `tuyen`, `PCCC_RAI`.

## CHƯA KIỂM
- Skill trong `cad/.claude/skills/` có hiện khi mở Claude Code ở gốc repo không (Claude Code nạp skill của thư mục con khi làm việc với file trong thư mục đó). Không hiện thì vẫn vẽ được nhờ hướng dẫn máy chủ MCP gửi lúc kết nối; cần thì đặt bản trỏ ở `.claude/skills/` gốc.
- `cad/.mcp.json` (mở Claude Code ngay tại `cad/`) chưa thử.
- `cad/rule/LUAT.md` lập theo tên file, chưa mở từng file căn cứ để đối chiếu phạm vi.

## VIỆC ĐỂ LẠI (thứ tự theo `KE_HOACH.md`)
- G3 `rai`, `tuyen`, `PCCC_RAI` · G4 PDF → CAD · G5 `so_do` · G6 bảng trong CAD + chat + `cad_anh` · G7 Hub · G8 YouTube · G9 bản quyền (trước đó chuyển repo sang riêng tư).
- Dữ liệu chờ người dùng cấp (xem `cad/data/README.md`): block động co/tê, catalog ống + phụ kiện, catalog bơm, bản vẽ mẫu + khung tên.

## GHI NHỚ KỸ THUẬT
- Thêm thao tác spec: hàm `Op...` trong `module/plugin/Spec.cs` + 1 dòng `MotOp` + 1 dòng `skills/TU_VUNG.md` + 1 phép kiểm trong `test/chay_thu.ps1`.
- Phần đầu `skills/TU_VUNG.md` (trước dòng `---`) gửi cho AI lúc kết nối: giữ dưới 1900 ký tự (`thu_mcp.ps1` có kiểm).
- Lệnh Bash của Claude làm mất một dấu gạch chéo ngược khi viết hai dấu liền nhau trong heredoc: script Python có đường dẫn Windows thì dùng `chr(92)`, hoặc ghi script ra file rồi chạy.
- `build.bat` đổi tên DLL/exe đang bị khoá thành `*.cu*` rồi build bản mới. Build hỏng giữa chừng thì bundle tạm không có DLL: sửa lỗi rồi chạy lại `build.bat`.
