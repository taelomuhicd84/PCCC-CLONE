---
name: tkpccc-tra-quy-chuan
description: Tra cứu quy chuẩn PCCC đã số hoá trong TKPCCC (01_TRI_THUC/ và 02_DIEU_KHOAN/: QCVN 06:2022 sửa đổi 2023, QCVN 10:2025/BCA, TCVN 7568-14:2025, NĐ 105/2025 + NĐ 347/2026, công văn C07) để trả lời câu hỏi thiết kế/thẩm định, viết khảo sát, viết hướng dẫn B0–B7 hay prompt cho Claude trong runner. Dùng mỗi khi cần một điều khoản, trị số, bảng tra, diện thẩm định, loại đầu báo, số lối thoát nạn, trang bị hệ thống nào — kể cả khi câu hỏi không nhắc tên quy chuẩn.
---

# Tra quy chuẩn PCCC trong TKPCCC

Nguyên tắc (vì sao): người dùng là kỹ sư PCCC, kết quả đi vào hồ sơ thẩm duyệt. Trích sai hoặc trích quy định đã hết hiệu lực là lỗi nặng.
- Chỉ trích điều khoản có trong file `01_TRI_THUC/` hoặc `02_DIEU_KHOAN/`, ghi rõ file + mục. Không có thì nói "cần tra <văn bản>", không viết theo trí nhớ.
- Chỉ dùng văn bản hiện hành: QCVN 10:2025/BCA thay tiêu chuẩn trang bị cũ; cấp nước chữa cháy theo QCVN 10 (QCVN 06 Điều 5 đã bỏ);
  diện thẩm định theo NĐ 105/2025 sửa bởi NĐ 347/2026 + công văn C07. Bản vẽ cũ trích TCVN 3890, NĐ 136/2020… chỉ là "cách thể hiện cũ".
- Thiếu số liệu quyết định (chiều cao PCCC, diện tích, nhóm F…) ⇒ kết luận "CẦN XÁC NHẬN", nói thiếu gì; số ước lượng ghi nhãn "ước lượng".
- Đọc đúng file cần (file dài 30–75 KB): dùng Grep theo số điều/từ khoá trước, rồi Read đoạn đó, đừng nạp cả loạt.

## Chọn file theo câu hỏi
| Câu hỏi | File |
|---|---|
| Nghĩa của thuật ngữ (nhóm F, chiều cao PCCC, lối ra…) | `01_TRI_THUC/dinh-nghia/dinh-nghia.md`, `01_TRI_THUC/thuat-ngu-chuan.md` |
| Có thuộc diện thẩm định thiết kế không | `01_TRI_THUC/nghi-dinh-tham-dinh-dieu-chinh.md` (Phụ lục III), `01_TRI_THUC/cv-2360-cahn-1183-c07-dien-tham-dinh.md` |
| Bậc chịu lửa, cấp nguy hiểm cháy kết cấu | `01_TRI_THUC/qcvn06-phu-luc-h.md` |
| Ngăn cháy, khoảng cách PCCC, giáp ranh | `01_TRI_THUC/qcvn06-dieu-4.md`, `01_TRI_THUC/giap-ranh-khoang-cach-pccc.md`, `02_DIEU_KHOAN/SKILL_ngan_chay_cong_nang.md` |
| Đường, bãi đỗ xe chữa cháy, thang máy chữa cháy, CNCH | `01_TRI_THUC/qcvn06-dieu-6.md`, `01_TRI_THUC/so-bo-thiet-ke-pccc.md` |
| Thoát nạn: số lối ra, cửa, hành lang, cầu thang, khoảng cách | `02_DIEU_KHOAN/SKILL_thoat_nan.md` (đọc trước) → `02_DIEU_KHOAN/MOD_*.md`, `02_DIEU_KHOAN/PHU_LUC_G_QCVN06.md` |
| Chống khói, tăng áp | `02_DIEU_KHOAN/MOD_chong_khoi_PL_D.md` |
| Hút khói sảnh thông tầng, thang hở nối tầng (hút đồng thời các tầng) | `01_TRI_THUC/hut-khoi-thong-tang.md` (nhận diện thang: `01_TRI_THUC/nhan-dien-thang-thoat-nan.md`; ngăn cháy 4.35: `01_TRI_THUC/qcvn06-dieu-4.md`) |
| Vật liệu hoàn thiện | `02_DIEU_KHOAN/MOD_vat_lieu_hoan_thien.md` |
| Nhà nào phải có báo cháy / chữa cháy tự động | `01_TRI_THUC/qcvn10-2025-bao-chay-chua-chay-tu-dong.md` (Phụ lục A: Bảng A.1–A.4) |
| Chọn loại đầu báo, khoảng cách đầu báo, khoang dầm | `01_TRI_THUC/chon-loai-dau-bao.md` → `01_TRI_THUC/tcvn7568-14-2025.md`, `02_DIEU_KHOAN/MOD_dau_bao_khoi_khoang_dam.md` |
| Họng nước trong nhà, cấp nước ngoài nhà, trạm bơm | `01_TRI_THUC/qcvn10-2025-nuoc-chua-chay.md` (Phụ lục B, C), `01_TRI_THUC/qcvn10-2025-phu-luc-h.md` (H.1–H.3) |
| Sprinkler, thủy lực TCVN 7336 | `01_TRI_THUC/qcvn10-2025-bao-chay-chua-chay-tu-dong.md` (2.5), `01_TRI_THUC/kiem-bang-tinh-thuy-luc-7336.md` |
| Bình chữa cháy, dụng cụ phá dỡ, mặt nạ, phương tiện cơ giới | `01_TRI_THUC/qcvn10-2025-binh-dung-cu-co-gioi.md` |
| Đèn sự cố, EXIT, loa thông báo | `01_TRI_THUC/qcvn10-2025-den-loa.md` |
| Quy định chung QCVN 10, định nghĩa QCVN 10 | `01_TRI_THUC/qcvn10-2025-chung.md` |
| Quy định chuyển tiếp QCVN 06 | `01_TRI_THUC/qcvn06-dieu-7.md` |
| Viết báo cáo khảo sát (8 mục, form tư vấn 7 mục) | `01_TRI_THUC/khao-sat-bao-gia.md` |
| Ước khối lượng, báo giá sơ bộ | `01_TRI_THUC/bao-gia-pccc.md` (đã mã hoá trong `04_BACKEND/shared/src/khao_sat.ts` → `uocKhoiLuong`) |

Từng bước thiết kế B0–B7 đã gắn sẵn danh sách tài liệu trong `03_DU_LIEU/huong_dan.json` (`tai_lieu`), web hiện nguyên văn ở tab Hướng dẫn.

## Thêm quy chuẩn mới
Chép nguyên văn bằng script (không gõ lại theo trí nhớ) vào `01_TRI_THUC/<ten-kebab>.md` (văn bản, bảng) hoặc `02_DIEU_KHOAN/MOD_<chu_de>.md`
(điều khoản theo chuyên đề); dòng đầu ghi nguồn + ngày văn bản; nối vào bước tương ứng trong `03_DU_LIEU/huong_dan.json` và bảng trên.
