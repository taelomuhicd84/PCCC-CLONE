# TKPCCC — hướng dẫn cho Claude Code

Bộ công cụ thiết kế PCCC: plugin AutoCAD 2021 + runner Node + web nội bộ. Người dùng là kỹ sư PCCC, giao tiếp **tiếng Việt**.
**Phiên mới: đọc `TIEN_DO.md` trước** (việc đang làm, việc tiếp theo). Cấu trúc thư mục: `STRUCTURE.md`. Các pha: `ROADMAP.md`. Nhật ký: `LICH_SU.md`.

## Cấu trúc (chi tiết: STRUCTURE.md, theo khung dự án mẫu PCCC_he_thong)
`shared/src` nghiệp vụ thuần · `runner/src` server, xuất Excel/Word, `engines/claude.ts` · `web/index.html` · `cad/src/Lenh.cs` plugin ·
`data/` danh mục + quy trình · `knowledge/` tri thức · `skills/` điều khoản MOD_/SKILL_ · `ket_qua/<dự án>/` kết quả (không commit).
Mỗi module có `test/` cạnh `src/`. Thêm/đổi thư mục thì cập nhật `STRUCTURE.md`.

## Quy tắc
- Plugin build bằng csc.exe của Windows ⇒ **chỉ C# 5** (không `$""`, `?.`, `nameof`, `out var`); chỉ tham chiếu `acdbmgd` + `accoremgd`
  (dùng `Core.Application`) để chạy được trong `accoreconsole`. Lệnh mới bọc `Chay(...)`.
- Gọi Claude **không dùng API key**: `runner/src/engines/claude.ts` spawn `claude -p` bằng đăng nhập CLI, chỉ công cụ đọc, không nạp hook/plugin cá nhân.
- `data/vat_tu.json` là nguồn DUY NHẤT của danh mục vật tư (plugin + báo giá).
- Số người dùng nhập kiểu Việt Nam ("1.200" = 1200, "3,5" = 3.5): đọc bằng `docSoVN` (`shared/src/bao_gia.ts`).
- Tri thức/điều khoản chép nguyên văn vào `knowledge/` hoặc `skills/`, không viết theo trí nhớ.
- Kiểm tra trước khi báo xong: `npm test` và `powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1` (không cần mở AutoCAD).
- Không commit `ket_qua/`, `data/don_gia.json`, DLL build. `PCCC_Blocks.dwg`, `block_alias.json` (người dùng sửa) ĐƯỢC commit.
- File `.bat`/`.ps1` chỉ dùng chữ ASCII.

## Git / GitHub
- Remote: https://github.com/taelomuhicd84/PCCC-CLONE (nhánh `main`, repo **công khai**: không đưa hồ sơ khách, token, đơn giá vào repo).
- Commit ở mỗi mốc việc. **Khi usage gần hết: cập nhật `TIEN_DO.md` + `LICH_SU.md`, commit, push** (người dùng dặn 2026-10-31).
- Push từ Bash của Claude có thể không lấy được đăng nhập Git Credential Manager: chạy `git push` trong tab Terminal của người dùng.
