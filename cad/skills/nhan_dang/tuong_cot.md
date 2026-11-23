# Nhận dạng tường, vách, cột

Mã: `ThuNet`, `LoaiLop`, `NetLien`, `GhiCotUng`, `TachCot`, `MatChon` trong `cad/module/plugin/NhanVung.cs`. Vẽ tường, cột: `cad/skills/ve/truc_tuong_cua.md`.

## Nét nào là tường
- Bản vẽ có layer tên tường (`TUONG`, `WALL`, `VACH`, `COT`, `COLUMN`, danh sách `LOP_TUONG`) thì dựng phòng bằng các layer đó trước.
  Lượt đó không ra phòng thật nào (mặt ≥ 1 m², rộng TB ≥ 1 m) thì dựng lại bằng mọi nét liền.
  Bài học 20/11 (Karaoke ABC, `cad/skills/mau/hoc-bao-chay-ban-mau.md`): layer `tuong` chỉ chứa block cửa, tường thật trên layer `0`; trước đây "Enter mọi phòng" ra 0 vùng.
- Mọi nét liền dùng làm tường, cả nét trong block (đệ quy) và biên hatch cột tô đặc. BỎ: nét có gạch (trục CENTER/DASHDOT, nét khuất), layer có chữ
  `TRUC`, `AXIS`, `GRID`, `CENTER`, `DIM`, `CUA`, `DOOR`, `WINDOW`, `NOI THAT`, `FURN` (so không dấu, danh sách `LOP_BO`), layer `PCCC_*` khác `PCCC_VUNG`,
  layer tắt / đóng băng, chữ, kích thước.
- Hatch gạch chéo (`LaGachCheo`) = công trình hiện hữu: Enter mọi phòng bỏ phòng có tâm nằm trong vùng gạch chéo (`gach_cheo=`). Hatch tường mỏng không làm mất phòng.
- Hai nét tường cắt chéo nhau (một nét vẽ ngược, người dùng 19/11): chọn nét song song ranh đất / thẳng hàng tường kế.
- Hatch gạch chéo tường ĐÃ NỔ thành nét (Karaoke 123 khung 4, 21/11: 3409 nét 45°, dài ≈ 0,28 m, cách 90 mm, layer `02 - TUONG HOAN THIEN` là layer tường):
  KHÔNG làm tường (`BoHatchNo` đầu `CacMat`). Trước đây mẩu cùng một đường gạch ở hai tường kế nhau thẳng hàng, bước khép khe nối ngang góc phòng
  (≤ 3 m luôn khép, tới 6 m nhờ mẩu kề hở cùng chỗ như tường 2 nét) ra vùng cạnh chéo 4–6 m, sót phòng. Nhận: mẩu ≤ 0,8 m lệch trục tường chính
  ≥ 10°, có mẩu song song kề CẢ HAI bên (≤ 0,25 m), cùng hướng ≥ 20 mẩu như thế. Nắp đầu tường (theo trục chính), cặp góc vát tường 2 nét (kề một bên) giữ lại. Test E17.
- Lan can / mép sàn quanh ô cầu thang, lỗ thông tầng (Karaoke 123 tầng 2, 21/11: 2 nét layer `06 - THAY 1` cách 60 mm nối cột này sang cột kia):
  lượt chỉ-tường vẫn lấy (`LanCan`): LINE / polyline hở (trường học khung 6 vẽ lan can bằng polyline) layer thường ≥ 2 m có nét song song cách 20–300 mm phủ ≥ 80 %, HAI đầu chạm (≤ 50 mm) tường / cột.
  Không có thì hành lang trên buồng thang hở thông xuống thang + ô thông tầng ra ngoài nhà, không thành phòng; mẫu có đầu báo ở đó. Test E19.
- Vách 2 nét hở đầu (23/11, test E26): một nét có đầu tự do, nét song song cách 0,05–0,35 m dừng cùng chỗ và chạm nét khác (góc tường) → bịt đầu vách
  (`CacMat` bước 1d). Không bịt thì nét hở bị bỏ như nhánh cụt, thân vách nhập vào phòng kề, vùng trùm lên vách (ô 14,56 m² ra 15,6).

## Cột
- L1 (người dùng duyệt 19/11): cột nhận theo HÌNH, không theo tên layer: đa giác kín nhỏ (hộp ≤ 1,2 m: chữ I 12–13 đỉnh, chữ nhật, vuông, tròn) cùng cỡ ± 50 mm,
  ≥ 3 cái thẳng hàng (tâm lệch ≤ 0,15 m) là cột (`cot=` ở dòng lệnh). Bản vẽ thật có cột chữ I trên layer tên ống (`$.2.9DN25`).
  Nét cột vẫn dựng mặt nhưng KHÔNG dùng để khép khe tường (trước đây bụng chữ I bị coi là tường 2 nét rồi bắc cầu sang cột kế, ra tường ảo).
  Review 21/11: nét cột thẳng hàng VÀ chạm (≤ 50 mm) nét tường thường thì khép khe như tường (cột trong tường, cửa cuốn để trống giữa hai cột);
  khe giữa mặt trong hai cột vẫn làm "khe song đôi" cho khe mặt ngoài. Hộp cạnh dài > 2,5 x cạnh ngắn (trụ tường 1000 x 220 vẽ polyline kín) không phải cột.
- Cột đứng giữa phòng: mặt cột < 1 m² bị bỏ, phòng vẫn là một vùng.
- Hàng cột là cơ sở chia khoang vì kèo: `cad/skills/nhan_dang/dam_khoang.md`.

## Vùng kẹp sát mặt tường (người dùng 20/11, CHƯA có mã)
- Vùng rải chạy thẳng theo MẶT TƯỜNG. Tường 200 có cột 300 lồi ra 100 thì vùng đi thẳng theo mặt tường 200, không bao vòng phần cột lồi.
- Nội thất trong phòng (bàn, tủ, thiết bị vệ sinh...) không bao: chỉ bao tường phòng. Plugin hiện bỏ nội thất kín < 1 m² hoặc hẹp; nội thất lớn kín
  có thể làm mất phòng (`PhongThat` bỏ mặt chứa mặt khác): cần nhận nội thất theo layer / block rồi bỏ khỏi `doan`.
