# Việc 3 + 4 (người dùng test 13/11): panel PCCC_BLOCK mới, PCCC_HELP thành panel

Nhánh `claude/cad-panel-ui-ca1a1e` (worktree `.claude/worktrees/cad-panel-ui-ca1a1e`), chưa merge vào `main`, chưa push.

## Đã làm (13/11)
- `cad/module/ui/GiaoDien.cs` (mới): kiểu chung của các bảng: tiêu đề nền xanh, ô tìm có chữ gợi ý, bảng dòng cao kẻ ngang,
  khung "Hành động" bên phải, nút xanh đậm bo góc (`NutBo`), ô tổng số (`OTong`), nút tròn màu đầu dòng, `GuiLenh` (chạy việc sau khi lệnh xong lúc AutoCAD rảnh).
- `cad/module/ui/BangBlock.cs` (làm lại): DataGridView nhóm theo hệ (dòng tiêu đề nhóm nền xanh), cột nút tròn (xanh + = `PCCC_DAT <mã>`, cam R = `PCCC_RAI <mã>`),
  ký hiệu lớn, tên đậm + mã xám, đơn vị, số lượng trong bản vẽ (đỏ khi > 0). Khung Hành động: Đặt thiết bị, Rải theo lưới, Thêm từ bản vẽ, Đếm lại,
  Nạp lại thư viện; ô chọn hệ; ô "Chỉ block có trong bản vẽ"; ảnh xem trước; ô tổng "Thiết bị trong bản vẽ". Bấm đúp vẫn ra `PCCC_DAT <mã>`.
  Số lượng đếm bằng `Lenh.DemBanVe` (mới, `cad/module/plugin/Block.cs`, cùng cách đếm với `PCCC_BOC`), đếm lại sau lệnh đặt/rải và khi đổi bản vẽ.
- `cad/module/ui/BangLenh.cs` (mới): `PCCC_HELP` mở bảng lệnh (tên, phím tắt, việc làm, cách dùng), tìm được, nút tròn chạy lệnh,
  sửa ô Phím tắt rồi Enter là lưu. Phím tắt ghi `phim_tat.txt` cạnh DLL (`PHIMTAT=LENH`), lúc nạp plugin đăng ký thành lệnh thật
  (`Autodesk.AutoCAD.Internal.Utils.AddCommand`, nhóm `TKPCCC_PHIMTAT`). KHÔNG sửa `acad.pgp`; từ chối phím tắt trùng lệnh AutoCAD hoặc trùng dòng trong `acad.pgp`.
- `cad/module/plugin/Lenh.cs`: danh sách lệnh thành `Lenh.DS_LENH` (tên, việc làm, cách dùng; thêm `PCCC_SPEC`, `PCCC_HELP`, `PCCC_BLOCK`).
  `PCCC_HELP` gọi `Lenh.MoBangLenh` khi có giao diện, không thì in chữ như cũ (accoreconsole). Thêm lệnh mới: thêm một dòng vào `DS_LENH`.
- `cad/test/thu_gui.ps1`: kiểm bảng block theo `AccessibleName` "Block: n" (DataGridView không còn LVM), 6 nút; ghi `phim_tat.txt` PHX=PCCC_HELP,
  kịch bản gõ PHX, kiểm bảng lệnh hiện đủ dòng; chụp ảnh 2 bảng (PrintWindow) vào thư mục thử.

## Trạng thái kiểm
- `cad\build.bat`: đạt.
- `chay_thu.ps1`: TAT CA DAT (79 dòng DAT); `thu_mcp.ps1`: TAT CA DAT (13/11).
- `thu_gui.ps1`: CHƯA chạy được: AutoCAD của người dùng đang mở bản vẽ (không tự tắt).

## Việc tiếp theo
1. Khi không còn AutoCAD nào mở: `powershell -ExecutionPolicy Bypass -File cad\test\thu_gui.ps1`, xem 2 ảnh `panel_block.png`, `panel_lenh.png` trong thư mục thử, gửi người dùng.
2. Merge nhánh vào `main` (phiên chính quyết định; phiên `cad-rai-cach-tuong` có thể sửa prompt `PCCC_RAI`: nút R gửi `PCCC_RAI\n<mã>\n`, cần giữ câu hỏi mã đầu tiên).
