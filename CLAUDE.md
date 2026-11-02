# TKPCCC — hướng dẫn cho Claude Code

Bộ công cụ thiết kế PCCC: plugin AutoCAD 2021 + runner Node + web nội bộ + skill Claude. Người dùng là kỹ sư PCCC, giao tiếp **tiếng Việt**.
**Phiên mới: đọc `TIEN_DO.md` trước** (việc đang làm, việc tiếp theo). Cấu trúc thư mục: `STRUCTURE.md`. Các pha: `ROADMAP.md`. Nhật ký: `LICH_SU.md`.

## Mỗi module là một skill (người dùng dặn 2026-11-02: sửa module nào thì chỉ vào đó, đỡ tốn token)
Làm module nào: đọc `.claude/skills/<skill>/SKILL.md` của module đó và các file nó liệt kê; không đọc cả repo.
| Module | Skill | Agent |
|---|---|---|
| Khảo sát – tư vấn PCCC (bảng KS + comment, phương án trên PDF) | `tkpccc-khao-sat-tu-van` | |
| Thẩm định PCCC: xây dựng (bảng KT) + hệ thống (bảng A, B) | `tkpccc-tham-dinh` | `pccc-xay-dung`, `pccc-he-thong` |
| Bảng đối chiếu A/B/KT/KS: chọn, lập, gộp, kiểm, xuất Excel | `tkpccc-bang-doi-chieu` | |
| Ghi chú + vẽ phương án lên PDF (annotation thật) | `tkpccc-ghi-chu-ban-ve` | |
| Máy kiểm luật PDF (`thamdinh/`, tab Thẩm định PDF) | `tkpccc-kiem-luat-pdf` | |
| Thủy lực TCVN 7336 | `tkpccc-thuy-luc` | |
| Khối lượng, báo giá, tab Khảo sát web | `tkpccc-bao-gia` | |
| Hướng dẫn thiết kế B0–B7 | `tkpccc-huong-dan` | |
| Plugin AutoCAD (+ đường thiết kế CAD qua MCP, Pha 6) | `tkpccc-cad-console` | |
| Tra quy chuẩn (chọn file, chỉ văn bản hiện hành) | `tkpccc-tra-quy-chuan` | |
| Khép đầu mục: kiểm → TIEN_DO/LICH_SU → commit → push | `tkpccc-xong-dau-muc` | |
- Module mới: tạo `.claude/skills/tkpccc-<tên>/` (`SKILL.md`; module chỉ Claude dùng thì để luôn `scripts/`, `data/`, `test/` trong đó), thêm 1 dòng
  vào bảng trên và `STRUCTURE.md`. Code chạy trong web/runner vẫn ở `shared/`, `runner/`, `thamdinh/`, `cad/` nhưng `SKILL.md` của module phải liệt kê.
- Quy trình nào lặp lại và hay thì gom thành skill (người dùng dặn 2026-10-31).

## Quy tắc chung
- Tư vấn, thẩm định: Claude chỉ BÁO CÁO (nhận diện, đánh giá, phương án, khả thi, căn cứ), không hỏi người dùng chọn; luôn ghi chú lên PDF.
  Chi tiết và các bài học đã train nằm trong skill `tkpccc-khao-sat-tu-van`, `tkpccc-tham-dinh`.
- Tri thức/điều khoản chép nguyên văn vào `knowledge/` hoặc `skills/`, không viết theo trí nhớ.
- Gọi Claude **không dùng API key**: `runner/src/engines/claude.ts` spawn `claude -p` bằng đăng nhập CLI, chỉ công cụ đọc, không nạp hook/plugin cá nhân.
- `data/vat_tu.json` là nguồn DUY NHẤT của danh mục vật tư (plugin + báo giá).
- Số người dùng nhập kiểu Việt Nam ("1.200" = 1200, "3,5" = 3.5): đọc bằng `docSoVN` (`shared/src/bao_gia.ts`).
- Plugin AutoCAD: **chỉ C# 5**, chỉ tham chiếu `acdbmgd` + `accoremgd` (chi tiết skill `tkpccc-cad-console`).
- Kiểm tra trước khi báo xong: `npm test`, `python -m pytest thamdinh/test .claude/skills -q` và
  `powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1` (không cần mở AutoCAD, nhưng máy phải cài AutoCAD 2021).
- Repo tham khảo (công khai từ 2026-11-01): `taelomuhicd84/SKILL-PCCC-NEW` (web thẩm định theo bảng đối chiếu A/B/KT, agent m01–m18,
  thủy lực), `taelomuhicd84/PcccVipPro` (module `pccc`: agent thẩm định, `tools/thuy_luc.py`, `thoat_nan.py`), `taelomuhicd84/PCCC`
  (lõi Python gốc). Clone vào thư mục tạm khi cần, không sửa các repo đó, không chạy code của chúng.
- Không commit `ket_qua/`, `data/don_gia.json`, DLL build. `PCCC_Blocks.dwg`, `block_alias.json` (người dùng sửa) ĐƯỢC commit.
- File `.bat`/`.ps1` chỉ dùng chữ ASCII; `.ps1` buộc có chữ Việt thì lưu UTF-8 có BOM (như `scripts/chen_hang_cuoi.ps1`).

## Nhịp làm việc (người dùng dặn 2026-10-31)
**Xong mỗi đầu mục: chạy skill `tkpccc-xong-dau-muc`** (lưu tiến độ, push, rồi xoá ngữ cảnh và làm tiếp việc kế trong `TIEN_DO.md`).

## Git / GitHub
- Remote: https://github.com/taelomuhicd84/PCCC-CLONE (nhánh `main`, repo **công khai**: không đưa hồ sơ khách, token, đơn giá vào repo).
- Commit ở mỗi mốc việc.
- **Gần hết usage: LUÔN tự làm, không chờ nhắc** (người dùng dặn 2026-10-31, nhắc lại 2026-11-01): kiểm bằng công cụ `get_usage`
  (ccd_session_mgmt) sau mỗi việc lớn; cửa sổ 5 giờ hoặc tuần ≥ 85% thì **dừng ngay ở việc đang làm**, ghi `TIEN_DO.md` (dở ở đâu,
  bước tiếp theo cụ thể) + `LICH_SU.md`, commit, push (skill `tkpccc-xong-dau-muc`).
- Push từ Bash của Claude có thể không lấy được đăng nhập Git Credential Manager: chạy `git push` trong tab Terminal của người dùng.
