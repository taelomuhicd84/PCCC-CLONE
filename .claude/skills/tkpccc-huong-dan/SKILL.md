---
name: tkpccc-huong-dan
description: Module hướng dẫn thiết kế B0–B7 trên web — quy trình từng bước (data/huong_dan.json), hiện tài liệu nguyên văn, hỏi Claude theo tài liệu của bước qua `claude -p` không API key. Dùng khi sửa hoặc thêm bước thiết kế, tài liệu gắn với bước, tab Hướng dẫn, hoặc cách runner gọi Claude.
model: sonnet
effort: medium
---
# Module: hướng dẫn thiết kế B0–B7
Phát triển module này: chỉ đọc file trong bảng dưới.

| File | Việc |
|---|---|
| `data/huong_dan.json` | các bước B0, B1, B4, B5a, B5b, B6a, B6b, B6c, B7: việc cần làm, tài liệu (đường dẫn từ gốc dự án), lệnh CAD |
| `runner/src/server.ts` | route `/api/huong-dan`, `/api/tai-lieu`, `/api/claude` |
| `runner/src/engines/claude.ts` | gọi `claude -p` bằng đăng nhập Claude Code (KHÔNG API key), chỉ công cụ đọc, không nạp hook/plugin cá nhân; module khác cũng dùng |
| `web/index.html` | tab "Hướng dẫn thiết kế" (`data-tab="hd"`) |

## Quy tắc
- Tài liệu gắn bước là file `knowledge/`, `skills/` chép nguyên văn; thêm quy chuẩn mới theo skill `tkpccc-tra-quy-chuan` rồi nối vào `tai_lieu` của bước.
- Chưa đăng nhập Claude Code trên máy thì nút "Hỏi" báo lỗi: người dùng mở terminal gõ `claude` rồi `/login`.

## Kiểm
`npm test`; bật web (`chay.bat` hoặc `npm start`, cổng 8787) xem tab Hướng dẫn.
