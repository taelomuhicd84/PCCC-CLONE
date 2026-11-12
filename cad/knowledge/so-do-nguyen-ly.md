# Sơ đồ nguyên lý (thao tác spec `so_do`, G5)

Mã: `cad/module/plugin/SoDo.cs`. Từ vựng: `cad/skills/TU_VUNG.md` (dòng `so_do`). Ví dụ: `cad/skills/vi_du/so_do.json`.
Lượt 2026-11-12 chỉ làm hệ SPRINKLER. Báo cháy (`he:"bc"`, hệ thường + địa chỉ theo `knowledge/tu-duy-di-day-di-ong.md`) để lượt sau.

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
- `tang[].dem`: số đếm của mặt bằng tầng đó (kết quả `dem` sau khi rải, hoặc người/AI tự nhập). Tầng không có `dem` (mái, tầng không có sprinkler) chỉ vẽ vạch + cao độ.
- `tang[].cot`: cao độ sàn (mm) đọc từ mặt đứng / mặt cắt. Sơ đồ xếp tầng theo `cot`.
- Van OS&Y lấy theo DN trục (`ONG_DN100` → `SPR_VAN_OSY_DN100`); số van = số trục × số tầng có `dem`.

## Khối lượng
Ống và chữ ở layer `PCCC_SODO`, vạch tầng ở `PCCC_SODO_TANG`: không phải layer `PCCC_<mã ống>` nên `PCCC_BOC` / `dem` không cộng chiều dài ống của sơ đồ.
Van là block có `MA_VT` nên được đếm: lấy số van từ sơ đồ thì đừng đặt thêm van trên mặt bằng trong cùng bản vẽ (hoặc ngược lại), không sẽ đếm hai lần.

## Còn lại của G5
- Sơ đồ báo cháy (`he:"bc"`): tủ trung tâm, zone theo tầng, số đầu báo / nút ấn / chuông đèn mỗi zone; hệ địa chỉ ZM, CM, RM, mạch vòng.
- Sơ đồ chữa cháy vách tường (tủ mỗi tầng, van giảm áp), trạm bơm, van báo động, họng tiếp nước, van 1 chiều, đồng hồ áp.
- Mã vật tư cho van xả; hình block van theo đúng ký hiệu bản mẫu.
- Cao độ tầng đọc tự động từ mặt đứng PDF (hiện người/AI nhập `cot`).
