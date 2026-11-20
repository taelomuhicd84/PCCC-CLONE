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

## Cột
- L1 (người dùng duyệt 19/11): cột nhận theo HÌNH, không theo tên layer: đa giác kín nhỏ (hộp ≤ 1,2 m: chữ I 12–13 đỉnh, chữ nhật, vuông, tròn) cùng cỡ ± 50 mm,
  ≥ 3 cái thẳng hàng (tâm lệch ≤ 0,15 m) là cột (`cot=` ở dòng lệnh). Bản vẽ thật có cột chữ I trên layer tên ống (`$.2.9DN25`).
  Nét cột vẫn dựng mặt nhưng KHÔNG dùng để khép khe tường (trước đây bụng chữ I bị coi là tường 2 nét rồi bắc cầu sang cột kế, ra tường ảo).
- Cột đứng giữa phòng: mặt cột < 1 m² bị bỏ, phòng vẫn là một vùng.
- Hàng cột là cơ sở chia khoang vì kèo: `cad/skills/nhan_dang/dam_khoang.md`.

## Vùng kẹp sát mặt tường (người dùng 20/11, CHƯA có mã)
- Vùng rải chạy thẳng theo MẶT TƯỜNG. Tường 200 có cột 300 lồi ra 100 thì vùng đi thẳng theo mặt tường 200, không bao vòng phần cột lồi.
- Nội thất trong phòng (bàn, tủ, thiết bị vệ sinh...) không bao: chỉ bao tường phòng. Plugin hiện bỏ nội thất kín < 1 m² hoặc hẹp; nội thất lớn kín
  có thể làm mất phòng (`PhongThat` bỏ mặt chứa mặt khác): cần nhận nội thất theo layer / block rồi bỏ khỏi `doan`.
