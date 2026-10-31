# Sơ bộ thiết kế PCCC (Mục 4: đường, bãi đỗ xe chữa cháy, bãi quay xe, bãi tránh xe)

Mục riêng ở tab "1. Hồ sơ", không phải việc thẩm định: nạp kiến thức này chỉ khi người dùng dùng mục "Sơ bộ thiết kế PCCC" (nặng prompt, không nạp mặc định). Khi dùng: agent đọc file này, đề xuất phương án và ghi chú lên bản vẽ cho người thiết kế.

Nội dung: từ mặt bằng tổng thể + số liệu công trình, đề xuất vị trí bãi đỗ xe chữa cháy, làn xe, bãi quay xe, bãi tránh xe và ghi chú lên bản vẽ để người dùng xem. Đây là PHƯƠNG ÁN ĐỀ XUẤT sơ bộ, không phải kết luận thẩm định. Quy tắc nền: `bai-hoc.md` (Bài học 12–20), `qcvn06-dieu-6.md` (Đ 6.2, 6.4, 6.5).

## Số liệu đầu vào cần có (thiếu thì hỏi, không đoán)
Nhóm nhà (VD F5.2), chiều cao PCCC, chiều cao công trình, diện tích sàn, dài x rộng nhà, chu vi, khối tích, có hay không sprinkler. Khối tích chưa có thì lấy diện tích x chiều cao công trình và ghi "ước tính". Chu vi = (dài + rộng) x 2.

## Tính yêu cầu
- Chiều dài bãi đỗ: nhà F5 theo Bảng 16 (khối tích <= 28.400 m3 và có/không sprinkler: 1/6 chu vi, không nhỏ hơn 15 m). Nhóm F1–F4 theo Bảng 15 (diện tích sàn). Mỗi bãi >= 15 m, được chia nhiều bãi cộng dồn đủ chiều dài yêu cầu.
- Chiều rộng bãi: Bảng 14, cao PCCC <= 15 m không yêu cầu riêng, lấy bằng đường 3,5 m; > 15 m thì >= 6 m.
- Cự ly mép đường/bãi đến tường: <= 5 m (cao PCCC < 12 m), <= 8 m (12–28 m), <= 10 m (> 28 m).
- Chỉ đoạn đường thông thủy >= 3,5 m (cao thông thủy > 4,5 m, dốc <= 1:8,3, chịu tải xe chữa cháy) mới là đường cho xe chữa cháy (Bài học 20).

## Đọc bản vẽ để định vị
1. Tìm tỷ lệ: bản in có thể bị thu nhỏ (VD ghi 1/250 trên A4 nhưng thực tế khoảng 16–25 px/m). Luôn suy tỷ lệ từ một dim đã biết (chiều dài nhà, nhãn 41500, 7000...) rồi đổi mét <-> điểm ảnh; ghi sai số khoảng +-1 m.
2. Trang xoay: xoay trang cho chữ đọc thẳng trước khi vẽ (PyMuPDF `set_rotation`).
3. Đọc hatch/chú giải để biết đâu là đường, sân, cỏ (Bài học 17). Chỉ vùng đường/sân thông thủy >= 3,5 m mới dùng làm làn xe.
4. Đo khoảng hở giữa nhà và hàng rào/bể/nhà khác ở bốn phía: khoảng < 3,5 m không phải đường, nên đường vòng bị đứt tại đó (Bài học 19–20).
5. Làn 3,5 m có thể là khoảng giữa hai nhãn 3500; xe đỗ hai bên làn không làm mất làn (Bài học 18).

## Quy tắc đặt phương án
- Ưu tiên bãi đỗ dọc cạnh nhà có sân rộng, gần cổng; mép bãi cách tường 1–5 m (đạt cự ly); mép ngoài bãi cách hàng rào tối thiểu vài mét để không vướng.
- Không bắt buộc bãi (chú thích 1 Bảng 14: <= 50 người/tầng và đường→họng tiếp nước <= 18 m) thì KHÔNG vẽ, không khoanh bãi (Bài học 23); chỉ ghi đường tiếp cận và căn cứ miễn.
- Chia 2 bãi (mỗi bãi >= 15 m) khi một phía không đủ chiều dài, hoặc một phía có vật cản tạm (xe container).
- Làn xe: từ cổng chính nối thẳng tới bãi; ghi bề rộng cổng, bán kính cong, tải trọng.
- Đường vòng liền mạch và ra được thì không cần bãi quay xe. Đường cụt (đi một chiều, hết đường quay lại) dài > 46 m thì cần bãi quay xe: tam giác đều cạnh 7 m (đỉnh nhọn ở đầu cụt, đáy 7 m ngang đường, nằm giữa bề rộng đường, không ép góc), vuông 12 m, tròn đường kính 10 m, chữ nhật 5x20 m (quy định cũ đã bị thay, chỉ THAM KHẢO; Đ 6.4 hiện hành: phù hợp xe địa phương). Đường cụt <= 46 m thì không cần.
- Bãi tránh xe 7x8 m chỉ cần khi đường một làn dài >= 100 m (Đ 6.5).
- Bán kính quay xe: chưa có số xe thật của PCCC địa phương; không tự đặt số. Mô phỏng thử với xe giả định 7 x 2,5 m cho thấy tam giác 7 m rất chật; nêu CẦN XÁC NHẬN xe thật (xem `NOTE_CUA_TOI.md`).

## Cách ghi chú lên ảnh
- Màu: bãi đỗ xanh lá (khung tô trong mờ), làn xe xanh dương (mũi tên), cự ly mép bãi–tường đỏ (đoạn có hai đầu gạch), bãi quay xe đỏ (tam giác), đoạn hẹp/không dùng gạch chéo đỏ hoặc cam nét đứt.
- Nhãn có dấu tiếng Việt: dùng font Arial (`C:\Windows\Fonts\arial.ttf`) khi vẽ bằng PyMuPDF; chữ đặt trên nền trắng mờ để đọc được trên hatch.
- Một khung đỏ ghi phép tính: S, chu vi, khối tích, dài bãi theo bảng, cự ly, kết luận đường cụt/vòng và bãi quay xe, bãi tránh xe.
- Ghi rõ điều cần xác nhận: bề rộng cổng, tải trọng, chiều cao thông thủy, độ dốc, lối vào từ trên cao, sai số tỷ lệ.
- Không dùng Gemini để khoanh vùng (khung lệch nhiều, bịa khung ngoài bản vẽ); dùng Claude đọc ảnh.

## Ví dụ đã làm
- Hồ sơ test (NM1, cao PCCC 11,5 m, chu vi 144 m): 2 bãi 15 m (Bắc, Nam), 2 bãi quay xe tam giác 7 m ở hai góc phía Tây, không cần bãi tránh xe. `ket_qua/cong_trinh_test/thiet_ke_bai_do_v3.png`.
- Kho Tân Kiên (nhà kho F5.2, cao PCCC 11 m, 770 m2, chu vi 120,8 m, có sprinkler): 1 bãi 21 m x 3,5 m dọc tường nam; khe Bắc 1,5 m và khe Đông–Bắc 1,8 m (< 3,5 m) không phải đường nên không có đường vòng; đường cụt ~37 m < 46 m nên không cần bãi quay xe. `ket_qua/kho_tan_kien/thiet_ke_m04_kho_tan_kien_v2.png`.

## Tư vấn thiết kế PCCC: form 7 mục
Người dùng nói "tư vấn thiết kế PCCC" thì trình bày theo form 7 mục ở `knowledge/khao-sat-bao-gia.md` (mục "Form tư vấn thiết kế PCCC"), không trả lời rời rạc.

## Thoát nạn: mẫu trình bày
Chọn phương án: chạy `python scripts/mau_giai_phap.py loc --F <nhóm F> --cao <m> --tang <n> --dt <m2/tầng> --nguoi <người/tầng> --ham 0|1 --cong-nang <chữ>` (bỏ tham số chưa biết), CHỈ dùng các mẫu trả về (yêu cầu, nhận diện, lỗi hay gặp, giải pháp); mẫu có `can_xac_nhan` thì hỏi số liệu đó. Mục lục: `muc-luc`. Nguồn `data/mau_giai_phap/*.json`, xem `thu_vien.html`.
Theo Bài học 24–25 (`bai-hoc.md`): điểm xa nhất, tuyến góc vuông, L từng đoạn, khung tính toán, chú giải, chi tiết cửa. Ví dụ: `ket_qua/the_sun/thiet_ke_thoat_nan_the_sun_lau1.png`, `ket_qua/mn_vs/thiet_ke_thoat_nan_mn_vs_tang3.png`.

## Bảng thông tin PCCC (form đầu ra của bot chat mục 7)
Khi người dùng cho quy mô (hoặc bảo "tự ướm chừng") và công năng từng tầng, bot lập BẢNG THÔNG TIN PCCC theo form của hồ sơ thiết kế (mẫu KT-03 The Sun; ví dụ `ket_qua/hoang_hung_tk/bang_thong_tin_pccc.md`):
1. Bảng theo tầng: TẦNG | DIỆN TÍCH (m2) | CHIỀU CAO (m) | KHỐI TÍCH (m3) = diện tích x chiều cao | TỈ LỆ DT SÀN (%) = diện tích tầng / tổng diện tích sàn | CÔNG NĂNG | NHÓM NHÀ (F1.2 khách sạn, F3.2 nhà hàng, F5.1 kỹ thuật...). Hàng TỔNG: tổng diện tích và tổng khối tích.
2. Bảng chỉ tiêu: tổng số tầng; chiều cao PCCC (cao độ sàn tầng trên cùng có người so với mặt đất, ghi phép cộng); chiều cao mái (ước tính); tổng khối tích; nhóm nhà (nhà hỗn hợp liệt kê từng nhóm); bậc chịu lửa (tra Bảng H.2 Phụ lục H theo chiều cao PCCC và diện tích tầng; khách sạn bậc II S0 được đến 50 m, không mặc định bậc I); cấp nguy hiểm cháy kết cấu (S0); sàn tầng lớn nhất; số người giả định mỗi tầng và tổng.
3. Hệ quả sơ bộ: bãi đỗ xe chữa cháy (bắt buộc khi cao PCCC > 28 m; chú thích 1 Bảng 14 chỉ miễn cho nhà cao > 15 m đến 28 m), chiều dài theo Bảng 15/16, rộng theo Bảng 14, lối vào từ trên cao theo tầng, thang máy chữa cháy khi cao PCCC > 28 m (Đ 6.13), số lối ra mỗi tầng (Đ 3.2.6.1), đường thoát nạn.
4. Quy ước: số liệu người dùng cho thì dùng nguyên; thiếu thì tự ướm chừng và ghi rõ "giả định/ước tính" trong bảng (kích thước theo mặt bằng chữ L trên bản vẽ, chiều cao tầng, số người). Số thập phân dùng dấu phẩy, hàng nghìn dấu chấm.

## Hướng phát triển (người dùng dặn 2026-10-23)
Bot chat mục 7 sẽ là nền cho tính năng BÁO CÁO KHẢO SÁT và BÁO GIÁ: bảng thông tin PCCC và các hệ quả sơ bộ (bãi đỗ, thang máy chữa cháy, số lối thoát nạn, hệ thống cần trang bị) là dữ liệu đầu vào để suy ra danh mục hạng mục và khối lượng. Giữ đầu ra có cấu trúc, ghi rõ giả định để sau này đọc lại.
