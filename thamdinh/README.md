# thamdinh/ — lõi Python thẩm định bản vẽ PDF (offline, không API key)

Đọc chữ vector trong PDF → kiểm 26 luật QCVN 06:2022 + Sửa đổi 1:2023 (`rules/qcvn06_2022_sd01_2023.yaml`, `verified: true`)
→ `findings.jsonl` (D Đạt / KD Không đạt / XM Cần xác minh) + PDF ghi chú (khung + comment ghi điều khoản). Runner gọi qua
`backend/src/thamdinh.ts`; web: tab **Thẩm định PDF**.

## Chạy
Cần Python 3.10+ với `pymupdf`, `pyyaml` (`python -m pip install pymupdf pyyaml`). Từ thư mục `thamdinh/`:
```
python -m pccc_checker run <file.pdf> --out <thư mục> [--pages auto|all|1-3,5] [--set height_pccc_m=24 --set function_group=F1.3]
python -m pytest test -q
python scripts/make_sample_pdf.py <ra.pdf>      :: PDF mẫu có lỗi cài sẵn
```
Ra trong `--out`: `extraction.json/.md` (số liệu máy đọc), `findings.json` + `report.md` (rule engine), `<tên>_annotated.pdf`,
`findings.jsonl` (bảng kết luận). Dòng cuối stdout là JSON tóm tắt.

## Nguồn
Chép từ https://github.com/taelomuhicd84/PCCC (TestPCCC), commit `ab546c7`, thư mục `src/pccc_checker` (giữ tên gói để import tương đối không đổi):
`models.py`, `pdf/` (render, classify, vn_encoding, arial_gid.json), `extractors/` (base, vector_extractor, postprocess),
`rules/` (engine, checks), `output/` (annotate_pdf, report_md), `pipeline/consistency.py`; ngoài gói: `rules/qcvn06_2022_sd01_2023.yaml`,
`scripts/make_sample_pdf.py`.

Đã cắt / sửa so với bản gốc:
- Bỏ Gemini, `project_info.py` (bước `info`), DXF, Word, `pccc_web/`, bảng đối chiếu `rules/checklists/`.
- `config.py`: không đọc `.env`, không khoá API; `PROJECT_ROOT` = `thamdinh/`. `extractors/__init__.py`: chỉ `vector`.
- `pdf/classify.py`: bỏ `_fire_note`, `smart_gemini_pages`, `info_pages` (chỉ phục vụ Gemini). `output/report_md.py`: bỏ `project_info_md`.
- `cli.py` viết mới, chỉ lệnh `run`. Thêm so với gốc:
  - dòng **Đạt**: luật có phần tử đo được số liệu mà không vi phạm (luật im lặng vì không khớp điều kiện/thiếu số liệu KHÔNG tính Đạt);
  - luật "không đọc được số liệu" (`missing_data`, `building_missing`) xếp **XM**, không phải KD (luật người dùng: thiếu dữ liệu thì cần xác nhận);
  - thiếu nhóm F / chiều cao PCCC / bậc chịu lửa… thì luật phụ thuộc trả XM "Không kiểm tra được: chưa có …" thay vì im lặng.
- `extractors/postprocess.py`: sửa lỗi chiều cao PCCC ≥ 20 m có ghi trên bản vẽ vẫn bị gắn nhãn "ước lượng" (`_numbers` coi số ≥ 20 là mm).
- `rules/engine.py`: câu nhắc "Xác nhận trong project.yaml" → "Nhập thông tin công trình rồi chạy lại".
- Sửa sau review 15/11 (ca kiểm `test/test_sua_1115.py`):
  - ngưỡng theo `cases` xét 3 trạng thái (`match3`, `possible_values`): thiếu số người / diện tích… thì đạt ngưỡng nhẹ mà chưa
    đạt ngưỡng chặt là **XM**, không còn Đạt theo ngưỡng thấp nhất; vi phạm mọi ngưỡng mới KD. Cờ phần tử `single_workplace` không ghi = không;
  - chỉ finding `error` thành **KD**; `warning` / `info` (nhắc việc, còn ngoại lệ máy không xét) thành XM;
  - luật số lối ra viết lại theo 3.2.6.1 (ai phải có 2 lối) rồi 3.2.6.2 (ngoại lệ, trừ bậc chịu lửa V), khoá YAML `need_two`,
    `need_two_basement`, `need_two_maybe`, `no_single_exit`; ngoại lệ khớp vẫn XM (máy không kiểm xe, kho, lối khẩn cấp);
  - đếm lối ra: cửa cách nhãn thang ≤ 3 m tính chung một lối với thang; thang chưa xét hình dạng (`shape_ok`) và lối ra nhận
    từ chữ đứng riêng (`text_only`) là lối chưa chắc chắn → đủ 2 lối chỉ nhờ chúng thì XM; chữ "LỐI THOÁT NẠN" ngoài khung mặt bằng bỏ qua;
  - `annotate_pdf.py`: trang `/Rotate` 90/270 không còn sập (cắt khung theo trang chưa xoay);
  - `classify.py`: ô bộ môn "KẾT CẤU" trong khung tên không làm trang thành kết cấu; mặt bằng tầng có bảng thống kê cửa vẫn là `plan`;
  - nhóm F suy từ chữ (`infer_function_group`) chỉ ghi gợi ý vào `assumptions`, không dùng để kết luận.

Giới hạn đã biết: bản scan (không có lớp chữ) không đọc được; thang bộ được tính là một lối ra khi xét phân tán 3.2.8.
