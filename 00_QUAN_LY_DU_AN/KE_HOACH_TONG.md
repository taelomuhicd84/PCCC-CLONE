# Kế hoạch tổng sau review toàn dự án (15/11/2026)

Nguồn: 6 bản review ở `12_KET_QUA/review_1115/R*.md` (không commit), R1 và R5 đã được Fable 5.2 soát vòng 2. Trạng thái việc: `00_QUAN_LY_DU_AN/TIEN_DO.md`.

## 1. Kết quả review

| Mảng | Nghiêm trọng | Cao | Vừa | Thấp | Rủi ro lớn nhất |
|---|---|---|---|---|---|
| R1 Đầu vào PDF, tri thức nhận biết (đã soát vòng 2) | 4 | 8 | 15 | 1 | Chiều cao PCCC định nghĩa sai ở 4 nơi; trích sai QCVN 10 Bảng A.1 chú thích (2); bài học mới ghi đè mà không xoá câu cũ |
| R2 Liên kết dữ liệu | 1 | 2 | 6 | 6 | `bang_doi_chieu.py job` trích sai văn bản cho 14 dòng; bảng B4 / B5 còn căn cứ cũ |
| R3a CAD lõi vẽ | 2 | 6 | 7 | 7 | `PCCC_KT` tính phủ xuyên tường; khoang dầm gom khoang nhỏ của mọi phòng; bản vẽ theo mét không khai đơn vị |
| R3b CAD giao diện, nạp, bản quyền | 0 | 6 | 8 | 2 | Ảnh block vẽ theo layer `0` nên sai màu; repo công khai nên ai cũng build được bản không chặn |
| R4 Web | 1 | 3 | 4 | 4 | Đổi dự án giữa chừng ghi đè dự án khác; `docSoVN("12,375")` = 12375; runner không kiểm Host / Origin |
| R5 Máy kiểm luật PDF (đã soát vòng 2) | 2 | 7 | 11 | 4 | Thiếu số liệu vẫn ra Đạt / Không đạt; trang xoay làm sập cả lượt chạy; thang và cửa vào chính thang đó bị đếm thành 2 lối ra |

Điều tốt đã xác nhận: mã vật tư khớp ở mọi nơi dùng; mã bảng A / B / KT / KS khớp; plugin đọc trị số rải từ `03_DU_LIEU/khoang_cach_rai.json`, không chép;
runner chỉ nghe `127.0.0.1`, không ghép chuỗi vào shell; khoá riêng RSA không lộ trong repo lẫn lịch sử git; `npm test` 230 ca và pytest 13 ca đạt.

## 2. Góp ý phát triển (xếp theo lợi ích / công)

1. **Một bản "thông số công trình" dùng chung** (`thong_so_cong_trinh.json`): mỗi trường có đơn vị, nguồn (trang + chữ nguyên văn), độ tin cậy, ai xác nhận.
   Bảng đối chiếu, máy kiểm luật, khảo sát, CAD, báo giá, thủy lực cùng đọc bản này thay vì mỗi nơi tự đọc lại PDF với tên trường khác nhau.
2. **Script kiểm liên kết chạy trong bộ test** (`09_SCRIPTS/kiem_lien_ket.py`, 12 phép kiểm, 0 token): đường dẫn chết, mã vật tư lạ, trị số rải lệch nguyên văn,
   trích điều sai file, văn bản hết hiệu lực, lệnh CAD ↔ `PackageContents.xml`, thao tác spec ↔ `TU_VUNG.md`. Lệch dữ liệu bị bắt tự động.
3. **Mỗi bài học một nơi**: bài học đang rải ở 6 nơi (skill, knowledge, bài dự án, bộ nhớ, gợi ý trong bảng KS, mã Python). Giữ MỘT nơi, nơi khác trỏ tới; sửa thì xoá câu cũ.
4. **Máy kiểm luật: thiếu số liệu thì "cần xác minh"**, không bao giờ tự ra Đạt; thêm ca thử trang xoay, bản vẽ font TCVN3, công trình một lối ra.
5. **CAD mượt**: mọi lệnh dài có Esc và thanh tiến độ; hình học nặng dùng chỉ mục lưới thay O(n²); nhớ kết quả explode block; việc đếm khi đổi tab bản vẽ làm trễ.
6. **CAD giao diện**: ảnh block theo màu layer `PCCC_<hệ>`, tô Solid / Hatch, nền theo chủ đề sáng / tối; dữ liệu người dùng sang `%APPDATA%\TKPCCC\`.
7. **Web**: tách `05_FRONTEND/` + `04_BACKEND/`; mọi yêu cầu mang tên dự án từ lúc bấm (không lấy dự án đang chọn lúc trả lời); kiểm Host / Origin.
8. **Bản quyền**: mã máy theo seri ổ cứng, vẫn nhận key cũ; lưu ý repo công khai thì khoá chỉ chặn người không biết build. Muốn chặn thật: tách mã plugin sang repo riêng tư.
9. **Dọn rác**: 3 worktree cũ trong `.claude/worktrees/` (vitest đang quét cả vào), file `.cu*` trong bundle.

## 3. Sơ đồ quy trình: khúc nào dùng agent và model nào

```
PDF / CAD khách gửi
   │  script (0 token): mục lục, tên bản vẽ, chữ vector, tỉ lệ, đếm ký hiệu
   │  agy gemini-4-carbon-medium: trang scan, đọc chữ trên hình (không giao đếm ký hiệu)
   ▼
thong_so_cong_trinh.json (có trích nguồn)  ◄── Opus 5.5 high soát đúng trang còn nghi
   │
   ├─► KHẢO SÁT / SƠ BỘ: phiên chính Opus 5.5 high ─► script điền bảng KS, xuất Excel, ghi chú PDF
   ├─► THẨM ĐỊNH: máy kiểm luật (0 token) ─► agent pccc-xay-dung (Opus high), pccc-he-thong (Sonnet medium; bảng có tính: Opus high) ─► script gộp, kiểm, xuất
   ├─► VẼ CAD: cad-thiet-ke (Opus high) lập spec ─► cad-ve (Sonnet low) chạy ─► cad-kiem (Sonnet medium) soát
   └─► KHỐI LƯỢNG / BÁO GIÁ / THỦY LỰC: plugin + shared (0 token)
```

| Khâu | Việc | Ai làm | Model + effort | Vì sao |
|---|---|---|---|---|
| Đầu vào | Mục lục, quét tên bản vẽ, tỉ lệ trang, đếm ký hiệu PDF vector | script | 0 token | Đúng tuyệt đối; đã đo Gemini đếm sai |
| Đầu vào | PDF scan, chữ trên hình (trục, kích thước, tên phòng) | Antigravity `agy` | `gemini-4-carbon-medium` (khó: `carbon-high`) | Đọc nhiều, không tốn Claude; Claude soát lại trang nghi |
| Khảo sát | Lập báo cáo, chọn bảng KS, phương án | phiên chính + skill `tkpccc-khao-sat-tu-van` | Opus 5.5 high | Kết luận vào hồ sơ khách, nhiều phán đoán |
| Khảo sát | Điền dòng biết trước, xuất Excel, ghi chú PDF | script | 0 token | Việc cơ học |
| Sơ bộ thiết kế | Phân loại, tra điều, diện thẩm định | skill `tkpccc-tra-quy-chuan` | Sonnet 5.5 medium | Tra file có sẵn; trị số quyết định thì chuyển Opus |
| Thẩm định | Sàng lọc 26 luật thoát nạn | `06_THAM_DINH_PDF/` Python | 0 token | Agent chỉ xét dòng "cần xác minh" |
| Thẩm định | Bảng KT (xây dựng) | agent `pccc-xay-dung` | Opus 5.5 high | Thoát nạn, ngăn cháy nhiều điều kiện chồng nhau |
| Thẩm định | Bảng A, B12–B14 (đối tượng trang bị) | agent `pccc-he-thong` | Sonnet 5.5 medium | Tra phụ lục theo nhóm công năng |
| Thẩm định | B1–B11 có trị số tính | agent `pccc-he-thong`, job ghi model | Opus 5.5 high | Sai số học là sai kết luận |
| Thẩm định | Gộp, kiểm, xuất Excel + PDF ghi chú | script | 0 token | |
| Vẽ CAD | Lập spec cả tầng, chia zone, chọn bước rải | agent `cad-thiet-ke` | Opus 5.5 high | Quyết định PCCC + hình học |
| Vẽ CAD | Chạy spec, sửa cú pháp, đếm | agent `cad-ve` | Sonnet 5.5 low | Không phán đoán |
| Vẽ CAD | Soát bản vẽ | agent `cad-kiem` | Sonnet 5.5 medium | Đối chiếu số |
| Mã plugin | Thuật toán, lệnh mới | agent `cad-lap-trinh` | Opus 5.5 high (sửa nhỏ: medium) | |
| Mã plugin | Review cuối mốc | agent `cad-review` | Opus 5.5 max, một lần mỗi mốc | |
| Giao diện | Tra mạng UX, dựng khung giao diện | `agy` | `gemini-4-carbon-medium` | Đọc, viết nhiều; ít rủi ro nghiệp vụ |
| Web | Giao diện | agent `web-frontend` | Sonnet 5.5 medium | |
| Web | API, xuất file, bảo mật | agent `web-backend` | Sonnet 5.5 medium (bảo mật, hợp đồng dữ liệu: Opus high) | |
| Báo giá, thủy lực | Tính | `04_BACKEND/shared/` + Excel mẫu | 0 token | Đã khớp Excel của người dùng |
| Thủy lực | Sửa lõi tính | skill `tkpccc-thuy-luc` | Opus 5.5 high | Sai là sai bơm |
| Điều phối | Chia việc, đọc kết quả, quyết định | phiên đầu não | model phiên chính; giữ ngữ cảnh nhỏ | Mỗi lượt đầu não đọc lại cả ngữ cảnh |
| Review lớn | Soát lại phát hiện NGHIÊM TRỌNG / CAO trước khi sửa | Fable 5.2 | high / xhigh, chỉ mảng quyết định kết quả PCCC | Rất tốn: 3 phiên soát ≈ 30 % hạn mức Fable tuần |
| Khép mốc | Tiến độ, lịch sử, commit | skill `tkpccc-xong-dau-muc` | Sonnet 5.5 low | |

Số đo 15/11 (gói Pro): một phiên review Opus 5.5 xhigh ≈ 13 % cửa sổ 5 giờ; sáu phiên song song ≈ 78 %; một cửa sổ 5 giờ đầy ≈ 13 % hạn mức tuần;
ba phiên Fable 5.2 soát vòng 2 đưa hạn mức Fable tuần từ 12 % lên 42 %. Vì vậy: Fable chỉ dùng soát điểm quyết định; sửa mã dùng Opus / Sonnet; đọc nhiều và tra mạng giao `agy`.

## 4. Hàng việc

Chi tiết từng việc: `12_KET_QUA/review_1115/VIEC.md`. Kết quả từng thợ: `12_KET_QUA/review_1115/KQ_*.md`.

| Thợ (phiên nhánh) | Model | Việc | Thứ tự |
|---|---|---|---|
| R5 → thợ máy kiểm luật | Opus 5.5 high | Thiếu số liệu ra "cần xác minh"; trang xoay; phân loại trang; `bang_doi_chieu.py job` 14 dòng; bảng B4 / B5 | đợt 1 |
| R1 → thợ tri thức | Opus 5.5 high | Chiều cao PCCC 4 nơi; QCVN 10 A.1 (2); checklist buồng thang; gộp bài học trùng | đợt 1 |
| R3b → thợ CAD giao diện | Opus 5.5 high | Ảnh block màu layer + UX; key theo seri ổ cứng (`.exe` + `.py`); dọn `PackageContents.xml` | đợt 1 |
| R4 → thợ web | Sonnet 5.5 medium | Lỗi web; tách `05_FRONTEND/` + `04_BACKEND/`; agent `web-frontend`, `web-backend` | đợt 1 |
| R3a → thợ CAD lõi | Opus 5.5 high | `PCCC_KT` xuyên tường; khoang dầm theo phòng; INSUNITS; vẽ mượt (Esc, tiến độ, chỉ mục) | đợt 2, sau thợ CAD giao diện (cùng build một DLL) |
| R5 + R1 | Opus 5.5 high | `thong_so_cong_trinh.json` + phép kiểm đầu vào; `09_SCRIPTS/kiem_lien_ket.py` | đợt 2 |

Đã xong: `07_CAD/module/lisp/TKPCCC.lsp` (nạp DLL thay NETLOAD) + `07_CAD/cai_dat.bat` (liên kết bundle để AutoCAD tự nạp); luật hạn mức mới trong `CLAUDE.md`.
