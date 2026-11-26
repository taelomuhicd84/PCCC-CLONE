---
name: tkpccc-xong-dau-muc
description: Quy trình khép một đầu mục việc trong TKPCCC — kiểm tra, cập nhật 00_QUAN_LY_DU_AN/TIEN_DO.md / 00_QUAN_LY_DU_AN/LICH_SU.md / 00_QUAN_LY_DU_AN/STRUCTURE.md, commit, push GitHub, rồi xoá ngữ cảnh và làm tiếp việc kế trong 00_QUAN_LY_DU_AN/TIEN_DO.md. Dùng khi xong một pha hoặc một việc lớn, khi người dùng nói "xong rồi", "lưu tiến độ", "đẩy github", "clear rồi làm tiếp", và BẮT BUỘC khi usage gói Claude gần hết (cửa sổ 5 giờ hoặc tuần từ khoảng 85%).
model: sonnet
effort: low
---

# Khép đầu mục: kiểm → ghi tiến độ → commit → push → ngữ cảnh mới làm tiếp

Người dùng dặn (2026-10-31): xong mỗi đầu mục thì lưu tiến độ, đẩy GitHub, **xoá ngữ cảnh rồi làm tiếp**; gần hết usage cũng lưu + đẩy
ngay. Lý do: hết usage hoặc ngữ cảnh quá dài giữa chừng thì mất mạch; phiên sau (hoặc máy khác) chỉ cần đọc repo là làm tiếp được.

## 1. Kiểm tra (không báo xong khi chưa chạy)
- `npm test` (04_BACKEND/shared/test + runner/test).
- `python 09_SCRIPTS/kiem_lien_ket.py`: đường dẫn chết, mã vật tư/bảng, trị số rải, lệnh CAD, model skill/agent lệch nhau. Mục LỖI phải = 0; CẢNH BÁO ghi vào TIEN_DO nếu mới.
- Có sửa `07_CAD/`: `powershell -ExecutionPolicy Bypass -File 07_CAD\04_KIEM_THU\chay_thu.ps1` → `TAT CA DAT` (xem skill `tkpccc-cad-console`).
- Có sửa 05_FRONTEND / 04_BACKEND: bật runner (`npm start`, chạy nền) và thử luồng vừa sửa trên trình duyệt tích hợp hoặc bằng `fetch`.
- Dọn dữ liệu thử mình tạo trong `12_KET_QUA/`, file rác (`plot.log`…); không xoá dữ liệu người dùng.

## 2. Ghi tiến độ
- `00_QUAN_LY_DU_AN/TIEN_DO.md`: mục "ĐANG LÀM / VIỆC TIẾP THEO" ở đầu — đã xong gì, việc người dùng cần làm (👤), việc tiếp theo đánh số, vấn đề mở.
  Viết để một phiên MỚI không đọc hội thoại này vẫn làm tiếp được.
- `00_QUAN_LY_DU_AN/LICH_SU.md`: thêm gạch đầu dòng dưới ngày hôm nay (đã làm, đã sửa lỗi gì, quyết định của người dùng).
- `00_QUAN_LY_DU_AN/ROADMAP.md`: đổi trạng thái pha. `00_QUAN_LY_DU_AN/STRUCTURE.md`: cập nhật nếu thêm/đổi/xoá thư mục.

## 3. Commit + push
- `git add -A`, xem `git status --short` (không có `12_KET_QUA/`, `03_DU_LIEU/don_gia.json`, DLL, token), commit tiếng Việt có dòng cuối
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.
- Push: thử `git push origin main` từ Bash trước (máy `F:\1_ToolMMo\2_VibeCode\PCCCVIP` đẩy được từ Bash). Nếu báo `could not read Username`
  (Bash không lấy được đăng nhập Git Credential Manager) ⇒ chạy `git -C <thư mục dự án> push` trong tab Terminal của người dùng
  (`run_in_terminal`), đọc kết quả bằng `read_terminal`. Cuối cùng xác nhận `git status -sb` không còn "ahead".
- Máy mới chưa có danh tính git: `git config user.name trantrongtien198`, `git config user.email trantrongtien198@gmail.com` (chỉ trong repo).
- Repo https://github.com/taelomuhicd84/PCCC-CLONE là **công khai**: không đưa hồ sơ khách, đơn giá, token, cookie lên.

## 4. Usage
`mcp__ccd_session_mgmt__get_usage`: cửa sổ 5 giờ hoặc tuần ≥ ~85% ⇒ làm xong bước 2–3 ngay, báo người dùng giờ reset, dừng việc lớn.

## 5. Ngữ cảnh mới, làm tiếp
`clear_session("self")` xoá được ngữ cảnh nhưng sau đó phiên đứng chờ, không tự làm tiếp. Vì vậy:
- Còn usage: mở phiên mới ngữ cảnh sạch bằng `start_session` (`context: "fresh"`, `use_worktree: false`, cùng thư mục) với brief:
  việc kế trong `00_QUAN_LY_DU_AN/TIEN_DO.md`, dặn đọc `CLAUDE.md` → `00_QUAN_LY_DU_AN/TIEN_DO.md` → `00_QUAN_LY_DU_AN/STRUCTURE.md` trước và dùng skill này khi xong. Sau đó
  `clear_session("self")` cho phiên hiện tại (người dùng duyệt), báo người dùng link phiên mới.
- Usage gần hết: không mở phiên mới; ghi rõ trong `00_QUAN_LY_DU_AN/TIEN_DO.md` việc kế tiếp để lần sau gõ "làm tiếp" là chạy.
