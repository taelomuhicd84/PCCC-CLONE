# Việc 14/11: hợp nhất addin mẫu (video + spec) vào plugin TKPCCC

Nguồn (người dùng đưa 14/11, KHÔNG commit, ngoài repo): video `D:\$.4. QC - TCVN\8_TAILIEUHOCTAP\08_Video\video addin cad.mp4`,
spec người dùng viết `...\08_Video\PROMPT_HOP_NHAT_PLUGIN_PCCC_CAD.md` (8 phân hệ, 28 tiện ích, Báo cháy 5 bước, ADIM 5 bước).
Người dùng dặn: "cái gì hay thì copy, phát triển, kết hợp và tối ưu", "cứ làm hết", "đừng tra TCVN 7568, dùng file .md đã train";
xong toàn bộ người dùng tự cho Fable kiểm cả dự án (KHÔNG chạy review Opus giữa chừng).
Trị số thiết bị: `data/khoang_cach_rai.json` (khớp spec: SMK 10,2 / 5,1 / R 7,2; HEAT 7,2 / 3,6 / R 5,1; BEAM 14,4 / 7,2; MCP, đèn ≤ 45 m).

Nguyên tắc: tái dùng lệnh có sẵn (không viết trùng), lệnh AutoCAD có sẵn thì thẻ gửi thẳng lệnh đó; mọi xử lý bản vẽ ở `module/plugin/`
(thử được trong accoreconsole), `module/ui/` chỉ giao diện; mỗi lệnh mới 1 dòng `DS_LENH` + 1 ca `test/chay_thu.ps1`.

## Đợt 1 — XONG (commit 14/11 tối)
- [x] Trang Báo cháy (`PCCC_BOTRI`, `ui/BangBoTri.cs`): thiết bị theo json, trần, mức tối ưu k (3 ô KPI), dầm, chia theo hàng, BỐ TRÍ, Xem trước (đếm), kết quả lượt cuối.
- [x] `PCCC_RAI`: từ khoá `HeSo` (k), `Dam` (dòng có `dam` nhân bước), `XemTruoc` (chỉ đếm); `BC_BEAM` vẽ tia nối hai tường (`TiaVung`, `VeTia`).
- [x] `PCCC_DAY` (Vẽ dây Loop): nối tiếp rắn bò, điểm đầu tủ, `Vong` về điểm đầu.
- [x] Kiểm cự ly nút ấn, đèn ≤ 45 m = `PCCC_VUNGBV` R = d/2 (báo đứng lẻ); huy hiệu thẻ kèm phím tắt.

## Đợt 2 — Tiện ích: XONG 14/11 tối (`PCCC_TIENICH`, `ui/BangTienIch.cs` 27 thẻ, lưới 2 cột + nút lọc nhóm 1–6; lệnh mới ở `plugin/TienIch.cs`)
Lệnh có sẵn / AutoCAD có sẵn (thẻ gửi thẳng): SZ vùng tay (PLINE layer PCCC_VUNG) · SP nhận diện (PCCC_RAI quét chọn) · ADIM · CDIM · SL `LAYMCH` ·
AP `MEASURE` · CLT `TEXTALIGN` · CLN `-OVERKILL` + `-PURGE` · SET trang Cài đặt · DIR mở thư mục dữ liệu · STD mở `khoang_cach_rai.json`.
Lệnh mới cần viết (plugin): 
- [x] CKC `PCCC_TRUNG` kiểm thiết bị trùng toạ độ, khoanh đỏ
- [x] CKL / APL `PCCC_LOPCHUAN` kiểm (và sửa) thiết bị sai layer PCCC_<hệ>
- [x] AN `PCCC_DANHSO` đánh số thiết bị (tiền tố, số đầu, thứ tự X→Y / Y→X)
- [x] MR `PCCC_XOAY` xoay từng block quanh điểm chèn của nó
- [x] SCB `PCCC_TYLE` đổi tỉ lệ từng block quanh điểm chèn
- [x] SB `PCCC_DOIBLOCK` thay block giữ điểm, góc, tỉ lệ
- [x] AD `PCCC_CANDIM` dóng đường dim về một mốc
- [x] DN `PCCC_DOIDN` đổi cỡ ống (layer PCCC_ONG_DNxx)
- [x] RN `RENAME`, LM `LAYER`, PD `PCCC_ONG`, REP `PCCC_BOC`, BAK mở chỗ `cai_dat.json`, SP `BOUNDARY`
- [x] CKO = `PCCC_KT`
- [ ] TZ tách khu vực, MZ quản lý khu vực: chưa có

## Đợt 3 — Báo cháy còn lại: XONG 14/11 tối
- [x] Vùng loại trừ (lỗ thang, giếng trời): PCCC_RAI bỏ điểm nằm trong polyline loại trừ
- [x] Xem trước vẽ tạm: điểm + vòng R trên layer không in `PCCC_XEM_TRUOC`, xoá khi BỐ TRÍ / xem lại
- [x] Bố trí nhanh: RECTANG trên PCCC_VUNG rồi PCCC_RAI chọn L
- [x] PCKT `PCCC_KT`: vùng mù (điểm trong PCCC_VUNG xa mọi thiết bị > R), khoanh, % mù
- [x] Preset Lưu / Nạp (khoá `bt_presets` trong cai_dat.json)

## Đợt 4 — ADIM sub-panel 5 bước (UI gửi PCCC_ADIM điền sẵn: layer, lọc, phương)
## Đợt 5 — Thanh dọc phân hệ: Quy trình · Tiện ích · Báo cháy · Chữa cháy · Thống kê · Thư viện · Lệnh · Bot chat · Cài đặt
