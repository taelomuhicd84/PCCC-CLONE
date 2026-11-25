---
name: tkpccc-kiem-luat-pdf
description: Module máy kiểm luật PDF (thư mục 06_THAM_DINH_PDF/) — đọc chữ vector bản vẽ, chạy 26 luật thoát nạn QCVN 06 viết bằng YAML, ra kết luận D/KD/XM + PDF ghi chú, tab web "Thẩm định PDF", "Claude xét" dòng XM. Dùng khi sửa hoặc thêm luật, sửa lõi Python 06_THAM_DINH_PDF/, sửa tab Thẩm định PDF, hoặc cần chạy máy sàng lọc trước khi thẩm định theo bảng đối chiếu.
model: opus
effort: high
---
# Module: máy kiểm luật PDF (06_THAM_DINH_PDF/)
Phát triển module này: chỉ đọc file trong bảng dưới. Thẩm định đầy đủ theo bảng đối chiếu là skill `tkpccc-tham-dinh` (dùng module này làm bước sàng lọc).

| File | Việc |
|---|---|
| `06_THAM_DINH_PDF/pccc_checker/` | gói Python chép repo PCCC (TestPCCC) ab546c7: `extractors/` (chữ vector), `rules/` (engine, checks), `output/` (annotate_pdf, report_md), `pipeline/consistency.py`; `cli.py` viết mới |
| `06_THAM_DINH_PDF/rules/qcvn06_2022_sd01_2023.yaml` | 26 luật thoát nạn (verified) |
| `06_THAM_DINH_PDF/config/settings.yaml` | rule pack + mức ghi chú |
| `06_THAM_DINH_PDF/scripts/make_sample_pdf.py` | PDF mẫu có lỗi cài sẵn |
| `06_THAM_DINH_PDF/test/` | pytest |
| `06_THAM_DINH_PDF/README.md` | nguồn, phần đã cắt/sửa |
| `04_BACKEND/src/thamdinh.ts`, `04_BACKEND/test/thamdinh.test.ts` | gọi Python, đọc `findings.jsonl`, câu hỏi "Claude xét" |
| `04_BACKEND/src/server.ts` | route `/api/tham-dinh` (thân PDF ≤ 200 MB), `/api/tham-dinh/pdf`, `/api/tham-dinh/xet` |
| `05_FRONTEND/index.html` | tab "Thẩm định PDF" (`data-tab="td"`) |

## Chạy
`python -m pccc_checker run <pdf> --out <thư mục> [--pages auto|all|1-3,5] [--set height_pccc_m=24 --set function_group=F1.3]`
với `cwd=06_THAM_DINH_PDF/` (hoặc `PYTHONPATH=thamdinh`) và `PYTHONIOENCODING=utf-8` → `findings.jsonl` (D/KD/XM), `<tên>_annotated.pdf`, `report.md`.

## Quy tắc
- Sửa luật ở `06_THAM_DINH_PDF/rules/*.yaml` (check có sẵn) hoặc hàm `@register` trong `06_THAM_DINH_PDF/pccc_checker/rules/checks.py`; thêm ca kiểm vào
  `06_THAM_DINH_PDF/test/`; ghi phần đã cắt/sửa vào `06_THAM_DINH_PDF/README.md`.
- Dòng "không đọc được số liệu" là XM, không phải KD; thiếu nhóm F, chiều cao, bậc chịu lửa thì luật phụ thuộc ra XM.
- Ngưỡng có `cases` phụ thuộc số liệu chưa biết (số người, diện tích): chỉ Đạt khi đạt MỌI ngưỡng có thể, chỉ KD khi vi phạm mọi ngưỡng,
  còn lại XM (`engine.possible_values`). Chỉ finding `error` thành KD; `warning`/`info` thành XM. Nhóm F suy từ chữ chỉ là gợi ý.

## Giới hạn đã biết
Bản scan (không có lớp chữ) không đọc được; mới có 26 luật thoát nạn; thang chưa xét hình dạng (rẽ quạt, bậc chéo) nên đủ 2 lối nhờ thang thì luật số lối ra ra XM;
mã dòng PC-001… đánh lại mỗi lần chạy nên chạy lại thì xoá "Claude xét" cũ.

## Kiểm
`python -m pytest 06_THAM_DINH_PDF/test -q`, `npx vitest run 04_BACKEND/test/thamdinh.test.ts`.
