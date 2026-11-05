---
name: tkpccc-thuy-luc
description: Module thủy lực chữa cháy TCVN 7336 (sprinkler, họng nước) — lõi tính, tab web Thủy lực, xuất Excel mẫu MAU_THUY_LUC_7336.xlsx. Dùng khi sửa hoặc phát triển phần thủy lực, tính thủy lực, kiểm bảng tính Excel thủy lực, hoặc người dùng nói "thủy lực", "lưu lượng sprinkler", "bơm chữa cháy", "Excel thủy lực".
model: opus
effort: high
---
# Module: thủy lực TCVN 7336
Phát triển module này: chỉ đọc file trong bảng dưới.

| File | Việc |
|---|---|
| `shared/src/thuy_luc.ts` | lõi: tính, kiểm theo Phụ lục B; sơ bộ từ mặt bằng; so phương án DN; chọn số đầu C07 (`chonSoDau`) |
| `shared/test/thuy_luc.test.ts`, `shared/test/chon_so_dau.test.ts` | test lõi (khớp Excel người dùng) |
| `runner/src/thuy_luc.ts`, `runner/test/thuy_luc.test.ts` | chuẩn hoá ô sơ bộ, số liệu → ô Excel (chặn vượt khung, đổi K → K_gpm, đối xứng chép nhánh trái sang phải), gọi script xuất |
| `runner/src/server.ts` | route `/api/thuy-luc`, `/api/thuy-luc/so-bo`, `/api/thuy-luc/tinh`, `/api/thuy-luc.xlsx` |
| `web/index.html` | tab "Thủy lực" (`data-tab="tl"`, `#tab-tl`) |
| `bang_tinh/thuy_luc/` | `MAU_THUY_LUC_7336.xlsx`, `o_nhap.json` (bản đồ ô nhập), `vi_du/` |
| `scripts/xuat_thuy_luc_excel.py`, `scripts/chen_hang_cuoi.ps1`, `scripts/hoan_thien_excel.ps1` | ghi ô Excel giữ công thức; hoàn thiện bằng Excel COM (`.ps1` UTF-8 có BOM vì có chữ Việt) |
| `knowledge/kiem-bang-tinh-thuy-luc-7336.md`, `knowledge/tcvn-4513-1988-muc-6-19.md` | tri thức, sổ lỗi bảng tính |

## Quy tắc
- Lõi, Excel mẫu và script chép từ SKILL-PCCC-NEW 24743f9 (người dùng phát triển ở đó): sửa ở TKPCCC thì ghi `LICH_SU.md` và báo người dùng để đồng bộ hai repo.
- Đã sửa so với mẫu gốc: F106 dùng áp nút b (N103) theo B.2.10.12; D184 dùng I162 (lăng H.7 tra theo chiều cao tia đặc);
  QBmin mặc định 2,5 l/s (H.5/H.6 chỉ là yêu cầu tối thiểu), cảnh báo khi lăng H.7 nhỏ hơn tối thiểu (người dùng chốt 2026-11-01).
- Số người dùng nhập kiểu Việt Nam: đọc bằng `docSoVN` (`shared/src/bao_gia.ts`).
- Tư duy tính theo Excel mẫu MỚI (người dùng cập nhật 2026-11-05; chi tiết `knowledge/kiem-bang-tinh-thuy-luc-7336.md` mục "Khối dữ liệu thiết kế … MỚI"):
  Smin (Bảng 1) ≠ Stt (diện tích thực tế trên bản vẽ); Qtt = Stt × imin; ntt,tk đếm từ bản vẽ; Ω = Stt/ntt,tk (kết quả, ≤ 12 hoặc 9 m²);
  Qsp = Qmintt (chốt 05/11, Qtt chỉ tham chiếu); q1min = Ω × imin; q1 chỉnh được; P1 = (q1/K)². Đừng dùng cách cũ (n nhập, Ω = 12/9 cố định).
- Đã đồng bộ `o_nhap.json` + script xuất với mẫu mới (05/11): khóa mới `S_tt` (H9); lõi cảnh báo Ω = Stt/n vượt 12/9 m².

## Kiểm
`npx vitest run shared/test/thuy_luc.test.ts shared/test/chon_so_dau.test.ts runner/test/thuy_luc.test.ts`. Xuất Excel hoàn thiện cần máy có Excel (COM).
