# Roadmap TKPCCC

Kế hoạch gốc: `C:\Users\HP\.claude\plans\t-i-mu-n-thi-t-k-eager-giraffe.md`. Việc đang làm: `TIEN_DO.md`.

| Pha | Nội dung | Trạng thái |
|---|---|---|
| 1 | Thư viện block PCCC (attribute `MA_VT`) → vẽ thiết bị/ống → bóc khối lượng → báo giá Excel theo đơn giá người dùng | ✅ 2026-10-31 |
| 1+ | Từ LHB Block Scheduler (chỉ ý tưởng): không đếm trùng, block lồng/MINSERT, bỏ XREF, tách tầng/khu, `PCCC_GAN`, `PCCC_BANG`, `PCCC_VUNGBV` | ✅ 2026-10-31 |
| 2 | Web hướng dẫn thiết kế B0–B7 (tài liệu nguyên văn, hỏi Claude), báo cáo khảo sát 8 mục (Word, Claude gợi ý), báo giá sơ bộ theo m² | ✅ 2026-10-31 |
| 3 | Đọc PDF + thẩm định: lõi Python `thamdinh/` (chép từ repo `taelomuhicd84/PCCC` ab546c7, offline, 26 luật QCVN 06), tab web Thẩm định PDF (D/KD/XM + PDF ghi chú), "Claude xét" dòng XM | ✅ 2026-11-01 (chưa thử hồ sơ thật trên máy này) |
| 3+ | Thẩm định sâu hơn: đọc bảng cửa/thang (bớt XM "không đọc được"), bảng đối chiếu A/B/KT như `SKILL-PCCC-NEW`, Word đối chiếu | ⏳ hỏi người dùng |
| 4a | Thủy lực sprinkler TCVN 7336 PL B: lõi `shared/src/thuy_luc.ts` (chép từ SKILL-PCCC-NEW, test khớp Excel), tab web Thủy lực (sơ bộ, so phương án, tính theo số liệu), xuất Excel mẫu `MAU_THUY_LUC_7336.xlsx` (sửa F106 theo B.2.10.12) | ✅ 2026-11-01 |
| 4b | Vẽ tự động trong CAD: `PCCC_RAI` rải đầu phun/đầu báo theo lưới trong polyline phòng (cần máy có AutoCAD để kiểm) | ⏳ |
| 4+ | Thủy lực mở rộng: kho nhóm 5–7 (Bảng 2), TCVN 14496 giá kệ, ô HCB2 cụm van trong Excel mẫu, "chọn số đầu C07" trên web, đọc số liệu ống từ bản vẽ | ⏳ hỏi người dùng |
| — | Repo tham khảo `SKILL-PCCC-NEW`, `PcccVipPro`, `PCCC` (người dùng mở công khai 2026-11-01) | ✅ đọc được |
| — | Đọc file đơn giá mẫu riêng của người dùng | ⏳ chờ file |
| — | Plugin cho AutoCAD 2025+ (.NET 8) | ⏳ khi cần |
