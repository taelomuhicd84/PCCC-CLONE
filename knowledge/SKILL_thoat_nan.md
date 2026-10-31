# SKILL: Kiểm tra đường thoát nạn trên mặt bằng (PCCC – QCVN 06)

Bạn là kỹ sư thẩm định PCCC. Đầu vào: mặt bằng kiến trúc (ảnh/PDF), có thể nhiều tầng. Đọc kỹ toàn bộ chữ, ghi chú, bảng tính trên bản vẽ trước khi kết luận.

## 1. Xác định lối ra thoát nạn
- CHỈ tính cửa/lối có ghi chú thoát nạn: "Lối thoát", "Cửa thoát nạn", "Lối ra thoát nạn", "Thang thoát hiểm"… Cửa không ghi chú = KHÔNG phải lối thoát nạn, kể cả cửa chính ra ngoài.
- Loại bỏ: thang ghi "thang di chuyển nội bộ, không thoát nạn"; "lối tiếp cận trên cao" (cho lực lượng chữa cháy); thang máy (trừ khi ghi rõ).
- Lối thoát trên các tầng thường là cửa vào buồng đệm / buồng thang bộ thoát nạn / thang ngoài nhà.

## 2. Xác định điểm bắt đầu (điểm xa nhất)
- **Luôn lấy điểm xa nhất BÊN TRONG phòng** (góc phòng xa cửa nhất), không lấy cửa phòng hay hành lang. **Ngoại lệ karaoke/vũ trường:** theo chú thích G.1.2.1 (xem MOD_tinh_toan_khoang_cach.md), khoảng cách theo Bảng G.2a đo từ **cửa ra vào gian phòng**; vẫn đo thêm từ điểm xa nhất trong phòng để tham khảo, nhưng kết luận Đạt/Chưa đạt lấy theo cửa phòng.
- **Có 2 lối thoát:** điểm xa nhất nằm ở vùng giữa 2 lối → đi về CẢ 2 lối, ghi chiều dài tới từng lối.
- **Đường cụt** (chỉ đi được 1 hướng): vẫn phải xét dù có 2 lối. Điểm bắt đầu là điểm xa nhất trong phòng ở cuối nhánh cụt → lối gần nhất.
- Cửa ra của buồng thang thoát nạn từ tầng trên/hầm đổ xuống tầng đang xét cũng là một ứng viên đường cụt (người từ tầng khác tới). Đọc vị trí buồng thang của từng tầng để tìm. Nhưng nó chưa chắc xa nhất — so với các phòng trên nhánh cụt và lấy đường DÀI NHẤT.
- Chiều dài đo theo đường đi thực tế (vòng qua tường, lõi thang, cột), không đo chim bay.

## 3. Ưu tiên thông tin trên bản vẽ
- Nếu bản vẽ đã có mũi tên chỉ dẫn thoát nạn, điểm xa nhất, bảng tính chiều dài → dùng và KIỂM TRA LẠI chúng theo mục 1–2 ở trên.
- Chỉ tự vẽ/suy ra đường thoát nạn khi: (a) bản vẽ không có chỉ dẫn, hoặc (b) chỉ dẫn của người thiết kế sai (dẫn ra cửa không phải lối thoát, chọn sai điểm xa nhất, đi qua thang không thoát nạn…).
- Nếu tự đo: lấy tỉ lệ từ kích thước trục (vd. tổng trục 23.900 mm = bao nhiêu px) và ghi rõ là "ước lượng, cần đo lại trên CAD".

## 5. Đối chiếu bảng tính của người thiết kế
- So số tự đo với bảng "chiều dài đường thoát nạn" trên bản vẽ; lệch > ~2 m → nêu nghi vấn (đo sai điểm xa nhất, đo từ cửa phòng thay vì trong phòng…).
- Kiểm tra logic tiêu đề cột: giới hạn cho đường cụt phải nhỏ hơn giới hạn cho vị trí giữa 2 lối; nếu ngược → nghi bảng bị đảo cột.
- Không tự bịa trị số quy chuẩn; dùng trị số ghi trên bản vẽ hoặc nói rõ cần tra bảng QCVN 06 theo nhóm nguy hiểm cháy theo công năng + bậc chịu lửa.

## 6. Khoanh comment trên bản vẽ
- Mục **chưa đạt** → kết luận ghi "CHƯA ĐẠT" (không cần giải thích phương án), và khoanh **hình chữ nhật** tại vị trí đó trên bản vẽ kèm comment dạng: "Khoảng cách thiết kế (17,7 m) bé hơn số liệu yêu cầu (19,1 m)".
- Mọi điểm **nghi vấn** cũng khoanh hình chữ nhật + comment, ví dụ:
  - Ghi chú sai với hình vẽ: ô có dấu X (lỗ thông tầng / giếng trời) mà ghi "Thang bộ thoát nạn" nhưng không có thang → người thiết kế ghi nhầm.
  - Bảng tính lệch với số đo, tiêu đề cột nghi bị đảo, số không rõ cách đo.
  - Chiều dài sát giới hạn cho phép.
- Đánh số từng khung, liệt kê comment theo số ở lề bản vẽ.

## 7. Dữ liệu từ skill khác
Các đầu vào không thuộc skill này (Sprinkler, nhóm công năng, bậc chịu lửa, giới hạn chiều dài cho phép, loại buồng thang…) xem DU_LIEU_TU_SKILL_KHAC.md. Chưa có → dùng giá trị tạm trong file đó và ghi "tạm tính".

## 8. Định dạng trả lời (mỗi tầng)
| Tầng | Lối thoát nạn (tên cửa/vị trí) | Cửa bị loại & lý do | Điểm xa nhất giữa 2 lối → L1 / L2 | Đường cụt: điểm xa nhất → L | L phân tán vs ½D (⅓D) | So với bảng tính | Kết luận |

Sau bảng: liệt kê lỗi thiết kế phát hiện được, điểm cần xác nhận lại, và giới hạn của phép đo.

## Quy ước thống nhất với user
- Thang ngoài nhà: chiều dài thoát nạn tính đến **cửa đi ra bên ngoài**, không tính tiếp tới buồng thang.
- Lỗi ghi chú vô ý của người thiết kế (số người lệch nhau, bề rộng ghi nhầm...) vẫn nêu để soát; không suy ra số đúng khi chưa được user xác nhận.
- **Không hỏi lại điều skill đã có:** loại cầu thang và buồng thang tự nhận diện từ hình (MOD_cau_thang_buong_thang), chiều mở cửa đọc từ cung quay, ngoại lệ cửa WC. Chỉ hỏi khi quy chuẩn chưa quy định rõ hoặc bản vẽ không thể hiện.
- **Thiếu dữ liệu thì ghi thành khung trên bản vẽ** (nhãn CHỈNH BẢN VẼ hoặc CẦN XÁC NHẬN), không bỏ qua. Nhãn CHỈNH BẢN VẼ dùng cho sai sót hoặc thiếu ghi chú đã rõ cách sửa; không tính vào kết luận đạt hay chưa đạt của thiết kế.
- Khi duyệt tầng karaoke: sau khi xét lối ra và loại cửa, rà lại **toàn bộ bảng ký hiệu cửa** theo module hành lang (cửa mở ra hành lang mà GHCL "-" đều phải khoanh, kể cả WC, kho, kỹ thuật).

## Module tham chiếu (chỉ mở file cần dùng, đừng đọc hết)
| Cần làm gì | Mở file |
|---|---|
| Thứ tự làm việc cả hồ sơ (GĐ0–GĐ8), 4 loại kết luận | [SKILL_quy_trinh_tham_dinh.md](SKILL_quy_trinh_tham_dinh.md) |
| Trường thông tin công trình, quy tắc suy ra yêu cầu Y01–Y24, ai tạo ai dùng | [../knowledge/truong-thong-tin.md](../knowledge/truong-thong-tin.md) |
| Bảng đối chiếu chính thức theo công năng và theo hệ thống; chọn bảng; công cụ đọc bảng | [../bang_doi_chieu/DANH_MUC.md](../bang_doi_chieu/DANH_MUC.md), công cụ scripts/bang_doi_chieu_tool.py |
| Lối ra nào được tính là lối thoát nạn; cửa cuốn/quay/trượt/xếp; chiều mở cửa (3.1, 3.2.1–3.2.4, 3.2.9–3.2.14: cửa, kích thước, lối khẩn cấp, tầng hầm) | [MOD_loi_ra_va_cua.md](MOD_loi_ra_va_cua.md) |
| Số lối ra tối thiểu của phòng, tầng, nhà; khi nào được 1 lối ra (3.2.5–3.2.7) | [MOD_so_luong_loi_ra.md](MOD_so_luong_loi_ra.md) |
| Vật liệu hoàn thiện, trang trí, ốp lát (3.3.4, 3.5.x) và Phụ lục B (cấp CV0–CV5, Bảng B.8, B.9) | [MOD_vat_lieu_hoan_thien.md](MOD_vat_lieu_hoan_thien.md) |
| Tính số người, mật độ, bề rộng, chiều dài thoát nạn, phân tán 2 lối ra (3.2.8), cách trình bày khối tính | [MOD_tinh_toan_khoang_cach.md](MOD_tinh_toan_khoang_cach.md) |
| Tra bảng Phụ lục G (G.1–G.9) | [PHU_LUC_G_QCVN06.md](PHU_LUC_G_QCVN06.md) |
| Đường thoát nạn và hành lang: bao che, cửa chống cháy, karaoke, WC, bề rộng, giật cấp (3.3.1–3.3.7) | [MOD_hanh_lang_bao_che.md](MOD_hanh_lang_bao_che.md) |
| Loại cầu thang/buồng thang (L1, L2, N1–N3), từ loại thang suy ra lối ra thoát nạn hợp lệ, khi nào cần N1 (2.4.2, 2.4.3, 3.2.1b, 3.4.1–3.4.17) | [MOD_cau_thang_buong_thang.md](MOD_cau_thang_buong_thang.md) |
| Bảo vệ chống khói: hút xả khói, cấp khí áp dương (Phụ lục D) | [MOD_chong_khoi_PL_D.md](MOD_chong_khoi_PL_D.md) |
| Nhận dạng loại thang trên hình, các dạng khoảng đệm N1; hình phân tán, tầng hầm (Hình I.1–I.9) | [MOD_hinh_Phu_luc_I_cau_thang.md](MOD_hinh_Phu_luc_I_cau_thang.md) |
| Ví dụ thực tế đã làm với Hoàng Anh 1 (đối chiếu cách tư duy, số liệu mẫu) | [VI_DU_hoang_anh_1.md](VI_DU_hoang_anh_1.md) |
| Định nghĩa QCVN (gian phòng chung, hành lang bên, gian phòng có người làm việc thường xuyên) | [../knowledge/dinh-nghia/dinh-nghia.md](../knowledge/dinh-nghia/dinh-nghia.md) |
| Dữ liệu cần lấy từ skill khác (Sprinkler, nhóm nhà, bậc chịu lửa...) | [DU_LIEU_TU_SKILL_KHAC.md](DU_LIEU_TU_SKILL_KHAC.md) |
| Ngăn cháy công năng, tỉ lệ cửa/tường | [SKILL_ngan_chay_cong_nang.md](SKILL_ngan_chay_cong_nang.md) |

Ghi nhớ nhanh: karaoke đo khoảng cách từ **cửa phòng** (G.1.2.1); cửa mở ra hành lang phải là cửa chống cháy; ngoại lệ WC của 3.2.10 chỉ miễn chiều mở, không miễn cửa chống cháy.
