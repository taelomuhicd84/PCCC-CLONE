# Sơ đồ nguyên lý trong hồ sơ PCCC thật (đọc 2026-11-12)

Nguồn: hồ sơ thiết kế PCCC một công trình karaoke 2 tầng do người dùng gửi (PDF 63 trang, để ở `ket_qua/cad_mau/drive_goc/`, KHÔNG commit vì là hồ sơ khách).
Mục lục trang (tên bản vẽ quét từ khung tên): `ket_qua/cad_mau/drive_goc/muc_luc.txt`. Đọc trang nào thì render đúng trang đó, không đọc cả file.
Trang có sơ đồ: 11 nguyên lý báo cháy (BC-08), 22 không gian sprinkler (CC-09), 23 không gian vách tường, 46 nguyên lý âm thanh, 58 nguyên lý hút khói, 63 nguyên lý cấp điện PCCC.

## Sơ đồ nguyên lý báo cháy (hệ thường, tủ 15 zone)
- Tủ trung tâm ở DƯỚI, các zone xếp chồng thành hàng ngang từ dưới lên (zone 1 dưới cùng); KHÔNG vẽ vạch tầng, cao độ.
  Dây tín hiệu từ tủ đi lên thành bó đứng bên trái (nét có chữ FR), rẽ ngang vào từng zone.
- Mỗi hàng zone, trái sang phải: nút ấn + còi đèn kết hợp (số lượng ghi DƯỚI ký hiệu), đầu báo nhiệt H / khói S (số dưới ký hiệu), thiết bị chỉ thị (số dưới),
  điện trở cuối dây ở cuối hàng, kèm chữ tên khu vực + "DIỆN TÍCH BẢO VỆ ZONE n: xx m²". Còi đèn kết hợp vẽ thành cột riêng bên phải, số lượng mỗi zone.
- Zone giám sát riêng cho chữa cháy: mỗi tầng một zone gồm van OS&Y giám sát + công tắc dòng chảy; phòng bơm một zone có van báo động. Có dòng "ZONE 13–15 DỰ PHÒNG".
- Từ tủ kẻ các mũi tên ra: kết nối âm thanh thoát nạn, quạt hút khói, van MFD, cơ quan cảnh sát PCCC (TCVN 3890), cắt âm thanh phòng karaoke; nguồn chính 220 V AC + nguồn dự phòng 24 V DC (ắc quy), tiếp địa, điện thoại.
- Kèm bảng ký hiệu (ký hiệu – tên thiết bị – loại dây) và bảng tính nguồn ắc quy theo phụ lục C TCVN 7568-14:2025 (dòng tĩnh, dòng báo cháy, dung lượng C20).

## Sơ đồ chữa cháy sprinkler: hồ sơ vẽ SƠ ĐỒ KHÔNG GIAN (trục đo), không vẽ nguyên lý phẳng
- Vẽ trục đo 30°: bể nước ngầm, bơm (2 bơm chính + bơm bù áp), van báo động, họng tiếp nước, 2 trục đứng DN100; mỗi tầng một mạch vòng DN100 với nhánh DN50,
  tuyến DN80 tới khu tính thủy lực có ghi điểm 1–5 và đầu phun chủ đạo; AAV ở đầu tuyến; cụm OS&Y + FS mỗi tầng ở cao độ ghi riêng (+1,300).
- Kèm chi tiết cụm OS&Y + FS (ống đứng DN100, ống xả DN50) và bảng ký hiệu có SỐ LƯỢNG từng loại.

## So với `so_do` hiện có (G5)
- `bc` hiện vẽ theo tầng có vạch cao độ, số "xN" phía trên ký hiệu: hồ sơ thật không có vạch tầng, số ghi dưới ký hiệu, có diện tích bảo vệ từng zone, có zone giám sát van và zone dự phòng, có các đường kết nối liên động và bảng ký hiệu.
- `spr` hiện là nguyên lý phẳng: hồ sơ thật dùng sơ đồ không gian trục đo; muốn giống thì cần thao tác mới (trục đo) — việc lớn, để sau khi người dùng chọn.
