# TKPCCC — Thiết kế, khảo sát, bóc khối lượng, báo giá PCCC

Plugin AutoCAD 2021 + web nội bộ (LAN). Đã có: Pha 1 (thư viện block → vẽ → bóc khối lượng → báo giá Excel),
Pha 2 (hướng dẫn thiết kế B0–B7, báo cáo khảo sát Word, báo giá sơ bộ theo m²) và Pha 3 (thẩm định bản vẽ PDF theo QCVN 06).
Tiến độ: `TIEN_DO.md` · Cấu trúc thư mục: `STRUCTURE.md` · Các pha: `ROADMAP.md` · Nhật ký: `LICH_SU.md`.

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
Thẩm định PDF cần Python 3 (python.org, tick "Add python.exe to PATH"):
```
python -m pip install pymupdf pyyaml
```

## Sử dụng
1. Bấm đúp `chay.bat` (bật runner + mở web), hoặc `npm start` → mở http://127.0.0.1:8787
   (cho máy khác trong LAN: `npm start -- --host 0.0.0.0`). Kết quả từng công trình lưu ở `ket_qua/<dự án>/`.
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
   - **Thẩm định PDF**: chọn PDF bản vẽ (vector, không phải bản scan), điền thông tin công trình nếu bản vẽ không ghi (nhóm F,
     chiều cao PCCC…), "Chạy thẩm định" → bảng Không đạt / Cần xác minh / Đạt kèm trang + điều khoản QCVN 06, "Mở PDF ghi chú"
     (khung đánh số #n khớp bảng). Dòng "Cần xác minh" có nút "Claude xét" (Claude tra `knowledge/`, `skills/`, xem đúng trang PDF).
     Đổi thông tin rồi bấm Chạy lại không cần chọn lại file. Không tốn token trừ khi bấm "Claude xét".

Danh mục vật tư: `data/vat_tu.json` (sửa xong chạy lại `cad\build.bat`). Đơn giá đã nhập lưu ở `data/don_gia.json`.

## Kiểm tra
```
npm test
python -m pytest thamdinh/test -q
powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1
```
Cái cuối chạy plugin trong `accoreconsole` (AutoCAD không giao diện) với bản vẽ thử, không cần mở AutoCAD.

## Các pha tiếp theo
Xem `ROADMAP.md` (Pha 4: rải thiết bị tự động, thủy lực TCVN 7336).

Mã nguồn: https://github.com/taelomuhicd84/PCCC-CLONE
