# Học từ bộ bản vẽ PCCC mẫu của người dùng (14/11/2026)

Người dùng đưa 2 nơi để học, KHÔNG được sửa file gốc:
- thư mục `file_cad_test/` trong repo (gitignore);
- thư mục hồ sơ thẩm duyệt một xưởng trên Drive công ty.

Gồm 5 bản PCCC (kho, xưởng sản xuất, nhà xưởng bao bì) + 1 bản kiến trúc. Bản chép để học ở `ket_qua/cad_test/hoc_mau/` (gitignore). Công cụ dùng:
- `dump2.lsp`: dump layer, đối tượng, block, MTEXT đủ các đoạn mã 3.
- `tom.py`: tóm tắt layer, block, chữ.
- `chu_dai.py`: gom các đoạn ghi chú dài.
- `to_chon.py` + `chay_scr.ps1`: in PNG từng tờ bằng accoreconsole.

Văn mẫu thuyết minh chép nguyên văn ở `cad/skills/ve/mau-thuyet-minh-pccc.md`.
Trị số phải đối chiếu `knowledge/` (TCVN 7568-14:2025, QCVN 10:2025, TCVN 13456:2022, TCVN 7336:2021) trước khi dùng.

## 1. Bộ tờ của một hồ sơ (thứ tự hay gặp)

Model space xếp tờ theo hàng; mỗi hàng là một hệ, có nhãn đỏ bên trái. Mỗi tờ là một khung A1/A3 có khung tên bên phải. Tên các tờ:

- **Thuyết minh công trình:** bảng THỐNG KÊ CÔNG TRÌNH (khu vực, diện tích, số tầng, chiều cao, khối tích, công năng, diện tích khu đất) + mặt bằng tổng thể sạch.
- **Báo cháy:**
  - MẶT BẰNG TỔNG THỂ BÁO CHÁY;
  - MẶT BẰNG BÁO CHÁY (TRÊN TRẦN / DƯỚI TRẦN) từng khối;
  - MẶT BẰNG DẦM TRẦN (khi có dầm);
  - MẶT CẮT LẮP ĐẶT BÁO CHÁY;
  - SƠ ĐỒ NGUYÊN LÝ HỆ THỐNG BÁO CHÁY;
  - THUYẾT MINH HỆ THỐNG BÁO CHÁY;
  - CHI TIẾT LẮP ĐẶT.
- **Chữa cháy:**
  - MẶT BẰNG TỔNG THỂ CHỮA CHÁY (SPRINKLER / VÁCH TƯỜNG);
  - MẶT BẰNG CHỮA CHÁY từng tầng hoặc khối;
  - CHI TIẾT LẮP ĐẶT SPRINKLER TẠI CỘT ĐIỂN HÌNH;
  - MẶT CẮT ĐIỂN HÌNH;
  - SƠ ĐỒ NGUYÊN LÝ HỆ THỐNG CHỮA CHÁY;
  - SƠ ĐỒ KHÔNG GIAN SPRINKLER;
  - SƠ ĐỒ ĐIỂM NÚT TÍNH TOÁN THỦY LỰC;
  - CHI TIẾT CỤM VAN TÍN HIỆU + CÔNG TẮC DÒNG CHẢY.
- **Trạm bơm:**
  - MẶT BẰNG / MẶT CẮT PHÒNG BƠM PCCC;
  - CHI TIẾT CỤM BƠM;
  - SƠ ĐỒ NGUYÊN LÝ BƠM;
  - SƠ ĐỒ 1 SỢI + SƠ ĐỒ NGUYÊN LÝ CẤP ĐIỆN PCCC;
  - GHI CHÚ VÀ KÝ HIỆU MẠCH ĐIỀU KHIỂN, HƯỚNG DẪN VẬN HÀNH.
- **Đèn:**
  - MẶT BẰNG BỐ TRÍ ĐÈN EXIT-EM tổng thể và từng khối;
  - THUYẾT MINH ĐÈN EXIT-EM (có tính độ rọi, chiều cao biển, độ chói);
  - CHI TIẾT LẮP ĐẶT ĐÈN.
- **Phương tiện ban đầu:**
  - MẶT BẰNG BÌNH CHỮA CHÁY;
  - BẢNG TÍNH TOÁN BÌNH CHỮA CHÁY XÁCH TAY;
  - THUYẾT MINH VÀ CHI TIẾT LẮP ĐẶT PHƯƠNG TIỆN CHỮA CHÁY BAN ĐẦU.
- **Hút khói (khi có):**
  - MẶT BẰNG TỔNG THỂ HỆ THỐNG HÚT KHÓI;
  - MẶT BẰNG BỐ TRÍ TỦ ĐIỀU KHIỂN HÚT KHÓI;
  - SƠ ĐỒ NGUYÊN LÝ HÚT KHÓI;
  - CHI TIẾT Ô BÙ KHÍ TỰ NHIÊN.
- **Hồ sơ điều chỉnh (thẩm duyệt lại):** tên tờ thêm chữ "ĐIỀU CHỈNH". Phần đổi khoanh MÂY (đường cung đỏ). Khung chữ ghi:
  - "I. Công trình đã được thẩm duyệt theo GCN số … CÔNG TRÌNH XIN PHÉP ĐIỀU CHỈNH CÁC NỘI DUNG SAU (NỘI DUNG ĐIỀU CHỈNH KHOANH MÂY): 1A, 2A… (mỗi mục một vị trí trục)";
  - "II. Các nội dung còn lại giữ nguyên theo GCN …".

## 2. Quy ước trình bày (giống nhau ở các bộ)

- **Tên tờ:**
  - Dưới hình, chữ to, gạch chân, căn giữa.
  - Màu đỏ ở bản thường, xanh dương nhạt ở bản điều chỉnh.
  - Khung tên lặp lại tên tờ một dòng.
- **Khung tên (cột phải, từ trên xuống):**
  - CHỦ ĐẦU TƯ;
  - ĐƠN VỊ TƯ VẤN GIÁM SÁT;
  - CHỦ TRÌ (chữ ký);
  - ĐƠN VỊ THI CÔNG / TƯ VẤN THIẾT KẾ PCCC (logo, giám đốc);
  - TÊN DỰ ÁN;
  - TÊN BẢN VẼ;
  - HẠNG MỤC (BÁO CHÁY, CHỮA CHÁY…);
  - SỐ HIỆU (vd `BC-02`);
  - NGÀY HOÀN THÀNH.
- **Bảng KÝ HIỆU:**
  - Góc dưới phải mỗi mặt bằng, chỉ ghi thiết bị có trên tờ.
  - Bảng 2 cột (KÝ HIỆU | TÊN THIẾT BỊ), dài thì 4 cột.
  - Đường viền bảng xanh lá, chữ xanh dương.
  - Các dòng báo cháy:
    - TRUNG TÂM BÁO CHÁY *n* ZONE;
    - ĐẦU BÁO CHÁY TIA CHIẾU (hộp phát đỏ + gương);
    - ĐẦU BÁO CHÁY KHÓI (chữ S trong vòng);
    - ĐẦU BÁO CHÁY NHIỆT (chữ H trong vòng);
    - ĐÈN "FIRE" CHỈ THỊ NGOÀI NHÀ;
    - ĐÈN BÁO PHÒNG;
    - CÒI ĐÈN KẾT HỢP BÁO CHÁY "(Cường độ âm thanh ≥ 65 dBA và ≤ 105 dBA)";
    - CHUÔNG BÁO CHÁY;
    - NÚT NHẤN BÁO CHÁY "(Có một không gian trống dạng nửa hình cầu bán kính 0,6 m…)";
    - ĐIỆN TRỞ CUỐI DÂY (Ω);
    - DÂY TÍN HIỆU FR 2×1,0 mm² (nét đứt đỏ).
  - Các dòng chữa cháy:
    - TỦ CHỮA CHÁY VÁCH TƯỜNG;
    - ĐẦU PHUN SPRINKLER HƯỚNG LÊN, K = 80;
    - VAN GIÁM SÁT TÍN HIỆU;
    - CÔNG TẮC DÒNG CHẢY (FS);
    - hatch ĐƯỜNG THOÁT NẠN.
- **Bảng QUY ĐỊNH LẮP ĐẶT ĐẦU BÁO CHÁY THEO TCVN 7568-14:2025:** đặt cạnh bảng ký hiệu trên tờ báo cháy.

  | Loại đầu báo | Bán kính bảo vệ tối đa (m) | Khoảng cách tối đa giữa các đầu báo (m) | Khoảng cách tối đa từ đầu báo đến tường (m) |
  |---|---|---|---|
  | Đầu báo khói kiểu điểm | 7,2 | 10,2 | 5,1 |
  | Đầu báo nhiệt kiểu điểm | 5,1 | 7,2 | 3,6 |
  | Đầu báo beam tia chiếu | – | 14,4 | 7,2 |

- **Bảng THỐNG KÊ:** tầng, diện tích, công năng, nhóm nhà, bậc chịu lửa, hạng sản xuất, cấp nguy hiểm cháy kết cấu. Nền đen, chữ trắng, đặt ở tờ dầm / tờ mặt bằng.
- **Lưới trục:**
  - Đầu trục tròn đỏ.
  - Dim xanh lá, gồm dim từng nhịp và dim tổng.
  - Dim định vị thiết bị: chuỗi dim dọc hàng đầu báo; với beam, dim bước tia (vd 14000) và khoảng cách tia tới tường (vd 4152).

## 3. Báo cháy trên mặt bằng

- **Tia chiếu (beam) trong kho, xưởng cao:**
  - Các tia chạy theo một phương qua hết gian.
  - Bộ phát là ô đỏ một đầu, gương hoặc bộ thu ở đầu kia. Nét tia đỏ đứt, có mũi tên vàng giữa tia.
  - Khoảng cách tia ghi bằng dim.
  - Gian có nhiều tia thì đánh số zone theo nhóm tia, ghi "ZONE n" cạnh tia đầu.
- **Đầu điểm:**
  - Rải lưới trong từng phòng, dây tín hiệu nối nối tiếp (đứt đỏ) theo từng hàng.
  - Cuối mạch có điện trở cuối dây (Ω).
  - Nhãn "ZONE n" và mũi tên "Về tủ TTBC Zone n" tại chỗ dây ra khỏi khu.
- **Khoang dầm:**
  - Mặt bằng dầm trần ghi rõ kích thước từng loại dầm (B3 300×500…), ô khoang vẽ nét đứt có ký hiệu "A1".
  - Ghi chú:
    - "ĐỘ SÂU DẦM > 0,3 m, CHIỀU CAO MÁI > 4 m, DIỆN TÍCH KHOANG DẦM > 9 m² ⇒ BỐ TRÍ TRONG KHOANG DẦM THEO KHOẢN f ĐIỀU 5.9.1.1.6";
    - khoang < 9 m² thì theo khoản a, không cần theo điểm nhô ra.
- **Thiết bị:**
  - Nút nhấn, còi đèn đặt thành cụm cạnh cửa ra, cách nhau ≤ 45 m.
  - Đèn FIRE đặt ngoài nhà.
  - Đèn báo phòng đặt cạnh cửa phòng kín.
- **Phân zone:**
  - Đặt tên ZONE 01, 02… (bộ lớn tới 25 zone, ghi "ZONE 25 DỰ PHÒNG"). Bản chỉnh sửa trên trần và dưới trần là hai mặt bằng riêng.
  - Nhà nhiều tầng ghi nhãn zone kèm diện tích: "DIỆN TÍCH BẢO VỆ ZONE 1: 187 m²". Dây nối tầng ghi "Kết nối với zone 8 tại tầng 1".
  - Mỗi tầng có mặt bằng báo cháy TRÊN TRẦN và DƯỚI TRẦN riêng.
  - Ghi chú "Tổng điện trở cuối kênh nhỏ hơn 100 Ω".
- **Tờ chi tiết lắp đặt (nhà nhiều tầng):** từng thiết bị một khung:
  - đầu báo hành lang, đầu báo trong gian phòng;
  - nút nhấn, chuông, đèn báo cháy;
  - tủ trung tâm, hộp kiểm tra;
  - báo động tại phòng bơm và phòng trực PCCC;
  - âm thanh PCCC, chống sét;
  - giá đỡ ống (trần bê tông, trên tường, ≤ DN65);
  - họng tiếp nước, trụ ngoài nhà;
  - tủ dụng cụ phá dỡ, mặt nạ lọc độc (có bảng thống kê tính số mặt nạ).
- **Sơ đồ nguyên lý báo cháy:**
  - Tủ trung tâm vẽ hình tủ ở giữa dưới; các trục dây thẳng đứng đi lên, mỗi zone một hàng ngang. Hàng gồm, theo thứ tự: nút nhấn (số lượng) → đầu khói / beam (số lượng) → còi đèn → điện trở cuối dây; chuông, đèn FIRE lệch phải.
  - Các đường ra từ tủ:
    - nguồn cung cấp 220 V;
    - kết nối về cơ quan CS PCCC theo QCVN 10:2025;
    - kết nối hệ thống quạt hút khói;
    - điều khiển hệ thống điện;
    - nguồn chính 220 VAC;
    - nguồn dự phòng 24 V.
  - Hình điện thoại kèm ghi chú "Nơi đặt tủ … có điện thoại liên lạc trực tiếp … Điều 5.12.2".
  - Khung chữ "Cáp tín hiệu điều khiển thiết bị ngoại vi … chịu nhiệt 30 min".
- **Thuyết minh báo cháy:** danh sách đánh số kèm điều khoản. Văn mẫu có ở `mau-thuyet-minh-pccc.md`. Các ý:
  - 5.8.2.1.1 khoảng trống 0,1 m / 0,6 m;
  - 5.8.2.6 bề mặt trung gian;
  - 5.8.2.5 tủ > 3 m³ / > 1 m³;
  - 5.10.3 nút nhấn ≤ 45 m;
  - 5.9.1.1.1 khoảng cách tới trần;
  - 5.9.2.1.1 đầu nhiệt;
  - 5.12.3–5.12.7 tủ trung tâm;
  - ắc quy 24 h + 30 min (chọn 7,5 Ah).

## 4. Chữa cháy

- **Sprinkler (mặt bằng):**
  - Đầu phun là chấm xanh lá theo lưới, bước khoảng 3 m (đo trên bản: trung vị 2,94 m).
  - Ống nhánh đỏ song song, ống chính vuông góc.
  - Hatch đường thoát nạn (ANSI31) phủ lối đi.
  - Vòng tròn nét đứt lớn quanh tủ vách tường là bán kính bảo vệ của họng.
  - Có chi tiết sprinkler tại cột (mặt cắt dầm).
- **Văn lý do chọn sprinkler nước:** "Xưởng không chứa các chất và vật liệu có hoạt tính hóa học mạnh… gian phòng có chiều cao không quá 20 m ⇒ Sử dụng hệ thống chữa cháy nước Sprinkler là phù hợp".
- **Khung tính toán (văn mẫu):**
  - Q họng = số tia × lưu lượng một tia, tra QCVN 10:2025 bảng H.6.
  - Q sprinkler lấy theo TCVN 7336:2021 bảng 3 (nhóm, chiều cao); có ghi +20 % khi nhóm kho.
  - Q bơm = Q họng + Q sprinkler.
  - Chọn bơm: bơm điện chính + bơm diesel dự phòng (cùng Q, H) + bơm bù (Q 1,5 l/s, H lớn hơn khoảng 10 m).
  - Bể: V = Q × 3,6 × t (t = 1 h).
  - Thời gian phục hồi bể: ống cấp DN, v = 2 m/s, phải < 24 h.
  - Nhiên liệu diesel: tiêu hao × 1 h + 10 %.
- **Vách tường:**
  - Hộp đặt cao 1,25 m tới tâm họng; vòi D50 dài 20 m, lăng D13.
  - Áp lực lăng 2,5 kg/cm² hoặc tia đặc ≥ 10 m (≥ 6 m ở bản khác).
  - Mỗi hộp có 1 van góc giảm áp, giữ áp < 0,4 MPa.
  - Họng chờ tiếp nước 2 ngõ, cao 0,6–1,25 m.
- **Trạm bơm:**
  - Lối đi giữa các bộ điều khiển 0,5 m; giữa các bơm 0,7 m; bơm tới tường 1 m.
  - Nhiệt độ phòng 5–40 °C.
  - Lối vào có đèn "TRẠM BƠM CHỮA CHÁY" nối đèn sự cố.
  - Bơm phải nối đất, có bảo vệ quá tải và quá nhiệt.
  - Phía ngoài có hai khớp nối xe chữa cháy, có phao báo mực nước, có hố thu chống ngập.
- **Vận hành bơm:**
  - Bơm bù chạy khi áp giảm ≥ 1 bar dưới áp tắt.
  - Bơm chính chạy khi áp thấp hơn mức khởi động bơm bù 0,5 bar.
  - Bơm dự phòng chạy khi áp thấp hơn mức khởi động bơm chính 1 bar.
  - Dừng bằng tay, hoặc tự dừng khi đã chạy ≥ 10 min và áp đã cao hơn áp duy trì.
- **Ghi chú đường ống:**
  - Đai giữ cách ≤ 4 m (ống ≤ DN50), ≤ 6 m (ống > DN50).
  - Đai cuối cách đầu phun ≤ 0,9 m (ống ≤ DN25), ≤ 1,2 m (ống lớn hơn).
  - Có bảng chỉ hướng dòng chảy tại các điểm nối.

## 5. Đèn sự cố, EXIT; bình; điện

- **Đèn EXIT-EM:**
  - Áp dụng TCVN 13456:2022; đèn chạy 120 min.
  - Độ rọi: đường thoát nạn ≥ 1 lux, gian phòng ≥ 0,5 lux; tỉ số lớn nhất / nhỏ nhất ≤ 40:1.
  - Biển cách nhau ≤ 25 m, cao 2–2,7 m.
  - Sơ đồ chỉ dẫn khi tầng > 1000 m² hoặc có ≥ 2 lối ra; kích thước 600×400 mm (tầng), 400×300 mm (phòng); treo cao 1,5 ± 0,2 m.
  - Có các phép tính E = Ksd·Φ/S, H = L/Z (Z = 200), độ chói L = Φ/2π so với 900 cd (bảng 1).
  - Dây chống cháy 2×1,5 mm² đi về tủ điện.
- **Bình chữa cháy:**
  - Theo TCVN 7435-1:2004 bảng 1: nguy hiểm trung bình 150 m²/bình, đi tới bình ≤ 20 m.
  - Kiểm tra 30 ngày/lần, bảo dưỡng 6 tháng/lần.
  - Có bảng tính số bình từng nhà.
- **Điện PCCC:**
  - Hai nguồn độc lập, duy trì ≥ 3 h; cáp chịu lửa ≥ 120 phút.
  - Mạch an toàn tách riêng khỏi mạch thường.
  - Tín hiệu báo cháy cắt điện sinh hoạt, giữ nguồn PCCC.
  - Bảo vệ quá tải máy phát chỉ báo động, không cắt.

## 6. So với plugin TKPCCC — 7 việc ĐÃ LÀM 15/11 (người dùng chọn làm hết)

Đã có: rải đầu điểm, beam, zone, sơ đồ nguyên lý BC (`so_do he:"bc"`, theo PDF mẫu trang 11), bảng khối lượng, dim tự động.

Đã làm, lệnh / chỗ sửa ghi ở `cad/README.md` bảng "Sửa gì vào đâu":
1. Chèn bảng KÝ HIỆU cho mặt bằng (chỉ thiết bị có trên tờ).
2. Chèn bảng "Quy định lắp đặt đầu báo cháy theo TCVN 7568-14:2025" (bảng 3 dòng ở trên, lấy số từ `data/khoang_cach_rai.json`).
3. Dim bước tia và dim tia tới tường cho `BC_BEAM`.
4. Nhãn "ZONE n" kèm "DIỆN TÍCH BẢO VỆ: … m²" + mũi tên "Về tủ TTBC Zone n"; điện trở cuối dây ở cuối mạch `PCCC_DAY`.
5. Rải theo khoang dầm: khoang > 9 m² và trần > 4 m thì mỗi khoang một đầu (5.9.1.1.6 f); mặt bằng dầm làm nét phân vùng mẫu.
6. Vòng bán kính bảo vệ cho tủ vách tường; hatch đường thoát nạn trên tờ chữa cháy.
7. Sinh thuyết minh theo `mau-thuyet-minh-pccc.md`: chọn đoạn theo hệ có thật, điền số liệu công trình.

Kết quả: 1–2 `PCCC_KYHIEU`, `PCCC_QUYDINH` (+ spec `bang`); 3 dim tia `kt=` trong `PCCC_RAI`; 4 `PCCC_DAY` hỏi tên zone, ghi diện tích, điện trở cuối dây mạch hở;
5 `PCCC_RAI` từ khoá Dam (khoang nhỏ gom); 6 `PCCC_THOATNAN`, `PCCC_VUNGBV` tủ vách tường nét đứt in được; 7 `PCCC_THUYETMINH`. Test `chay_thu.ps1` đạt.
