# TKPCCC — Thiết kế, khảo sát, bóc khối lượng, báo giá PCCC

Plugin AutoCAD 2021 + web nội bộ (LAN). Đã có: Pha 1 (thư viện block → vẽ → bóc khối lượng → báo giá Excel) và
Pha 2 (hướng dẫn thiết kế B0–B7, báo cáo khảo sát Word, báo giá sơ bộ theo m²). Tiến độ: `TIEN_DO.md`.

## Cài đặt (1 lần)
```
npm install
cad\build.bat          :: biên dịch plugin bằng csc.exe có sẵn của Windows, không cần cài .NET SDK
```
Claude (không cần API key — dùng tài khoản Claude đăng nhập trong Claude Code):
```
npm i -g @anthropic-ai/claude-code      :: nếu máy chưa có lệnh claude
claude                                  :: đăng nhập 1 lần (/login), rồi thoát
```

## Sử dụng
1. `npm start` → mở http://127.0.0.1:8787 (cho máy khác trong LAN: `npm start -- --host 0.0.0.0`).
2. AutoCAD: `NETLOAD` → `cad\TKPCCC.bundle\Contents\TKPCCC.Cad.dll`
   (tự nạp mỗi lần mở: chép thư mục `cad\TKPCCC.bundle` vào `%APPDATA%\Autodesk\ApplicationPlugins`).
   Build lại khi AutoCAD đang mở vẫn được; tắt hẳn AutoCAD rồi mở lại để nạp bản mới.
3. Lệnh AutoCAD (`PCCC_HELP` để xem):

   | Lệnh | Việc |
   |---|---|
   | `PCCC_LIB` | Nạp/cập nhật block PCCC từ `PCCC_Blocks.dwg` (cạnh DLL). Sửa hình block trong file đó (BEDIT), lưu, chạy lại. `Taomoi` = cất bản cũ, tạo lại mặc định. Thêm mã vào `vat_tu.json` thì tự thêm block mặc định. |
   | `PCCC_DAT` | Đặt thiết bị theo mã (`?` = danh sách; gõ một phần tên không dấu, vd `dau bao khoi`). Layer `PCCC_<hệ>`, attribute `MA_VT`, tự đổi tỉ lệ theo đơn vị bản vẽ. |
   | `PCCC_ONG` | Vẽ ống/dây: chọn mã (vd `ONG_DN50`) → layer `PCCC_ONG_DN50` → PLINE. |
   | `PCCC_GAN` | Bản vẽ không vẽ bằng plugin: chọn block có sẵn, gán mã vật tư (lưu `block_alias.json`, dùng cho mọi bản vẽ). |
   | `PCCC_BOC` | Bóc khối lượng: đếm block theo `MA_VT`/tên/đã gán (bỏ bản copy đè, đếm block lồng và MINSERT, bỏ XREF) + chiều dài tuyến theo layer. Tuỳ chọn tách theo tầng/khu (chọn polyline kín, tên lấy chữ to nhất bên trong). Gửi lên web; runner tắt thì lưu `<bản vẽ>_KL.json`. |
   | `PCCC_BANG` | Chèn bảng thống kê (ký hiệu, tên, ĐVT, số lượng theo khu) của lần `PCCC_BOC` gần nhất. |
   | `PCCC_VUNGBV` | Vẽ vòng bảo vệ bán kính R (m, người thiết kế nhập theo tiêu chuẩn) quanh thiết bị, layer không in; báo thiết bị đứng lẻ. |
4. Web:
   - **Hướng dẫn thiết kế**: các bước B0–B7, tài liệu quy chuẩn nguyên văn, hỏi Claude theo tài liệu của bước.
   - **Khảo sát**: tạo dự án, điền số liệu (gõ kiểu "1.200", "3,5" được), "Claude gợi ý" điền phần đánh giá, tải Word,
     "Ước khối lượng sơ bộ" → báo giá sơ bộ.
   - **Khối lượng & Báo giá**: chọn nguồn (bóc từ CAD / sơ bộ), tải đơn giá mẫu, điền giá, tải lên, xuất báo giá `.xlsx`
     (có công thức; có sheet theo tầng/khu nếu bóc có tách khu).

Danh mục vật tư: `data/vat_tu.json` (sửa xong chạy lại `cad\build.bat`). Đơn giá đã nhập lưu ở `data/don_gia.json`.

## Kiểm tra
```
npm test
powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1
```
Cái thứ hai chạy plugin trong `accoreconsole` (AutoCAD không giao diện) với bản vẽ thử, không cần mở AutoCAD.

## Các pha tiếp theo
- Pha 3: đọc PDF + thẩm định (lõi Python từ `D:\1_Claude\TestPCCC\PCCC`).
- Pha 4: `PCCC_RAI` rải đầu phun/đầu báo tự động theo lưới, thủy lực TCVN 7336.
