# Tiến độ CAD (riêng cho thư mục cad/)

Phiên mới làm việc CAD: đọc file này trước, rồi `cad/README.md` (cây thư mục, sửa gì vào đâu). Kế hoạch tổng G0–G9: `cad/KE_HOACH.md`. Nhật ký: `cad/LICH_SU.md`. Luật riêng: `cad/CLAUDE.md`.
Luật: dùng tới 90 % hạn mức thì dừng, ghi file này + `cad/LICH_SU.md`, commit, push; usage hồi lại thì tự làm tiếp, không hỏi (`cad/CLAUDE.md`).

## VIỆC 14/11 CHIỀU — PCCC_RAI TREO CAD, ĐỔI SANG QUÉT CHỌN MẶT BẰNG (người dùng báo, ảnh: bấm điểm trong kho `PCCC HOÁ CHẤT.dwg` thì AutoCAD đứng)
Nguyên nhân: nhánh bấm điểm gọi `VungBam` (`NhanVung.cs`) đọc MỌI đối tượng model space (`DocDs`, `GeometricExtents`, explode mọi block), nới ô dò gấp đôi
tới cả bản vẽ khi phòng lớn hoặc hở, `MatChua` cắt đoạn cỡ O(m²), dò 2 lượt (chỉ tường rồi mọi nét); hỏng thì rơi xuống `Editor.TraceBoundary` trên cả bản vẽ (treo kinh điển).
Cách làm (người dùng chốt): sau mã + trần + theo hàng, `GetSelection` "Quét chọn mặt bằng"; chỉ dựng đoạn từ đối tượng đã chọn (một lần), tính mọi mặt kín một lần;
polyline kín layer `PCCC_VUNG` trong vùng chọn dùng thẳng làm vùng; dấu nhắc "Bấm điểm trong phòng [TatCa] <TatCa>": Enter ngay = mọi phòng (bỏ mặt hẹp là lòng tường,
2A/P < 0,5 m hoặc < 1 m²), bấm điểm = chỉ phòng đó; vẽ vùng lên `PCCC_VUNG` rồi rải. Bỏ `TraceBoundary`, bỏ `VungBam`. Test: bỏ đáp 'D' trong `chay_thu.ps1` (dòng RAI) và `thu_gui.ps1`;
dòng `2610500,1200` thêm khung chọn quanh 2 phòng ở gốc 2600000. Đổi model 14/11: `cad-ve` Sonnet (bỏ Haiku), Gemini mặc định `gemini-4-argon-medium` (bỏ 3.8 Flash).
TRẠNG THÁI (dừng 90 % lúc ~11:05 14/11): MÃ ĐÃ VIẾT, build đạt (`MatChon`, `PhongThat`, `CacMat`, `BoThangHang` ở `NhanVung.cs`; vòng mới ở `Rai.cs` `RaiTay`), CHƯA ĐẠT TEST:
`chay_thu.ps1` 89 DAT / 4 LOI. Lỗi gốc: "PCCC_RAI bam diem trong phong khong co polyline" (bản sao `vung_ve2.json` gốc 2700000, đáp `'C','2699000,-1000','2714000,6000','','2710500,1200',''`);
3 lỗi còn lại (`rai da giac canh xien` 0 đầu báo, `so_do` kiểu chữ, `BC_COI_DEN`) đọc file LISP ghi SAU dòng đó ⇒ nhiều khả năng kịch bản lệch đáp án ở `PCCC_RAI`, không phải lỗi riêng.
VIỆC KẾ: in `$out2` quanh "Quét chọn" để xem dấu nhắc nào nuốt đáp án (ngờ: `GetPoint` có từ khoá `TatCa` + `AllowNone`; hoặc `MatChon` không ra mặt vì `vung_ve2` khác), sửa, chạy lại
`chay_thu.ps1` + `thu_mcp.ps1` tới TAT CA DAT.
NGƯỜI DÙNG BÁO THÊM 14/11 (ảnh `ket_qua/cad_test/2026-11-14/2_ca_ban_ve_mot_block.png`, không commit): bản vẽ mặt bằng xưởng bao bì là MỘT BLOCK duy nhất (cả khung tên, bảng thống kê);
plugin coi cả block là một vùng, rải đầu báo đều khắp tờ giấy (cả khung tên, ngoài nhà). Người dùng đã KẺ NÉT PHÂN VÙNG: polyline dày màu XANH LÁ (Xưởng 1, Xưởng 2, phòng nhỏ)
và ĐỎ (khối văn phòng). Sửa (làm NGAY sau khi test đạt): sau quét chọn hỏi "Chọn một nét phân vùng mẫu, Enter = tự nhận từ tường"; có mẫu thì chỉ lấy nét (kể cả nét trong block,
explode đệ quy như `ThuNet`) cùng layer + màu thật (ByLayer/ByBlock quy ra màu) với mẫu, cho phép nhiều màu (chọn mẫu xanh rồi đỏ), dựng mặt kín từ các nét đó rồi rải từng mặt;
bỏ mặt bao ngoài / khung giấy. Trước khi viết: dùng MCP `doc` gi:"layer" trên bản vẽ đang mở xem layer, màu, độ dày thật của nét xanh, đỏ (bản vẽ chưa biết đường dẫn: hỏi người dùng nếu cần).
Phép kiểm: dựng block chứa khung + 2 polyline xanh + 1 đỏ, rải chỉ ra đầu báo trong 3 vùng, 0 đầu báo ngoài. Tiếp: thêm 1 phép kiểm nhánh Enter = mọi phòng trên mặt bằng 2 phòng (ra 2 vùng, bỏ mặt bao ngoài); `cad\build.bat` vào bundle; báo người dùng tắt hẳn AutoCAD, mở lại, thử trên `PCCC HOÁ CHẤT.dwg`.

## VIỆC 14/11 — XONG PHẦN MÃ, đã gộp vào `main` (kế hoạch, hợp đồng key: `cad/tien_do/viec_14_11.md`; nhật ký `cad/LICH_SU.md`)
Quy trình vẽ 6 bước + `tuong`/`cua`/`kt_chuoi` + `PCCC_TRUC/TUONG/CUA/ADIM/CDIM`; palette `PCCC` (bot chat, Cài đặt 3 giao diện); ảnh block nền đen;
bản quyền (`PCCC_BANQUYEN`, KeyGen, Apps Script). Test tự động đạt hết. VIỆC KẾ, theo thứ tự:
1. XONG 14/11 10:40: `thu_gui.ps1` 12/12 trên AutoCAD thật (palette `PCCC`, 8 thẻ quy trình, Thư viện 43 block ảnh nền đen, Lệnh 20 lệnh, MCP thật); palette hẹp:
   khung Hành động xuống đáy, ẩn cột phụ. CÒN xem tận mắt: đổi giao diện tại chỗ, bot chat chạy cùng cầu nối,
   `PCCC_BANQUYEN`, chặn lệnh ở bản `phat_hanh` (giả định `GlobalCommandName` có "#" khi lệnh kết thúc chưa thử). Bot chat cần `claude` CLI đăng nhập lại (`claude` → `/login`; 14/11 báo OAuth hết hạn).
2. Review Opus max (`cad-review`) cho mốc 13/11 + 14/11 (`git diff 4ba0ba7..HEAD -- cad`).
3. Người dùng làm 1 lần theo `cad/module/banquyen/HUONG_DAN.md` (tạo khoá, Sheet, Apps Script, web app, URL vào `cad/data/ban_quyen/cau_hinh_ban_quyen.json`), rồi thử Apps Script thật.
4. Rủi ro gói A: tường giao chữ T còn nét thò qua mặt tường; `cua` chỉ cắt tường hai nét thẳng; CDIM nhớ chuỗi cho cả phiên. Gói B: phân hệ Lệnh (bảng 880 px) chật trong palette 400 px;
   nút Dừng bot chỉ tắt `claude.cmd`, không tắt tiến trình con; chưa có block màu để thử nhánh ByLayer/ByBlock/true color.
5. Để sau (ý của file tham khảo): phân hệ Báo cháy có thanh trượt k % + KPI, xem trước TransientGraphics, hoàn tác lượt bố trí, tiện ích đánh số / kiểm trùng / kiểm layer.
6. XONG mã 14/11 (người dùng yêu cầu): gõ `?` ở dấu nhắc mã (`PCCC_RAI`, `PCCC_DAT`, `PCCC_TUYEN`, `PCCC_VUNGBV`, `PCCC_GAN`) mở bảng chọn
   `ui/BangChonVatTu.cs` (ký hiệu, tên, mã, tìm không dấu, Enter/bấm đúp chọn) thay cho in dòng lệnh; gõ chữ khớp nhiều mã cũng mở bảng lọc sẵn.
   Đã thấy trên AutoCAD thật (script gỡ lỗi, COM): bảng "TKPCCC - Mã thiết bị cần rải" mở đúng, 520 x 625. DỞ (dừng 93 % lúc ~11:10 14/11): phép kiểm cuối `thu_gui.ps1`
   (khối "Go ? o dau nhac ma") gửi lệnh qua COM `SendCommand` trong `Start-Job` KHÔNG ỔN ĐỊNH: `RPC_E_CALL_REJECTED` khi AutoCAD bận, lần ra 57 BC_KHOI, lần không mở bảng,
   một lần bảng tự đóng sau ~22 s (chưa rõ vì sao). VIỆC KẾ: bỏ COM + job; đưa vào `gui.scr` (dòng 18) ngay sau 'PHX': `'PCCC_DAT', '?'` + đáp điểm đặt (đọc dấu nhắc `DatThietBi`
   trong `Lenh.cs` ~dòng 477) — KHÔNG dùng `PCCC_RAI` vì phiên khác đang làm lại dấu nhắc RAI (`d20e5a9`, chưa đạt test). Kiểm bảng NGAY sau `song.json`
   (dời `Add-Type` UIA + `WinU` lên trước), lọc 'dau bao khoi', bấm nút "Chọn" (InvokePattern), kịch bản tự đáp tiếp; cuối test `dem` BC_KHOI = 4 (rai MCP) + số đặt.
   Rồi `thu_gui.ps1` tới TAT CA DAT, `chay_thu.ps1`, `thu_mcp.ps1`.
7. Gemini (skill `tkpccc-phoi-hop-model` gốc repo): CHẠY ĐƯỢC 14/11 qua Antigravity CLI `agy` (Gemini CLI bị Google từ chối tài khoản cá nhân);
   thử PDF mẫu đúng 10/10. CÒN: đo token trước/sau trên
   `ket_qua/cad_mau/drive_goc/cong_trinh.pdf` (bóc trục, kích thước trang mặt bằng), ghi `LICH_SU.md`.

## NGƯỜI DÙNG TEST 13/11 — 4 VIỆC LÀM SONG SONG VỚI VIỆC DỞ Ở PHIÊN SAU (dặn: chỉ ghi nhận, phiên reset 5 giờ kế tiếp mới làm)
**XONG CẢ 4 VIỆC (13/11 chiều)**: 3 phiên song song trên 3 nhánh, đã gộp vào `main` (sửa tay xung đột `cad/skills/TU_VUNG.md`, `cad/test/chay_thu.ps1`;
phép kiểm `vung_tu_dong` dời sang gốc x 2600000). Sau gộp: build đạt, `chay_thu.ps1` + `thu_mcp.ps1` TAT CA DAT. Chi tiết, việc còn lại từng mục: `cad/tien_do/viec1.md`,
`viec2.md`, `viec3_4.md`. CHƯA: review Opus max cho mốc này; xem dim chéo góc và `vung_tu_dong` trên DWG thật của người dùng; `thu_gui.ps1` sau gộp chưa chạy lại.
Ảnh người dùng gửi: `ket_qua/cad_test/2026-11-13/` (không commit). Làm song song = chia agent theo file không chồng nhau (Rai.cs / module/ui / SoDo.cs), mỗi lúc chỉ một agent sửa một file.
1. RẢI SAI KHOẢNG CÁCH (`1_rai_6771.png`): vùng đa giác lồi lõm, người dùng đặt bước tối đa 10,5 m, cách tường tối đa 1/2 bước (5,25 m), nhưng có đầu báo cách tường 6771 mm
   (đo theo phương đứng tới cạnh xiên dưới). Sửa `rai` / `PCCC_RAI` để mọi điểm của vùng (kể cả góc nhọn, cạnh xiên) nằm trong bán kính phủ theo bước và cách tường; thêm phép kiểm với chính đa giác đó.
   Kèm: KHÔNG hỏi người dùng khoảng cách hay m². Nối với tiêu chuẩn báo cháy (skill `tkpccc-tra-quy-chuan`, `knowledge/tcvn7568-14-2025.md`, `cad/rule/LUAT.md`): lấy bước, cách tường,
   diện tích bảo vệ tối đa theo loại đầu báo + chiều cao trần (và sprinkler theo TCVN 7336) rồi tự thiết kế. Trị số vẫn đọc từ file luật/knowledge, không viết cứng trong mã C#.
   Kèm (dặn 13/11, ảnh `5_dim_cheo_goc_7m2.png`): đầu báo GẦN GÓC phòng phải có kích thước CHÉO (aligned) từ đầu báo tới góc tường, ví dụ 7,2 m; KHÔNG vẽ vòng tròn bán kính.
   Khoảng cách chéo góc tối đa lấy theo tiêu chuẩn báo cháy (skill `tkpccc-tra-quy-chuan` / `knowledge/`), không viết cứng. Ảnh mẫu còn ghi kích thước bước (10,2) và cách tường (5,1) theo hai phương.
2. TỰ NHẬN DIỆN TRỤC / TƯỜNG (`2_mat_bang_khong_polyline.png`): người dùng rải trên mặt bằng kiến trúc thật mà không vẽ polyline kín thì plugin báo lỗi. Phải tự phân biệt đâu là trục
   (nét chấm gạch, layer trục), đâu là tường (cặp nét song song, layer tường, cột), tự dựng polyline vùng từng phòng; người dùng CLICK vào một vùng là rải theo mục 1.
3. GIAO DIỆN PANEL BLOCK (`4_panel_block_hien_tai.png` bị chê xấu, đơn điệu): tham khảo GIAO DIỆN (không chép tính năng) của `3_mau_ux_thong_ke_block.png`:
   bảng có cột ảnh ký hiệu lớn, tên đậm, cột đơn vị, số lượng tô màu, nút thao tác tròn màu ở đầu dòng; khung "Hành động" bên phải với nút xanh đậm bo góc, ô chọn, ảnh xem trước,
   ô tổng số nổi bật; thanh tìm kiếm trên đầu; tiêu đề nhóm nền xanh. File `cad/module/ui/BangBlock.cs`; sửa xong chạy `thu_gui.ps1` (chỉ khi không có AutoCAD nào mở).
4. `PCCC_HELP` THÀNH PANEL: bảng danh sách mọi lệnh (tên lệnh, việc làm, cách dùng ngắn), tìm kiếm được, SỬA ĐƯỢC PHÍM TẮT (alias) ngay trong panel, lưu lại cho lần sau
   (gợi ý: ghi alias vào file cạnh DLL hoặc acad.pgp của người dùng, có sao lưu; hỏi người dùng nếu phải sửa file cấu hình AutoCAD). Panel để ở `module/ui/`, cùng kiểu với panel Block mới.

## GHI 13/11 CUỐI NGÀY (tuần của người dùng còn ~5 %, dừng, chưa làm thêm)
- Chuỗi agent `cad-thiet-ke` → `cad-ve` → `cad-kiem` CHƯA chạy thử: cần AutoCAD có plugin. Nối tự động đã thử ĐƯỢC (không đổi cấu hình): PowerShell
  `[Runtime.InteropServices.Marshal]::GetActiveObject("AutoCAD.Application").ActiveDocument.SendCommand('(command "_.NETLOAD" "D:/1_Claude/TKPCCC/cad/TKPCCC.bundle/Contents/TKPCCC.Cad.dll") ')`
  rồi `cad_tt` ra `ok`. Lúc thử AutoCAD mở `PCCC HOÁ CHẤT.dwg` của người dùng (KHÔNG vẽ vào); chạy chuỗi trên bản chép `ket_qua/cad_mau/cad/t1_spr.dwg`.
- G7 Hub (người dùng muốn "cài MCP một lần, sau mở CAD là có"): làm `module/hub/` → `TKPCCC.Hub.exe` bản đầu gồm (1) autoload plugin: registry HKCU
  `Software\Autodesk\AutoCAD\R24.0\<ACAD-id>\Applications\TKPCCC` (LOADER, MANAGED=1, LOADCTRLS=2), có lệnh gỡ; (2) ghi `mcpServers` cho Claude Code/Desktop, sao lưu trước; (3) chẩn đoán + thử bắt tay MCP.
  Chạy thử trên thư mục giả trước; ghi registry/cấu hình máy thật phải hỏi người dùng. Việc nhỏ hơn có thể làm trước: cho `TKPCCC.Mcp.exe` tự NETLOAD qua COM như trên (Bước 6).

## TRẠNG THÁI (cập nhật 2026-11-13, 09:30) — ĐỌC TRƯỚC KHI LÀM TIẾP
- 13/11: `so_do` `bc` LÀM LẠI giống quy cách trang 11 hồ sơ thật (commit `b0af585`, `f07433c`): bảng ký hiệu (mẫu nét theo layer dây), ghi chú, bảng tính nguồn phụ lục C
  TCVN 7568-14:2025 (hệ số, dòng tĩnh/báo cháy từ spec; ra đúng 4081 mA, 8,20568 Ah), tủ có đèn zone, điện thoại, tiếp địa, nguồn 220 V/24 V, bó dây FR có elip, zone giám sát van,
  cột còi đèn, zone dự phòng, liên động ngang, tiêu đề. Nhãn zone 3 dòng: khu vực có tầng / DIỆN TÍCH BẢO VỆ: x M² / ZONE n (zone theo tầng lấy từ trang 5–8).
  Ký hiệu: block có sẵn trong bản vẽ được GIỮ (NapBlock không ghi đè, riêng PCCC_LIB cập nhật); bộ mặc định lấy từ nét PDF (`cad/data/ky_hieu/hinh_bc.json`, script
  `cad/.claude/skills/tkpccc-cad-console/scripts/hinh_tu_pdf.py`). Mã mới: `BC_CHI_THI`, `BC_CHI_THI_HL`, `BC_COI_DEN`, `BC_DIEN_THOAI`, `DAY_BC_10`, `DAY_BC_10_FR`, `DAY_NGUON_15`, `DAY_NGUON_15_FR`.
  Review Opus max (`ket_qua/cad_review/review_2026-11-13.md`): 0 CHẶN, 3 NÊN SỬA, 9 GHI CHÚ, 14 chỗ khác trang 11. ĐÃ SỬA 3 NÊN SỬA + cỡ thiết bị chỉ thị (tỉ lệ 1,47), dây nối nét đứt;
  `chay_thu.ps1` 78/78, `thu_mcp.ps1` 9/9.
  VIỆC KẾ: (1) XONG 13/11: mã `BC_KHOI_TRAN` (đầu báo khói trên trần, hình S khung vuông từ PDF), bảng ký hiệu có dòng này, ví dụ zone 4, 9 dùng mã này; (2) XONG 13/11: zone có chỉ thị HL cách hàng trên ≥ 60 pt
  (phép kiểm chỉ đo zone 4 vì `doc chu` trả tối đa 40 chữ; zone 9 chưa kiểm, chưa xem ảnh); (3) XONG 13/11 cuối phiên: cột đầu bảng tính nguồn rộng gấp 1,7 cột số (chưa xem ảnh; BUNDLE CHƯA BUILD LẠI sau sửa này: đầu phiên chạy `caduild.bat`); (4) các chỗ khác trang 11 còn lại trong review 13/11 (hàng zone không cách đều như bản gốc, ký hiệu theo cột cố định, cột bảng tính rộng đều); sinh sơ đồ từ MẶT BẰNG CAD
  báo cháy (đọc `dem` theo zone + diện tích vùng); hỏi người dùng kiểu sơ đồ sprinkler (không gian trục đo hay nguyên lý phẳng); đọc trang 46, 58, 63 khi làm sơ đồ âm thanh, hút khói, cấp điện.

## TRẠNG THÁI (cập nhật 2026-11-12, 19:05) (cũ)
- 19:05 ngày 12/11: G5 `so_do` đã có 2 hệ: `he`:"spr" (sơ đồ nguyên lý phẳng sprinkler) và `he`:"bc" (báo cháy hệ thường). Thiết bị trên layer `PCCC_SODO*` không vào `dem`/`PCCC_BOC`.
  Review Opus max (`ket_qua/cad_review/review_2026-11-12d.md`) 0 CHẶN; 4 NÊN SỬA đã sửa (commit `24b876f`), `chay_thu.ps1` 70/70, `thu_mcp.ps1` 9/9. 6 GHI CHÚ của review 12/11d chưa sửa.
  Lưu ý agent sửa báo: `bc` tự tăng `h` khi nhỏ mà không báo; `MaSoDo` vẫn nạp block thừa vì `DAY_BC`.
- HỒ SƠ THẬT người dùng gửi 12/11 (PDF 63 trang, có sơ đồ nguyên lý): `ket_qua/cad_mau/drive_goc/cong_trinh.pdf` (không commit). Mục lục tên bản vẽ từng trang: `ket_qua/cad_mau/drive_goc/muc_luc.txt`
  (người dùng dặn: quét tên bản vẽ trước, cần gì thì chỉ đọc đúng trang đó). Đã đọc trang 11 (nguyên lý báo cháy) và 22 (không gian sprinkler), ghi ở `cad/knowledge/mau-so-do-that.md`.
- VIỆC KẾ (ngày 13/11), theo thứ tự:
  1. XONG tối 12/11 (19:30): `so_do` `bc` theo trang 11 (bỏ vạch tầng, zone xếp trên tủ, bó dây trái, số dưới ký hiệu, `kv` + `dt` diện tích bảo vệ, zone giám sát van,
     `du_phong`, `lien_dong`); ảnh `ket_qua/cad_mau/cad/so_do/anh/so_do_bc_2.png`, chưa review. CÒN: bảng ký hiệu, bảng tính ắc quy, nguồn/tiếp địa/điện thoại,
     cột còi đèn riêng; mũi tên liên động đang đi xuống (bản gốc đi ngang). Làm tiếp các phần này rồi review Opus max cho G5.
  1b. NGƯỜI DÙNG DẶN 13/11: sơ đồ nguyên lý phải GIỐNG 100% quy cách trang 11 của hồ sơ thật (ký hiệu thật lấy từ nét PDF, bó dây FR, cột còi đèn, liên động ngang,
     nguồn + tiếp địa + điện thoại, bảng ký hiệu, bảng tính ắc quy, ghi chú, tiêu đề); bản đơn giản bị chê "quá đơn điệu". Áp dụng cả khi người dùng có MẶT BẰNG CAD BÁO CHÁY:
     đọc mặt bằng (`dem` theo zone, diện tích vùng) lấy số thiết bị, khu vực, diện tích từng zone rồi sinh sơ đồ. Nhãn cuối zone 3 dòng: Khu vực bảo vệ / Diện tích bảo vệ / Zone.
     KÝ HIỆU thiết bị tuỳ người, tuỳ bản vẽ (dặn 13/11): sơ đồ dùng đúng block của bản vẽ/mặt bằng hoặc thư viện người dùng; "giống 100%" là bố cục, bảng, chữ, dây. Bảng ký hiệu
     trên sơ đồ in lại chính các block đó. Ký hiệu lấy từ nét PDF mẫu chỉ làm bộ mặc định trong `PCCC_Blocks.dwg`.
     DÂY tín hiệu, dây nguồn cũng vậy (dặn 13/11): mỗi loại dây một layer riêng (nét, màu) theo bản vẽ người dùng; sơ đồ vẽ dây đúng layer đó, bảng ký hiệu in mẫu nét theo layer đó.
  2. HỎI NGƯỜI DÙNG: sơ đồ sprinkler vẽ kiểu không gian trục đo như hồ sơ thật (việc lớn) hay giữ nguyên lý phẳng.
  3. Đọc thêm trang 46 (âm thanh), 58 (hút khói), 63 (cấp điện) khi làm các sơ đồ đó; trang 23 (không gian vách tường).
  4. GHI CHÚ còn lại của review 12/11c, 12/11d; `thu_gui.ps1` + thử `cad-ve`/`cad-kiem` khi AutoCAD có giao diện.
- PUSH: các commit từ `f22d98b` trở đi CHƯA lên GitHub (push trong tab Terminal kẹt ở đăng nhập Git Credential Manager). Đầu phiên mai chạy `git push origin main` trong Terminal.
- 18:20 ngày 12/11: CHỐT MỐC Bước 5 phần mã. Review cuối bằng Opus max (`ket_qua/cad_review/review_2026-11-12c.md`): 0 CHẶN, 0 NÊN SỬA, 6 GHI CHÚ;
  `chay_thu.ps1` 58/58, `thu_mcp.ps1` 9/9, `npm test` 57/57. Tầng 1 bản mẫu vẽ lại ở `ket_qua/cad_mau/cad/lan4/`: 64/64 đầu phun, 2 OS&Y DN100, 2 công tắc dòng chảy, trục xả DN50,
  2 van xả khí (lệch mẫu dưới 60 mm); mã mới `SPR_VAN_OSY_DN65`..`DN150`, `SPR_VAN_XK` trong `data/vat_tu.json`. Đo token một lần vẽ: 70 nghìn token (chi tiết `cad/LICH_SU.md`).
  CÒN của Bước 5 (không phải mã, cần AutoCAD có giao diện): chạy thử `cad-ve` / `cad-kiem` qua MCP; `thu_gui.ps1`. Chưa có mã: van xả trên ống xả, đồng hồ cụm van xả khí; block van chưa vẽ hình thật.
  GHI CHÚ của review 12/11c để lại (sửa nhỏ, gộp vào lượt mã kế): `cot` so điểm nối dung sai 1e-6 nên dùng `saiSo`; thiếu phép kiểm cho `Rai.cs:189` và câu trả lời `Co` của `PCCC_RAI`;
  `cot` nối hình quạt dư ống; `theo_hang` hở góc khoét to hơn (có báo `ho`).
  VIỆC KẾ: theo `KE_HOACH.md` G5 `so_do` (sơ đồ nguyên lý), hoặc lệnh nạp dữ liệu người dùng (mục "NẠP DỮ LIỆU NGƯỜI DÙNG QUA PLUGIN").
- 17:10 ngày 12/11: XONG `PCCC_RAI` gõ tay hỏi thêm `le` + `theo_hang` (phép kiểm phòng 14 × 10 m, S 4, `le` 1,5 ra 4 × 3, bước 3650 × 3500); thử HL1 `s` 3500 ở
  `ket_qua/cad_mau/cad/lan4/`: HL1 10/10, tổng 64/64 như mẫu. `chay_thu.ps1` + `thu_mcp.ps1` TAT CA DAT, bundle build lại.
  VIỆC KẾ: trục xả DN50, van OS&Y, công tắc dòng chảy, van xả khí; đo token một lần vẽ; review cuối mốc Bước 5 bằng `cad-review` Opus max.
- 15:05 ngày 12/11: XONG 2 NÊN SỬA + GHI CHÚ `TU_VUNG.md` của review 12/11b (agent `cad-lap-trinh` Opus medium): `theo_hang` phòng chữ L có `dt` 12 ra 5 đầu; `cot` với `truc` chữ Z ra 6 đoạn 12,77 m.
  `chay_thu.ps1` + `thu_mcp.ps1` TAT CA DAT, bundle build lại. Dừng ở cửa sổ 5 giờ 87 % (người dùng dặn chạy tới 95 % rồi ghi, push; việc kế cần một agent nữa, dễ vượt 95 %).
  VIỆC KẾ: phần còn lại của Bước 5 (mục "Bước 5" dưới): vẽ trục xả DN50, van OS&Y, công tắc dòng chảy, van xả khí; thử HL1 `s` 3500 (tính tay ra 10/10);
  `PCCC_RAI` gõ tay thêm `le`/`theo_hang`; đo token một lần vẽ. Review cuối mốc bằng `cad-review` Opus max.
- 15:00 ngày 12/11: CHỐT MỐC Bước 5 phần a–f (commit `1c20265`). Review lại hẹp bằng Opus max (`ket_qua/cad_review/review_2026-11-12b.md`): 0 CHẶN, 2 NÊN SỬA, 2 GHI CHÚ;
  6 lỗi của review 12/11 chạy lại đúng đầu vào cũ đều đã hết; `chay_thu.ps1` 54/54, `thu_mcp.ps1` 9/9. Dừng ở cửa sổ 5 giờ 82 % (reset 17:30).
  VIỆC KẾ (giao `cad-lap-trinh` Opus, effort medium):
  (1) NÊN SỬA `Rai.cs:191`: `theo_hang` có `dt` thừa đầu ở phòng chữ L vì nMin = ceil(số cột × w / W) (phòng 10 × 6 m khoét WC 3 × 3 m: có `dt` 12 ra 6, không `dt` ra 5; Python: 14,6 % phòng chữ L thừa);
  (2) NÊN SỬA `Rai.cs:525-530`: `cot` với `truc` bậc chữ Z gộp các đoạn nối thành một đoạn chéo, một nhánh không nối vào đâu (9,61 m, đúng 12,77 m);
  (3) GHI CHÚ: cập nhật `TU_VUNG.md` cho `theo_hang`. Mỗi lỗi một phép kiểm trong `chay_thu.ps1`. Rồi phần còn lại của Bước 5 (trục xả, van, đo token).
- 14:45 ngày 12/11: agent `cad-lap-trinh` (Opus) SỬA XONG review 12/11 (`ket_qua/cad_review/review_2026-11-12.md`): 1 CHẶN (`pdf` với PDF có mật khẩu, chỉ khoá chống sửa, file hỏng:
  báo lỗi, không gọi `-PDFIMPORT`) + 5 NÊN SỬA (`cot` nối chồng, `xuong_ca`/`cot` đè ống chính khi `truc` có đỉnh thẳng hàng, `theo_hang` theo `s` thừa thiết bị, `theo_hang` theo `buoc` vượt `dt` không báo,
  phép kiểm phá hoà). 5 GHI CHÚ để nguyên. `chay_thu.ps1` 54/54, `thu_mcp.ps1` 9/9, bundle build lại 14:45. CHƯA commit.
  VIỆC KẾ: review lại HẸP bằng `cad-review` (Opus max) chỉ các sửa này (`git diff` của `module/plugin/Rai.cs`, `Spec.cs`, `test/`), rồi chốt mốc Bước 5 (commit); sau đó phần còn lại của Bước 5.
- 14:05 ngày 12/11: agent `cad-lap-trinh` (Opus) XONG Bước 5 mục c, d, e (`tuyen` `cot`; `rai` `le` cùng `s` + `theo_hang`; `rai` theo `tim`). `chay_thu.ps1` 47/47, `thu_mcp.ps1` 9/9, bundle build lại 13:55.
  Vẽ lại tầng 1 ở `ket_qua/cad_mau/cad/lan3/`: 63/64 đầu phun (HL1 9/10, HL2 6/6), lệch vị trí trung bình 546 → 412 mm; chi tiết `cad/LICH_SU.md`. CHƯA commit.
  VIỆC KẾ: review cả hai lượt (a, b, f + CHẶN/NÊN SỬA, rồi c, d, e) bằng `cad-review` (Opus max), rồi phần còn lại của Bước 5 (đo token, trục xả, van).
- 13:50 ngày 12/11: agent `cad-lap-trinh` (Opus) XONG lượt sửa lỗi: CHẶN + 12 NÊN SỬA của `ket_qua/cad_review/review_2026-11-11.md`, Bước 5 mục a, b, f, các GHI CHÚ loại tài liệu cũ
  (chi tiết + GHI CHÚ còn để nguyên ở `cad/LICH_SU.md`). `chay_thu.ps1` 42/42, `thu_mcp.ps1` 9/9, bundle build lại 13:44. CHƯA commit (phiên điều phối commit).
  Chạy lại `t1_spr.json` trên bản chép nền `ket_qua/cad_mau/cad/lan2/t1_spr.dwg`: ống góp P01 hết hở (ảnh `lan2/anh/t1_spr_2.png`), 62 đầu phun, ống như cũ.
  VIỆC KẾ: review mốc này (`cad-review`, Opus max) rồi Bước 5 mục c, d, e.
- 07:35 ngày 12/11: cửa sổ 02:30 đã dùng hết 100 % lúc khoảng 02:50 (phiên chính ngữ cảnh 600 nghìn token + review Fable max), review bị ngắt giữa chừng, CHƯA có báo cáo.
  07:30 hạn mức reset, đã gọi agent review làm tiếp và bảo nó ghi báo cáo ngay (`ket_qua/cad_review/review_2026-11-11.md`). Fable tuần đã 66 % (reset 05:00 ngày 14/11):
  mỗi lượt gọi "nguội" của phiên chính 11/11 tốn khoảng 35 % cửa sổ 5 giờ và 10 % Fable tuần. VIỆC SỬA MÃ (review + mục a–f của Bước 5) NÊN LÀM Ở PHIÊN MỚI, giao Opus theo `cad-lap-trinh.md`.
- 02:35 ngày 12/11: lần chạy hẹn giờ bắt đầu, phiên chính chỉ điều phối. Đang chạy song song 2 subagent, cả hai KHÔNG sửa mã:
  (1) review Fable effort max cho mã tối 11/11 (`git diff 24109da..HEAD -- cad`), báo cáo ở `ket_qua/cad_review/review_2026-11-11.md`;
  (2) Opus effort high làm Bước 5: XONG 02:47 (62 đầu phun, mẫu 64; xem mục "Bước 5"), kết quả ở `ket_qua/cad_mau/cad/skills/` (`t1_vung.json`, `t1_spr.json`, `t1_spr.md`).
  Việc sau đó (chờ review xong mới làm, vì mỗi lúc chỉ một agent sửa mã): giao Opus (theo `cad-lap-trinh.md`) sửa các phát hiện CHẶN / NÊN SỬA của review cùng các mục a–f của Bước 5, rồi `cad\build.bat`,
  chạy lại `t1_spr.json` để thấy ống góp P01 hết hở. Phiên chết giữa chừng thì xem `ket_qua/cad_review/` để biết review tới đâu.
- 22:05: XONG Bước 1b (`cad_anh`), 1c (`rai` + `dt`, `tuyen` `tia`, `PCCC_TUYEN`), 2 (PDF → CAD), 3 (bảng block `PCCC_BLOCK`, `PCCC_THEMBLOCK`) và 4 (agent). VIỆC KẾ: Bước 5.
- ĐÃ THỬ TRÊN AUTOCAD CÓ GIAO DIỆN bằng `cad\test\thu_gui.ps1` (script tự mở AutoCAD thật rồi tự tắt, 7/7 đạt): NETLOAD, cầu nối + máy chủ MCP thật (`cad_tt`, `cad_chay`, `cad_anh`),
  `pdf`, `rai`, `tuyen`, `anh` (`-PLOT` trong AutoCAD thật), bảng `PCCC_BLOCK` hiện 31 dòng + 2 nút. Máy người dùng `SECURELOAD` = 0, nạp plugin bằng NETLOAD tay (bundle không nằm trong ApplicationPlugins).
- XONG Bước 3: `module/plugin/Block.cs` (lệnh `PCCC_THEMBLOCK`, ảnh xem trước 64 px tự vẽ vào `Contents\anh_block`) + `module/ui/BangBlock.cs` → `TKPCCC.UI.dll` (lệnh `PCCC_BLOCK`).
  NETLOAD một file `TKPCCC.Cad.dll` là đủ: nó tự nạp `TKPCCC.UI.dll` khi chạy trong `acad.exe`. `chay_thu.ps1` 33/33, `thu_mcp.ps1` 9/9.
- XONG Bước 4 (22:15): 4 agent trong `cad/.claude/agents/` (`cad-thiet-ke`, `cad-ve`, `cad-kiem`, `cad-lap-trinh`), bảng việc ở `cad/CLAUDE.md`. CHƯA chạy thử agent nào trên việc thật;
  chưa kiểm Claude Code có nạp agent của thư mục con khi mở ở gốc repo không (README cũ ghi chỉ nạp khi mở tại `cad/`).
- Hạn mức: người dùng tự reset khoảng 21:40 (mọi cửa sổ về 0 %) và bảo làm tiếp. 22:20 chốt ở mốc Bước 4: cửa sổ 5 giờ 72 % (reset 02:30 ngày 2026-11-12), tuần Fable 20 %, không đủ chỗ cho Bước 5.
  Đã hẹn `CronCreate` một lần lúc 02:33 ngày 2026-11-12 trong phiên đang mở để tự làm Bước 5 (đóng ứng dụng thì mất hẹn: phiên mới đọc file này rồi làm).
  Phiên 11/11 đã dài 520 nghìn token ngữ cảnh nên mỗi lượt gọi tốn khoảng 1,2 % cửa sổ 5 giờ; phiên mới rẻ hơn nhiều lần.
  22:35 làm thêm bước con 1 của Bước 5 rồi dừng hẳn ở khoảng 85 % cửa sổ 5 giờ.
- MODEL (người dùng chốt khoảng 23:00 ngày 11/11, thay các lời dặn trước trong ngày): viết code bằng Opus (agent `cad-lap-trinh`; effort Claude chọn: high cho thuật toán và thao tác mới,
  medium cho sửa nhỏ); review và kiểm tra bằng Opus effort max (`cad-review`, một lần cuối mỗi mốc; người dùng dặn 2026-11-12 bỏ Fable, thay lời dặn 11/11); chạy dự án thật thì chia agent theo bảng ở `cad/CLAUDE.md`.
  Phiên chính chỉ điều phối. Mã viết tối 11/11 (commit `24109da`..HEAD, do Fable viết) CHƯA qua review: là việc đầu tiên của lần chạy 02:33.
- Người dùng dặn 22:45 cho lần chạy 02:33 ngày 12/11: chạy HẾT usage 5 giờ của cửa sổ đó (không dừng ở 90 %), xong tự ghi file này + `cad/LICH_SU.md` và commit, KHÔNG push.
  Vì chạy tới hết hạn mức: ghi tiến độ + commit sau MỖI bước con.
- PUSH: các commit từ `f3d354f` (Bước 1b, 19:30 ngày 11/11) trở đi CHƯA lên GitHub. Người dùng dặn 22:45: Claude không push nữa, sáng 12/11 họ tự chạy `git push origin main`.
  (Lệnh push thử lúc 19:30 kẹt ở Git Credential Manager; máy không có `gh`.)
- XONG Bước 2: thao tác spec `pdf` (`Spec.cs`, hàm `NhapPdf`, lệnh `-PDFIMPORT`) + `cad/.claude/skills/tkpccc-ve-cad/scripts/pdf_ti_le.py` (tính `tl` từ chuỗi kích thước).
  `chay_thu.ps1` 29/29 (PDF thử 1:200: `tl` 200,0, trục 1–7 đo lại 41000,0), `thu_mcp.ps1` 9/9. PDF mẫu thật `ket_qua/cad_mau/test_HA2.pdf` trang 3: `tl` 285,0, nhập 39 056 đối tượng trong 8 giây,
  trục 1–7 lệch −0,09 % (đạt ± 1 %). Tỉ lệ 4 trang: 260,3 / 285,2 / 285,0 / 249,6.
- XONG Bước 1c: `rai` nhận `dt` (m² tối đa một thiết bị) và trả `m2`; `tuyen` kiểu `tia`; lệnh `PCCC_TUYEN` (chọn đầu phun, chọn ống chính, bấm vị trí ống góp); `PCCC_RAI` hỏi thêm diện tích bảo vệ.
  `chay_thu.ps1` 27/27, `thu_mcp.ps1` 9/9, bundle build lại 20:05. CHƯA thử trên AutoCAD có giao diện: `PCCC_TUYEN` (chọn bằng chuột, bấm điểm) và dấu nhắc mới của `PCCC_RAI`.
- XONG Bước 1b: thao tác spec `anh` (`module/plugin/Spec.cs`, hàm `InAnh`) + công cụ MCP `cad_anh` (`module/mcp/Mcp.cs`). In Model ra PNG bằng `-PLOT` qua `Editor.Command`,
  chạy SAU khi commit gói nên vẽ + chụp trong một lần gọi được. `chay_thu.ps1` 24/24, `thu_mcp.ps1` 9/9; bundle đã build lại 19:45 (tắt mở AutoCAD và Claude Code để nạp).
  Trên AutoCAD có giao diện: đạt (PNG 800 và 640 px có nét).
- XONG: MCP CAD (G0–G2), `cad/` thành dự án con, thử thật với AutoCAD đang mở (`luoi_truc`, `dat`, `ong`, `chu`, `dem`, `doc` đều đúng).
  Lưu ý khi vẽ thật: `cad_chay` vẽ vào bản vẽ đang hiện (đổi tab giữa chừng là vẽ sang bản khác); `doc` `vung` chỉ đọc layer `PCCC_VUNG`; `hop` chỉ lấy đối tượng nằm trọn trong hộp.
- XONG Bước 1 (G3): thao tác `rai`, `tuyen` (3 kiểu `noi_tiep`, `xuong_ca`, `nhanh`), lệnh `PCCC_RAI`. Mã ở `module/plugin/Rai.cs`, ví dụ `skills/vi_du/rai.json`,
  từ vựng trong `skills/TU_VUNG.md`. `chay_thu.ps1` 21/21, `thu_mcp.ps1` 7/7. CHƯA thử trên AutoCAD có giao diện: nhánh bấm điểm trong phòng của `PCCC_RAI`
  (`Editor.TraceBoundary`) chỉ chạy được ở đó; DLL trong bundle chưa build lại (chạy `cad\build.bat`, tắt mở AutoCAD).
- Thư mục `cad/layer/` để chứa chuẩn layer; người dùng sẽ nạp qua lệnh plugin (mục "NẠP DỮ LIỆU NGƯỜI DÙNG QUA PLUGIN").
- **ĐANG LÀM (người dùng giao 2026-11-11, giai đoạn dựng khung làm bằng Fable): vẽ được bản vẽ PCCC như bản mẫu.** Việc kế: Bước 5 dưới đây.
- Chỉ MỘT phiên được sửa mã trong `cad/` tại một thời điểm (14:29 ngày 2026-11-11 hai phiên ghi đè nhau).
- Hẹn giờ: người dùng bảo "xoá hết mọi hẹn giờ, tự động chạy lại tại phiên này" (2026-11-11 15:20), scheduled task `tkpccc-cad-lam-tiep` đã xoá.
  Claude hiểu là: hẹn làm tiếp bằng `CronCreate` một lần ngay trong phiên đang làm, không tạo scheduled task mở phiên mới (chưa hỏi lại người dùng có áp dụng cho mọi lần không).
- Push: shell của Claude không lấy được đăng nhập Git thì chạy `git push origin main` trong tab Terminal của người dùng.

## VIỆC LÀM TIẾP, THEO THỨ TỰ (tự làm, không cần hỏi người dùng)
Mỗi bước xong: `cad\test\chay_thu.ps1` + `cad\test\thu_mcp.ps1` ra `TAT CA DAT`, ghi file này + `cad/LICH_SU.md`, commit, push. Kiểm `get_usage` giữa các bước.

### Bước 1b. XONG 2026-11-11 (khoá `ban_ve` 15:20, `cad_anh` 19:45; chi tiết ở `cad/LICH_SU.md`)
Skill `tkpccc-ve-cad` đã nhắc ghi `ban_ve` khi vẽ nhiều lượt và cách thêm `anh` vào cuối gói.

### Bước 1c. XONG 2026-11-11 20:00 (sửa theo trả lời của người dùng 11/11; chi tiết ở `cad/LICH_SU.md`)
Quy trình vùng (vẽ polyline `PCCC_VUNG` trước, người dùng kiểm, rồi `rai` theo `l`) đã ghi vào skill `tkpccc-ve-cad`; luật chia zone, đi dây, bước rải ở `rule/LUAT.md` (BC-04, BC-05, SP-02).
Còn lại của các ý này: agent `cad-thiet-ke` (Bước 4) và `so_do` hệ địa chỉ (G5) dùng các luật đó. `knowledge/` gốc chưa có bản nguyên văn bảng khoảng cách đầu phun của TCVN 7336.

### Bước 2. PDF → CAD (G4). XONG 2026-11-11 21:50 (chi tiết ở `cad/LICH_SU.md`; cách dùng ở skill `tkpccc-ve-cad`, mục "Nền kiến trúc từ PDF")
Còn lại của G4:
- Chữ font VNI trong PDF nhập vào bị sai dấu (vd "TAÛI TROÏNG"): chưa chuyển mã. `thamdinh/pccc_checker/pdf/` có sẵn `vni_to_unicode`, `is_vni_font` để tham khảo khi làm.
- Nền nhập vào giữ màu của PDF; bản mẫu muốn nền kiến trúc xám nhạt, nét mảnh: làm cùng lúc có file chuẩn layer.
- Nhận phòng tự động chưa có: AI xem ảnh (`anh`) rồi đưa hộp từng phòng lên `PCCC_VUNG`, hoặc người dùng bấm phòng bằng `PCCC_RAI`.
- Tờ PDF chỉ có ảnh quét thì `pdf` báo lỗi: khi gặp mới làm đường `PDFATTACH` (gắn làm nền, không thành nét).

### Bước 3. Bảng block trong AutoCAD. XONG 2026-11-11 22:05 (chi tiết ở `cad/LICH_SU.md`)
Người dùng cần tự xem và báo lại (máy không kiểm được):
- Hình thức bảng `PCCC_BLOCK`: cỡ cột, ảnh 40 px, chữ; bấm đúp một dòng có ra `PCCC_DAT <mã>` và con trỏ về vùng vẽ không; nút "Thêm từ bản vẽ" (gọi `PCCC_THEMBLOCK`, xong thì bảng tự nạp lại).
- `PCCC_THEMBLOCK` với block ĐỘNG thật (accoreconsole chỉ thử được block thường): chèn ra bằng `PCCC_DAT`, đổi trạng thái, rồi `PCCC_BOC` có đếm không.
Giới hạn bản này (ghi `shortcut:` trong mã):
- Chỉ thêm mã MỚI. Đổi hình cho mã đã có: sửa trong `PCCC_Blocks.dwg` hoặc `PCCC_GAN`. Block phải vẽ theo mm như thư viện.
- Ảnh xem trước do plugin tự vẽ từ nét (không hatch, không xoay chữ, block động theo trạng thái đang lưu). Ảnh cũ hơn `PCCC_Blocks.dwg` thì `PCCC_LIB`, `PCCC_BLOCK` tự vẽ lại.
- Mã mới ghi vào `vat_tu.json` cạnh DLL và vào `data/vat_tu.json` của repo (đường dẫn do `build.bat` để ở `Contents\nguon_vat_tu.txt`); bundle chép đi máy khác không có file đó thì chỉ ghi bản cạnh DLL và báo.

### Bước 4. Agent, model, effort. XONG 2026-11-11 22:15 (file ở `cad/.claude/agents/`, bảng việc ở `cad/CLAUDE.md`)
Còn lại: chạy thử cả chuỗi `cad-thiet-ke` → `cad-ve` → `cad-kiem` trên một tầng của bản mẫu (gộp vào Bước 5), rồi sửa lời giao việc theo chỗ agent làm sai.
Người dùng dặn 2026-11-11: lúc dựng khung và viết thuật toán thì dùng Fable; về sau chỉ dùng Opus trở xuống. Bảng lúc lập kế hoạch:
| Agent | Việc | Quyền | Model, effort |
|---|---|---|---|
| `cad-thiet-ke` | Đọc nền kiến trúc, lấy trị số theo `rule/LUAT.md`, chia zone, chọn vị trí ống chính, lập spec cho cả tầng | đọc file, ghi spec vào thư mục dự án; không gọi `cad_chay` | opus, high |
| `cad-ve` | Chạy spec đã có qua `cad_chay`, đọc kết quả, sửa lỗi nhỏ của spec | `cad_tt`, `cad_chay`, đọc file | sonnet, low |
| `cad-kiem` | Soát bản vẽ sau khi vẽ: đếm, vùng hở, đối chiếu luật | `cad_chay` chỉ với `doc`, `dem`; đọc file | sonnet, medium |
| `cad-lap-trinh` | Sửa plugin C#, thêm thao tác spec, chạy bộ test | sửa `cad/module`, `cad/test`; Bash | opus, high |
Skill `tkpccc-ve-cad` giữ sonnet/low (vẽ theo spec có sẵn).

### Bước 5. Vẽ lại một tầng của bản mẫu, đo token (XONG phần mã 2026-11-12 18:20, xem TRẠNG THÁI)
- XONG từ các bước trước: dòng README cho `mau-ban-ve-pccc.md`, `rule/LUAT.md`, ví dụ `rai` + `tuyen` trong skill.
- Vẽ lại tầng 1 sprinkler của bản mẫu (`ket_qua/cad_mau/test_HA2.pdf` trang 3, `tl` 285,0), không cần AutoCAD có giao diện (chạy `PCCC_SPEC` trong accoreconsole như `chay_thu.ps1`):
  1. XONG 22:35: nền đã nhập vào `ket_qua/cad_mau/cad/t1_spr.dwg` (39 056 đối tượng, `tl` 285,0); ảnh mặt bằng 1600 px ở `ket_qua/cad_mau/cad/skills/anh/t1_nen_1.png`
     (vùng [28000, 31000, 72500, 54800], 27,8 mm mỗi px), CHƯA đọc ảnh. Chạy tiếp vào đúng DWG đó bằng
     `cad\.claude\skills\tkpccc-ve-cad\scripts\chay_spec.ps1 -Spec <spec.json> -Dwg ket_qua\cad_mau\cad\t1_spr.dwg` (nhánh mở DWG có sẵn của script chưa thử).
     Điểm đặt chữ nhãn trục ở `tl` 285,0 (lệch tim trục vài trăm mm): trục 1 → 7 tại x = 29291, 36578, 43781, 50767, 57270, 64208, 70253; A → F tại y = 33429, 38327, 40114, 45530, 47314, 52213.
     Dãy phòng dưới A–B (sâu 4900), hành lang B–C, khối giữa C–D (5400), hành lang D–E, dãy phòng trên E–F (4900).
  2–4. XONG 02:47 ngày 12/11 (subagent Opus effort high theo `cad-thiet-ke.md`: 11,6 phút, 235 nghìn token, chạy accoreconsole 2 lần, không phải chạy lại). 15 vùng trên `PCCC_VUNG`
     (11 phòng P01–P11, 4 hành lang HL1–HL4); tim trục và mép tường lấy từ nét vector của PDF (PyMuPDF + ma trận xoay trang), hộp khớp tường ngay lần đầu.
     Rải `s` 4000, `dt` 12 (hai số ghi trên bảng thông số của tờ mẫu) ra 62 đầu phun, mẫu 64: 11 phòng đủ 4 đầu như mẫu; hành lang thiếu 2 (HL1 9/10, HL2 5/6).
     Ống: DN100 56,91 m, DN80 20,54 m, DN50 130,79 m. Spec `t1_vung.json`, `t1_spr.json`, báo cáo `t1_spr.md`, ảnh `anh/t1_spr_1..5.png`, `anh/t1_vung_1..4.png` trong `ket_qua/cad_mau/cad/skills/` (không commit).
     Phiên chính đã xem ảnh tổng `t1_spr_1.png`: hộp vùng, đầu phun, ống nằm đúng trên nền.
  CHỖ PLUGIN VẼ KHÁC KỸ SƯ (toạ độ và đầu vào cụ thể ở mục 3, 4 của `t1_spr.md`); a–f XONG 12/11 (c, d, e lúc 14:05, kết quả vẽ lại ở `cad/LICH_SU.md`):
  a. XONG 12/11 (chạy lại: P01 ống góp x 34894 nối vào DN100). LỖI `tuyen` `xuong_ca`: chân ống góp rơi ra ngoài đoạn ống chính mà `loi` vẫn rỗng (P01: ống góp (31444, 47459)–(31444, 51196), ống chính bắt đầu ở x 32965, hở 1521 mm).
     Sửa: chỉ chọn cột có chân nằm trong đoạn; không cột nào được thì nối thêm dọc ống chính hoặc đưa vào `loi`.
  b. XONG 12/11 (hoà: cột x nhỏ hơn rồi y nhỏ hơn; `qua` nhiều điểm, mỗi điểm cho vùng chứa nó). `xuong_ca` chọn cột khi hoà phụ thuộc chiều vẽ `truc` (trái → phải ra cột trái, ngược lại ra cột phải): cần quy tắc phá hoà cố định, và `qua` theo TỪNG vùng (hiện một `qua` áp cho mọi vùng).
  c. XONG (`tuyen` `cot`). Ống chính chạy sát hoặc qua phòng (P06, hàng đầu phun cách ống chính 330 mm): `xuong_ca` ra một nhánh song song ống chính. Kỹ sư đi mỗi cột một nhánh vuông góc: thêm kiểu `cot`.
  d. XONG (`le` cùng `s`, `theo_hang`). `rai` theo `s` luôn để cách tường nửa bước; kỹ sư dàn bước gần `s`, sát tường hơn, và xếp từng hàng theo bề rộng trống (phòng chữ L có WC). Vì thế theo phương dài lệch 0,2–1,4 m
     (phương ngắn lệch dưới 25 mm). Đề xuất: cho `le` dùng cùng `s`; tuỳ chọn xếp theo từng hàng.
  e. XONG (`rai` `tim`; HL1 còn 9/10 vì mẫu dày hơn `s`). Hành lang: kỹ sư đặt đầu ở các góc giao hành lang, bước 3000–4000 xen nhau; `rai` chia đều nên thiếu 2 đầu. Đề xuất: rải theo đường tim có điểm bắt buộc.
  f. XONG 12/11 (bỏ chữ khi gần đó đã có chữ cùng nội dung, kể cả chữ nền PDF). `nhan` ghi trùng chữ DN50 ở chỗ ống góp gặp nhánh.
  NGƯỜI DÙNG ĐÃ DUYỆT 2026-11-12 (chốt): 15 hộp vùng (`anh/t1_vung_*.png`), nhất là ranh giới tự đặt ở chỗ không có tường (HL1 / HL3 / P06; HL4 phía sảnh thông tầng, x 51056); vị trí ống góp từng phòng.
  Chưa vẽ: trục xả DN50, van OS&Y, công tắc dòng chảy, van xả khí. Chưa thử `cad-ve`, `cad-kiem` (cần AutoCAD mở để gọi MCP).
- Đo số token một lần vẽ (`claude -p --output-format json`), ghi vào `cad/LICH_SU.md`.
- Sau đó theo `KE_HOACH.md`: G5 `so_do`, G6 chat trong CAD (bảng `PCCC_BLOCK` là tab đầu của G6).

### Bước 6. Việc ưu tiên 2 và 3 từ `knowledge/tham-khao-mcp-hub.md` (chi tiết từng việc ở file đó)
- Ưu tiên 2: bảng layer `data/ky_hieu/layer.json` cho `DamBaoLayer` (màu, kiểu đường, độ dày; dây báo cháy ra nét đứt);
  thao tác `dan` (mũi tên + chữ); thao tác `in_pdf`, `luu` (chung đường `-PLOT` với `cad_anh`).
- Ưu tiên 3, sau Bước 2–4: `doc` thêm `gi`:"block"; `cung`, `hatch` khi vẽ lại nền cần tới; `TKPCCC.Mcp.exe` tự `NETLOAD` qua COM + thời gian chờ bằng biến môi trường;
  bộ ký hiệu van của bản mẫu khi làm `so_do`; đoạn cấu hình mẫu cho AI khác và 4–5 câu lệnh mẫu PCCC trong `cad/README.md`.
- Đưa vào skill `tkpccc-ve-cad`: vẽ theo giai đoạn (nền + trục → rải → tuyến → ghi chú), hết mỗi giai đoạn gọi `cad_anh` và báo số đếm; chỉ dừng chờ duyệt khi người dùng yêu cầu.

## NẠP DỮ LIỆU NGƯỜI DÙNG QUA PLUGIN (người dùng dặn 2026-11-12, thay mục "nhắc đầu phiên" của 11/11)
Chuẩn layer, block động thiết bị, catalog ống + phụ kiện, catalog bơm: người dùng KHÔNG gửi file cho Claude; sau này plugin có chức năng để họ tự thêm vào trong CAD.
Không cần nhắc đầu phiên nữa. Việc của Claude: làm các lệnh nạp đó (block đã có `PCCC_THEMBLOCK`, cần kiểm với block động thật; còn thiếu lệnh nạp chuẩn layer, catalog ống + phụ kiện, catalog bơm),
xếp sau Bước 5 hoặc gộp vào G6 (bảng trong CAD). Trong lúc chưa có, plugin vẫn dùng layer và block mặc định.

## NGƯỜI DÙNG ĐÃ TRẢ LỜI 2026-11-11 (đầy đủ: `cad/knowledge/tu-duy-di-day-di-ong.md`)
1. Dây báo cháy hệ thường: nét thẳng, rắn bò, phòng nối tiếp phòng trong cùng zone, không rẽ chữ T, cuối zone có điện trở cuối dây. Có đèn báo phòng thì mỗi đầu báo trong phòng một nét kéo ra đèn.
2. Chia zone: theo giới hạn m² của TCVN VÀ không quá 25 đầu báo một zone (hệ thường), chạm giới hạn nào trước thì dừng. Zone là khu vực: không lấn sang khu vực khác, không để một khu vực thành hai zone lẻ tẻ. Hệ địa chỉ: ZM, CM, RM và mạch vòng, xem knowledge.
3. Ống góp sprinkler: người dùng tự bấm chọn vị trí trong CAD (lệnh của plugin).
4. Bước rải: theo quy chuẩn, tính từ diện tích bảo vệ một đầu phun (TCVN 7336: 12 m² hoặc 9 m²). Trước khi rải phải vẽ polyline phân khu vực theo tường, dầm sàn, vì kèo.
5. Nhận phòng: AI đưa hộp từng phòng, người dùng kiểm lại rồi bấm vào phòng.
6. Chuẩn layer, block động, catalog: để sau, làm sườn trước, phải nhắc (mục trên).

## CHƯA KIỂM
- Skill trong `cad/.claude/skills/` có hiện khi mở Claude Code ở gốc repo không (Claude Code nạp skill của thư mục con khi làm việc với file trong thư mục đó). Không hiện thì vẫn vẽ được nhờ hướng dẫn máy chủ MCP gửi lúc kết nối; cần thì đặt bản trỏ ở `.claude/skills/` gốc.
- `cad/.mcp.json` (mở Claude Code ngay tại `cad/`) chưa thử.
- `cad/rule/LUAT.md` lập theo tên file, chưa mở từng file căn cứ để đối chiếu phạm vi.

## VIỆC ĐỂ LẠI (thứ tự theo `KE_HOACH.md`)
- (G3, G4 đang làm, xem trên) · G5 `so_do` · G6 bảng trong CAD + chat + `cad_anh` · G7 Hub · G8 YouTube · G9 bản quyền (trước đó chuyển repo sang riêng tư).
- Dữ liệu chờ người dùng cấp (xem `cad/data/README.md`): block động co/tê, catalog ống + phụ kiện, catalog bơm, bản vẽ mẫu + khung tên.

## GHI NHỚ KỸ THUẬT
- `rai`: trường `goc` là góc xoay LƯỚI (phòng nghiêng), không phải góc xoay block; `kt` chỉ vẽ khi `goc` = 0. `le` mặc định bước / 4 (với `buoc`); với `s` thì `le` là cách tường tối thiểu, bước dàn tới `s`. Trần 5000 thiết bị một vùng.
- `theo_hang` (12/11): mỗi hàng chia như lưới chung (nMin = số cột lưới × bề rộng hàng / bề rộng vùng, cùng ba lần thử bỏ `chan`, bỏ `le`, thêm thiết bị khi ô hàng vượt `dt`): hàng rộng bằng vùng ra đúng lưới chung.
  `m2` = ô lớn nhất của các hàng; với `buoc` ô đó vượt `dt` thì báo lỗi như lưới chung. `nx`, `bx`, `tuong` vẫn là của lưới chung (GHI CHÚ review 12/11 chưa sửa).
- `tuyen` `xuong_ca`/`cot`: chân ống góp/nhánh "ngoài" khi cách CẢ polyline `truc` (hàm `ChanTruc`), đoạn nối vẽ tới điểm gần nhất của polyline; `cot` gom các chân cùng điểm nối cùng phía, chỉ vẽ đoạn nối từ chân xa nhất.
- `pdf`: `SoTrangPdf` = 0 (bộ đọc underlay không mở được: PDF có mật khẩu kể cả chỉ khoá sửa, file hỏng) thì báo lỗi, không gọi `-PDFIMPORT` (lệnh sẽ hỏi lại tên file mãi). `tao_pdf_mau.py <thư mục>` tạo 3 file thử.
- `theo_hang` làm cột các hàng lệch nhau: `xuong_ca` vẫn một ống góp nhưng nhánh hàng dài hơn (tầng 1: DN50 +15 m). `tim` không dùng đa giác vùng: `qua` của `tuyen` sau nhận mọi điểm.
- Xuất toạ độ block để so với mẫu: accoreconsole `/i <bản chép dwg> /s` một dòng LISP `ssget "X"` `((0 . "INSERT") (2 . "SPR_DAU_DOWN"))` ghi ra file; đầu phun mẫu = vòng tròn đường kính 160–200 mm trong PDF (PyMuPDF `get_drawings`).
- `rai` + `dt`: lấy số cột × số hàng ÍT thiết bị nhất mà ô lưới không vượt `dt`, hoà thì lấy ô vuông hơn (16 × 16 m, `s` 4 m, `dt` 12 ra 4 × 6 = 24 đầu, không phải 5 × 5).
  `m2` = Sx × Sy, mỗi phương S = số lớn hơn giữa bước và 2 lần cách tường; làm tròn bước theo `chan` mà đẩy `m2` vượt `dt` thì bỏ làm tròn.
- Kịch bản accoreconsole: `GetSelection` có `SingleOnly` kết thúc ngay sau `L` (không thêm dòng trống); chọn bằng `W x1,y1 x2,y2` được sau khi `_.ZOOM _E`; `GetEntity` thì không trả lời được.
- `tuyen` `noi_tiep` nối MỌI vùng của `rai` trước đó thành một đường; muốn mỗi phòng một đường thì gọi `rai` + `tuyen` từng phòng.
- Thêm thao tác spec: hàm `Op...` trong `module/plugin/Spec.cs` + 1 dòng `MotOp` + 1 dòng `skills/TU_VUNG.md` + 1 phép kiểm trong `test/chay_thu.ps1`.
- Thao tác phải chạy LỆNH AutoCAD (`anh` dùng `-PLOT`, `pdf` dùng `-PDFIMPORT`): không đặt trong `MotOp`. `ThucHienSpec` gom chúng rồi gọi `Editor.Command` ngoài transaction
  (`anh` sau commit). `Editor.Command` chạy được trong accoreconsole. Toạ độ gõ cho lệnh thêm dấu `*` đằng trước để tính theo WCS.
- Giao diện: chỉ `module/ui/` được tham chiếu `acmgd.dll` (+ `AdWindows`, `PresentationCore`, `WindowsBase`, `System.Xaml` vì `PaletteSet` kéo theo WPF). DLL này không xử lý bản vẽ:
  gọi `Lenh.DanhMucBlock` của lõi và gửi lệnh bằng `SendStringToExecute`. Trong AutoCAD, điều khiển WinForms chỉ hiện là "Pane" với UI Automation:
  `thu_gui.ps1` tìm khung theo tên rồi đếm dòng bằng thông điệp `LVM_GETITEMCOUNT`. `thu_gui.ps1` chép DLL ra thư mục tạm rồi mới NETLOAD để không đụng thư viện block thật.
- Thứ tự dấu nhắc `-PDFIMPORT` (AutoCAD 2021): F, tên file, số trang, điểm chèn, tỉ lệ, góc xoay. Ở tỉ lệ 1 thì 1 đơn vị bản vẽ = 1 inch giấy, KHÔNG theo `INSUNITS`
  (plugin nhân 25,4 × `tl`). Trang có `/Rotate` vào đúng chiều đang hiển thị. Layer mặc định `PDF_Geometry`, `PDF_Text`, `PDF_Solid Fills`. Lệnh không trả danh sách đối tượng:
  đếm đối tượng mới theo handle ≥ `Handseed` trước lệnh.
- Kịch bản accoreconsole: điểm GÕ cho `GetPoint` vẫn bị OSNAP bắt vào đối tượng gần (bấm 65000,2000 ra x 64200 của đầu phun): đặt `OSMODE` 0 trước lệnh cần điểm chính xác.
- Số trang PDF đọc được bằng `UnderlayHost.PdfHost.GetFile(f, "").Items.Count` (có trong `acdbmgd`, chạy được trong accoreconsole). Lần nhập PDF thứ hai vào layer `PDF2_*`.
- `chay_spec.ps1` làm trên bản chép spec + DWG trong thư mục tạm rồi chép về: kết quả `<spec>.kq.json` và DWG chỉ đổi khi chạy xong.
- `pdf_ti_le.py`: `get_text("words")` của PyMuPDF trả toạ độ trang CHƯA xoay (trang `/Rotate 90` thì chuỗi ngang trên màn hình là chuỗi đứng trong toạ độ này); script xét cả hai phương nên không sao.
  Số kích thước ghi trên bản vẽ có thể lệch nét vẽ (bản mẫu: nhịp ghi 1700, vẽ 1785; tổng ghi 18800 nhưng các nhịp cộng lại 18600): vì vậy lọc hai vòng 1 % rồi 0,5 %.
- Phần đầu `skills/TU_VUNG.md` đang dài 1893 / 1900 ký tự (12/11): thêm thao tác nữa phải rút gọn các dòng cũ.
- Thứ tự dấu nhắc `-PLOT` ra PNG (AutoCAD 2021): Y, Model, `PublishToWeb PNG.pc3`, khổ giấy, L, N, W + 2 góc (hoặc E), F, C, Y, `.`, Y, Enter, tên file, N, Y.
  File đã có thì lệnh hỏi ghi đè (lệch dấu nhắc): luôn đặt tên mới. Thiết bị này chỉ có khổ định sẵn (640–1600 px), không nhận cỡ tuỳ ý.
- Phần đầu `skills/TU_VUNG.md` (trước dòng `---`) gửi cho AI lúc kết nối: giữ dưới 1900 ký tự (`thu_mcp.ps1` có kiểm).
- Lệnh Bash của Claude làm mất một dấu gạch chéo ngược khi viết hai dấu liền nhau trong heredoc: script Python có đường dẫn Windows thì dùng `chr(92)`, hoặc ghi script ra file rồi chạy.
- `build.bat` đổi tên DLL/exe đang bị khoá thành `*.cu*` rồi build bản mới. Build hỏng giữa chừng thì bundle tạm không có DLL: sửa lỗi rồi chạy lại `build.bat`.
- PDF người dùng gửi là dữ liệu chưa tin: script đọc PDF chạy bằng `python -I`, script đặt ở thư mục khác với PDF.
- Lệnh Bash dài quá khoảng 8000 ký tự bị cắt (báo thiếu dấu nháy): nội dung dài thì ghi ra file bằng công cụ Write rồi mới chạy lệnh ngắn.
