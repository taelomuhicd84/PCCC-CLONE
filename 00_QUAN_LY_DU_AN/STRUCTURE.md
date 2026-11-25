# Cấu trúc thư mục TKPCCC

Cây gốc ĐÁNH SỐ theo mẫu danh mục công việc `DA-GT-2026-01` (người dùng giao 2026-11-25): số đầu = thứ tự đọc, tên CHỮ HOA không dấu,
mỗi số một mảng việc. Trong từng mảng giữ cấu trúc cũ (mã `src/` + `test/`, tri thức tách khỏi mã, kết quả công trình nằm riêng).
Ở gốc chỉ còn file công cụ bắt buộc: `CLAUDE.md`, `README.md`, `.claude/`, `.mcp.json`, `package.json`, `vitest.config.ts`, `chay.bat`.
Thêm/đổi/xoá thư mục thì **sửa file này**. Mảng mới: số kế tiếp (`13_...`), không chèn giữa.

```
TKPCCC/
├─ CLAUDE.md · README.md   quy tắc cho Claude Code (đọc trước mọi phiên) · giới thiệu, cài đặt, cách dùng
├─ chay.bat                1 cú bấm: bật runner (ẩn) + mở web
├─ package.json            1 gói Node chung (tsx, vitest, exceljs, docx, marked), không workspaces · vitest.config.ts
├─ .mcp.json               máy chủ MCP "cad" -> 07_CAD/bin/TKPCCC.Mcp.exe
├─ .claude/
│  ├─ launch.json            xem trước web trong Claude Code (runner cổng 8787)
│  ├─ agents/                pccc-xay-dung (bảng KT), pccc-he-thong (bảng A, B), web-frontend, web-backend
│  └─ skills/                MỖI MODULE MỘT SKILL (bảng module ↔ skill ở CLAUDE.md); SKILL.md liệt kê mọi file của module
│     ├─ tkpccc-khao-sat-tu-van/  điều phối báo cáo khảo sát – tư vấn (bảng KS + ghi chú PDF)
│     ├─ tkpccc-tham-dinh/        điều phối thẩm định xây dựng (KT) + hệ thống (A, B)
│     ├─ tkpccc-bang-doi-chieu/   data/ (A1–A26, B1–B14, KT*, KS, index.json; A/B/KT chép SKILL-PCCC-NEW 24743f9),
│     │                           scripts/bang_doi_chieu.py (chon, khung, dong, tim, lamviec, job, gop, kiem, xlsx, md), test/
│     ├─ tkpccc-ghi-chu-ban-ve/   scripts/ghi_chu_pdf.py (ve, luoi, tim: annotation khung/callout/leader/nét/tường/cửa), test/
│     ├─ tkpccc-kiem-luat-pdf/ · tkpccc-thuy-luc/ · tkpccc-bao-gia/ · tkpccc-huong-dan/   thẻ module (code ở 04_BACKEND/ 06_THAM_DINH_PDF/ 05_FRONTEND/)
│     ├─ tkpccc-phoi-hop-model/   scripts/goi_gemini.py: gọi Gemini CLI không giao diện bóc PDF, Claude chỉ đọc JSON kết quả
│     ├─ tkpccc-quan-ly-drive/    thẻ module Drive (code ở 10_DRIVE/)
│     └─ tkpccc-tra-quy-chuan/ · tkpccc-xong-dau-muc/   quy trình chung
│
├─ 00_QUAN_LY_DU_AN/       QUẢN LÝ: STRUCTURE.md (file này) · TIEN_DO.md (việc đang làm, việc tiếp theo; cập nhật mỗi mốc, trước khi hết usage)
│                          · LICH_SU.md (nhật ký theo ngày) · ROADMAP.md (các pha) · KE_HOACH_TONG.md (kế hoạch tổng rà soát 15/11)
├─ 01_TRI_THUC/            TRI THỨC: quy chuẩn chép nguyên văn + quy tắc nghiệp vụ (chép từ PCCC_he_thong/knowledge)
│  ├─ dinh-nghia/            định nghĩa QCVN 06
│  ├─ qcvn06-*.md, qcvn10-2025-*.md, tcvn7568-14-2025.md, nghi-dinh-*.md, cv-2360-*.md
│  └─ khao-sat-bao-gia.md, bao-gia-pccc.md, so-bo-thiet-ke-pccc.md, chon-loai-dau-bao.md …
├─ 02_DIEU_KHOAN/          ĐIỀU KHOẢN THEO CHUYÊN ĐỀ (chép từ PCCC_he_thong/skills): MOD_*.md, SKILL_*.md, PHU_LUC_G_QCVN06.md
├─ 03_DU_LIEU/             DỮ LIỆU CẤU HÌNH
│  ├─ vat_tu.json            danh mục vật tư: nguồn DUY NHẤT cho plugin + báo giá (loai: block / tuyen / khac)
│  ├─ khoang_cach_rai.json   trị số rải đầu báo theo mã (TCVN 7568-14 chép nguyên văn): plugin CAD `rai` / `PCCC_RAI` đọc khi không có bước
│  ├─ huong_dan.json         quy trình B0–B7: việc cần làm, tài liệu, lệnh CAD của từng bước
│  ├─ mau_giai_phap/         thư viện mẫu giải pháp thoát nạn (17 mẫu; chép từ SKILL-PCCC-NEW)
│  ├─ mau_bao_cao/           BCKS_mau.xlsx: mẫu báo cáo khảo sát MẶC ĐỊNH 4 mục (BCKS_mau_7muc.xlsx mẫu cũ; mẫu trắng), điền bằng `bang_doi_chieu.py bcks`
│  └─ don_gia.json           đơn giá người dùng đã nhập (tự sinh, không commit)
├─ 04_BACKEND/             PHẦN SAU: server cục bộ + lõi nghiệp vụ (Node, `npm start`, cổng 8787)
│  ├─ shared/                LÕI NGHIỆP VỤ dùng chung, hàm thuần (không đọc/ghi file, không server)
│  │  ├─ src/bao_gia.ts        khối lượng × danh mục × đơn giá -> báo giá theo hệ; docSoVN (đọc số kiểu Việt Nam)
│  │  ├─ src/khao_sat.ts       khảo sát 8 mục: chuẩn hoá, ghép gợi ý Claude, ước khối lượng sơ bộ theo m²
│  │  ├─ src/thuy_luc.ts       thủy lực sprinkler TCVN 7336 PL B (chép nguyên từ SKILL-PCCC-NEW 24743f9; sửa thì báo để đồng bộ)
│  │  └─ test/                 *.test.ts + mau.ts (dữ liệu mẫu dùng chung cho test)
│  ├─ src/server.ts          API + phục vụ 05_FRONTEND/index.html; đọc/ghi 03_DU_LIEU/ và 12_KET_QUA/
│  ├─ src/xuat_xlsx.ts · src/xuat_docx.ts   Excel (đơn giá mẫu, báo giá có công thức) · Word (báo cáo khảo sát)
│  ├─ src/thamdinh.ts        gọi lõi Python 06_THAM_DINH_PDF/ (`python -m pccc_checker run`), "Claude xét" dòng XM
│  ├─ src/thuy_luc.ts        ô sơ bộ thủy lực; số liệu -> ô Excel mẫu + 09_SCRIPTS/xuat_thuy_luc_excel.py
│  ├─ src/thong_ke_du_an.ts  quét tệp, gom mã quy chuẩn, đếm theo module/skill/agent cho tab Dự án
│  ├─ src/engines/claude.ts  gọi `claude -p` bằng đăng nhập Claude Code (KHÔNG API key), chỉ công cụ đọc
│  └─ test/
├─ 05_FRONTEND/            GIAO DIỆN: 1 file index.html (HTML + JS thuần, không build); 6 tab Hướng dẫn · Khảo sát · Khối lượng & Báo giá · Thủy lực · Thẩm định PDF · Dự án
├─ 06_THAM_DINH_PDF/       MÁY KIỂM LUẬT PDF (Python 3 + pymupdf + pyyaml; offline) — nguồn, phần đã cắt: 06_THAM_DINH_PDF/README.md
│  ├─ pccc_checker/          gói Python chép từ repo PCCC (TestPCCC) ab546c7: đọc chữ vector, rule engine, ghi chú PDF; cli.py viết mới
│  ├─ rules/ · config/settings.yaml   qcvn06_2022_sd01_2023.yaml (26 luật thoát nạn) · rule pack + mức ghi chú
│  ├─ scripts/make_sample_pdf.py      PDF mẫu có lỗi cài sẵn
│  └─ test/                  pytest
├─ 07_CAD/                 DỰ ÁN CON AUTOCAD 2021–2024 (C# 5, .NET Framework 4.8): bản đồ đủ ở 07_CAD/README.md, luật riêng 07_CAD/CLAUDE.md
│  ├─ .claude/               skill riêng (tkpccc-ve-cad, tkpccc-cad-console) và agent riêng của CAD
│  ├─ module/                mã chạy: plugin/ · ui/ · mcp/ · chung/ · hub/ · botchat/ · youtube/ · banquyen/ · lisp/
│  ├─ skills/ · data/ · rule/   ĐẦU NGUỒN tri thức vẽ (TU_VUNG.md, nhan_dang/, ve/, mau/) · dữ liệu vẽ · LUAT.md
│  ├─ test/                  chay_thu.ps1 (accoreconsole) · thu_mcp.ps1 · thu_gui.ps1 (AutoCAD thật, không bắt buộc)
│  ├─ build.bat · bin/       build DLL + TKPCCC.Mcp.exe, Hub, KeyGen (bin/ không commit)
│  ├─ TKPCCC*.bundle/        gói AutoCAD tự nạp (DLL do build tạo; PCCC_Blocks.dwg, block_alias.json người dùng sửa)
│  ├─ file_cad_test/         bản vẽ mẫu của khách để học (không commit) · thamkhao_plugincad/ plugin tham khảo (chưa commit)
│  └─ KE_HOACH.md · TIEN_DO.md · LICH_SU.md   kế hoạch, tiến độ, nhật ký riêng của CAD
├─ 08_BANG_TINH/thuy_luc/  EXCEL MẪU THỦY LỰC (chép từ SKILL-PCCC-NEW): MAU_THUY_LUC_7336.xlsx, o_nhap.json (bản đồ ô nhập), vi_du/
├─ 09_SCRIPTS/             xuat_thuy_luc_excel.py (sửa F106 theo B.2.10.12) · kiem_lien_ket.py · chen_hang_cuoi.ps1, hoan_thien_excel.ps1 (UTF-8 có BOM)
├─ 10_DRIVE/               QUẢN LÝ GOOGLE DRIVE / ONEDRIVE: drive.py (quét, lịch sử, tìm, dựng cây hồ sơ, xếp file, hoàn tác, chia sẻ qua rclone),
│                          cay_du_an.json (cây hồ sơ một dự án PCCC), test/; dữ liệu thật ở 12_KET_QUA/drive/
├─ 11_TIEN_ICH/vscode-usage/  extension VS Code đếm usage (tự build bằng vsce)
└─ 12_KET_QUA/             KẾT QUẢ TỪNG CÔNG TRÌNH (tự sinh, không commit): 12_KET_QUA/<dự án>/
                           khao_sat.json, khoi_luong.json (bóc từ CAD), khoi_luong_so_bo.json (ước theo khảo sát),
                           thamdinh/ (ban_ve.pdf, ban_ve_annotated.pdf, findings.jsonl, tom_tat.json, claude_xet.json),
                           thuy_luc/ (mat_bang.json, so_lieu.json, thuy_luc.xlsx),
                           khảo sát – tư vấn / thẩm định theo bảng đối chiếu: ban_ve.pdf, doi_chieu.json (bảng + ghi_chu),
                           doi_chieu.xlsx, ban_ve_ghi_chu.pdf, BCKS_<tên>.xlsx + anh_bcks/, anh/, jobs/ + phan/;
                           drive/ (cấu hình + ảnh chụp + lịch sử Drive của máy này)
```

## Mỗi lớp một việc
| Lớp | Thư mục | Ai sửa | Commit |
|---|---|---|---|
| Quy tắc, tiến độ | `CLAUDE.md`, `00_QUAN_LY_DU_AN/TIEN_DO.md`, `00_QUAN_LY_DU_AN/LICH_SU.md`, `00_QUAN_LY_DU_AN/ROADMAP.md`, `00_QUAN_LY_DU_AN/STRUCTURE.md` | Claude cập nhật mỗi mốc | có |
| Quy trình lặp lại của Claude | `.claude/skills/<tên>/SKILL.md` (+ `scripts/`) | Claude gom việc hay thành skill | có |
| Nghiệp vụ (tính toán) | `04_BACKEND/shared/src` | Claude, kèm test trong `04_BACKEND/shared/test` | có |
| Server, xuất file, gọi Claude | `04_BACKEND/src` | Claude (agent `web-backend`), kèm test trong `04_BACKEND/test` | có |
| Giao diện | `05_FRONTEND/index.html` | Claude (agent `web-frontend`) | có |
| Thẩm định PDF (Python) | `06_THAM_DINH_PDF/` (luật: `06_THAM_DINH_PDF/rules/*.yaml`) | Claude, kèm test trong `06_THAM_DINH_PDF/test` | có |
| AutoCAD | `07_CAD/module/plugin/Lenh.cs` | Claude, kiểm bằng `07_CAD/test/chay_thu.ps1` | có |
| Thư viện block, gán block | `07_CAD/TKPCCC.bundle/Contents/PCCC_Blocks.dwg`, `block_alias.json` | người dùng (BEDIT, `PCCC_GAN`) | có |
| Danh mục, quy trình | `03_DU_LIEU/vat_tu.json`, `03_DU_LIEU/huong_dan.json` | người dùng / Claude | có |
| Tri thức, điều khoản | `01_TRI_THUC/`, `02_DIEU_KHOAN/` | chép nguyên văn bằng script, không viết theo trí nhớ | có |
| Đơn giá, kết quả công trình | `03_DU_LIEU/don_gia.json`, `12_KET_QUA/` | tự sinh khi dùng | **không** |

## Luồng dữ liệu
```
AutoCAD: PCCC_DAT / PCCC_ONG vẽ ─► PCCC_BOC ──POST /api/khoi-luong──► 12_KET_QUA/<dự án>/khoi_luong.json ─┐
Web: tab Khảo sát ─► 12_KET_QUA/<dự án>/khao_sat.json ─► "Ước khối lượng" ─► khoi_luong_so_bo.json ────────┤
                                                                                                     ▼
03_DU_LIEU/vat_tu.json + 03_DU_LIEU/don_gia.json ──► 04_BACKEND/shared/src/bao_gia.ts ──► 04_BACKEND/src/xuat_xlsx.ts ──► báo giá .xlsx
Claude ("Claude gợi ý", "Hỏi") ◄── 04_BACKEND/src/engines/claude.ts (`claude -p`, đọc 01_TRI_THUC/ + 02_DIEU_KHOAN/)
Web: tab Thủy lực ─POST /api/thuy-luc/so-bo|tinh─► 04_BACKEND/shared/src/thuy_luc.ts ─► 12_KET_QUA/<dự án>/thuy_luc/so_lieu.json
   ─GET /api/thuy-luc.xlsx─► 04_BACKEND/src/thuy_luc.ts ─► 09_SCRIPTS/xuat_thuy_luc_excel.py (+ Excel COM) ─► thuy_luc.xlsx
Web: tab Thẩm định PDF ─POST /api/tham-dinh─► 12_KET_QUA/<dự án>/thamdinh/ban_ve.pdf ─► 04_BACKEND/src/thamdinh.ts ─► python 06_THAM_DINH_PDF/
   ─► findings.jsonl (D/KD/XM) + PDF ghi chú ─► bảng kết luận; dòng XM ─"Claude xét"─► claude_xet.json
```

## Thêm mới thì đặt ở đâu
- Module mới (người dùng dặn 2026-11-02): `.claude/skills/tkpccc-<tên>/SKILL.md`; module chỉ Claude dùng thì để luôn `scripts/`, `data/`,
  `test/` trong thư mục skill; thêm 1 dòng vào bảng module ↔ skill trong `CLAUDE.md`. Agent con: `.claude/agents/pccc-<việc>.md`.
- Vật tư / thiết bị mới: 1 dòng trong `03_DU_LIEU/vat_tu.json` → `07_CAD\build.bat` → `PCCC_LIB` (tự thêm block mặc định cho mã mới).
- Phép tính nghiệp vụ mới (ước khối lượng, thủy lực…): file mới trong `04_BACKEND/shared/src/` + test trong `04_BACKEND/shared/test/`.
- API mới: route trong `04_BACKEND/src/server.ts`; xuất định dạng mới: `04_BACKEND/src/xuat_<định dạng>.ts`; engine AI mới: `04_BACKEND/src/engines/`.
- Lệnh AutoCAD mới: `07_CAD/module/plugin/Lenh.cs` (bọc `Chay(...)`), thêm dòng kiểm vào `07_CAD/test/chay_thu.ps1`, khai báo trong `PackageContents.xml`.
- Quy chuẩn mới: nguyên văn vào `01_TRI_THUC/` (văn bản, bảng) hoặc `02_DIEU_KHOAN/` (điều khoản theo chuyên đề); nối vào bước trong `03_DU_LIEU/huong_dan.json`.
- Luật thẩm định PDF mới: thêm vào `06_THAM_DINH_PDF/rules/qcvn06_2022_sd01_2023.yaml` (check có sẵn) hoặc hàm `@register` trong
  `06_THAM_DINH_PDF/pccc_checker/rules/checks.py`; thêm ca kiểm vào `06_THAM_DINH_PDF/test/`.
- Bước thiết kế mới: 1 mục trong `03_DU_LIEU/huong_dan.json`.
- Script tiện ích (py/ps1/mjs): `09_SCRIPTS/`; mẫu Excel: `08_BANG_TINH/`. Sau này nếu cần: `so_do/` (sơ đồ html) — giống dự án mẫu.

## Quy ước đặt tên
- Thư mục, file mã: tiếng Việt không dấu, chữ thường, `snake_case` (`khao_sat.ts`, `xuat_xlsx.ts`); tri thức `.md`: `kebab-case` như mẫu.
- Tài liệu gốc dự án: CHỮ HOA (`00_QUAN_LY_DU_AN/TIEN_DO.md`, `00_QUAN_LY_DU_AN/LICH_SU.md`, `00_QUAN_LY_DU_AN/STRUCTURE.md`).
- Đường dẫn tài liệu trong `03_DU_LIEU/huong_dan.json` ghi từ gốc dự án (`01_TRI_THUC/…`, `02_DIEU_KHOAN/…`).
- File `.bat`, `.ps1`: chỉ chữ ASCII (cmd và PowerShell 5.1 đọc sai UTF-8 không BOM). Ngoại lệ: `.ps1` cần chữ Việt (ghi vào Excel)
  thì lưu UTF-8 **có BOM** (`09_SCRIPTS/chen_hang_cuoi.ps1`, `09_SCRIPTS/hoan_thien_excel.ps1`).
