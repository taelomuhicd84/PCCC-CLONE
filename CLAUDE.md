# TKPCCC — hướng dẫn cho Claude Code

Bộ công cụ thiết kế PCCC: plugin AutoCAD 2021 + runner Node + web nội bộ. Người dùng là kỹ sư PCCC, giao tiếp **tiếng Việt**.
**Phiên mới: đọc `TIEN_DO.md` trước** (việc đang làm, việc tiếp theo), lịch sử ở `LICH_SU.md`, kế hoạch các pha ở README.

## Cấu trúc
- `data/vat_tu.json` — danh mục vật tư, nguồn DUY NHẤT cho plugin + báo giá (`loai`: block / tuyen / khac). `data/huong_dan.json` — các bước B0–B7.
- `cad/TKPCCC.Cad/Lenh.cs` — plugin (1 file). Build `cad\build.bat` bằng csc.exe của Windows ⇒ **chỉ C# 5** (không `$""`, `?.`, `nameof`, `out var`).
  Chỉ tham chiếu `acdbmgd` + `accoremgd` (dùng `Core.Application`) để chạy được trong `accoreconsole`.
- `runner/` — `server.ts` (API + phục vụ `web/index.html`), `bao_gia.ts`, `excel.ts`, `khao_sat.ts`, `word.ts`, `claude.ts`.
- `knowledge/` — trích quy chuẩn (chép nguyên văn từ dự án PCCC_he_thong). Không viết điều khoản theo trí nhớ.

## Quy tắc
- Gọi Claude **không dùng API key**: `runner/claude.ts` spawn `claude -p` bằng đăng nhập CLI, chỉ công cụ đọc, không nạp hook/plugin cá nhân.
- Kiểm tra trước khi báo xong: `npm test` (runner) và `powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1` (plugin, chạy trong accoreconsole, không cần mở AutoCAD).
- Số liệu người dùng nhập kiểu Việt Nam ("1.200" = 1200, "3,5" = 3.5): đọc bằng `docSoVN`.
- Không commit `du_an/`, `data/don_gia.json`, DLL build. `PCCC_Blocks.dwg` và `block_alias.json` (người dùng sửa) ĐƯỢC commit.
- Commit ở mỗi mốc việc. **Khi usage gần hết: cập nhật `TIEN_DO.md` + `LICH_SU.md`, commit, push GitHub** (người dùng dặn 2026-10-31).
- File `.bat`/`.ps1` chỉ dùng chữ ASCII (cmd và PowerShell 5.1 đọc sai UTF-8 không BOM).
