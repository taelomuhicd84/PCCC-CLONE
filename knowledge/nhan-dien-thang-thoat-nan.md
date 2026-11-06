# Nhận diện thang nào được tính thoát nạn (người dùng dặn 2026-10-30)

Thang rẽ quạt, bậc chéo, thang cong, thang xoắn thường KHÔNG được tính thoát nạn. Gốc: 3.3.7 cấm thang xoắn ốc, thang cong trên đường thoát nạn (trừ 3.4.4) và cấm bậc khác cao hoặc khác rộng trong một bản thang, một buồng thang. Bậc chéo làm mặt bậc khác nhau nên chỉ còn ngoại lệ 3.4.4. Phải xét hình dạng từng thang trước khi đếm lối. Đếm sai thì cả phương án sai. Ví dụ: BTB2 khách sạn 10 tầng bị đếm là lối thoát nạn, tư vấn đổi cả hai thang sang N2/N3, trong khi người thiết kế đúng khi ghi BTB2 "không thoát nạn" và thêm thang ngoài nhà.

## Bước bắt buộc
1. Cắt phóng to từng thang ở mọi trang mặt bằng có thang:
   ```
   python scripts/cat_thang.py <ban_ve.pdf> <ket_qua/.../thang> [--trang 6,8,10]
   ```
   Mỗi thang ra một ảnh `<trang>_<nhãn>.png`, zoom 5. Thang không có nhãn thì tự cắt vùng quanh nó. Không kết luận từ ảnh cả trang: ở zoom trang, bậc chéo nhìn giống chiếu nghỉ.
2. Xem TỪNG chiếu nghỉ, đầu vế, góc quay trên ảnh phóng to. Phân loại theo bảng dưới.
3. Ghi kết quả cho từng thang ở từng tầng (hình dạng có thể đổi theo tầng): `BTB2 T3: bậc chéo ở chiếu nghỉ dưới → không tính thoát nạn (3.4.4)`.
4. Chỉ đếm lối thoát nạn bằng các thang ĐẠT (Bài học 26). Thang không đạt thì giải pháp là ghi "thang di chuyển nội bộ, không thoát nạn", để trong buồng thang ngăn cháy, rồi tìm lối khác: thang ngoài nhà loại 3, hoặc buồng thang mới ở chỗ đất trống.

## Hình dạng trên mặt bằng
| Thấy gì | Là gì | Tính thoát nạn? |
|---|---|---|
| Chiếu nghỉ là ô phẳng, không có nét bậc, dài ≥ bề rộng vế; hai vế song song chạy thẳng | Thang 2 vế thẳng có chiếu nghỉ | Xét tiếp 3.4.1–3.4.3, 3.4.7 |
| Ở chiếu nghỉ, các nét chéo tỏa từ đầu tường giữa hai vế (hình chữ V, rẻ quạt) | Bậc chéo (rẽ quạt) ở chiếu nghỉ. Mẫu: `knowledge/hinh/bac_cheo_chieu_nghi_mau.png` | Chỉ được khi nhà F1.2, F1.3, F2–F5 cao PCCC ≤ 15 m VÀ ≤ 15 người/tầng, tối đa 3 bậc tại góc xoay ≤ 90° (3.4.4). Ngoài ra KHÔNG |
| Bậc hình thang hẹp đầu ở góc quay, tâm là một điểm | Bậc rẻ quạt ở góc | Như trên |
| Các bậc là cung tròn đồng tâm, hoặc vế cong | Thang cong | Chỉ khi đủ mọi điều kiện 3.4.4: F1.2, F1.3, F2–F5; chiều cao thang ≤ 9 m; bán kính ≥ 2 lần bề rộng vế; cổ bậc 150–190; mặt bậc phía trong ≥ 220, ở giữa ≥ 250, phía ngoài ≤ 450 mm. Thiếu kích thước thì CẦN XÁC NHẬN, thường là KHÔNG |
| Vòng tròn có trụ giữa, bậc tỏa quanh trụ | Thang xoắn ốc | KHÔNG tính thoát nạn: trên đường thoát nạn cấm thang xoắn ốc, thang cong toàn phần hoặc từng phần trừ trường hợp 3.4.4 (3.3.7). Chỉ là thang nội bộ hoặc thang lên mái kỹ thuật |
| Số bậc chạy liền qua chỗ quay (ví dụ 9 → 11 → 13 vòng qua đầu vế), không có ô chiếu nghỉ phẳng | Thang 3 vế hoặc bậc trên chiếu nghỉ | CẦN XÁC NHẬN chiếu nghỉ theo 3.4.3 (rộng ≥ bản thang, chiếu nghỉ trung gian ≥ 1,0 m) và kiểu bậc |
| Thang không có tường bao, nằm ngoài đường bao nhà | Thang ngoài nhà loại 3 (dù tên là "thang sắt thoát hiểm") | Có. Nhà cao > 28–50 m thì chống rơi ngã phần trên 28 m (3.4.11); bắt buộc tính G |
| Thang thép thẳng đứng, thang leo | Thang chữa cháy P1, thang leo | KHÔNG tính thoát nạn |
| Thang hở trong sảnh, không buồng | Thang loại 2 | Chỉ khi quy chuẩn cho phép (3.4.16, 3.2.1b) |

## Bẫy đã gặp
- Vòng tròn chồng lên vế thang (như BTB1 khách sạn 10 tầng) có thể chỉ là ký hiệu bán kính, cung quay. Phải nhìn nét bậc chứ không nhìn vòng tròn.
- Nhãn "900 100 900" chỉ cho biết bề rộng vế, không nói có chiếu nghỉ hay không.
- Mặt cắt thang cũng phải xem: vế nghiêng liền mạch qua cao độ chiếu nghỉ là dấu hiệu không có chiếu nghỉ phẳng.
- Không ghi "2 vế thẳng, không rẽ quạt" khi chưa phóng to chiếu nghỉ.
- **Mũi tên đường đi khác bậc chéo.** Chữ V nhỏ ở cuối đường chỉ hướng (đầu mũi tên, cạnh bậc cuối, không chạm tường) chỉ là mũi tên. Bậc chéo là các nét chạy từ đầu tường giữa hai vế ra tới góc hoặc mép tường ngoài, chiếm cả ô chiếu nghỉ. Ví dụ: ST1, ST2 Karaoke SV - Gia Đình có V ở bậc 15, 19, 21 là mũi tên; BTB2 khách sạn 10 tầng là bậc chéo.
- **Chiếu nghỉ bo tròn (bán nguyệt) không có nét bậc** là chiếu nghỉ phẳng. Đo bán kính phải ≥ bản thang (3.4.3). Ví dụ: ST2 ngoài nhà, R 900 = bản 900.
- **Thang hình móng ngựa**: vài bậc thẳng ở hai đầu, giữa là bậc tỏa quanh lõi bán nguyệt. Đây là thang cong một phần, nên KHÔNG thoát nạn (3.3.7), trừ khi đủ mọi điều kiện 3.4.4.
- **Nhãn của người thiết kế** ("thang di chuyển nội bộ, không dùng thoát nạn", "đóng kín thang không sử dụng", "thang sắt thoát nạn") chỉ để đối chiếu. Vẫn phải xếp loại theo hình rồi so với nhãn; lệch thì báo.

## Thang rẻ quạt, bậc chéo: hai điều kiện của QCVN 06 3.4.4 (đọc nguyên văn 2026-10-30)
Hình A.12 TCVN 13967:2024 (trang 48; ảnh `knowledge/hinh/tcvn13967_hinh_A12_thang_re_quat.png`) minh họa rẻ quạt cho công trình bất kỳ có thang thoát nạn (người dùng dặn). A = vùng mặt bậc an toàn để đi, B = dải đánh dấu mép bậc; 9.3.3.3.1 c (trang 30) yêu cầu giải pháp nhận biết mép bậc.
QCVN 06 3.4.4 (`knowledge/hoc/hop-nhat/qcvn-6.md` dòng 990): được dùng thang cong, thang bậc chéo khi thỏa MỘT trong hai:
1) Mỗi bậc có một phần mặt bậc thỏa 3.4.1 (bản thang ≥ 0,9 m; ≥ 0,7 m nếu nhà ≤ 15 m và ≤ 15 người/tầng) và 3.4.2 (mặt bậc ≥ 250 mm, cao ≤ 220 mm). Phần đó chính là vùng A của Hình A.12.
2) Nhóm F1.2, F1.3, F2–F5, cao PCCC ≤ 15 m, ≤ 15 người/tầng: tại mỗi chiếu nghỉ hoặc góc xoay ≤ 90° tối đa 3 bậc chéo (rẻ quạt).
Cách kiểm trên bản vẽ: đếm bậc chéo; nếu > 3 thì tính vùng A: bậc xoay quanh tâm với góc θ mỗi bậc, bề rộng mặt bậc = r·θ, vùng A bắt đầu ở r = 250/θ (mm); bề rộng vùng A = bề rộng vế − r. Đạt điều kiện 1 khi vùng A ≥ 0,7/0,9 m. Thêm dải đánh dấu mép bậc (Hình A.12). Thang xoắn ốc tròn trụ giữa vẫn không tính.

## Quy trình đánh giá thang thoát nạn trong nhà và phương án (người dùng dạy 2026-11-02)
Không chọn thang theo cảm tính, không tự quyết phương án. Làm đủ các bước dưới, mỗi kết luận kèm lý do và điều khoản. Phương án vướng điều luật không nói rõ thì ghi vào báo cáo "cần người dùng xem xét", kèm căn cứ. Chấp nhận hay không là việc của người dùng; việc của mình là báo cáo, không hỏi người dùng chọn (dặn 2026-11-02). Thiếu kiến thức (vd thang loại 2) thì nhờ người dùng train.

1. **Nhận diện từng thang** (bảng, mỗi thang một dòng):
   - Vị trí (thang giữa nhà, thang đầu trái…), chạy từ tầng nào tới tầng nào. Phân biệt thang hầm → trệt và thang các tầng trên → trệt; không chắc thì hỏi.
   - Hình dạng: 2 vế thẳng có chiếu nghỉ phẳng, rẽ quạt (bậc chéo), cong hay xoắn (bảng ở trên).
   - Bề rộng vế, mặt bậc, cao bậc: bản vẽ có số thì ghi số ("vế 900 mm"); không có thì ghi yêu cầu ("vế phải ≥ 900 mm", "mặt bậc ≥ 250 mm, cao bậc ≤ 220 mm").
   - Đóng kín: có tường bao và cửa chưa. Chỉ ghi "cửa chống cháy" khi bản vẽ có ký hiệu (FD, EI…); không có thì ghi "cửa".
   - Chung buồng với thang khác (thang hầm → trệt) không; thang máy nằm trong hay ngoài buồng thang.
2. **Đạt / không đạt từng tiêu chí**: hình dạng (3.3.7, 3.4.4), vế và bậc (3.4.1, 3.4.2), chiếu nghỉ (3.4.3), đóng kín, tách thang hầm (3.2.1, Hình I.2), lối ra ở tầng 1 (3.4.7).
3. **Phương án cho TỪNG thang không đạt, rồi kiểm khả thi trên mặt bằng.** Phần khó nhất là đóng kín buồng thang và tách thang hầm → trệt khỏi thang các tầng trên → trệt. Vẽ thử vị trí tường, cửa rồi kiểm.
   Vẽ ở TẤT CẢ các tầng có thang (hầm, tầng 1, lửng, tầng điển hình, tầng thượng), buồng thang bằng nét liền khép kín, kèm cung vế ở mọi
   đầu vế giáp chiếu tầng (người dùng dặn 2026-11-02; công cụ: skill `tkpccc-ghi-chu-ban-ve`, kiểu `tuong`, `cua`, `cung`). Kiểm:
   - Tường bao mới có chặn cửa căn hộ, phòng bên cạnh không. Chặn thì không hợp lý.
   - Khoảng cách từ mép bậc đầu/cuối vế tới tường ≥ bề rộng bản thang (≥ 0,9 m): chiếu thang rộng ≥ bản thang (3.4.3).
   - Cung mở cửa không lấn vùng chiếu nghỉ: lấy đầu mép bậc phía trong (sát giếng thang) làm tâm, bán kính tới đầu mép bậc còn lại (= bề rộng vế), vẽ cung 1/4. Cánh cửa quét không được cắt vào cung đó (3.4.3: cánh cửa mở vào buồng thang không làm giảm bề rộng tính toán của chiếu và bản thang). Mẫu người dùng gửi: thang CT1, cửa FD1, cung bán kính 900.
   - Thang máy nằm ngoài buồng thang: cửa giếng thang máy E 30 (4.23; bảng giới hạn chịu lửa của cửa: "Cửa giếng thang máy (khi có yêu cầu về giới hạn chịu lửa)" loại 2 = E 30). Thang máy trong buồng thang theo 3.4.6 (≤ 2 thang chở người, chỉ xuống tầng 1, giếng vật liệu không cháy) không theo 4.23. E 30 CHỈ khi thang máy nằm ngoài buồng thang; phương án mở rộng buồng thang ra sảnh thang máy làm thang máy nằm TRONG buồng thang, khi đó xét 3.4.6 chứ không xét E 30 (người dùng sửa 2026-11-02).
   - Tách thang hầm → trệt: vách đặc ngăn cháy loại 1 + lối riêng ra ngoài (3.2.1, Hình I.2). Nhà F1.2, F1.3, F2–F4 cao < 28 m không bố trí được lối riêng thì vào buồng thang chung từ hầm qua khoang đệm bao che như khoang đệm ngăn cháy loại 1. Phải còn chỗ cho tường, cửa mở theo chiều thoát nạn và chiếu ≥ 0,9 m; nhà phố thường không đủ chỗ.
   - Ô lấy sáng KHÔNG phải lý do loại một thang. Không mở được ô ≥ 1,2 m²/tầng (nhà phố xây sát nhau) thì chiếu sáng nhân tạo theo 3.4.8b: cấp điện như chú thích 3.4.13 (duy trì liên tục khi cháy), đủ sáng, không lóa. Buồng thang thông thường thì thêm lỗ thoát khói trên tum thang ≥ 10 % diện tích phủ bì sàn buồng thang; miễn khi nhà có ≥ 2 thang thoát nạn hoặc 1 thang nhưng có lối thoát khẩn cấp theo 3.2.6.2. Ô kính lấy sáng trên mái (buồng thang L2) chỉ dùng khi đạt 3.4.12 (cao ≤ 9 m, tới 12 m nếu lỗ tự mở khi cháy), ít khi đạt.
   - Căn hộ phải mở cửa thẳng vào buồng thang: QCVN 06 3.2.1b có liệt kê "trực tiếp vào buồng thang bộ" là lối ra thoát nạn (tầng 1: 3.2.1a "qua buồng thang bộ"), nhưng người dùng thấy bất hợp lý (đi qua buồng thang rồi ra sảnh). Gặp thì ghi "cần người dùng xem xét", kèm căn cứ.
4. **Không thang trong nhà nào cải tạo được** thì xét thang ngoài nhà (thường ở mặt tiền): tính thử bề rộng vế, bậc, chiếu nghỉ theo luật và các vướng ở tầng 1 (ram dốc, bậc tam cấp). Vẫn không được thì KẾT LUẬN "không có phương án khả thi, cần người dùng xem xét". Không tự chọn một phương án vi phạm.
5. **Thang loại 2 (thang hở trong nhà)**: thỏa điều kiện thì trả lời "được dùng thang hở theo điều …". Điều kiện nhiều: 3.2.1b (hành lang bên, nhà < 28 m), 3.2.6 (thay buồng thang bằng thang loại 2 khi đồng thời đủ các điều kiện; đoạn này trong bản hợp nhất bị lỗi chữ), 3.4.15, 3.4.16. Ngoài điều kiện về thang, đường thoát nạn qua thang loại 2 phải thỏa 3.3.2 + Phụ lục G: L = L1 (trên tầng, tới mép bậc đầu tiên) + 3H (H = chiều cao thang) + L3 (chân thang tới lối ra thật) ≤ giới hạn Phụ lục G; phải cộng cả, không dừng ở L1 (người dùng dạy 2026-11-02; nguyên văn 3.3.2 và ví dụ: `skills/MOD_tinh_toan_khoang_cach.md`). Chưa được train đủ: hỏi người dùng, không tự kết luận.

### Ví dụ: Căn hộ DV Lưu Xuân Lộc (6 tầng nổi + hầm, cao PCCC 18,4 m, ~160 m²/tầng)
| Thang | Nhận diện | Đánh giá | Phương án | Khả thi |
|---|---|---|---|---|
| Giữa nhà (trục 2–3, cạnh thang máy) | Rẽ quạt; vế phải ≥ 900 mm; chưa đóng kín bằng tường và cửa chống cháy | Không đạt: rẽ quạt ở nhà > 15 m (3.4.4), chưa đóng kín | PA1 (nét tím người dùng vẽ): tường sát thang; thang máy nằm ngoài buồng nên cửa E 30 | Không: chặn cửa căn hộ bên phải; mép bậc tới tường ~0,35 m < 0,9 m (đo sơ trên PDF) |
| | | | PA2 (nét xanh lá): mở rộng buồng thang ra sảnh nên thang máy nằm TRONG buồng (xét 3.4.6, không xét E 30; thang máy xuống hầm trong khi 3.4.6 chỉ cho xuống tới tầng 1), sửa rẽ quạt | Cần người dùng xem xét: căn hộ bên phải phải mở cửa thẳng vào buồng thang |
| Đầu trái (trục 4–4a) | Rẽ quạt; vế phải ≥ 900 mm; đã có tường và cửa; chung buồng với thang hầm → trệt | Không đạt: rẽ quạt, chưa tách thang hầm | Sửa rẽ quạt, tách thang hầm bằng tường + cửa chống cháy riêng | Không: không còn chỗ cho tường, cửa mở ra hành lang và chiếu ≥ 0,9 m |
| (Cả hai thang không cải tạo được) | | | PA3: làm thang ngoài nhà ở mặt tiền | Không: thiếu chỗ cho vế, bậc, chiếu; vướng ram dốc 18 %, tam cấp |

Trong bảng đối chiếu, thang ngoài nhà là PHƯƠNG ÁN do mình đề xuất, không phải hạng mục đối chiếu: ghi trong cột Kết luận của dòng "Số lối thoát nạn" (người dùng sửa 2026-11-02).

Kết luận đúng: không có phương án khả thi, cần người dùng xem xét. Lỗi mình đã mắc: chọn thang trái vì "có tường ngoài để lấy sáng" (sai, 3.4.8b cho chiếu sáng nhân tạo) và không kiểm khả thi việc tách thang hầm.

## Đường thoát nạn: đo từ VỊ TRÍ XA NHẤT TRONG PHÒNG (người dùng train 2026-11-03, hồ sơ Q.4)
- Chiều dài đường thoát nạn L1 đo từ ĐIỂM XA NHẤT BÊN TRONG PHÒNG (chỗ xa cửa nhất có người ở: đầu giường, góc phòng), đi theo lối đi
  thật (vòng qua giường, tủ, bàn) ra cửa phòng rồi tới lối ra thoát nạn (cửa buồng thang). KHÔNG đo từ cửa phòng.
- Đường chỉ đi qua SÀN TRỐNG: né giường, bàn ghế, sofa, tủ, bếp, thiết bị trên mặt bằng nội thất (người dùng vẽ nét xanh men theo
  giường, qua khoảng trống giữa WC và bàn ăn, lách giữa ghế và sofa). Không vẽ, không đo đường thẳng chéo qua đồ đạc; đường vòng nào
  dài hơn thì L1 tính theo đường vòng. Phải XEM ảnh mặt bằng nội thất trước khi vẽ, và chạy `cat` phóng vùng để thấy lối đi.
- Vẽ đường này bằng `net` có mũi tên, màu xanh, đầu đường nằm TRONG phòng (người dùng vẽ tay bằng nét xanh dương dày), nhãn "L1 ≈ … m".
- Lỗi đã mắc: Q.4 Lầu 2 đo L1 từ cửa P. ngủ 06 = 7,9 m. Đường người dùng vẽ (từ góc xa trong P. ngủ 06 lên rồi sang trái tới cửa
  buồng thang cạnh P. ăn) đo trên PDF ≈ 2,2 + 5,5 + 1,5 ≈ 9,2 m (tỉ lệ 76,65 px/m ở zoom 1.6). Tính lại L = L1 + 3H + L3 với L1 ≈ 9,2 m
  (PA1 đóng kín buồng thang: L = L1 ≈ 9,2 m ≤ 25 m vẫn ĐẠT; thang hở: L ≈ 34–35 m > 25 m vẫn CHƯA ĐẠT). Mọi bảng, ghi chú, BCKS Q.4 ghi
  7,9 m phải đổi: dòng KS 12, 15; KT7.1 #96; ghi chú "L1 · 7,9 m" trang 5.
- Cửa chống cháy: người dùng dặn "cửa chống cháy là ngay cửa phòng" (đặt ngay tại cửa phòng, không đặt xa ở chỗ khác). Cách áp dụng
  với PA1 đóng kín buồng thang chưa được người dùng nói rõ: khi gặp, ghi vị trí cửa chống cháy tại cửa phòng và "cần người dùng xem xét".

## Tách thang hầm → tầng 1 khỏi thang các tầng trên → tầng 1 (người dùng train 2026-11-05, hồ sơ Nguyễn Viết Thanh)
Ảnh mẫu người dùng vẽ: `knowledge/hinh/tach_thang_ham_tret_PA_tang1.png` (tầng 1), `knowledge/hinh/tach_thang_ham_tret_PA_ham.png` (hầm).
Hình QCVN 06 tham khảo: `knowledge/hinh/qcvn06_hinh_I1_loi_ra_ham_truc_tiep.png` (Hình I.1: lối ra từ hầm trực tiếp ra ngoài), `knowledge/hinh/qcvn06_hinh_I2_loi_ra_ham_qua_sanh.png` (Hình I.2: từ hầm lên sảnh tầng 1 rồi có lối đi riêng ra ngoài).
- MÀU ĐỎ = tường quây kín buồng thang (hầm và tầng 1; ở tầng 1 có thêm vách ngăn cháy loại 1 chia buồng thang làm hai phần: phần thang hầm lên và phần thang các tầng trên xuống).
- MÀU XANH LÁ = cửa đi từ hầm lên tầng 1 (lối ra riêng của thang hầm). MÀU XANH DƯƠNG = cửa đi từ các tầng trên xuống tầng 1. Mỗi cửa có mũi tên ghi "≥ 900 mm".
- Thang hầm chỉ lên tới sàn tầng 1; thang các tầng trên chỉ xuống tới sàn tầng 1; hai phần ngăn bằng vách ngăn cháy loại 1, mỗi phần có cửa riêng ra tầng 1/ngoài nhà (Hình I.1, I.2).
- Vẽ lên PDF: tường ngăn, vách ngăn, cửa chống cháy mỗi thứ có CHÚ THÍCH KÉO MŨI TÊN THẲNG RA ghi rõ "Tường ngăn cháy …", "Cửa chống cháy EI … (từ hầm lên T1)", "(từ các tầng xuống T1)"; ảnh cắt các chỗ đó đưa vào cột GHI CHÚ của BCKS.
- HƯỚNG CỬA VÀ CHIẾU TRỐNG (người dùng sửa 2026-11-05, lỗi: tường đặt sát mép bậc, cửa quét vào vế): tường quây buồng thang đặt CÁCH mép bậc
  đầu vế ≥ 900 mm (≥ bề rộng bản thang) để có chiếu trống mở cửa; không đặt tường sát mép bậc. Cửa ≥ 900 mm mở theo chiều thoát nạn:
  tầng trên và hầm mở VÀO buồng thang, tầng 1 (lối ra) mở RA ngoài buồng thang; cánh cửa quét trong chiếu trống, không chạm vế, không
  lấn chiếu (3.4.3). Chỗ đặt cửa: tường có khoảng trống 900 mm phía trước (thường tường đầu vế phía sảnh, hoặc tường bên ở đầu chiếu);
  phía ngoài cửa mở ra cũng phải còn ≥ 900 mm (vd khe giữa thang máy và buồng thang). Mẫu Nguyễn Viết Thanh: tầng 1 cửa xanh dương ở tường
  trên mở ra sảnh, cửa xanh lá ở tường trái mở ra phía thang máy (khe ≈ 0,92 m); hầm cửa ở tường trái mở vào buồng thang.
- TỰ KIỂM SAU KHI VẼ (lỗi 2026-11-05, Nguyễn Viết Thanh lửng, T2+3, T4): đo CHIẾU TẦNG = khoảng từ tường mới tới mép bậc đầu vế. Phải
  ≥ bề rộng bản thang VÀ cánh cửa mở vào không quét vào phần đó (3.4.3), tức cửa không được nằm ngay đầu vế. Ở đây thang máy nằm sát
  đầu vế nên tường mới chỉ cách mép bậc ≈ 0,8 m, cửa ở tường trên đặt ngay đầu vế trên: mở ra là chạm bậc, không có chỗ đứng. Tầng 1
  (≈ 1,6 m) và hầm (≈ 1,7 m) thì đủ. Khi khe trước đầu vế không đủ: mở rộng buồng thang sang phía sảnh (có thể đưa thang máy vào trong
  buồng thang, xét 3.4.6) hoặc báo "cần người dùng xem xét"; không vẽ cửa ở chỗ không có chiếu trống.
- VẼ ĐÚNG MẪU NGƯỜI DÙNG (lỗi 2026-11-06): khi người dùng đã vẽ mẫu phương án thì vẽ theo đúng mẫu đó (màu: tường quây ĐỎ, cửa từ hầm lên
  XANH LÁ, cửa từ các tầng xuống XANH DƯƠNG; mũi tên ghi ">= 900 mm"; tường quây sát buồng thang, cửa ở tường trên đầu vế mở ra phía sảnh),
  áp cho mọi tầng có thang; không tự nghĩ phương án khác (đã tự mở rộng buồng thang bao thang máy: sai). Chữ trên bản vẽ dùng ">=" thay "≥"
  vì font annotation không có ký tự ≥ (hiện thành "·").
- ĐỌC MẪU KỸ (lỗi 2026-11-06: đọc ảnh mẫu ở độ phân giải thấp, đặt sai cửa): phóng to ảnh mẫu (PIL crop + resize x4) trước khi chép, đổi tọa độ
  ảnh mẫu → px bản vẽ bằng 2 trục lưới. Mẫu Nguyễn Viết Thanh đọc đúng: TẦNG 1 hộp đỏ quanh thang; vách ngăn cháy loại 1 chạy từ mặt giếng
  thang máy theo tay vịn; phần trên có tường trái riêng, cửa xanh dương ở tường TRÊN đầu trái mở ra sảnh; phần dưới kéo sang tới thang máy,
  cửa xanh lá nằm TRONG đoạn vách giữa thang máy và buồng thang, mở lên phía sảnh (đi qua khe thang máy – buồng thang); chiếu trước mỗi cửa
  ≥ 900 mm (hai mũi tên ">=900mm"). HẦM hộp đỏ dạng bậc: đoạn trái thấp (tường trên theo tay vịn), đoạn phải cao tới mép trên thang; cửa xanh
  lá ở tường trên đoạn trái, giữa thang máy và đầu vế, chiếu ≈ 1,0 m. Tầng trên: chiếu từ mặt giếng thang máy tới mép bậc ≈ 0,96 m (đo từ
  mặt ngoài tường giếng, không đo từ tường mới vẽ lệch).
