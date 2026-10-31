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
