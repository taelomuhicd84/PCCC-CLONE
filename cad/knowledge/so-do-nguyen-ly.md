# Sơ đồ nguyên lý (thao tác spec `so_do`, G5)

Mã: `cad/module/plugin/SoDo.cs`. Từ vựng: `cad/skills/TU_VUNG.md` (dòng `so_do`). Ví dụ: `cad/skills/vi_du/so_do.json` (sprinkler), `cad/skills/vi_du/so_do_bc.json` (báo cháy).
Có hệ SPRINKLER (`he:"spr"`) và BÁO CHÁY HỆ THƯỜNG (`he:"bc"`, mục cuối). Vạch tầng + cao độ chỉ cho `spr` (hàm `VeVachTang`); `bc` không vẽ tầng (theo hồ sơ thật).

## Nguồn: lấy từ bản mẫu hay tự đặt
Bộ mẫu `ket_qua/cad_mau/test_HA2.pdf` (4 tờ mặt bằng) KHÔNG có tờ sơ đồ nguyên lý, KHÔNG có mặt đứng / mặt cắt (không có cao độ tầng).
Chỉ có khung "CHI TIẾT: VAN CỔNG OS&Y + CÔNG TẮC DÒNG CHẢY" trên tờ CC-07 (sprinkler tầng 1) và chú thích trục đứng trên CC-06, CC-07.

| Chi tiết | Nguồn |
|---|---|
| 2 trục cấp DN100 + 1 trục xả DN50 đặt cạnh nhau, trục xả bên trái | bản mẫu (CC-07, chi tiết + chú thích "2 Trục đứng DN100 cấp nước Sprinkler + vách tường, 1 trục xả DN50") |
| Mỗi trục, mỗi tầng một nhánh ngang: van OS&Y giám sát → công tắc dòng chảy FS → mũi tên "Sprinkler" | bản mẫu (chi tiết CC-07) |
| Sau FS của nhánh dưới có ống xả xuống, qua van xả, về trục xả | bản mẫu (chi tiết CC-07: ống DN50 có van về "Ống nước xả DN50") |
| Ống vượt nhau vẽ nét cầu (nửa vòng) | bản mẫu (chi tiết CC-07) |
| Ký hiệu OS&Y, FS, AAV | block thư viện `PCCC_Blocks.dwg` (mã `SPR_VAN_OSY_DN*`, `SPR_CTDC`, `SPR_VAN_XK`), chưa vẽ hình thật theo mẫu |
| Van xả trên ống xả | nét hình nơ (hai tam giác), TỰ ĐẶT: danh mục chưa có mã van xả |
| Vạch tầng nét mảnh xám, tam giác cao độ + chữ `±0.000` / `+4.200` trên vạch, tên tầng dưới vạch | TỰ ĐẶT (cách ghi cao độ thông dụng) |
| Các tầng cách đều `h` (mặc định 3000), không theo cao độ thật | TỰ ĐẶT theo cách kỹ sư hay vẽ (người dùng dặn) |
| Nhánh tầng đặt ở 0,6 `h` trên vạch sàn (dưới trần), hai nhánh cách nhau 900 | TỰ ĐẶT |
| Van xả khí AAV ở đầu mỗi trục cấp | TỰ ĐẶT (trên mặt bằng mẫu AAV nằm cuối tuyến từng tầng) |
| Chữ "TỪ TRẠM BƠM", "ỐNG XẢ" ở chân trục; chữ số đếm "SPRINKLER <tầng>" + "<tên vật tư>: <số> <đvt>" sau mũi tên | TỰ ĐẶT |

## Đầu vào
`{"op":"so_do","he":"spr","p":[x,y],"truc":["ONG_DN100","ONG_DN100"],"xa":"ONG_DN50","h":3000,"tang":[{"ten":"TẦNG 1","cot":0,"dem":{"SPR_DAU_DOWN":64}},...]}`
- `tang[].dem`: số đếm của mặt bằng tầng đó (kết quả `dem` sau khi rải, hoặc người/AI tự nhập). Tầng không có `dem` (mái, tầng không có sprinkler) chỉ vẽ vạch + cao độ. Plugin chỉ ghi mã hệ `SPR`, bỏ mã hệ khác, ống, dây; mã lạ hoặc số ≤ 0 báo lỗi. `dem` không có `hop`: bản vẽ nhiều tầng thì cộng `rai[].n` của tầng, hoặc mỗi tầng một bản vẽ.
- `tang[].cot`: cao độ sàn (mm) đọc từ mặt đứng / mặt cắt. Sơ đồ xếp tầng theo `cot`.
- Van OS&Y lấy theo DN trục (`ONG_DN100` → `SPR_VAN_OSY_DN100`); số van = số trục × số tầng có `dem`.

## Khối lượng (chống đếm trùng, sửa 2026-11-12)
Mọi thứ của sơ đồ, CẢ block van và thiết bị, nằm trên layer bắt đầu bằng `PCCC_SODO` (`PCCC_SODO` sprinkler, `PCCC_SODO_BC` báo cháy, `PCCC_SODO_TANG` vạch tầng).
Hàm `Duyet` (`Lenh.cs`, dùng cho `dem` và `PCCC_BOC`) bỏ qua mọi đối tượng trên các layer này: số lượng thật chỉ lấy từ mặt bằng.
Kiểm: bản vẽ có 2 OS&Y trên mặt bằng + sơ đồ 2 tầng × 2 trục thì `dem` OS&Y vẫn 2 (`cad/test/chay_thu.ps1`).
Hệ quả: van chỉ vẽ trên sơ đồ (không có trên mặt bằng) thì không vào khối lượng, phải đặt trên mặt bằng hoặc cộng tay.

## Báo cháy hệ thường (`he:"bc"`)
`{"op":"so_do","he":"bc","p":[x,y],"tu":"BC_TT","day":"DAY_BC","toi_da":25,"h":3000,"tang":[{"ten":"TẦNG 1","cot":0,"zone":[{"ten":"ZONE 1","dem":{"BC_KHOI":12,"BC_NUTAN":1}},...]},{"ten":"TẦNG 2","cot":4200,"dem":{...}},{"ten":"MÁI","cot":8400}]}`
- `toi_da` bắt buộc, truyền từ spec (giới hạn đầu báo một zone theo `cad/rule/LUAT.md` BC-04; người dùng dặn không quá 25). Zone vượt thì báo lỗi, không vẽ.
  Plugin chỉ kiểm số đầu báo (vật tư có tên bắt đầu "Đầu báo"), KHÔNG kiểm giới hạn m² (việc của AI thiết kế khi chia zone).
- `zone` mỗi tầng một hay nhiều; tầng chỉ có `dem` = một zone. `dem` lấy từ kết quả `dem` của từng zone trên mặt bằng.
- Không có tờ sơ đồ báo cháy trong bộ mẫu `test_HA2.pdf`, nên bố trí TỰ ĐẶT; ký hiệu lấy theo mẫu mặt bằng (`mau-ban-ve-pccc.md` mục 2):

| Chi tiết | Nguồn |
|---|---|
| Mỗi zone một dây riêng về tủ, cuối zone điện trở cuối dây; nhãn "Zone n" | bản mẫu (mặt bằng báo cháy: "Zone 1 về tủ TTBC", điện trở cuối dây ký hiệu Ω) + lời dạy người dùng (`tu-duy-di-day-di-ong.md` mục 1, 2) |
| Ký hiệu đầu báo S, H, nút ấn N, chuông C, đèn DL, tủ TT | block thư viện `PCCC_Blocks.dwg` (chữ trong vòng tròn như mẫu) |
| Điện trở cuối dây: mã MỚI `BC_DTCD`, block mặc định chữ `R` | TỰ ĐẶT (font txt.shx không có Ω) |
| Tủ = khung chữ nhật + block `BC_TT` trên tầng thấp nhất, bó dây zone đứng cạnh nhau cách 300 | TỰ ĐẶT |
| Dây zone đi đứng từ tủ lên tầng của zone, rẽ ngang qua thiết bị; dây vượt nhau vẽ nét cầu (như sprinkler) | TỰ ĐẶT |
| Mỗi loại thiết bị một cột (thứ tự như danh mục), trên ký hiệu ghi `x<số>`; chú thích tên cột dưới vạch tầng thấp nhất; tên tủ + tên dây dưới tủ | TỰ ĐẶT |
| Dây zone trong tầng ở 0,4 h đến 0,85 h trên vạch sàn, chia đều theo số zone | TỰ ĐẶT (plugin tự tăng `h` cho dây cách nhau đủ chữ `x<số>` + ký hiệu, dây thấp nhất không chạm khung tủ) |
| Dây vẽ nét liền (mẫu mặt bằng vẽ nét đứt) | TỰ ĐẶT, chưa đổi loại đường |

Vẽ thử: `ket_qua/cad_mau/cad/so_do/so_do_bc.json` (2 tầng + mái, 3 zone, SỐ ĐẾM GIẢ ĐỊNH), ảnh `anh/so_do_bc_1.png`.

## Còn lại của G5
- Sơ đồ báo cháy hệ địa chỉ: ZM, CM, RM, mạch vòng giám sát, đường nguồn đấu chữ T (`tu-duy-di-day-di-ong.md` mục 2); đèn báo phòng; loại đường nét đứt cho dây.
- Sơ đồ chữa cháy vách tường (tủ mỗi tầng, van giảm áp), trạm bơm, van báo động, họng tiếp nước, van 1 chiều, đồng hồ áp.
- Mã vật tư cho van xả; hình block van theo đúng ký hiệu bản mẫu.
- Cao độ tầng đọc tự động từ mặt đứng PDF (hiện người/AI nhập `cot`).

### Bố cục `bc` giống quy cách trang 11 hồ sơ thật (làm lại 2026-11-13, người dùng dặn "giống 100 %")
Nguồn: `ket_qua/cad_mau/drive_goc/trang11.png` (PDF gốc `cong_trinh.pdf` trang 11, đọc nét vector bằng PyMuPDF), mô tả `mau-so-do-that.md`.
Ví dụ dựng lại đúng số liệu trang 11: `cad/skills/vi_du/so_do_bc.json`. Ảnh so sánh: `ket_qua/cad_mau/cad/so_do/anh/so_do_bc_3.png`.
- Toạ độ trong `SoDo.cs` viết theo pt của trang 11 (A3), đổi ra bản vẽ bằng S = 33 mm/pt × k (vòng H của bảng ký hiệu 12,12 pt = 400 mm như block thư viện).
  Gốc `p` = góc dưới trái mặt tủ. Hàng zone cách `h` (mặc định 1500 mm, tối thiểu 38 pt). Phần bên phải bó dây cố định theo tủ; bảng ký hiệu đặt ở đỉnh trang hoặc cao hơn nếu nhiều zone.
- Ký hiệu (quy tắc người dùng): dùng block mang mã thiết bị đã có trong bản vẽ, KHÔNG ghi đè bằng thư viện (`NapBlock` chỉ chép mã bản vẽ chưa có; riêng lệnh `PCCC_LIB` vẫn cập nhật).
  Hình block mặc định các mã báo cháy lấy từ nét vector bảng ký hiệu trang 11: `cad/data/ky_hieu/hinh_bc.json`, sinh bằng `cad/.claude/skills/tkpccc-cad-console/scripts/hinh_tu_pdf.py`
  (H, S, nút ấn, Ω, thiết bị chỉ thị + HL, còi đèn, OS&Y, FS, van báo động, điện thoại). Thư viện cũ còn block mặc định vòng tròn + chữ thì `PCCC_LIB` vẽ lại; block người dùng đã sửa giữ nguyên.
  Trên sơ đồ ký hiệu phóng so với bảng ký hiệu theo `TlKyHieu` (đo trang 11: đầu báo 1,75; nút ấn 1,95; còi 1,83; FS 1,65; OS&Y 1,43; Ω 0,87).
- Dây: mỗi mã dây một layer (`layer_so_do`, `net`, `mau` trong `data/vat_tu.json`): `DAY_BC_10` tín hiệu 2C×1,0 Cu-PVC nét đứt (DASHED), `DAY_BC_10_FR` 2C×1 Cu-PVC/FR nét có chữ FR
  (loại đường `PCCC_FR` tạo trong mã), `DAY_NGUON_15` nguồn 2C×1,5 Cu-PVC nét gạch chấm (DASHDOT), `DAY_NGUON_15_FR` nguồn 2C×1,5 Cu-PVC/FR nét liền. Bản vẽ có sẵn layer thì giữ nét/màu người dùng.
  Bảng ký hiệu in mẫu nét bằng chính layer đó. Dây ngắt qua ký hiệu (trang 11 ký hiệu che dây).
- Bảng tính nguồn theo phụ lục C TCVN 7568-14:2025 (công thức C.1, C.2, C.3 ở `knowledge/tcvn7568-14-2025.md`): C20 = hs[(IQ×TQ)+FC(IA×TA)], IC = hs[(IQ×tq_nap)+(IA×TA)]/t_nap, IPSE = IQ+IC;
  IQ, IA cộng dòng tủ `i_tu`. Mọi hệ số và dòng từng loại thiết bị truyền từ spec; số lượng cộng từ zone, `sl` ghi đè được (trang 11 tính "trên 1 tầng": 59 H, 24 S, 28 còi, 3 nút, 11 đèn —
  KHÔNG khớp số trên sơ đồ, nên ví dụ ghi `sl`). Kiểm tay: IA 4081 mA, IQ 103,481 mA, C20 8,20568 Ah, IC 0,133 A, IPSE 0,24 A.
- Tự đặt / khác trang 11: tủ vẽ bằng nét trong mã (số đèn zone theo `zone_tu`), không phải block; vị trí ký hiệu theo cột cố định (nút ấn 697 pt, đầu báo từ 763 pt) trong khi trang 11 đặt tay xê dịch;
  chữ mô tả zone giám sát mặc định = tên các thiết bị + (khu vực), muốn y hệt trang 11 thì ghi `ghi`; bảng ký hiệu thiếu dòng "đầu báo khói kiểu điểm trên trần" (chưa có mã riêng);
  màu theo layer (trang 11 in đen trắng); ghi chú bó dây lấy tên dây `day_truc`.

