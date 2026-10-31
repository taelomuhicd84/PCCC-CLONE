---
name: tkpccc-tra-quy-chuan
description: Tra cứu quy chuẩn PCCC đã số hoá trong TKPCCC (knowledge/ và skills/: QCVN 06:2022 sửa đổi 2023, QCVN 10:2025/BCA, TCVN 7568-14:2025, NĐ 105/2025 + NĐ 347/2026, công văn C07) để trả lời câu hỏi thiết kế/thẩm định, viết khảo sát, viết hướng dẫn B0–B7 hay prompt cho Claude trong runner. Dùng mỗi khi cần một điều khoản, trị số, bảng tra, diện thẩm định, loại đầu báo, số lối thoát nạn, trang bị hệ thống nào — kể cả khi câu hỏi không nhắc tên quy chuẩn.
---

# Tra quy chuẩn PCCC trong TKPCCC

Nguyên tắc (vì sao): người dùng là kỹ sư PCCC, kết quả đi vào hồ sơ thẩm duyệt. Trích sai hoặc trích quy định đã hết hiệu lực là lỗi nặng.
- Chỉ trích điều khoản có trong file `knowledge/` hoặc `skills/`, ghi rõ file + mục. Không có thì nói "cần tra <văn bản>", không viết theo trí nhớ.
- Chỉ dùng văn bản hiện hành: QCVN 10:2025/BCA thay tiêu chuẩn trang bị cũ; cấp nước chữa cháy theo QCVN 10 (QCVN 06 Điều 5 đã bỏ);
  diện thẩm định theo NĐ 105/2025 sửa bởi NĐ 347/2026 + công văn C07. Bản vẽ cũ trích TCVN 3890, NĐ 136/2020… chỉ là "cách thể hiện cũ".
- Thiếu số liệu quyết định (chiều cao PCCC, diện tích, nhóm F…) ⇒ kết luận "CẦN XÁC NHẬN", nói thiếu gì; số ước lượng ghi nhãn "ước lượng".
- Đọc đúng file cần (file dài 30–75 KB): dùng Grep theo số điều/từ khoá trước, rồi Read đoạn đó, đừng nạp cả loạt.

## Chọn file theo câu hỏi
| Câu hỏi | File |
|---|---|
| Nghĩa của thuật ngữ (nhóm F, chiều cao PCCC, lối ra…) | `knowledge/dinh-nghia/dinh-nghia.md`, `knowledge/thuat-ngu-chuan.md` |
| Có thuộc diện thẩm định thiết kế không | `knowledge/nghi-dinh-tham-dinh-dieu-chinh.md` (Phụ lục III), `knowledge/cv-2360-cahn-1183-c07-dien-tham-dinh.md` |
| Bậc chịu lửa, cấp nguy hiểm cháy kết cấu | `knowledge/qcvn06-phu-luc-h.md` |
| Ngăn cháy, khoảng cách PCCC, giáp ranh | `knowledge/qcvn06-dieu-4.md`, `knowledge/giap-ranh-khoang-cach-pccc.md`, `skills/SKILL_ngan_chay_cong_nang.md` |
| Đường, bãi đỗ xe chữa cháy, thang máy chữa cháy, CNCH | `knowledge/qcvn06-dieu-6.md`, `knowledge/so-bo-thiet-ke-pccc.md` |
| Thoát nạn: số lối ra, cửa, hành lang, cầu thang, khoảng cách | `skills/SKILL_thoat_nan.md` (đọc trước) → `skills/MOD_*.md`, `skills/PHU_LUC_G_QCVN06.md` |
| Chống khói, tăng áp | `skills/MOD_chong_khoi_PL_D.md` |
| Vật liệu hoàn thiện | `skills/MOD_vat_lieu_hoan_thien.md` |
| Nhà nào phải có báo cháy / chữa cháy tự động | `knowledge/qcvn10-2025-bao-chay-chua-chay-tu-dong.md` (Phụ lục A: Bảng A.1–A.4) |
| Chọn loại đầu báo, khoảng cách đầu báo, khoang dầm | `knowledge/chon-loai-dau-bao.md` → `knowledge/tcvn7568-14-2025.md`, `skills/MOD_dau_bao_khoi_khoang_dam.md` |
| Họng nước trong nhà, cấp nước ngoài nhà, trạm bơm | `knowledge/qcvn10-2025-nuoc-chua-chay.md` (Phụ lục B, C), `knowledge/qcvn10-2025-phu-luc-h.md` (H.1–H.3) |
| Sprinkler, thủy lực TCVN 7336 | `knowledge/qcvn10-2025-bao-chay-chua-chay-tu-dong.md` (2.5), `knowledge/kiem-bang-tinh-thuy-luc-7336.md` |
| Bình chữa cháy, dụng cụ phá dỡ, mặt nạ, phương tiện cơ giới | `knowledge/qcvn10-2025-binh-dung-cu-co-gioi.md` |
| Đèn sự cố, EXIT, loa thông báo | `knowledge/qcvn10-2025-den-loa.md` |
| Quy định chung QCVN 10, định nghĩa QCVN 10 | `knowledge/qcvn10-2025-chung.md` |
| Quy định chuyển tiếp QCVN 06 | `knowledge/qcvn06-dieu-7.md` |
| Viết báo cáo khảo sát (8 mục, form tư vấn 7 mục) | `knowledge/khao-sat-bao-gia.md` |
| Ước khối lượng, báo giá sơ bộ | `knowledge/bao-gia-pccc.md` (đã mã hoá trong `shared/src/khao_sat.ts` → `uocKhoiLuong`) |

Từng bước thiết kế B0–B7 đã gắn sẵn danh sách tài liệu trong `data/huong_dan.json` (`tai_lieu`), web hiện nguyên văn ở tab Hướng dẫn.

## Thêm quy chuẩn mới
Chép nguyên văn bằng script (không gõ lại theo trí nhớ) vào `knowledge/<ten-kebab>.md` (văn bản, bảng) hoặc `skills/MOD_<chu_de>.md`
(điều khoản theo chuyên đề); dòng đầu ghi nguồn + ngày văn bản; nối vào bước tương ứng trong `data/huong_dan.json` và bảng trên.
