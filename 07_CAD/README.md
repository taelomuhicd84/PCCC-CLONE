# 07_CAD/ — dự án con AutoCAD của TKPCCC

Plugin AutoCAD 2021, bot chat, YouTube và đường vẽ tự động qua MCP. Phiên mới đọc `00_QUAN_LY_DU_AN/TIEN_DO.md` trước; kế hoạch tổng `00_QUAN_LY_DU_AN/KE_HOACH.md`; nhật ký `00_QUAN_LY_DU_AN/LICH_SU.md`; luật riêng `CLAUDE.md`.

## Cây thư mục (đánh số như gốc repo, người dùng giao 26/11)
Số đầu = thứ tự đọc. `00`–`04` là quản lý, tri thức, luật, dữ liệu, kiểm thử; từ `05` trở đi **mỗi số là MỘT hạng mục tính năng** (sửa tính năng nào chỉ vào thư mục đó).
Người dùng 26/11 (chiều): mọi hạng mục RIÊNG của PCCC gom vào nhóm `09_PCCC/` (mục con đánh số `01_`…`06_`), để sau thêm nhóm lĩnh vực khác (xây dựng, kiến trúc, LISP phụ trợ) thành `15_...` cùng kiểu; `07_KIEN_TRUC`, `08_KICH_THUOC`, `10_TIEN_ICH` là phần vẽ chung, không riêng PCCC.
Trong một hạng mục của plugin PCCC: `cad/` -> `TKPCCC.Cad.dll` (xử lý bản vẽ, thử được trong accoreconsole, chỉ `acdbmgd` + `accoremgd`), `ui/` -> `TKPCCC.UI.dll` (giao diện, chỗ DUY NHẤT được tham chiếu `acmgd`).
`build.bat` và bộ test tự gom mọi `NN_*\cad\*.cs`, `NN_*\ui\*.cs` và một cấp nhóm `NN_*\NN_*\cad`, `NN_*\NN_*\ui`: thêm tính năng = thêm thư mục số kế tiếp (`15_...`, hoặc `07_...` trong `09_PCCC/`), không chèn giữa, không sửa `build.bat`.
Mọi file `cad/` là phần nối của `partial class Lenh` (bot chat: `partial class BotChat`), nên dời hàm giữa các hạng mục không phải sửa mã. Lệnh `[CommandMethod]` và danh sách `DS_LENH` vẫn gom ở `05_NEN_TANG/cad/Lenh.cs`.
```
07_CAD/
├─ CLAUDE.md · README.md · .mcp.json · .claude/   file công cụ bắt buộc ở gốc: luật riêng, bản đồ này, MCP "cad", agent + skill CAD
│     .claude/agents/  cad-thiet-ke (lập spec) · cad-ve (chạy spec) · cad-kiem (soát bản vẽ) · cad-lap-trinh (sửa plugin) · cad-review (review mỗi mốc); bảng việc ở CLAUDE.md
│     .claude/skills/  tkpccc-ve-cad (vẽ qua MCP; scripts/pdf_ti_le.py, scripts/chay_spec.ps1) · tkpccc-cad-console (build, kiểm thử, in PNG; scripts/hinh_tu_pdf.py)
├─ build.bat · cai_dat.bat   build mọi DLL + exe, chép dữ liệu vào bundle · liên kết 3 bundle vào %APPDATA%\Autodesk\ApplicationPlugins (`/lai` build tự gọi, `/go` gỡ)
├─ TKPCCC.bundle/ · TKPCCC_BotChat.bundle/ · TKPCCC_YouTube.bundle/ · bin/   gói AutoCAD tự nạp + kết quả build (giữ ở gốc: liên kết AutoCAD và Hub trỏ tới đây)
│
├─ 00_QUAN_LY_DU_AN/     TIEN_DO.md · LICH_SU.md · KE_HOACH.md · HUONG_DAN_SU_DUNG.md (hướng dẫn kỹ sư: cài, mọi lệnh, mã, layer; sửa lệnh thì sửa cả file này + `DS_LENH`) · viec/ (phiếu việc cũ)
├─ 01_TRI_THUC_VE/       ĐẦU NGUỒN tri thức vẽ cho MỌI AI (mục lục README.md): TU_VUNG.md (từ vựng spec) · vi_du/ · nhan_dang/ (đọc bản vẽ) · ve/ (vẽ + quy ước layer) · mau/ (bằng chứng từ bản mẫu)
├─ 02_LUAT_VE/           LUAT.md: việc trên CAD ↔ file quy chuẩn nguyên văn ↔ skill, agent
├─ 03_DU_LIEU_VE/        ban_quyen/ (cau_hinh_ban_quyen.json, khoa_cong_khai.xml; khoá riêng KHÔNG ở repo) · ky_hieu/ (ky_hieu.json, hinh_bc.json) · block/ · ong/ · bom/ · mau/ (chờ file người dùng)
├─ 04_KIEM_THU/          chay_thu.ps1 (plugin + PCCC_SPEC) · thu_mcp.ps1 · thu_hub.ps1 · thu_bot.ps1 + ThuBot.cs · thu_brave.ps1 + ThuBrave.cs · thu_ban_quyen.ps1 · thu_gui.ps1 (AutoCAD thật) · ThuBlockDong.cs · mau_kt.pdf + tao_pdf_mau.py
│
├─ 05_NEN_TANG/          khung plugin: cad/ Lenh.cs (khởi động, đăng ký mọi lệnh PCCC_*, DS_LENH, layer, đơn vị, hỏi mã) · CaiDat.cs (cai_dat.json: layer dây, dây theo thiết bị, kiểu dim)
│                        ui/ BangChinh.cs (palette PCCC, thanh biểu tượng, trang Cài đặt) · GiaoDien.cs (3 kiểu màu, widget) · PhanHe.cs (lưới thẻ Chữa cháy, Thống kê) · BangLenh.cs (PCCC_HELP, phím tắt) · KhoiDongUi.cs
│                        chung/ HangDoi.cs (hàng đợi file: plugin + MCP + Hub + YouTube) · GiaoDienNho.cs (màu, chữ chung cho Bot chat, YouTube) · lisp/ TKPCCC.lsp
├─ 06_VE_TU_DONG_AI/     cad/ Spec.cs (PCCC_SPEC, MotOp, thao tác dat ong net tron chu kt xoa sua doc dem, pdf, anh) · Cau.cs (cầu nối hàng đợi) · mcp/ Mcp.cs -> bin/TKPCCC.Mcp.exe (cad_tt, cad_chay, cad_anh)
├─ 07_KIEN_TRUC/         cad/ KienTruc.cs (tuong, cua, kt_chuoi, PCCC_TRUC/TUONG/CUA) · NhanVung.cs (vung_tu_dong, nhận tường, khe cửa, làm gọn) · Truc.cs (luoi_truc)
├─ 08_KICH_THUOC/        cad/ Dim.cs (PCCC_ADIM, PCCC_CDIM, lõi chuỗi dim) · DimRai.cs (dim định vị sau rải) · ui/ BangAdim.cs (trang ADIM 5 bước)
├─ 09_PCCC/              NHÓM PCCC (mọi hạng mục riêng của PCCC):
│  ├─ 01_RAI_THIET_BI/    cad/ Rai.cs (rai, PCCC_RAI, lưới rải, dời vào vùng, phủ kín) · KhoangDam.cs (rải theo khoang dầm, m² khoang) · Tia.cs (BC_BEAM) · LoaiTru.cs (PCCC_LOAITRU, ống gió, xem trước)
│  ├─ 02_BAO_CHAY/        cad/ DayBc.cs (PCCC_DAYBC hệ thường / địa chỉ) · DayTay.cs (PCCC_DAY) · ui/ BangBoTri.cs (trang Báo cháy 4 tab, PCCC_BOTRI) · BangTraCuu.cs (Tra cứu TCVN khoang dầm)
│  ├─ 03_CHUA_CHAY/       cad/ Tuyen.cs (tuyen, PCCC_TUYEN, ống nhánh tự chọn DN) · VungBaoVe.cs (PCCC_VUNGBV)
│  ├─ 04_SO_DO_NGUYEN_LY/ cad/ SoDo.cs (so_do spr / bc)
│  ├─ 05_THONG_KE/        cad/ BocKhoiLuong.cs (PCCC_GAN, PCCC_BOC, PCCC_BANG) · BangMb.cs (PCCC_KYHIEU, PCCC_QUYDINH, PCCC_THUYETMINH, thao tác bang)
│  └─ 06_THU_VIEN_BLOCK/  cad/ ThuVien.cs (PCCC_LIB, hình mặc định) · Block.cs (PCCC_THEMBLOCK, ảnh xem trước) · DatThietBi.cs (PCCC_DAT, PCCC_ONG) · ui/ BangBlock.cs (PCCC_BLOCK) · BangChonVatTu.cs
├─ 10_TIEN_ICH/          cad/ TienIch.cs (PCCC_KT, TRUNG, LOPCHUAN, DANHSO, XOAY, TYLE, DOIBLOCK, CANDIM, DOIDN, TACHVUNG, KHUVUC, THOATNAN) · ui/ BangTienIch.cs (PCCC_TIENICH)
├─ 11_BAN_QUYEN/         HUONG_DAN.md · cad/ BanQuyen.cs · ui/ BanQuyenUi.cs (PCCC_BANQUYEN) · keygen/ KeyGen.cs -> bin/TKPCCC.KeyGen.exe, keygen.py, apps_script/Code.gs
├─ 12_BOT_CHAT/          PLUGIN RIÊNG -> TKPCCC_BotChat.bundle (lệnh TKBOTCHAT, TKBOTCHON); build gom cả cây con:
│                        01_KHUNG_CHAT/ (LenhBotChat.cs palette + theo dõi chọn; BotChat.cs khung, nhập, bong bóng, menu +, Cài đặt; GiaoDienChat.cs widget) · 02_MAY_AI_MODEL/ (MayAi.cs: Claude / Antigravity, model, effort, gọi CLI, đọc dòng; DanhMucModel.cs)
│                        03_PHIEN_LICH_SU/ (LichSu.cs hội thoại mới, lịch sử, link phiên; PhienAi.cs đọc phiên, chỗ skill -> cờ CLI, MCP) · 04_DU_AN_SKILL/ (DuAnSkill.cs thư mục dự án, skill; ChonThuMuc.cs) · 05_DINH_KEM/ (DinhKem.cs ảnh, vùng chọn CAD)
├─ 13_YOUTUBE/           PLUGIN RIÊNG -> TKPCCC_YouTube.bundle (TKYOUTUBE): BangYoutube.cs · LenhYoutube.cs · GanBrave.cs · webview2/ (3 DLL NuGet 1.0.2903.40) · brave_khung/ (extension Brave)
└─ 14_HUB/               Hub.cs -> bin/TKPCCC.Hub.exe (cài mọi thứ: Nền tảng · MCP cad · Bot chat · YouTube)
```
Hạng mục tính năng ↔ trang palette: Quy trình vẽ = 06–08 + `09_PCCC/01` · Báo cháy = `09_PCCC/01` + `02` · Chữa cháy = `09_PCCC/03` · Thống kê = `09_PCCC/05` · Thư viện = `09_PCCC/06` · Tiện ích = 10 · Lệnh, Cài đặt = 05.

## Vẽ tự động chạy thế nào
```
AI (Claude Code, VS Code, Antigravity...) -> bin\TKPCCC.Mcp.exe -> hàng đợi file -> lệnh PCCC_SPEC trong AutoCAD -> bản vẽ
```
AI chỉ viết một gói thao tác ngắn (spec JSON), plugin sinh hình. Một gói = một bước Undo. Kết quả trả về chỉ gồm số đếm và lỗi.

## Sửa gì thì vào đâu
| Muốn | Sửa file |
|---|---|
| Đổi cỡ chữ, bóng trục, tên và màu layer trục, khoảng cách kích thước mặc định | `03_DU_LIEU_VE/ky_hieu/ky_hieu.json`, rồi chạy `build.bat` |
| Thêm hoặc đổi một thao tác vẽ (`luoi_truc`, `dat`, `ong`...) | `06_VE_TU_DONG_AI/cad/Spec.cs` (hàm `Op...` + 1 dòng trong `MotOp`) và `01_TRI_THUC_VE/TU_VUNG.md` |
| Đổi cách rải thiết bị, cách đi dây, đi ống (`rai`, `tuyen`, `PCCC_RAI`) | `09_PCCC/01_RAI_THIET_BI/cad/Rai.cs`; tuyến `09_PCCC/03_CHUA_CHAY/cad/Tuyen.cs`; cách vẽ gốc ở `01_TRI_THUC_VE/mau/mau-ban-ve-pccc.md`. Dời thiết bị vào trong vùng cách cạnh ≥ `tuong_min` (cạnh xiên, khe hẹp): `DoiVao`, `KepDay`, mặc định `CACH_TUONG_MIN_M` |
| Đổi cách tự nhận diện tường / trục, dựng vùng phòng khi bấm điểm (`vung_tu_dong`, `PCCC_RAI` bấm điểm) | `07_KIEN_TRUC/cad/NhanVung.cs` (từ khoá layer `LOP_BO`, `LOP_TUONG`, khe cửa `HO_CUA_M`; khe có cửa / tường 2 nét tới `HO_CUA_MAX_M`, `GomCua`, `SongDoi`; ô X `TachOX`; gạch chéo hiện hữu `LaGachCheo`) |
| Đổi câu hướng dẫn AI nhận lúc kết nối | phần đầu `01_TRI_THUC_VE/TU_VUNG.md`, trước dòng `---` (giữ dưới 1900 ký tự) |
| Đổi tên, mô tả công cụ MCP | `06_VE_TU_DONG_AI/mcp/Mcp.cs`, hàm `CongCu` |
| Đổi cách vẽ sơ đồ nguyên lý (`so_do`) | `09_PCCC/04_SO_DO_NGUYEN_LY/cad/SoDo.cs`; cách vẽ, nguồn ở `01_TRI_THUC_VE/ve/so-do-nguyen-ly.md` |
| Đổi cách in ảnh (`anh`, `cad_anh`): khổ, thiết bị in, chỗ lưu PNG | `06_VE_TU_DONG_AI/cad/Spec.cs`, hàm `InAnh` |
| Đổi cách nhập PDF làm nền (`pdf`) | `06_VE_TU_DONG_AI/cad/Spec.cs`, hàm `NhapPdf` |
| Đổi cách tính tỉ lệ trang PDF từ chuỗi kích thước | `.claude/skills/tkpccc-ve-cad/scripts/pdf_ti_le.py` (kiểm bằng `04_KIEM_THU/chay_thu.ps1`) |
| Đổi nhịp xem hàng đợi, điều kiện "AutoCAD đang rảnh" | `06_VE_TU_DONG_AI/cad/Cau.cs` |
| Plugin YouTube: trang chủ, cách tìm, dừng video khi đóng; cửa sổ (`TKYOUTUBE`); đổi bản WebView2 (thay 3 DLL trong `13_YOUTUBE/webview2/`) | `13_YOUTUBE/BangYoutube.cs`, `LenhYoutube.cs`; chép DLL ở `build.bat`; chế độ Brave (`yt_trinh_duyet`, tìm brave.exe: `HangDoi.TimBrave`; `MoBrave`: hồ sơ riêng `%LOCALAPPDATA%\TKPCCC\Brave`, cửa sổ thường có tab) cũng ở đây; toàn màn hình trong khung Brave: extension `13_YOUTUBE/brave_khung/` (build.bat chép cạnh DLL, nạp bằng `--load-extension`) |
| Nút ☰ đầu palette (menu phân hệ, thu gọn thanh dọc `ben_an`) | `05_NEN_TANG/ui/BangChinh.cs` (`DauTrang`, `MenuPhanHe`) |
| Hub: thêm thành phần / nhóm mới (mục, cách kiểm, cách cài, trang tải), câu hỏi xác nhận, cửa sổ | `14_HUB/Hub.cs` (`DanhSach`, `SuaJson`, `ChayCli`, `CuaSoHub`); test `04_KIEM_THU/thu_hub.ps1` |
| Đổi chỗ đặt hàng đợi | `05_NEN_TANG/chung/HangDoi.cs` hoặc biến môi trường `TKPCCC_CAD_HANG_DOI` |
| Lệnh gõ tay `PCCC_*` | `05_NEN_TANG/cad/Lenh.cs` (xem skill `tkpccc-cad-console`) |
| Tường, vách, cửa (`tuong`, `cua`, `kt_chuoi`; lệnh `PCCC_TRUC`, `PCCC_TUONG`, `PCCC_CUA`) | `07_KIEN_TRUC/cad/KienTruc.cs`; layer, dày, rộng cửa mặc định ở nhóm `kien_truc` của `03_DU_LIEU_VE/ky_hieu/ky_hieu.json`, chuẩn `01_TRI_THUC_VE/ve/quy_uoc.md` |
| Dim tự động (`PCCC_ADIM`, `PCCC_CDIM`, lõi chuỗi dim của `kt_chuoi`) | `08_KICH_THUOC/cad/Dim.cs` |
| Thêm vật tư, thiết bị | `03_DU_LIEU/vat_tu.json` ở GỐC repo (dùng chung với báo giá), rồi `build.bat`, `PCCC_LIB` |
| Đưa block của mình (kể cả block động) vào thư viện dưới một mã mới | trong AutoCAD: `PCCC_THEMBLOCK` hoặc nút "Thêm từ bản vẽ" của bảng `PCCC_BLOCK`; mã nguồn `09_PCCC/06_THU_VIEN_BLOCK/cad/Block.cs` |
| Tách khu vực `PCCC_TACHVUNG`, đặt tên vùng `PCCC_KHUVUC`, vòng tròn làm vùng (`DaGiacTron`) | `10_TIEN_ICH/cad/TienIch.cs`; vùng tròn đọc ở `NhanVung.cs` `MatChon` |
| Kiểm vùng mù đầu báo (`PCCC_KT`) và các tiện ích chỉnh sửa, kiểm tra (`PCCC_TRUNG`, `PCCC_LOPCHUAN`, `PCCC_DANHSO`, `PCCC_XOAY`, `PCCC_TYLE`, `PCCC_DOIBLOCK`, `PCCC_CANDIM`, `PCCC_DOIDN`) | `10_TIEN_ICH/cad/TienIch.cs` |
| Trang con ADIM 5 bước (layer, DimStyle, lọc, phương) | `08_KICH_THUOC/ui/BangAdim.cs`; lệnh `08_KICH_THUOC/cad/Dim.cs` (`AdimPhuong`) |
| Thẻ phân hệ Chữa cháy, Thống kê (lưới thẻ) | `05_NEN_TANG/ui/PhanHe.cs` (thêm thẻ = 1 dòng) |
| Trang Tiện ích của palette (`PCCC_TIENICH`: 27 thẻ, mã tắt, nhóm) | `10_TIEN_ICH/ui/BangTienIch.cs`, mảng `THE` (thêm thẻ = 1 dòng) |
| Vùng loại trừ `PCCC_LOAITRU`, ống gió (layer ONG_GIO / DUCT, đầu báo dịch ra), vẽ tạm xem trước `PCCC_XEM_TRUOC` của `PCCC_RAI` | `09_PCCC/01_RAI_THIET_BI/cad/LoaiTru.cs`, hàm `TachLoaiTru`, `TachOngGio` (từ khoá `DUOI_ONG_GIO`), `DichRaNgoai`, `XoaXemTruoc` |
| Dây báo cháy nối tiếp thiết bị (`PCCC_DAY`, dây loop); đầu báo tia chiếu `BC_BEAM` vẽ tia (`PCCC_RAI`) | `09_PCCC/02_BAO_CHAY/cad/DayTay.cs` (`DayTay`); tia `09_PCCC/01_RAI_THIET_BI/cad/Tia.cs` (`TiaVung`, `VeTia`) |
| Gợi ý loại đầu báo theo công năng (trang Báo cháy) | bảng "Loại theo khu vực" ở `01_TRI_THUC/07_BAO_CHAY/chon-loai-dau-bao.md` gốc repo (build chép cạnh DLL) |
| Đổi trang Bố trí BC (`PCCC_BOTRI`: thiết bị theo tiêu chuẩn, mức tối ưu k, nút BỐ TRÍ gửi `PCCC_RAI` điền sẵn) | `09_PCCC/02_BAO_CHAY/ui/BangBoTri.cs`; trị số ở `03_DU_LIEU/khoang_cach_rai.json` gốc repo; từ khoá `HeSo` ở `09_PCCC/01_RAI_THIET_BI/cad/Rai.cs`, hàm `RaiTay` |
| Đổi palette chính `PCCC` (thẻ quy trình 6 bước, trang Cài đặt) | `05_NEN_TANG/ui/BangChinh.cs`; màu 3 kiểu giao diện ở `05_NEN_TANG/ui/GiaoDien.cs`, hàm `BangMau` |
| Plugin Bot chat (`TKBOTCHAT`), theo 5 mục con của `12_BOT_CHAT/` | khung chat, ô nhập, bong bóng (`Ghi`, `XepTin`), menu + và Cài đặt (`MenuCong`, `MenuCaiDat`), chip (`VeChip`): `01_KHUNG_CHAT/BotChat.cs`; widget `GiaoDienChat.cs`; palette, theo dõi chọn (`ImpliedSelectionChanged`), lệnh `TKBOTCHON`: `LenhBotChat.cs` · máy AI (Claude `claude -p` có MCP cad / Antigravity `agy -p`), model, effort, gọi CLI (`Gui`, `DocDong`, `MenuModel`): `02_MAY_AI_MODEL/MayAi.cs`; danh mục model `DanhMucModel.cs` (Antigravity đọc `agy models`) · hội thoại mới, lịch sử, link phiên (`MoLichSu`, `Link`): `03_PHIEN_LICH_SU/LichSu.cs`; đọc phiên + chỗ chứa skill `PhienAi.cs` (`KhoPhien`) · thư mục dự án (`--add-dir`, `MenuDuAn`), skill (`MenuSkill`): `04_DU_AN_SKILL/DuAnSkill.cs`; hộp chọn thư mục `ChonThuMuc.cs` · dán / kéo thả ảnh (`DanAnh`), thẻ vùng chọn CAD (`MoTaVung`, handle gửi kèm): `05_DINH_KEM/DinhKem.cs`; AI sửa vùng bằng thao tác spec `sua`, `doc gi:"dt"` (`06_VE_TU_DONG_AI/cad/Spec.cs`) |
| Thanh biểu tượng trái (màu, emoji mỗi phân hệ), nút tròn biểu tượng, tab viên thuốc, tiêu đề nhóm, khung TRẠNG THÁI | thanh: `05_NEN_TANG/ui/BangChinh.cs` (`NutBen`, mảng `icon`, `mau`); widget: `05_NEN_TANG/ui/GiaoDien.cs` (`NutIcon`, `ThanhTab`, `TieuDeNhom`, `KhungTrangThai`, `MauNhom`) |
| Ghi m² từng khoang dầm + dấu X khoang dưới ngưỡng (layer PCCC_KHOANG_M2), công tắc `kd_ghi_m2` | `09_PCCC/01_RAI_THIET_BI/cad/KhoangDam.cs` (`GhiKhoang`); công tắc ở tab Khoang dầm `09_PCCC/02_BAO_CHAY/ui/BangBoTri.cs` |
| Ống nhánh tự chọn DN theo số đầu phun phía sau (`PCCC_TUYEN` TuDong, mức Den05 / Tren05), chữ DN | `09_PCCC/03_CHUA_CHAY/cad/Tuyen.cs` (`TuyenXuongCa` tham số `dem`, `BangDn`, `MaDn`, `ChuDn`); bảng số đầu theo DN ở `03_DU_LIEU/ong_nhanh_dn.json` gốc repo (build.bat chép cạnh DLL); thẻ palette ở `05_NEN_TANG/ui/PhanHe.cs` |
| Trang Báo cháy 4 tab (Rải đầu báo · Khoang dầm · Kiểm tra · Đi dây), nội dung khung TRẠNG THÁI, nút Tra cứu TCVN | `09_PCCC/02_BAO_CHAY/ui/BangBoTri.cs` (`TaoKhoangDam`, `CapNhatTrangThai`); đếm vùng `Lenh.DemVung` ở `09_PCCC/02_BAO_CHAY/cad/DayBc.cs`; bảng tra `09_PCCC/02_BAO_CHAY/ui/BangTraCuu.cs` đọc `02_DIEU_KHOAN/07_BAO_CHAY/MOD_dau_bao_khoi_khoang_dam.md` gốc repo |
| Đổi bảng block (cột, ô lọc, nút) | `09_PCCC/06_THU_VIEN_BLOCK/ui/BangBlock.cs`; cách vẽ ảnh xem trước ở `09_PCCC/06_THU_VIEN_BLOCK/cad/Block.cs`, hàm `VeAnhBlock` |
| Sửa hình block thiết bị | `TKPCCC.bundle/Contents/PCCC_Blocks.dwg` bằng BEDIT, rồi `PCCC_LIB` |
| Bản quyền: key, mã máy, chặn lệnh, cấp key qua Google Sheet | `11_BAN_QUYEN/cad/BanQuyen.cs`, `11_BAN_QUYEN/` (đọc `HUONG_DAN.md`); build khoá lệnh: `build.bat phat_hanh` |
| Thêm một luật vẽ, nối tới quy chuẩn | 1 dòng trong `02_LUAT_VE/LUAT.md` |
| Ghi bài học khi vẽ | luật -> đúng file `01_TRI_THUC_VE/nhan_dang/` hoặc `01_TRI_THUC_VE/ve/`; số đo bản mẫu -> `01_TRI_THUC_VE/mau/` (mục lục `01_TRI_THUC_VE/README.md`) |
| Bảng KÝ HIỆU mặt bằng `PCCC_KYHIEU`, bảng quy định lắp đặt đầu báo `PCCC_QUYDINH`, thao tác spec `bang` | `09_PCCC/05_THONG_KE/cad/BangMb.cs` (trị số từ `03_DU_LIEU/khoang_cach_rai.json`) |
| Thuyết minh theo hệ có trên bản vẽ `PCCC_THUYETMINH` (mục chọn ở `MUC_THEO_HE`) | `09_PCCC/05_THONG_KE/cad/BangMb.cs`; văn mẫu `01_TRI_THUC_VE/ve/mau-thuyet-minh-pccc.md` (build.bat chép cạnh DLL) |
| Dim bước tia beam (`PCCC_RAI` BC_BEAM, layer `PCCC_BC_TIA_KT`); nhãn zone + diện tích, điện trở cuối dây (`PCCC_DAY`) | `09_PCCC/01_RAI_THIET_BI/cad/Tia.cs` (`KtTia`), `09_PCCC/02_BAO_CHAY/cad/DayTay.cs` (`DayTay`) |
| Rải theo khoang dầm (`PCCC_RAI` từ khoá Dam, đầu khói): bấm nét DẦM mẫu, độ sâu d từ ký hiệu b x h (không có thì hỏi cỡ chung / TungDam), d ≤ 0,3 m bỏ dầm; khoang ≥ 9 m² (trần ≥ 4 m) / 4 m² (2–4 m) rải từng khoang, khoang nhỏ gom rải như trần phẳng | `09_PCCC/01_RAI_THIET_BI/cad/Rai.cs` (`khoangDam`, `hopNho`); nguyên văn 5.9.1.1.6 ở `01_TRI_THUC/07_BAO_CHAY/tcvn7568-14-2025.md` |
| Hatch đường thoát nạn `PCCC_THOATNAN`; vòng bán kính tủ vách tường in được (`PCCC_VUNGBV` mã hệ HVT) | `10_TIEN_ICH/cad/TienIch.cs`, `09_PCCC/03_CHUA_CHAY/cad/VungBaoVe.cs` (`VungBaoVe`) |
| Làm gọn vùng tự dựng: bỏ khấc cột, cung cửa, gờ nhỏ hơn 1 m, ra đa giác vuông góc (`PCCC_RAI`, `vung_tu_dong`) | `07_KIEN_TRUC/cad/NhanVung.cs` (`LamGon`, ngưỡng `LAM_GON_M`) |
| Dim tự động sau `PCCC_RAI`: một chuỗi X, một chuỗi Y, trùng hàng / cột chỉ dim một lần; kiểu dim, cách dim, layer dim | `08_KICH_THUOC/cad/DimRai.cs` (`KtLuoi`, `KtSauRai`); cài đặt ở `05_NEN_TANG/cad/CaiDat.cs` + trang Cài đặt `05_NEN_TANG/ui/BangChinh.cs` (`CaiDim`) |
| Đi dây báo cháy `PCCC_DAYBC` (hệ thường: một đường, điện trở cuối ở thiết bị xa tủ nhất; hệ địa chỉ: mạch vòng module, đầu báo thường về ZM, chuông về CM / R2ML) | `09_PCCC/02_BAO_CHAY/cad/DayBc.cs`; cách vẽ gốc ở `01_TRI_THUC_VE/ve/bao_chay/day_zone.md` |
| Bảng layer dây báo cháy, ống chữa cháy (thêm, xoá, lấy từ bản vẽ); chọn dây cho từng loại thiết bị | `05_NEN_TANG/cad/CaiDat.cs` (`LOP_MAC_DINH`, `VAI_TRO`); trang Cài đặt `05_NEN_TANG/ui/BangChinh.cs` (`BangLopDay`); trang Báo cháy mục 6 `09_PCCC/02_BAO_CHAY/ui/BangBoTri.cs` (`TaoDiDay`) |

## Build và kiểm thử
- `07_CAD\build.bat`: ra `TKPCCC.bundle\Contents\TKPCCC.Cad.dll`, `TKPCCC.UI.dll` và `bin\TKPCCC.Mcp.exe`. AutoCAD đang mở thì tắt hẳn rồi mở lại để nạp bản mới.
- `powershell -ExecutionPolicy Bypass -File 07_CAD\04_KIEM_THU\thu_gui.ps1` (không bắt buộc, chạy khi sửa giao diện hoặc cầu nối): tự mở một AutoCAD thật 1–2 phút rồi tự tắt, thử bảng `PCCC_BLOCK` và đường MCP thật. Chỉ chạy khi không có AutoCAD nào đang mở.
- `powershell -ExecutionPolicy Bypass -File 07_CAD\04_KIEM_THU\chay_thu.ps1`: plugin + `PCCC_SPEC`, không cần mở AutoCAD.
- `powershell -ExecutionPolicy Bypass -File 07_CAD\04_KIEM_THU\thu_mcp.ps1`: máy chủ MCP + hàng đợi + plugin, không cần mở AutoCAD.
- Cả hai phải ra `TAT CA DAT`.

## Dùng
1. Chạy `07_CAD\build.bat`, rồi `07_CAD\cai_dat.bat` MỘT lần (liên kết bundle để AutoCAD tự nạp mỗi lần mở; gỡ bằng `07_CAD\cai_dat.bat /go` trong cmd / PowerShell thường: sandbox của Claude không đi qua và không xoá được liên kết này).
   Đã kiểm 15/11 (ngoài sandbox, liên kết đúng đích): accoreconsole KHÔNG tự nạp bundle này, bộ test không bị ảnh hưởng. AutoCAD có giao diện tự nạp: chưa thử.
   Cách khác: `APPLOAD` file `05_NEN_TANG\lisp\TKPCCC.lsp`, hoặc `NETLOAD` file `TKPCCC.Cad.dll` (file này tự nạp luôn `TKPCCC.UI.dll`). Gõ `PCCC` để mở palette chính (`PCCC_BLOCK` mở thẳng phân hệ Thư viện, `PCCC_HELP` phân hệ Lệnh).
2. Mở Claude Code ở gốc repo hoặc tại `07_CAD/`: file `.mcp.json` khai máy chủ `cad`. AI khác thì khai lệnh chạy là đường dẫn tới `07_CAD\bin\TKPCCC.Mcp.exe`.
3. Gõ yêu cầu vẽ. Chạy tay một spec: trong AutoCAD gõ `PCCC_SPEC` rồi dán đường dẫn file `.json`; kết quả nằm ở `<tên>.kq.json`.
