# Đi dây báo cháy trên mặt bằng: hệ thường và hệ địa chỉ

Nguồn: lời người dùng ngày 16/11/2026 và bản vẽ mẫu `07_CAD\04_KIEM_THU\file_cad_test\test báo cháy.dwg` (không commit). Bản vẽ này có hai nửa:
nửa trái vẽ hệ địa chỉ, nửa phải vẽ hệ thường. Lệnh vẽ theo cách này là `PCCC_DAYBC`, mã ở `09_PCCC/02_BAO_CHAY/cad/DayBc.cs`.

## Luật chung
- Dây chỉ đi vuông góc (zíc zắc theo hàng thiết bị). Không đi xéo.
- Mỗi loại dây một layer. Bảng layer nằm ở trang Cài đặt. Dây cho từng loại thiết bị chọn ở trang Báo cháy, mục 6.

## Hệ thường (nửa phải bản mẫu)
- Dây tín hiệu là MỘT đường nối các đầu báo và nút nhấn. Đây là mạch hở: không có đường vòng về tủ trung tâm báo cháy.
- Đầu báo hoặc nút nhấn ở cuối đường được gắn điện trở cuối dây.
- Muốn tiết kiệm dây thì điện trở cuối dây phải đặt ở điểm xa tủ nhất.
- Chuông đi một đường dây nguồn riêng. Đường này là mạch cụt.
- Mẫu dùng layer `$.1.FA-SMOKE` (đỏ, nét HIDDEN) cho dây tín hiệu và `$.1.FA-FR-BELL` (xanh dương) cho dây chuông.
  Dây chuông của mẫu là một đường trục chạy dọc tường giữa, có nhánh rẽ tới chuông phía dưới.

## Hệ địa chỉ (nửa trái bản mẫu)
- Mạch vòng: dây đi từ tủ tới các module rồi quay về tủ. Nếu có đầu báo và module địa chỉ thì vòng đi từ tủ qua chúng, qua các module, rồi về tủ.
  Mẫu vẽ mạch vòng bằng nét màu hồng (màu 6). Trong mẫu, vòng đi tủ → CM → ZM → R2ML (dưới) → R2ML → ZM (trên) → về tủ theo chu vi.
- Đầu báo địa chỉ và nút nhấn địa chỉ nối thẳng vào mạch vòng màu hồng.
- Đầu báo thường và nút nhấn thường nối về module ZM. Đây là mạch hở có điện trở cuối dây (đường đỏ của mẫu).
  Các module ZM nối với nhau rồi về tủ, nằm trên mạch vòng màu hồng.
- Chuông thường nối về module CM, rồi CM về tủ (đường xanh dương).
- Chuông địa chỉ nối về module nguồn như R2ML, rồi về tủ. Đường này là mạch cụt, kiểu xương cá cũng được.
- Trong mẫu, module là block `ZM`, `CM` và `RM`. Block `RM` có thuộc tính `R2ML`. Gốc các block này đặt xa hình vẽ,
  nên plugin lấy tâm hình làm điểm nối.

## Plugin làm thế nào (`PCCC_DAYBC`)
- Plugin nhận loại thiết bị theo thứ tự sau:
  1. mã vật tư: `BC_ZM`, `BC_CM`, `BC_R2ML`, `BC_MODULE`, `BC_TOHOP`, `BC_NUTAN*`, `BC_CHUONG*`, `BC_KHOI*`, `BC_NHIET*`, `BC_TT`;
  2. nếu không có mã thì theo chữ của block: tên block, chữ thuộc tính, tên block lồng bên trong (ví dụ "NUT NHAN", "CHUONG", "ZM").
- Mã có đuôi `_DC`, hoặc chữ có "DIA CHI", là thiết bị địa chỉ.
- Thứ tự đi dây: rắn bò theo hàng đầu báo; nút nhấn và module chèn vào chỗ tốn ít dây nhất; sau đó chạy 2-opt theo khoảng cách Manhattan,
  vì dây vuông góc có chiều dài đúng bằng khoảng cách Manhattan.
- Hệ thường: thiết bị cuối là thiết bị xa tủ nhất (Manhattan). Nhóm về ZM: cuối đường tự do, chọn đường ngắn nhất.
- Dây chuông: cây xương cá. Mỗi chuông nối vào điểm gần nhất của cây bằng nhánh vuông góc.
- Chỗ chưa làm: các đường khác layer có thể chồng lên nhau trên cùng một đoạn (chưa dời song song).
  Mạch vòng đi theo đường ngắn, không bám chu vi phòng như mẫu.

## Luật người dùng chỉnh trên bản vẽ (19/11 Quyết Thắng, 20/11)
- Dây báo cháy chỉ đi thẳng theo X và Y, không xiên. Riêng dây đầu báo -> đèn báo phòng được xiên (`tuyen` `tia`).
  Dây zone: tự lập đường rắn bò rồi vẽ bằng `ong` `ma`:"DAY_BC" với điểm gấp khúc vuông góc (thêm điểm góc khi hai thiết bị lệch cả x lẫn y; lệch vài chục mm thì một đoạn ngắn).
  Thứ tự hàng sao cho dây không đi lại trên chính nó, không cắt nhau. `tuyen` `noi_tiep` nối thẳng điểm–điểm nên còn đoạn chéo (chưa có tuỳ chọn vuông góc).
- Dây đi KẸP SÁT TƯỜNG (hoặc sát kết cấu), cách mặt tường ~300 mm, trong phạm vi công trình; không vòng ra ngoài nhà.
- Đầu báo và nút ấn cùng trên dây tín hiệu; điện trở cuối dây `BC_DTCD` ở cuối mỗi zone, sau thiết bị cuối.
- Không kéo dây về tủ TTBC trên mặt bằng: đầu zone chỉ kéo một đoạn ngắn (~2 m) ra khỏi công trình rồi ghi "VỀ ZONE n TỦ TTBC". Chỉ khi bóc khối lượng mới kéo đủ về tủ
  (vẫn kẹp tường, X/Y, không chim bay).
- Đầu ra zone (mũi tên "ZONE n") đặt ở mép zone hướng về tủ / về trục dây chung: chọn điểm của zone gần tủ nhất (cửa, tường phía tủ). Đầu ra xa tủ thì dây về tủ dài gấp đôi (19/11).
- Vị trí điện trở cuối: người dùng 16/11 và 20/11 "điện trở cuối đặt ở vị trí XA tủ TTBC để tiết kiệm dây". Bản mẫu "TK PCCC XƯỞNG TIÊU LIÊN THÀNH FINAL.dwg" (19/11) lại đi
  tuyến chữ U (ra qua nửa thiết bị, quay về qua nửa còn lại) và đặt điện trở cuối ngay cạnh đầu ra. Hai cách này đang được đối chiếu trên các khung mẫu
  `TESTBCHAY_20_11` (VIỆC 20/11 (2) mục 4); chưa chốt thì theo lời mới nhất (20/11): đầu ra gần tủ, điện trở cuối ở thiết bị xa tủ nhất.
- Lời nhắn Bot chat gắn với bản vẽ mẫu thì chỉ đọc; không gửi gói vẽ của dự án khác vào bản mẫu (19/11 đã lỡ vẽ 12 nét vào bản mẫu).

