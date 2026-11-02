---
name: tkpccc-kiem-luat-pdf
description: Module máy kiểm luật PDF (thư mục thamdinh/) — đọc chữ vector bản vẽ, chạy 26 luật thoát nạn QCVN 06 viết bằng YAML, ra kết luận D/KD/XM + PDF ghi chú, tab web "Thẩm định PDF", "Claude xét" dòng XM. Dùng khi sửa hoặc thêm luật, sửa lõi Python thamdinh/, sửa tab Thẩm định PDF, hoặc cần chạy máy sàng lọc trước khi thẩm định theo bảng đối chiếu.
---
# Module: máy kiểm luật PDF (thamdinh/)
Phát triển module này: chỉ đọc file trong bảng dưới. Thẩm định đầy đủ theo bảng đối chiếu là skill `tkpccc-tham-dinh` (dùng module này làm bước sàng lọc).

| File | Việc |
|---|---|
| `thamdinh/pccc_checker/` | gói Python chép repo PCCC (TestPCCC) ab546c7: `extractors/` (chữ vector), `rules/` (engine, checks), `output/` (annotate_pdf, report_md), `pipeline/consistency.py`; `cli.py` viết mới |
| `thamdinh/rules/qcvn06_2022_sd01_2023.yaml` | 26 luật thoát nạn (verified) |
| `thamdinh/config/settings.yaml` | rule pack + mức ghi chú |
| `thamdinh/scripts/make_sample_pdf.py` | PDF mẫu có lỗi cài sẵn |
| `thamdinh/test/` | pytest |
| `thamdinh/README.md` | nguồn, phần đã cắt/sửa |
| `runner/src/thamdinh.ts`, `runner/test/thamdinh.test.ts` | gọi Python, đọc `findings.jsonl`, câu hỏi "Claude xét" |
| `runner/src/server.ts` | route `/api/tham-dinh` (thân PDF ≤ 200 MB), `/api/tham-dinh/pdf`, `/api/tham-dinh/xet` |
| `web/index.html` | tab "Thẩm định PDF" (`data-tab="td"`) |

## Chạy
`python -m pccc_checker run <pdf> --out <thư mục> [--pages auto|all|1-3,5] [--set height_pccc_m=24 --set function_group=F1.3]`
với `cwd=thamdinh/` (hoặc `PYTHONPATH=thamdinh`) và `PYTHONIOENCODING=utf-8` → `findings.jsonl` (D/KD/XM), `<tên>_annotated.pdf`, `report.md`.

## Quy tắc
- Sửa luật ở `thamdinh/rules/*.yaml` (check có sẵn) hoặc hàm `@register` trong `thamdinh/pccc_checker/rules/checks.py`; thêm ca kiểm vào
  `thamdinh/test/`; ghi phần đã cắt/sửa vào `thamdinh/README.md`.
- Dòng "không đọc được số liệu" là XM, không phải KD; thiếu nhóm F, chiều cao, bậc chịu lửa thì luật phụ thuộc ra XM.

## Giới hạn đã biết
Bản scan (không có lớp chữ) không đọc được; mới có 26 luật thoát nạn; thang bộ được tính 1 lối ra khi xét phân tán 3.2.8;
mã dòng PC-001… đánh lại mỗi lần chạy nên chạy lại thì xoá "Claude xét" cũ.

## Kiểm
`python -m pytest thamdinh/test -q`, `npx vitest run runner/test/thamdinh.test.ts`.
