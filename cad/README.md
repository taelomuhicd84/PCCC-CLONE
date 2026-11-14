# cad/ — dự án con AutoCAD của TKPCCC

Plugin AutoCAD 2021 và đường vẽ tự động qua MCP. Phiên mới đọc `TIEN_DO.md` trước; kế hoạch tổng ở `KE_HOACH.md`; nhật ký ở `LICH_SU.md`; luật riêng ở `CLAUDE.md`.

## Cây thư mục
```
cad/
├─ CLAUDE.md · README.md · KE_HOACH.md · TIEN_DO.md · LICH_SU.md   luật riêng, bản đồ, kế hoạch, tiến độ, nhật ký
├─ .mcp.json               khai máy chủ MCP "cad" khi mở Claude Code ngay tại cad/ (gốc repo có bản riêng)
├─ .claude/
│  ├─ agents/              cad-thiet-ke (lập spec) · cad-ve (chạy spec) · cad-kiem (soát bản vẽ) · cad-lap-trinh (sửa plugin) · cad-review (review mỗi mốc, Write chỉ để ghi báo cáo); bảng việc ở CLAUDE.md
│  └─ skills/              tkpccc-ve-cad (vẽ qua MCP; scripts/pdf_ti_le.py tính tỉ lệ trang PDF, scripts/chay_spec.ps1 chạy spec không cần mở AutoCAD) · tkpccc-cad-console (build, kiểm thử, in PNG; scripts/hinh_tu_pdf.py lấy hình ký hiệu từ nét vector PDF)
├─ skills/                 hướng dẫn dùng cho MỌI AI (Claude, Gemini, Codex): TU_VUNG.md (từ vựng spec, nguồn duy nhất) · vi_du/
├─ knowledge/              kiến thức vẽ CAD: quy-uoc-ban-ve.md · mau-ban-ve-pccc.md (cách rải, đi dây, đi ống của bản mẫu) · tham-khao-mcp-hub.md (ý lấy từ MCP bên khác) · so-do-nguyen-ly.md (cách vẽ sơ đồ nguyên lý `so_do`, chi tiết nào lấy từ mẫu, chi tiết nào tự đặt) · hoc/
├─ rule/                   LUAT.md: việc trên CAD ↔ file quy chuẩn nguyên văn ↔ skill, agent liên quan
├─ layer/                  file .md chuẩn layer (tên, màu, loại đường) để tạo layer đúng chuẩn · kien_truc.md (TUONG, VACH, CUA, DIM)
├─ data/                   dữ liệu vẽ (xem data/README.md)
│  ├─ ky_hieu/             ky_hieu.json: mặc định ký hiệu trục, kích thước, chữ (plugin đọc) · hinh_bc.json: hình block mặc định báo cháy lấy từ nét PDF hồ sơ thật (sinh bằng tkpccc-cad-console/scripts/hinh_tu_pdf.py)
│  ├─ block/               thư viện block, block động co, tê (chờ file của người dùng)
│  ├─ ong/ · bom/          kích thước ống + phụ kiện, kích thước bơm (chờ catalog)
│  └─ mau/                 bản vẽ mẫu, khung tên (chờ file của người dùng)
├─ module/                 mã chạy
│  ├─ plugin/              Lenh.cs (lệnh PCCC_*) · Spec.cs (PCCC_SPEC, thao tác spec) · Rai.cs (rai, tuyen, PCCC_RAI, PCCC_TUYEN) · NhanVung.cs (vung_tu_dong: tự dựng vùng phòng từ nét tường, bỏ trục) · KienTruc.cs (tuong, cua, kt_chuoi, PCCC_TRUC, PCCC_TUONG, PCCC_CUA) · Dim.cs (PCCC_ADIM, PCCC_CDIM) · SoDo.cs (so_do, sơ đồ nguyên lý) · Block.cs (PCCC_THEMBLOCK, ảnh xem trước) · Cau.cs (cầu nối) -> TKPCCC.Cad.dll
│  ├─ ui/                  BangBlock.cs (bảng PCCC_BLOCK) -> TKPCCC.UI.dll; chỗ DUY NHẤT được tham chiếu acmgd, không xử lý bản vẽ
│  ├─ mcp/                 Mcp.cs -> bin/TKPCCC.Mcp.exe (công cụ cad_tt, cad_chay, cad_anh)
│  ├─ chung/               HangDoi.cs: hàng đợi file dùng chung plugin + máy chủ MCP
│  └─ lisp/                LISP của người dùng (nạp tay bằng APPLOAD)
├─ test/                   chay_thu.ps1 (plugin + PCCC_SPEC) · thu_mcp.ps1 (máy chủ MCP), không cần mở AutoCAD · mau_kt.pdf (PDF thử 1:200, sinh bằng tao_pdf_mau.py) · thu_gui.ps1 (tự mở AutoCAD thật, không bắt buộc)
├─ build.bat               build DLL + exe, chép dữ liệu vào bundle
├─ bin/                    kết quả build máy chủ MCP (không commit)
└─ TKPCCC.bundle/          gói AutoCAD tự nạp: PackageContents.xml, Contents/ (DLL, PCCC_Blocks.dwg, block_alias.json)
```

## Vẽ tự động chạy thế nào
```
AI (Claude Code, VS Code, Antigravity...) -> bin\TKPCCC.Mcp.exe -> hàng đợi file -> lệnh PCCC_SPEC trong AutoCAD -> bản vẽ
```
AI chỉ viết một gói thao tác ngắn (spec JSON), plugin sinh hình. Một gói = một bước Undo. Kết quả trả về chỉ gồm số đếm và lỗi.

## Sửa gì thì vào đâu
| Muốn | Sửa file |
|---|---|
| Đổi cỡ chữ, bóng trục, tên và màu layer trục, khoảng cách kích thước mặc định | `data/ky_hieu/ky_hieu.json`, rồi chạy `build.bat` |
| Thêm hoặc đổi một thao tác vẽ (`luoi_truc`, `dat`, `ong`...) | `module/plugin/Spec.cs` (hàm `Op...` + 1 dòng trong `MotOp`) và `skills/TU_VUNG.md` |
| Đổi cách rải thiết bị, cách đi dây, đi ống (`rai`, `tuyen`, `PCCC_RAI`) | `module/plugin/Rai.cs`; cách vẽ gốc ở `knowledge/mau-ban-ve-pccc.md` |
| Đổi cách tự nhận diện tường / trục, dựng vùng phòng khi bấm điểm (`vung_tu_dong`, `PCCC_RAI` bấm điểm) | `module/plugin/NhanVung.cs` (từ khoá layer `LOP_BO`, `LOP_TUONG`, khe cửa `HO_CUA_M`) |
| Đổi câu hướng dẫn AI nhận lúc kết nối | phần đầu `skills/TU_VUNG.md`, trước dòng `---` (giữ dưới 1900 ký tự) |
| Đổi tên, mô tả công cụ MCP | `module/mcp/Mcp.cs`, hàm `CongCu` |
| Đổi cách vẽ sơ đồ nguyên lý (`so_do`) | `module/plugin/SoDo.cs`; cách vẽ, nguồn ở `knowledge/so-do-nguyen-ly.md` |
| Đổi cách in ảnh (`anh`, `cad_anh`): khổ, thiết bị in, chỗ lưu PNG | `module/plugin/Spec.cs`, hàm `InAnh` |
| Đổi cách nhập PDF làm nền (`pdf`) | `module/plugin/Spec.cs`, hàm `NhapPdf` |
| Đổi cách tính tỉ lệ trang PDF từ chuỗi kích thước | `.claude/skills/tkpccc-ve-cad/scripts/pdf_ti_le.py` (kiểm bằng `test/chay_thu.ps1`) |
| Đổi nhịp xem hàng đợi, điều kiện "AutoCAD đang rảnh" | `module/plugin/Cau.cs` |
| Đổi chỗ đặt hàng đợi | `module/chung/HangDoi.cs` hoặc biến môi trường `TKPCCC_CAD_HANG_DOI` |
| Lệnh gõ tay `PCCC_*` | `module/plugin/Lenh.cs` (xem skill `tkpccc-cad-console`) |
| Tường, vách, cửa (`tuong`, `cua`, `kt_chuoi`; lệnh `PCCC_TRUC`, `PCCC_TUONG`, `PCCC_CUA`) | `module/plugin/KienTruc.cs`; layer, dày, rộng cửa mặc định ở nhóm `kien_truc` của `data/ky_hieu/ky_hieu.json`, chuẩn `layer/kien_truc.md` |
| Dim tự động (`PCCC_ADIM`, `PCCC_CDIM`, lõi chuỗi dim của `kt_chuoi`) | `module/plugin/Dim.cs` |
| Thêm vật tư, thiết bị | `data/vat_tu.json` ở GỐC repo (dùng chung với báo giá), rồi `build.bat`, `PCCC_LIB` |
| Đưa block của mình (kể cả block động) vào thư viện dưới một mã mới | trong AutoCAD: `PCCC_THEMBLOCK` hoặc nút "Thêm từ bản vẽ" của bảng `PCCC_BLOCK`; mã nguồn `module/plugin/Block.cs` |
| Đổi bảng block (cột, ô lọc, nút) | `module/ui/BangBlock.cs`; cách vẽ ảnh xem trước ở `module/plugin/Block.cs`, hàm `VeAnhBlock` |
| Sửa hình block thiết bị | `TKPCCC.bundle/Contents/PCCC_Blocks.dwg` bằng BEDIT, rồi `PCCC_LIB` |
| Thêm một luật vẽ, nối tới quy chuẩn | 1 dòng trong `rule/LUAT.md` |
| Ghi bài học khi vẽ | `knowledge/hoc/` |

## Build và kiểm thử
- `cad\build.bat`: ra `TKPCCC.bundle\Contents\TKPCCC.Cad.dll`, `TKPCCC.UI.dll` và `bin\TKPCCC.Mcp.exe`. AutoCAD đang mở thì tắt hẳn rồi mở lại để nạp bản mới.
- `powershell -ExecutionPolicy Bypass -File cad\test\thu_gui.ps1` (không bắt buộc, chạy khi sửa giao diện hoặc cầu nối): tự mở một AutoCAD thật 1–2 phút rồi tự tắt, thử bảng `PCCC_BLOCK` và đường MCP thật. Chỉ chạy khi không có AutoCAD nào đang mở.
- `powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1`: plugin + `PCCC_SPEC`, không cần mở AutoCAD.
- `powershell -ExecutionPolicy Bypass -File cad\test\thu_mcp.ps1`: máy chủ MCP + hàng đợi + plugin, không cần mở AutoCAD.
- Cả hai phải ra `TAT CA DAT`.

## Dùng
1. Chạy `cad\build.bat`, mở AutoCAD (bundle tự nạp, hoặc `NETLOAD` file `TKPCCC.Cad.dll`; file này tự nạp luôn `TKPCCC.UI.dll`). Gõ `PCCC_BLOCK` để mở bảng block, `PCCC_HELP` để xem mọi lệnh.
2. Mở Claude Code ở gốc repo hoặc tại `cad/`: file `.mcp.json` khai máy chủ `cad`. AI khác thì khai lệnh chạy là đường dẫn tới `cad\bin\TKPCCC.Mcp.exe`.
3. Gõ yêu cầu vẽ. Chạy tay một spec: trong AutoCAD gõ `PCCC_SPEC` rồi dán đường dẫn file `.json`; kết quả nằm ở `<tên>.kq.json`.
