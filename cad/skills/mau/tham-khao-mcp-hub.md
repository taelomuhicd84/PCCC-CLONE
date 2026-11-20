# Ý lấy từ tài liệu "Hướng dẫn kết nối & sử dụng AutoCAD MCP" (MCP Hub Pro 2026)

Nguồn: file người dùng gửi 2026-11-11 (`HD_KET_NOI_VA_DUNG_AUTOCAD_MCP.md`, tác giả ghi trong tài liệu: KS. Lê Nguyên Vũ). Đó là máy chủ MCP viết bằng Python,
8 nhóm công cụ (`drawing`, `system`, `view`, `entity`, `annotation`, `layer`, `block`, `pid`), nối AutoCAD bằng file trung gian LISP hoặc COM.
File này chỉ ghi Ý TƯỞNG đáng lấy và cách đưa vào TKPCCC. Không chép mã, không dùng máy chủ đó.

## Cái họ có, mình chưa có, nên làm
| Ưu tiên | Ý của tài liệu | TKPCCC hiện có | Việc thêm |
|---|---|---|---|
| 1 | `view.get_screenshot` + `zoom_extents`, `zoom_window`: vẽ xong AI NHÌN lại bản vẽ rồi mới báo | chưa có (`cad_anh` để lại ở G6); chỉ có `in_png.ps1` cho accoreconsole | Công cụ MCP `cad_anh` {`hop` hoặc toàn bản vẽ, `rong` px}: plugin in vùng đó ra PNG (lệnh `-PLOT` cửa sổ như `in_png.ps1`, đẩy qua `Cau.cs`), trả ĐƯỜNG DẪN file; AI chỉ đọc ảnh khi cần. Đây là vòng tự kiểm còn thiếu: dùng ngay để so `rai` + `tuyen` với PDF mẫu |
| 1 | `drawing.info`, `open`: AI biết và chọn đúng bản vẽ | `cad_tt` trả tên bản vẽ; `cad_chay` vẽ vào bản đang hiện, hôm 11/11 đã vẽ nhầm tab | Trường cấp gói `ban_ve` trong spec: tên file phải khớp bản vẽ đang hiện, lệch thì từ chối, không vẽ. Thao tác `mo` (mở DWG theo đường dẫn) làm sau |
| 2 | `layer`: tạo layer đủ màu, kiểu đường, độ dày nét | `DamBaoLayer` chỉ có tên + màu | Bảng layer chuẩn `cad/data/ky_hieu/layer.json` (lập từ file `.md` người dùng sẽ thả vào `cad/layer/`): tên, màu, kiểu đường, độ dày, in hay không. `DamBaoLayer` đọc bảng này. Dây báo cháy phải ra nét đứt như bản mẫu |
| 2 | `annotation.leader`: đường dẫn có mũi tên | chưa có | Thao tác `dan` (mũi tên + chữ): bản mẫu dùng cho "Zone 1 về tủ TTBC", "KẾT NỐI TRẠM BƠM", ghi chú van |
| 2 | `drawing.plot_pdf`, `save`: xuất hồ sơ | chưa có | Thao tác `in_pdf` (in layout hoặc cửa sổ ra PDF) và `luu`; làm cùng `cad_anh` vì chung đường `-PLOT` |
| 3 | `block.list`, `block.create` | `dat` (chèn từ thư viện), `PCCC_GAN` (gán block có sẵn) | `doc` thêm `gi`:"block" (tên block + số lượng trong bản vẽ) để AI gợi ý gán mã. Tạo block từ bản vẽ: đã nằm trong Bước 3 (bảng block, nút "Thêm từ bản vẽ") |
| 3 | `entity.arc`, `rectangle`, `hatch` | `net`, `tron` | `cung` (cung tròn: cánh cửa, ống cong) và `hatch` (tô tường, vùng) chỉ thêm khi vẽ lại nền kiến trúc cần tới |
| 3 | Hai đường nối (file + COM), `health`, biến `IPC_TIMEOUT` | hàng đợi file + `song.json` (cùng ý "file trung gian", đã chạy ổn) | Tự nối một chạm: `TKPCCC.Mcp.exe` thấy AutoCAD đang mở mà chưa có `song.json` thì tự gửi `NETLOAD` qua COM (late binding, không thêm thư viện), khỏi bắt người dùng gõ tay. Cho đặt thời gian chờ bằng biến môi trường |
| 3 | `pid`: ký hiệu thiết bị, ống, van cho sơ đồ công nghệ | chưa có (G5 `so_do`) | Khi làm `so_do`: lấy đúng bộ ký hiệu van của bản mẫu (OS&Y, FS, AAV, 1 chiều, giảm áp, báo động) |
| 3 | File cấu hình mẫu cho nhiều AI (Gemini, Antigravity, Claude Desktop) | `.mcp.json` cho Claude Code | Thêm vào `cad/README.md` đoạn cấu hình mẫu cho AI khác. Lợi thế của mình: một file `.exe`, không cần Python hay venv |

## Cách làm việc đáng lấy (đưa vào skill `tkpccc-ve-cad`)
- **Vẽ theo giai đoạn, mỗi giai đoạn có ảnh để duyệt** (prompt mẫu số 4 của tài liệu): nền + trục → rải thiết bị → đi tuyến → ghi chú + bảng. Hết mỗi giai đoạn gọi `cad_anh`, báo số đếm, rồi mới làm tiếp. Với TKPCCC: người dùng đã dặn tự làm không hỏi, nên chỉ dừng chờ duyệt khi người dùng yêu cầu.
- **Kiểm trước khi vẽ**: hỏi bản vẽ đang mở, số đối tượng, layer (đã có `cad_tt`), rồi mới vẽ.
- **Bộ câu lệnh mẫu cho người dùng**: viết 4–5 câu mẫu PCCC vào `cad/README.md` (kiểm bản vẽ; rải đầu báo một tầng; đi ống sprinkler theo ống chính; nhập PDF kiến trúc; xuất PDF + ảnh).

## Cái KHÔNG lấy, và lý do
- `system.execute_lisp` (AI viết và chạy LISP tự do): trái luật của dự án (`cad/CLAUDE.md`: máy chủ MCP không chạy LISP hay lệnh tự do). Tốn token vì AI phải viết từng nét, khó kiểm, và mở đường chạy lệnh tuỳ ý trên máy. TKPCCC giữ cách "spec ngắn, plugin tự sinh hình".
- Máy chủ Python + venv: mình đã có `TKPCCC.Mcp.exe` bằng C#, không phải cài gì thêm.
- Công cụ vẽ từng nét làm công cụ MCP riêng (`entity`, `annotation` tách nhiều công cụ): mình gộp mọi thao tác vào MỘT lần gọi `cad_chay` để ít lượt gọi.

## Thứ tự đưa vào kế hoạch
Sau Bước 1 (`rai`, `tuyen`): làm ngay hai việc ưu tiên 1 (`cad_anh`, khoá `ban_ve`) vì chúng dùng để kiểm mọi bước sau. Ưu tiên 2 làm cùng lúc người dùng cấp file chuẩn layer.
Ưu tiên 3 xếp sau Bước 2–4 trong `cad/TIEN_DO.md`.
