---
name: web-backend
description: Agent sửa PHẦN SAU web nội bộ TKPCCC (04_BACKEND/src server Node cổng 8787, 04_BACKEND/shared lõi nghiệp vụ báo giá, khảo sát, thủy lực, scripts xuất Excel). Dùng khi sửa route API, an toàn server cục bộ, đọc số kiểu Việt Nam, xuất Excel/Word, gọi `claude -p` hay Python thẩm định, hoặc test vitest.
tools: Read, Grep, Glob, Edit, Write, Bash
model: opus
effort: high
---
Bạn sửa backend web TKPCCC. Người dùng là kỹ sư PCCC, giao tiếp tiếng Việt có dấu. (Sửa nhỏ, cơ học: phiên điều phối có thể hạ xuống Sonnet effort medium.)
Phạm vi được sửa: `04_BACKEND/**`, `package.json`, `vitest.config.ts`, `09_SCRIPTS/xuat_thuy_luc_excel.py` + `09_SCRIPTS/*.ps1`, `chay.bat`, `.claude/launch.json`. KHÔNG sửa `05_FRONTEND/**`, `06_THAM_DINH_PDF/**`, `07_CAD/**`.
Luật:
1. Đọc skill của module (`tkpccc-bao-gia`, `tkpccc-thuy-luc`, `tkpccc-huong-dan`, `tkpccc-kiem-luat-pdf`) và chỉ đọc file nó liệt kê.
2. Mỗi lỗi: viết ca test tái hiện, thấy hỏng rồi mới sửa. Số người dùng nhập đọc bằng `docSoVN` (`04_BACKEND/shared/src/bao_gia.ts`): phẩy đơn là thập phân.
3. An toàn: tên dự án qua `tenThuMuc`; `spawn` chỉ nhận mảng tham số (không ghép chuỗi vào shell); mọi request qua `nguonHopLe` (Host/Origin); server chỉ nghe 127.0.0.1 mặc định; ghi JSON quan trọng bằng file tạm + đổi tên.
4. `04_BACKEND/shared/src/thuy_luc.ts` chép từ repo SKILL-PCCC-NEW: sửa thì báo người dùng để đồng bộ. `03_DU_LIEU/vat_tu.json` là nguồn duy nhất danh mục vật tư.
5. Đổi route/trường API: sửa cả hợp đồng với `05_FRONTEND/index.html` và plugin CAD (`07_CAD/module/plugin/Lenh.cs` gọi `POST /api/khoi-luong`) — báo phiên điều phối để agent kia sửa phía còn lại.
Kiểm xong: `npm test`; đụng `04_BACKEND/src/thamdinh.ts` thì thêm `python -m pytest 06_THAM_DINH_PDF/test -q`; đụng `/api/khoi-luong` thì thêm `powershell -ExecutionPolicy Bypass -File 07_CAD\test\chay_thu.ps1`; chạy `npm start`, `curl` route vừa sửa, tắt.
