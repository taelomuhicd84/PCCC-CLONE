# Bản vẽ PCCC mẫu của người dùng: cách thể hiện, cách rải, cách đi dây và đi ống

Nguồn: bộ PDF 4 tờ người dùng gửi 2026-11-11 (nhà karaoke 2 tầng, mặt bằng 41,0 × 20,3 m, phòng hát hai bên hành lang).
Bản sao để ở `ket_qua/cad_mau/test_HA2.pdf`, không commit vì là hồ sơ công trình thật.

File này ghi CÁCH người dùng vẽ, để lệnh `rai`, `tuyen` vẽ ra giống vậy. Các số đo dưới đây là số trên bản mẫu, KHÔNG phải quy chuẩn:
vẽ công trình khác thì lấy trị số từ file quy chuẩn (`cad/rule/LUAT.md` chỉ đường).

## 1. Mỗi hệ một tờ mặt bằng
| Tờ mẫu | Tên bản vẽ | Có gì |
|---|---|---|
| BC-02 | Mặt bằng báo cháy dưới trần tầng 1 | đầu báo, đèn chỉ thị ở cửa phòng, còi đèn, nút ấn, dây tín hiệu, nhãn zone |
| CC-06 | Mặt bằng tủ chữa cháy vách tường tầng 2 | ống chính, tủ vách tường, van giảm áp, vòng bán kính R = 20 m |
| CC-07 | Mặt bằng sprinkler tầng 1 | đầu phun, ống chính, ống nhánh, trục đứng, bảng thông số, bảng thống kê |
| EXEM-02 | Mặt bằng EXIT - EM tầng 1 | đèn sự cố, đèn EXIT các loại, sơ đồ chỉ dẫn thoát nạn |

Chung cho mọi tờ:
- Nền kiến trúc (tường, trục, cửa, thiết bị vệ sinh) màu xám nhạt, nét mảnh. Hệ PCCC màu đen, nét đậm.
- Lưới trục có bóng tên trục và kích thước nhịp + tổng. Tên bản vẽ gạch chân đặt dưới mặt bằng.
- Bảng ký hiệu (cột KÍ HIỆU, TÊN THIẾT BỊ, có tờ thêm SỐ LƯỢNG), khối ghi chú, chi tiết lắp đặt, khung tên bên phải.
- Kích thước định vị thiết bị ghi trong từng phòng theo cả hai phương: tường → thiết bị đầu, giữa các thiết bị, thiết bị cuối → tường.

## 2. Báo cháy (hệ thường, chia zone, có điện trở cuối dây)
Ký hiệu: đầu báo nhiệt là vòng tròn chữ H, đầu báo khói là vòng tròn chữ S, đèn chỉ thị ở cửa phòng (kèm chữ F = biển "FIRE"),
còi đèn kết hợp, nút ấn, điện trở cuối dây (ký hiệu Ω). Dây tín hiệu là nét đứt, ghi chú "2C x 1,0 mm2 Cu-PVC".

Cách rải trên mẫu:
- Phòng hát khoảng 6,5 × 4,5 m: 4 đầu báo nhiệt, lưới 2 × 2, bước 4000 theo chiều dài và 3000 theo chiều rộng. Bước là số tròn,
  phần dư dồn đều ra hai tường (cách tường 500–1700 theo chiều dài, 640–900 theo chiều rộng).
- Hành lang rộng 1,7 m: một hàng đầu báo trên tim hành lang (cách tường 850), bước 4500 (đoạn lẻ 3300–4550).
- Mỗi cửa phòng: đèn chỉ thị + chữ F đặt ngoài hành lang cạnh cửa; còi đèn đặt trong phòng gần cửa.
- Nút ấn + còi đèn đặt ở hành lang gần lối ra và cầu thang.

Cách đi dây trên mẫu:
- Nét thẳng nối thiết bị với thiết bị, cho phép đi chéo, không bẻ vuông góc theo tường.
- Trong phòng: dây từ đèn chỉ thị ở cửa đi qua các đầu báo trong phòng và còi đèn.
- Hành lang: một đường thẳng chạy qua cả hàng đầu báo.
- Mỗi zone kết thúc bằng điện trở cuối dây ở đầu báo cuối cùng. Đầu về tủ ghi nhãn kèm mũi tên: "Zone 1 về tủ TTBC".
  Tầng mẫu có 4 zone (dãy phòng dưới, dãy phòng trên + cánh phải, khối giữa, sảnh thông tầng).

## 3. Sprinkler
Ký hiệu: đầu phun hướng xuống K = 5,6 (68 °C), van cổng OS&Y có giám sát, công tắc dòng chảy FS, van xả khí AAV, van 1 chiều, van giảm áp,
van báo động, họng tiếp nước. Ống là nét liền đậm. Tầng mẫu có 64 đầu phun.
Bảng thông số trên tờ: nhóm nguy cơ, cường độ phun, diện tích bảo vệ mỗi đầu (12 m2), khoảng cách tối đa giữa các đầu phun (4 m), hệ số K.

Cách rải trên mẫu:
- Phòng hát: 4 đầu phun, lưới 2 × 2, bước 3300–4000 theo chiều dài và 2300 (khối giữa 2950) theo chiều rộng; cách tường 600–1500 và 1000–1100.
- Hành lang: một hàng, bước 3000–4000, cách tường 850.

Cách đi ống trên mẫu (kiểu xương cá):
- Ống chính DN100 chạy thành mạch vòng trong hành lang quanh khối giữa. Cánh phải cấp bằng ống DN80 chạy dọc hành lang.
- Mỗi phòng có MỘT ống góp DN50 đi vuông góc từ ống chính vào phòng, tới hàng đầu phun xa nhất. Ống góp đi trùng một cột đầu phun
  hoặc lệch khỏi cột đó vài trăm mm.
- Mỗi hàng đầu phun có một ống nhánh DN50 song song ống chính, nối các đầu phun của hàng và cắt ống góp.
- Đầu phun hành lang nối thẳng vào ống chính bằng một đoạn ngắn vuông góc.
- Mọi ống nhánh trên mẫu đều ghi DN50 (không giảm cỡ theo số đầu phun). Chữ "DN50", "DN100" đặt dọc theo từng đoạn ống.
- Trục đứng: 2 trục DN100 cấp nước chung cho sprinkler và vách tường + 1 trục xả DN50, đặt trong hộp kỹ thuật. Mỗi nhánh tầng có
  van cổng OS&Y giám sát + công tắc dòng chảy, báo về tủ trung tâm báo cháy. Cuối tuyến có van xả khí AAV.

## 4. Chữa cháy vách tường
- Dùng chung mạch vòng DN100 với sprinkler; nhánh DN50 xuống từng tủ; mỗi tủ một van giảm áp (áp tại tủ < 0,4 MPa).
- Mỗi tủ vẽ một vòng tròn nét đứt bán kính R = 20 m để chứng minh vùng phủ (lệnh `PCCC_VUNGBV` đã làm được).
- Có chi tiết phóng to trục đứng và chi tiết tủ.

## 5. Đèn EXIT và đèn sự cố
- Mỗi cửa phòng (phía trong): một đèn sự cố (ký hiệu hai mắt) + một đèn EXIT.
- Hành lang: đèn EXIT 1 hướng / 2 hướng treo dọc đường thoát nạn, ghi kích thước khoảng cách giữa các đèn; đèn EXIT tầm thấp;
  sơ đồ chỉ dẫn thoát nạn đặt ở cầu thang và lối ra.

## 6. Suy ra cho lệnh vẽ
| Việc | Lệnh | Tham số lấy từ mẫu |
|---|---|---|
| Rải đầu báo, đầu phun theo phòng | `rai` | lưới chữ nhật, bước số tròn, dư dồn đều ra tường; hành lang hẹp ra một hàng |
| Dây báo cháy | `tuyen` kiểu `noi_tiep` | nét thẳng qua lần lượt các thiết bị, đi kiểu rắn bò |
| Ống nhánh phòng | `tuyen` kiểu `xuong_ca` | một ống góp vuông góc ống chính + nhánh theo từng hàng |
| Đầu phun hành lang, tủ vách tường | `tuyen` kiểu `nhanh` | mỗi thiết bị một đoạn vuông góc vào ống chính |
| Nền kiến trúc từ PDF | `pdf` | `PDFIMPORT`, tỉ lệ tính từ chuỗi kích thước trục |

Chỗ chưa chắc, đang chờ người dùng xác nhận: xem mục "CHỜ NGƯỜI DÙNG" trong `cad/TIEN_DO.md`.
