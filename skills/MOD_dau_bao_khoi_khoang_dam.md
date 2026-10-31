# MODULE: Đầu báo cháy khói trong khoang dầm (TCVN 7568-14:2025, 5.9.1.1.6)
Mở khi kiểm tra bố trí đầu báo cháy khói kiểu điểm (và khói kiểu hút, cùng Hình 4) trên trần có dầm. Dùng để (1) thẩm: ghi kết luận và ghi chú; (2) hướng dẫn thiết kế: nói cho đúng cách đặt.
Nguồn điều khoản: `knowledge/tcvn7568-14-2025.md`. Sơ đồ minh hoạ 6 trường hợp: file "6TH BC 7568-142025.pdf" (Sugar MEPF, N. Duong), cùng nội dung với 5.9.1.1.6 a–f.
Chọn khói hay nhiệt hay tia chiếu theo khu vực: `knowledge/chon-loai-dau-bao.md` (nguồn duy nhất, không chép lại ở đây).
Phạm vi: **chỉ đầu báo khói**. Đầu báo nhiệt có quy tắc riêng (5.9.2.1.5: dầm sâu hơn 0,3 m thì giảm khoảng cách 30 %). Không áp quy tắc này cho đầu báo nhiệt.

## 1. Ba số cần đo trước khi kết luận
| Ký hiệu | Là gì | Lấy ở đâu |
|---|---|---|
| h | chiều cao trần (m) | mặt cắt, cao độ sàn và trần |
| d | độ sâu dầm tính từ mặt trần xuống đáy dầm (m) | mặt cắt, bảng dầm kết cấu |
| S | diện tích khu vực giữa các dầm, mỗi ô tính riêng (m²) | mặt bằng dầm, kích thước thông thủy giữa hai mép dầm |

Xét **từng ô giữa các dầm**, không xét cả phòng một lần. Một phòng có thể có ô S < 4 m² lẫn ô S ≥ 4 m².
Thiếu mặt bằng dầm, hoặc không đọc được d, h: ghi CẦN XÁC NHẬN, yêu cầu bổ sung mặt bằng dầm và mặt cắt. Không đoán d.

## 2. Bảng quyết định (nguyên văn 5.9.1.1.6)
Mọi trường hợp vẫn phải đạt khoảng cách 5.9.1.1.2 và 5.9.1.1.4 (bên dưới).
| TH | Điều kiện | Cách đặt đầu báo | Mục |
|---|---|---|---|
| 1 | d ≤ 0,3 m (h ≤ 15 m) | coi như trần phẳng; đặt ở điểm bất kỳ, miễn đủ khoảng cách | a |
| 2 | h < 2 m và d > 0,3 m | như TH1: điểm bất kỳ, miễn đủ khoảng cách | b |
| 3 | 2 ≤ h ≤ 4 m, d ≥ 0,3 m, **S < 4 m²** | lắp ở **mặt dưới các dầm** | c |
| 4 | 2 ≤ h ≤ 4 m, d ≥ 0,3 m, **S ≥ 4 m²** | **ít nhất một đầu báo trong mỗi ô** giữa các dầm; nếu khoảng cách y không đạt thì thêm đầu báo | d |
| 5 | h ≥ 4 m, d ≥ 0,3 m, **S < 9 m²** (h ≤ 15 m) | lắp ở **mặt dưới các dầm** | e |
| 6 | h ≥ 4 m, d ≥ 0,3 m, **S ≥ 9 m²** (h ≤ 15 m) | lắp **trong các ô** giữa các dầm | f |

Ngưỡng diện tích: 4 m² khi trần thấp (≤ 4 m), 9 m² khi trần cao (≥ 4 m). Ô nhỏ thì dầm gần như không chặn khói nên đặt ở đáy dầm; ô lớn thì khói tụ trong ô nên phải có đầu báo ngay trong ô.

## 3. Khoảng cách luôn phải đạt (5.9.1.1.2 và 5.9.1.1.4)
- Từ điểm bất kỳ trên trần tới đầu báo gần nhất **≤ 7,2 m**; giữa hai đầu báo **≤ 10,2 m**.
- Từ hàng đầu báo gần nhất tới tường, vách **≤ 5,1 m và ≥ 0,5 m**.
- Tới mép cửa cấp không khí **≥ 0,4 m**; tới mép ngoài cánh quạt **≥ 0,4 m**.
- Cảm biến cách trần 0,025–0,3 m khi h < 4 m; không quá 0,6 m khi h từ 4 đến 15 m (5.9.1.1.1). Trần cao hơn 15 m: dùng loại đầu báo khác.
- Trần giả, khoảng cách tính tới mặt trên trần giả > 2 m: như trần phẳng. Nhỏ hơn 2 m và có cấu kiện sâu ≤ 0,3 m: khoảng cách giữa đầu báo ≤ 15 m, tới tường ≤ 10,2 m (5.9.1.1.7).

## 4. Tư duy xử lý (diễn giải, không phải nguyên văn tiêu chuẩn)
1. **Đi từ d trước, rồi h, rồi S.** d ≤ 0,3 thì hết chuyện, quay về trần phẳng. d > 0,3 mới xét h và S.
2. **Đặt "dưới dầm" khác với 5.9.1.1.1.** Cảm biến dưới đáy dầm cách trần bằng d, lớn hơn 0,3 m. TH3 và TH5 nói rõ lắp ở mặt dưới dầm, nên coi đây là ngoại lệ có chủ đích của 5.9.1.1.6; không ghi CHƯA ĐẠT chỉ vì khoảng cách tới trần.
3. **Ranh giới chồng nhau:** a và c cùng chứa d = 0,3 m; c/d và e cùng chứa h = 4 m. Với h = 4 m và 4 ≤ S < 9 m², c/d đòi một đầu báo trong ô còn e đòi dưới dầm. Với d = 0,3 m, a coi như trần phẳng còn c/e coi như có dầm. Cách xử lý an toàn: chọn yêu cầu chặt hơn (đặt đầu báo trong ô, đủ khoảng cách), ghi kết luận CẦN XÁC NHẬN nếu thiết kế chỉ đạt theo phương án lỏng.
4. **Đo S theo ô thật.** Lấy kích thước thông thủy giữa hai mép dầm, không lấy khoảng cách tim cột. Ô hình chữ L, ô bị cột chia: tách từng phần nếu dầm hoặc cột sâu hơn 0,3 m.
5. **Khoảng cách y trong ô lớn.** Khi S ≥ ngưỡng, một đầu báo trong ô chưa chắc đủ: ô dài tới trên 10,2 m hoặc có điểm cách đầu báo trên 7,2 m thì thêm đầu. Kiểm tra theo bán kính 7,2 m, không chỉ đếm số đầu.
6. **Vách, kệ chia phòng:** 5.8.2.1.1 c coi khu vực bị chia bởi tường, vách hoặc giá kệ là phòng riêng khi chúng cách trần (hoặc đáy dầm ngang) không quá 0,3 m. Gặp dầm rất sâu sát sàn, đọc kỹ điều này và ghi CẦN XÁC NHẬN nếu chưa rõ có tính là vách ngăn không.
7. **Không gian kín, trần dốc:** trần giả, hộp kỹ thuật theo 5.9.1.1.7; mái dốc theo 5.9.1.1.3. Không dùng bảng này cho các trường hợp đó.
8. **Phối hợp với các hạng mục khác:** đầu báo không nằm vùng gió thổi trực tiếp (cửa cấp gió, quạt: ≥ 0,4 m). Khoảng trống quanh đầu báo bán kính ≥ 0,1 m, sâu 0,6 m (5.8.2.1.1 d); dầm sát đầu báo có thể vi phạm điều này.

## 5. Kết luận mẫu cho dòng đối chiếu
| Tình huống đọc được từ bản vẽ | Kết luận | Ghi chú thẩm (mẫu) |
|---|---|---|
| Thấy dầm, có số h, d, S, bố trí đúng bảng mục 2 và khoảng cách | ĐẠT | "Phòng X: h = 3,3 m, d = 0,6 m, ô 3 × 6 m = 18 m² ≥ 4 m², có 1 đầu báo trong mỗi ô, khoảng cách 5,2 m ≤ 10,2 m. Đạt 5.9.1.1.6 d TCVN 7568-14:2025." |
| Ô S < ngưỡng mà đầu báo đặt giữa ô, không ở đáy dầm | CHƯA ĐẠT | "Ô 1,5 × 2 m = 3 m² < 4 m², h = 3,3 m, d = 0,5 m: phải lắp ở mặt dưới dầm (5.9.1.1.6 c). Thiết kế đặt giữa ô." |
| Ô S ≥ ngưỡng mà không có đầu báo trong ô, chỉ đặt dưới dầm | CHƯA ĐẠT | "Ô 4 × 5 m = 20 m² ≥ 4 m²: cần ít nhất 1 đầu báo trong ô (5.9.1.1.6 d)." |
| Bản vẽ không có mặt bằng dầm hoặc không có d | CẦN XÁC NHẬN | "Chưa có mặt bằng dầm, chưa xác định d, S: bổ sung để xét 5.9.1.1.6." |
| h = 4 m hoặc d = 0,3 m đúng ranh giới | CẦN XÁC NHẬN | "Ranh giới giữa c/d và e (hoặc a và c): đề nghị bố trí theo yêu cầu chặt hơn." |
| Trần h > 15 m | CẦN XÁC NHẬN | "Trần trên 15 m: đầu báo khói điểm không áp dụng, xem loại đầu báo khác (5.9.1.1.1)." |

## 6. Hướng dẫn thiết kế (khi bật công tắc "Hướng dẫn thiết kế")
Nói theo thứ tự việc phải làm, kèm số cụ thể:
1. Lập bảng từng ô giữa dầm: h, d, S.
2. d ≤ 0,3 m: bố trí như trần phẳng (7,2 m, 10,2 m, 0,5–5,1 m tới tường).
3. d > 0,3 m: h < 2 m thì như trần phẳng; 2 ≤ h ≤ 4 m thì ô dưới 4 m² lắp ở đáy dầm, ô từ 4 m² trở lên đặt ít nhất 1 đầu trong ô; h ≥ 4 m thì ô dưới 9 m² lắp ở đáy dầm, ô từ 9 m² trở lên đặt trong ô.
4. Sau khi đặt, vẽ lại vòng bán kính 7,2 m và kiểm tra khoảng cách 10,2 m, 5,1 m, 0,4 m (cửa gió, quạt); thêm đầu báo ở chỗ còn hở.
5. Ghi trên bản vẽ cao độ lắp (dưới dầm hoặc trong ô), kích thước ô và d, để người thẩm tra không phải suy đoán.
