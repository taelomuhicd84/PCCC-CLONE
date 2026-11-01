# Lịch sử TKPCCC

## 2026-10-31
- Lập kế hoạch 4 pha (plan `C:\Users\HP\.claude\plans\t-i-mu-n-thi-t-k-eager-giraffe.md`). Người dùng chốt: AutoCAD .NET, web nội bộ/LAN,
  MVP bóc KL + báo giá, block do tool tạo (attribute MA_VT, sửa hình được), đơn giá: tool tạo mẫu, gọi Claude không dùng API key.
- Pha 1: `data/vat_tu.json` (51 mã), plugin `PCCC_LIB/DAT/ONG/BOC`, runner (`/api/khoi-luong`, đơn giá mẫu Excel, báo giá Excel có công thức),
  web tab Khối lượng & Báo giá, `/api/claude` qua `claude -p`.
- Pha 2: tab Hướng dẫn B0–B7 (theo quy trình người dùng: B0, B1, B4, B5a, B5b, B6a, B6b, B6c, B7) đọc nguyên văn `knowledge/*.md`,
  hỏi Claude theo tài liệu của bước; tab Khảo sát 8 mục (Claude gợi ý, xuất Word); báo giá sơ bộ theo m².
- Tham khảo LHB Block Scheduler (chỉ tính năng, DLL có khoá bản quyền nên không dịch ngược): thêm không đếm trùng, block lồng/MINSERT,
  bỏ XREF, tách theo tầng/khu (`PCCC_BOC` → sheet `TheoKhuVuc`), `PCCC_GAN` (gán block có sẵn → mã), `PCCC_BANG`, `PCCC_VUNGBV`.
- Rà lại với effort cao, sửa: runner sập khi lỗi sau khi gửi header, EPIPE stdin của claude; `eWrongDatabase` khi tạo thư viện;
  ký hiệu lệch tâm (DBText → MText); bảng thống kê bị đếm như thiết bị (Table kế thừa BlockReference); tỉ lệ block theo INSUNITS;
  ô số của trình duyệt hiểu "1.200" là 1,2. Thêm `cad/test/chay_thu.ps1` (kiểm plugin trong accoreconsole), `docSoVN`, chuẩn hoá khảo sát.
- Tạo lại `PCCC_Blocks.dwg` (bản do plugin cũ tạo bị lệch tâm, cất ở `PCCC_Blocks_cu_20261031_151400.dwg`).
- git init (identity trantrongtien198); đẩy lên https://github.com/taelomuhicd84/PCCC-CLONE (nhánh `main`).
- Sắp xếp lại thư mục theo khung dự án mẫu PCCC_he_thong: `shared/src` (bao_gia, khao_sat) + `shared/test`, `runner/src`
  (server, xuat_xlsx, xuat_docx, engines/claude) + `runner/test`, `cad/src/Lenh.cs` + `cad/test`, `skills/` (MOD_*, SKILL_*, PHU_LUC_G
  chuyển từ knowledge/), `du_an/` → `ket_qua/`; thêm `STRUCTURE.md`, `ROADMAP.md`, `chay.bat`, `.claude/launch.json`.
- Gom quy trình hay thành 3 skill dự án: `tkpccc-cad-console` (+ `scripts/in_png.ps1` in DWG ra PNG, đã chạy thử),
  `tkpccc-xong-dau-muc`, `tkpccc-tra-quy-chuan`. Người dùng dặn: xong mỗi đầu mục thì lưu tiến độ, push, xoá ngữ cảnh rồi làm tiếp.
- Pha 3 (bắt đầu): khảo sát lõi Python `D:\1_Claude\TestPCCC\PCCC` (chỉ đọc). Chốt đường chạy offline không API key: extract vector →
  rule engine YAML (26 luật QCVN 06:2022 + SĐ1:2023) → D/KD/XM (`consistency.py`) → PDF ghi chú; bỏ `info`/Gemini/DXF/Word.
  Danh sách file cần chép + kế hoạch 6 bước ghi ở cuối `TIEN_DO.md`. Dừng trước khi viết code vì usage 5 giờ chạm 85%.
- Pha 3: chạy thử CLI gốc TestPCCC (offline, vector) trên 1 bản vẽ PDF thật → ra report + PDF ghi chú (để trong `ket_qua/`).
  Xác nhận đường chạy dùng được; chưa chép code vào TKPCCC, web chưa có tab Thẩm định.
- Pha 3: chạy thử lõi TestPCCC (bản sao thư mục tạm) trên PDF mẫu `make_sample_pdf.py` (4 KD + 1 XM đúng lỗi cài sẵn, PDF ghi chú đúng vị trí)
  và hồ sơ KS Hoàng Hùng 18 trang (0 Đạt, 3 KD "không đọc được số liệu", 3 XM). Đề xuất hạ KD "không đọc được" xuống XM khi chép vào `thamdinh/`.

## 2026-11-01
- Clone dự án về máy mới `F:\1_ToolMMo\2_VibeCode\PCCCVIP`. Không có `D:\1_Claude\TestPCCC` → lấy lõi từ repo `taelomuhicd84/PCCC` (đúng commit ab546c7).
- Người dùng đưa thêm repo tham khảo `SKILL-PCCC-NEW`, `PcccVipPro` rồi mở công khai cả ba (đã báo: SKILL-PCCC-NEW có hồ sơ khách trong `ket_qua/`).
- Pha 3 xong: `thamdinh/` (lõi Python, CLI `run`, dòng Đạt, "không đọc được" → XM, thiếu thông tin công trình → XM theo luật, sửa lỗi chiều cao
  ≥ 20 m bị coi là ước lượng, pytest), `runner/src/thamdinh.ts` + 4 route, web tab "Thẩm định PDF", "Claude xét" dòng XM (chạy thật 27 giây).
  Kiểm: npm test 39, pytest 2, trình duyệt (cả màn hình điện thoại). Chưa thử hồ sơ thật trên máy này; chưa chạy test CAD (máy không có AutoCAD).
- Người dùng dặn lại (ghi vào `CLAUDE.md` + bộ nhớ): gần hết usage thì dừng ngay, ghi `TIEN_DO.md` + `LICH_SU.md`, push — luôn tự làm.
