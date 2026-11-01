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
