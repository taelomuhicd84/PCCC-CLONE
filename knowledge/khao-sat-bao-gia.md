# Báo cáo khảo sát PCCC (tab 8)

Không phải thẩm định. Từ bản vẽ (hoặc thông tin bằng chữ) lập số liệu cho file Excel báo cáo khảo sát; báo giá lấy từ danh sách hệ thống ở đây. Mẫu Excel thật do người dùng cấp sau. Dùng tri thức nạp kèm (QCVN 06, QCVN 10:2025, NĐ 105/2025 + NĐ 347/2026 và các skill thoát nạn); không bịa trị số.

## Các thông tin phải lấy (theo đúng thứ tự)
1. **Hạng mục** (tên công trình/hạng mục khảo sát), **công năng** (F1.x–F5.x theo thuật ngữ trong `dinh-nghia`; nêu công năng thực tế bằng chữ kèm nhóm F nếu xác định được).
2. **Diện tích** (tổng sàn; nêu cả diện tích từng tầng nếu bản vẽ có) và **số tầng** (nổi, hầm, tum/mái nếu có).
3. **Chiều cao PCCC** (theo Điều 3 QCVN 06: từ mặt đường xe chữa cháy đến cao độ sàn tầng trên cùng có người, ghi phép cộng). Không có mặt đứng: ước lượng khoảng (xem mục "Ước lượng chiều cao").
4. **Khối tích**: lấy trên bản vẽ nếu có; không có thì tự tính = diện tích x chiều cao, ghi rõ "tự tính" và phép tính (từng tầng nếu có diện tích và chiều cao từng tầng, không thì tổng diện tích x chiều cao công trình).
5. **Có thuộc diện thẩm định thiết kế PCCC không**: đối chiếu Phụ lục III NĐ 105/2025 (đã sửa bởi NĐ 347/2026, file `nghi-dinh-tham-dinh-dieu-chinh.md`); nhà hỗn hợp, nhà có tầng chủ hộ ở, dự án nhiều hạng mục, cải tạo mở rộng: xét theo `cv-2360-cahn-1183-c07-dien-tham-dinh.md` (C07 thống nhất) theo loại công trình, chiều cao, diện tích, khối tích, số tầng. Trả CÓ / KHÔNG / CẦN XÁC NHẬN kèm căn cứ (mục, điểm của Phụ lục III). Thiếu số liệu quyết định thì CẦN XÁC NHẬN và nói thiếu gì.
6. **Số lối thoát nạn** thấy trên bản vẽ (đếm buồng thang bộ thoát nạn và lối ra thẳng ngoài nhà; ghi từng lối ở tầng nào, loại nào) và **đánh giá đảm bảo yêu cầu không**:
   - Trước khi đếm: xét TỪNG thang theo MOD_cau_thang_buong_thang (loại thang, lối ra 3.2.1, 3.4.1–3.4.3, 3.4.7; thang rẽ quạt/bậc chéo thêm 3.4.4). Thang không đạt thì không tính; thiếu kích thước bậc thì thang đó CẦN XÁC NHẬN, ghi "x lối đạt + y lối chờ xác nhận" (Bài học 26). Đếm theo tầng: tầng trên = số buồng thang đạt; tầng trệt = số lối ra ngoài nhà; tầng hầm = số thang lên tầng 1 hoặc lên thẳng sân. Ghi số lối từng tầng.
   - Yêu cầu số lối theo MOD_so_luong_loi_ra (QCVN 06 mục 3.2.5–3.2.8): số lối tối thiểu của gian phòng, tầng, nhà; phân tán; khoảng cách theo Phụ lục G.
   - Bản vẽ có đủ số liệu (số lối, vị trí, khoảng cách, bề rộng) thì kết luận ĐẠT / CHƯA ĐẠT từng ý.
   - Bản vẽ KHÔNG có thông tin bề rộng (cửa, hành lang, thang) thì KHÔNG kết luận phần bề rộng: chỉ ghi yêu cầu theo QCVN 06 (bề rộng tối thiểu và cách tính theo số người thoát) để người thiết kế bổ sung.
7. **Hệ thống PCCC phải lắp đặt**: với từng hệ thống ghi CÓ / KHÔNG / CẦN XÁC NHẬN, căn cứ (điều, bảng, phụ lục) và ghi chú:
   báo cháy tự động; chữa cháy họng nước vách tường; chữa cháy tự động (sprinkler, drencher); cấp nước chữa cháy ngoài nhà (trụ, bể); trạm bơm; hút xả khói, tăng áp thang; chiếu sáng sự cố và chỉ dẫn thoát nạn; bình chữa cháy; cấp điện cho PCCC; chống sét; thang máy chữa cháy; đường và bãi đỗ xe chữa cháy; ngăn cháy (cửa, tường, sàn) và vật liệu.
8. **Bể nước PCCC**: nếu có hệ thống chữa cháy dùng nước (họng nước, sprinkler, ngoài nhà) thì ghi "CẦN TÍNH BỂ NƯỚC PCCC" và liệt kê hệ thống dùng nước. Chưa tự tính dung tích: sẽ có skill riêng tính bể nước. Khi người dùng đã cung cấp dung tích thì ghi nguyên.

## Ba loại đầu vào
1. Mặt bằng + mặt đứng: đọc chiều cao và số tầng từ mặt đứng, mặt cắt.
2. Chỉ có mặt bằng các tầng (không mặt đứng): số tầng = số mặt bằng tầng; chiều cao ước lượng.
3. Chỉ có diện tích và số tầng bằng chữ (hoặc một mặt bằng điển hình + số tầng): chia diện tích cho số tầng nếu chưa có diện tích từng tầng; chiều cao ước lượng. Không có bản vẽ thì không đếm được lối thoát nạn: ghi CẦN XÁC NHẬN, nêu yêu cầu số lối theo QCVN 06.

## Ước lượng chiều cao (loại 2 và 3)
- Một tầng khoảng 3,5 m; cho KHOẢNG 3,3–3,8 m (không chốt một số).
- Chiều cao công trình ≈ số tầng x 3,3 đến 3,8 m (chưa cộng tum, mái dốc).
- Chiều cao PCCC ≈ (số tầng - 1) x 3,3 đến 3,8 m (tính tới sàn tầng trên cùng, không tính mái; tầng hầm không cộng). Tầng kỹ thuật hoặc trệt cao bất thường thì nói rõ và nới khoảng.
- Nếu khoảng này nằm hai bên một ngưỡng của quy chuẩn (vd. 15, 28 m) thì kết luận liên quan là CẦN XÁC NHẬN.

## Form tư vấn thiết kế PCCC (người dùng chốt 30/10/2026)
Khi người dùng nói "tư vấn thiết kế PCCC" kèm bản vẽ: trình bày báo cáo đúng 7 mục, đúng thứ tự, lưu `ket_qua/<cong_trinh>/bao_cao_khao_sat.md`:
1. Thông tin công trình (bảng theo tầng: diện tích, công năng, nhóm F; chiều cao PCCC, khối tích, bậc chịu lửa)
2. Thuộc diện thẩm định PCCC không (Phụ lục III NĐ 105/2025, cơ quan thẩm định; không thuộc thì thuộc diện quản lý của ai)
3. Bãi đỗ xe chữa cháy (và đường, lối vào từ trên cao)
4. Thoát nạn (số lối, phân tán 3.2.8, loại thang N1/N2/N3 đối chiếu QCVN 06 3.4.7–3.4.10, thang rẻ quạt 3.4.4, cửa, hành lang, hút khói hành lang D.1.2, lối ra mái)
5. Ngăn cháy: công năng (4.5; kho, labo > 50 m², bếp đun > 10 kW mới thành F5; tầng có công năng chính ≥ 90 % thì phụ trợ không phải ngăn) + phương đứng, ngang (buồng thang, giếng thang máy, hộp kỹ thuật, 4.33; sàn ngăn cháy giữa công năng ở và công cộng)
6. Khoảng cách PCCC
7. Hệ thống PCCC yêu cầu (QCVN 10:2025 Bảng A.1, B.1; bể nước)
Bổ sung thêm khi thiếu mục nào trong bảng đối chiếu: bậc chịu lửa, bố trí công năng, thang máy chữa cháy, chống tụ khói, CNCH, chống sét, liên lạc, biển quảng cáo.
(Thứ tự theo lời người dùng nói lại 31/10/2026; bản ghi 30/10 xếp "ngăn cháy" thành hai mục 4–5 và gộp thoát nạn, khoảng cách vào mục 7. Lệch thì theo người dùng.)
Trước khi viết: chạy `python -I -X utf8 scripts/mau_giai_phap.py loc …`, tra thẻ bản vẽ mẫu cùng công năng (`knowledge/ban-ve-mau.md`), bài học `knowledge/hoc/du-an/so-bo-thiet-ke.md`. Mẫu văn phong: `ket_qua/hoang_hung_so_bo2/tu_van_thiet_ke_v3.md` (nếu máy có).

## Định dạng trả lời
Phần chữ ngắn (tóm tắt từng mục 1–8, mục nào là ước lượng thì ghi "ước lượng"), rồi thêm đúng MỘT khối ```json ở cuối để điền Excel:
```json
{"khao_sat":{"hang_muc":"","cong_nang":"","dien_tich_m2":0,"so_tang":0,"chieu_cao_pccc":"","khoi_tich":"","dien_tham_dinh":"CÓ|KHÔNG|CẦN XÁC NHẬN","dien_tham_dinh_can_cu":"","so_loi_thoat_nan":0,"loi_thoat_nan":"theo tầng: tầng trên N buồng thang đạt; tầng trệt N lối ra ngoài; hầm N thang lên","thoat_nan_danh_gia":"ĐẠT|CHƯA ĐẠT|CẦN XÁC NHẬN","thoat_nan_ghi_chu":"yêu cầu QCVN 06, phần bề rộng nếu bản vẽ thiếu","he_thong":[{"ten":"","co":"CÓ|KHÔNG|CẦN XÁC NHẬN","can_cu":"","ghi_chu":""}],"be_nuoc_pccc":"CẦN TÍNH BỂ NƯỚC PCCC (…) hoặc dung tích người dùng đã cho"}}
```
Số không biết thì để null hoặc chuỗi rỗng, không điền 0.

## Quy ước
- Số người dùng cung cấp thì dùng nguyên; thiếu thì hỏi đúng một câu ngắn hoặc ước lượng có nhãn "ước lượng".
- Chỉ trích điều khoản có trong tri thức nạp kèm; chưa có thì ghi "cần tra <văn bản>".
