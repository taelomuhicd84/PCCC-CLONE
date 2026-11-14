# cad/ — dự án con AutoCAD của TKPCCC

Luật chung nằm ở `CLAUDE.md` gốc repo và vẫn áp dụng. File này chỉ thêm phần riêng của CAD.
Mở Claude Code tại gốc repo hay ngay tại `cad/` đều làm việc được (`cad/.mcp.json`, `cad/.claude/`).

- **Phiên mới: đọc `cad/TIEN_DO.md` trước.** Bản đồ thư mục và "sửa gì vào đâu": `cad/README.md`. Kế hoạch tổng: `cad/KE_HOACH.md`. Nhật ký: `cad/LICH_SU.md`.
- Tới 95 % hạn mức (người dùng dặn 2026-11-14, thay mức 90 % ngày 11/11; cấp toàn quyền tự làm tiếp): dừng, ghi `cad/TIEN_DO.md` + `cad/LICH_SU.md`, commit, push, hẹn giờ tới lúc reset rồi TỰ làm tiếp việc trong `cad/TIEN_DO.md`, không hỏi người dùng.
- Skill riêng của CAD ở `cad/.claude/skills/`: `tkpccc-ve-cad` (vẽ qua MCP), `tkpccc-cad-console` (build, kiểm thử, thêm lệnh). Agent riêng ở `cad/.claude/agents/`
  (model và effort ghi ở frontmatter từng file, nguồn duy nhất). Người dùng chốt tối 2026-11-11 (thay các lời dặn trước trong ngày): VIẾT CODE bằng Opus (`cad-lap-trinh`; effort do Claude chọn theo việc:
  high cho thuật toán và thao tác mới, medium cho sửa nhỏ); REVIEW, kiểm tra bằng Opus effort max (`cad-review`, một lần cuối mỗi mốc; người dùng dặn 2026-11-12 bỏ Fable); chạy dự án thật thì chia việc cho từng agent,
  model hợp công việc, hợp token mà vẫn giữ chính xác. Phiên chính chỉ điều phối:
  | Agent | Việc | Được làm |
  |---|---|---|
  | `cad-thiet-ke` | đọc nền, tra trị số theo `rule/LUAT.md`, phân vùng, chia zone, lập spec cả tầng | đọc file, ghi spec + báo cáo vào thư mục dự án; không có công cụ vẽ |
  | `cad-ve` | chạy spec có sẵn qua `cad_chay`, sửa lỗi cú pháp nhỏ, báo số đếm | `cad_tt`, `cad_chay`, `cad_anh`, đọc file |
  | `cad-kiem` | soát bản vẽ sau khi vẽ: đếm, vùng hở, zone, đối chiếu luật | chỉ `doc`, `dem`, `anh`; ghi báo cáo kiểm |
  | `cad-lap-trinh` | sửa plugin C#, thêm thao tác spec và lệnh, chạy bộ test | sửa `cad/module`, `cad/test`, tài liệu CAD |
  | `cad-review` | review diff của một mốc + chạy lại test, báo CHẶN / NÊN SỬA / GHI CHÚ | chỉ đọc và chạy test, không sửa file |
- Cây thư mục cố định, thêm gì thì đặt đúng chỗ và ghi 1 dòng vào `cad/README.md`:
  `module/` mã chạy · `data/` dữ liệu vẽ · `layer/` chuẩn layer (đọc trước khi tạo layer) · `rule/` luật nối tới quy chuẩn · `knowledge/` kiến thức vẽ · `skills/` hướng dẫn cho mọi AI · `test/` kiểm thử.
- Trị số quy chuẩn không chép vào `cad/`: `rule/LUAT.md` chỉ trỏ tới file nguyên văn trong `knowledge/`, `skills/` gốc.
- Plugin chỉ C# 5, tham chiếu `acdbmgd` + `accoremgd` (cộng `System.Windows.Forms`, `System.Drawing` của .NET). Máy chủ MCP không chạy LISP hay lệnh tự do.
  Riêng `module/ui/` (`TKPCCC.UI.dll`) được tham chiếu `acmgd`: chỉ chứa giao diện, mọi xử lý bản vẽ để ở `module/plugin/` cho thử được trong accoreconsole.
- Trước khi báo xong việc CAD: `cad\test\chay_thu.ps1` và `cad\test\thu_mcp.ps1` phải ra `TAT CA DAT`.
  Sửa giao diện hoặc cầu nối thì chạy thêm `cad\test\thu_gui.ps1` (tự mở AutoCAD thật 1–2 phút rồi tự tắt; chỉ khi không có AutoCAD nào đang mở).
