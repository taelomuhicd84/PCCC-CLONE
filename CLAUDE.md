# TKPCCC — hướng dẫn cho Claude Code

Bộ công cụ thiết kế PCCC: plugin AutoCAD 2021 + runner Node + web nội bộ. Người dùng là kỹ sư PCCC, giao tiếp **tiếng Việt**.
**Phiên mới: đọc `TIEN_DO.md` trước** (việc đang làm, việc tiếp theo). Cấu trúc thư mục: `STRUCTURE.md`. Các pha: `ROADMAP.md`. Nhật ký: `LICH_SU.md`.

## Cấu trúc (chi tiết: STRUCTURE.md, theo khung dự án mẫu PCCC_he_thong)
`shared/src` nghiệp vụ thuần · `runner/src` server, xuất Excel/Word, `thamdinh.ts`, `engines/claude.ts` · `web/index.html` · `cad/src/Lenh.cs` plugin ·
`thamdinh/` lõi Python thẩm định PDF · `data/` danh mục + quy trình · `knowledge/` tri thức · `skills/` điều khoản MOD_/SKILL_ ·
`ket_qua/<dự án>/` kết quả (không commit).
Mỗi module có `test/` cạnh `src/`. Thêm/đổi thư mục thì cập nhật `STRUCTURE.md`.

## Quy tắc
- Plugin build bằng csc.exe của Windows ⇒ **chỉ C# 5** (không `$""`, `?.`, `nameof`, `out var`); chỉ tham chiếu `acdbmgd` + `accoremgd`
  (dùng `Core.Application`) để chạy được trong `accoreconsole`. Lệnh mới bọc `Chay(...)`.
- Gọi Claude **không dùng API key**: `runner/src/engines/claude.ts` spawn `claude -p` bằng đăng nhập CLI, chỉ công cụ đọc, không nạp hook/plugin cá nhân.
- `data/vat_tu.json` là nguồn DUY NHẤT của danh mục vật tư (plugin + báo giá).
- Số người dùng nhập kiểu Việt Nam ("1.200" = 1200, "3,5" = 3.5): đọc bằng `docSoVN` (`shared/src/bao_gia.ts`).
- Tri thức/điều khoản chép nguyên văn vào `knowledge/` hoặc `skills/`, không viết theo trí nhớ.
- Kiểm tra trước khi báo xong: `npm test`, `python -m pytest thamdinh/test -q` và
  `powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1` (không cần mở AutoCAD, nhưng máy phải cài AutoCAD 2021).
- `thamdinh/` chép từ repo PCCC (TestPCCC): sửa luật ở `thamdinh/rules/*.yaml`, ghi phần đã cắt/sửa vào `thamdinh/README.md`.
- Repo tham khảo (công khai từ 2026-11-01): `taelomuhicd84/SKILL-PCCC-NEW` (web thẩm định theo bảng đối chiếu A/B/KT, agent m01–m18,
  thủy lực `shared/src/thuy_luc.ts`), `taelomuhicd84/PcccVipPro` (module `pccc`: agent thẩm định, `tools/thuy_luc.py`, `thoat_nan.py`),
  `taelomuhicd84/PCCC` (lõi Python gốc). Clone vào thư mục tạm khi cần, không sửa các repo đó.
- Không commit `ket_qua/`, `data/don_gia.json`, DLL build. `PCCC_Blocks.dwg`, `block_alias.json` (người dùng sửa) ĐƯỢC commit.
- File `.bat`/`.ps1` chỉ dùng chữ ASCII.

## Skill dự án (`.claude/skills/`)
- `tkpccc-cad-console` — build + kiểm thử plugin trong accoreconsole, in bản vẽ ra PNG, các bẫy API AutoCAD đã gặp.
- `tkpccc-xong-dau-muc` — khép đầu mục: kiểm → TIEN_DO/LICH_SU → commit → push → ngữ cảnh mới làm tiếp.
- `tkpccc-tra-quy-chuan` — chọn file quy chuẩn theo câu hỏi, nguyên tắc chỉ trích văn bản hiện hành.
Quy trình nào lặp lại và hay thì gom thành skill mới ở đây (người dùng dặn 2026-10-31).

## Nhịp làm việc (người dùng dặn 2026-10-31)
**Xong mỗi đầu mục: chạy skill `tkpccc-xong-dau-muc`** (lưu tiến độ, push, rồi xoá ngữ cảnh và làm tiếp việc kế trong `TIEN_DO.md`).

## Git / GitHub
- Remote: https://github.com/taelomuhicd84/PCCC-CLONE (nhánh `main`, repo **công khai**: không đưa hồ sơ khách, token, đơn giá vào repo).
- Commit ở mỗi mốc việc.
- **Gần hết usage: LUÔN tự làm, không chờ nhắc** (người dùng dặn 2026-10-31, nhắc lại 2026-11-01): kiểm bằng công cụ `get_usage`
  (ccd_session_mgmt) sau mỗi việc lớn; cửa sổ 5 giờ hoặc tuần ≥ 85% thì **dừng ngay ở việc đang làm**, ghi `TIEN_DO.md` (dở ở đâu,
  bước tiếp theo cụ thể) + `LICH_SU.md`, commit, push (skill `tkpccc-xong-dau-muc`).
- Push từ Bash của Claude có thể không lấy được đăng nhập Git Credential Manager: chạy `git push` trong tab Terminal của người dùng.
