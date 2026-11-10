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

Cấp gói: `goc` [x,y], `zoom`:true (zoom toàn bản vẽ sau khi vẽ, tốn thêm một bước Undo).

## Ví dụ
```json
{"ops":[
 {"op":"luoi_truc","x":[3600,3600,3600,3600],"y":[3600,3600,3600,3600,3600,3600,3600,3600,3600]},
 {"op":"dat","ma":"BC_KHOI","p":[[1800,1800],[5400,1800]]},
 {"op":"ong","ma":"DAY_BC","p":[[1800,1800],[5400,1800]]},
 {"op":"dem"}]}
```

## Quy tắc tiết kiệm token
- Gộp mọi thao tác vào một lần gọi `cad_chay`. Không chia nhỏ theo từng nét.
- Không gọi `cad_tt` hay `doc` để kiểm lại khi kết quả đã có `ok:true`.
- Đọc bản vẽ: dùng `doc` có `l` hoặc `hop` để thu hẹp, không đọc cả bản vẽ.
- Hình lặp lại nhiều lần: xin thêm thao tác tham số trong `cad/src/Spec.cs` thay vì liệt kê từng nét.
