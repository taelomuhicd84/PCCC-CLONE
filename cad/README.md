# cad/ — dự án con AutoCAD của TKPCCC

Plugin AutoCAD 2021 và đường vẽ tự động qua MCP. Phiên mới đọc `TIEN_DO.md` trước; kế hoạch tổng ở `KE_HOACH.md`; nhật ký ở `LICH_SU.md`; luật riêng ở `CLAUDE.md`.

## Cây thư mục
```
cad/
├─ CLAUDE.md · README.md · KE_HOACH.md · TIEN_DO.md · LICH_SU.md   luật riêng, bản đồ, kế hoạch, tiến độ, nhật ký
├─ .mcp.json               khai máy chủ MCP "cad" khi mở Claude Code ngay tại cad/ (gốc repo có bản riêng)
├─ .claude/
│  ├─ agents/              agent riêng của CAD (chưa có; chỉ nạp khi mở Claude Code tại cad/)
│  └─ skills/              tkpccc-ve-cad (vẽ qua MCP) · tkpccc-cad-console (build, kiểm thử, in PNG)
├─ skills/                 hướng dẫn dùng cho MỌI AI (Claude, Gemini, Codex): TU_VUNG.md (từ vựng spec, nguồn duy nhất) · vi_du/
├─ knowledge/              kiến thức vẽ CAD: quy-uoc-ban-ve.md · mau-ban-ve-pccc.md (cách rải, đi dây, đi ống của bản mẫu) · tham-khao-mcp-hub.md (ý lấy từ MCP bên khác) · hoc/
├─ rule/                   LUAT.md: việc trên CAD ↔ file quy chuẩn nguyên văn ↔ skill, agent liên quan
├─ layer/                  file .md chuẩn layer (tên, màu, loại đường) để tạo layer đúng chuẩn
├─ data/                   dữ liệu vẽ (xem data/README.md)
│  ├─ ky_hieu/             ky_hieu.json: mặc định ký hiệu trục, kích thước, chữ (plugin đọc)
│  ├─ block/               thư viện block, block động co, tê (chờ file của người dùng)
│  ├─ ong/ · bom/          kích thước ống + phụ kiện, kích thước bơm (chờ catalog)
│  └─ mau/                 bản vẽ mẫu, khung tên (chờ file của người dùng)
├─ module/                 mã chạy
│  ├─ plugin/              Lenh.cs (lệnh PCCC_*) · Spec.cs (PCCC_SPEC, thao tác spec) · Rai.cs (rai, tuyen, PCCC_RAI) · Cau.cs (cầu nối) -> TKPCCC.Cad.dll
│  ├─ mcp/                 Mcp.cs -> bin/TKPCCC.Mcp.exe (công cụ cad_tt, cad_chay)
│  ├─ chung/               HangDoi.cs: hàng đợi file dùng chung plugin + máy chủ MCP
│  └─ lisp/                LISP của người dùng (nạp tay bằng APPLOAD)
├─ test/                   chay_thu.ps1 (plugin + PCCC_SPEC) · thu_mcp.ps1 (máy chủ MCP), không cần mở AutoCAD
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
| Đổi câu hướng dẫn AI nhận lúc kết nối | phần đầu `skills/TU_VUNG.md`, trước dòng `---` (giữ dưới 1900 ký tự) |
| Đổi tên, mô tả công cụ MCP | `module/mcp/Mcp.cs`, hàm `CongCu` |
| Đổi nhịp xem hàng đợi, điều kiện "AutoCAD đang rảnh" | `module/plugin/Cau.cs` |
| Đổi chỗ đặt hàng đợi | `module/chung/HangDoi.cs` hoặc biến môi trường `TKPCCC_CAD_HANG_DOI` |
| Lệnh gõ tay `PCCC_*` | `module/plugin/Lenh.cs` (xem skill `tkpccc-cad-console`) |
| Thêm vật tư, thiết bị | `data/vat_tu.json` ở GỐC repo (dùng chung với báo giá), rồi `build.bat`, `PCCC_LIB` |
| Sửa hình block thiết bị | `TKPCCC.bundle/Contents/PCCC_Blocks.dwg` bằng BEDIT, rồi `PCCC_LIB` |
| Thêm một luật vẽ, nối tới quy chuẩn | 1 dòng trong `rule/LUAT.md` |
| Ghi bài học khi vẽ | `knowledge/hoc/` |

## Build và kiểm thử
- `cad\build.bat`: ra `TKPCCC.bundle\Contents\TKPCCC.Cad.dll` và `bin\TKPCCC.Mcp.exe`. AutoCAD đang mở thì tắt hẳn rồi mở lại để nạp bản mới.
- `powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1`: plugin + `PCCC_SPEC`, không cần mở AutoCAD.
- `powershell -ExecutionPolicy Bypass -File cad\test\thu_mcp.ps1`: máy chủ MCP + hàng đợi + plugin, không cần mở AutoCAD.
- Cả hai phải ra `TAT CA DAT`.

## Dùng
1. Chạy `cad\build.bat`, mở AutoCAD (bundle tự nạp, hoặc `NETLOAD` file `TKPCCC.Cad.dll`).
2. Mở Claude Code ở gốc repo hoặc tại `cad/`: file `.mcp.json` khai máy chủ `cad`. AI khác thì khai lệnh chạy là đường dẫn tới `cad\bin\TKPCCC.Mcp.exe`.
3. Gõ yêu cầu vẽ. Chạy tay một spec: trong AutoCAD gõ `PCCC_SPEC` rồi dán đường dẫn file `.json`; kết quả nằm ở `<tên>.kq.json`.
