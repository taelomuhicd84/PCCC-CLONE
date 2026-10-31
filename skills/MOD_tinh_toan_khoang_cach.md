> Bề rộng lối vào buồng thang tính theo G.2.1.1 (N/165), không áp 1,2 m của 3.2.9: xem MOD_loi_ra_va_cua.md, mục 'Cách áp dụng bề rộng 3.2.9'.

# MODULE: Tính toán số người, mật độ, bề rộng, khoảng cách thoát nạn và phân tán 2 lối (3.2.8, Phụ lục G)
Đọc khi lập khối tính toán. Trị số bảng tra ở PHU_LUC_G_QCVN06.md. Số lượng lối ra và quy tắc 50% lối qua khoang cháy khác công năng (3.2.4): xem MOD_loi_ra_va_cua.md.

## 4. Khoảng cách phân tán giữa 2 lối ra (điều 3.2.8, Hình I.4, I.5)
- Giả thiết đám cháy chặn 1 lối, các lối còn lại phải đủ thoát nạn.
- L ≥ **½D** (D = đường chéo lớn nhất của gian phòng / phần nhà / tầng). Toàn nhà có Sprinkler → L ≥ **⅓D**.
- Cách đo: đường thẳng nối **2 cạnh xa nhất** của 2 lối ra, phải ≥ 7 m. Nếu < 7 m → đo lại theo **2 cạnh gần nhất** rồi so số đó với ½D.
- Phòng nằm trong phòng khác: xét theo đường chéo **d** của chính phòng đó (L ≥ ½d).
- Hai buồng thang thoát nạn nối nhau bằng hành lang trong/hành lang bên **được bảo vệ theo 3.3.5** (xem MOD_hanh_lang_bao_che.md) → đo L **dọc theo đường di chuyển trên hành lang** (Hình I.5). Không rõ/không bảo vệ → đo đường thẳng. Thang hở (không phải buồng thang) → coi như không áp dụng, ghi nghi vấn.
- Có Sprinkler hay không lấy từ skill khác (xem [DU_LIEU_TU_SKILL_KHAC.md](DU_LIEU_TU_SKILL_KHAC.md)). Chưa có dữ liệu → tạm dùng ½D.

## 11. Phụ lục G (nạp đầy đủ)
Toàn bộ Phụ lục G (G.1–G.3, Bảng G.1–G.9) nằm ở [PHU_LUC_G_QCVN06.md](PHU_LUC_G_QCVN06.md), chép từ `QCVN 06 SỬA ĐỔI BỔ SUNG 10.2023.pdf`. Khi đo khoảng cách, tính mật độ, bề rộng, số người: tra file này, không dùng trị số nhớ.
Điểm cần nhớ nhất:
- **Cửa chống cháy của gian phòng là điểm xuất phát của đường thoát nạn** (người thẩm định dạy, xem knowledge/bai-hoc.md bài 9): có cửa chống cháy thật sự thì đo từ cửa đó đi; không tính từ gian phòng. Xác nhận cửa đúng GHCL, tự đóng, đúng ký hiệu FD.
- G.1.2.1: khoảng cách đo từ **cửa ra vào gian phòng** (Bảng G.2a); chú thích "tính từ điểm xa nhất của gian phòng" khi hành lang không bao che 3.3.5/3.3.4 **không áp dụng cho tầng có karaoke, vũ trường** → karaoke luôn đo từ cửa phòng.
- Bảng G.2a bậc I, II, III, MĐ ≤ 2: 60 m (cửa ở giữa 2 lối/thang), 30 m (hành lang cụt hoặc sảnh chung).
- G.2.1.1: 165 / 115 / 80 người/m cho bậc I–II / III–IV / V; G.2.1.4: lối ra gian phòng > 50 người ≥ 1,2 m.
- G.3 + Bảng G.9: không có số người thiết kế → diện tích sàn / 1,0 m² (karaoke) hoặc 3,0 (sảnh) hoặc 30 (kho), không tính thang, WC, phụ trợ.

### Chuỗi logic một khối tính (học từ hồ sơ thiết kế thật)
### 9.1 Bố cục một bản vẽ thoát nạn
1. Bảng ký hiệu: mũi tên đậm = hướng thoát nạn; chấm đen tròn = vị trí lối thoát xa nhất; tam giác = lối ra thoát nạn.
2. Bảng ký hiệu cửa (D1, D2, FD1…) kèm kích thước và GHCL (FD1 = cửa thoát hiểm 0,9×2,1 m, EI60). Ghi chú cửa chống cháy: có cơ cấu tự đóng, khe cửa chèn kín.
3. Ký hiệu GHSN = bảng thông báo hạn chế số người đặt trên cửa từng phòng karaoke; trong phòng ghi S, S sử dụng, S bố trí, SL_LV (số người).
4. Trên mặt bằng: tên lối thoát có kích thước ("Lối thoát nạn thứ 1: 1,9×2,3 m"), từng đoạn L ghi ngay trên đường đi, đường chéo D vẽ ra, L giữa 2 lối ghi so với ⅓D.
5. Các khối tính toán: bề rộng thoát nạn → khoảng cách thoát nạn → ghi chú chung → vật liệu hoàn thiện → kết luận.

### 9.2 Chuỗi logic (làm theo thứ tự này)
1. **Chỉ định lối thoát nạn**: đặt tên Lối 1, Lối 2, nêu cửa nào (D1, D2…) và bề rộng. Tầng trệt: cửa ra ngoài; Lầu 1: cửa vào thang CT1/CT2 (ghi "thang thoát nạn số 01/02").
2. **Số người**: tổng SL_LV các phòng (đối chiếu với GHSN). Lấy số lớn nhất làm số tính toán; tránh để 2 con số khác nhau trên cùng bản vẽ.
3. **Diện tích đường thoát nạn S1** = tổng (chiều dài × bề rộng) từng đoạn hành lang/sảnh (vd. 13,4×1,7+7×4,1+1,4×1,7+4×1,2+2×2,9 = 64,46 m²).
4. **Mật độ** MĐ = số người / S1 (92/64,46 = 1,43; 105/60,15 = 1,75 người/m²) so với 2 người/m² theo bảng G.2a (bậc II).
5. **Bề rộng yêu cầu** n = N / 165 (nhà công cộng bậc II, ng/m, điều G.2.1.1 phụ lục G sửa đổi 01) rồi so với tổng bề rộng các lối thiết kế (0,56 m và 0,64 m, đều nhỏ hơn rất nhiều so với 1,9 + 1,5 m).
6. **Khoảng cách**: chọn điểm xa nhất (chấm đen), cộng các đoạn L theo đường đi thực tế đến lối gần nhất (13,4+7 = 20,4 m tầng trệt; 13,1+8,4 = 21,5 m Lầu 1) so với giới hạn theo Bảng G.2a. Bản vẽ Hoàng Anh 1 dùng 40 m, nhưng Bảng G.2a (bậc II, MĐ ≤ 2) cho 60 m với cửa phòng ở giữa 2 lối ra, 30 m với hành lang cụt/sảnh chung → ghi nghi vấn "con số 40 m không khớp Bảng G.2a" (kết luận Đạt không đổi vì 20,4 và 21,5 m đều < 30 m). Vẽ cả đường về lối còn lại.
7. **Phân tán 2 lối**: vẽ D (đường chéo lớn nhất, vd. 32,3 m) và ghi L > ⅓D vì có Sprinkler.
8. **Bao che hành lang** (3.3.5): tường gạch 200 mm REI150, cửa phòng EI60, sàn BTCT REI150, trần không cháy; chứng minh bằng tỉ lệ diện tích cửa/tường < 25% (xem SKILL_ngan_chay_cong_nang.md mục 9). Kết luận "không yêu cầu giải pháp ngăn cháy hành lang bổ sung".
9. **Vật liệu hoàn thiện** (3.5.10 sửa đổi 01): gian phòng karaoke xem là gian phòng chung + có Sprinkler → không yêu cầu cấp nguy hiểm cháy cho vật liệu trang trí, ốp lát.
10. Ghi chú chung: hành lang rộng ≥ 1,2 m, cao > 2,1 m, không đặt vật cản trên đường thoát nạn.
