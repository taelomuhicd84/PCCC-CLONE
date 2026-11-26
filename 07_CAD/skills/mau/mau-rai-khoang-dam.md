# Mẫu rải đầu báo khói theo khoang dầm (bản người dùng vẽ)

Nguồn: bản vẽ người dùng gửi tối 16/11/2026 (hồ sơ khách, nằm trong `12_KET_QUA/`, không đưa vào repo), tờ "Mặt bằng tổng thể hệ thống báo cháy",
nhà xưởng 2 tầng, tầng 1 có 118 đầu, tầng 2 có 120 đầu báo khói thường. Số liệu dưới đây đo bằng toạ độ block đầu báo, không phải đoán.
Điều khoản: `02_DIEU_KHOAN/07_BAO_CHAY/MOD_dau_bao_khoi_khoang_dam.md` (TCVN 7568-14:2025 5.9.1.1.6 và khoảng cách 7,2 m / 10,2 m / 5,1 m).

## Đo được
- Lưới cột 10,0 m (X) x 10,6 m (Y); dầm chạy trên các trục cột. Khoang = ô giữa hai trục dầm.
- Mỗi khoang 2 x 2 đầu báo, đặt đối xứng, **cách trục dầm 2,0 m** về hai phía:
  - theo X: 2 000 | 6 000 | 2 000 trong khoang, qua dầm 4 000 (2 000 + 2 000);
  - theo Y: 2 000 | 6 600 | 2 000 trong khoang, qua dầm 4 000.
- Mọi khoang cùng một kiểu nên hàng, cột đầu báo thẳng hàng xuyên suốt cả tầng (12 cột x 10 hàng).
- Khoang giáp tường: đầu báo cách tường 2 300 (= 2 000 tới trục + tường lệch trục), không đổi bước.
- Chỗ cầu thang ở góc: bỏ đầu báo trùng chỗ (tầng 1: 120 - 2 = 118).
- Kiểm tra phủ: vẽ vòng R7200 (ghi chữ "R7200") ở mọi đầu báo hàng biên để chứng minh góc phòng, sát tường đều trong 7,2 m.
- Mỗi tầng 2 zone, ghi tên zone, "kết nối trung tâm báo cháy", diện tích vùng bảo vệ (1 616 m² mỗi zone). Dây tín hiệu đi dọc theo cột đầu báo.

## Vì sao 2 x 2 mỗi khoang (đối chiếu tiêu chuẩn)
- Khoang 10 x 10,6 = 106 m² ≥ 9 m²: phải có đầu báo trong khoang (5.9.1.1.6 d/f).
- Một đầu ở tâm khoang: tới góc khoang √(5² + 5,3²) = 7,29 m > 7,2 m, không đạt. Nên chia 2 x 2.
- 2 x 2 với lề 2 m: điểm xa nhất trong khoang tới đầu báo gần nhất √(3² + 3,3²) = 4,5 m; bước lớn nhất 6,6 m ≤ 10,2 m.

## Khác với cách plugin đang rải (tầng 8 KOCHAM 16/11)
- Plugin rải từng khoang độc lập: một đầu ở tâm mỗi khoang, hàng cột giữa các khoang lệch nhau theo kích thước khoang.
- Mẫu: một lưới chung cho cả tầng, lề cố định tới dầm, số đầu mỗi khoang theo kích thước khoang (7,2 m / 10,2 m).
- Người dùng chốt tối 16/11: khoang dầm thường theo **vì kèo (trục cột)** (nhà xưởng mái kèo, như mẫu); nhà nhiều tầng có **sàn deck hoặc sàn BTCT** thì
  khoang theo dầm sàn (như tầng 8 KOCHAM: dầm sàn 400 cách 3,7 m ra 20 khoang). Luôn xét TỪNG dầm: dầm d ≤ 0,3 m (vd 300X200) bỏ, coi như trần phẳng.
