# Tiến độ TKPCCC

File này cập nhật sau mỗi mốc việc và trước khi hết usage. Phiên mới đọc mục "ĐANG LÀM / VIỆC TIẾP THEO" trước.

## ĐANG LÀM / VIỆC TIẾP THEO (cập nhật 2026-11-01 tối)
**Xong Pha 1 (bóc KL + báo giá), Pha 2 (hướng dẫn B0–B7, khảo sát, báo giá sơ bộ), Pha 3 (thẩm định PDF), Pha 4a (thủy lực TCVN 7336).**
Máy đang làm: `F:\1_ToolMMo\2_VibeCode\PCCCVIP` (Python 3.13 + pymupdf 1.28.2 + pyyaml, Node 24, Claude CLI đã đăng nhập, có Excel, KHÔNG có AutoCAD).
Đã kiểm 2026-11-01: `npm test` 56 test, `python -m pytest thamdinh/test -q` 2 test, trình duyệt (tab Thẩm định PDF, tab Thủy lực: tính sơ bộ,
tính theo số liệu, xuất Excel qua API có Excel COM hoàn thiện; số Excel tự tính khớp web: Q sprinkler 76,2816 / 76,2801).
CHƯA chạy `cad\test\chay_thu.ps1` (máy không có AutoCAD; phiên này không sửa `cad/`). Chưa bấm nút tải Excel trong trình duyệt (chỉ gọi API).

Pha 4a đã làm:
- `shared/src/thuy_luc.ts` + 2 file test chép nguyên từ SKILL-PCCC-NEW (24743f9): 14 test khớp Excel người dùng. `thuy_luc.py` của PcccVipPro
  dùng Hazen–Williams (gần đúng), không theo PL B → không dùng.
- `bang_tinh/thuy_luc/` (Excel mẫu, `o_nhap.json`, `vi_du/`), `scripts/xuat_thuy_luc_excel.py` + 2 `.ps1` (Excel COM) chép từ SKILL-PCCC-NEW.
  Sửa thêm: cờ `--khong-hoan-thien`; hoàn thiện lỗi chỉ cảnh báo; **F106 dùng áp nút b (N103)** theo B.2.10.12 (mẫu gốc dùng L101 = nút a).
- `runner/src/thuy_luc.ts` (chuẩn hoá ô sơ bộ, số liệu → ô Excel: chặn vượt khung 5/4/7 dòng, đổi K → K_gpm, báo khóa không có ô như `P_van`,
  đối xứng thì chép nhánh trái sang nhánh phải); route `GET /api/thuy-luc`, `POST /api/thuy-luc/so-bo`, `POST /api/thuy-luc/tinh`, `GET /api/thuy-luc.xlsx`.
- Web tab "Thủy lực"; `data/huong_dan.json` B4, B5a trỏ tới tab; B5b thêm `knowledge/tcvn-4513-1988-muc-6-19.md` (chép từ SKILL-PCCC-NEW).

Cần người dùng trả lời (thủy lực):
- Lỗi F106 (lưu lượng hàng II dùng áp nút a) và lỗi bảng nhánh phải giữ số cũ khi đối xứng cũng có trong SKILL-PCCC-NEW:
  **người dùng tự sửa bên đó** (trả lời 2026-11-01), TKPCCC không đụng repo đó.
- 👤 Ống vòng/Qtt: Excel mẫu cộng họng theo H.6 (2,5 l/s/tia, ô H18) còn lõi web theo lăng H.7 (2,6 l/s/tia) → Q bơm lệch ~0,2 l/s,
  PB lệch ~0,001 MPa. Theo luật bạn chốt 28/10 ("lưu lượng họng vào bơm, bể = số tia × lưu lượng lăng H.7") thì Excel cần sửa — chọn bên nào?

Pha 3 đã làm (chi tiết, phần đã cắt/sửa: `thamdinh/README.md`):
- `thamdinh/`: lõi Python chép từ repo `taelomuhicd84/PCCC` (TestPCCC) commit `ab546c7` (thư mục `D:\1_Claude\TestPCCC` không có trên máy này).
  CLI mới `python -m pccc_checker run <pdf> --out <dir> [--pages auto|all] [--set k=v]` → `findings.jsonl` D/KD/XM + PDF ghi chú.
- Thêm dòng Đạt; dòng "không đọc được số liệu" xếp XM, không phải KD (luật người dùng trong SKILL-PCCC-NEW: thiếu dữ liệu thì CẦN XÁC NHẬN);
  thiếu nhóm F/chiều cao PCCC/bậc chịu lửa thì từng luật phụ thuộc ra XM thay vì im lặng. Sửa lỗi gốc: chiều cao ≥ 20 m bị gắn "ước lượng".
- Runner `runner/src/thamdinh.ts`: `POST /api/tham-dinh` (thân PDF ≤ 200 MB; rỗng = chạy lại file cũ), `GET /api/tham-dinh`,
  `GET /api/tham-dinh/pdf`, `POST /api/tham-dinh/xet` (Claude xét 1 dòng XM, lưu `claude_xet.json`).
- Web tab "Thẩm định PDF": 9 ô thông tin công trình (→ `--set`), bảng KD đỏ / XM cam nghiêng / Đạt xanh, cột # khớp nhãn PDF.

Việc người dùng cần làm:
- 👤 Đưa PDF hồ sơ thật (vd KS Hoàng Hùng 18 trang) vào máy này để thử tab Thẩm định PDF (kết quả nằm trong `ket_qua/`, không commit).
- 👤 Lưu ý: repo `SKILL-PCCC-NEW` đã công khai cùng 284 file `ket_qua/` hồ sơ khách (Hoàng Hùng, Hoàng Anh, Galaxy…) và PDF TCVN trong
  `8_TAILIEUHOCTAP/`. Muốn ẩn phải xoá khỏi cả lịch sử git hoặc chuyển repo về private. `PcccVipPro` sạch.
- 👤 Máy có AutoCAD: tắt hẳn AutoCAD rồi mở lại để nạp plugin mới.
- 👤 Gửi file đơn giá mẫu của bạn (khi có) để tool đọc đúng cột.

Việc tiếp theo (đề xuất):
1. Thử thẩm định trên hồ sơ thật khi có PDF; so với lần chạy CLI gốc 31/10 trên Hoàng Hùng (Đạt 0, KD 3 đều "không đọc được số liệu", XM 3 →
   nay 3 dòng KD đó phải thành XM).
2. Thủy lực: làm theo câu trả lời 2 câu hỏi trên; thử trên mặt bằng sprinkler thật; mở rộng (hỏi trước): kho nhóm 5–7 (Bảng 2), ô HCB2 cụm van
   trong Excel mẫu, nút "chọn số đầu C07" (`chonSoDau` đã có trong lõi).
3. Pha 3+ (hỏi người dùng trước): đọc bảng cửa/bảng thang để lấy kích thước (bớt XM); nối bảng đối chiếu A/B/KT như `SKILL-PCCC-NEW`
   (`data/bang/*.json`, `shared/src/chon_bang.ts`); xuất Word đối chiếu.
4. Pha 4b: `PCCC_RAI` rải đầu phun/đầu báo theo lưới trong polyline phòng (cần máy có AutoCAD để kiểm).
5. Hỏi người dùng: kích thước/ký hiệu block chuẩn họ muốn (hiện là vòng tròn R200 + chữ), quy tắc ước khối lượng theo kinh nghiệm của họ.

Vấn đề còn mở / giới hạn đã biết:
- Plugin chỉ cho AutoCAD 2021–2024 (.NET Framework 4.8); 2025+ cần build .NET 8.
- Ray-casting khu vực bỏ qua cung (bulge) của polyline; ống trong block đặt ở layer 0 không đếm; VUNGBV chỉ xét thiết bị ở model space cấp 1.
- Báo giá sơ bộ dùng quy tắc mặc định của `knowledge/bao-gia-pccc.md` (DN ống chưa đúng thiết kế, ghi "cỡ ống chờ thiết kế").
- `PCCC_ONG` dùng lệnh PLINE thường (không kiểm được trong accoreconsole, chỉ kiểm trong AutoCAD thật).
- Thẩm định PDF: bản scan (không có lớp chữ) không đọc được; mới có 26 luật thoát nạn QCVN 06; thang bộ được tính là 1 lối ra khi xét
  phân tán 3.2.8 (PDF mẫu có 2 cửa sát nhau nhưng không báo). Mã dòng PC-001… đánh lại mỗi lần chạy nên chạy lại thì xoá "Claude xét" cũ.

## Ghi chú kỹ thuật Pha 3
- Repo tham khảo (đều công khai từ 2026-11-01): `taelomuhicd84/PCCC` (lõi Python gốc), `taelomuhicd84/SKILL-PCCC-NEW` (web thẩm định
  React + runner + 58 bảng đối chiếu + agent m01–m18), `taelomuhicd84/PcccVipPro` (module `pccc` cho Claude Code). Clone vào thư mục tạm, không sửa.
- Bash của Claude bị bộ lọc chặn chạy code của repo tải về; muốn so với bản gốc thì chạy trên bản đã chép trong `thamdinh/`.
- Lõi gốc còn chưa chép (để khi cần): Gemini đọc bản scan, `project_info.py` (đọc thông tin công trình), bảng đối chiếu `rules/checklists/`
  B4–B50, xuất DXF/Word.
- Hoàng Hùng (CLI gốc 31/10): KD là STAIR-DATA BTB1/BTB2 tr 4, EXIT-DOOR-DATA D01 tr 6; thiếu `height_pccc_m`, bậc chịu lửa, tỉ lệ bản vẽ;
  không nhận diện được lối ra tr 17.
