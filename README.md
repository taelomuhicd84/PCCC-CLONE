# TKPCCC — Thiết kế, bóc khối lượng, báo giá PCCC

Plugin AutoCAD 2021 + web nội bộ. Pha 1: thư viện block → vẽ → bóc khối lượng → báo giá Excel.
Kế hoạch các pha sau (hướng dẫn thiết kế B0–B7, khảo sát, đọc PDF thẩm định, rải thiết bị tự động): xem cuối file.

## Cài đặt (1 lần)
```
npm install
cad\build.bat          :: biên dịch plugin bằng csc.exe có sẵn của Windows, không cần cài .NET SDK
```
Claude: cài Claude Code CLI và đăng nhập tài khoản Claude (không cần API key):
```
npm i -g @anthropic-ai/claude-code
claude                 :: đăng nhập 1 lần, rồi thoát
```

## Sử dụng
1. `npm start` → mở http://127.0.0.1:8787 (cho máy khác trong LAN: `npm start -- --host 0.0.0.0`).
2. AutoCAD: `NETLOAD` → `cad\TKPCCC.bundle\Contents\TKPCCC.Cad.dll`
   (tự nạp mỗi lần mở: chép thư mục `cad\TKPCCC.bundle` vào `%APPDATA%\Autodesk\ApplicationPlugins`).
3. Lệnh:
   | Lệnh | Việc |
   |---|---|
   | `PCCC_LIB` | Nạp/cập nhật block PCCC vào bản vẽ. Lần đầu tạo `PCCC_Blocks.dwg` cạnh DLL — mở file đó sửa hình block, lưu, chạy lại `PCCC_LIB`. |
   | `PCCC_DAT` | Đặt thiết bị theo mã (`?` = danh sách), layer `PCCC_<hệ>`, attribute `MA_VT`. |
   | `PCCC_ONG` | Vẽ ống/dây: chọn mã (vd `ONG_DN50`) → layer `PCCC_ONG_DN50` → PLINE. |
   | `PCCC_BOC` | Đếm block theo `MA_VT` + cộng chiều dài tuyến theo layer → gửi runner. Runner tắt thì lưu `<bản vẽ>_KL.json` cạnh DWG. |
4. Web: chọn dự án → tải đơn giá mẫu, điền giá, tải lên → xuất báo giá `.xlsx` (có công thức, sửa tiếp trong Excel).

Danh mục vật tư: `data/vat_tu.json` (nguồn duy nhất cho plugin + báo giá; sửa xong chạy lại `cad\build.bat`).
Đơn vị bản vẽ: theo `INSUNITS` (mm/cm/m); đơn vị khác coi là mm.

## Kiểm tra
```
npm test
```

## Các pha tiếp theo
- Pha 2: web hướng dẫn thiết kế B0–B7, báo cáo khảo sát (.docx), báo giá sơ bộ theo m² — tham khảo thêm repo PcccVipPro.
- Pha 3: đọc PDF + thẩm định (lõi Python từ `D:\1_Claude\TestPCCC\PCCC`).
- Pha 4: `PCCC_RAI` rải đầu phun/đầu báo tự động theo lưới, thủy lực TCVN 7336.
