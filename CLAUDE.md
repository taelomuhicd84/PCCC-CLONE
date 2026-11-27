# TKPCCC — hướng dẫn cho Claude Code

Bộ công cụ thiết kế PCCC: plugin AutoCAD 2021 + runner Node + web nội bộ + skill Claude. Người dùng là kỹ sư PCCC, giao tiếp **tiếng Việt**.
**Phiên mới: đọc `00_QUAN_LY_DU_AN/TIEN_DO.md` trước** (việc đang làm, việc tiếp theo). Cấu trúc thư mục: `00_QUAN_LY_DU_AN/STRUCTURE.md`. Các pha: `00_QUAN_LY_DU_AN/ROADMAP.md`. Nhật ký: `00_QUAN_LY_DU_AN/LICH_SU.md`.
**Cây gốc đánh số** (người dùng giao 2026-11-25, theo mẫu danh mục công việc DA-GT-2026-01): `00_QUAN_LY_DU_AN` · `01_TRI_THUC` (quy chuẩn nguyên văn) ·
`02_DIEU_KHOAN` (điều khoản theo chuyên đề) · `03_DU_LIEU` · `04_BACKEND` · `05_FRONTEND` · `06_THAM_DINH_PDF` · `07_CAD` · `08_BANG_TINH` · `09_SCRIPTS` ·
`10_DRIVE` · `11_TIEN_ICH` · `12_KET_QUA` (không commit). Gốc chỉ giữ file công cụ bắt buộc (`CLAUDE.md`, `README.md`, `.claude/`, `.mcp.json`, `package.json`,
`vitest.config.ts`, `chay.bat`). Mảng mới lấy số kế tiếp, không chèn giữa. Trong `.claude/skills/<skill>/` vẫn là `scripts/`, `data/`, `test/` riêng của skill.

## QUY TẮC SỐ 1, ÁP DỤNG MÃI MÃI: gần hết usage thì lưu và đẩy GitHub, không chờ nhắc
(người dùng dặn 2026-10-31, nhắc lại 2026-11-01 và 2026-11-03 "ghi nhớ mãi mãi")
- Kiểm `get_usage` (ccd_session_mgmt) đầu phiên, sau mỗi việc lớn, trước khi mở agent và giữa các lượt agent.
- Cửa sổ 5 giờ (hoặc tuần) ≥ 85 % ⇒ **dừng ngay ở việc đang làm**: ghi `00_QUAN_LY_DU_AN/TIEN_DO.md` (dở ở đâu, bước tiếp theo cụ thể, đủ để phiên mới
  làm tiếp không cần hội thoại này) + `00_QUAN_LY_DU_AN/LICH_SU.md`, commit, push (skill `tkpccc-xong-dau-muc`); báo người dùng giờ reset.
- Dự tính trước: việc sắp làm có thể đẩy usage qua 85 % (vd mở agent ≈ 10–25 % mỗi agent) thì lưu + push TRƯỚC khi làm.
- Việc CAD (thư mục `07_CAD/`, người dùng dặn 2026-11-10): tiến độ và nhật ký lưu RIÊNG ở `07_CAD/00_QUAN_LY_DU_AN/TIEN_DO.md`, `07_CAD/00_QUAN_LY_DU_AN/LICH_SU.md`
  (`00_QUAN_LY_DU_AN/TIEN_DO.md` gốc chỉ trỏ tới); tới 95 % (người dùng dặn 2026-11-14, thay mức 90 % ngày 11/11 cho việc CAD) thì dừng, ghi 2 file đó, commit, push, hẹn giờ tới lúc reset rồi TỰ làm tiếp, không hỏi người dùng.
  `07_CAD/` là dự án con (người dùng dặn 2026-11-11): luật riêng `07_CAD/CLAUDE.md`, bản đồ thư mục `07_CAD/README.md`, skill và agent riêng trong `07_CAD/.claude/`.
- Cập nhật 2026-11-16 (người dùng dặn, thắng mọi mức ở dưới): cửa sổ 5 giờ tới 95–98 % ⇒ dừng ở task gần nhất, ghi tiến độ + lịch sử, commit;
  khi usage reset lần tiếp theo thì phân nhánh phiên, xoá đoạn chat của phiên nhánh mới (= `start_session` context "fresh", brief trỏ tới TIEN_DO) và làm tiếp, KHÔNG hỏi quyền.
  Cách hẹn: lúc dừng tạo luôn phiên nhánh fresh, brief bảo nó CronCreate một lần vài phút sau `resetsAt` rồi mới làm.
- Cập nhật 2026-11-15, áp cho MỌI việc (thắng các mức ở trên khi mâu thuẫn): cửa sổ 5 giờ tới 85–95 % mà chưa xong ⇒ dừng ở task gần nhất,
  ghi tiến độ + lịch sử (đủ để làm tiếp), commit, hẹn giờ (CronCreate một lần, vài phút sau `resetsAt`) rồi TỰ làm tiếp khi reset, không hỏi,
  không xin quyền; lặp lại tới khi xong. Ngữ cảnh phiên > 50 % ⇒ đẩy việc sang phiên nhánh (mỗi agent một phiên nhánh, chạy song song;
  phiên nhánh > 50 % thì compact); không đẩy được thì compact phiên rồi chạy tiếp.
  Phiên nhánh thừa hưởng model app ghi cho phiên mẹ: cần model khác thì `set_session_model` TRƯỚC khi gửi việc. Mỗi đợt chỉ mở 2–3 phiên
  (15/11: sáu phiên review song song tốn ≈ 78 % cửa sổ 5 giờ).

## Mỗi module là một skill (người dùng dặn 2026-11-02: sửa module nào thì chỉ vào đó, đỡ tốn token)
Làm module nào: đọc `.claude/skills/<skill>/SKILL.md` của module đó và các file nó liệt kê; không đọc cả repo.
| Module | Skill | Agent |
|---|---|---|
| Web nội bộ: giao diện `05_FRONTEND/` (6 tab) và server + lõi `04_BACKEND/` | `tkpccc-bao-gia`, `tkpccc-thuy-luc`, `tkpccc-huong-dan` | `web-frontend`, `web-backend` |
| Khảo sát – tư vấn PCCC (bảng KS + comment, phương án trên PDF) | `tkpccc-khao-sat-tu-van` | |
| Thẩm định PCCC: xây dựng (bảng KT) + hệ thống (bảng A, B) | `tkpccc-tham-dinh` | `pccc-xay-dung`, `pccc-he-thong` |
| Bảng đối chiếu A/B/KT/KS: chọn, lập, gộp, kiểm, xuất Excel | `tkpccc-bang-doi-chieu` | |
| Ghi chú + vẽ phương án lên PDF (annotation thật) | `tkpccc-ghi-chu-ban-ve` | |
| Máy kiểm luật PDF (`06_THAM_DINH_PDF/`, tab Thẩm định PDF) | `tkpccc-kiem-luat-pdf` | |
| Thủy lực TCVN 7336 | `tkpccc-thuy-luc` | |
| Khối lượng, báo giá, tab Khảo sát web | `tkpccc-bao-gia` | |
| Hướng dẫn thiết kế B0–B7 | `tkpccc-huong-dan` | |
| Plugin AutoCAD: build, kiểm thử, thêm lệnh và thao tác spec | `tkpccc-cad-console` (ở `07_CAD/.claude/skills/`) | |
| Vẽ CAD tự động qua MCP CAD (`cad_tt`, `cad_chay`, spec ngắn) | `tkpccc-ve-cad` (ở `07_CAD/.claude/skills/`) | |
| Tra quy chuẩn (chọn file, chỉ văn bản hiện hành) | `tkpccc-tra-quy-chuan` | |
| Khép đầu mục: kiểm → TIEN_DO/LICH_SU → commit → push | `tkpccc-xong-dau-muc` | |
| Phối hợp Gemini + Claude: Gemini CLI bóc PDF dài/scan, Claude đọc JSON nhỏ | `tkpccc-phoi-hop-model` | |
| Quản lý Google Drive / OneDrive: file đã up, lịch sử, chia sẻ, cây hồ sơ, xếp file (`10_DRIVE/`) | `tkpccc-quan-ly-drive` | |
- Model + effort (người dùng dặn 2026-11-03: việc phức tạp cần tỉ mỉ, chính xác cực cao → Opus effort high; việc nhẹ → Sonnet effort thấp)
  ghi trong frontmatter `model:`, `effort:` của SKILL.md và agent; nguồn duy nhất là frontmatter. Skill phụ trợ gọi giữa luồng
  (`tkpccc-bang-doi-chieu`, `tkpccc-ghi-chu-ban-ve`, `tkpccc-tra-quy-chuan`) KHÔNG đặt: model của skill áp cho cả phần còn lại của lượt.
- Module mới: tạo `.claude/skills/tkpccc-<tên>/` (`SKILL.md`; module chỉ Claude dùng thì để luôn `scripts/`, `data/`, `test/` trong đó), thêm 1 dòng
  vào bảng trên và `00_QUAN_LY_DU_AN/STRUCTURE.md`. Code chạy trong web vẫn ở `04_BACKEND/` (gồm `04_BACKEND/shared/`), `05_FRONTEND/`, `06_THAM_DINH_PDF/`, `07_CAD/` nhưng `SKILL.md` của module phải liệt kê.
- Quy trình nào lặp lại và hay thì gom thành skill (người dùng dặn 2026-10-31).

## Quy tắc chung
- Đối chiếu (khảo sát, thẩm định) LUÔN ra BẢNG ĐỐI CHIẾU (người dùng dặn 2026-11-03): file `bang_doi_chieu.md` + Excel ĐỦ dòng, gửi bằng
  SendUserFile; KHÔNG dán bảng vào chat (tốn token output, người dùng dặn 2026-11-03, thay luật 2026-11-02). Chat chỉ: đường dẫn file,
  đếm theo kết luận, điểm cần người dùng xem xét, số liệu cần bổ sung.
- Tư vấn, thẩm định: Claude chỉ BÁO CÁO (nhận diện, đánh giá, phương án, khả thi, căn cứ), không hỏi người dùng chọn; luôn ghi chú lên PDF.
  Chi tiết và các bài học đã train nằm trong skill `tkpccc-khao-sat-tu-van`, `tkpccc-tham-dinh`.
- Không trùng lắp, không làm việc thừa (người dùng dặn 2026-11-03): một số liệu một chỗ rồi link tới; chỉ làm, chỉ tra phần công việc
  yêu cầu và đối tượng có thật (không có hệ thống thì không tra chi tiết hệ thống đó); kết luận biết trước thì điền hàng loạt, không giao agent.
- Tri thức/điều khoản chép nguyên văn vào `01_TRI_THUC/<NN_HANG_MUC>/` hoặc `02_DIEU_KHOAN/<NN_HANG_MUC>/` (12 hạng mục, số chung, danh sách ở
  `00_QUAN_LY_DU_AN/STRUCTURE.md`), không viết theo trí nhớ; giữ tên file.
- Gọi Claude **không dùng API key**: `04_BACKEND/src/engines/claude.ts` spawn `claude -p` bằng đăng nhập CLI, chỉ công cụ đọc, không nạp hook/plugin cá nhân.
- `03_DU_LIEU/vat_tu.json` là nguồn DUY NHẤT của danh mục vật tư (plugin + báo giá).
- Số người dùng nhập kiểu Việt Nam ("1.200" = 1200, "3,5" = 3.5): đọc bằng `docSoVN` (`04_BACKEND/shared/src/bao_gia.ts`).
- Plugin AutoCAD: **chỉ C# 5**, chỉ tham chiếu `acdbmgd` + `accoremgd` (chi tiết skill `tkpccc-cad-console`).
- Kiểm tra trước khi báo xong: `npm test`, `python -m pytest 06_THAM_DINH_PDF/test .claude/skills 10_DRIVE/test -q` và
  `powershell -ExecutionPolicy Bypass -File 07_CAD\04_KIEM_THU\chay_thu.ps1` (không cần mở AutoCAD, nhưng máy phải cài AutoCAD 2021).
- Repo tham khảo (công khai từ 2026-11-01): `taelomuhicd84/SKILL-PCCC-NEW` (web thẩm định theo bảng đối chiếu A/B/KT, agent m01–m18,
  thủy lực), `taelomuhicd84/PcccVipPro` (module `pccc`: agent thẩm định, `tools/thuy_luc.py`, `thoat_nan.py`), `taelomuhicd84/PCCC`
  (lõi Python gốc). Clone vào thư mục tạm khi cần, không sửa các repo đó, không chạy code của chúng.
- Không commit `12_KET_QUA/`, `03_DU_LIEU/don_gia.json`, DLL build. `PCCC_Blocks.dwg`, `block_alias.json` (người dùng sửa) ĐƯỢC commit.
- File `.bat`/`.ps1` chỉ dùng chữ ASCII; `.ps1` buộc có chữ Việt thì lưu UTF-8 có BOM (như `09_SCRIPTS/chen_hang_cuoi.ps1`).

## Nhịp làm việc (người dùng dặn 2026-10-31)
**Xong mỗi đầu mục: chạy skill `tkpccc-xong-dau-muc`** (lưu tiến độ, push, rồi xoá ngữ cảnh và làm tiếp việc kế trong `00_QUAN_LY_DU_AN/TIEN_DO.md`).

## Git / GitHub
- Remote: https://github.com/taelomuhicd84/PCCC-CLONE (nhánh `main`, repo **công khai**: không đưa hồ sơ khách, token, đơn giá vào repo).
- Commit ở mỗi mốc việc.
- Push từ Bash / PowerShell của Claude (kể cả Bot chat CAD, người dùng cho phép 27/11) chạy được khi Git Credential Manager đã lưu đăng nhập; báo lỗi đăng nhập thì chạy `git push` trong tab Terminal của người dùng.
