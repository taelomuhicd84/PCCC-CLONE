# Việc 2 (người dùng test 13/11): tự nhận diện trục / tường, bấm điểm là ra vùng

Nhánh: `claude/cad-nhan-dien-tuong-89708b` (worktree riêng, chưa merge vào main, chưa push).

## Đã làm (13/11)
- `cad/module/plugin/NhanVung.cs` (file mới): `VungTaiDiem` dựng đa giác phòng chứa điểm bấm, chỉ đọc DatabaseServices (chạy được trong accoreconsole,
  không phụ thuộc vùng zoom như `Editor.TraceBoundary`).
  - Bỏ: nét có gạch (kiểu nét `NumDashes > 0`: CENTER, DASHDOT, khuất), layer có TRUC/AXIS/GRID/CENTER/DIM/CUA/DOOR/WINDOW/NOITHAT/FURN/DEFPOINTS
    (so tên đã bỏ dấu), layer `PCCC_*` (trừ `PCCC_VUNG`), layer tắt/đóng băng, chữ, kích thước, Xline/Ray, Table.
  - Lấy: Line, Polyline (cung chia nhỏ), Arc/Circle/Ellipse/Spline (24 khúc), biên Hatch (cột tô đặc), nét trong block (Explode đệ quy, layer 0 kế thừa).
  - Có layer TUONG/WALL/COT/COLUMN/VACH thì dò bằng các layer đó trước, không ra mới dò mọi nét liền.
  - Nối khe giữa hai đoạn THẲNG HÀNG ≤ `ho` (mặc định 3 m, `HO_CUA_M`): ô cửa giữa hai má tường, cột chỉ có hatch.
  - Cắt đoạn tại giao điểm / chữ T hụt ≤ 2 mm / chồng nhau, gộp đỉnh, bỏ nhánh cụt, đi mặt (half-edge), lấy mặt CCW nhỏ nhất chứa điểm, bỏ đỉnh thẳng hàng.
  - Ô dò quanh điểm 10 m, gấp đôi tới khi mặt nằm trọn trong ô hoặc ô phủ cả bản vẽ.
- Thao tác spec `vung_tu_dong` (`p`, `ho`, `l`, `mau`) trả `vung_tu_dong`: `m2`, `dinh`, `hop` (theo gốc, làm tròn ra ngoài, dán vào `hop` của `rai`).
  Móc trong `Spec.cs`: 1 dòng `MotOp`, 1 trường `GoiSpec.KqVung`, 1 dòng ghi kết quả.
- `PCCC_RAI` bấm điểm (`Rai.cs`, 3 dòng): gọi `VungBam` trước (vẽ polyline lên `PCCC_VUNG`, in `Vùng tự dựng m2=...`), không ra mới dùng `TraceBoundary` như cũ.
- Phép kiểm `chay_thu.ps1`: 2 phòng tường 2 nét 200 trên layer trung tính `KT_A`, cửa 1000 có má tường, cột góc phòng 1 CHỈ có hatch (vẽ bằng `-HATCH`),
  cột góc phòng 2 polyline, cột đảo giữa phòng, chữ + kích thước trong phòng, trục `luoi_truc` + một trục CENTER trên layer trung tính `NEN_A` cắt cả hai phòng:
  hai phòng ra đúng 27,8 m² (6 đỉnh), `ho` 500 thì hai phòng thông nhau 55,8 m²; điểm ngoài công trình báo lỗi; `PCCC_RAI` bấm điểm ra 27,8 m² rồi rải.
- `TU_VUNG.md` (tóm tắt + bảng chi tiết), `README.md`. Tóm tắt TU_VUNG đang đúng 1900 ký tự = trần của `thu_mcp.ps1`: thêm chữ vào tóm tắt thì phải rút chỗ khác.

## Kết quả kiểm
- `cad\build.bat` đạt; `thu_mcp.ps1` TAT CA DAT; `chay_thu.ps1`: xem commit cuối (ghi kết quả ở đây khi chạy xong).

## Chưa làm / bước tiếp
1. Chưa thử trên bản vẽ kiến trúc THẬT của người dùng (`ket_qua/cad_test/2026-11-13/2_mat_bang_khong_polyline.png` chỉ là ảnh). Khi có DWG: bấm vài phòng,
   xem layer nào lọt (tường trên layer có chữ CUA? cửa vẽ nét liền trên layer tường?), chỉnh `LOP_BO`/`LOP_TUONG` trong `NhanVung.cs`.
2. Chưa lọc "cặp nét song song" (tường 2 nét) ở chế độ mọi nét liền: đồ đạc vẽ nét liền trên layer trung tính vẫn có thể thành cạnh phòng
   (mặt nhỏ hơn phòng khi bấm trúng trong đồ đạc). Làm khi gặp bản vẽ thật bị lỗi này.
3. Khe không thẳng hàng (góc phòng hở, tường dừng không có má) chưa nối: phòng hở như vậy báo "không có đường bao kín".
4. Merge nhánh này với nhánh của phiên sửa `Rai.cs` (dim chéo góc, cách tường): xung đột có thể ở 3 dòng trong `RaiTay` (chỗ gọi `TraceBoundary`)
   và dòng `rai` trong tóm tắt `TU_VUNG.md`.
