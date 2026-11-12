# Vẽ AutoCAD bằng spec (TKPCCC)
Gọi `cad_chay` MỘT lần cho mọi thao tác: `{"ops":[...],"goc":[x,y]}` = một bước Undo. Kết quả: `ve`, `xoa`, `loi` (chỉ thao tác hỏng), `hop`. `loi` rỗng thì không gọi lại để kiểm. Toạ độ theo đơn vị bản vẽ (thường mm), cộng thêm `goc`. Spec dài, chạy lại: lưu `.json` vào thư mục dự án rồi truyền `file`.
Thao tác (`op`):
- `luoi_truc` x:[nhịp...], y:[nhịp...], cao (cỡ chữ, mặc định 350), ten_x "1", ten_y "A", p:[x,y], kt:false bỏ kích thước
- `dat` ma (mã thiết bị trong vat_tu.json, vd BC_KHOI), p:[[x,y],...], goc (độ)
- `ong` ma (mã ống/dây, vd ONG_DN50, DAY_BC), p:[[x,y],...]
- `net` l (layer), p:[[x,y],...], kin:true, mau (màu layer mới)
- `tron` l, c:[x,y], r
- `chu` l, p:[x,y], t, cao, goc, can:"giua"
- `kt` l, p1, p2, cach (+ trái / - phải hướng p1→p2), cao
- `xoa` l (đuôi * được) và/hoặc hop:[x1,y1,x2,y2]
- `doc` gi:"layer"|"vung"|"chu", l, hop
- `dem` (thiết bị cái, ống m theo mã)
- `rai` ma, vùng = hop | vung:[[x,y],...] | l (polyline kín, mặc định PCCC_VUNG) | tim (tim hành lang, đỉnh giữa có đầu), s (bước TỐI ĐA) + dt (m² tối đa/thiết bị) hoặc buoc:[bx,by] (bước đúng), le (cách tường), theo_hang:true (phòng chữ L), kt:true. Trả `rai`: n, bx, by, tuong, m2, ho (% hở)
- `tuyen` ma, kieu:"noi_tiep" (dây, tu/den) | "tia" (mỗi điểm một nét tới den) | "xuong_ca" | "cot" (mỗi cột một nhánh) | "nhanh" (3 kiểu sau cần truc:[[x,y],...] ống chính), p (bỏ = điểm của `rai` trước), nhan:true ghi DN. Trả `tuyen`: doan, m
- `anh` hop, rong (px): in PNG sau khi vẽ xong cả gói, trả đường dẫn; chỉ đọc ảnh khi cần nhìn
- `pdf` file, trang, tl (mẫu số tỉ lệ, tính bằng `pdf_ti_le.py`), p, goc: nhập nét + chữ trang PDF làm nền, chạy TRƯỚC các thao tác khác. Trả `pdf`: n, l
`s`, `dt` lấy theo quy chuẩn (`cad/rule/LUAT.md`), plugin không tự đặt. Rải theo `l`: vẽ polyline kín từng khu vực lên PCCC_VUNG trước.
Chi tiết: `cad_tt` với tu_vung=true.
---
## Chi tiết từng thao tác
Mọi thao tác vẽ nhận `l` (layer, mặc định `0`; chưa có thì tạo với màu `mau`, mặc định 7). Thao tác sai không làm hỏng các thao tác khác: nó vào `loi` với `i` (vị trí trong `ops`), `op`, `ly_do`.

| op | Trường | Ghi chú |
|---|---|---|
| `luoi_truc` | `x`, `y` (dãy nhịp), `cao`, `r` (bán kính bóng, mặc định 1,15 × cao), `du` (đoạn trục kéo dài, mặc định 5 × cao), `ten_x`, `ten_y`, `p`, `kt` | Trục đứng đánh theo `ten_x`, trục ngang theo `ten_y`. Layer `TRUC` (đỏ, nét CENTER), `KH_TRUC` (bóng + tên), `DIM_TRUC` (kích thước nhịp + tổng ở dưới và bên trái) |
| `dat` | `ma`, `p`, `goc` | Block lấy từ thư viện `PCCC_Blocks.dwg`, layer `PCCC_<hệ>`, có thuộc tính `MA_VT` để `PCCC_BOC` đếm. Van sprinkler: `SPR_VAN_OSY_DN<DN ống chính>` (65–150), `SPR_CTDC` (công tắc dòng chảy), `SPR_VAN_XK` (van xả khí); `goc` 90 trên ống đứng |
| `ong` | `ma`, `p` | Polyline trên layer `PCCC_<mã>`; `PCCC_BOC` tính chiều dài theo layer này |
| `net` | `l`, `p`, `kin`, `mau` | Polyline thường (tường, vùng, khung) |
| `tron` | `l`, `c`, `r` | |
| `chu` | `l`, `p`, `t`, `cao` (mặc định 250), `goc`, `can` | Chữ Arial có dấu tiếng Việt. `can:"giua"` căn tâm tại `p`, mặc định `p` là góc dưới trái |
| `kt` | `l`, `p1`, `p2`, `cach` (mặc định 500), `cao` (mặc định 250) | Kích thước song song, số nguyên, kiểu `PCCC_KT` |
| `xoa` | `l`, `hop`, `tat_ca` | Phải có `l` hoặc `hop`. Chỉ xoá đối tượng nằm trọn trong `hop`. Xoá hết: `"tat_ca":true` |
| `doc` | `gi`, `l`, `hop` | `layer`: số đối tượng theo layer. `vung`: polyline kín trên `l` (mặc định `PCCC_VUNG`) kèm `m2`, `hop`, đỉnh, handle `h`. `chu`: nội dung + vị trí chữ. Tối đa 40 dòng, phần dư báo ở `con` |
| `dem` | | Số thiết bị (cái) và ống/dây (m) theo mã, cùng cách đếm với `PCCC_BOC` |
| `rai` | `ma`; vùng: `vung` hoặc `hop` hoặc `l` (+ `hop` để lọc), hoặc `tim`; `s` hoặc `buoc` (số hoặc [x, y]); `dt`, `le` (số hoặc [x, y]), `theo_hang`, `chan` (mặc định 50 mm), `goc`, `kt` | Rải thiết bị theo lưới đặt giữa vùng. `s`: n = ceil(W / s), bước chia đều rồi làm tròn xuống theo `chan`, dư dồn đều ra hai tường. `dt` (m²) = diện tích bảo vệ tối đa của một thiết bị: đi cùng `s` thì lấy số cột × số hàng ÍT thiết bị nhất mà ô lưới không vượt `dt` (phòng 16 × 12 m, `s` 4000, `dt` 12 ra bước 4000 × 3000, không phải 4000 × 4000); đi cùng `buoc` mà vượt thì báo lỗi. `buoc`: bước đúng, số thiết bị nhiều nhất mà cách tường còn ≥ `le` (mặc định bước / 4). `le` đi cùng `s`: giữ số thiết bị, dàn bước tới `s` = min(`s`, (W − 2·`le`) / (n − 1)) để cách tường gần `le` thay vì nửa bước (`le` quá `s` / 2 thì lấy `s` / 2); vượt `dt` thì bỏ làm tròn, rồi bỏ `le` (chia đều). `theo_hang`:true: mỗi hàng chia lại theo bề rộng trống thật của hàng đó (phòng chữ L có WC): hàng rộng bằng vùng lấy đúng lưới chung (số và vị trí); hàng hẹp hơn chia theo `s` ít thiết bị nhất, ô của hàng vượt `dt` thì thêm thiết bị (bước hàng có thể khác bước lưới); với `buoc` ô của hàng vượt `dt` thì báo lỗi. `tim` [[x,y],...] (hành lang): rải dọc đường tim, đỉnh giữa là chỗ BẮT BUỘC có thiết bị (góc, chỗ giao hành lang; muốn thêm thì chèn đỉnh thẳng hàng), giữa hai đỉnh chia đều bước ≤ `s`; hai đầu tim là tường mút: thêm ít thiết bị nhất để bước ≤ `s`, cách tường bằng nửa bước (có `le` thì dàn bước tới `s`, cách tường gần `le`); tim khép kín (điểm cuối trùng điểm đầu) thì mọi đỉnh bắt buộc; không xét `dt`; kết quả `n`, `bx` (bước lớn nhất), `tuong` [đầu, cuối]. `goc` (độ) xoay lưới cho phòng nghiêng. Vùng không chữ nhật: lưới theo hộp bao, bỏ điểm ngoài đa giác. `kt`:true ghi chuỗi kích thước tường–thiết bị–tường trên layer `PCCC_KT` (chỉ khi `goc` = 0). Thiếu cả `s` và `buoc` thì báo lỗi. Quá 5000 thiết bị một vùng thì báo lỗi (sai đơn vị). Một vùng lỗi thì cả thao tác không rải vùng nào (gửi lại không bị trùng). Kết quả `rai`: mỗi vùng `n`, `nx`, `ny`, `bx`, `by`, `tuong` [x, y], `m2` (diện tích bảo vệ thực của một thiết bị = Sx × Sy, mỗi phương S là số lớn hơn giữa bước và 2 lần cách tường), `ho` (% diện tích xa mọi thiết bị hơn nửa đường chéo ô lưới; > 0 thì phải xem lại) |
| `tuyen` | `ma` (ống/dây), `kieu`, `p`, `truc`, `tu`, `den`, `qua`, `nhan`, `cao`, `sai_so` (mặc định 100 mm) | Không có `p` thì dùng điểm của `rai` ngay trước, theo từng vùng. `noi_tiep`: MỘT polyline đi rắn bò qua mọi điểm, hết vùng này sang vùng kế; `tu` nối thêm đầu vào (hàng bắt đầu từ góc gần `tu`), `den` nối thêm đầu ra. `tia`: mỗi điểm một nét thẳng tới `den` (đèn báo phòng: phòng có bao nhiêu đầu báo thì bấy nhiêu nét). `xuong_ca`: mỗi vùng một ống góp vuông góc ống chính `truc` tới hàng xa nhất + mỗi hàng một nhánh tới ống góp. Ống góp trùng cột thiết bị gần `tu` (không có `tu` thì cột gần giữa), chỉ xét cột có chân nằm trong đoạn ống chính gần vùng nhất (không cột nào thì cột chân gần đoạn nhất); hoà thì lấy cột có x nhỏ hơn rồi y nhỏ hơn, chiều vẽ `truc` không đổi kết quả. `qua` [x,y] hoặc [[x,y],...]: chỉ chỗ đặt ống góp, mỗi điểm dùng cho vùng của `rai` chứa nó (điểm ngoài mọi vùng, hai điểm một vùng thì báo lỗi); vùng không có điểm thì tự chọn cột. Chân ống góp ra ngoài đoạn ống chính thì nối thêm một đoạn tới điểm gần nhất của cả polyline `truc` (có thể chéo hoặc vuông góc tới đoạn khác). `cot`: mỗi cột thiết bị (cùng vị trí dọc ống chính) một nhánh vuông góc ống chính tới thiết bị xa nhất của cột, không có nhánh song song ống chính; ống chính chạy sát hoặc xuyên qua phòng thì nhánh đi cả hai phía; chân nhánh ra ngoài đoạn ống chính thì nối thêm như `xuong_ca`; các chân cùng phía nối về cùng một điểm nằm trên đường trục thì chỉ vẽ đoạn từ chân xa nhất, điểm nối là đỉnh ngoài đường trục (ống chính có bậc) thì mỗi chân một đoạn. `nhanh`: mỗi điểm một đoạn vuông góc vào ống chính. `nhan`:true ghi chữ DN (từ mã `ONG_DN50`) dọc đoạn đủ dài, layer `PCCC_NHAN`; bỏ chữ khi trong bán kính cỡ chữ × số ký tự đã có chữ cùng nội dung (kể cả chữ của nền PDF). Kết quả `tuyen`: `kieu`, `doan`, `m`. Chưa né tường: là phương án nháp để kỹ sư chỉnh |
| `anh` | `hop`, `rong` (640, 800, 1024 mặc định, 1280, 1600; số khác thì lấy khổ lớn hơn kế tiếp) | In Model ra PNG (thiết bị `PublishToWeb PNG.pc3` của AutoCAD), bỏ `hop` = toàn bản vẽ. Đặt ở đâu trong `ops` cũng chạy SAU khi cả gói vẽ xong. Kết quả `anh`: mảng đường dẫn file trong `%LOCALAPPDATA%\TKPCCC\cad\anh` (tự xoá sau 1 ngày, cần giữ thì chép đi). Ảnh luôn khổ ngang 4:3 hoặc 5:4, vùng không cùng tỉ lệ thì có lề trắng. Công cụ `cad_anh` là thao tác này gọi riêng |
| `pdf` | `file` (đường dẫn tuyệt đối), `trang` (mặc định 1), `tl`, `p` (điểm đặt góc dưới trái trang, mặc định `goc` của gói), `goc` (độ xoay) | Nhập nét và chữ vector của một trang PDF bằng `-PDFIMPORT`, ra đúng kích thước thật. `trang` ngoài số trang của file thì báo lỗi, không gọi lệnh. `tl` = mẫu số tỉ lệ của trang (in 1:100 thì 100), tính từ chuỗi kích thước bằng `python -I cad/.claude/skills/tkpccc-ve-cad/scripts/pdf_ti_le.py <file.pdf> [trang]` (trả `tl`, `cap`, `tin_cay`, khổ giấy). Mọi `pdf` của gói chạy TRƯỚC các thao tác khác. Trang xoay trong PDF vào đúng chiều đang hiển thị. Kết quả `pdf`: mỗi lần nhập `n` (số đối tượng, đã cộng vào `ve`) và `l` (layer đã dùng: lần nhập đầu `PDF_Geometry`, `PDF_Text`, `PDF_Solid Fills`, các lần sau AutoCAD đặt `PDF2_...`, `PDF3_...`). Nhập lại (vd sửa tỉ lệ): xoá nền cũ bằng `xoa` theo ĐÚNG tên layer ở `pdf[].l` của lần trước, ở một gói RIÊNG gửi trước gói nhập lại (cùng gói thì `pdf` chạy trước `xoa`, `xoa` sẽ xoá luôn nền mới). PDF ảnh quét không có nét vector thì báo lỗi. Chữ font VNI nhập vào bị sai dấu (chưa chuyển mã). Nhập xong đo lại một kích thước đã biết: `doc` `gi`:"chu" với `hop` quanh hàng bóng trục, so khoảng cách hai nhãn trục với tổng kích thước |

Cấp gói: `goc` [x,y], `zoom`:true (zoom toàn bản vẽ sau khi vẽ, tốn thêm một bước Undo), `ban_ve`:"ten.dwg" (khoá: bản vẽ đang hiện không đúng tên này thì từ chối cả gói, không vẽ gì; tên lấy từ `cad_tt` hoặc trường `ban_ve` của kết quả trước. Nên ghi khi vẽ nhiều lượt vào cùng một bản vẽ).

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
- Cần nhìn lại hình: thêm `{"op":"anh"}` vào cuối gói vẽ (không tốn thêm lần gọi), `rong` nhỏ nhất còn đọc được, `hop` bó sát vùng cần xem.
- Đọc bản vẽ: dùng `doc` có `l` hoặc `hop` để thu hẹp, không đọc cả bản vẽ.
- Hình lặp lại nhiều lần: xin thêm thao tác tham số trong `cad/module/plugin/Spec.cs` thay vì liệt kê từng nét.
