# Việc 1 (người dùng test 13/11): rải sai khoảng cách, dim chéo góc

Nhánh `claude/cad-rai-cach-tuong-bc08fd` (worktree riêng, chưa push, chưa gộp main). Gộp xong thì chuyển nội dung chính vào `cad/LICH_SU.md` và xoá file này.

## Đã xong (2026-11-13)
- Lỗi gốc: `LuoiVung` rải lưới theo hộp bao rồi bỏ điểm ngoài đa giác; chỗ hở chỉ được đo bằng vòng tròn nửa đường chéo ô
  (điểm cạnh xiên cách đầu báo 6771 mm theo phương đứng vẫn "phủ" vì < 7425). Ảnh: `ket_qua/cad_test/2026-11-13/1_rai_6771.png`.
- Mô hình phủ mới: mỗi thiết bị phủ ô ±cách tường tối đa theo X và Y (theo `s`: s/2; theo mã: `tuong`; theo `buoc`: max(nửa bước, cách tường lưới)).
  `PhuKin` (Rai.cs): điểm mẫu trong vùng + dọc cạnh, tham lam thêm thiết bị (ưu tiên thẳng cột/hàng lưới, cách tường ≥ `tuong_min`),
  `DiemHo` kiểm CHÍNH XÁC khe hẹp (đỉnh vùng, giao cạnh vùng × cạnh ô, giao cạnh hai ô), cuối cùng lược thiết bị thêm thừa.
  Đa giác của ảnh (dựng lại, s 10500): 13 lưới + 4 bù = 17 đầu báo, mọi điểm lệch ≤ 5250.
- Trị số theo tiêu chuẩn: `data/khoang_cach_rai.json` (gốc repo, nguyên văn TCVN 7568-14:2025 5.9.1.1.x, 5.9.2.1.x; `build.bat` chép cạnh DLL).
  `rai` bỏ `s`/`buoc` thì đọc theo mã (`BC_KHOI`, `BC_KHOI_TRAN`, `BC_NHIET`), tham số mới `h` (trần, m; > `h_max` báo lỗi).
  `PCCC_RAI` với mã có trị số: chỉ hỏi chiều cao trần (Enter bỏ qua), không hỏi S, m², cách tường. Sprinkler vẫn hỏi / cần `s` (chưa có bảng TCVN 7336 nguyên văn).
- `kt`:true: chuỗi kích thước hàng trên cùng (đặt trên vùng) + cột trái (bên trái), tường theo giao với đa giác; mỗi góc lồi 1 kích thước CHÉO
  aligned từ thiết bị gần nhất tới góc (không vòng tròn). Kết quả `rai` thêm `cheo` (chéo lớn nhất).
- Test: `chay_thu.ps1` 84/84 (thêm: đa giác cạnh xiên kiểm độc lập bằng LISP + PowerShell, BC_KHOI theo TCVN 3 x 2 cheo 7212,5, trần 16 m báo lỗi,
  PCCC_RAI BC_KHOI); `thu_mcp.ps1` 9/9. Kỳ vọng cũ đổi có lý do: PCCC_RAI phòng chữ L theo_hang 6 → 7 đầu (trước HO 1,1 %), rai.json ve 53 → 57 (4 dim chéo).
- Tài liệu: `cad/skills/TU_VUNG.md`, skill `tkpccc-ve-cad`, `cad/rule/LUAT.md` BC-02, `cad/data/README.md`, `STRUCTURE.md`.

## Chưa làm / bước tiếp
1. Review Opus max (`cad-review`) cho diff nhánh này trước khi gộp.
2. `PCCC_HELP` (Lenh.cs, hàm `TroGiup`) vẫn ghi PCCC_RAI "nhập bước tối đa S và diện tích bảo vệ": KHÔNG sửa ở đây vì phiên việc 4 làm PCCC_HELP thành panel; khi gộp thì sửa câu đó.
3. Chưa xem ảnh AutoCAD thật của kích thước chéo + chuỗi đặt ngoài vùng (chỉ đếm số đối tượng). Chạy `rai` `kt`:true trên AutoCAD có giao diện rồi `anh` để soát bố cục chữ.
4. Đường chéo góc chưa xét có cắt tường không (vùng lõm sâu) — ghi `shortcut` trong `CheoGoc`.
5. Trường hợp tiêu chuẩn chưa đưa vào file: trần dốc (5.9.1.1.3), dầm > 0,3 m (MOD khoang dầm, nhiệt giảm 30 %), trần < 2 m không gian kín, Bảng 1 thông gió mạnh.
6. Sprinkler: chép bảng khoảng cách đầu phun TCVN 7336 nguyên văn vào `knowledge/` rồi thêm dòng vào `data/khoang_cach_rai.json`.
7. `PCCC_RAI` chưa vẽ kích thước (chỉ thao tác spec `kt`:true có).
