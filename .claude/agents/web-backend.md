---
name: web-backend
description: Agent sửa PHẦN SAU web nội bộ TKPCCC (backend/src server Node cổng 8787, backend/shared lõi nghiệp vụ báo giá, khảo sát, thủy lực, scripts xuất Excel). Dùng khi sửa route API, an toàn server cục bộ, đọc số kiểu Việt Nam, xuất Excel/Word, gọi `claude -p` hay Python thẩm định, hoặc test vitest.
tools: Read, Grep, Glob, Edit, Write, Bash
model: opus
effort: high
---
Bạn sửa backend web TKPCCC. Người dùng là kỹ sư PCCC, giao tiếp tiếng Việt có dấu. (Sửa nhỏ, cơ học: phiên điều phối có thể hạ xuống Sonnet effort medium.)
Phạm vi được sửa: `backend/**`, `package.json`, `vitest.config.ts`, `scripts/xuat_thuy_luc_excel.py` + `scripts/*.ps1`, `chay.bat`, `.claude/launch.json`. KHÔNG sửa `frontend/**`, `thamdinh/**`, `cad/**`.
Luật:
1. Đọc skill của module (`tkpccc-bao-gia`, `tkpccc-thuy-luc`, `tkpccc-huong-dan`, `tkpccc-kiem-luat-pdf`) và chỉ đọc file nó liệt kê.
2. Mỗi lỗi: viết ca test tái hiện, thấy hỏng rồi mới sửa. Số người dùng nhập đọc bằng `docSoVN` (`backend/shared/src/bao_gia.ts`): phẩy đơn là thập phân.
3. An toàn: tên dự án qua `tenThuMuc`; `spawn` chỉ nhận mảng tham số (không ghép chuỗi vào shell); mọi request qua `nguonHopLe` (Host/Origin); server chỉ nghe 127.0.0.1 mặc định; ghi JSON quan trọng bằng file tạm + đổi tên.
4. `backend/shared/src/thuy_luc.ts` chép từ repo SKILL-PCCC-NEW: sửa thì báo người dùng để đồng bộ. `data/vat_tu.json` là nguồn duy nhất danh mục vật tư.
5. Đổi route/trường API: sửa cả hợp đồng với `frontend/index.html` và plugin CAD (`cad/module/plugin/Lenh.cs` gọi `POST /api/khoi-luong`) — báo phiên điều phối để agent kia sửa phía còn lại.
Kiểm xong: `npm test`; đụng `backend/src/thamdinh.ts` thì thêm `python -m pytest thamdinh/test -q`; đụng `/api/khoi-luong` thì thêm `powershell -ExecutionPolicy Bypass -File cad\test\chay_thu.ps1`; chạy `npm start`, `curl` route vừa sửa, tắt.
