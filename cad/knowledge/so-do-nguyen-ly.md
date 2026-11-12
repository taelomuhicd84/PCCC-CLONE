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

### Bố cục `bc` theo hồ sơ thật (sửa 2026-11-12, nguồn `cad/knowledge/mau-so-do-that.md`, trang 11)
`{"op":"so_do","he":"bc","toi_da":25,"zone":[{"ten":"ZONE 1","kv":"KARAOKE","dt":88.11,"dem":{"BC_TOHOP":1,"BC_NHIET":12}},{"ten":"ZONE 10","kv":"PHÒNG BƠM","dem":{"SPR_VAN_OSY_DN100":9,"SPR_VAN_BD":2}}],"du_phong":"ZONE 13 - 15 DỰ PHÒNG","lien_dong":["KẾT NỐI VAN MFD"]}`
- Lấy từ bản mẫu: không vạch tầng/cao độ; tủ dưới, zone xếp từ dưới lên; bó dây đứng bên trái rẽ vào từng zone; số lượng DƯỚI ký hiệu;
  cuối hàng điện trở cuối dây + tên khu vực + "DIỆN TÍCH BẢO VỆ ZONE n: xx m²"; zone giám sát van (van OS&Y + FS, phòng bơm van báo động); dòng zone dự phòng; mũi tên liên động từ tủ.
- Tự đặt (khác bản mẫu): mũi tên liên động đi từ ĐÁY tủ xuống (bản mẫu đi ngang từ cạnh phải tủ); tên thiết bị ghi trên đầu cột thay bảng ký hiệu;
  còi đèn không tách cột riêng bên phải; chưa có bảng tính ắc quy, nguồn 220 V / 24 V, tiếp địa, điện thoại.
- `h` = khoảng cách hai hàng zone (mặc định 1500, tối thiểu 700 + 2 cỡ chữ). Thử: `ket_qua/cad_mau/cad/so_do/so_do_bc_2.json` (12 zone như trang 11, toi_da 32 như bản gốc).
