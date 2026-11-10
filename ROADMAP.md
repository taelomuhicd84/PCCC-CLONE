# Roadmap TKPCCC

Kế hoạch gốc: `C:\Users\HP\.claude\plans\t-i-mu-n-thi-t-k-eager-giraffe.md`. Việc đang làm: `TIEN_DO.md`.

| Pha | Nội dung | Trạng thái |
|---|---|---|
| 1 | Thư viện block PCCC (attribute `MA_VT`) → vẽ thiết bị/ống → bóc khối lượng → báo giá Excel theo đơn giá người dùng | ✅ 2026-10-31 |
| 1+ | Từ LHB Block Scheduler (chỉ ý tưởng): không đếm trùng, block lồng/MINSERT, bỏ XREF, tách tầng/khu, `PCCC_GAN`, `PCCC_BANG`, `PCCC_VUNGBV` | ✅ 2026-10-31 |
| 2 | Web hướng dẫn thiết kế B0–B7 (tài liệu nguyên văn, hỏi Claude), báo cáo khảo sát 8 mục (Word, Claude gợi ý), báo giá sơ bộ theo m² | ✅ 2026-10-31 |
| 3 | Đọc PDF + thẩm định: lõi Python `thamdinh/` (chép từ repo `taelomuhicd84/PCCC` ab546c7, offline, 26 luật QCVN 06), tab web Thẩm định PDF (D/KD/XM + PDF ghi chú), "Claude xét" dòng XM | ✅ 2026-11-01 (chưa thử hồ sơ thật trên máy này) |
| 3+ | Máy kiểm luật đọc bảng cửa/thang (bớt XM "không đọc được") | ⏳ hỏi người dùng |
| 4a | Thủy lực sprinkler TCVN 7336 PL B: lõi `shared/src/thuy_luc.ts` (chép từ SKILL-PCCC-NEW, test khớp Excel), tab web Thủy lực (sơ bộ, so phương án, tính theo số liệu), xuất Excel mẫu `MAU_THUY_LUC_7336.xlsx` (sửa F106 theo B.2.10.12) | ✅ 2026-11-01 |
| 4b | Vẽ tự động trong CAD: `PCCC_RAI` rải đầu phun/đầu báo theo lưới trong polyline phòng (cần máy có AutoCAD để kiểm) | ⏳ |
| 4+ | Thủy lực mở rộng: kho nhóm 5–7 (Bảng 2), TCVN 14496 giá kệ, ô HCB2 cụm van trong Excel mẫu, "chọn số đầu C07" trên web, đọc số liệu ống từ bản vẽ | ⏳ hỏi người dùng |
| 5a | Hai luồng theo yêu cầu người dùng 2026-11-02 — (1) **khảo sát – tư vấn**: bảng KS + comment, phương án vẽ lên PDF; (2) **thẩm định**: XÂY DỰNG (bảng KT theo công năng + chiều cao) và HỆ THỐNG PCCC (bảng A theo công năng + B theo hệ thống), Kết luận từng dòng, comment callout/leader/khung/nét cho thiếu số liệu, nghi vấn, phương án điều chỉnh. Nền: module `tkpccc-bang-doi-chieu` (bảng A/B/KT chép SKILL-PCCC-NEW 24743f9 + KS; chọn bảng, lập, job, gộp, kiểm, Excel, markdown), `tkpccc-ghi-chu-ban-ve` (annotation thật, xử lý trang xoay, neo theo chữ, lưới tọa độ), skill điều phối `tkpccc-khao-sat-tu-van`, `tkpccc-tham-dinh`, agent `pccc-xay-dung`, `pccc-he-thong`. Sắp xếp lại: mỗi module một skill | ✅ 2026-11-02 (chưa chạy trọn trên hồ sơ thật) |
| 5b | Web: tab chạy luồng khảo sát/thẩm định qua runner (`claude -p`), tải PDF ghi chú + Excel; xuất Word theo mẫu bảng A/B chính thức (`data/mau/*.docx` của SKILL-PCCC-NEW); điền **mẫu báo cáo khảo sát của người dùng** từ `doi_chieu.json` (✅ 2026-11-02 phần Excel: lệnh `bcks`, mẫu mặc định `data/mau_bao_cao/BCKS_mau.xlsx`; còn đưa lên web); tách thêm agent theo chuyên đề (m01–m18 như SKILL-PCCC-NEW) nếu bảng KT quá dài | ⏳ |
| 6 | **Thiết kế PCCC trong AutoCAD qua MCP** (người dùng dặn 2026-11-02 chừa đường): máy chủ MCP (Node, stdio, thư mục `mcp/`) ↔ plugin lắng nghe 127.0.0.1 chạy lệnh trên luồng chính AutoCAD (`PCCC_DAT`, `PCCC_ONG`, `PCCC_BOC`…, thêm lệnh vẽ tường/cửa/rải đầu); quyết định thiết kế lấy từ skill + `knowledge/`; phương án đã vẽ trên PDF (`tuong`, `cua`, `net` của `ghi_chu`) là đầu vào chung, đổi tọa độ PDF → mm bằng 2 điểm hiệu chỉnh. Gộp với Pha 4b | 🔄 2026-11-10: xong MCP CAD (lệnh `PCCC_SPEC`, cầu nối hàng đợi file, `cad/bin/TKPCCC.Mcp.exe` bằng C# thay cho Node + cổng 127.0.0.1); phần còn lại theo `cad/KE_HOACH.md`, tiến độ `cad/TIEN_DO.md` |
| — | Repo tham khảo `SKILL-PCCC-NEW`, `PcccVipPro`, `PCCC` (người dùng mở công khai 2026-11-01) | ✅ đọc được |
| — | Đọc file đơn giá mẫu riêng của người dùng | ⏳ chờ file |
| — | Plugin cho AutoCAD 2025+ (.NET 8) | ⏳ khi cần |
