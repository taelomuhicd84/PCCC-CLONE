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

### Đối chiếu báo cáo khảo sát thật của người dùng: Căn hộ DV Lưu Xuân Lộc (2026-11-02, nguồn: BCKS .xlsx; CHỜ NGƯỜI DÙNG DUYỆT)
- Trạng thái: da_duyet (mục 4 hầm xe → sprinkler + bể 126 m³: người dùng nhắc lại 2026-11-05 "đã train"; các mục còn lại chờ duyệt)
- Căn cứ: QCVN 06:2022/BXD 3.2.6.2a, 3.2.13, 3.4.4, Bảng 14; QCVN 10:2025 Bảng A.1 mục 25.1; BCKS Hoàng Quân Phát
- Nội dung: (1) Tầng thượng (người dùng giải thích 2026-11-02): bản xin phép là sân thượng + mái che tum thang thì không tính tầng; nếu bản hiện trạng đã xây kín cả tầng (có phòng/căn hộ) hoặc diện tích tum vượt mức cho phép thì TÍNH 1 tầng. Phải so bản xin phép với bản hiện trạng (vùng mây) trước khi đếm tầng. Hệ quả: Lưu Xuân Lộc 6 tầng nổi + hầm, cao PCCC 18,4 m (sàn +17.200 + khoảng 1,2 m), không phải 13,8 m. (1b) Khảo sát có thể ghi "1 thang thoát nạn" mà không nói thang nào, vì chưa thang nào đạt: tư vấn phải xét cải tạo TỪNG thang theo `knowledge/nhan-dien-thang-thoat-nan.md` mục "Quy trình đánh giá thang thoát nạn trong nhà" (sửa 2026-11-02: không chọn thang theo ô lấy sáng vì 3.4.8b cho chiếu sáng nhân tạo; không tự quyết, trình bày lý do; không có phương án khả thi thì báo người dùng xem xét). (2) Nhà cao > 15–21 m vẫn được 1 thang (3.2.6.2a) nếu ≤ 200 m²/tầng, ≤ 20 người, báo cháy tự động toàn nhà và có lối thoát khẩn cấp/sân thượng thoáng: khảo sát chỉ sửa thang hiện có (đóng kín bằng tường + cửa chống cháy, bậc xéo, bản thang ≥ 0,9 m, tách thang hầm khỏi thang T1 trở lên, khoang đệm thang hầm–thang máy hầm), không thêm thang khi một thang hiện có cải tạo được. Chỉ xét thang ngoài nhà khi mọi thang trong nhà không cải tạo được, và phải tính thử kích thước, ram dốc, tam cấp. (3) Vùng mây trên bản vẽ là phần cơi nới so với bản xin phép: không phải hai phương án; cơi nới không ảnh hưởng PCCC. (4) Hầm để xe (kể cả xe máy) → bể 126 m³ + sprinkler hầm; hầm kỹ thuật → bể 9 m³; chủ đầu tư chọn. (5) Danh mục chuẩn: báo cháy, đèn EX–EM, bình + tủ dụng cụ phá dỡ thô sơ + tủ mặt nạ phòng độc, họng vách tường + cụm bơm, chống sét. (6) Giáp ranh: tường cao tới mái, không trổ cửa sổ. (7) Nhà > 15 m: bãi đỗ xe chữa cháy rộng ≥ 6 m (Bảng 14). Lỗi đã mắc: tính cao 14,7 m, coi bản cơi nới là bản chọn, loại xe máy khỏi sprinkler hầm, thêm thang ngoài nhà, bỏ chống sét và tủ phá dỡ.

### Bản vẽ cải tạo có 2 mặt bằng: xét số tầng, diện tích, công năng theo BẢN CẢI TẠO (2026-11-02, nguồn: Lưu Xuân Lộc, người dùng dạy)
- Trạng thái: da_duyet
- Căn cứ: người dùng dạy 2026-11-02; QCVN 06:2022/BXD 3.5 (cách tính tầng, chiều cao PCCC)
- Nội dung: Hồ sơ công trình cải tạo/cơi nới thường vẽ trên cùng một tờ HAI mặt bằng cùng tên: mặt bằng phía trên ghi "theo bản vẽ xin phép" (hiện trạng đã xin phép), mặt bằng phía dưới là bản cải tạo (có mây khoanh vùng cơi nới). Phải xét theo bản CẢI TẠO (phía dưới): số tầng, diện tích, công năng, thang, lối ra, chiều cao PCCC. Bản xin phép chỉ để so sánh phần thay đổi; hai bản KHÔNG phải hai phương án chọn một. Ví dụ: tầng thượng xin phép là sân thượng + mái che tum thang (không tính tầng), bản cải tạo xây kín 4 căn hộ → tính 1 tầng (diện tích tum vượt mức cho phép cũng tính tầng). Công trình không cải tạo (chỉ một mặt bằng mỗi tầng) thì xét bình thường. Lỗi đã mắc: lấy bản xin phép tính số tầng và cao PCCC (14,7 m thay vì 18,4 m), coi bản cải tạo là phương án đề xuất.

### BCKS nhà ở riêng lẻ kết hợp kinh doanh (2026-11-05, nguồn: Nguyễn Viết Thanh, người dùng sửa BCKS)
- Trạng thái: da_duyet
- Căn cứ: người dạy 2026-11-05; C07 1183 (nhà ở riêng lẻ kết hợp kinh doanh); QCVN 10:2025 Bảng A.1 mục 25.1
- Nội dung: (1) Nhà ở riêng lẻ kết hợp kinh doanh là nhà còn ÍT NHẤT 1 tầng ở; dù các tầng dưới kinh doanh vẫn là "kết hợp kinh doanh", KHÔNG phải nhà hỗn hợp; chỉ xét như nhà hỗn hợp khi MỘT công năng kinh doanh chiếm ≥ 30 % tổng sàn (Bảng A.1 chú thích 2). (2) Công năng chỉ ghi theo thông tin người dùng cấp, không đoán, không tính ngược; trong BCKS mục Thông tin chỉ có Quy mô được kèm ngoặc, các ngoặc giải thích khác bỏ; thông tin công năng người dùng cấp thì ghi lên bản vẽ ghi chú ở một góc. (3) Hệ thống PCCC: đúng luật, KHÔNG có "khuyến nghị"; chống sét có vì đã có hệ thống PCCC bắt buộc (bảo vệ thiết bị PCCC khỏi sét đánh); hầm để xe máy: sprinkler + bể nước PCCC (số 126 m³ đã nêu trong bài Lưu Xuân Lộc, chưa duyệt riêng; số 9 m³ hầm kỹ thuật chưa có nguồn: không ghi). (4) Tư vấn không có mục "Cung cấp hồ sơ", không có "ngăn cháy phần kinh doanh với phần ở" (không phải nhà hỗn hợp); số lối ra: nhà ở riêng lẻ kết hợp kinh doanh nhóm F1.4 không bắt buộc ≥ 2 lối (3.2.6.1a không liệt kê F1.4), không đặt "lối ra thứ hai". (5) Thang hầm: phải có phương án tách thang hầm → tầng 1 với thang các tầng trên (xem `knowledge/nhan-dien-thang-thoat-nan.md` mục "Tách thang hầm"), vẽ lên bản vẽ cho cả hầm và tầng 1.

### Thang giữa nhà phố: không có tường ngoài để trổ, thoát qua tiền sảnh (2026-11-06, nguồn: Phạm Ngũ Lão, người dùng dạy)
- Trạng thái: da_duyet
- Căn cứ: QCVN 06:2022/BXD 3.4.7; người dùng dạy 2026-11-06
- Nội dung: Nhà phố hẹp có thang ở giữa nhà, hai bên là tường giáp ranh, hai đầu cách xa thì KHÔNG trổ được lối ra ngoài trực tiếp từ buồng thang; không ghi "cần lối ra ngoài trực tiếp". Cách đúng: buồng thang quây kín (cả phần tiểu cảnh/không gian quanh vế), thoát qua TIỀN SẢNH tới cửa ra ngoài, với điều kiện sảnh ngăn cách các gian phòng tiếp giáp (phòng khách, phòng ngủ, bếp, sinh hoạt chung) bằng vách ngăn cháy loại 1, cửa chống cháy tự đóng, chèn kín khe. Thiếu gì: vạch ranh giới tiền sảnh từ cửa buồng thang tới cửa ra, ngăn cháy các phòng tiếp giáp, cửa EI ở mọi tầng, lỗ lấy sáng ≥ 1,2 m² (3.4.8). Lỗi đã mắc: bắt buộc lối ra ngoài trực tiếp ở tầng trệt và quây buồng chỉ ôm vế thang.


### Căn hộ dịch vụ 8 m, Q.2 (2026-11-06, nguồn: người dùng sửa BCKS nhiều lần)
- Trạng thái: da_duyet
- Căn cứ: người dạy 2026-11-06; QCVN 06:2022/BXD 3.2.6.2a, 3.4.16; QCVN 10:2025 Bảng A.1 mục 25.1, Bảng 1 mục 1.2
- Nội dung: (1) Ghi chú PDF phải có khung cho MỌI TT của bảng KS (6–27), không chỉ thang. (2) Nhà không thuộc diện thẩm định: BCKS khoảng cách PCCC và giao thông ghi
  "-Đảm bảo". (3) THOÁT NẠN phải mở đầu bằng số lối: "Công trình có 1 thang bộ (1 lối thoát nạn/tầng). Tuy nhiên … => Chưa đảm bảo …". (4) Hệ thống: "họng nước" mặc định gọi
  "chữa cháy vách tường + bơm"; hầm xe ⇒ sprinkler tầng hầm để xe; có vách tường hoặc sprinkler ⇒ ghi bể nước chữ đỏ đậm: 126m3 (hầm để xe), 9m3 (hầm kỹ thuật), không có hai
  hệ thống đó thì không ghi bể. (5) BCKS ngắn gọn, cùng một font; thoát nạn gom thông số thang một câu (vế ≥ 900, bậc ≥ 250, cao ≤ 220). (6) Cơ sở lưu trú F1.2: lửng tính 1 tầng;
  5 tầng nổi ⇒ vách tường + bơm + bể; < 5 tầng nổi và < 1.500 m² mới được báo cháy độc lập. (7) Quây thang: xem `knowledge/nhan-dien-thang-thoat-nan.md` mục "Vẽ tường quây buồng thang".

### Cửa chống cháy chỉ cho phòng ở/căn hộ, KHÔNG cho WC; đọc tên phòng và cửa trước khi khoanh (2026-11-06, nguồn: Căn hộ DV 8 m, người dùng sửa)
- Trạng thái: da_duyet
- Căn cứ: QCVN 06:2022/BXD (WC là phòng phụ trợ, không phải gian phòng ở/căn hộ); người dạy 2026-11-06
- Nội dung: Khi đề xuất "cửa chống cháy EI cho các gian phòng/căn hộ mở vào thang, hành lang" thì chỉ khoanh cửa của PHÒNG Ở/căn hộ (P. L-01, L-02…), không khoanh cửa WC. Trước khi khoanh phải
  (1) đọc nhãn từng phòng, (2) phóng to xem cửa nào thuộc phòng nào (cung cửa mở ra hành lang/sảnh), (3) so lại với ảnh người dùng. Lỗi đã mắc: lửng khoanh tím cửa WC L-02, WC L-03 và đặt khung lệch cửa phòng L-02, L-01;
  lửng chỉ có P. L-01, L-02 (hai cửa mở ra sảnh thang), lầu 1–3 có 4 phòng L-01…04 (bốn cửa mở ra hành lang).

### Tum thang không có người vẫn có cửa chống cháy (2026-11-06, nguồn: Căn hộ DV 8 m, người dùng nhắc)
- Trạng thái: da_duyet
- Căn cứ: người dạy 2026-11-06
- Nội dung: Tum chỉ có thang, hành lang, kho (không có người) vẫn phải ghi cửa chống cháy EI cho cửa kho/phòng phụ trợ; cửa ra sân thượng không khóa. Không bỏ qua tum chỉ vì "không có người".

### Sảnh thông tầng, thang hở nối tầng nổi: áp `knowledge/hut-khoi-thong-tang.md` (2026-11-06, người dùng nhắc "áp dụng link với nhau")
- Trạng thái: da_duyet
- Căn cứ: PL D D.2 e), D.1.5; QCVN 06 4.35; `knowledge/hut-khoi-thong-tang.md`
- Nội dung: Căn hộ DV 8 m có sảnh thông tầng trệt–lửng và thang hở nối lửng → tum ⇒ khảo sát phải áp bài hút khói thông tầng: ghi ở dòng ngăn cháy phương đứng (đóng kín hoặc màn ngăn khói E45) và dòng chống khói
  (hút khói chung hút đồng thời các tầng, D.1.5, thiếu bản tính ⇒ CHƯA ĐẠT/cần bổ sung); chỉ các tầng NỔI, hầm là khoang riêng. BCKS mục Ngăn cháy thêm "nếu không đóng kín phải có màn ngăn khói E45 và hút khói đồng thời các tầng thông nhau".
