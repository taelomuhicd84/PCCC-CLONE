# Bài học từ dự án – Sơ bộ thiết kế PCCC
Chỉ áp dụng bài có "Trạng thái: da_duyet". Ghi bởi agent pccc-hoc-du-an, kiểm bằng `npx tsx runner/src/hoc_du_an.ts kiem`.

### Nhà có sẵn 2 thang thường, cao > 28 m: ưu tiên phương án ít đục phá (2026-10-30, nguồn: khách sạn 10 tầng, so tư vấn với hồ sơ người thiết kế nộp)
- Trạng thái: da_duyet
- Căn cứ: QCVN 06:2022/BXD 3.4.13, 3.4.9, 6.13; hồ sơ thiết kế đã nộp
- Nội dung: Không mặc định đổi cả hai thang sang N2/N3. Người thiết kế làm: một thang ghi "thang di chuyển nội bộ, không thoát nạn" (vẫn trong buồng thang ngăn cháy, không tính lối ra); thang còn lại thêm buồng đệm (≈ 4–9 m²); xây khối mới ở đất trống sát nhà gồm buồng thang thoát nạn + thang máy chữa cháy chung buồng đệm (≈ 20 m², ≥ 6 m²). Tư vấn phải nêu phương án này cạnh phương án cải tạo cả hai thang, so khối lượng đục phá.

### Đo đất trống quanh nhà trước khi loại thang ngoài nhà hoặc khối mới (2026-10-30, nguồn: khách sạn 10 tầng)
- Trạng thái: da_duyet
- Căn cứ: QCVN 06:2022/BXD 3.4.11; tổng mặt bằng
- Nội dung: Không kết luận "thang ngoài nhà không khả thi" chỉ vì ba phía giáp nhà dân. Xét từng khoảng sân (sân sau, góc lô, lối phụ): còn ≥ 4–6 m thì đề xuất được buồng thang hoặc thang ngoài nhà mới. Lỗi đã mắc: bỏ qua sân phía Nam cánh Đông dù đã biết khoảng đó hướng ra sân.

### Số người: dùng sức chứa thiết kế trước, Bảng G.9 sau (2026-10-30, nguồn: khách sạn 10 tầng)
- Trạng thái: da_duyet
- Căn cứ: QCVN 06:2022/BXD Phụ lục G, G.3 chú thích 1; 3.4.1; 3.2.9
- Nội dung: Bảng G.9 chỉ dùng khi thiết kế không ghi số người. Gian đông người (hội trường tiệc) chưa ghi sức chứa thì tư vấn đề xuất ghi SỨC CHỨA THIẾT KẾ trên mặt bằng trước (người thiết kế ghi 100 người, bảng G cần 0,61 m < 0,9 m nên giữ thang 0,9 m), sau đó mới nói mở rộng thang/cửa nếu chủ đầu tư cần sức chứa lớn. Không lấy G.9 (≈ 280 người) làm kết luận CHƯA ĐẠT.

### Lối vào từ trên cao: không tự đặt thêm điều cấm (2026-10-30, nguồn: khách sạn 10 tầng)
- Trạng thái: da_duyet
- Căn cứ: QCVN 06:2022/BXD 6.3.2, 6.3.4, 6.3.3
- Nội dung: 6.3.2 chỉ cấm kho, phòng máy, buồng thang thoát nạn, sảnh không nhiễm khói, sảnh thang máy chữa cháy, không gian dẫn tới điểm cụt. Cửa sổ WC (1.500 x 1.500, mép dưới 900) người thiết kế chọn làm lối vào trên cao là hợp lệ; tư vấn đã loại sai vì "là WC". Ưu tiên ô không có lan can chắn. Thể hiện như hồ sơ: tam giác trên mặt đứng, bảng ký hiệu kích thước, ghi chú 1 m trống trên mặt bằng.

### Hồ sơ thoát nạn thể hiện gì trên mỗi mặt bằng (2026-10-30, nguồn: khách sạn 10 tầng)
- Trạng thái: da_duyet
- Căn cứ: QCVN 06:2022/BXD 3.2.8, Phụ lục G, 6.17.1; hồ sơ thiết kế đã nộp
- Nội dung: Người thiết kế đặt trên đầu mỗi mặt bằng một bảng tính G (diện tích, khối tích, bậc chịu lửa, số người, mật độ, chiều dài thoát nạn theo quy định/thực tế, bề rộng tính toán/thực tế, kết luận); ghi L hai lối và D đường chéo; ghi sức chứa; ghi chú cấp vật liệu CV2/CV3 trên đường thoát nạn; cửa không chốt khóa; phòng trực PCCC; chi tiết thang máy chữa cháy. Sơ bộ tư vấn nên ra đúng danh mục này.

### Sân thượng một thang: ghi "không có người có mặt thường xuyên" (2026-10-30, nguồn: khách sạn 10 tầng)
- Trạng thái: da_duyet
- Căn cứ: QCVN 06:2022/BXD 3.2.6.1; định nghĩa chiều cao PCCC (1.4); người dùng xác nhận 2026-10-30
- Nội dung: Sân thượng chỉ có một thang lên thì ghi trên mặt bằng "không có người có mặt thường xuyên (không có mặt 6 giờ liên tục)". Người dùng xác nhận cơ quan thẩm định chấp nhận cách này, không cần kéo thang thứ hai lên. Tư vấn đề xuất cách này trước.

### Bậc chịu lửa tra Bảng H.2, không mặc định bậc I khi cao > 28 m (2026-10-30, nguồn: khách sạn 10 tầng)
- Trạng thái: da_duyet
- Căn cứ: QCVN 06:2022/BXD Phụ lục H, H.2.1, Bảng H.2
- Nội dung: Khách sạn thuộc nhà công cộng (H.2.1, Bảng H.2). Bậc II, S0 cho phép chiều cao PCCC đến 50 m; diện tích một tầng trong khoang cháy đến 2.200 m² khi 10–16 tầng, đến 4.000 m² khi 6–9 tầng. Khách sạn cao ≈ 31–34 m, 10 tầng, ≈ 454 m²/tầng nên bậc II, S0 ĐẠT, người thiết kế chọn đúng. Lỗi đã mắc: tư vấn theo quy tắc nhớ "cao > 28 m thì bậc I" mà không tra bảng. Bậc I chỉ bắt buộc khi vượt giới hạn bậc II (cao > 50 m hoặc tầng lớn hơn diện tích cho phép).

### Thang ngoài nhà (loại 3) và cửa chống cháy chỗ vào thang (2026-10-30, nguồn: khách sạn 10 tầng, người dùng giải thích)
- Trạng thái: da_duyet
- Căn cứ: QCVN 06:2022/BXD 3.4.11, 3.4.2; người dùng (người thiết kế) giải thích 2026-10-30
- Nội dung: Thang nằm ngoài nhà, không có tường bao quanh, là cầu thang bộ loại 3 (thang ngoài nhà), dù tên trên bản vẽ là "thang sắt thoát hiểm bên ngoài". Cửa chống cháy ở chỗ từ nhà ra thang là do khoang phía trước (ghi "buồng đệm") không đảm bảo yêu cầu buồng đệm, nên ngăn bằng cửa chống cháy tại đó; cửa này không biến thang thành buồng thang. Nhà cao PCCC trên 28 đến 50 m dùng thang loại 3 khi phần thang từ trên 28 m chống rơi ngã toàn bộ các mặt hở (3.4.11). Lỗi đã mắc: thấy cửa chống cháy trên mặt cắt rồi suy là buồng thang.

### Chiếu nghỉ có bậc chéo (rẽ quạt) thì thang không dùng thoát nạn (2026-10-30, nguồn: khách sạn 10 tầng, người dùng chỉ ra)
- Trạng thái: da_duyet
- Căn cứ: QCVN 06:2022/BXD 3.4.4, 3.4.2, 3.4.3
- Nội dung: Nhận diện trên mặt bằng: ở chiếu nghỉ, thay vì một ô phẳng có các nét chéo tỏa từ đầu tường giữa hai vế (hình chữ V hoặc rẻ quạt) thì đó là bậc chéo. Bậc chéo trên đường thoát nạn chỉ cho phép khi nhà F1.2, F1.3, F2–F5 cao PCCC ≤ 15 m và ≤ 15 người/tầng (tối đa 3 bậc tại góc xoay ≤ 90°), hoặc thang cong đủ điều kiện 3.4.4 với chiều cao thang ≤ 9 m. Nhà cao > 15 m: thang có bậc chéo KHÔNG tính là lối thoát nạn; chỉ là thang di chuyển nội bộ, vẫn để trong buồng thang ngăn cháy. Lỗi đã mắc: BTB2 vẽ hai vế 900 + 100 + 900, chiếu nghỉ dưới có bậc chéo, tư vấn ghi "2 vế thẳng, không rẽ quạt" và tính BTB2 là lối thoát nạn. Người thiết kế đúng khi ghi BTB2 "không thoát nạn" và thêm thang ngoài nhà. Khi đếm lối: phóng to từng chiếu nghỉ trước khi tính thang.

### Công năng đọc từ nhãn phòng trên mặt bằng, không đọc tên bản vẽ; số tầng theo cốt sàn (2026-10-31, nguồn: Roy PPS, người dùng sửa)
- Trạng thái: da_duyet
- Căn cứ: người dùng dạy 2026-10-31; TCVN 13967:2024 Điều 9; QCVN 06:2022/BXD 3.5
- Nội dung: Công năng lấy từ nhãn phòng ghi trên mặt bằng, không lấy từ khung tên hay tên file (lỗi: ghi "nhà ở" vì tên bản vẽ). Số tầng đếm theo cốt sàn và đánh số tầng; ít tờ bản vẽ thì suy ra và ghi "ước lượng", không suy từ nhãn WC. Lửng của nhà ở riêng lẻ kết hợp ≤ 65 % sàn tầng dưới thì không tính; các nhà khác tính 1 tầng (F1–F4 chỉ khu kỹ thuật ≤ 10 % và ≤ 300 m² mới không tính). Chiều cao PCCC tính từ mặt đường xe chữa cháy tiếp cận tới mép dưới cửa sổ tầng trên cùng (QCVN 06 mục 3.5).

### Thang rẻ quạt, bậc chéo: hai điều kiện của QCVN 06 3.4.4 và Hình A.12 (2026-10-31, nguồn: Roy PPS, người dùng sửa)
- Trạng thái: da_duyet
- Căn cứ: QCVN 06:2022/BXD 3.4.4; TCVN 13967:2024 Hình A.12; `knowledge/nhan-dien-thang-thoat-nan.md`
- Nội dung: Thang rẻ quạt dùng thoát nạn khi đạt MỘT trong hai: (a) vùng A bề rộng bậc ≥ 250 mm nằm trong bản thang rộng ≥ 0,7/0,9 m, hoặc (b) ≤ 3 bậc chéo, góc ≤ 90°, cao ≤ 15 m, ≤ 15 người/tầng. Hình A.12 (dải đánh dấu mép bậc) áp dụng cho thang thoát nạn của công trình bất kỳ, không riêng nhà ở. Phải đếm bậc chéo ở chiếu nghỉ (lỗi: đếm 3 bậc thay vì 4). Thang loại 2 nối ≥ 3 tầng không tính thoát nạn trừ 3.2.1/3.2.2/3.2.6 (3.4.16).

### Đối chiếu thang N1/N2/N3, hút khói hành lang, phân tán lối ra (2026-10-31, nguồn: Duy Anh, người dùng hỏi)
- Trạng thái: da_duyet
- Căn cứ: QCVN 06:2022/BXD 3.4.7–3.4.10, 3.2.8, Phụ lục D D.1.2
- Nội dung: Không ghi "cần N1/N2/N3" khi chưa đối chiếu: N1 bắt buộc chỉ khi cao > 28 m; ≥ 2 tầng hầm thì N3 hoặc N2 qua khoang đệm; L1 cần lỗ sáng ≥ 1,2 m²; L2 chỉ cho phép khi cao ≤ 9 m (12 m). Hành lang > 15 m không thông gió tự nhiên phải có thoát khói (D.1.2), chỉ miễn nhà F4 ≤ 6 tầng có báo cháy khói hoặc sprinkler. Hai lối ra tầng phải phân tán ≥ ½ đường chéo (⅓ nếu sprinkler toàn nhà, tối thiểu 7 m).
