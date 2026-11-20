# Nhận dạng loại trần, dầm, khoang (vì kèo, dầm sàn)

Mã: `PCCC_RAI` từ khoá `LoaiTran` [CoTran/KhongTran/HonHop/MaiKeo], `Dam`, `DaySan`, `SuaDam` (`cad/module/plugin/Rai.cs`); `ChiaKhoang`, `DuongViKeo`, `TimDam` (`NhanVung.cs`).
Điều khoản khoang dầm (TCVN 7568-14:2025 5.9.1.1.6): `skills/MOD_dau_bao_khoi_khoang_dam.md` gốc repo. Rải trong khoang: `cad/skills/ve/bao_chay/rai_dau_bao.md`.

## Chọn loại trần TRƯỚC khi rải (người dùng 17/11)
- **Có trần**: rải như trần phẳng theo tiêu chuẩn, bỏ dầm.
- **Không trần** ("không đóng trần" = trần hở, dầm nhô dưới sàn): đọc mặt bằng dầm, xác định trường hợp 5.9.1.1.6, vẽ vùng / khoang rồi mới rải;
  đầu phun bố trí hướng lên giữa các khoang dầm. Nhớ điều này mỗi khi người dùng nói không trần (16/11: bản đầu coi trần phẳng ra 8 đầu, bản gốc 18).
- **Vừa có vừa không**: người dùng tự vẽ vùng và vẽ dầm ở chỗ không trần.
- **Mái kèo** (xưởng): khoang = giữa hai hàng vì kèo.
- Mặt bằng dầm sàn: người dùng block lại, chép đè lên mặt bằng tầng (khớp trục); plugin đọc cỡ dầm b x h trên đó (nét dầm trong block đó là dầm, không dựng tường).

## Loại kết cấu quyết định khoang (người dùng 16/11, 19/11)
- Nhà xưởng mái kèo: khoang theo VÌ KÈO (trục cột), mỗi khoang chạy hết bề rộng xưởng tới tường; KHÔNG chia theo xà gồ dù xà gồ > 0,3 m.
  Hàng vì kèo = hàng cột cắt ngang nhịp, cần ≥ 2 cột nằm trong bề ngang chính phòng (± 0,6 m); cột nhà khác không tạo hàng; hàng sát biên < nửa bước vì kèo gộp vào khoang kề.
  Xưởng không thấy cột: giả định theo hàng cột xưởng bên cạnh rồi hỏi. Chỉ chia phòng ≥ 200 m², hai cạnh ≥ 10 m có lưới cột bên trong.
- Nhà nhiều tầng có sàn deck hoặc sàn BTCT: khoang theo DẦM SÀN (tầng 8 KOCHAM: dầm 400 cách 3,7 m ra 20 khoang).

## Dầm: đọc b x h, tính d
- Nhãn dầm **b x h = rộng x cao** ("B1 (400x600)": rộng 400, cao 600 mm).
- Bản vẽ ghi ngược (KOCHAM "DS 400X250", mặt cắt ghi 250X400): lấy số LỚN làm chiều cao. NGOẠI LỆ dầm bẹt: "DS 1400X250" thật là rộng 1400, cao 700 (17/11):
  gặp dầm bẹt (rộng > cao) thì hỏi người dùng, không tự đảo; `SuaDam` = bấm nét dầm nhập cỡ thật, thắng nhãn.
- **d = h dầm − chiều dày sàn** (phần nhô dưới sàn, người dùng chốt 16/11). Đọc dày sàn ở mặt cắt / ghi chú kết cấu; không có thì hỏi, không đoán (`DaySan`).
- Không có nhãn: hỏi một cỡ dầm cho cả mặt bằng, hoặc người dùng vẽ line dọc dầm và chọn cỡ từng vị trí.
- Xét TỪNG dầm: dầm d ≤ 0,3 m bỏ, coi như trần phẳng (5.9.1.1.6 a). Phải xét CẢ d lẫn chiều cao trần h, không chỉ h (người dùng 15/11).
- Muốn kiểm hiểu đúng: vẽ mặt bằng minh hoạ từng trường hợp a–f cho người dùng xem.
