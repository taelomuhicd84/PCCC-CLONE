# cad/ — dự án con AutoCAD của TKPCCC

Plugin AutoCAD 2021 và đường vẽ tự động qua MCP. Phiên mới đọc `TIEN_DO.md` trước; kế hoạch tổng ở `KE_HOACH.md`; nhật ký ở `LICH_SU.md`; luật riêng ở `CLAUDE.md`.

## Cây thư mục
```
cad/
├─ CLAUDE.md · README.md · KE_HOACH.md · TIEN_DO.md · LICH_SU.md   luật riêng, bản đồ, kế hoạch, tiến độ, nhật ký
├─ HUONG_DAN_SU_DUNG.md     hướng dẫn dùng plugin cho kỹ sư (cài, quy trình, mọi lệnh, mã, layer, lưu ý); sửa lệnh thì sửa cả file này + `DS_LENH`
├─ .mcp.json               khai máy chủ MCP "cad" khi mở Claude Code ngay tại cad/ (gốc repo có bản riêng)
├─ .claude/
│  ├─ agents/              cad-thiet-ke (lập spec) · cad-ve (chạy spec) · cad-kiem (soát bản vẽ) · cad-lap-trinh (sửa plugin) · cad-review (review mỗi mốc, Write chỉ để ghi báo cáo); bảng việc ở CLAUDE.md
│  └─ skills/              tkpccc-ve-cad (vẽ qua MCP; scripts/pdf_ti_le.py tính tỉ lệ trang PDF, scripts/chay_spec.ps1 chạy spec không cần mở AutoCAD) · tkpccc-cad-console (build, kiểm thử, in PNG; scripts/hinh_tu_pdf.py lấy hình ký hiệu từ nét vector PDF)
├─ skills/                 hướng dẫn dùng cho MỌI AI (Claude, Gemini, Codex): TU_VUNG.md (từ vựng spec, nguồn duy nhất) · vi_du/
├─ knowledge/              kiến thức vẽ CAD: di-day-bao-chay.md (đi dây hệ thường / địa chỉ theo bản vẽ mẫu người dùng) · quy-uoc-ban-ve.md · mau-ban-ve-pccc.md (cách rải, đi dây, đi ống của bản mẫu) · tham-khao-mcp-hub.md (ý lấy từ MCP bên khác) · so-do-nguyen-ly.md (cách vẽ sơ đồ nguyên lý `so_do`, chi tiết nào lấy từ mẫu, chi tiết nào tự đặt) · hoc-ban-ve-mau-dwg.md (học 5 bộ DWG mẫu: bộ tờ, khung tên, bảng ký hiệu, bảng quy định lắp đặt, beam, zone, sprinkler, việc plugin còn thiếu) · mau-thuyet-minh-pccc.md (văn thuyết minh mẫu chép nguyên văn từ bản vẽ, theo hệ) · hoc-video-plugin-16-11.md (học giao diện + ý hay của 2 plugin PCCC qua 3 video, việc chưa làm) · mau-rai-khoang-dam.md (mẫu người dùng rải đầu báo theo khoang dầm: lưới chung, lề 2 m tới dầm, 2 x 2 mỗi khoang 10 m) · hoc/ (phan-vung-xuong-mai-keo.md: đối chiếu AI / plugin, luật L1–L8 dạy plugin phân vùng xưởng mái kèo, test)
├─ rule/                   LUAT.md: việc trên CAD ↔ file quy chuẩn nguyên văn ↔ skill, agent liên quan
├─ layer/                  file .md chuẩn layer (tên, màu, loại đường) để tạo layer đúng chuẩn · kien_truc.md (TUONG, VACH, CUA, DIM)
├─ data/                   dữ liệu vẽ (xem data/README.md)
│  ├─ ban_quyen/           cau_hinh_ban_quyen.json (URL web app, Google Form) · khoa_cong_khai.xml (KeyGen tạo; khoá riêng KHÔNG ở repo)
│  ├─ ky_hieu/             ky_hieu.json: mặc định ký hiệu trục, kích thước, chữ (plugin đọc) · hinh_bc.json: hình block mặc định báo cháy lấy từ nét PDF hồ sơ thật (sinh bằng tkpccc-cad-console/scripts/hinh_tu_pdf.py)
│  ├─ block/               thư viện block, block động co, tê (chờ file của người dùng)
│  ├─ ong/ · bom/          kích thước ống + phụ kiện, kích thước bơm (chờ catalog)
│  └─ mau/                 bản vẽ mẫu, khung tên (chờ file của người dùng)
├─ module/                 mã chạy
│  ├─ plugin/              Lenh.cs (lệnh PCCC_*) · Spec.cs (PCCC_SPEC, thao tác spec) · Rai.cs (rai, tuyen, PCCC_RAI, PCCC_TUYEN) · NhanVung.cs (vung_tu_dong: tự dựng vùng phòng từ nét tường, bỏ trục) · DayBc.cs (PCCC_DAYBC đi dây báo cháy hệ thường / địa chỉ) · CaiDat.cs (cai_dat.json dùng chung với palette: bảng layer dây, dây theo thiết bị, kiểu dim) · KienTruc.cs (tuong, cua, kt_chuoi, PCCC_TRUC, PCCC_TUONG, PCCC_CUA) · Dim.cs (PCCC_ADIM, PCCC_CDIM) · SoDo.cs (so_do, sơ đồ nguyên lý) · Block.cs (PCCC_THEMBLOCK, ảnh xem trước) · Cau.cs (cầu nối) -> TKPCCC.Cad.dll
│  ├─ ui/                  BangChinh.cs (palette chính lệnh PCCC: Quy trình vẽ, Tiện ích, Báo cháy, Chữa cháy, Thống kê, Thư viện, Lệnh, Cài đặt) · BangBoTri.cs (phân hệ Báo cháy, PCCC_BOTRI) · BangTienIch.cs (phân hệ Tiện ích, PCCC_TIENICH) · BangBlock.cs (phân hệ Thư viện, PCCC_BLOCK) · BangLenh.cs (phân hệ Lệnh, PCCC_HELP) · BangChonVatTu.cs (gõ ? khi hỏi mã: bảng chọn thiết bị) · BangTraCuu.cs (nút Tra cứu TCVN: bảng khoang dầm 5.9.1.1.6) · GiaoDien.cs (3 kiểu màu, cai_dat.json, widget NutIcon / ThanhTab / TieuDeNhom / KhungTrangThai) -> TKPCCC.UI.dll; chỗ DUY NHẤT được tham chiếu acmgd, không xử lý bản vẽ
│  ├─ banquyen/            KeyGen.cs -> bin/TKPCCC.KeyGen.exe (chủ plugin tạo khoá, ký key) · keygen.py (tạo key bằng Python, cùng hợp đồng key với KeyGen.exe, khoá riêng ngoài repo) · apps_script/Code.gs (Google Sheet tự cấp key) · HUONG_DAN.md · plugin/BanQuyen.cs + ui/BanQuyenUi.cs (PCCC_BANQUYEN)
│  ├─ mcp/                 Mcp.cs -> bin/TKPCCC.Mcp.exe (công cụ cad_tt, cad_chay, cad_anh)
│  ├─ youtube/             PLUGIN RIÊNG YouTube (17/11): BangYoutube.cs + LenhYoutube.cs (lệnh TKYOUTUBE, cửa sổ riêng thu nhỏ / kéo giãn) + webview2/ (3 DLL WebView2 gói NuGet 1.0.2903.40) + brave_khung/ (extension Brave: toàn màn hình chỉ phủ khung cửa sổ) + GanBrave.cs (cửa sổ Brave nhận AutoCAD làm chủ: luôn hiện trên CAD) -> TKPCCC_YouTube.bundle
│  ├─ botchat/             PLUGIN RIÊNG Bot chat (17/11): BotChat.cs (chọn Claude/Gemini, model, effort, thẻ thư mục dự án, dán ảnh, thẻ vùng chọn CAD) + PhienAi.cs (đọc phiên Claude Code / Antigravity để link, chỗ chứa skill -> cờ CLI, MCP tuyệt đối) + GiaoDienChat.cs (bong bóng, chip, nút biểu tượng, danh mục model) + ChonThuMuc.cs (hộp chọn thư mục kiểu Explorer) + LenhBotChat.cs (lệnh TKBOTCHAT, TKBOTCHON, palette) -> TKPCCC_BotChat.bundle
│  ├─ nho/                 GiaoDienNho.cs: màu, chữ, cai_dat.json chung cho 2 plugin riêng (biên dịch kèm, không cần DLL PCCC)
│  ├─ hub/                 Hub.cs -> bin/TKPCCC.Hub.exe (mọi thứ cần cài, chia 4 nhóm: Nền tảng · MCP cad · Bot chat · YouTube; quét máy, cài mục đã chọn, nhật ký)
│  ├─ chung/               HangDoi.cs: hàng đợi file dùng chung plugin + máy chủ MCP + Hub
│  └─ lisp/                TKPCCC.lsp (nạp DLL thay NETLOAD) + LISP của người dùng (nạp tay bằng APPLOAD)
├─ test/                   chay_thu.ps1 (plugin + PCCC_SPEC) · thu_mcp.ps1 (máy chủ MCP) · thu_hub.ps1 (Hub trên nhà giả) · thu_brave.ps1 + ThuBrave.cs (chế độ Brave: extension toàn màn hình trong khung, gán AutoCAD làm chủ cửa sổ) · thu_bot.ps1 + ThuBot.cs (Bot chat: đọc phiên, chỗ chứa skill), không cần mở AutoCAD · mau_kt.pdf (PDF thử 1:200, sinh bằng tao_pdf_mau.py) · thu_gui.ps1 (tự mở AutoCAD thật, không bắt buộc) · thu_ban_quyen.ps1 (key KeyGen + Apps Script giả lập) · ThuBlockDong.cs (DLL chỉ cho chay_thu: chèn block động mẫu Autodesk, đặt Visibility)
├─ build.bat               build DLL + exe, chép dữ liệu vào bundle
├─ cai_dat.bat             cài một lần: liên kết bundle vào %APPDATA%\Autodesk\ApplicationPlugins (AutoCAD tự nạp, khỏi NETLOAD); `/go` để gỡ
├─ bin/                    kết quả build: TKPCCC.Mcp.exe, TKPCCC.Hub.exe, TKPCCC.KeyGen.exe (không commit)
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
| Đổi cách rải thiết bị, cách đi dây, đi ống (`rai`, `tuyen`, `PCCC_RAI`) | `module/plugin/Rai.cs`; cách vẽ gốc ở `knowledge/mau-ban-ve-pccc.md`. Dời thiết bị vào trong vùng cách cạnh ≥ `tuong_min` (cạnh xiên, khe hẹp): `DoiVao`, `KepDay`, mặc định `CACH_TUONG_MIN_M` |
| Đổi cách tự nhận diện tường / trục, dựng vùng phòng khi bấm điểm (`vung_tu_dong`, `PCCC_RAI` bấm điểm) | `module/plugin/NhanVung.cs` (từ khoá layer `LOP_BO`, `LOP_TUONG`, khe cửa `HO_CUA_M`; khe có cửa / tường 2 nét tới `HO_CUA_MAX_M`, `GomCua`, `SongDoi`; ô X `TachOX`; gạch chéo hiện hữu `LaGachCheo`) |
| Đổi câu hướng dẫn AI nhận lúc kết nối | phần đầu `skills/TU_VUNG.md`, trước dòng `---` (giữ dưới 1900 ký tự) |
| Đổi tên, mô tả công cụ MCP | `module/mcp/Mcp.cs`, hàm `CongCu` |
| Đổi cách vẽ sơ đồ nguyên lý (`so_do`) | `module/plugin/SoDo.cs`; cách vẽ, nguồn ở `knowledge/so-do-nguyen-ly.md` |
| Đổi cách in ảnh (`anh`, `cad_anh`): khổ, thiết bị in, chỗ lưu PNG | `module/plugin/Spec.cs`, hàm `InAnh` |
| Đổi cách nhập PDF làm nền (`pdf`) | `module/plugin/Spec.cs`, hàm `NhapPdf` |
| Đổi cách tính tỉ lệ trang PDF từ chuỗi kích thước | `.claude/skills/tkpccc-ve-cad/scripts/pdf_ti_le.py` (kiểm bằng `test/chay_thu.ps1`) |
| Đổi nhịp xem hàng đợi, điều kiện "AutoCAD đang rảnh" | `module/plugin/Cau.cs` |
| Plugin YouTube: trang chủ, cách tìm, dừng video khi đóng; cửa sổ (`TKYOUTUBE`); đổi bản WebView2 (thay 3 DLL trong `module/youtube/webview2/`) | `module/youtube/BangYoutube.cs`, `LenhYoutube.cs`; chép DLL ở `build.bat`; chế độ Brave (`yt_trinh_duyet`, tìm brave.exe: `HangDoi.TimBrave`; `MoBrave`: hồ sơ riêng `%LOCALAPPDATA%\TKPCCC\Brave`, cửa sổ thường có tab) cũng ở đây; toàn màn hình trong khung Brave: extension `module/youtube/brave_khung/` (build.bat chép cạnh DLL, nạp bằng `--load-extension`) |
| Nút ☰ đầu palette (menu phân hệ, thu gọn thanh dọc `ben_an`) | `module/ui/BangChinh.cs` (`DauTrang`, `MenuPhanHe`) |
| Hub: thêm thành phần / nhóm mới (mục, cách kiểm, cách cài, trang tải), câu hỏi xác nhận, cửa sổ | `module/hub/Hub.cs` (`DanhSach`, `SuaJson`, `ChayCli`, `CuaSoHub`); test `test/thu_hub.ps1` |
| Đổi chỗ đặt hàng đợi | `module/chung/HangDoi.cs` hoặc biến môi trường `TKPCCC_CAD_HANG_DOI` |
| Lệnh gõ tay `PCCC_*` | `module/plugin/Lenh.cs` (xem skill `tkpccc-cad-console`) |
| Tường, vách, cửa (`tuong`, `cua`, `kt_chuoi`; lệnh `PCCC_TRUC`, `PCCC_TUONG`, `PCCC_CUA`) | `module/plugin/KienTruc.cs`; layer, dày, rộng cửa mặc định ở nhóm `kien_truc` của `data/ky_hieu/ky_hieu.json`, chuẩn `layer/kien_truc.md` |
| Dim tự động (`PCCC_ADIM`, `PCCC_CDIM`, lõi chuỗi dim của `kt_chuoi`) | `module/plugin/Dim.cs` |
| Thêm vật tư, thiết bị | `data/vat_tu.json` ở GỐC repo (dùng chung với báo giá), rồi `build.bat`, `PCCC_LIB` |
| Đưa block của mình (kể cả block động) vào thư viện dưới một mã mới | trong AutoCAD: `PCCC_THEMBLOCK` hoặc nút "Thêm từ bản vẽ" của bảng `PCCC_BLOCK`; mã nguồn `module/plugin/Block.cs` |
| Tách khu vực `PCCC_TACHVUNG`, đặt tên vùng `PCCC_KHUVUC`, vòng tròn làm vùng (`DaGiacTron`) | `module/plugin/TienIch.cs`; vùng tròn đọc ở `NhanVung.cs` `MatChon` |
| Kiểm vùng mù đầu báo (`PCCC_KT`) và các tiện ích chỉnh sửa, kiểm tra (`PCCC_TRUNG`, `PCCC_LOPCHUAN`, `PCCC_DANHSO`, `PCCC_XOAY`, `PCCC_TYLE`, `PCCC_DOIBLOCK`, `PCCC_CANDIM`, `PCCC_DOIDN`) | `module/plugin/TienIch.cs` |
| Trang con ADIM 5 bước (layer, DimStyle, lọc, phương) | `module/ui/BangAdim.cs`; lệnh `module/plugin/Dim.cs` (`AdimPhuong`) |
| Thẻ phân hệ Chữa cháy, Thống kê (lưới thẻ) | `module/ui/PhanHe.cs` (thêm thẻ = 1 dòng) |
| Trang Tiện ích của palette (`PCCC_TIENICH`: 27 thẻ, mã tắt, nhóm) | `module/ui/BangTienIch.cs`, mảng `THE` (thêm thẻ = 1 dòng) |
| Vùng loại trừ `PCCC_LOAITRU`, ống gió (layer ONG_GIO / DUCT, đầu báo dịch ra), vẽ tạm xem trước `PCCC_XEM_TRUOC` của `PCCC_RAI` | `module/plugin/Rai.cs`, hàm `TachLoaiTru`, `TachOngGio` (từ khoá `DUOI_ONG_GIO`), `DichRaNgoai`, `XoaXemTruoc` |
| Dây báo cháy nối tiếp thiết bị (`PCCC_DAY`, dây loop); đầu báo tia chiếu `BC_BEAM` vẽ tia (`PCCC_RAI`) | `module/plugin/Rai.cs`, hàm `DayTay`, `TiaVung`, `VeTia` |
| Gợi ý loại đầu báo theo công năng (trang Báo cháy) | bảng "Loại theo khu vực" ở `knowledge/chon-loai-dau-bao.md` gốc repo (build chép cạnh DLL) |
| Đổi trang Bố trí BC (`PCCC_BOTRI`: thiết bị theo tiêu chuẩn, mức tối ưu k, nút BỐ TRÍ gửi `PCCC_RAI` điền sẵn) | `module/ui/BangBoTri.cs`; trị số ở `data/khoang_cach_rai.json` gốc repo; từ khoá `HeSo` ở `module/plugin/Rai.cs`, hàm `RaiTay` |
| Đổi palette chính `PCCC` (thẻ quy trình 6 bước, trang Cài đặt) | `module/ui/BangChinh.cs`; màu 3 kiểu giao diện ở `module/ui/GiaoDien.cs`, hàm `BangMau` |
| Plugin Bot chat (`TKBOTCHAT`): máy AI (Claude `claude -p` có MCP cad / Gemini `agy -p` chỉ đọc), danh sách model, effort (Claude `--effort`, Gemini ghép vào tên model), thẻ thư mục dự án (`--add-dir`); dán / kéo thả ảnh, thẻ vùng chọn CAD (handle gửi kèm) | `module/botchat/BotChat.cs` (`AI`, `MODEL`, `Effort`, `VeThe`, `Gui`, `DocDong`; ảnh `DanAnh`, `ONhap`; vùng `VeDinhKem`, `MoTaVung`); bố cục theo khung chat mẫu 18/11: thanh đầu (`MenuDuAn`, `MoLichSu`, `MenuCaiDat`), bong bóng (`Ghi`, `XepTin`), khung soạn (`VeChip`, `MenuCong`, `MenuSkill`, `MenuModel`); model Claude / Antigravity ở `GiaoDienChat.cs` (`DanhMucModel`, Antigravity đọc `agy models`); hộp chọn thư mục `ChonThuMuc.cs`; đọc phiên + skill ở `PhienAi.cs` (`KhoPhien`); palette, theo dõi chọn (`ImpliedSelectionChanged`), lệnh `TKBOTCHON` ở `LenhBotChat.cs`; AI sửa vùng bằng thao tác spec `sua`, `doc gi:"dt"`, lọc `h` (`module/plugin/Spec.cs`) |
| Thanh biểu tượng trái (màu, emoji mỗi phân hệ), nút tròn biểu tượng, tab viên thuốc, tiêu đề nhóm, khung TRẠNG THÁI | thanh: `module/ui/BangChinh.cs` (`NutBen`, mảng `icon`, `mau`); widget: `module/ui/GiaoDien.cs` (`NutIcon`, `ThanhTab`, `TieuDeNhom`, `KhungTrangThai`, `MauNhom`) |
| Ghi m² từng khoang dầm + dấu X khoang dưới ngưỡng (layer PCCC_KHOANG_M2), công tắc `kd_ghi_m2` | `module/plugin/Rai.cs` (`GhiKhoang`); công tắc ở tab Khoang dầm `module/ui/BangBoTri.cs` |
| Ống nhánh tự chọn DN theo số đầu phun phía sau (`PCCC_TUYEN` TuDong, mức Den05 / Tren05), chữ DN | `module/plugin/Rai.cs` (`TuyenXuongCa` tham số `dem`, `BangDn`, `MaDn`, `ChuDn`); bảng số đầu theo DN ở `data/ong_nhanh_dn.json` gốc repo (build.bat chép cạnh DLL); thẻ palette ở `module/ui/PhanHe.cs` |
| Trang Báo cháy 4 tab (Rải đầu báo · Khoang dầm · Kiểm tra · Đi dây), nội dung khung TRẠNG THÁI, nút Tra cứu TCVN | `module/ui/BangBoTri.cs` (`TaoKhoangDam`, `CapNhatTrangThai`); đếm vùng `Lenh.DemVung` ở `module/plugin/DayBc.cs`; bảng tra `module/ui/BangTraCuu.cs` đọc `skills/MOD_dau_bao_khoi_khoang_dam.md` gốc repo |
| Đổi bảng block (cột, ô lọc, nút) | `module/ui/BangBlock.cs`; cách vẽ ảnh xem trước ở `module/plugin/Block.cs`, hàm `VeAnhBlock` |
| Sửa hình block thiết bị | `TKPCCC.bundle/Contents/PCCC_Blocks.dwg` bằng BEDIT, rồi `PCCC_LIB` |
| Bản quyền: key, mã máy, chặn lệnh, cấp key qua Google Sheet | `module/plugin/BanQuyen.cs`, `module/banquyen/` (đọc `HUONG_DAN.md`); build khoá lệnh: `build.bat phat_hanh` |
| Thêm một luật vẽ, nối tới quy chuẩn | 1 dòng trong `rule/LUAT.md` |
| Ghi bài học khi vẽ | `knowledge/hoc/` |
| Bảng KÝ HIỆU mặt bằng `PCCC_KYHIEU`, bảng quy định lắp đặt đầu báo `PCCC_QUYDINH`, thao tác spec `bang` | `module/plugin/BangMb.cs` (trị số từ `data/khoang_cach_rai.json`) |
| Thuyết minh theo hệ có trên bản vẽ `PCCC_THUYETMINH` (mục chọn ở `MUC_THEO_HE`) | `module/plugin/BangMb.cs`; văn mẫu `knowledge/mau-thuyet-minh-pccc.md` (build.bat chép cạnh DLL) |
| Dim bước tia beam (`PCCC_RAI` BC_BEAM, layer `PCCC_BC_TIA_KT`); nhãn zone + diện tích, điện trở cuối dây (`PCCC_DAY`) | `module/plugin/Rai.cs` (`KtTia`, `DayTay`) |
| Rải theo khoang dầm (`PCCC_RAI` từ khoá Dam, đầu khói): bấm nét DẦM mẫu, độ sâu d từ ký hiệu b x h (không có thì hỏi cỡ chung / TungDam), d ≤ 0,3 m bỏ dầm; khoang ≥ 9 m² (trần ≥ 4 m) / 4 m² (2–4 m) rải từng khoang, khoang nhỏ gom rải như trần phẳng | `module/plugin/Rai.cs` (`khoangDam`, `hopNho`); nguyên văn 5.9.1.1.6 ở `knowledge/tcvn7568-14-2025.md` |
| Hatch đường thoát nạn `PCCC_THOATNAN`; vòng bán kính tủ vách tường in được (`PCCC_VUNGBV` mã hệ HVT) | `module/plugin/TienIch.cs`, `module/plugin/Lenh.cs` (`VungBaoVe`) |
| Làm gọn vùng tự dựng: bỏ khấc cột, cung cửa, gờ nhỏ hơn 1 m, ra đa giác vuông góc (`PCCC_RAI`, `vung_tu_dong`) | `module/plugin/NhanVung.cs` (`LamGon`, ngưỡng `LAM_GON_M`) |
| Dim tự động sau `PCCC_RAI`: một chuỗi X, một chuỗi Y, trùng hàng / cột chỉ dim một lần; kiểu dim, cách dim, layer dim | `module/plugin/Rai.cs` (`KtLuoi`, `KtSauRai`); cài đặt ở `module/plugin/CaiDat.cs` + trang Cài đặt `module/ui/BangChinh.cs` (`CaiDim`) |
| Đi dây báo cháy `PCCC_DAYBC` (hệ thường: một đường, điện trở cuối ở thiết bị xa tủ nhất; hệ địa chỉ: mạch vòng module, đầu báo thường về ZM, chuông về CM / R2ML) | `module/plugin/DayBc.cs`; cách vẽ gốc ở `knowledge/di-day-bao-chay.md` |
| Bảng layer dây báo cháy, ống chữa cháy (thêm, xoá, lấy từ bản vẽ); chọn dây cho từng loại thiết bị | `module/plugin/CaiDat.cs` (`LOP_MAC_DINH`, `VAI_TRO`); trang Cài đặt `module/ui/BangChinh.cs` (`BangLopDay`); trang Báo cháy mục 6 `module/ui/BangBoTri.cs` (`TaoDiDay`) |

## Build và kiểm thử
- `cad\build.bat`: ra `TKPCCC.bundle\Contents\TKPCCC.Cad.dll`, `TKPCCC.UI.dll` và `bin\TKPCCC.Mcp.exe`. AutoCAD đang mở thì tắt hẳn rồi mở lại để nạp bản mới.
- `powershell -ExecutionPolicy Bypass -File cad\test\thu_gui.ps1` (không bắt buộc, chạy khi sửa giao diện hoặc cầu nối): tự mở một AutoCAD thật 1–2 phút rồi tự tắt, thử bảng `PCCC_BLOCK` và đường MCP thật. Chỉ chạy khi không có AutoCAD nào đang mở.
- `powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1`: plugin + `PCCC_SPEC`, không cần mở AutoCAD.
- `powershell -ExecutionPolicy Bypass -File cad\test\thu_mcp.ps1`: máy chủ MCP + hàng đợi + plugin, không cần mở AutoCAD.
- Cả hai phải ra `TAT CA DAT`.

## Dùng
1. Chạy `cad\build.bat`, rồi `cad\cai_dat.bat` MỘT lần (liên kết bundle để AutoCAD tự nạp mỗi lần mở; gỡ bằng `cad\cai_dat.bat /go` trong cmd / PowerShell thường: sandbox của Claude không đi qua và không xoá được liên kết này).
   Đã kiểm 15/11 (ngoài sandbox, liên kết đúng đích): accoreconsole KHÔNG tự nạp bundle này, bộ test không bị ảnh hưởng. AutoCAD có giao diện tự nạp: chưa thử.
   Cách khác: `APPLOAD` file `module\lisp\TKPCCC.lsp`, hoặc `NETLOAD` file `TKPCCC.Cad.dll` (file này tự nạp luôn `TKPCCC.UI.dll`). Gõ `PCCC` để mở palette chính (`PCCC_BLOCK` mở thẳng phân hệ Thư viện, `PCCC_HELP` phân hệ Lệnh).
2. Mở Claude Code ở gốc repo hoặc tại `cad/`: file `.mcp.json` khai máy chủ `cad`. AI khác thì khai lệnh chạy là đường dẫn tới `cad\bin\TKPCCC.Mcp.exe`.
3. Gõ yêu cầu vẽ. Chạy tay một spec: trong AutoCAD gõ `PCCC_SPEC` rồi dán đường dẫn file `.json`; kết quả nằm ở `<tên>.kq.json`.
