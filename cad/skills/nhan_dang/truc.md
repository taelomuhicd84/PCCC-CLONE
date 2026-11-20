# Nhận dạng trục

Mã: `GomTruc`, `KhungTruc` trong `cad/module/plugin/NhanVung.cs`. Vẽ trục: `cad/skills/ve/truc_tuong_cua.md`.

## Plugin nhận trục thế nào
- Block ký hiệu trục: tên hoặc layer có `TRUC`, `AXIS`, `GRID`; hoặc vòng tròn có chữ ngắn bên trong (bóng trục). Tâm bóng = mốc trục.
- Nét trục: nét gạch (CENTER, DASHDOT) và layer có `TRUC`, `AXIS`, `GRID`, `CENTER` không bao giờ dùng làm tường.
- Khi dựng phòng bằng mọi nét liền (bản vẽ không có layer tường): nét thẳng ≥ 3 m trùng đường trục (lệch ≤ 5 mm) là trục vẽ nét liền, bị bỏ (`bo_net_truc=`).
- Khung trục = hộp các mốc trục ngoài cùng (`khung_truc x=.. y=..` ở dòng lệnh).

## Luật người dùng (17/11, file KOCHAM: trục vẽ polyline nét liền layer 0 trong block)
- Đường trục KHÔNG BAO GIỜ là biên vùng.
- Tự nhận vùng (Enter mọi phòng, `vung_tu_dong`) chỉ lấy phòng nằm TRONG đường trục ngoài cùng (X-1..X-n, Y-1..Y-n).
  Phòng ngoài trục (mái che, phần tờ khác, khoảng giữa trục và ký hiệu trục) KHÔNG phân vùng khi người dùng chưa cho phép; bấm điểm trong phòng = cho phép.
  Plugin: `bo_ngoai_truc=`. Ngoại lệ L8 (20/11): phòng có chữ tên phòng ("VĂN PHÒNG", có chữ PHÒNG trừ phòng bơm) nằm ngoài trục vẫn giữ.

## PDF
Trục cũng là thước kiểm tỉ lệ khi nhập PDF: `cad/skills/nhan_dang/pdf.md`.
