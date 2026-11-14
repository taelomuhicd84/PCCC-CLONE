---
name: tkpccc-phoi-hop-model
description: Phối hợp Gemini + Claude để tiết kiệm hạn mức Claude — giao việc ĐỌC NHIỀU (PDF dài, trang scan, ảnh mặt bằng, mục lục hồ sơ) cho Gemini CLI chạy không giao diện, Claude chỉ đọc file JSON kết quả nhỏ rồi lập kế hoạch, kết luận, kiểm. Dùng khi phải đọc PDF trên khoảng 10 trang, trang scan không có chữ vector, cần bóc trục / kích thước / tên phòng / thông số công trình từ bản vẽ, hoặc người dùng nói "dùng Gemini", "tiết kiệm token", "phối hợp model".
---

# Phối hợp Gemini + Claude (người dùng duyệt 14/11)

Nguyên tắc: model đắt chỉ đọc bản đã rút gọn. Thứ tự ưu tiên khi cần bóc số liệu:
1. **Script** (0 token, đúng nhất): chữ vector PDF (`thamdinh/`, `ghi_chu_pdf.py tim`), `pdf_ti_le.py`, quét tên bản vẽ trước (memory "PDF lớn").
2. **Gemini** (`scripts/goi_gemini.py`): phần script không làm được — trang scan, đọc hình (tường, cửa, phòng), tóm hồ sơ dài.
3. **Claude** đọc ảnh trang: chỉ đúng trang đang cần xét, sau khi 1–2 đã thu hẹp.

| Việc | Ai làm |
|---|---|
| Lập kế hoạch, chia job, điều phối | Claude Sonnet (việc khó: Opus) |
| Bóc PDF dài: mục lục, thông số công trình, chữ trên trang scan | script → Gemini |
| CAD bước 1–3 (trục, tường, cửa, tên phòng) từ PDF | `pdf_ti_le.py` + Gemini bóc toạ độ thô → Claude lập spec |
| Chạy spec, đếm, soát số (`cad-ve`, `cad-kiem`) | Claude Haiku / Sonnet effort thấp |
| Viết mã plugin | Claude Opus (`cad-lap-trinh`) |
| Kết luận bảng đối chiếu, thẩm định, review mã | Claude Opus, chỉ đọc bản tóm có trích nguyên văn |

## Gọi Gemini
```
python .claude/skills/tkpccc-phoi-hop-model/scripts/goi_gemini.py --kiem
python .claude/skills/tkpccc-phoi-hop-model/scripts/goi_gemini.py --file <pdf> --trang 3-5 --json --out ket_qua/<dự án>/gemini_<việc>.json --yeu-cau "<trường cần lấy>"
```
- Script chép file (hoặc chỉ các trang `--trang`) vào thư mục tạm, chạy `gemini -p ... --approval-mode plan` (chỉ đọc) ở đó: Gemini không thấy repo.
- Kết quả ghi vào `ket_qua/<dự án>/` (không commit). Claude đọc file đó, KHÔNG dán PDF vào hội thoại.
- `--model` để chọn model Flash cho việc bóc đơn giản (xem tên bằng `gemini` → `/model`); bỏ trống thì CLI tự chọn.

## Mẫu yêu cầu bóc (luôn đòi trích nguồn để kiểm)
`--yeu-cau` liệt kê trường cần lấy, mỗi giá trị kèm `trang` và `trich` (chữ nguyên văn trên bản vẽ), không đọc được thì `null`. Ví dụ CAD bước 1:
"Trả JSON {truc_x:[{ten, trang, trich}], nhip_x_mm:[...], truc_y:[...], nhip_y_mm:[...], ti_le_ghi:'1:100'|null}".

## Luật
- Số Gemini bóc ra là CHƯA KIỂM: trước khi vào bảng đối chiếu, spec CAD hay kết luận phải đối chiếu bằng script (chữ vector, tỉ lệ) hoặc Claude xem ảnh đúng trang đó. Trị số quy chuẩn vẫn lấy nguyên văn từ `knowledge/`, `skills/`, không lấy từ Gemini.
- Hồ sơ khách: gói miễn phí của Google có thể dùng nội dung để cải thiện sản phẩm. Chỉ gửi trang cần thiết (`--trang`); người dùng chịu trách nhiệm về điều khoản tài khoản.
- Gemini lỗi đăng nhập / hết hạn mức: quay về bước 3 (Claude đọc đúng trang), không chờ.
- 14/11: đăng nhập Google cá nhân bị Google từ chối (`IneligibleTierError`: Code Assist cho cá nhân không còn hỗ trợ Gemini CLI, chuyển sang Antigravity).
  Gemini CLI chỉ chạy được bằng `GEMINI_API_KEY` (Google AI Studio) do người dùng tự tạo; chưa có key thì bỏ bước 2.
- Đo hiệu quả: ghi token Gemini (script in ra) và token Claude của lượt đó vào `LICH_SU.md` khi thử trên hồ sơ thật.
