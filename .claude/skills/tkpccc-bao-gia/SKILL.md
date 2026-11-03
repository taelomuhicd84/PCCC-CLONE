---
name: tkpccc-bao-gia
description: Module khối lượng – báo giá – khảo sát trên web — danh mục vật tư, khối lượng bóc từ CAD, đơn giá mẫu, báo giá Excel có công thức, tab Khảo sát 8 mục (Word, Claude gợi ý, ước khối lượng sơ bộ theo m²). Dùng khi sửa hoặc phát triển báo giá, đơn giá, vật tư, khối lượng, tab Khảo sát hay tab Khối lượng & Báo giá trên web.
model: sonnet
effort: medium
---
# Module: khối lượng, báo giá, khảo sát web
Phát triển module này: chỉ đọc file trong bảng dưới. Báo cáo khảo sát – tư vấn theo bảng đối chiếu (có ghi chú PDF) là skill `tkpccc-khao-sat-tu-van`.

| File | Việc |
|---|---|
| `data/vat_tu.json` | danh mục vật tư: nguồn DUY NHẤT cho plugin + báo giá (loai: block / tuyen / khac) |
| `data/don_gia.json` | đơn giá người dùng đã nhập (tự sinh, không commit) |
| `shared/src/bao_gia.ts` | khối lượng × danh mục × đơn giá → báo giá theo hệ; `docSoVN` |
| `shared/src/khao_sat.ts` | khảo sát 8 mục: chuẩn hoá, ghép gợi ý Claude, `uocKhoiLuong` theo m² |
| `runner/src/xuat_xlsx.ts` | đơn giá mẫu, đọc đơn giá, báo giá có công thức, sheet theo tầng/khu |
| `runner/src/xuat_docx.ts` | Word báo cáo khảo sát |
| `runner/src/server.ts` | route `/api/vat-tu`, `/api/khoi-luong`, `/api/don-gia`, `/api/don-gia/mau`, `/api/bao-gia`, `/api/bao-gia.xlsx`, `/api/khao-sat`, `/api/khao-sat.docx`, `/api/khao-sat/goi-y`, `/api/khao-sat/so-bo`, `/api/du-an` |
| `web/index.html` | tab "Khảo sát" (`ks`), "Khối lượng & Báo giá" (`bg`) |
| `knowledge/bao-gia-pccc.md`, `knowledge/khao-sat-bao-gia.md` | quy tắc báo giá sơ bộ, 8 mục khảo sát |
| `shared/test/bao_gia.test.ts`, `shared/test/khao_sat.test.ts`, `shared/test/mau.ts`, `runner/test/runner.test.ts` | test |

Bóc khối lượng trong AutoCAD: lệnh `PCCC_BOC` (skill `tkpccc-cad-console`) gửi `POST /api/khoi-luong`.

## Quy tắc
- Thêm vật tư: 1 dòng trong `data/vat_tu.json` → `cad\build.bat` → `PCCC_LIB` (tự thêm block mặc định cho mã mới).
- Số người dùng nhập kiểu Việt Nam ("1.200" = 1200, "3,5" = 3.5): đọc bằng `docSoVN`.
- Báo giá sơ bộ theo m² dùng quy tắc mặc định của `knowledge/bao-gia-pccc.md` (DN ống ghi "cỡ ống chờ thiết kế").

## Kiểm
`npx vitest run shared/test/bao_gia.test.ts shared/test/khao_sat.test.ts runner/test/runner.test.ts`.
