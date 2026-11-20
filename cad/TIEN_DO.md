# Tiến độ CAD (riêng cho thư mục cad/)

Phiên mới làm việc CAD: đọc file này trước, rồi `cad/README.md` (cây thư mục, sửa gì vào đâu). Kế hoạch tổng G0–G9: `cad/KE_HOACH.md`. Nhật ký: `cad/LICH_SU.md`. Luật riêng: `cad/CLAUDE.md`.
Luật: dùng tới 90 % hạn mức thì dừng, ghi file này + `cad/LICH_SU.md`, commit, push; usage hồi lại thì tự làm tiếp, không hỏi (`cad/CLAUDE.md`).

## VIỆC 20/11 (3) — GOM TRI THỨC CAD THÀNH MỘT ĐẦU NGUỒN `cad/skills/` (NHẬN DẠNG / VẼ / MẪU): XONG 21/11 (4 lô commit, chưa push)
KẾT QUẢ 21/11: `cad/knowledge/`, `cad/layer/` đã dời hết vào `cad/skills/` (mục lục `cad/skills/README.md`; `nhan_dang/` 7 file luật viết mới từ memory + hành vi mã;
`ve/` quy ước + layer, trục tường cửa, `bao_chay/` rải đầu báo, chuông nút đèn, dây zone; `mau/` 8 file bằng chứng). Mọi đường dẫn cũ đã sửa (build.bat, test,
kiem_lien_ket.py, chú thích .cs, ky_hieu.json, agent, SKILL, README, CLAUDE.md, STRUCTURE.md). Skill `tkpccc-ve-cad` chỉ còn quy trình + trỏ tới nguồn.
14 memory luật CAD còn một dòng trỏ tới file mới (xoá memory bị chặn); memory mới `luat-cad-o-cad-skills`. Luật mới từ nay ghi vào `cad/skills/`, không tạo memory.
Chưa có mã: vùng kẹp mặt tường bỏ cột lồi, bỏ nội thất lớn (ghi ở `cad/skills/nhan_dang/tuong_cot.md`). Vị trí điện trở cuối (xa tủ hay cạnh đầu ra) còn hai nguồn mâu thuẫn:
`cad/skills/ve/bao_chay/day_zone.md`, chốt khi làm VIỆC 20/11 (2) mục 4.
(Kế hoạch gốc 20/11, để tham khảo:)
Người dùng 20/11 tối: "đưa các data skill hay knowledge trong folder cad thành 1 đầu nguồn: nhận diện trục, vẽ trục, nhận diện tường cột, vẽ cột, vẽ đầu báo...
quy cách, kiến thức nhận diện và vẽ lại ... sau này phát triển MCP CAD vẽ PDF thành bản vẽ AutoCAD thì gọi chung skill. Gom folder hợp lí (folder skill nhận dạng,
folder skill vẽ cad), phân tầng chi tiết từng folder để sau vào thẳng đó sửa cho đỡ tốn token." Làm sau VIỆC 20/11 (2)? KHÔNG: làm việc này TRƯỚC (người dùng giao mới nhất),
rồi quay lại khung Karaoke.
Kiểm kê 20/11 (dòng): `cad/knowledge/` 11 file ≈ 1 100 dòng (mau-thuyet-minh-pccc 447, hoc-ban-ve-mau-dwg 239, mau-ban-ve-pccc 81, so-do-nguyen-ly 80,
tu-duy-di-day-di-ong 53, di-day-bao-chay 39, hoc-video 35, tham-khao-mcp-hub 33, mau-rai-khoang-dam 27, mau-so-do-that 23, quy-uoc-ban-ve 16) + `knowledge/hoc/` 2 file;
`cad/skills/TU_VUNG.md` 82 + `vi_du/`; `cad/rule/LUAT.md` 26; `cad/layer/` 2; skill Claude `tkpccc-ve-cad` 64, `tkpccc-cad-console` 69; 5 agent; cộng ~25 file memory
của Claude (luật dây zone, khoang dầm, vùng, xoá vùng...) CHƯA có trong repo -> chép vào nguồn mới (memory chỉ còn 1 dòng trỏ tới).
Cây đề xuất (`cad/skills/` = đầu nguồn duy nhất, mọi AI, skill Claude, agent, Bot chat, MCP trỏ vào; mỗi thư mục có README.md mục lục 1 dòng / file):
  cad/skills/README.md            mục lục "việc gì -> mở file nào"
  cad/skills/TU_VUNG.md           giữ chỗ (build.bat, Mcp.cs, test trỏ tới)
  cad/skills/nhan_dang/           ĐỌC bản vẽ (DWG hoặc PDF) ra dữ liệu: truc.md · tuong_cot.md (2 nét, hatch, cột chữ I, cột lồi -> kẹp mặt tường) ·
                                  cua_thang.md (cửa, thang bộ, thang máy, ô X, WC) · phong_vung.md (dựng phòng, khu không rải, hiên, mái che, nội thất) ·
                                  dam_khoang.md (dầm, khoang, mái kèo) · ky_hieu.md (block thiết bị có sẵn, tên lạ) · pdf.md (tỉ lệ, nhập, kiểm tỉ lệ)
  cad/skills/ve/                  VẼ: quy_uoc.md (layer, cỡ chữ, màu; gộp quy-uoc-ban-ve + layer/) · truc.md · tuong_cot_cua.md ·
                                  bao_chay/ (rai_dau_bao.md, khoang_dam.md, day_zone.md, chuong_nut_den.md, so_do.md) · chua_chay/ (sprinkler, ống) · thuyet_minh.md
  cad/skills/mau/                 BẰNG CHỨNG học từ bản mẫu (hoc-*, mau-*): chỉ ghi quan sát + số liệu, luật rút ra thì chuyển lên nhan_dang/ hoặc ve/
  cad/skills/vi_du/               giữ
  cad/rule/LUAT.md, cad/data/     giữ (luật nối quy chuẩn; dữ liệu máy đọc). cad/knowledge/ -> xoá sau khi chuyển hết, sửa mọi chỗ trỏ tới.
Chỗ phải sửa khi dời file (git grep 20/11): build.bat (mau-thuyet-minh-pccc.md, TU_VUNG.md), test chay_thu.ps1, scripts/kiem_lien_ket.py, .gitignore,
chú thích trong cad/module/plugin/*.cs (DayBc, BangMb, Lenh, Rai, TienIch, SoDo, Spec, KienTruc), Mcp.cs, data/ky_hieu/ky_hieu.json, agent cad-*, 2 SKILL.md,
cad/README.md, cad/CLAUDE.md, STRUCTURE.md, KE_HOACH_TONG.md. Kiểm xong: `python scripts/kiem_lien_ket.py`, chay_thu + thu_mcp TAT CA DAT.
BƯỚC TIẾP: tạo cây + README; tách nội dung từng file cũ vào đúng file mới (không bỏ sót dòng, chép nguyên văn phần quy chuẩn); chép luật từ memory;
sửa đường dẫn; kiểm; commit (không push). Làm theo lô nhỏ, mỗi lô một commit, để dừng kịp khi hết hạn mức.

## VIỆC 20/11 (2) — HỌC RẢI BÁO CHÁY THEO BẢN MẪU (file_cad_test/TESTBCHAY_20_11.dwg): KHUNG 5 KARAOKE ĐANG ĐỐI CHIẾU; 1 LỖI ĐÃ SỬA + TEST; LÀM TIẾP SAU VIỆC (3)
Người dùng (20/11, chép ý): mỗi khung rectang = 1 công trình mẫu vẽ báo cháy đã thẩm định, là CHUẨN, phải tư duy như nó.
1. Copy các block mặt bằng mẫu ra chỗ trống (KHÔNG sửa bản gốc), explode, rồi vẽ vùng rải báo cháy.
2. Vùng kẹp sát mặt tường: tường 200 có cột 300 lồi ra 100 thì vùng chạy thẳng theo mặt tường 200, không bao phần cột lồi.
   Nội thất trong phòng: không bao (hoặc bao rồi nhận ra nội thất thì xoá), chỉ bao tường phòng.
3. Rải thử đầu báo, chuông, nút nhấn; đối chiếu bản mẫu (bản gốc có sẵn thiết bị). Không khớp: ghi lý do, rút logic, sửa logic vùng / rải cho đúng.
4. Xong mục 3 mới sang dây báo cháy: vẽ thử, đối chiếu mẫu, học logic. Điện trở cuối đặt xa tủ TTBC (tiết kiệm dây).
Luật riêng việc này: 5 giờ tới 95 % thì ghi file này + `cad/LICH_SU.md`, commit, KHÔNG push, tự làm tiếp sau reset không hỏi.
Ngữ cảnh phiên > 40–45 % thì làm xong task đang dở rồi chuyển sang phiên nhánh ngữ cảnh sạch. Có gì không hiểu thì hỏi người dùng.
ĐÃ LÀM (20/11 tối): bản đồ 8 khung, cách chép / explode / chạy PCCC_RAI qua COM, kết quả khung 5: `cad/skills/mau/hoc-bao-chay-ban-mau.md` (ĐỌC TRƯỚC).
- Lỗi 1 đã sửa: layer tên tường chỉ chứa block cửa -> "Enter mọi phòng" ra 0 vùng (`NhanVung.cs` MatChon lùi về mọi nét); test `chay_thu.ps1` ca 15.
  Bundle chưa build lại (AutoCAD đang mở bản chép; `build.bat` đổi tên DLL cũ được, AutoCAD mở lại mới nạp).
- Khung 5 sau sửa: 30 vùng, 50 đầu báo nhiệt; mẫu 62. 21 / 30 vùng khớp số; mẫu dày hơn ở phòng hẹp (bước ≈ 4–4,5 m, cách tường ≤ ≈ 2,5 m).
NGƯỜI DÙNG CHỐT 20/11 tối: giữ TCVN 7568-14:2025; bản mẫu chỉ là chuẩn tham khảo cách rải + cách chia vùng (không rải dày theo 5738).
BƯỚC TIẾP: (1) build bundle (tắt AutoCAD, `build.bat`), mở lại `ket_qua/HOC_BAO_CHAY/cad/hoc_bao_chay.dwg` (bản chép đã có mặt bằng khung 5 explode ở y −50 m);
(2) khung 5: hành lang bị nét mảnh layer `xref` cắt 5 vùng (mẫu coi là 1), lối 1,6 m phía nam không dựng vùng; đối chiếu chuông (mẫu: mỗi phòng hát 1 chuông),
nút ấn (3), đèn báo phòng; (3) lần lượt khung 3, 4, 6, 7, 8 (khung 2 đã học); kiểm luật kẹp tường bỏ cột lồi + nội thất trên khung có cột (trường học, nhà ở XH);
(4) mục 4 dây + điện trở cuối. Hạn mức TUẦN đã 92 % lúc 19:25 20/11 (reset 05:00 21/11): làm tiếp sau mốc đó.

## VIỆC 20/11 — DẠY PLUGIN PHÂN VÙNG NHƯ AI (xưởng mái kèo): MÃ L1, L2, L5, L5b, L6, L7, L8: BUILD + TEST ĐẠT + COMMIT (chưa push); BẢN CHÉP 25 VÙNG = ĐÁP ÁN, 57 / 55 ĐB
LƯỢT 9 (20/11 tối, phiên nhánh tự chạy sau reset): L7. `NhanVung.cs` `GomKhu`: block tên THANG BỘ / STAIR, hoặc `HinhBac` (≥ 6 đoạn song song dài bằng nhau 0,6–2,5 m,
cách đều 0,2–0,4 m; hộp 0,8–8 m, ≤ 50 m²; nhớ theo định nghĩa block + tỉ lệ + góc) → hộp bao vào `NhanThem.ThangBo`. Thang Quyết Thắng là block tên vô nghĩa `fsdff`
(bậc 250 mm, layer "3H"). `Rai.cs` sau `LamGon`, chỉ hệ BC: hộp chứa tâm trong vùng, cạnh cách cạnh vùng ≤ 0,3 m nắn ra sát tường (`NanHop`), trừ bằng Region (`KhoetHop`,
trừ xong không còn đúng 1 vòng thì không khoét) (`khoet_thang=`); hộp ≥ 50 % phòng = buồng thang, bỏ cả phòng (`bo_buong_thang=`). Ca 14 thêm block thang 9 bậc trong phòng
làm việc (kiểm `khoet_thang=1 m2=2.2`, vùng 45,8 m²). build + `chay_thu` + `thu_mcp` TAT CA DAT (bản vẽ thật dựng phòng 9,5 s, như trước). Bản chép (`kb6.scr` →
`qt_maikeo_6.dwg`, ảnh `anh/so_dap_an_1.png`): `khoet_thang=1 m2=7.3`, phòng giữa nhà văn phòng 2 còn 45,8 m², 8 đỉnh vuông góc trùng đáp án AI; vẫn 25 vùng, 57 ĐB.
BƯỚC TIẾP: `cad-review` (Opus max) diff `bc88a99^..HEAD`, sửa mục CHẶN; người dùng push từ tab Terminal. Sau đó VIỆC 20/11 (2).
LƯỢT 10 (20/11 tối, cùng phiên nhánh): `cad-review` (Opus max, agent general-purpose đọc `cad/.claude/agents/cad-review.md` vì agent CAD không nạp từ gốc repo) xong:
chay_thu 174 phép TAT CA DAT, thu_mcp 11 TAT CA DAT; **4 CHẶN, 5 NÊN SỬA, 7 GHI CHÚ** (báo cáo đủ, có hình học từng ca thử: `ket_qua/QUYET_THANG/cad/thu_maikeo/review_L1_L8.md`,
không commit). DỪNG vì hạn mức TUẦN 95 % (reset 21/11 05:00 giờ máy), chưa sửa CHẶN nào. CHẶN (sửa theo thứ tự, mỗi mục thêm ca thử vào `chay_thu.ps1`):
1. L1 `NhanVung.cs` CacMat (khoảng dòng 910, 922, 984; `TachCot` 530–549): khe có một đầu là nét cột không còn khép → phòng hở, mất hẳn, MỌI loại trần.
   E1: xưởng 18 x 24 m, cột 400 x 400 hai tường dọc bước 6 m (mặt ngoài cột = mặt ngoài tường), tường 2 nét 200 giữa cột, khoang cửa cuốn 6 m tường nam để trống,
   CoTran: trước mốc 424,8 m² / 6 ĐB, nay `vung=0` (có block CUA_CUON trong khe thì vẫn đúng). E8: tường nam 5 trụ polyline kín 1000 x 220, cửa sổ 1,5 m trống,
   3 tường còn lại polyline hở 2 nét: trước 56,53 m², nay 0. Sửa: chỉ chặn khép khe nét cột khi khe KHÔNG thẳng hàng nét tường thường nào (ô giả giữa lòng xưởng);
   khe trên đường tường (tường–cột–khe–cột–tường) khép như cũ; ứng viên cột thêm tỉ lệ cạnh (trụ 1000 x 220 không phải cột). Phải giữ E2 CoTran đúng 40 / 42 m².
2. L6 `Rai.cs` (khoảng 1860–1869): không xét "mép ngoài không có tường" → phòng / hành lang hẹp có cột trong tường ngoài bị bỏ. E2: văn phòng 20 x 8 m tường 1 nét,
   hành lang 2 m dọc tường nam, 3 phòng 7 x 6, 7 x 6, 6 x 6 m phía bắc, cột 300 x 300 trên tường nam và bắc bước 5 m; MaiKeo h 3,5: `bo_hien=2 m2=35.9,39.8` (CoTran 4 phòng 5 ĐB).
   Sửa: chỉ bỏ khi mép ngoài giữa các cột không có nét song song cách 0,1–0,4 m (tường) hoặc có chữ HIÊN; còn lại chỉ báo `hien_nghi=`. E2 làm ca âm.
3. L5 theo khoang `Rai.cs` (khoảng 1967–1974, cùng gốc 1844): bỏ cả khoang chạy hết bề rộng xưởng vì có chữ mái che. E3: xưởng 24 x 48 m, cột x = 0, 20, 24 m bước 6 m,
   dải mái che x 20–24 m không tường ngăn, chữ "MAI CHE" tại (20,8; 21) m, MaiKeo h 9: `bo_khoang_khu_ten=1 m2=144` (120 m² là xưởng, mất 3 ĐB). Sửa: chỉ bỏ khoang chứa chữ
   khi là khoang đầu / cuối theo phương chia và hẹp hơn khoang trung vị, còn lại giữ + báo; ngưỡng bỏ cả phòng 500 m² (dòng 1844) thêm điều kiện hình dải hoặc hạ.
4. `Rai.cs` (khoảng 1522) nhánh MaiKeo đặt `khoangDam = true` → phòng < 9 m² sát nhau gom như khoang dầm nhỏ, rải chung 1 lưới, ĐB nằm trên tường chung, dấu X sai.
   E5: hai phòng 2,8 x 3 m chung tường 1 nét, MaiKeo h 9: 1 ĐB tại x 2,8 m (CoTran 2 ĐB). Sửa: bỏ `khoangDam = true` ở nhánh MaiKeo (hoặc chỉ gom khoang của `khoangChia`).
NÊN SỬA (để sau CHẶN): L8 `TenPhong` giữ phòng có chữ PHÒNG trong hình chi tiết (lỗi 16/11 quay lại); `DuongViKeo` mượn hàng cột xưởng bên cắt phòng họp 15 x 15 m;
L5 bỏ phòng bơm nằm trong nhà (luật chỉ nói phòng bơm đứng riêng); L5b bỏ kho có chữ "KHO" và lan sang kho không sát mái che; L1 `khongNoi` không có phép kiểm nào.
Chưa kiểm: `HinhBac` nhận nhầm block khác (chỉ suy từ mã). Commit 7cbb70e (phiên khác, MatChon dựng lại bằng mọi nét liền) không thuộc mốc review.
BƯỚC TIẾP (tự làm sau 21/11 05:00, không hỏi): `get_usage` (tuần < 95 %, 5 giờ < 95 %) → sửa CHẶN 4 → 2 → 3 → 1 (nhẹ tới nặng), mỗi mục build + `chay_thu` + `thu_mcp`
TAT CA DAT; chạy lại bản chép (`kb6.scr` chép thành `kb7.scr`, DWG ra `qt_maikeo_7.dwg`) phải còn 25 vùng / 57 ĐB; ghi LƯỢT 11, commit, KHÔNG push.
LƯỢT 5 (người dùng "Làm L6"): `Rai.cs` khối Enter mọi phòng, sau L5: hiên = dải hẹp (rộng TB 2A/P ≤ 3 m, ≤ 200 m²) có ≥ 2 cột (`nhanThem.Cot`) nằm trong / cách viền
≤ 0,6 m mà cột đó không sát phòng khác còn lại → bỏ (`bo_hien=`). CHỈ chạy với `LoaiTran MaiKeo`: chạy mọi chế độ thì `test báo cháy.dwg` (CoTran, 506 cột) còn đúng 1 phòng
hẹp 12,7 m² sát cột bị bỏ nốt, ca kiểm "rải được" hỏng (phòng kề đã bị bỏ ngoài trục nên không loại được cột). build + `chay_thu` + `thu_mcp` TAT CA DAT.
Bản chép (`kb4.scr` → `qt_maikeo_4.dwg`, ảnh `anh/so_dap_an_1.png`): `bo_hien=3 m2=35.3,34.3,23.2`, **28 vùng/khoang, 62 ĐB** (đáp án 25 / 55); nhà văn phòng 2 còn đúng 4 phòng / 4 ĐB.
CÒN: vùng 63,55 m² mái che tây nam cạnh trạm điện (3 ĐB, không có chữ khu bên trong, không có cột); L7 thang bộ; test tổng hợp mái kèo + hiên trong `chay_thu.ps1`; hiên không vẽ cột chưa nhận.
LƯỢT 6 (người dùng "Viết test"): `chay_thu.ps1` ca 14 (kịch bản kb6): xưởng 20 x 48 m cột 400 x 400 hai tường dọc bước 6 m, dải mái che 3,4 m có chữ không tường ngăn,
phần lồi đầu hồi 8 x 2 m, nhà văn phòng cách xưởng 4 m có hiên 2,4 m + 3 cột mép ngoài; kiểm `mai_keo cot=21 phong_chia=1 khoang=8`, `bo_khoang_khu_ten=1`, `bo_hien=1`.
Ca 13 nay cắt tới trước lời nhắc LoaiTran của ca 14. TAT CA DAT.
LƯỢT 7 (người dùng "Làm dải xiên"): soát lại, KHÔNG phải lỗi, không sửa mã. Dải xiên sát tường phải xưởng 1 đã là vùng riêng 37,41 m² (đáp án 35,2 m²).
Khoang 2 xưởng 1 (giữa tường đầu hồi và hàng cột thứ hai, kéo tới tường xiên vì chỗ đó không có vách, như đáp án) rộng ≈ 30,8 m (lưới 4 x 7,7 m) > 3 x 10,2 = 30,6 m:
3 đầu báo thì đầu ngoài cùng cách tường > 5,1 m, nên 4 là đúng TCVN 7568-14; khoang đáp án cùng chỗ rộng 30,9 m cũng vượt. Chênh 62 / 55 còn lại: khu nam xưởng 1
(6 ĐB, khoang 514 m²/ĐB tính theo lưới 3 x 2), vùng mái che tây nam 63,55 m² + 2 phòng nhỏ cạnh trạm điện (5 ĐB) — cần người dùng xác nhận khu đó có rải hay không.
LƯỢT 8 (người dùng trả lời "Không rải" khu mái che tây nam cạnh trạm điện): L5b ở `Rai.cs` khối MaiKeo: phòng ≤ 100 m² không có chữ "PHÒNG", không phải khoang xưởng,
cách khoang đã bỏ vì chữ mái che / không có công năng (hoặc phòng vừa bỏ theo luật này) ≤ 0,3 m thì bỏ theo (`mai_keo bo_ke_mai_che=`). Ca 14 thêm kho 4 x 2,9 m sát dải mái che.
build + `chay_thu` + `thu_mcp` TAT CA DAT. Bản chép (`kb5.scr` → `qt_maikeo_5.dwg`): `bo_ke_mai_che=3 m2=63.5,15.9,4.4`, **25 vùng (= đáp án), 57 ĐB** (đáp án 55;
khoang 2 xưởng 1 cần 4 vì rộng 30,8 m, xem LƯỢT 7; khu nam xưởng 1 6 ĐB chưa so kỹ).
BƯỚC TIẾP (cũ, xem LƯỢT 9): L7 → `cad-review` → push từ tab Terminal người dùng.
LƯỢT 4 (Claude Code, người dùng "Sửa tiếp"): đã sửa lỗi 1–3 của lượt 3. `DuongViKeo`: hàng vì kèo cần ≥ 2 cột nằm trong bề ngang chính phòng (± 0,6 m),
không có hàng nào mới mượn cột gần 6 m; hàng cách biên hộp bao < nửa bước vì kèo (trung vị) bỏ, dải đó gộp vào khoang kề. `Rai.cs` khối MaiKeo: sau chia, khoang ≤ 500 m²
chứa tâm chữ khu (L5) bỏ (`mai_keo bo_khoang_khu_ten=`). build + `chay_thu` + `thu_mcp` TAT CA DAT. Chạy lại bản chép (`kb3.scr` → `qt_maikeo_3.dwg`, ảnh `anh/so_dap_an_1.png`):
**32 vùng/khoang, 68 ĐB** (đáp án 25 / 55). Xưởng 2: 8 khoang / 16 ĐB = đáp án; dải mái che nam 75,9 m² bỏ; xưởng 1 + khu nam + phòng 5: 32 ĐB (đáp án 33), khoang đầu gộp phần lồi.
CÒN: hiên quanh nhà văn phòng 2 (L6, phần lớn số thừa); vùng 63,55 m² mái che tây nam cạnh trạm điện (3 ĐB, không có chữ khu bên trong); khoang 2 xưởng 1 ra 4 ĐB
(bước 7,7 m) vì dải xiên sát tường phải chưa tách thành vùng riêng như đáp án; L7 thang bộ; chưa có test tổng hợp cho luật mái kèo trong `chay_thu.ps1`.
BƯỚC TIẾP: L6 → dải xiên → L7 → test tổng hợp → `cad-review` → push từ tab Terminal người dùng.
LƯỢT 3 (Claude Code, 20/11 chiều): `build.bat` đạt (không lỗi C# 5), `chay_thu.ps1` TAT CA DAT, `thu_mcp.ps1` TAT CA DAT. Bundle đã build lại: AutoCAD đang mở phải tắt hẳn, mở lại mới nạp bản mới.
Thử trên bản chép (`ket_qua/QUYET_THANG/cad/thu_maikeo/qt_maikeo_2.dwg`, không commit; kịch bản `kb2.scr`, log `kb2.out.utf8.txt`, ảnh so đáp án đỏ `anh/so_dap_an_1.png`):
`PCCC_RAI BC_KHOI LoaiTran MaiKeo 9 Khong`, quét C 1305000,-1655000 → 1397000,-1575000, Enter mọi phòng: 68 s, `cot=100 mai_keo phong_chia=2 khoang=21`,
**36 vùng/khoang, 80 ĐB** (đáp án 25 vùng / 55 ĐB). ĐÚNG: cột chữ I không còn thành tường ảo (hết ô giả dọc cột); phòng bơm 11,4 m² + mái che 187,2 m² bỏ (L5);
2 phòng văn phòng góc phải có vùng (L8); ô X, hiện hữu bỏ; khoang 6 m giữa hàng vì kèo đúng ở thân hai xưởng. CÒN SAI (sửa theo thứ tự):
1. Xưởng 2 thừa 3 khoang mỏng đầu bắc (16; 18,5; 25,5 m²): `DuongViKeo` (`NhanVung.cs` khoảng dòng 482–492, `VI_KEO_GAN_M = 6`) mượn cột nhà văn phòng 2 / hiên
   cách hộp bao ≤ 6 m làm hàng vì kèo. Sửa: hàng phải có ≥ 2 cột nằm trong khoảng ngang (dọc) của chính phòng (± 1 m), cột nhà khác không tạo hàng.
2. Xưởng 1 thừa dải mỏng 23 m² (4 ĐB) giữa phần lồi tường đầu hồi phía bắc và hàng cột đầu: dải < nửa bước vì kèo (≈ 3 m) phải gộp vào khoang kề.
3. Xưởng 2 nuốt dải "KHU VỰC MÁI CHE" phía nam (không có tường ngăn, phòng 1029,6 m² chỉ báo `chu_khu_trong_phong_lon`) → khoang cuối 4 ĐB thay vì 2.
4. Hiên quanh nhà văn phòng 2 vẫn thành nhiều vùng (L6 chưa làm); khu mái che tây nam cạnh trạm điện còn vùng 63,55 m² (3 ĐB).
BƯỚC TIẾP: sửa 1 → 2 → 3, L6, L7; thêm test tổng hợp (mục Thử trong file kiến thức); chạy lại `kb2.scr` (đổi tên DWG ra) so ảnh với đáp án; `cad-review`; push từ tab Terminal người dùng.
LƯỢT 2 (phiên Bot chat thứ hai, không có Bash/PowerShell; người dùng: "điều chỉnh tư duy vùng rải đầu báo của plugin giống với AI"): sửa `module/plugin/NhanVung.cs`,
`module/plugin/Rai.cs`, `module/ui/BangBoTri.cs` (mục "Mái kèo"), `test/chay_thu.ps1` (chuỗi dò LoaiTran), `HUONG_DAN_SU_DUNG.md`. Chi tiết + rủi ro:
`skills/mau/phan-vung-xuong-mai-keo.md` mục "Đã viết mã 20/11".
BƯỚC TIẾP (Claude Code có shell, tắt AutoCAD): (1) `build.bat`, sửa lỗi biên dịch C# 5 nếu có; (2) `chay_thu.ps1`, `thu_mcp.ps1` TAT CA DAT; (3) thêm test tổng hợp (mục Thử
trong file kiến thức) vào `chay_thu.ps1`; (4) thử trên bản chép "BEAM QUYẾT THẮNG.dwg" mặt bằng dưới: `PCCC_RAI BC_KHOI LoaiTran MaiKeo 9`, so với đáp án 25 vùng / 55 ĐB;
(5) L6 hiên, L7 thang bộ; (6) `cad-review` một lần cuối; (7) commit + push.
LƯỢT 1:
Người dùng (Bot chat, ảnh): "Vẽ báo cháy ở mặt bằng vùng trên, rồi đối chiếu mặt bằng vùng trên và vùng dưới (phân vùng bằng plugin và rải đầu báo bằng plugin).
Từ đó train cho plugin phân vùng và rải được như AI, train vào plugin dự án. Để không có AI mà tôi vẫn dùng được bằng plugin".
- XONG (phiên Bot chat, không có Bash): mặt bằng trên vẽ 25 vùng theo bộ đã duyệt 19/11 + rải 55 BC_KHOI (vùng còn giữ để người dùng so); plugin (mặt bằng dưới) 35 vùng / 69 ĐB.
  Bảng đối chiếu: `ket_qua/QUYET_THANG/cad/so_sanh/so_sanh_ai_plugin.md`; đáp án vùng `ai_vung_tren.json` (không commit).
  Luật + chỗ sửa + test: `skills/mau/phan-vung-xuong-mai-keo.md` (L1–L8).
- Nguyên nhân chính: cột thép chữ I (layer tên ống `$.2.9DN25`) bị coi là tường 2 nét rồi bắc cầu sang cột kế bên thành tường ảo; chưa có luật khoang vì kèo,
  từ khoá MÁI CHE / PHÒNG BƠM / SÂN, hiên; 2 phòng văn phòng góc ngoài bị bỏ sót.
- BƯỚC TIẾP (cần Claude Code có Bash, AutoCAD tắt khi build): agent `cad-lap-trinh` (Opus high) sửa `module/plugin/NhanVung.cs` theo L1 → L8 (thứ tự: L1 cột, L8 sót phòng,
  L5 từ khoá, L6 hiên, L7 thang, L2 khoang vì kèo + lựa chọn kết cấu MaiKeo ở `PCCC_RAI` / palette Khoang dầm), thêm test tổng hợp vào `test/chay_thu.ps1`,
  thử cục bộ trên bản chép của bản vẽ thật so với đáp án; `cad-review` (Opus max) một lần cuối; sửa `HUONG_DAN_SU_DUNG.md`, `README.md` nếu thêm từ khoá / lựa chọn.

## VIỆC 19/11 — RẢI KHÔNG ĐỂ THIẾT BỊ NGOÀI / SÁT NÉT VÙNG: XONG MÃ + TEST (nhánh `claude/cad-rai-trong-vung-18cb55`, đã gộp vào main 19/11)
Người dùng (2 ảnh): "có 1 số đầu báo khói rải lệch ra ngoài polyline vùng, chắc do rải theo hàng cột đều nhau ... rải chuẩn (dù cho không cùng hàng, cột)".
- Nguyên nhân: (1) lưới chia theo hộp bao, chỉ lọc điểm ngoài đa giác: dải hẹp cạnh xiên (V5 XUONG) giữ cột ở giữa hộp bao, điểm trên cùng cách cạnh xiên 1 mm;
  (2) `PhuKin` lượt 2 bỏ điều kiện cách tường: hành lang nhánh hẹp (V3e HL, rộng 1500) chỉ còn vị trí thử trên nét vùng.
- Sửa ở `LuoiVung` (dùng chung `rai` spec + `PCCC_RAI`): `DoiVao` dời điểm cách cạnh < `tuong_min` vào trong (kẹp theo hàng hoặc cột, lấy bước dời ít hơn,
  hoà thì theo hàng; góc lõm đẩy ra xa đường bao; khe hẹp hơn 2 lần thì đặt giữa khe) TRƯỚC khi phủ kín; `PhuKin` dời vị trí thử sát tường vào chứ không bỏ.
  Mã chưa có `tuong_min` (đầu phun) dùng `CACH_TUONG_MIN_M` = 0,1 m (không phải trị số quy chuẩn). Phòng chữ nhật không đổi. Không đụng luồng khoang dầm (đầu báo trên dầm).
- Test mới `rai_hep` (dải xiên, chữ L nhánh 1500, chữ L theo_hang) + `rai_xien` kiểm thêm cách cạnh ≥ 500; sửa hàm kiểm khoảng cách PowerShell (`[math]::Min(1, x)` chọn overload int).
  chay_thu 171 DAT / 0 LOI (1 BO: bản vẽ thật không có trong worktree), thu_mcp TAT CA DAT. Ảnh: thư mục ghi chú phiên `rai_vung_hep_19_11.png`.
CÒN: chưa thử trên bản vẽ thật của người dùng (AutoCAD đang mở, chưa chạy thu_gui); hành lang hẹp đặt sát mức tối thiểu (0,5 m tới một tường) chứ không canh giữa;
dịch ra ngoài ống gió (`DichRaNgoai`) chưa kiểm cách tường.

## VIỆC 18/11 — BOT CHAT NHƯ VS CODE (ẢNH + VÙNG CHỌN CAD) · BRAVE CÓ TAB, TOÀN MÀN HÌNH TRONG KHUNG: XONG MÃ + TEST, CHỜ NGƯỜI DÙNG THỬ TRONG AUTOCAD
Người dùng: "điều chỉnh bot chat giống như VS code và claude code, có thể paste ảnh vào, quét chọn vùng trên cad và ra lệnh cho bot chat sửa ... YouTube vừa lướt web
bằng brave vừa xem youtube, thêm tab mới trong trình duyệt brave"; giữa chừng: "ấn fullscreen trên youtube bằng brave thì nhảy full màn, chỉ muốn full viền (cửa sổ) đã fix cứng".
- Bot chat (`module/botchat/`): Enter gửi, Shift+Enter xuống dòng; Ctrl+V / menu Dán (`ONhap` bắt WM_PASTE) / kéo thả / "+ Ảnh" đính kèm ảnh (chép vào
  `%LOCALAPPDATA%\TKPCCC\botchat\anh`, dọn > 7 ngày; lời nhắn ghi đường dẫn, `--add-dir` thư mục đó, AI xem bằng Read). Thẻ vùng chọn: đối tượng đang chọn
  (pickfirst, `ImpliedSelectionChanged`) hoặc nút "⬚ Quét vùng CAD" = lệnh mới `TKBOTCHON` (giữ cả khi bỏ chọn). Gửi kèm: tên bản vẽ, số đối tượng, hộp bao,
  thành phần (loại DXF + tên block + layer, 15 nhóm), ≤ 200 handle + cách sửa. Ảnh, vùng đi theo một lời nhắn, gửi xong thì bỏ.
- Spec (`module/plugin/Spec.cs`, `skills/TU_VUNG.md`): lọc `h` [handle] cho `xoa`, `doc`; `doc gi:"dt"` (từng đối tượng: h, loai, l, mau, hop, p/c/r/t/ten);
  thao tác mới `sua` (chọn h | l | hop; tl, xoay quanh tam, doi, l_moi, mau, t), kết quả `sua`. Test: mục "sua (Bot chat)" trong `chay_thu.ps1`.
- Brave (`module/youtube/BangYoutube.cs` `MoBrave`): hồ sơ riêng `%LOCALAPPDATA%\TKPCCC\Brave` (tiến trình riêng nên cờ luôn có hiệu lực), cửa sổ THƯỜNG có tab +
  thanh địa chỉ (bỏ `--app`), link sau mở thành tab mới; lần đầu áp mép phải AutoCAD rộng 480, sau đó Brave nhớ vị trí người dùng chỉnh.
  Toàn màn hình: extension `module/youtube/brave_khung/` (MV3, content script world MAIN thay requestFullscreen / fullscreenElement: phần tử phủ khung trang,
  Esc thoát), build.bat chép cạnh DLL, nạp bằng `--load-extension`. ĐÃ THỬ Brave 1.99 (Chromium 157) headless: YouTube bấm nút toàn màn hình → player phủ đúng 500 x 675 khung trang.
- Sửa test: `chay_thu.ps1` còn tham chiếu `module\ui\webview2` (đã dời sang `module\youtube\webview2` lúc tách plugin) nên dừng ngay ở bước biên dịch UI.
LƯỢT 2 (người dùng: "browser brave phải mở trong cad, khi đó tôi thao tác trong cad vẫn hiển thị ở bên cad"): `module/youtube/GanBrave.cs` gán cửa sổ AutoCAD làm
CHỦ (owner, SetWindowLongPtr GWLP_HWNDPARENT) cho mọi cửa sổ trình duyệt của Brave hồ sơ riêng (timer 0,8 s tới khi Brave tắt; brave.exe chỉ chuyển link thì dò Brave
chính bằng WMI); AutoCAD BeginQuit thì trả về độc lập. Không nhúng bằng SetParent (khung tab tự vẽ của Chromium hỏng). Rủi ro: Brave treo thì AutoCAD đứng theo
(hàng đợi nhập nối khi chủ khác tiến trình). Test mới `test/thu_brave.ps1` (+ `ThuBrave.cs`, hồ sơ Brave tạm, cửa sổ nhỏ hiện vài giây): TAT CA DAT.
LƯỢT 3 (người dùng gửi ảnh khung chat mẫu: "link với vs code hoặc claude code hoặc anti, cho phép chọn vị trí chứa skill hay folder dự án"):
- `module/botchat/PhienAi.cs` (`KhoPhien`): phiên Claude Code ở `~/.claude/projects/*/*.jsonl` (cwd, entrypoint -> VS Code / Claude Code / Desktop / Bot chat, tên custom-title > ai-title > câu đầu);
  hội thoại Antigravity đọc thô SQLite `~/.gemini/antigravity/conversation_summaries.db` + `-wal` (header bản ghi, cột id/title/preview/giờ/thư mục; khớp Python sqlite3).
- Bot chat: ô "Dự án" = thư mục làm việc CLI (`chat_du_an`, gần đây `chat_du_an_ds`), khác cad\ thì `--add-dir` cad\; MCP cấu hình tuyệt đối `%LOCALAPPDATA%\TKPCCC\botchat\mcp.json`;
  bỏ `--disable-slash-commands`, thêm công cụ `Skill`. "+ Skill" (`chat_skill`): `.claude\skills` -> `--add-dir`, `skills\` -> `--plugin-dir`, thư mục skill trần -> lớp bọc junction
  `%LOCALAPPDATA%\TKPCCC\botchat\skill\<tên>\skills` + `--plugin-dir` (dò 18/11 bằng dòng init của claude 2.1.326 với model giả: không tốn hạn mức). Menu "/ Skill".
  "Lịch sử / Link": Claude `--resume <id> --fork-session` lần đầu (phiên gốc giữ nguyên), Antigravity `--conversation <id>`. Bong bóng tin (RichTextBox canh phải/trái + nền),
  dòng trạng thái "✻ Claude đang … m:ss · effort". Ảnh chụp giao diện ngoài AutoCAD (Form thường, DLL AutoCAD nạp từ thư mục cài): đạt. Test mới `test/thu_bot.ps1`: TAT CA DAT.
LƯỢT 4 (người dùng: hộp "Browse For Folder" chậm, "thả link folder để nhảy tới nhanh nhất"; "copy luôn kiểu cách khung chat mẫu, model có nhiều đời Opus 5.5 / 5 / 4.8, Gemini flash 3.8, argon, carbon"):
- `ChonThuMuc.cs`: hộp chọn thư mục kiểu Explorer (IFileOpenDialog + FOS_PICKFOLDERS, dán đường dẫn Ctrl+L); clipboard có đường dẫn thư mục thì mở sẵn tới đó;
  lỗi COM thì về FolderBrowserDialog. Nút dự án: thả thư mục từ Explorer, mục "Dùng đường dẫn đã copy".
- Giao diện viết lại theo ảnh mẫu (`BotChat.cs` + `GiaoDienChat.cs`): thanh đầu (dự án giữa, mới / lịch sử / cài đặt, biểu tượng font Segoe MDL2 Assets), bong bóng tự vẽ
  (chuột phải Sao chép), dòng trạng thái trên khung soạn, khung soạn bo góc: chip, ô nhập, + / model / gửi tròn. Model: Claude theo bảng Anthropic (Opus 5.5, 5, 4.8, 4.7, 4.6;
  Sonnet 5.5, 5, 4.6; không Haiku / Fable), Antigravity đọc `agy models` nền (lưu `agy_models`, gom gốc-mức). Khoá cũ model "opus"/"sonnet" đổi sang claude-opus-5-5 / claude-sonnet-5-5.
  Ảnh chụp ngoài AutoCAD (Form ngoài màn hình + PrintWindow): khớp bố cục mẫu.
LƯỢT 5 (19/11, ảnh: bot trả lời "phiên này tắt mọi công cụ sửa file"; hộp "Plot Job Progress" kẹt không tự tắt; "như VS Code: vừa chat, vừa xuống hàng, vừa gửi ảnh"):
- Bot chat Claude: thêm Edit/Write + `--permission-mode acceptEdits` (tự duyệt trong thư mục dự án + --add-dir; không mở Bash). Kiểm bằng dòng init (model giả).
- Enter khi bot đang chạy = xếp hàng (gửi khi xong lượt), nút đỏ mới dừng (dừng thì bỏ hàng chờ). Phím gõ ở ô nhập: `Application.PreTranslateMessage`
  chuyển thẳng WM_KEYDOWN/KEYUP/CHAR cho ô nhập (AutoCAD không giữ Ctrl+V = PASTECLIP, Enter); `PaletteSet.KeepFocus` bật khi ô nhập có focus.
- `InAnh` (`module/plugin/Spec.cs`, thao tác `anh` / `cad_anh`): bỏ lệnh -PLOT, in bằng PlotEngine (hộp tiến trình null: không hiện "Plot Job Progress"),
  khổ theo tên chuẩn, cửa sổ WCS -> DCS bằng acedTrans, đang có việc in khác thì báo lỗi ngay. chay_thu ca anh đạt, ảnh test đúng vùng.
- Việc rải đầu báo lệch ra ngoài polyline vùng (vùng cạnh xiên V5, hành lang chữ L V3e): phiên nhánh `cad-rai-trong-vung` sửa (mục VIỆC 19/11 RẢI bên trên),
  đã gộp vào main 19/11.
LƯỢT 6 (19/11, ảnh: gõ "chiếu" bằng Unikey ra "chiẤu"; "làm giống ảnh 2" = khung chat mẫu gõ đúng chữ Việt): `ChuyenPhim` gọi `DispatchMessage` khai báo thiếu
  CharSet nên P/Invoke lấy bản ANSI (DispatchMessageA) -> WM_CHAR Unicode bị đổi sang mã ANSI (ế 1EBF thành 00BF). Sửa: `CharSet.Unicode` (DispatchMessageW).
  Tái hiện ngoài AutoCAD bằng TextBox + WM_CHAR: bản cũ ra 00BF, bản mới 1EBF.
LƯỢT 7 (19/11, người dùng: "dùng bot chat ở tab vẽ này, qua tab file khác vẫn làm được, không nhận diện sai file"): trước đây cầu nối luôn chạy trên tab đang hiện,
  spec có `ban_ve` mà người dùng đã đổi tab thì cả gói bị từ chối. Nay: Bot chat gắn bản vẽ đang hiện lúc GỬI (Document.Name) vào env `TKPCCC_CAD_BAN_VE` của máy chủ MCP
  (mcp.json, `KhoPhien.FileMcp`; dò 19/11: Claude Code gộp env này với env hệ thống) + báo AI một dòng "Bản vẽ của lời nhắn này" khi hội thoại mới / đổi bản vẽ.
  `Mcp.cs` ghi đè `ban_ve` của gói bằng env. `Cau.cs`: gói có `ban_ve` thì tìm bản vẽ đang mở khớp (`Lenh.TimBanVe`: đường dẫn, rồi tên file); đang hiện thì PCCC_SPEC
  như cũ, không hiện thì `Lenh.ChayNen` (LockDocument có tên lệnh = 1 bước Undo của bản đó, WorkingDatabase tạm trỏ bản đó), không còn mở thì trả lỗi.
  `pdf`, `anh` cần bản vẽ đang hiện (báo lỗi rõ). TU_VUNG.md + skill tkpccc-ve-cad sửa nghĩa `ban_ve`. Test: thu_mcp (env ghi đè ban_ve), thu_gui (COM mở bản vẽ 2,
  việc khoá bản 1 vẽ nền đúng file, anh bị từ chối, cad_tt = bản đang hiện, bản không mở bị từ chối). chay_thu 172 DAT, thu_mcp, thu_bot TAT CA DAT.
  thu_gui TAT CA DAT trên AutoCAD 2021 thật (Drawing1 vẽ nền khi Drawing2 đang hiện, anh bị từ chối, bản không mở bị từ chối).
LƯỢT 8 (19/11, người dùng: "bot chat như cửa sổ Claude Code và VS Code, tuỳ chọn trong setting link với VS Code hay Claude Code"; "đang chạy prompt 1, Ctrl+X gửi liền
  prompt 2 để tích hợp chung prompt 1, không nhấn thì để hàng chờ"; hỏi lại: "link" = NỐI PHIÊN CHUNG):
- Gửi gấp Ctrl+X (`GuiGap`): Claude chạy `--input-format stream-json`, stdin mở suốt lượt; Ctrl+X ghi thêm lời nhắn vào stdin, Claude nhận ở chỗ nghỉ kế tiếp
  giữa hai bước công cụ và làm tiếp gộp với yêu cầu trước (dò 19/11 trên claude 2.1.326 bằng Haiku: đọc f1, nhận "bỏ f4–f6", ra một lượt một result).
  Có dòng result thì đóng stdin (`DongVao`) cho claude thoát; lời nhắn gấp tới sát lúc đó claude đã đọc thì chạy thêm một lượt. Ctrl+X khi có bôi đen = Cắt như thường;
  bot không chạy = gửi như Enter; Antigravity / lượt đang kết thúc = xếp hàng. Lời nhắn gấp gửi lúc đang xem bản vẽ khác: báo AI cad vẫn làm trên bản của lượt.
  Enter khi đang chạy vẫn xếp hàng như cũ. Đã dò: lệnh `/skill` vẫn chạy khi gửi qua stream-json. Luôn `--add-dir` thư mục ảnh (ảnh của lời nhắn gấp đọc được).
- Cài đặt "Nối phiên chung" (`chat_noi`: "" / "vscode" / "claude"): chọn VS Code hoặc Claude Code thì phiên chọn ở Lịch sử ghi thẳng vào phiên gốc (không --fork-session),
  trừ phiên vừa ghi trong 2 phút (có thể đang chạy nơi khác: vẫn tách nhánh). Lịch sử xếp phiên của bên đó lên trước. "Mở hội thoại này trong …" (menu cài đặt, hoặc bấm
  chip link): VS Code = `code <thư mục phiên>` rồi 3 s sau `vscode://anthropic.claude-code/open?session=<id>` (extension 2.1.327); Claude Code = `claude://resume?session=<id>`
  (app nhập phiên CLI, dò trong app.asar 6.6395). Dựng dòng stream-json chung: `KhoPhien.DongNguoiDung` (PhienAi.cs).
- Test: thu_bot mục 4 (claude thật, model giả không tốn hạn mức, `--replay-user-messages`: nhận đúng chữ có \, ngoặc kép, xuống dòng, chữ Việt; result rồi đóng stdin thì thoát).
  chay_thu, thu_mcp, thu_bot TAT CA DAT. thu_gui lần 1 lỗi 2 mục (`cad_anh` ngay sau gói pdf + plot báo "AutoCAD chưa mở": song.json trễ > 6 s lúc AutoCAD bận; đếm
  BC_KHOI rỗng theo đó), chạy lại TAT CA DAT: chập chờn của test, không do mã sửa.
  CHƯA thử bằng tay: Ctrl+X trong palette thật với model thật; mở phiên sang VS Code / app Claude Code (cần người dùng bấm thử).
CHƯA: thử trong AutoCAD có giao diện (AutoCAD người dùng đang mở: tắt hẳn, mở lại để nạp bản mới; `thu_gui.ps1` chưa chạy); chưa gửi câu Bot chat thật (tốn hạn mức);
Ctrl+V trong palette có thể bị AutoCAD giữ làm PASTECLIP (khi đó dùng chuột phải → Paste hoặc "+ Ảnh"); WebView2 trong CAD chưa có tab (người dùng hỏi tab ở Brave).

## VIỆC 17/11 (8) — TÁCH 3 PLUGIN (PCCC · YOUTUBE · BOT CHAT), BOT CHAT CHỌN EFFORT, YOUTUBE CỬA SỔ CO GIÃN: XONG, ĐÃ THỬ TRONG AUTOCAD
Người dùng: "tách riêng plugin youtube, plugin TKPCCC-PCCC và plugin bot chat ra riêng làm 3 plugin ... bot chat cho chọn model và effort ... Youtube thu nhỏ lại,
kéo giãn lên xuống trái phải được ... tự động chạy không cần hỏi quyền".
- 3 bundle: `TKPCCC.bundle` (PCCC, như cũ) · `TKPCCC_YouTube.bundle` (`TKPCCC.YouTube.dll` + 3 DLL WebView2, lệnh `TKYOUTUBE`) · `TKPCCC_BotChat.bundle`
  (`TKPCCC.BotChat.dll`, lệnh `TKBOTCHAT`). Mã: `module/youtube/`, `module/botchat/`, giao diện chung tối thiểu `module/nho/GiaoDienNho.cs` (cùng `cai_dat.json`,
  cùng kiểu màu) → 2 plugin riêng KHÔNG cần DLL PCCC. Palette PCCC bỏ nút YouTube, Bot chat; thẻ "Sơ đồ nguyên lý" gọi `TKBOTCHAT` (khoá `bot_mau_cho`), chưa cài thì chỉ Hub.
- YouTube: Form riêng `ShowModelessDialog` (AutoCAD nhớ vị trí, kích thước), thu nhỏ / phóng to / kéo giãn 4 cạnh, đóng thì dừng video.
- Bot chat: ô effort. Claude `--effort low|medium|high|xhigh|max` (`chat_effort`); Gemini mức nằm trong tên model agy (`gemini-4-argon-high`): model = gốc + "-" + mức.
- Hub: mục `plugin` (PCCC), `plugin_bot` (nhóm Bot chat), `plugin_yt` (nhóm YouTube) cài / gỡ riêng; DLL WebView2 chép vào bundle YouTube. `kiem_lien_ket.py` đọc mọi `cad/*.bundle/PackageContents.xml`.
- Test: `chay_thu`, `thu_mcp`, `thu_hub` (15 mục, gỡ riêng YouTube), `thu_gui` (AutoCAD thật: TKYOUTUBE cửa sổ CanResize + CanMinimize, TKBOTCHAT mở palette) đều TAT CA DAT.
- Máy người dùng: đã chạy Hub `/cai plugin_yt,plugin_bot` (2 liên kết mới trong ApplicationPlugins). Mở lại AutoCAD là có 3 plugin.
CÒN: chưa gửi thử một câu Bot chat thật với effort (tốn hạn mức; cờ đã kiểm bằng `claude --help`, `agy models`).

## VIỆC 17/11 (7) — KHOANG DẦM: 6 TRƯỜNG HỢP + 3 LOẠI TRẦN: XONG MÃ + TEST, CÒN CHẠY THỬ FILE "test vùng báo cháy.dwg"
Người dùng dạy quy trình: (1) vẽ mặt bằng mô tả từng trường hợp 5.9.1.1.6 để xem hiểu đúng không; đầu báo lắp trên dầm phải nằm GIỮA bề rộng dầm.
(2) Mặt bằng dầm sàn block lại, chép đè lên mặt bằng tầng; đọc cỡ dầm để biết trường hợp. Có trần: rải như tiêu chuẩn. Không trần: xác định trường hợp,
vẽ vùng rồi mới rải; mép dầm chính coi như tường để dim (khi phải rải trong khoang ≥ 4 / ≥ 9 m²). Vừa có vừa không: người dùng vẽ tay vùng + dầm. Thêm list chọn 3 trường hợp.
Đã làm (Rai.cs, NhanVung.cs, ui/BangBoTri.cs):
- `PCCC_RAI` từ khoá `LoaiTran` [CoTran/KhongTran/HonHop] ở dấu nhắc trần (`Dam` cũ = KhongTran, ẩn); palette tab Khoang dầm có ô chọn "LOẠI TRẦN" (`kd_loai_tran`).
  HonHop: bỏ bước nét dầm mẫu, chọn thẳng line dầm vẽ tay (TungDam) + b x h; bắt buộc có polyline PCCC_VUNG. Line ở tim dầm tự dời ra hai mép ± b/2 (`MepDam`).
- Tim dầm (`TimDam`): ghép cặp mép dầm song song cách 0,1–1,6 m. Khoang nhỏ hơn ngưỡng (c, e): đầu báo dời về tim dầm gần nhất (`DatLenDam`), phủ bù trên tim dầm (`PhuBuDam`), dòng lệnh `len_dam=`.
- Dim chung các khoang (`kt_gom`) thêm mốc mép dầm (đầu báo trong khoang: mép dầm = tường). Trần < 2 m không đánh X khoang.
- SỬA LỖI: `ChiaKhoang` bỏ mọi ô < 1 m rộng TB (ô 1,8 x 1,8 m = 0,9 m) nên khoang nhỏ không bao giờ chia được; nay ngưỡng 0,5 m² / 0,4 m, lòng dầm bỏ theo tim dầm.
- Hình minh hoạ 6 trường hợp a–f do plugin tự rải: `ket_qua/cad_test/2026-11-17/khoang_dam_6th/` (3 PNG + DWG, đã gửi người dùng). Sinh lại: `scratchpad` `tao_6th.py` (đã chép vào thư mục kết quả).
- Test ca 13 (chay_thu kb6): 24 khoang 1,8 x 1,8 m, `len_dam=2`. Loi 6 nới ≤ 10 dim (thêm mốc mép dầm).
ĐÃ CHẠY THỬ file test (13:46): block mặt bằng dầm (DAM_T7) dời (-30858, +750) khớp trục; khung C cả mặt bằng (cần lấy cả ký hiệu trục, khung hẹp thì trục ngang thành tường) → 1 vùng 270,82 m² (8 đỉnh), 2 khoang theo dầm sâu 700X250: 28,4 + 230,8 m² (≥ 9 m²), dầm 400X250 d = 0,28 bỏ, 7 đầu báo. Ảnh + DWG: `ket_qua/cad_test/2026-11-17/khoang_dam_file_test/` (đã gửi, CHỜ DUYỆT).
SỬA thêm: dầm trong block mặt bằng dầm chép đè (block ngoài cùng chứa nét dầm mẫu) nhận theo LAYER, mọi màu (`NenVung.LopKhoiDam`), chỉ khi còn khối/tường khác được chọn.
Mẹo console: nét dầm mẫu cách trục 100 mm, bấm phải zoom trong suốt (`'_.ZOOM W`) kẻo trúng trục.
Cũ (đã làm xong):
1. Chạy thử trên bản chép `file_cad_test/test vùng báo cháy.dwg` (người dùng đang mở, KHÔNG sửa gốc): mặt bằng tầng = block "2333" (trục dọc x 711176 / 715376 / 726551,
   trục ngang y 403864 / 411264 / 418564 / 425964); mặt bằng dầm tầng 7 ở x 739000–761000, y 400000–427000 (dầm = hình chữ nhật kín layer AAP-TUONG, nhãn "DS 400X250"
   ghi ngược kiểu KOCHAM, layer AAP-TEXT). Chép đè: block phần mặt bằng dầm, dời (-30858, +750). Rồi `PCCC_RAI BC_KHOI LoaiTran KhongTran DaySan 120 4.5`, khung C phần
   bên phải lõi + lõi, bấm nét dầm mẫu (AAP-TUONG trong block dầm). Lưu ý: layer dầm là AAP-TUONG (tên tường) — xem có bị dựng làm tường không. Vẽ vùng + khoang, gửi ảnh, CHỜ DUYỆT mới rải.
   Phần trái mặt bằng có hatch BT-DA1X2 (4 hatch, layer AAP-HATCH 50): chưa rõ nghĩa (có thể là phần sân thượng / không thuộc phạm vi) → hỏi người dùng nếu plugin nhận làm vùng.
2. Chưa thử palette trong AutoCAD có giao diện (AutoCAD người dùng đang mở; `thu_gui.ps1` chỉ chạy khi tắt AutoCAD).

## VIỆC 17/11 (8) — PALETTE: THU GỌN THANH DỌC + YOUTUBE BẰNG BRAVE: XONG MÃ + TEST, CHỜ NGƯỜI DÙNG XEM TRONG AUTOCAD
ĐÃ LÀM (13:45): (1) nút ☰ góc phải đầu palette (`BangChinh.DauTrang`, `MenuPhanHe`): "Thu gọn thanh dọc" / "Hiện thanh dọc" (nhớ `ben_an`) + danh sách
phân hệ để chuyển khi thanh dọc ẩn. (2) Tab YouTube nút "Xem bằng Brave" / "Xem trong CAD" (nhớ `yt_trinh_duyet`): chế độ Brave bỏ WebView2, Enter/Trang chủ
chạy `brave.exe --app=<url> --window-position --window-size` rộng 480 px áp mép phải trong cửa sổ AutoCAD; chưa có Brave thì mở trang tải. Tìm brave.exe:
`HangDoi.TimBrave` (Program Files, x86, LOCALAPPDATA). (3) Hub nhóm YouTube thêm mục `brave` (13 mục); `thu_hub.ps1` kiểm 13 mục + trang brave.com/download.
Kiểm: build.bat OK, chay_thu / thu_mcp / thu_hub TAT CA DAT. CHƯA xem tận mắt (palette ngoài AutoCAD không chụp được: không viết harness; AutoCAD người dùng đang mở).
CÒN: người dùng tắt hẳn AutoCAD, mở lại, gõ PCCC, thử ☰ và Brave; hỏi có muốn ẩn hẳn thanh dọc không. Brave đang mở sẵn có thể bỏ qua vị trí cửa sổ (shortcut ghi trong `BangYoutube.MoBrave`).
YÊU CẦU GỐC:
Người dùng 17/11 (ảnh palette đang xem YouTube, khoanh đỏ cả thanh biểu tượng dọc bên trái):
1. "Build pallet sao cho ẩn mấy hàng tôi khoanh" = thanh dọc phân hệ (`ben` trong `module/ui/BangChinh.cs`). Ẩn hẳn thì mất đường chuyển phân hệ,
   nên làm: nút thu gọn / hiện thanh dọc (nhớ trạng thái trong cài đặt, vd khoá `ben_an`); thu gọn vẫn chuyển phân hệ được (vd menu ☰ ở đầu trang).
   Làm xong hỏi người dùng có muốn ẩn hẳn không.
2. "Dùng Brave để lướt YouTube thay WebView được không": Brave KHÔNG nhúng vào palette được (không có SDK nhúng). Làm: cài đặt chọn trình xem
   YouTube = WebView2 (trong palette, như hiện tại) | Brave (cửa sổ riêng `brave.exe --app=<url>`, đặt sát cạnh phải cửa sổ AutoCAD). Tab YouTube ở chế độ
   Brave: ô link/từ khoá + nút mở trong Brave. Brave ở `C:\Program Files\BraveSoftware\Brave-Browser\Application\brave.exe` (tìm cả LOCALAPPDATA).
   Hub (`module/hub/Hub.cs`, nhóm YouTube): thêm mục "Brave (tuỳ chọn)": kiểm có/không, Cài = mở https://brave.com/download/.
3. Người dùng dặn: làm ở lần reset 5 giờ kế, compact (phiên nhánh ngữ cảnh sạch) rồi mới làm.

## VIỆC 17/11 (6) — HUB BẢN 2: MỌI BỘ CÀI CHIA THEO TÍNH NĂNG: XONG, CHỜ NGƯỜI DÙNG DÙNG THỬ
Người dùng (ảnh MCP Hub Pro): gom mọi bộ cài vào Hub, phân rõ từng loại. Hub thành bảng 12 mục, 4 nhóm, nút lọc theo nhóm, ô chọn, "Chọn mục còn thiếu", "Cài / Gỡ mục đã chọn" (một hộp xác nhận liệt kê từng việc), "Quét lại máy". Nền tảng: AutoCAD, .NET 4.8 (registry Release ≥ 528040), Plugin tự nạp · MCP cad: máy chủ + 4 AI · Bot chat: `claude`, `agy` (--version) · YouTube: WebView2 Runtime (registry EdgeUpdate pv), 3 DLL (Cài = chép từ module/ui/webview2). Phần mềm hãng khác: chỉ mở trang tải chính thức. `thu_hub.ps1` 17/17 DAT. Máy người dùng: 7/12 xong; chưa: 4 AI ← MCP cad.
LƯU Ý 17/11: build.bat lúc đó hỏng ở `Rai.cs` (phiên khác đang sửa dở) → bundle mất TKPCCC.Cad.dll, đã chép lại bản 10:32 (TKPCCC.Cad.dll.cu1372).

## VIỆC 17/11 (5) — G8 TAB YOUTUBE: XONG, CHỜ XEM TRONG AUTOCAD
Người dùng chọn WebView2 (Chromium có sẵn của AutoCAD 2021 `AcCef` là bản 77, YouTube không chạy). Tải gói NuGet Microsoft.Web.WebView2 1.0.2903.40 (được người dùng cho phép), lấy 3 DLL net462 + win-x64 (chữ ký Microsoft hợp lệ) vào `module/ui/webview2/`; build.bat chép cạnh TKPCCC.UI.dll khi thiếu.
`module/ui/BangYoutube.cs`: phân hệ "YouTube" (nút ▶ thanh dọc): ô link / từ khoá, Lùi, Trang chủ, Mở bằng trình duyệt; dữ liệu trình duyệt `%LOCALAPPDATA%\TKPCCC\WebView2`; dừng video khi rời tab; nhớ link cuối `yt_cuoi`. Thử thật ngoài AutoCAD: Chrome 157, tìm ra 16 video.
CHƯA thử trong AutoCAD (đang mở): rủi ro nạp DLL WebView2 từ thư mục bundle (đã gắn AssemblyResolve + SetLoaderDllFolderPath).

## VIỆC 17/11 (4) — G7 HUB CÀI MCP: XONG BẢN ĐẦU, CHỜ NGƯỜI DÙNG BẤM CÀI
`module/hub/Hub.cs` → `bin/TKPCCC.Hub.exe` (build.bat). Mục: AutoCAD 2021–2024 (registry, đang mở, plugin đang nối theo song.json) · Plugin tự nạp (junction như cai_dat.bat; Gỡ chỉ bỏ liên kết, thư mục thật thì từ chối) · MCP cad (bắt tay initialize) · Claude Code (`claude mcp add -s user`) · Claude Desktop (`%APPDATA%\Claude\claude_desktop_config.json`) · Gemini CLI (`~/.gemini/settings.json`) · Antigravity (`agy mcp add`). JSON: sao lưu `.tkpccc_<giờ>.bak`, giữ khoá khác. Test `test/thu_hub.ps1` (nhà giả, CLI chỉ ghi lệnh) 14/14 DAT.
Máy người dùng 17/11: AutoCAD 2021 mở + plugin nối, bundle đã liên kết, MCP bắt tay được; 4 AI CHƯA có cad (Claude chưa tự bấm: sửa cấu hình máy là việc người dùng).
CÒN: VS Code / Codex chưa có mục; bot chat chạy Gemini ở `--mode plan` nên dù agy có cad vẫn chưa vẽ (cần thử quyền gọi MCP ở chế độ in); G8 YouTube chưa làm.

## VIỆC 17/11 (3) — BOT CHAT CHỌN CLAUDE/GEMINI + THẺ THƯ MỤC DỰ ÁN: XONG MÃ, CHỜ XEM TRONG AUTOCAD
Người dùng gửi ảnh khung chat FP AI: chọn model Claude hay Gemini, gắn thẻ thư mục dự án làm ngữ cảnh. Bot chat tách ra `module/ui/BotChat.cs`:
đầu khung chọn máy AI + model (bỏ ô model ở trang Cài đặt), hàng thẻ "+ Thư mục dự án" (`--add-dir` cho CLI + danh sách ở đầu lời nhắn, lưu `chat_thu_muc`).
Gemini = `agy -p ... --output-format stream-json --mode plan` (chỉ đọc), nói tiếp bằng `--conversation`. Đã chạy thật ngoài AutoCAD: Gemini đọc đúng file
trong thư mục gắn thẻ; Claude CLI trên máy báo "OAuth session expired" (người dùng gõ `claude` rồi `/login`). CHƯA xem trong AutoCAD (đang mở nên chưa chạy thu_gui.ps1).
CÒN: (a) Gemini vẽ CAD cần đăng ký MCP cad cho agy (`agy mcp add cad <repo>\cad\bin\TKPCCC.Mcp.exe`, sửa cấu hình máy: hỏi người dùng; `--settings` không nhận mcpServers)
và cho phép công cụ MCP ở chế độ in; (b) G7 Hub (`module/hub/`) CHƯA làm; (c) G8 tab YouTube (WebView2) CHƯA làm.

## VIỆC 17/11 (2) — TỰ NHẬN VÙNG RẢI KHỚP VÙNG NGƯỜI DÙNG VẼ: XONG MÃ + TEST, CHỜ NGƯỜI DÙNG XEM
Người dùng gửi 2 ảnh: ảnh 1 = plugin tự nhận vùng (nhiều vùng vụn), ảnh 2 = người dùng vẽ lại (1 vùng văn phòng chữ L, bỏ lõi thang, sảnh, WC).
Chạy lại `PCCC_RAI BC_KHOI Dam DaySan 120 4.5`, khung C cả sàn, Enter mọi phòng trên `t8_mat_bang_dam.dwg` (kịch bản `scripts/kb_tu_nhan.txt`):
TRƯỚC: 4 vùng (văn phòng bị dầm 700 cắt 30,1 + 231,4 m²; WC dưới 8,9 m² vì dầm cắt WC ra mảnh không có block có tên WC; sảnh 6,1 m²).
SAU: 1 vùng 271,7 m² 8 đỉnh, trùng vùng người dùng vẽ (`t8_vung_tu_nhan_TRUOC_sua.png`, `t8_vung_tu_nhan_SAU_sua.png`).
Sửa: (1) `MatChon` không đưa dầm vào dựng phòng, trả `damSau`; `PCCC_RAI` chia khoang trong phòng đã chọn (`ChiaKhoang`), vẽ phòng lên `PCCC_VUNG`,
khoang lên `PCCC_KHOANG_M2`; (2) block thang máy (tên/layer THANG MAY, LIFT, ELEVATOR, hộp ≤ 20 m²) thành ô X; phòng ≤ 30 m² cách ô X ≤ 0,6 m = sảnh
thang, đầu báo bỏ (`bo_sanh_thang=`). Test ca 10, 11 `chay_thu.ps1`.
LƯỢT 2 (cùng ngày): ảnh 1 chạy trên file người dùng "PHƯƠNG ÁN KT-KC-HT KOCHAM" (tìm thấy bản tự lưu .sv$ của AutoCAD, thử trên bản chép trong scratchpad,
không lưu vào repo). Người dùng dặn: "ngoài đường trục thì không được phân vùng khi không được tôi cho phép" (memory `vung-trong-duong-truc`).
File đó: cả hồ sơ là block "2333" trên layer `AAP-HATCH 50`, trục + mặt dựng là polyline layer 0 → dựng bằng mọi nét liền, trục cắt văn phòng; đọc cả tờ
khác (vùng lặp, vùng dưới X-1, 9,5 s); cung nắp bồn cầu (= "cửa") làm khép khe 11 m; 2 khe song song (mặt cột + mặt vách lõi) khép khe 11 m. Sửa:
(3) quét khung thì chỉ đọc nét trong khung nới 5 m, chỉ giữ phòng có điểm trong trong khung; (4) ký hiệu trục (block TRUC/AXIS/GRID, vòng có chữ) → khung
trục; nét ≥ 3 m trùng trục bỏ khi dựng bằng mọi nét; Enter mọi phòng bỏ phòng ngoài khung trục (`bo_ngoai_truc=`); (5) hộp cửa phải phủ ≥ 25 % khe mới
khép; hai khe song song chỉ khép khi ≤ 6 m. Kết quả file người dùng: 1 vùng 270,8 m² 8 đỉnh (= ảnh 2). Test ca 12.

## VIỆC 17/11 — TẦNG 8 KOCHAM TRẦN 4,5 M, SÀN 120 (ĐỀ XUẤT LẦN 2 ĐÃ DUYỆT): ĐÃ RẢI, CHỜ NGƯỜI DÙNG NHẬN XÉT
Người dùng duyệt đề xuất vùng lần 2 (d = h dầm − dày sàn; không cho số sàn, dùng 120: 100–150 kết quả như nhau). Plugin: từ khoá `DaySan` ở dấu nhắc
trần (đầu báo) và bước S (sprinkler), giuDam/TungDam/cỡ chung so d − dày sàn > 0,3; test ca 8 `chay_thu.ps1`. Kịch bản `scripts/tao_dam.py` (kb_bc3, kb_spr3):
`t8_bc_tran45.dwg/.png`: 2 khoang (231,3 m² lưới 2 x 3 bước 5,4 x 7,2 m; 29,2 m² 1 đầu) + hành lang 1 = 8 đầu, KT mù 0 %, dây 54,6 m.
`t8_spr_tran45.dwg/.png`: 21 + 4 + sảnh 1 = 26 đầu lên, 5 đầu xuống (WC, hành lang), nhánh TuDong 94,5 m.
GHI CHÚ: hàng đầu báo khoang 1 nằm sát dầm 400 (cách tim 0,1–0,2 m) — đúng luật (dầm nông coi trần phẳng) nhưng lắp dưới đáy dầm; nếu người dùng muốn
tránh dầm nông thì thêm bước dịch hàng/cột ra giữa ô (như mẫu: lề cố định tới dầm). Lưới chung cho nhiều khoang (bài học mẫu) chưa làm trong plugin.
17/11 người dùng sửa: dầm nhãn "DS 1400X250" qua sảnh thang máy thật là RỘNG 1400, CAO 700 (công trình ghi h x b ngược đời). d = 0,58 m vẫn sâu; dầm nằm
ở sảnh/lõi (không rải đầu báo) nên kết quả không đổi. ĐÃ SỬA PLUGIN (người dùng "sửa luôn"): từ khoá `SuaDam` ở dấu nhắc "Bấm nét dầm màu khác"
= bấm nét dầm, nhập cỡ thật b x h, thắng nhãn; cỡ gõ tay (SuaDam, TungDam, cỡ chung) đọc đúng thứ tự rộng x cao (`DocBxh(s, true)`); test ca 9 `chay_thu.ps1`.
`kb_bc3`/`kb_spr3` thêm SuaDam nét y 30773 (x 32143–35418) = 1400x700: d 0,7, vẫn sâu, kết quả y như cũ (8 đầu báo, dây 54,58 m; 26 + 5 đầu phun, 94,5 m).
Thử 300x250 trên cùng nét: dam_sau 83 → 82, chứng tỏ bấm trúng nét dầm. Chưa làm: vẽ bề rộng thật của dầm bẹt (bản kết cấu đã vẽ viền dầm, plugin dùng các nét đó).

## VIỆC 16/11 TỐI (5) — SỬA 6 LỖI PLUGIN LỘ RA Ở TẦNG 8 KOCHAM: XONG MÃ + TEST, CHƯA XEM TRONG AUTOCAD CÓ GIAO DIỆN
Người dùng dặn 16/11 tối: "Sửa 3 vấn đề về plugin đi" (1–3), cộng 3 lỗi lộ ra khi thiết kế theo dầm (4–6). Test mới: mục "Sua 6 loi" trong
`cad/test/chay_thu.ps1` (một bản vẽ, 6 ca). `chay_thu.ps1`, `thu_mcp.ps1` TAT CA DAT; bundle đã build (AutoCAD đang mở thì tắt hẳn rồi mở lại).
1. Gộp lõi thang: block lõi "CORE" đặt trên layer "HANA FURNITURE" (nội thất = bỏ) nên tường `AAP-TUONG` bên trong bị bỏ. `ThuNet` (`NhanVung.cs`): nét lồng
   có layer RIÊNG là tường vẫn lấy dù block cha trên layer bỏ; `DocDs` (vung_tu_dong) không bỏ block trên layer bỏ. Tầng 8: 293,19 → 271,72 m²
   (vung_tu_dong 269,35; lệch 2,4 m² = bề dày tường trái dải trên vì mép trong tường vẽ trên layer `0`, chế độ chỉ-tường không thấy).
2. `PCCC_ONG` (`Lenh.cs` `VeOng`): tự hỏi điểm (GetPoint vòng lặp, thêm đỉnh dần), không còn `SendStringToExecute("_.PLINE")`; in `PCCC_ONG <mã> doan= m=`.
3. `PCCC_TUYEN` (`Rai.cs` `LocDauPhun`): nhóm đầu phun theo (cụm vùng `PCCC_VUNG` hộp bao cách ≤ 0,6 m, mã thiết bị), nối nhóm đông nhất, in `tuyen_loc`.
   Tầng 8 khung W: bỏ 5 đầu hướng xuống (WC, hành lang), giữ 31 đầu hướng lên (cả 1 đầu sảnh, vùng sảnh sát văn phòng).
4. `Dam` + vùng vẽ sẵn (`MatChon` → `ChiaKhoang`): chia vùng theo nét dầm sâu (kéo dài mỗi đầu 0,4 m: nét dầm sơ đồ dừng cách dầm biên 200 mm),
   bỏ dải lòng dầm (`PhongThat`), vẽ nét khoang trên `PCCC_KHOANG_M2`, in `chia_khoang=`. Nhãn dầm ghép với nét cùng phương chữ trước (trước: nhãn dầm
   cắt ngang gần hơn làm dầm sâu thành nông, 2 cặp khoang gộp). Tầng 8: 20 khoang, 20 đầu báo (+ sảnh, hành lang = 22).
5. `PCCC_DAYBC` (`DayBc.cs` `LuoiDay`, `NeLoi`): có vùng `PCCC_VUNG` (≤ 10 m quanh thiết bị) thì đoạn gãy góc phải nằm trong vùng, hoặc khe giữa HAI vùng
   ≤ 0,5 m (qua tường), hoặc ≤ 1 m quanh điểm dừng; không được thì thử góc gãy kia, rồi Dijkstra lưới ô 0,25 m (rẽ phạt 2 m). In `ne_loi=`. Tầng 8: dây
   sảnh → dải trên đi qua văn phòng (x 36020), không cắt lõi.
6. Dim: khoang chia từ một vùng vẽ sẵn dim chung trên vùng đó sau vòng rải (`kt_gom`). Tầng 8: 80 dim → 15. Thêm từ khoá `Dam` cho sprinkler ở dấu nhắc S
   (chia khoang, dim chung như đầu báo).
Chạy lại: `scripts/tao_dam.py` (S = thư mục scripts) sinh `kb_bc3.txt`, `kb_spr3.txt` chạy trên `t8_mat_bang_dam.dwg` → `t8_bc_dam_sua.dwg/.png`,
`t8_spr_dam_sua.dwg/.png` (ket_qua, gitignore). Sprinkler: 31 đầu hướng lên vì khoang đo theo nét dầm sơ đồ rộng 200 (thật 250) ra 12,1–12,4 m² > 12 m².
THÊM 16/11 tối (người dùng đồng ý làm cả hai): (d) Enter = mọi phòng của đầu báo tự bỏ phòng ≤ 60 m² có dấu hiệu lõi thang, buồng thang, WC
(`GomKhu` trong `NhanVung.cs`: tên block/layer, chữ phòng), in `bo_khu_khong_bao= m2=`; tầng 8 bỏ 2 WC (10,7; 13 m²), hành lang không tách được phòng
(gộp với buồng thang, giới hạn dò phòng có từ trước); (c) dây đi vòng: ô lưới cách mép vùng ≤ 0,4 m phạt x4, tầng 8 dây từ x 36020 sang 36520
(cách mép lõi 0,55 m). Test: ca 7 + kiểm dây cách mép trong mục "Sua 6 loi" của `chay_thu.ps1`.
CÒN LẠI (chưa làm): (a) khoang vẽ tay từng polyline `PCCC_VUNG` (không qua `Dam`) vẫn dim từng khoang — không phân biệt được khoang với phòng;
(b) lọc đầu phun không tách được hai phòng cùng loại đầu phun sát nhau qua một tường; (e) độ rộng dầm lấy theo nét vẽ, chưa theo nhãn.

## VIỆC 16/11 TỐI (4) — ỐP MẶT BẰNG DẦM VÀO KIẾN TRÚC + VẼ KHOANG DẦM: XONG
`ket_qua/cad_test/2026-11-16/cong_trinh_drive/t8_mat_bang_dam.dwg`: mặt bằng tầng 8 + dầm chép nguyên nét từ file kết cấu (tờ "MẶT BẰNG DẦM TẦNG 7"), đổi layer
`AAP-TUONG`→`KC_DAM`, `AAP-TEXT`→`KC_DAM_CHU`, `AAP-TRUC`→`KC_TRUC`... (để không lẫn tường), dời khớp trục dx −1106627, dy −366924 (`scripts/tach_dam.lsp`,
chèn `-INSERT *kc_dam_t8.dwg`). 20 khoang K1–K20 polyline `PCCC_VUNG` theo mép trong dầm + chữ m² layer `PCCC_KHOANG_M2` (`scripts/tao_khoang.py`).
Dùng file này làm bản thử cho lỗi 4 bên dưới. Nhãn dầm của bộ kết cấu này ghi ngược `DS hXb` (`DS 400X250` = b 250, h 400, theo mặt cắt cùng tờ).
ĐÃ SỬA 16/11 (người dùng chốt): quy tắc vẫn là b x h (rộng x cao), nhưng `DocBxh` (Rai.cs) lấy số LỚN làm h nên đọc đúng cả nhãn ngược;
test `chay_thu.ps1` vùng C dùng nhãn `DS 700X300`. Giới hạn: dầm bẹt (rộng > cao) sẽ bị đọc thành dầm cao — gặp thì thêm lựa chọn.

## VIỆC 16/11 TỐI (2) — THIẾT KẾ LẠI TẦNG 8 BẰNG LỆNH GÕ TAY + HƯỚNG DẪN SỬ DỤNG: XONG
Người dùng muốn thấy plugin thiết kế bằng chính lệnh `PCCC_*`, báo cáo từng bước dùng lệnh nào, và file hướng dẫn dùng plugin.
Kịch bản lệnh chạy trong accoreconsole (`kb_bc2.txt`, `kb_spr2.txt`), kết quả `t8_bc_lenh.dwg`, `t8_spr_lenh.dwg`, `BAO_CAO_QUY_TRINH_LENH_PLUGIN.pdf`
(trong `ket_qua/cad_test/2026-11-16/cong_trinh_drive/`, gitignore). Hướng dẫn: `cad/HUONG_DAN_SU_DUNG.md` (repo) → `HUONG_DAN_SU_DUNG_PLUGIN.pdf`.
VIỆC ĐỂ LẠI (lỗi lộ ra khi chạy lệnh): (a) `PCCC_RAI` dò phòng theo khung chọn (`MatChon`) gộp lõi thang máy + sảnh vào văn phòng (293 m²) trong khi
`vung_tu_dong` ra đúng 269 m² — hai đường dò phải cho cùng kết quả; (b) `PCCC_ONG` mở PLINE bằng `SendStringToExecute` nên không chạy được trong
file .scr (toạ độ bị đọc thành lệnh); (c) `PCCC_TUYEN` chọn đầu phun bằng khung dễ dính phòng bên cạnh: nên lọc đầu phun theo vùng `PCCC_VUNG`.

## VIỆC 16/11 TỐI — THIẾT KẾ THỬ TẦNG 8 KOCHAM (BÁO CHÁY + SPRINKLER): XONG, CHỜ NGƯỜI DÙNG NHẬN XÉT ĐỂ TRAIN TIẾP
Người dùng gửi DWG Drive "TKTD KOCHAM FINAL 16-12.dwg" (hồ sơ thẩm duyệt điều chỉnh tầng 8 văn phòng, ~30 tờ), bảo xoá thiết bị PCCC, tự rải báo cháy,
chữa cháy, đi dây, đi ống, báo cáo "hiểu tới đâu". Kết quả ở `ket_qua/cad_test/2026-11-16/cong_trinh_drive/` (gitignore, hồ sơ khách): gốc `goc/`,
mặt bằng sạch `t8_mat_bang.dwg`, `t8_bao_chay.dwg`, `t8_sprinkler.dwg`, spec `skills/b4_bao_chay.json`, `skills/b5_sprinkler.json`, báo cáo `bao_cao_thiet_ke_thu_t8.pdf`.
- Mặt bằng mọi tờ PCCC là một block "2333" chèn lệch xa tờ (dò bằng LISP `ssget "C"` trong accoreconsole); tách bằng xoá phần còn lại + EXPLODE + MOVE.
- Báo cháy: 8 đầu khói (gốc 18: gốc vẽ khung từng đầu, nghi khoang dầm), tổ hợp, EOL, dây 47 m. Sprinkler nhóm 1: 31 đầu (= gốc 28 + 3), ống DN65/50/32/25.
VIỆC ĐỂ TRAIN (điểm yếu plugin lộ ra, chưa sửa): (a) `vung_tu_dong` gộp hành lang với buồng thang; (b) `rai` vùng chữ L thêm đầu "phủ kín" ngay trên tường
(sprinkler không có cách tường tối thiểu); (c) `tuyen noi_tiep` đi chéo cắt lõi thang, chưa né tường; (d) chưa đọc mặt bằng dầm để chia khoang đầu báo;
(e) `bang ky_hieu` sprinkler dư cột "KÝ HIỆU" trống; nhãn DN đè ký hiệu đầu phun. Chờ người dùng trả lời: cao độ trần, dầm, hộp kỹ thuật thật, WC có đầu báo không.

## VIỆC 16/11 ĐÊM — RẢI NHANH, BỎ PHÒNG RÁC, THEO VÙNG VẼ SẴN, DIM THEO MẪU: XONG MÃ + TEST, CHƯA XEM TRONG AUTOCAD
Gốc: người dùng dán lỗi `PCCC_RAI` (mặt 763 079 m², "bước quá nhỏ"), mặt bằng phải rải đứng máy, "vừa dở vừa lag"; gửi ảnh mẫu dim (`ket_qua/cad_test/2026-11-16/dim_mau.png`).
1. Nhanh (`NhanVung.cs` `CacMat`): dựng phòng block "Factory 1A4- 1st" 74 s → 2,9–4 s, giữ 57 phòng / 112 đầu như cũ. Đo từng bước: nối khe thẳng hàng ≤ 3 m
   nối MỌI đoạn cùng hàng (không chỉ đoạn gần nhất) sinh cầu chồng nhau, 23 triệu điểm cắt; HashSet<long> khoá cặp đỉnh băm hi ^ lo trùng hàng loạt
   (bước đỉnh 15 s, đi mặt 34–51 s); bước khe chữ T có cửa quét cả 30 nghìn đoạn mỗi đầu (15–19 s). Sửa: chỉ nối khe gần nhất ở cả hai đầu,
   bộ băm `KhoaLong`, lưới ô `LuoiDoan` cho bước 1b.
2. "Dở" (`Rai.cs` nhánh Enter mọi phòng + `NhanVung.cs`): (a) quét chọn cả block tờ thì nhận cả phòng trong hình chi tiết, mặt cắt (7 cụm, 18 phòng ngoài mặt bằng):
   `CumChinh` bỏ cụm (hộp bao cách ≤ 2 m) có tổng diện tích < 20 % cụm lớn nhất, báo `bo_cum=`; (b) polyline PCCC_VUNG người dùng vẽ sẵn (nét xanh hướng dẫn) không
   được dùng, plugin dựng phòng rồi vẽ chồng: `TheoVungCoSan` vùng vẽ sẵn nằm trong một phòng tự dựng (≤ 1,3 lần diện tích phòng) thay phòng đó, không vẽ lại,
   báo `theo_vung_co_san=`. Mặt bằng phải còn 39 phòng / 91 đầu, không còn đầu rơi ngoài mặt bằng.
3. Dim (`KtLuoi`, `Rai.cs`) theo mẫu người dùng: MỘT chuỗi ngang dọc hàng đông nhất (trên cùng), MỘT chuỗi đứng dọc cột đông nhất (gần giữa vùng), đường dim
   cách 2 x cao (sát thiết bị, trước đặt ngoài mép vùng); hàng / cột lệch lưới ≥ 2 thiết bị (so le) một chuỗi cho cả hàng; thiết bị lẻ: tường gần – thiết bị – lưới gần nhất.
Test mới `chay_thu.ps1`: cụm chi tiết + vùng vẽ sẵn (2 phòng x 4 đầu, 6 dim/phòng); thời gian dựng phòng trên `file_cad_test\test báo cháy.dwg` < 10 s (có file mới chạy).
`chay_thu.ps1`, `thu_mcp.ps1` TAT CA DAT; bundle đã build.
CHƯA: người dùng tắt hẳn AutoCAD mở lại để nạp bản mới, thử lại mặt bằng phải; chữ dim chuỗi đứng có thể đè chữ chuỗi ngang chỗ hai chuỗi cắt nhau (chưa né chữ);
ngưỡng cụm 2 m / 20 % cố định (nhà nhỏ tách rời bị bỏ thì bấm điểm trong phòng). Bước nối khe (`noi`) còn ~1,3 s, chưa tối ưu.

## VIỆC 16/11 (phiên nhánh, đã gộp vào main) — THỐNG KÊ BLOCK ĐỘNG THEO VISIBILITY: XONG MÃ + TEST, CHƯA XEM TRONG AUTOCAD
Học tư duy từ LHBBlockScheduler v9.7 (chỉ đọc tài liệu, không có mã nguồn, không chạy DLL): "chủng loại" = trạng thái Visibility; không có Visibility thì
tham số chữ có danh sách (Lookup); độ dài, góc, lật không làm chủng loại. Đã làm (`module/plugin/Lenh.cs`, `Spec.cs`):
- `ChungLoai(br)`: Visibility (tên `Visibility*`) > Lookup chữ. `PCCC_BOC` thêm dòng `+ chủng loại "..."` dưới mã; tổng theo mã KHÔNG đổi (web, sơ đồ, PCCC_BLOCK đọc như cũ).
- `PCCC_BANG`: block động mỗi chủng loại một dòng, thêm cột "Chủng loại" (chỉ khi có block động); dòng lệnh `[TKPCCC] PCCC_BANG dong= chung_loai=`.
- Gán mã riêng từng trạng thái: `block_alias.json` khoá `"tên block|trạng thái"`; `PCCC_GAN` chọn block động thì hỏi Rieng/Chung. Spec `dem` thêm `dem_loai`.
- Test: `test/ThuBlockDong.cs` (DLL chỉ cho test) chèn block động từ bản vẽ mẫu Autodesk `Sample\en-us\Dynamic Blocks` (không đưa bản vẽ khách vào repo).
Việc sau (chưa làm):
1. Web báo giá / Excel chưa có cột chủng loại: JSON `PCCC_BOC` gửi web vẫn chỉ `{ma, sl}` (server `sach` lọc trường lạ). Thêm `chung_loai` vào `dong` + cột Excel
   là việc của module web (`backend/src/server.ts`, xuất báo giá).
2. Tách theo kích thước (tham số độ dài) như ô "Tách theo kích thước" của LHB: chưa làm, mặc định không tách là đúng với đầu báo tia chiếu.
3. Ký hiệu trong ô bảng của dòng chủng loại vẫn là block thư viện theo mã (trạng thái mặc định), chưa vẽ đúng hình trạng thái.
4. Thử `PCCC_GAN` (Rieng/Chung) và `PCCC_BANG` trong AutoCAD thật trên bản chép `file_cad_test/BEAM_Quyetthang.dwg` (BÌNH CHỮA CHÁY 4 trạng thái, ĐÈN EXIT 5 trạng thái).

## VIỆC 16/11 TỐI (TIẾP) — GHI m² KHOANG + ỐNG NHÁNH TỰ CHỌN DN: XONG MÃ + TEST, CHƯA XEM TRONG AUTOCAD
1. `PCCC_RAI` khoang dầm (Dam, đầu khói): ghi "x,x m²" dưới tâm mỗi khoang, dấu X hồng ở khoang dưới ngưỡng 4 / 9 m², khoang < 1 m² bỏ ghi; layer `PCCC_KHOANG_M2`;
   tắt ở tab Khoang dầm (khoá `kd_ghi_m2`). Dòng lệnh `ghi_khoang= dau_x= bo_nho_1m2=`.
2. `PCCC_TUYEN` gõ `TuDong` thay mã ống: hỏi mức áp suất Den05 (đến 0,5 MPa, mặc định) / Tren05; ống góp cắt tại từng hàng, nhánh cắt tại từng đầu phun,
   mỗi đoạn DN nhỏ nhất có "số đầu tối đa" ≥ số đầu phun phía sau (bảng người dùng gửi 16/11: `data/ong_nhanh_dn.json`; DN70 = ONG_DN65; ONG_DN20 chưa có
   trong vat_tu.json nên lấy DN25 và báo); chữ DN ở đoạn đầu mỗi tuyến và chỗ đổi cỡ (layer PCCC_NHAN). Thẻ "DN≤", "DN>" ở trang Chữa cháy.
Test mới trong `chay_thu.ps1` (phòng 4 x 4 đầu: 20 đoạn, DN25 = 8, DN32 = 4, DN40 = 5, DN50 = 1, DN65 = 2) đạt, cả bộ TAT CA DAT.
CHƯA: in ảnh xem chữ DN, chữ m² đặt có đẹp không (dừng ở 81 % hạn mức 5 giờ); thu_gui.ps1; spec MCP `tuyen` chưa có TuDong.

## VIỆC 16/11 TỐI — GIAO DIỆN MỚI THEO 3 VIDEO PLUGIN (VPTTOOL, HTS_DESIGN): XONG MÃ + TEST, CHƯA XEM TRONG AUTOCAD
Người dùng gửi 3 video TikTok, dặn: lấy tư duy hay, nhanh, tự động, không phụ thuộc AI, giao diện đẹp như mẫu. Phân tích + việc chưa làm: `cad/skills/mau/hoc-video-plugin-16-11.md`.
Đã làm (chỉ `module/ui/` + 1 hàm đọc ở `DayBc.cs`): thanh dọc thành ô biểu tượng màu; widget `NutIcon`, `ThanhTab`, `TieuDeNhom`, `KhungTrangThai` (`GiaoDien.cs`);
trang Báo cháy 4 tab RẢI ĐẦU BÁO · KHOANG DẦM (ranh giới, Tra cứu TCVN, h trần, rải `PCCC_RAI BC_KHOI Dam h`) · KIỂM TRA · ĐI DÂY, khung TRẠNG THÁI cuối mỗi tab
(`Lenh.DemVung` đếm PCCC_VUNG / PCCC_LOAITRU); lưới Tiện ích / Chữa cháy / Thống kê: tab viên thuốc + vòng tròn màu ghi mã tắt, palette hẹp thì 1 cột;
`BangTraCuu.cs` (bảng 5.9.1.1.6 đọc từ `skills/MOD_dau_bao_khoi_khoang_dam.md`, build.bat chép cạnh DLL).
Xem thử ngoài AutoCAD: dựng exe giả lập lớp AutoCAD (scratchpad, không commit), chụp tab tối + sáng: đạt. `chay_thu.ps1`, `thu_mcp.ps1` TAT CA DAT, bundle đã build.
CHƯA: `thu_gui.ps1` (AutoCAD đang mở bản vẽ của người dùng); người dùng tắt hẳn AutoCAD mở lại để nạp, xem tận mắt.
VIỆC KẾ (chờ người dùng chọn, xem mục 3 file kiến thức): 1 ghi m² từng khoang + đánh dấu ô nhỏ + bấm tay đặt giữa dầm; 2 ống nhánh tự chọn DN theo số đầu phun
(cần người dùng chốt bảng DN); 3 bước nhảy 100 mm + rải so le; 5 rải bằng block mẫu lấy trên bản vẽ.

## VIỆC 16/11 CHIỀU — SỬA "NHẬN SAI VÙNG, RẢI LINH TINH" (ảnh người dùng trên `test báo cháy.dwg`): XONG MÃ + TEST
Nguyên nhân 1: file để INSUNITS 1 (inch) mà vẽ mm → plugin coi 1 m = 39 đơn vị (khe cửa không khép, lòng tường thành phòng, bước 0,4 m): 8 837 đầu trong 684 "phòng".
Sửa: `KiemDonVi` (`Lenh.cs`, gọi đầu `PCCC_RAI`, `PCCC_DAYBC`): khổ EXTMIN–EXTMAX theo INSUNITS > 2 km (hoặc < 2 m) mà theo mm (m) ra 5 m – 2 km thì đặt lại INSUNITS, báo `don_vi_doi=`.
Nguyên nhân 2: vùng xanh lá bên phải (PCCC_VUNG) vẽ về đúng điểm đầu nhưng Closed = 0 → không được coi là vùng. Sửa: `KinPl` (`Rai.cs`) cho mọi chỗ đọc vùng.
Chạy lại trên bản chép file mẫu: rải đúng 2 vùng xanh, mỗi vùng 15 đầu, lưới 3 × 5 bước 7,9 × 9,4 m (ảnh `ket_qua/cad_test/2026-11-16/rai_xanh.png`).
`chay_thu.ps1` 147/147, `thu_mcp.ps1` đạt, bundle build lại. CHƯA: người dùng tắt hẳn AutoCAD mở lại để nạp, thử lại trên máy.

## VIỆC 16/11 — 5 YÊU CẦU BÁO CHÁY (người dùng gửi `file_cad_test\test báo cháy.dwg`: trái hệ địa chỉ, phải hệ thường): XONG MÃ + TEST
Bản chép, dump, ảnh ở `ket_qua/cad_test/2026-11-16/` (gitignore). Kiến thức đọc từ mẫu: `cad/skills/ve/bao_chay/day_zone.md`.
1. Phân vùng: vùng tự dựng bám khấc cột, cung cửa (44–53 đỉnh) → `LamGon` (`NhanVung.cs`, ngưỡng 1 m): bỏ khấc nhỏ ra đường ngoài, giữ khấc lớn (lõi thang);
   đa giác của mẫu ra 8 đỉnh, 1 029 m² (vùng anh vẽ tay 1 041 m²). Áp cho `PCCC_RAI` và `vung_tu_dong`.
2. Dim: `PCCC_RAI` tự dim sau khi rải (`KtLuoi`, `KtSauRai` trong `Rai.cs`): mọi cột một chuỗi X, mọi hàng một chuỗi Y, hai đầu tới mép vùng; spec `rai` `kt` dùng chung.
3. Trang Cài đặt: kiểu dim (DimStyle bản vẽ hoặc PCCC_KT), cách dim (chuỗi / chuỗi + chéo góc / không), layer dim; lưu `cai_dat.json` qua `CaiDat.cs` (plugin + palette dùng chung).
4. `PCCC_DAYBC` (`DayBc.cs`): hệ thường một đường vuông góc, điện trở cuối ở thiết bị xa tủ nhất, chuông xương cá; hệ địa chỉ mạch vòng tủ → module / thiết bị địa chỉ → tủ,
   đầu báo + nút nhấn thường về ZM gần nhất, chuông thường về CM, chuông địa chỉ về R2ML. Nhận block lạ theo chữ (ZM, CM, RM/R2ML, NUT NHAN, CHUONG). Mã mới trong
   `data/vat_tu.json`: BC_KHOI_DC, BC_NHIET_DC, BC_NUTAN_DC, BC_CHUONG_DC, BC_ZM, BC_CM, BC_R2ML. Chạy trên bản chép file mẫu: 0 đoạn xéo (ảnh `ket_qua/cad_test/2026-11-16/daybc.png`).
5. Bảng layer dây / ống ở trang Cài đặt (thêm, xoá, lấy layer có sẵn trong bản vẽ, Lưu); trang Báo cháy mục 6 chọn dây cho 9 loại thiết bị + nút ĐI DÂY.
Kèm: `PCCC_DAT` gõ đúng cả tên ("dau bao nhiet") nhận mã đó dù tên khác chứa nó. `chay_thu.ps1` 145/145, `thu_mcp.ps1`, `npm test` 68/68 đạt; `build.bat` đã build vào bundle.
CHƯA: `thu_gui.ps1` (AutoCAD đang mở lúc làm, không chạy được), xem tận mắt trang Cài đặt + Báo cháy mục 6 trong AutoCAD; dây khác layer còn chồng nhau trên cùng đoạn
(chưa dời song song); mạch vòng địa chỉ đi đường ngắn, chưa bám chu vi như mẫu. File mẫu để INSUNITS 1 (inch) mà vẽ theo mm: đặt INSUNITS 4 trước khi chạy plugin.
VIỆC KẾ: chờ người dùng thử, báo lỗi thì sửa theo bảng "Sửa gì vào đâu" của `cad/README.md`.

## VIỆC 15/11 — LUẬT DẦM (người dùng bổ sung): XONG
Khoang dầm phải xét cả độ sâu dầm d (b x h) lẫn chiều cao trần h (5.9.1.1.6). `PCCC_RAI` từ khoá Dam: bấm nét DẦM mẫu (tách khỏi nét tường, `NenVung.BoMau`);
d lấy từ ký hiệu "b x h" (vd `B1 (400x600)`, chữ trong block cũng đọc) gần nhất trong 10 m; không có ký hiệu thì hỏi một cỡ cho cả mặt bằng hoặc `TungDam`
(chọn line dọc dầm rồi nhập b x h từng nhóm); dầm d ≤ 0,3 m bỏ (trần phẳng, khoản a); dầm không ký hiệu gần, không cỡ chung: coi là sâu.
Test `chay_thu.ps1`: 3 phòng có ký hiệu (sâu / nông / lưới nhỏ), 1 phòng hỏi cỡ chung, 1 phòng TungDam: TAT CA DAT.

## VIỆC 15/11 RẠNG SÁNG — 7 VIỆC PLUGIN CÒN THIẾU SO VỚI BẢN VẼ MẪU: XONG (người dùng chọn làm hết; dặn tới 95 % ghi tiến độ, không push, đợi reset 5:00 tự làm tiếp)
1–2 `PCCC_KYHIEU`, `PCCC_QUYDINH` + spec `bang` (`module/plugin/BangMb.cs`); 3 dim bước tia beam (`KtTia`, layer `PCCC_BC_TIA_KT`, thông báo `kt=`);
4 `PCCC_DAY` hỏi tên zone (nhãn "VỀ TỦ TTBC - ZONE n" + diện tích vùng PCCC_VUNG), điện trở cuối dây ở mút mạch hở dây báo cháy;
5 `PCCC_RAI` từ khoá Dam với đầu khói: khoang ≥ 9 m² (trần ≥ 4 m) / 4 m² (2–4 m) rải từng khoang, khoang nhỏ gom rải như trần phẳng (5.9.1.1.6 c–f);
6 `PCCC_THOATNAN` (hatch ANSI31 layer PCCC_THOAT_NAN; luật gạch chéo hiện hữu bỏ qua layer PCCC_*), `PCCC_VUNGBV` mã hệ HVT: vòng nét đứt in được;
7 `PCCC_THUYETMINH` (văn mẫu `cad/skills/ve/mau-thuyet-minh-pccc.md`, build.bat chép cạnh DLL). `chay_thu.ps1`, `thu_mcp.ps1` TAT CA DAT.
CHƯA: thu_gui (không đổi giao diện), nút trên palette cho 4 lệnh mới (đang chỉ có ở trang Lệnh); xem tận mắt trong AutoCAD. VIỆC KẾ: chờ người dùng.

## VIỆC 14/11 KHUYA — HỌC BỘ BẢN VẼ PCCC MẪU (DWG): XONG
Người dùng đưa `file_cad_test/` (đã thêm vào `.gitignore`) + thư mục hồ sơ một xưởng trên Drive; dặn KHÔNG đụng file gốc, cần thì save as riêng.
Đã chép sang `ket_qua/cad_test/hoc_mau/` (gitignore), dump bằng `dump2.lsp` (MTEXT đủ đoạn mã 3), in PNG vài tờ. Kết quả:
`cad/skills/mau/hoc-ban-ve-mau-dwg.md` (bộ tờ, quy ước, beam, zone, sprinkler; mục 6 = 7 việc plugin còn thiếu so với mẫu) và
`cad/skills/ve/mau-thuyet-minh-pccc.md` (27 đoạn thuyết minh chép nguyên văn, đã bỏ tên khách, địa chỉ, số GCN). VIỆC KẾ: chờ người dùng chọn làm việc nào ở mục 6.

## VIỆC 14/11 ĐÊM — RẢI THỬ BẢN VẼ THẬT "BEAM QUYẾT THẮNG.dwg" + THUYẾT MINH: XONG 19h40, ĐANG CHỜ NGƯỜI DÙNG DUYỆT (không làm thêm)
KẾT QUẢ (ket_qua/cad_test/2026-11-14/quyet_thang/, gitignore): `BEAM_QUYET_THANG_rai.dwg`, `thuyet_minh.md`, `rai_ket_qua.png` đã gửi người dùng.
5 bộ `BC_BEAM` (xưởng 1: 3 tia, xưởng 2: 2 tia) + 27 `BC_KHOI`, 7 zone (5.7.2.1: tách mái che dọc, phòng bơm, trạm vì không có lối vào từ trong nhà), `PCCC_KT` mù 0 %.
Rải trên tờ "THUYẾT MINH CÔNG TRÌNH" (block `A$Cbcabb6d6` chèn tại 1076601.6, 218732.6); kịch bản `final.scr` (tạo bằng python, điểm bấm phòng ở `phong_diem.json`).
Luật mới trong `NhanVung.cs` (test `chay_thu.ps1` ca 4300000–4477000): ô X → `TachOX` (bỏ 2 chéo, vùng loại trừ, `o_x=`);
hatch ANSI31–36 / User 45° → `gach_cheo=`, Enter mọi phòng thì bỏ vùng (`bo_phong=`); khe 3–12 m (`HO_CUA_MAX_M`) khép khi có ký hiệu cửa
(`GomCua`: cung 60–120° r 0,4–3 m, block tên/layer CUA/DOOR) hoặc tường 2 nét cùng hở (`SongDoi`); khe chữ T có cửa (tia từ đầu tường); chữ T / góc L hụt 2–10 mm tự nối.
Bài học chạy accoreconsole trên bản vẽ khách: PICKBOX mặc định 13 px ≈ 1,8 m ở zoom cả tờ → bấm nét mẫu trượt sang nét khác; đặt `PICKBOX 2` rồi trả `13`;
bấm nét mẫu xa thiết bị / tia vừa vẽ; SAVEAS tên đã có thì hỏi ghi đè, treo kịch bản (xoá file trước).
VIỆC KẾ: chờ người dùng xem `thuyet_minh.md` mục 5 (7 điểm cần duyệt), chốt hoặc tự sửa; KHÔNG tự làm thêm.
File gốc (hồ sơ khách, KHÔNG commit, KHÔNG sửa file gốc): `D:\1.DRIVE CÔNG TY\My Drive\1.PHÒNG THIẾT KẾ\1. HỒ SƠ THẨM DUYỆT\10. Năm 2026\1. Xưởng\XƯỞNG BAO BÌ QUYẾT THẮNG\BEAM QUYẾT THẮNG.dwg`.
Làm trên bản chép ở `ket_qua/cad_test/2026-11-14/quyet_thang/` (gitignore), chạy accoreconsole, in PNG kiểm. Cả tờ là MỘT block (không có nét rời).
Màu nét: xanh lá = nhà văn phòng, phòng 2; xanh dương = 2 xưởng, mái che; hồng = cửa; đỏ đứt = ranh đất; gạch chéo = công trình hiện hữu.
Người dùng chốt (14/11 đêm):
1. Ô có dấu X (hai đường chéo) = LỖ THÔNG TẦNG: không bố trí → plugin nhận ô X (2 đoạn cắt nhau gần trung điểm, 4 đầu mút là 4 góc) thành vùng loại trừ, bỏ 2 đường chéo khỏi dựng phòng.
2. Mái che không công năng: VẪN RẢI (người dùng tự xoá nếu thấy không ổn).
3. Công trình hiện hữu (gạch chéo): KHÔNG rải.
4. CỬA: người dùng chốt (14/11 đêm, sau khi nghe giải thích ngưỡng `HO_CUA_M` 3 m) "cứ có cửa mình khép lại 1 vùng": khe tường có ký hiệu cửa (cung cánh,
   block cửa, kể cả cửa cuốn xưởng > 3 m) thì khép vùng qua khe cửa, không phụ thuộc bề rộng; khe không có cửa vẫn theo ngưỡng 3 m.
   XONG việc rải + thuyết minh thì DỪNG, chờ người dùng xem, chốt duyệt hoặc tự chỉnh tiếp; không làm thêm việc khác.
5. Phân vùng: 2 xưởng riêng (tách bởi lỗ thông tầng); có tường ngăn cháy thì tách; dính nhau thì rải chung; phòng cách bởi tường / vách → vùng riêng,
   gom zone báo cháy theo TCVN 7568-14:2025 5.7.2.1 (người dùng gửi ảnh): zone ≤ 2000 m², kích thước ≤ 100 m, một tầng; phòng không liền kề gom một zone
   khi lối vào cách nhau ≤ 10 m và nhìn thấy nhau; 5.7.2.3 không gian che kín ≤ 500 m² gộp được nếu tổng đầu báo ≤ 40; 5.7.2.5 một vùng phát hiện thuộc một vùng báo động.
6. Rải luôn + viết thuyết minh công trình (bảng phòng, m², loại đầu báo, số lượng, zone, căn cứ). Tên file "BEAM": xưởng cao dùng đầu báo tia chiếu `BC_BEAM` (vẽ tia), văn phòng đầu khói điểm.
Bước: (1) dump layer/màu/hình trong block bằng LISP; (2) thêm luật ô X (NhanVung.cs + test); (3) PCCC_RAI trên bản chép (bấm nét mẫu xanh lá + xanh dương);
(4) gom zone, viết `thuyet_minh.md` ở thư mục kết quả, gửi người dùng; (5) ghi file này + LICH_SU, commit, push.
(Lịch sử) DỞ Ở ĐÂU (14/11 ~16h30, hạn mức 93 %, dừng theo luật; 19h03 đã làm tiếp xong — xem KẾT QUẢ ở trên):
- Đã chép file gốc thành `ket_qua/cad_test/2026-11-14/quyet_thang/goc.dwg`; `dump.lsp` + `dump.scr` (accoreconsole /i goc.dwg /s dump.scr) ghi `dump.txt`:
  dòng `L|layer|màu|nét`, `M|...` đối tượng model space, `B:<block>|...` nội dung block (kiểu|layer|màu|truecolor|nét|tên block|p10|p11|...).
  Tóm tắt bằng python đọc mã `mbcs`. INSUNITS 4 (mm). Model space 5 500 đối tượng (1 607 sprinkler `*U313`, 197 đầu khói, ống DN…), 99 block; bản vẽ nhiều tờ.
- Chữ "XƯỞNG 1", "XƯỞNG 2" của ảnh người dùng: MTEXT model space màu 9 tại (1583239, -126826), (1552172, -124044).
- Ứng viên block nền (ảnh: nét xanh dương + xanh lá): `A$Cbcabb6d6` (243 đối tượng: màu 7: 67, xanh lá 3: 62, 8: 36, 9: 13, 5: 10), chèn 5 lần trên layer
  `0.10-140` (màu 140 xanh dương: nét layer 0 ByLayer trong block ăn màu này), ví dụ điểm chèn (1080015, -559902), (1076602, -176384). CHƯA xác nhận lần chèn nào
  phủ vùng ảnh (tính khung bao block ra số lạ, có thể block chứa đối tượng ở xa).
- BƯỚC KẾ: in PNG cửa sổ quanh chữ XƯỞNG bằng PCCC_SPEC `{"ops":[{"op":"anh","hop":[1480000,-200000,1680000,-50000],"rong":1600}]}` trong accoreconsole
  (NETLOAD bundle\Contents\TKPCCC.Cad.dll), xem ảnh, xác định block + 2 điểm nằm trên nét xanh lá, xanh dương (bấm nét mẫu); rồi luật ô X, PCCC_RAI, zone, thuyết minh.

## VIỆC 14/11 TỐI — HỢP NHẤT ADDIN MẪU (video + spec người dùng): ĐANG LÀM, danh sách việc `cad/tien_do/viec_video_addin.md`
Người dùng dặn: làm HẾT theo spec `PROMPT_HOP_NHAT_PLUGIN_PCCC_CAD.md` (ngoài repo, đường dẫn trong file việc), không tra TCVN 7568 (trị số đã có trong
`data/khoang_cach_rai.json`), KHÔNG review giữa chừng: xong toàn bộ người dùng tự cho Fable kiểm. Đợt 1 xong (trang Báo cháy `PCCC_BOTRI`, `PCCC_RAI` HeSo/Dam/XemTruoc,
BC_BEAM vẽ tia, `PCCC_DAY` dây loop, cự ly 45 m). Đợt 2 xong (trang Tiện ích `PCCC_TIENICH` 27 thẻ, 8 lệnh mới `plugin/TienIch.cs`): `chay_thu.ps1` 109/109,
`thu_gui.ps1` TAT CA DAT. Đợt 3 xong (vùng loại trừ `PCCC_LOAITRU`, xem trước vẽ tạm, `PCCC_KT` vùng mù, Bố trí nhanh, preset): `chay_thu.ps1` 113/113.
Đợt 4 xong (trang con ADIM 5 bước, `PCCC_ADIM` lọc Mline): `chay_thu.ps1` 114/114, `thu_mcp`, `thu_gui` đạt.
Luật hạn mức (người dùng dặn 14/11): tới 95 % thì ghi 2 file này + push; 19h reset thì TỰ làm tiếp, toàn quyền. Đợt 5 xong (thanh dọc 9 phân hệ, thêm Chữa cháy, Thống kê; Bố trí nhanh đa giác đều). Sau đó: TZ `PCCC_TACHVUNG`, MZ `PCCC_KHUVUC`, vòng tròn PCCC_VUNG / PCCC_LOAITRU làm vùng (Bố trí nhanh "Tròn"): `chay_thu.ps1` 117/117,
`thu_gui` đạt. Thêm: trang ADIM bấm trước 2 mép tường (`PCCC_ADIMMOC`, hiện toạ độ), trang Báo cháy hiện kết quả kiểm vùng mù: 118/118.
Bước "Công năng" của trang Báo cháy đọc `knowledge/chon-loai-dau-bao.md`; ống gió (layer ONG_GIO / DUCT) dịch đầu báo: `chay_thu.ps1` 119/119.
ĐÃ XONG HẾT danh sách spec làm được (file việc, mục "Còn lại" chỉ còn việc của người dùng). VIỆC KẾ: người dùng mở AutoCAD xem, cho Fable kiểm toàn dự án;
lỗi họ báo thì sửa theo `cad/README.md` bảng "Sửa gì vào đâu". Người dùng tự mở AutoCAD xem và cho Fable kiểm.

## VIỆC 14/11 CHIỀU — PCCC_RAI TREO CAD: XONG MÃ + TEST (chi tiết `cad/LICH_SU.md` 14/11 chiều)
`PCCC_RAI` mới: mã → (trần | S, m², cách tường) → theo hàng → QUÉT CHỌN mặt bằng → bấm NÉT PHÂN VÙNG MẪU (mỗi màu một lần, bấm được nét trong block; Enter = tự nhận từ tường)
→ "Bấm điểm trong phòng cần rải, Enter = mọi phòng [TatCa]" → vẽ vùng `PCCC_VUNG` rồi rải. Polyline kín layer `PCCC_VUNG` trong vùng chọn dùng thẳng.
Hết treo: chỉ đọc đối tượng đã chọn, dựng mặt một lần (`MatChon`, `CacMat`), bỏ `VungBam` + `TraceBoundary`. Enter = mọi phòng (`PhongThat`): bỏ mặt 2A/P < 1 m hoặc < 1 m², bỏ mặt CHỨA phòng khác
(khung giấy, vòng tường ngoài; sảnh có lõi thang ở giữa thì bấm điểm). `chay_thu.ps1` 96/96 (thêm: bản vẽ 1 block + nét xanh/đỏ ra đúng 3 vùng; khung giấy chứa 2 phòng ra 2 phòng),
`thu_mcp.ps1` TAT CA DAT, `build.bat` đã build vào bundle. Đổi model 14/11: `cad-ve` Sonnet, Gemini mặc định `gemini-4-argon-medium`.
CÒN: người dùng tắt hẳn AutoCAD, mở lại, thử trên `PCCC HOÁ CHẤT.dwg` và bản vẽ xưởng bao bì (ảnh `ket_qua/cad_test/2026-11-14/`); chưa biết nét xanh/đỏ thật là màu ACI hay
true color, ByLayer hay đặt riêng (khoá so khớp = layer + màu thật). Mẫu chỉ lấy nét liền (nét đứt đỏ ranh đất bị bỏ), không lấy hatch. Review Opus max chưa làm.

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
6. XONG 14/11 14:35 (người dùng yêu cầu): gõ `?` ở dấu nhắc mã (`PCCC_RAI`, `PCCC_DAT`, `PCCC_TUYEN`, `PCCC_VUNGBV`, `PCCC_GAN`) mở bảng chọn
   `ui/BangChonVatTu.cs` (ký hiệu, tên, mã, tìm không dấu, Enter/bấm đúp chọn) thay cho in dòng lệnh; gõ chữ khớp nhiều mã cũng mở bảng lọc sẵn.
   `thu_gui.ps1` 19/19 trên AutoCAD thật (kịch bản `PCCC_DAT ?`: 43 dòng, lọc 'dau bao khoi' 3 dòng, bấm Chọn đặt 2 BC_KHOI); `chay_thu.ps1` 96/96, `thu_mcp.ps1` đạt.
   Bài học test: gửi lệnh qua COM chập chờn (`RPC_E_CALL_REJECTED`); UIA `RootElement` không thấy hộp thoại modal mở từ kịch bản, phải `EnumWindows` + `WM_SETTEXT`/`BM_CLICK`;
   phím tắt (`PHX`) gửi lệnh bất đồng bộ nên để cuối kịch bản. CÒN: review Opus max cùng mốc 14/11.
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
  (người dùng dặn: quét tên bản vẽ trước, cần gì thì chỉ đọc đúng trang đó). Đã đọc trang 11 (nguyên lý báo cháy) và 22 (không gian sprinkler), ghi ở `cad/skills/mau/mau-so-do-that.md`.
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

### Bước 6. Việc ưu tiên 2 và 3 từ `skills/mau/tham-khao-mcp-hub.md` (chi tiết từng việc ở file đó)
- Ưu tiên 2: bảng layer `data/ky_hieu/layer.json` cho `DamBaoLayer` (màu, kiểu đường, độ dày; dây báo cháy ra nét đứt);
  thao tác `dan` (mũi tên + chữ); thao tác `in_pdf`, `luu` (chung đường `-PLOT` với `cad_anh`).
- Ưu tiên 3, sau Bước 2–4: `doc` thêm `gi`:"block"; `cung`, `hatch` khi vẽ lại nền cần tới; `TKPCCC.Mcp.exe` tự `NETLOAD` qua COM + thời gian chờ bằng biến môi trường;
  bộ ký hiệu van của bản mẫu khi làm `so_do`; đoạn cấu hình mẫu cho AI khác và 4–5 câu lệnh mẫu PCCC trong `cad/README.md`.
- Đưa vào skill `tkpccc-ve-cad`: vẽ theo giai đoạn (nền + trục → rải → tuyến → ghi chú), hết mỗi giai đoạn gọi `cad_anh` và báo số đếm; chỉ dừng chờ duyệt khi người dùng yêu cầu.

## NẠP DỮ LIỆU NGƯỜI DÙNG QUA PLUGIN (người dùng dặn 2026-11-12, thay mục "nhắc đầu phiên" của 11/11)
Chuẩn layer, block động thiết bị, catalog ống + phụ kiện, catalog bơm: người dùng KHÔNG gửi file cho Claude; sau này plugin có chức năng để họ tự thêm vào trong CAD.
Không cần nhắc đầu phiên nữa. Việc của Claude: làm các lệnh nạp đó (block đã có `PCCC_THEMBLOCK`, cần kiểm với block động thật; còn thiếu lệnh nạp chuẩn layer, catalog ống + phụ kiện, catalog bơm),
xếp sau Bước 5 hoặc gộp vào G6 (bảng trong CAD). Trong lúc chưa có, plugin vẫn dùng layer và block mặc định.

## NGƯỜI DÙNG ĐÃ TRẢ LỜI 2026-11-11 (đầy đủ: `cad/skills/ve/tu-duy-di-day-di-ong.md`)
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
