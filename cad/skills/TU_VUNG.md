# Vẽ AutoCAD bằng spec (TKPCCC)
Gọi `cad_chay` MỘT lần với mọi thao tác: `{"ops":[...],"goc":[x,y]}`. Một lần gọi = một bước Undo. Kết quả: `ve`, `xoa`, `loi` (chỉ thao tác hỏng), `hop`. Không gọi lại để kiểm nếu `loi` rỗng. Toạ độ theo đơn vị bản vẽ (thường mm), `goc` cộng vào mọi toạ độ. Spec dài hoặc cần chạy lại: lưu `.json` vào thư mục dự án rồi truyền `file`.
Thao tác (`op`):
- `luoi_truc` x:[nhịp...], y:[nhịp...], cao (cỡ chữ, mặc định 350), ten_x "1", ten_y "A", p:[x,y], kt:false để bỏ kích thước
- `dat` ma (mã thiết bị trong vat_tu.json, vd BC_KHOI), p:[[x,y],...], goc (độ)
- `ong` ma (mã ống/dây, vd ONG_DN50, DAY_BC), p:[[x,y],...]
- `net` l (layer), p:[[x,y],...], kin:true, mau (màu layer mới)
- `tron` l, c:[x,y], r
- `chu` l, p:[x,y], t, cao, goc, can:"giua"
- `kt` l, p1, p2, cach (+ trái / - phải hướng p1→p2), cao
- `xoa` l (đuôi * được) và/hoặc hop:[x1,y1,x2,y2]
- `doc` gi:"layer"|"vung"|"chu", l, hop
- `dem` (thiết bị cái, ống m theo mã)
- `rai` ma, vùng = hop | vung:[[x,y],...] | l (polyline kín, mặc định PCCC_VUNG), s (bước TỐI ĐA) hoặc buoc:[bx,by] (bước đúng), kt:true. Trả `rai`: n, bx, by, tuong, ho (% hở)
- `tuyen` ma, kieu:"noi_tiep" (dây, tu/den) | "xuong_ca" | "nhanh" (hai kiểu sau cần truc:[[x,y],...] ống chính), p (bỏ = điểm của `rai` ngay trước), nhan:true ghi DN. Trả `tuyen`: doan, m
Bước rải lấy theo quy chuẩn (`cad/rule/LUAT.md`), plugin không tự đặt.
Chi tiết: `cad_tt` với tu_vung=true.
---
## Chi tiết từng thao tác
Mọi thao tác vẽ nhận `l` (layer, mặc định `0`; chưa có thì tạo với màu `mau`, mặc định 7). Thao tác sai không làm hỏng các thao tác khác: nó vào `loi` với `i` (vị trí trong `ops`), `op`, `ly_do`.

| op | Trường | Ghi chú |
|---|---|---|
| `luoi_truc` | `x`, `y` (dãy nhịp), `cao`, `r` (bán kính bóng, mặc định 1,15 × cao), `du` (đoạn trục kéo dài, mặc định 5 × cao), `ten_x`, `ten_y`, `p`, `kt` | Trục đứng đánh theo `ten_x`, trục ngang theo `ten_y`. Layer `TRUC` (đỏ, nét CENTER), `KH_TRUC` (bóng + tên), `DIM_TRUC` (kích thước nhịp + tổng ở dưới và bên trái) |
| `dat` | `ma`, `p`, `goc` | Block lấy từ thư viện `PCCC_Blocks.dwg`, layer `PCCC_<hệ>`, có thuộc tính `MA_VT` để `PCCC_BOC` đếm |
| `ong` | `ma`, `p` | Polyline trên layer `PCCC_<mã>`; `PCCC_BOC` tính chiều dài theo layer này |
| `net` | `l`, `p`, `kin`, `mau` | Polyline thường (tường, vùng, khung) |
| `tron` | `l`, `c`, `r` | |
| `chu` | `l`, `p`, `t`, `cao` (mặc định 250), `goc`, `can` | Chữ Arial có dấu tiếng Việt. `can:"giua"` căn tâm tại `p`, mặc định `p` là góc dưới trái |
| `kt` | `l`, `p1`, `p2`, `cach` (mặc định 500), `cao` (mặc định 250) | Kích thước song song, số nguyên, kiểu `PCCC_KT` |
| `xoa` | `l`, `hop`, `tat_ca` | Phải có `l` hoặc `hop`. Chỉ xoá đối tượng nằm trọn trong `hop`. Xoá hết: `"tat_ca":true` |
| `doc` | `gi`, `l`, `hop` | `layer`: số đối tượng theo layer. `vung`: polyline kín trên `l` (mặc định `PCCC_VUNG`) kèm `m2`, `hop`, đỉnh, handle `h`. `chu`: nội dung + vị trí chữ. Tối đa 40 dòng, phần dư báo ở `con` |
| `dem` | | Số thiết bị (cái) và ống/dây (m) theo mã, cùng cách đếm với `PCCC_BOC` |
| `rai` | `ma`; vùng: `vung` hoặc `hop` hoặc `l` (+ `hop` để lọc); `s` hoặc `buoc` (số hoặc [x, y]); `le`, `chan` (mặc định 50 mm), `goc`, `kt` | Rải thiết bị theo lưới đặt giữa vùng. `s`: n = ceil(W / s), bước chia đều rồi làm tròn xuống theo `chan`, dư dồn đều ra hai tường. `buoc`: bước đúng, số thiết bị nhiều nhất mà cách tường còn ≥ `le` (mặc định bước / 4). `goc` (độ) xoay lưới cho phòng nghiêng. Vùng không chữ nhật: lưới theo hộp bao, bỏ điểm ngoài đa giác. `kt`:true ghi chuỗi kích thước tường–thiết bị–tường trên layer `PCCC_KT` (chỉ khi `goc` = 0). Thiếu cả `s` và `buoc` thì báo lỗi. Quá 5000 thiết bị một vùng thì báo lỗi (sai đơn vị). Kết quả `rai`: mỗi vùng `n`, `nx`, `ny`, `bx`, `by`, `tuong` [x, y], `ho` (% diện tích xa mọi thiết bị hơn nửa đường chéo ô lưới; > 0 thì phải xem lại) |
| `tuyen` | `ma` (ống/dây), `kieu`, `p`, `truc`, `tu`, `den`, `qua`, `nhan`, `cao`, `sai_so` (mặc định 100 mm) | Không có `p` thì dùng điểm của `rai` ngay trước, theo từng vùng. `noi_tiep`: MỘT polyline đi rắn bò qua mọi điểm, hết vùng này sang vùng kế; `tu` nối thêm đầu vào (hàng bắt đầu từ góc gần `tu`), `den` nối thêm đầu ra. `xuong_ca`: mỗi vùng một ống góp vuông góc ống chính `truc` tới hàng xa nhất (trùng cột thiết bị gần `tu`, không có `tu` thì cột gần giữa; `qua` để chỉ chỗ khác) + mỗi hàng một nhánh tới ống góp. `nhanh`: mỗi điểm một đoạn vuông góc vào ống chính. `nhan`:true ghi chữ DN (từ mã `ONG_DN50`) dọc đoạn đủ dài, layer `PCCC_NHAN`. Kết quả `tuyen`: `kieu`, `doan`, `m`. Chưa né tường: là phương án nháp để kỹ sư chỉnh |

Cấp gói: `goc` [x,y], `zoom`:true (zoom toàn bản vẽ sau khi vẽ, tốn thêm một bước Undo).

Mặc định của `luoi_truc`, `kt`, `chu` (cỡ chữ, bóng trục, tên và màu layer trục, khoảng cách kích thước) nằm ở `cad/data/ky_hieu/ky_hieu.json`. Muốn đổi cho mọi bản vẽ thì sửa file đó, không cần truyền lại trong từng spec.

## Ví dụ
```json
{"ops":[
 {"op":"luoi_truc","x":[3600,3600,3600,3600],"y":[3600,3600,3600,3600,3600,3600,3600,3600,3600]},
 {"op":"dat","ma":"BC_KHOI","p":[[1800,1800],[5400,1800]]},
 {"op":"ong","ma":"DAY_BC","p":[[1800,1800],[5400,1800]]},
 {"op":"dem"}]}
```

Rải một phòng rồi đi dây, đi ống (đủ bộ: `cad/skills/vi_du/rai.json`):
```json
{"ops":[
 {"op":"rai","ma":"BC_NHIET","hop":[0,0,6500,4500],"s":4000},
 {"op":"tuyen","ma":"DAY_BC","kieu":"noi_tiep","tu":[6500,-500]},
 {"op":"rai","ma":"SPR_DAU_DOWN","hop":[0,0,6500,4500],"buoc":[4000,3000],"kt":true},
 {"op":"tuyen","ma":"ONG_DN50","kieu":"xuong_ca","truc":[[-1000,-1000],[8000,-1000]],"nhan":true}]}
```
Số `s`, `buoc` trong ví dụ là số của bản mẫu, không phải quy chuẩn.

## Quy tắc tiết kiệm token
- Gộp mọi thao tác vào một lần gọi `cad_chay`. Không chia nhỏ theo từng nét.
- Không gọi `cad_tt` hay `doc` để kiểm lại khi kết quả đã có `ok:true`.
- Đọc bản vẽ: dùng `doc` có `l` hoặc `hop` để thu hẹp, không đọc cả bản vẽ.
- Hình lặp lại nhiều lần: xin thêm thao tác tham số trong `cad/module/plugin/Spec.cs` thay vì liệt kê từng nét.
