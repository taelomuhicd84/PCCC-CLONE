# 07_CAD/ — dự án con AutoCAD của TKPCCC

Luật chung nằm ở `CLAUDE.md` gốc repo và vẫn áp dụng. File này chỉ thêm phần riêng của CAD.
Mở Claude Code tại gốc repo hay ngay tại `07_CAD/` đều làm việc được (`07_CAD/.mcp.json`, `07_CAD/.claude/`).

- **Phiên mới: đọc `07_CAD/TIEN_DO.md` trước.** Bản đồ thư mục và "sửa gì vào đâu": `07_CAD/README.md`. Kế hoạch tổng: `07_CAD/KE_HOACH.md`. Nhật ký: `07_CAD/LICH_SU.md`.
- Cập nhật 2026-11-16 (thắng dòng dưới): tới 95–98 % dừng task gần nhất, ghi `07_CAD/TIEN_DO.md` + `07_CAD/LICH_SU.md`, commit; reset thì làm tiếp ở phiên nhánh ngữ cảnh sạch, không hỏi (luật đủ ở `CLAUDE.md` gốc).
- Tới 95 % hạn mức (người dùng dặn 2026-11-14, thay mức 90 % ngày 11/11; cấp toàn quyền tự làm tiếp): dừng, ghi `07_CAD/TIEN_DO.md` + `07_CAD/LICH_SU.md`, commit, push, hẹn giờ tới lúc reset rồi TỰ làm tiếp việc trong `07_CAD/TIEN_DO.md`, không hỏi người dùng.
- Skill riêng của CAD ở `07_CAD/.claude/skills/`: `tkpccc-ve-cad` (vẽ qua MCP), `tkpccc-cad-console` (build, kiểm thử, thêm lệnh). Agent riêng ở `07_CAD/.claude/agents/`
  (model và effort ghi ở frontmatter từng file, nguồn duy nhất). Người dùng chốt tối 2026-11-11 (thay các lời dặn trước trong ngày): VIẾT CODE bằng Opus (`cad-lap-trinh`; effort do Claude chọn theo việc:
  high cho thuật toán và thao tác mới, medium cho sửa nhỏ); REVIEW, kiểm tra bằng Opus effort max (`cad-review`, một lần cuối mỗi mốc; người dùng dặn 2026-11-12 bỏ Fable); chạy dự án thật thì chia việc cho từng agent,
  model hợp công việc, hợp token mà vẫn giữ chính xác. Phiên chính chỉ điều phối:
  | Agent | Việc | Được làm |
  |---|---|---|
  | `cad-thiet-ke` | đọc nền, tra trị số theo `rule/LUAT.md`, phân vùng, chia zone, lập spec cả tầng | đọc file, ghi spec + báo cáo vào thư mục dự án; không có công cụ vẽ |
  | `cad-ve` | chạy spec có sẵn qua `cad_chay`, sửa lỗi cú pháp nhỏ, báo số đếm | `cad_tt`, `cad_chay`, `cad_anh`, đọc file |
  | `cad-kiem` | soát bản vẽ sau khi vẽ: đếm, vùng hở, zone, đối chiếu luật | chỉ `doc`, `dem`, `anh`; ghi báo cáo kiểm |
  | `cad-lap-trinh` | sửa plugin C#, thêm thao tác spec và lệnh, chạy bộ test | sửa `07_CAD/module`, `07_CAD/test`, tài liệu CAD |
  | `cad-review` | review diff của một mốc + chạy lại test, báo CHẶN / NÊN SỬA / GHI CHÚ | chỉ đọc và chạy test, không sửa file |
- Cây thư mục cố định, thêm gì thì đặt đúng chỗ và ghi 1 dòng vào `07_CAD/README.md`:
  `module/` mã chạy · `data/` dữ liệu vẽ · `rule/` luật nối tới quy chuẩn · `skills/` ĐẦU NGUỒN tri thức vẽ cho mọi AI (`nhan_dang/` đọc bản vẽ, `ve/` vẽ + quy ước layer, `mau/` bằng chứng học từ bản mẫu; mục lục `skills/README.md`) · `test/` kiểm thử.
  Người dùng chốt 21/11: luật nhận dạng / vẽ chỉ ghi ở `07_CAD/skills/`; skill, agent, memory chỉ giữ quy trình và trỏ tới đó.
- Trị số quy chuẩn không chép vào `07_CAD/`: `rule/LUAT.md` chỉ trỏ tới file nguyên văn trong `01_TRI_THUC/`, `02_DIEU_KHOAN/` gốc.
- Plugin chỉ C# 5, tham chiếu `acdbmgd` + `accoremgd` (cộng `System.Windows.Forms`, `System.Drawing` của .NET). Máy chủ MCP không chạy LISP hay lệnh tự do.
  Riêng `module/ui/` (`TKPCCC.UI.dll`) được tham chiếu `acmgd`: chỉ chứa giao diện, mọi xử lý bản vẽ để ở `module/plugin/` cho thử được trong accoreconsole.
- Trước khi báo xong việc CAD: `07_CAD\test\chay_thu.ps1` và `07_CAD\test\thu_mcp.ps1` phải ra `TAT CA DAT`.
  Sửa giao diện hoặc cầu nối thì chạy thêm `07_CAD\test\thu_gui.ps1` (tự mở AutoCAD thật 1–2 phút rồi tự tắt; chỉ khi không có AutoCAD nào đang mở).
