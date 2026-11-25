---
name: tkpccc-quan-ly-drive
description: Quản lý file người dùng gửi lên Google Drive / OneDrive (module 10_DRIVE) — quét xem vừa up file gì, lịch sử thêm / sửa / xoá / dời chỗ, tìm file, dựng cây hồ sơ dự án PCCC đánh số, xếp file nằm lẻ vào thư mục con (có hoàn tác), tạo và ghi sổ link chia sẻ. Dùng khi người dùng nói "tôi vừa up lên drive", "drive có gì mới", "lịch sử drive", "tìm file trên drive", "sắp xếp drive", "quy hoạch drive", "tạo thư mục dự án trên drive", "share file", "lấy link".
model: sonnet
effort: low
---

# Quản lý Drive (10_DRIVE)

Làm qua thư mục đồng bộ trên máy (Google Drive for desktop, OneDrive), không cần API; chỉ chia sẻ cần `rclone`.
Mã: `10_DRIVE/drive.py` (một file, chỉ thư viện chuẩn Python), cây hồ sơ `10_DRIVE/cay_du_an.json`, test `10_DRIVE/test/test_drive.py`.
Dữ liệu thật (tên file, đường dẫn có tên người dùng) ở `12_KET_QUA/drive/`: KHÔNG commit, KHÔNG dán đường dẫn đầy đủ vào repo.

## Lệnh (chạy từ gốc repo)
| Việc | Lệnh |
|---|---|
| Lần đầu trên máy: dò kho (OneDrive + mọi tài khoản DriveFS) | `python 10_DRIVE/drive.py cai` |
| Người dùng vừa up file / hỏi có gì mới | `python 10_DRIVE/drive.py quet` rồi `lich_su --ngay 1` |
| Lịch sử N ngày (ghi `12_KET_QUA/drive/LICH_SU_DRIVE.md`) | `python 10_DRIVE/drive.py lich_su --ngay 7` |
| Tìm file | `python 10_DRIVE/drive.py tim <chữ>` |
| Dựng cây hồ sơ một dự án | `python 10_DRIVE/drive.py tao_cay "<thư mục trên Drive>" DA-xx` |
| Xếp file nằm lẻ vào CAD / PDF / BANG_TINH / VAN_BAN / ANH / NEN | `quy_hoach "<thư mục>"` (chỉ in kế hoạch) → người dùng đồng ý → `quy_hoach "<thư mục>" --lam` |
| Trả lại đợt xếp gần nhất | `python 10_DRIVE/drive.py hoan_tac` |
| Lấy link chia sẻ, ghi sổ | `python 10_DRIVE/drive.py chia_se "<file>"`; sổ: `ds_chia_se` |

## Luật
- Quét chỉ đọc. Xếp file: luôn in kế hoạch trước, chỉ `--lam` khi người dùng đồng ý; không ghi đè (trùng tên thì bỏ qua), không xoá.
- Báo người dùng gọn: số file thêm / sửa / xoá / dời theo kho, vài tên file mới nhất; bảng đủ nằm ở `LICH_SU_DRIVE.md`.
- Lần quét đầu mỗi kho chỉ ghi mốc (`LAN_DAU`), lần sau mới ra THEM / SUA / XOA / DOI_CHO. Bỏ qua mặc định: `1_Claude` (repo này), `node_modules`, `.git`, file tạm Office.
- Luật xếp (`sap_xep`) và thư mục bỏ qua (`bo_qua`) sửa trong `12_KET_QUA/drive/cau_hinh.json`; cây hồ sơ sửa trong `10_DRIVE/cay_du_an.json`.
- Chia sẻ: cần `rclone` (người dùng tự cài `winget install Rclone.Rclone` và tự đăng nhập `rclone config`; Claude không nhập mật khẩu, mã).
  Ghi `"rclone": {"gdrive1": "<tên remote>:"}` vào cấu hình. Link tạo bằng `rclone link` = ai có link đều xem được: hỏi người dùng trước khi tạo.
  Sổ chia sẻ chỉ biết link tạo qua công cụ này; link người dùng tự tạo trên web không có trong sổ.
- Tự quét định kỳ: người dùng tự đặt Task Scheduler (Claude không tự tạo lịch hệ thống), ví dụ
  `schtasks /create /sc minute /mo 30 /tn TKPCCC_Drive /tr "python <repo>\10_DRIVE\drive.py quet"`.
