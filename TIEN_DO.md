# Tiến độ TKPCCC

File này cập nhật sau mỗi mốc việc và trước khi hết usage. Phiên mới đọc mục "ĐANG LÀM / VIỆC TIẾP THEO" trước.

## ĐANG LÀM / VIỆC TIẾP THEO (cập nhật 2026-10-31)
**Xong Pha 1 (bóc KL + báo giá) và Pha 2 (hướng dẫn B0–B7, khảo sát, báo giá sơ bộ).** Đã kiểm: 35 test (`shared/test`, `runner/test`),
6 test plugin trong accoreconsole, chạy thử web trên trình duyệt (tạo dự án, khảo sát, ước KL, báo giá, Word, màn hình điện thoại).
**Đã sắp xếp lại thư mục theo khung PCCC_he_thong** (`shared/`, `runner/src`+`test`, `skills/`, `ket_qua/`…), mô tả ở `STRUCTURE.md`.
**Code ở GitHub:** https://github.com/taelomuhicd84/PCCC-CLONE (công khai).
**Skill dự án** (`.claude/skills/`): `tkpccc-cad-console`, `tkpccc-xong-dau-muc`, `tkpccc-tra-quy-chuan`. Xong mỗi đầu mục chạy `tkpccc-xong-dau-muc`.
**Pha 3 (thẩm định PDF): ĐÃ KHẢO SÁT lõi Python, CHƯA viết code** — dừng vì usage 5 giờ chạm 85% (15:36 ngày 2026-10-31).
Làm tiếp theo mục "Kế hoạch Pha 3" ở cuối file này, bắt đầu từ bước 1.
Đã chạy thử CLI gốc TestPCCC (đường offline `--extractor vector`) trên 1 bản vẽ PDF thật (16:06): chạy được, ra
`findings.jsonl`, `report.md`, PDF ghi chú (0 lỗi, 1 cảnh báo, 5 thông tin; thiếu `height_pccc_m`, nhóm F tự suy ra). Kết quả để trong
`ket_qua/` (không commit). Web CHƯA có tab "Thẩm định PDF" (runner cổng 8787 chỉ có 3 tab: Hướng dẫn, Khảo sát, Khối lượng & Báo giá).
Chạy thử thêm trên hồ sơ KS Hoàng Hùng (18 trang, `ket_qua/thu_ks_hoang_hung/`, không commit): Đạt 0, KD 3, XM 3. Cả 3 KD là
"không đọc được số liệu" (STAIR-DATA BTB1/BTB2 tr 4, EXIT-DOOR-DATA D01 tr 6), chưa phải vi phạm thật. Thiếu `height_pccc_m`, bậc chịu lửa,
tỉ lệ bản vẽ; không nhận diện được lối ra tr 17. **Khi chép lõi vào `thamdinh/` (bước 1): hạ các dòng "không đọc được" từ KD xuống XM,
chỉ giữ KD khi đo được số liệu và vi phạm** (đề xuất, chờ người dùng đồng ý). Muốn đọc được số liệu cửa/thang mà không dùng API: thêm đọc bảng cửa/thang.
Lõi Python chạy được ở thư mục tạm `scratchpad/tdcore` (bản sao TestPCCC, không `.env`), lệnh: `extract --extractor vector --pages auto` → `check` → `annotate` → `findings`.

Việc người dùng cần làm:
- 👤 Tắt hẳn AutoCAD rồi mở lại để nạp plugin mới (DLL đã build; bản cũ đổi tên `TKPCCC.Cad.dll.cu*`).
- 👤 Đăng nhập lại Claude Code trên máy (phiên OAuth hết hạn): mở terminal gõ `claude` rồi `/login`. Chưa đăng nhập thì nút "Claude gợi ý"/"Hỏi" báo lỗi.
- 👤 Clone `taelomuhicd84/PcccVipPro` (private) về máy và báo đường dẫn để tham khảo.
- 👤 Gửi file đơn giá mẫu của bạn (khi có) để tool đọc đúng cột.

Việc tiếp theo (đề xuất):
1. Pha 3: đọc PDF + thẩm định — làm theo "Kế hoạch Pha 3" cuối file (bước 1 → 6).
2. Pha 4: `PCCC_RAI` rải đầu phun/đầu báo theo lưới trong polyline phòng; thủy lực TCVN 7336 (port `shared/src/thuy_luc.ts` của PCCC_he_thong).
3. Hỏi người dùng: kích thước/ký hiệu block chuẩn họ muốn (hiện là vòng tròn R200 + chữ), quy tắc ước khối lượng theo kinh nghiệm của họ.

Vấn đề còn mở / giới hạn đã biết:
- Plugin chỉ cho AutoCAD 2021–2024 (.NET Framework 4.8); 2025+ cần build .NET 8.
- Ray-casting khu vực bỏ qua cung (bulge) của polyline; ống trong block đặt ở layer 0 không đếm; VUNGBV chỉ xét thiết bị ở model space cấp 1.
- Báo giá sơ bộ dùng quy tắc mặc định của `knowledge/bao-gia-pccc.md` (DN ống chưa đúng thiết kế, ghi "cỡ ống chờ thiết kế").
- `PCCC_ONG` dùng lệnh PLINE thường (không kiểm được trong accoreconsole, chỉ kiểm trong AutoCAD thật).

## Kế hoạch Pha 3 — thẩm định PDF (khảo sát 2026-10-31)

### Kết quả khảo sát `D:\1_Claude\TestPCCC\PCCC` (commit `ab546c7`; CHỈ ĐỌC, không sửa repo đó, không đọc `.env`/`api.txt`)
- Máy có Python 3.12.10 (`python` trên PATH) + `pymupdf` (MuPDF 1.28.5) + `pyyaml` 6.0.3. Thiếu `ezdxf`, `python-docx`
  (chỉ cần cho DXF / Word — Pha 3 bỏ hai đầu ra này, nên KHÔNG phải cài gì).
- Đường chạy offline tối thiểu, không Gemini, không API key (gói `src/pccc_checker`):
  `extract --extractor vector` (chữ vector của PDF → `extraction.json`) → `check` (rule engine YAML, 0 token → `findings.json`,
  `report.md`) → `pipeline/consistency.py` `from_engine` + `validate` (→ dòng D/KD/XM, KD thiếu trang/bbox/bằng chứng tự hạ XM)
  → `annotate` (`<tên>_annotated.pdf`: khung đỏ/cam/xanh + comment ghi điều khoản).
  Bỏ bước `info` (`project_info.py` 1570 dòng, cần Gemini): `check` chạy được không có `project_info.json`, dùng thông tin
  công trình trong `extraction.json` + `--set height_pccc_m=24 --set function_group=F1.3 ...`.
- File cần chép (đường dẫn trong `src/pccc_checker/`):
  `config.py` (bỏ `_load_dotenv`, `gemini_api_key`), `models.py`, `pdf/render.py`, `pdf/classify.py`, `pdf/vn_encoding.py`,
  `pdf/arial_gid.json`, `extractors/base.py`, `extractors/vector_extractor.py`, `extractors/postprocess.py`,
  `extractors/__init__.py` (cắt còn `make_extractor("vector")`, bỏ `HybridExtractor` + import gemini), `rules/__init__.py`,
  `rules/engine.py`, `rules/checks.py`, `output/annotate_pdf.py`, `output/report_md.py`, `pipeline/consistency.py`;
  ngoài gói: `rules/qcvn06_2022_sd01_2023.yaml` (26 luật, `verified: true`, điều khoản QCVN 06:2022 + SĐ1:2023),
  `scripts/make_sample_pdf.py` (chỉ cần pymupdf, tạo PDF mẫu có lỗi cài sẵn).
- Import muộn tới `project_info` (KHÔNG chép file đó): `pdf/classify.py` `_fire_note` (chỉ hàm lọc trang cho Gemini dùng, dòng 152)
  và `output/report_md.py` `project_info_md` (chỉ khi có info) → xoá hai nhánh này khi chép.
- Không chép: `extractors/gemini*`, `info_gemini.py`, `project_info.py`, `pipeline/` khác (goi, va_cham, sheets, chuyen_de…),
  `output/export_dxf.py`, Word, `pccc_web/`, `rules/checklists/` (bảng đối chiếu B4–B50 — để Pha 3b nếu người dùng cần).
- Lưu ý "D": rule engine chỉ sinh vi phạm + `cannot_check`, không có dòng Đạt. Luật không khớp `when` hoặc thiếu dữ liệu thì im lặng
  → KHÔNG được coi là D. Muốn hiện D: sửa nhẹ `run_rules` ghi luật nào có phần tử mục tiêu (`targets`) mà không vi phạm → D.
- CLI gốc để đối chiếu: `set PYTHONPATH=src` rồi
  `python -m pccc_checker run <pdf> --extractor vector --pages all --no-dxf --gemini-info never --out <thư mục>`.

### Các bước (mỗi bước kiểm được rồi mới sang bước sau)
1. **`thamdinh/`** (thư mục mới, ghi vào `STRUCTURE.md`): `thamdinh/pccc_checker/` (giữ tên gói để import tương đối không đổi),
   `thamdinh/rules/qcvn06_2022_sd01_2023.yaml`, `thamdinh/config/settings.yaml` (chỉ `rules.packs` + `annotate`),
   `thamdinh/scripts/make_sample_pdf.py`, `thamdinh/test/` (pytest: tạo PDF mẫu → chạy → có KD với trang + điều khoản),
   `thamdinh/README.md` (nguồn: TestPCCC commit `ab546c7`, danh sách file đã chép/cắt). `cli.py` mới rút gọn:
   `python -m pccc_checker run <pdf> --out <dir> [--pages all] [--set k=v]` → extract vector → check → findings.jsonl → annotate,
   in 1 dòng JSON tóm tắt ra stdout. Chạy với `cwd=thamdinh/` (hoặc `PYTHONPATH=thamdinh`), `PYTHONIOENCODING=utf-8`.
2. **Runner**: `runner/src/thamdinh.ts` spawn `python -m pccc_checker run …` (có timeout, bắt lỗi "không có python");
   route trong `runner/src/server.ts`: `POST /api/thamdinh/<dự án>` (thân = PDF nhị phân, giới hạn dung lượng) → lưu
   `ket_qua/<dự án>/thamdinh/ban_ve.pdf` → chạy → trả danh sách dòng; `GET /api/thamdinh/<dự án>` (kết quả lần trước),
   `GET /api/thamdinh/<dự án>/pdf` (PDF đã ghi chú). Tên dự án qua `tenThuMuc` như các route cũ. Test `runner/test/thamdinh.test.ts`
   (đọc findings.jsonl → D/KD/XM; phần gọi python bỏ qua nếu máy không có python).
3. **Web** `web/index.html`: tab "Thẩm định PDF" — chọn PDF, ô thông tin công trình tuỳ chọn (nhóm F, chiều cao PCCC → `--set`),
   nút Chạy, bảng kết luận (KD đỏ, XM cam nghiêng, D xanh; cột trang, điều khoản, nội dung, số liệu yêu cầu/thực tế),
   link tải PDF ghi chú.
4. **"Claude xét"** cho từng dòng XM: `POST /api/thamdinh/<dự án>/xet` {id} → `runner/src/engines/claude.ts` (`claude -p`, không API key)
   với prompt: dòng XM + dặn CHỈ trích điều khoản có trong `knowledge/`, `skills/` (cách chọn file: skill `tkpccc-tra-quy-chuan`),
   trả lời đề xuất D / KD / vẫn XM + nguyên văn căn cứ + file nguồn; lưu `ket_qua/<dự án>/thamdinh/claude_xet.json`, hiện dưới dòng.
5. **Kiểm**: `python -m pytest thamdinh/test -q`; `npm test`; bật runner, chạy thử PDF mẫu từ `make_sample_pdf.py` trên trình duyệt
   (tải lên → bảng kết luận → tải PDF ghi chú → "Claude xét" khi đã `/login`).
6. Hỏi người dùng PDF hồ sơ thật nào để thử (để trong `ket_qua/`, không commit). Khép đầu mục bằng skill `tkpccc-xong-dau-muc`.
