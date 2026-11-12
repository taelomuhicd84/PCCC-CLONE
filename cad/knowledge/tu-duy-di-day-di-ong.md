# Tư duy đi dây báo cháy, chia zone, đi ống và rải thiết bị (người dùng dạy 2026-11-11)

Người dùng trả lời 6 câu hỏi sau khi Claude đọc bản vẽ mẫu (`mau-ban-ve-pccc.md`). Phần trong ngoặc kép là lời người dùng, giữ nguyên ý.
Giới hạn m² của một zone và các trị số khác phải tra ở văn bản quy chuẩn (`cad/rule/LUAT.md` chỉ đường), không lấy theo trí nhớ.

## 1. Dây báo cháy, hệ zone thường
- "Nét thẳng nối thiết bị, kiểu rắn bò; phòng nối tiếp phòng trong cùng zone, không rẽ chữ T; cuối zone đặt điện trở cuối dây."
- "Nếu có đèn báo phòng thì trong phòng đó có bao nhiêu đầu báo thì sẽ có bấy nhiêu nét line từ đầu báo kéo ra đèn."

## 2. Chia zone
Hệ zone thường:
- Chia theo TCVN (giới hạn m²) VÀ "1 zone không quá 25 đầu báo", dù chưa đủ giới hạn m². Ví dụ của người dùng: giới hạn là 2000 m² nhưng 25 đầu báo
  chỉ bảo vệ 1000 m² thì vẫn dừng ở 25 đầu báo.
- Khuyến cáo: bố trí NHỎ HƠN giới hạn m² và nhỏ hơn 25 đầu báo một zone để chắc chắn đạt.
- "Luôn nhớ rằng zone là khu vực": không cố nhét đủ đầu báo rồi lấn sang khu vực khác, để khu vực kia chỉ còn lẻ tẻ vài đầu và thành ra một khu vực có
  hai zone. Chỉ khi một khu vực rất lớn mới chia nó thành nhiều zone. "Đừng cắm râu ông này sang bà nọ."

Hệ địa chỉ:
- Đầu báo thường, nút nhấn thường: nối về **ZM** (module giám sát đầu báo).
- Chuông: nối với **CM** (module điều khiển còi đèn báo cháy).
- Các van giám sát trạng thái: gắn với **RM** (module giám sát ngoại vi).
- Van deluge, van MFD, màn sập ngăn cháy, cửa thang máy và các module khác: người dùng sẽ bổ sung sau (phần này chỉ liên quan vẽ sơ đồ nguyên lý).
- Sau khi nối dây từ thiết bị ra module: các module GIÁM SÁT nối liền nhau thành một mạch vòng (loop) về tủ trung tâm báo cháy.
  Các module ĐIỀU KHIỂN (kể cả ngoại vi) nối dây nguồn với nhau, được đấu chữ T, kéo một đường về tủ trung tâm báo cháy.
- Nếu dùng đầu báo địa chỉ và chuông đèn địa chỉ: đầu báo và nút nhấn tự nối thành một đường mạch vòng giám sát tín hiệu về tủ. Các van vẫn qua module
  như trên. Chuông địa chỉ "vẫn vậy": một đường line chạy thẳng về tủ, các thiết bị nối chữ T vào đường line đó.

## 3. Ống góp sprinkler trong phòng
"Phần này thì sẽ vào plugin và tôi sẽ click chọn vị trí nào trong cad làm vị trí ống góp cho dễ." Tức là plugin có lệnh để người dùng bấm chọn chỗ đặt ống góp,
không để máy tự chọn cột đầu phun.

## 4. Bước rải
- "Bước rải phải tuân theo tiêu chuẩn và quy chuẩn." TCVN 7336 yêu cầu diện tích bảo vệ của một đầu phun (12 m² hoặc 9 m² tuỳ nhóm); từ đó căn ra bước.
- Ví dụ của người dùng với 12 m²: hai đầu phun cách nhau tối đa 3,5 m × 3,5 m hoặc 4 m × 3 m, cách tường 2 m.
- "Trước khi rải, hãy vẽ polyline các vùng tường hoặc dầm sàn hay vì kèo, phân ra khu vực rải, tránh tình trạng rải lung tung."

## 5. Nhận phòng từ bản vẽ
"AI đưa hộp từng phòng, tôi sẽ kiểm tra lại và bấm vào phòng."

## 6. Chuẩn layer, block động, catalog ống và bơm
Để sau. Claude làm sườn trước. Người dùng dặn 2026-11-12: sau này plugin có chức năng để họ tự thêm các thứ này vào trong CAD, không cần nhắc đầu phiên
(mục "NẠP DỮ LIỆU NGƯỜI DÙNG QUA PLUGIN" trong `cad/TIEN_DO.md`).

## Suy ra cho lệnh vẽ và cho AI thiết kế
| Lời dạy | Việc trong plugin hoặc trong skill |
|---|---|
| Mỗi đầu báo một nét ra đèn báo phòng | `tuyen` kiểu `tia`: mỗi điểm một nét thẳng tới điểm `den` |
| Rắn bò, phòng nối tiếp phòng, không chữ T, cuối zone có điện trở | `tuyen` kiểu `noi_tiep` (đã có); AI đặt thiết bị điện trở cuối dây ở điểm cuối |
| Zone theo khu vực, không quá 25 đầu báo, dưới giới hạn m² | Việc của AI thiết kế (agent `cad-thiet-ke`): chia zone trước, đếm đầu báo và m² từng zone, rồi mới gọi `tuyen` cho từng zone |
| Hệ địa chỉ: ZM, CM, RM, mạch vòng giám sát, đường nguồn đấu T | Dùng khi vẽ sơ đồ nguyên lý (`so_do`, G5) và khi đi dây hệ địa chỉ |
| Người dùng bấm chọn vị trí ống góp | Lệnh gõ tay `PCCC_TUYEN`: chọn đầu phun, chọn ống chính, bấm vị trí ống góp |
| Bước rải căn từ diện tích bảo vệ một đầu phun | `rai` thêm `dt` (m² tối đa mỗi thiết bị): bx × by không vượt `dt`, bước không vượt `s` |
| Vẽ polyline phân khu vực trước khi rải | AI vẽ hộp từng phòng lên layer `PCCC_VUNG`, người dùng kiểm, rồi `rai` theo layer |
