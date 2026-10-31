---
name: tkpccc-cad-console
description: Build và kiểm thử plugin AutoCAD của TKPCCC (cad/src/Lenh.cs) KHÔNG cần mở AutoCAD — biên dịch bằng csc.exe (C# 5), chạy kịch bản lệnh trong accoreconsole, đọc kết quả dòng lệnh, in bản vẽ ra PNG để xem bằng mắt. Dùng mỗi khi sửa/thêm lệnh PCCC_*, gặp lỗi plugin (eWrongDatabase, đếm sai, chữ lệch), cần chạy thử bản vẽ mẫu, hoặc người dùng nói "chạy thử plugin", "test CAD", "kiểm tra lệnh AutoCAD", kể cả khi AutoCAD đang mở trên máy.
---

# Kiểm thử plugin AutoCAD bằng accoreconsole

AutoCAD 2021 có `accoreconsole.exe` (AutoCAD không giao diện): nạp được DLL .NET, chạy file kịch bản `.scr`, in ra dòng lệnh.
Nhờ vậy kiểm thử được plugin mà không đụng AutoCAD người dùng đang mở và không cần họ thao tác.

## Làm nhanh
- Kiểm thử hồi quy có sẵn (build ra thư mục tạm + kịch bản mẫu + kiểm số đếm):
  `powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1` → phải ra `TAT CA DAT`.
- Build vào bundle thật: `cad\build.bat` (chạy được khi AutoCAD đang mở: đổi tên DLL cũ thành `*.dll.cu*` vì Windows cho đổi tên
  DLL đang nạp nhưng không cho ghi đè; người dùng tắt hẳn AutoCAD rồi mở lại để nạp bản mới).
- Xem bản vẽ kết quả bằng mắt: `powershell -ExecutionPolicy Bypass -File .claude\skills\tkpccc-cad-console\scripts\in_png.ps1 <ban_ve.dwg> <ra.png>`
  rồi đọc ảnh PNG (Read). Dùng để kiểm hình block, chữ căn giữa, bảng, màu layer — những thứ đọc dòng lệnh không thấy.

## Ràng buộc biên dịch (vì sao)
- `csc.exe` của .NET Framework (`%WINDIR%\Microsoft.NET\Framework64\v4.0.30319`) chỉ hiểu **C# 5**: không `$"..."`, `?.`, `nameof`,
  `out var`, `=>` cho thuộc tính, `default` không kiểu. Máy không có .NET SDK.
- Chỉ tham chiếu `acdbmgd.dll` + `accoremgd.dll`, dùng `Autodesk.AutoCAD.ApplicationServices.Core.Application`. Tham chiếu `acmgd.dll`
  (lớp giao diện) thì accoreconsole không nạp được.
- `/codepage:65001` vì mã nguồn có tiếng Việt. Gọi `Exception` phải ghi rõ `System.Exception` (trùng tên với `Autodesk.AutoCAD.Runtime`).

## Viết kịch bản .scr (dễ sai nhất)
Mỗi dòng = một câu trả lời cho một dấu nhắc; dòng trống = Enter. Sai một dấu nhắc là các dòng sau lệch hết, console có thể quay vòng
mãi ⇒ **luôn chạy với giới hạn thời gian** (`$p.WaitForExit(180000)`, quá giờ thì `Stop-Process`) và đọc log để tìm dấu nhắc lệch.
- `MINSERT`: tên, điểm, tỉ lệ X, **tỉ lệ Y (dòng trống = theo X)**, góc, số hàng, số cột, khoảng hàng, khoảng cột.
- `TEXT` trong console kết thúc ngay sau 1 dòng chữ: KHÔNG thêm dòng trống (dòng trống sẽ lặp lại lệnh TEXT).
- Chọn đối tượng: console không chọn được bằng toạ độ điểm (`Nothing Selected`); dùng `L` (đối tượng vừa tạo), `W x1,y1 x2,y2`, hoặc Enter
  khi lệnh có nhánh "Enter = cả model space".
- Lưu bản vẽ (`_.SAVEAS 2018 "<tạm>\x.dwg"`) TRƯỚC `PCCC_BOC`: runner tắt thì plugin ghi `<bản vẽ>_KL.json` cạnh DWG; bản vẽ chưa lưu thì
  file rơi vào Documents của người dùng.
- Đặt `-WorkingDirectory` của accoreconsole vào thư mục tạm: lệnh PLOT ghi `plot.log` vào thư mục làm việc.

## Đọc kết quả
- Output dạng UTF-16LE: `[Text.Encoding]::Unicode.GetString(bytes) -replace "`0",""`. Đọc khi tiến trình còn chạy phải mở chia sẻ:
  `[IO.File]::Open(f,'Open','Read','ReadWrite')`.
- Lỗi plugin hiện dạng `[TKPCCC] Lỗi: ...` (mọi lệnh bọc `Chay(...)`). Tìm theo `Command: PCCC_X` chứ không theo `PCCC_X` trần
  (thông báo cũng có thể chứa tên lệnh).
- PowerShell không phân biệt hoa thường: `$a` đè `$A` — đặt tên biến khác hẳn nhau.

## Bẫy API AutoCAD đã gặp (đã sửa trong Lenh.cs, đừng tái phạm)
- Tạo đối tượng trong database phụ (thư viện block) mà `HostApplicationServices.WorkingDatabase` vẫn trỏ bản vẽ hiện hành ⇒ `eWrongDatabase`.
  Tạm đặt `WorkingDatabase = db` trong `try/finally`.
- `DBText` căn giữa trong database phụ không tự `AdjustAlignment` ⇒ chữ lệch tâm. Dùng `MText` (`Attachment = MiddleCenter`).
- `Table` kế thừa `BlockReference`: quét block phải bỏ `Table`, không thì ký hiệu trong bảng thống kê bị đếm như thiết bị.
- `PromptKeywordOptions`/`PromptStringOptions` có mặc định tự nối `[A/B] <mặc định>` vào câu nhắc: đừng tự viết lại (sẽ lặp).
- Block thư viện vẽ theo mm: chèn bằng API phải tự nhân tỉ lệ `0.001 / HeSoMet(db)` theo `INSUNITS`.
- Font SHX (`txt.shx`) không có dấu tiếng Việt: chữ tiếng Việt dùng kiểu chữ TrueType (`PCCC_ARIAL`).

## Khi thêm lệnh mới
Thêm `[CommandMethod]` bọc `Chay(...)`, khai báo trong `cad/TKPCCC.bundle/PackageContents.xml`, thêm bước vào kịch bản và một dòng kiểm
(`@('regex', 'mô tả')`) trong `cad/test/chay_thu.ps1`, chạy lại đến khi `TAT CA DAT`.
