# Cấu trúc thư mục TKPCCC

Theo khung của dự án mẫu `PCCC_he_thong` (`C:\Users\HP\Downloads\PCCC_he_thong_2026-10-17\PCCC_he_thong`): mỗi module tách
`src/` (mã) và `test/` (kiểm thử), tri thức tách khỏi mã, kết quả từng công trình nằm riêng. Thêm/đổi/xoá thư mục thì **sửa file này**.

```
TKPCCC/
├─ CLAUDE.md            quy tắc cho Claude Code (đọc trước mọi phiên)
├─ README.md            giới thiệu, cài đặt, cách dùng
├─ STRUCTURE.md         file này: cấu trúc thư mục, đặt cái gì ở đâu
├─ ROADMAP.md           các pha, việc đã/chưa làm
├─ TIEN_DO.md           việc đang làm, việc tiếp theo (cập nhật mỗi mốc, trước khi hết usage)
├─ LICH_SU.md           nhật ký theo ngày
├─ chay.bat             1 cú bấm: bật runner (ẩn) + mở web
├─ package.json         1 gói Node chung (tsx, vitest, exceljs, docx, marked) — không dùng workspaces
├─ .claude/
│  ├─ launch.json         cấu hình xem trước web trong Claude Code (runner cổng 8787)
│  ├─ agents/             pccc-xay-dung (bảng KT), pccc-he-thong (bảng A, B): agent nhận job thẩm định, ghi file phần
│  └─ skills/             MỖI MODULE MỘT SKILL (bảng module ↔ skill ở CLAUDE.md); SKILL.md liệt kê mọi file của module
│     ├─ tkpccc-khao-sat-tu-van/  điều phối báo cáo khảo sát – tư vấn (bảng KS + ghi chú PDF)
│     ├─ tkpccc-tham-dinh/        điều phối thẩm định xây dựng (KT) + hệ thống (A, B)
│     ├─ tkpccc-bang-doi-chieu/   data/ (A1–A26, B1–B14, KT*, KS, index.json; A/B/KT chép SKILL-PCCC-NEW 24743f9),
│     │                           scripts/bang_doi_chieu.py (chon, khung, dong, tim, lamviec, job, gop, kiem, xlsx, md), test/
│     ├─ tkpccc-ghi-chu-ban-ve/   scripts/ghi_chu_pdf.py (ve, luoi, tim: annotation khung/callout/leader/nét/tường/cửa), test/
│     ├─ tkpccc-kiem-luat-pdf/ · tkpccc-thuy-luc/ · tkpccc-bao-gia/ · tkpccc-huong-dan/   thẻ module (code ở shared/ runner/ thamdinh/ web/)
│     └─ tkpccc-tra-quy-chuan/ · tkpccc-xong-dau-muc/   quy trình chung
│
├─ shared/              LÕI NGHIỆP VỤ dùng chung, hàm thuần (không đọc/ghi file, không server)
│  ├─ src/bao_gia.ts       khối lượng × danh mục × đơn giá -> báo giá theo hệ; docSoVN (đọc số kiểu Việt Nam)
│  ├─ src/khao_sat.ts      khảo sát 8 mục: chuẩn hoá, ghép gợi ý Claude, ước khối lượng sơ bộ theo m²
│  ├─ src/thuy_luc.ts      thủy lực sprinkler TCVN 7336 PL B: tính/kiểm, sơ bộ từ mặt bằng, so phương án DN, chọn số đầu C07
│  │                       (chép nguyên từ SKILL-PCCC-NEW 24743f9; test khớp Excel người dùng — sửa thì báo để đồng bộ)
│  └─ test/                *.test.ts + mau.ts (dữ liệu mẫu dùng chung cho test)
│
├─ runner/              SERVER CỤC BỘ (Node, chạy `npm start`, cổng 8787)
│  ├─ src/server.ts        API + phục vụ web/index.html; đọc/ghi data/ và ket_qua/
│  ├─ src/xuat_xlsx.ts     Excel: đơn giá mẫu, đọc đơn giá, báo giá có công thức, sheet theo tầng/khu
│  ├─ src/xuat_docx.ts     Word: báo cáo khảo sát
│  ├─ src/thamdinh.ts      gọi lõi Python thamdinh/ (`python -m pccc_checker run`), đọc findings.jsonl, câu hỏi "Claude xét" dòng XM
│  ├─ src/thuy_luc.ts      chuẩn hoá ô sơ bộ thủy lực; số liệu -> ô Excel mẫu + gọi scripts/xuat_thuy_luc_excel.py
│  ├─ src/engines/claude.ts  gọi `claude -p` bằng đăng nhập Claude Code (KHÔNG API key), chỉ công cụ đọc
│  └─ test/
│
├─ web/                 GIAO DIỆN: 1 file index.html (HTML + JS thuần, không build);
│                       5 tab Hướng dẫn · Khảo sát · Khối lượng & Báo giá · Thủy lực · Thẩm định PDF
│
├─ bang_tinh/thuy_luc/  EXCEL MẪU THỦY LỰC (chép từ SKILL-PCCC-NEW): MAU_THUY_LUC_7336.xlsx, o_nhap.json (bản đồ ô nhập), vi_du/
├─ scripts/             xuat_thuy_luc_excel.py (ghi ô Excel mẫu, giữ công thức; sửa F106 theo B.2.10.12),
│                       chen_hang_cuoi.ps1, hoan_thien_excel.ps1 (Excel COM; UTF-8 có BOM vì có chữ Việt)
│
├─ thamdinh/            THẨM ĐỊNH PDF (Python 3 + pymupdf + pyyaml; offline, không API key) — nguồn, phần đã cắt: thamdinh/README.md
│  ├─ pccc_checker/        gói Python chép từ repo PCCC (TestPCCC) ab546c7: đọc chữ vector, rule engine, ghi chú PDF; cli.py viết mới
│  ├─ rules/               qcvn06_2022_sd01_2023.yaml (26 luật thoát nạn, verified)
│  ├─ config/settings.yaml rule pack + mức ghi chú
│  ├─ scripts/make_sample_pdf.py  PDF mẫu có lỗi cài sẵn
│  └─ test/                pytest
│
├─ cad/                 DỰ ÁN CON AUTOCAD 2021–2024 (C# .NET Framework 4.8, cú pháp C# 5): bản đồ đầy đủ ở cad/README.md, luật riêng cad/CLAUDE.md
│  ├─ .claude/             skill riêng (tkpccc-ve-cad + scripts/pdf_ti_le.py, tkpccc-cad-console + scripts/in_png.ps1) và agent riêng của CAD
│  ├─ module/              mã chạy: plugin/ (Lenh.cs, Spec.cs, Rai.cs, Block.cs, Cau.cs) · ui/ (BangBlock.cs, bảng PCCC_BLOCK) · mcp/ (Mcp.cs) · chung/ (HangDoi.cs) · lisp/
│  ├─ skills/              TU_VUNG.md (từ vựng spec cho mọi AI, nguồn duy nhất) + vi_du/
│  ├─ data/                dữ liệu vẽ: ky_hieu/ (mặc định trục, kích thước, chữ) · block/ · ong/ · bom/ · mau/
│  ├─ rule/ · knowledge/   LUAT.md (việc CAD ↔ file quy chuẩn ↔ skill, agent) · quy ước bản vẽ, bài học vẽ
│  ├─ test/                chay_thu.ps1 (accoreconsole) · thu_mcp.ps1 (máy chủ MCP), không cần mở AutoCAD · thu_gui.ps1 (tự mở AutoCAD thật, không bắt buộc)
│  ├─ build.bat · bin/     build DLL + TKPCCC.Mcp.exe (bin/ không commit)
│  ├─ KE_HOACH.md · TIEN_DO.md · LICH_SU.md   kế hoạch, tiến độ, nhật ký riêng của CAD
│  └─ TKPCCC.bundle/       gói AutoCAD tự nạp: Contents/ (DLL do build tạo, PCCC_Blocks.dwg, block_alias.json)
│
├─ data/                DỮ LIỆU CẤU HÌNH
│  ├─ vat_tu.json          danh mục vật tư: nguồn DUY NHẤT cho plugin + báo giá (loai: block / tuyen / khac)
│  ├─ huong_dan.json       quy trình B0–B7: việc cần làm, tài liệu, lệnh CAD của từng bước
│  └─ don_gia.json         đơn giá người dùng đã nhập (tự sinh, không commit)
│
├─ knowledge/           TRI THỨC: quy chuẩn chép nguyên văn + quy tắc nghiệp vụ (chép từ PCCC_he_thong/knowledge)
│  ├─ dinh-nghia/          định nghĩa QCVN 06
│  ├─ qcvn06-*.md, qcvn10-2025-*.md, tcvn7568-14-2025.md, nghi-dinh-*.md, cv-2360-*.md
│  └─ khao-sat-bao-gia.md, bao-gia-pccc.md, so-bo-thiet-ke-pccc.md, chon-loai-dau-bao.md …
│
├─ data/mau_giai_phap/  thư viện mẫu giải pháp thoát nạn (17 mẫu: điều kiện lọc, điều khoản, lỗi hay gặp, giải pháp); chép từ SKILL-PCCC-NEW
├─ data/mau_bao_cao/    BCKS_mau.xlsx: mẫu báo cáo khảo sát MẶC ĐỊNH 4 mục (từ BCKS kho hoá chất, 2026-11-03; BCKS_mau_7muc.xlsx mẫu cũ; mẫu trắng, đã xoá nội dung khách),
│                       điền bằng `bang_doi_chieu.py bcks`
│
├─ skills/              ĐIỀU KHOẢN THEO CHUYÊN ĐỀ (chép từ PCCC_he_thong/skills): MOD_*.md, SKILL_*.md, PHU_LUC_G_QCVN06.md
│
└─ ket_qua/             KẾT QUẢ TỪNG CÔNG TRÌNH (tự sinh, không commit): ket_qua/<dự án>/
                        khao_sat.json, khoi_luong.json (bóc từ CAD), khoi_luong_so_bo.json (ước theo khảo sát),
                        thamdinh/ (ban_ve.pdf, ban_ve_annotated.pdf, findings.jsonl, tom_tat.json, claude_xet.json),
                        thuy_luc/ (mat_bang.json, so_lieu.json, thuy_luc.xlsx),
                        khảo sát – tư vấn / thẩm định theo bảng đối chiếu: ban_ve.pdf, doi_chieu.json (bảng + ghi_chu),
                        doi_chieu.xlsx, ban_ve_ghi_chu.pdf, BCKS_<tên>.xlsx + anh_bcks/ (báo cáo theo mẫu), anh/ (luoi_<trang>.png, ra/),
                        jobs/ + phan/ (job và phần của agent)
```

## Mỗi lớp một việc
| Lớp | Thư mục | Ai sửa | Commit |
|---|---|---|---|
| Quy tắc, tiến độ | `CLAUDE.md`, `TIEN_DO.md`, `LICH_SU.md`, `ROADMAP.md`, `STRUCTURE.md` | Claude cập nhật mỗi mốc | có |
| Quy trình lặp lại của Claude | `.claude/skills/<tên>/SKILL.md` (+ `scripts/`) | Claude gom việc hay thành skill | có |
| Nghiệp vụ (tính toán) | `shared/src` | Claude, kèm test trong `shared/test` | có |
| Server, xuất file, gọi Claude | `runner/src` | Claude, kèm test trong `runner/test` | có |
| Giao diện | `web/index.html` | Claude | có |
| Thẩm định PDF (Python) | `thamdinh/` (luật: `thamdinh/rules/*.yaml`) | Claude, kèm test trong `thamdinh/test` | có |
| AutoCAD | `cad/module/plugin/Lenh.cs` | Claude, kiểm bằng `cad/test/chay_thu.ps1` | có |
| Thư viện block, gán block | `cad/TKPCCC.bundle/Contents/PCCC_Blocks.dwg`, `block_alias.json` | người dùng (BEDIT, `PCCC_GAN`) | có |
| Danh mục, quy trình | `data/vat_tu.json`, `data/huong_dan.json` | người dùng / Claude | có |
| Tri thức, điều khoản | `knowledge/`, `skills/` | chép nguyên văn bằng script, không viết theo trí nhớ | có |
| Đơn giá, kết quả công trình | `data/don_gia.json`, `ket_qua/` | tự sinh khi dùng | **không** |

## Luồng dữ liệu
```
AutoCAD: PCCC_DAT / PCCC_ONG vẽ ─► PCCC_BOC ──POST /api/khoi-luong──► ket_qua/<dự án>/khoi_luong.json ─┐
Web: tab Khảo sát ─► ket_qua/<dự án>/khao_sat.json ─► "Ước khối lượng" ─► khoi_luong_so_bo.json ────────┤
                                                                                                     ▼
data/vat_tu.json + data/don_gia.json ──► shared/src/bao_gia.ts ──► runner/src/xuat_xlsx.ts ──► báo giá .xlsx
Claude ("Claude gợi ý", "Hỏi") ◄── runner/src/engines/claude.ts (`claude -p`, đọc knowledge/ + skills/)
Web: tab Thủy lực ─POST /api/thuy-luc/so-bo|tinh─► shared/src/thuy_luc.ts ─► ket_qua/<dự án>/thuy_luc/so_lieu.json
   ─GET /api/thuy-luc.xlsx─► runner/src/thuy_luc.ts ─► scripts/xuat_thuy_luc_excel.py (+ Excel COM) ─► thuy_luc.xlsx
Web: tab Thẩm định PDF ─POST /api/tham-dinh─► ket_qua/<dự án>/thamdinh/ban_ve.pdf ─► runner/src/thamdinh.ts ─► python thamdinh/
   ─► findings.jsonl (D/KD/XM) + PDF ghi chú ─► bảng kết luận; dòng XM ─"Claude xét"─► claude_xet.json
```

## Thêm mới thì đặt ở đâu
- Module mới (người dùng dặn 2026-11-02): `.claude/skills/tkpccc-<tên>/SKILL.md`; module chỉ Claude dùng thì để luôn `scripts/`, `data/`,
  `test/` trong thư mục skill; thêm 1 dòng vào bảng module ↔ skill trong `CLAUDE.md`. Agent con: `.claude/agents/pccc-<việc>.md`.
- Vật tư / thiết bị mới: 1 dòng trong `data/vat_tu.json` → `cad\build.bat` → `PCCC_LIB` (tự thêm block mặc định cho mã mới).
- Phép tính nghiệp vụ mới (ước khối lượng, thủy lực…): file mới trong `shared/src/` + test trong `shared/test/`.
- API mới: route trong `runner/src/server.ts`; xuất định dạng mới: `runner/src/xuat_<định dạng>.ts`; engine AI mới: `runner/src/engines/`.
- Lệnh AutoCAD mới: `cad/module/plugin/Lenh.cs` (bọc `Chay(...)`), thêm dòng kiểm vào `cad/test/chay_thu.ps1`, khai báo trong `PackageContents.xml`.
- Quy chuẩn mới: nguyên văn vào `knowledge/` (văn bản, bảng) hoặc `skills/` (điều khoản theo chuyên đề); nối vào bước trong `data/huong_dan.json`.
- Luật thẩm định PDF mới: thêm vào `thamdinh/rules/qcvn06_2022_sd01_2023.yaml` (check có sẵn) hoặc hàm `@register` trong
  `thamdinh/pccc_checker/rules/checks.py`; thêm ca kiểm vào `thamdinh/test/`.
- Bước thiết kế mới: 1 mục trong `data/huong_dan.json`.
- Script tiện ích (py/ps1/mjs): `scripts/`; mẫu Excel: `bang_tinh/`. Sau này nếu cần: `so_do/` (sơ đồ html) — giống dự án mẫu.

## Quy ước đặt tên
- Thư mục, file mã: tiếng Việt không dấu, chữ thường, `snake_case` (`khao_sat.ts`, `xuat_xlsx.ts`); tri thức `.md`: `kebab-case` như mẫu.
- Tài liệu gốc dự án: CHỮ HOA (`TIEN_DO.md`, `LICH_SU.md`, `STRUCTURE.md`).
- Đường dẫn tài liệu trong `data/huong_dan.json` ghi từ gốc dự án (`knowledge/…`, `skills/…`).
- File `.bat`, `.ps1`: chỉ chữ ASCII (cmd và PowerShell 5.1 đọc sai UTF-8 không BOM). Ngoại lệ: `.ps1` cần chữ Việt (ghi vào Excel)
  thì lưu UTF-8 **có BOM** (`scripts/chen_hang_cuoi.ps1`, `scripts/hoan_thien_excel.ps1`).
