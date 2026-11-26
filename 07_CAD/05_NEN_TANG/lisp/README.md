# 07_CAD/05_NEN_TANG/lisp — LISP cho AutoCAD

Chỗ đặt file `.lsp` của người dùng (lệnh tắt, tiện ích vẽ quen dùng). Nạp tay trong AutoCAD bằng `APPLOAD`.

- Máy chủ MCP cố ý KHÔNG chạy LISP: AI chỉ gọi được các thao tác trong `07_CAD/01_TRI_THUC_VE/TU_VUNG.md`. Muốn AI dùng được một việc đang làm bằng LISP thì viết thành thao tác trong `07_CAD/06_VE_TU_DONG_AI/cad/Spec.cs`.
- Mỗi file `.lsp` thêm vào đây: ghi 1 dòng bên dưới (tên file, lệnh nó tạo, làm gì).

| File | Lệnh | Làm gì |
|---|---|---|
| `TKPCCC.lsp` | `TKPCCC` | Nạp `TKPCCC.Cad.dll` thay cho gõ `NETLOAD` (một lần mỗi phiên AutoCAD). `APPLOAD` file này, thêm vào Startup Suite để tự nạp. Tìm DLL theo biến môi trường `TKPCCC_DLL` (do `07_CAD\cai_dat.bat` đặt) → bundle trong `ApplicationPlugins` → Support File Search Path. Đã thử bằng accoreconsole 15/11; đường Startup Suite trong AutoCAD có giao diện chưa thử. |
