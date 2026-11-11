# cad/rule — luật vẽ, nối tới quy chuẩn

Mỗi dòng nối MỘT việc trên CAD với file quy chuẩn nguyên văn và skill, agent áp dụng nó.
Trị số và điều khoản KHÔNG chép vào đây: khi dùng luật nào thì mở file căn cứ và đọc nguyên văn ở đó (đường dẫn tính từ gốc repo).

Bảng dưới lập ngày 2026-11-11 theo TÊN file trong `knowledge/` và `skills/`, chưa mở từng file để đối chiếu phạm vi. Dòng nào dùng lần đầu thì kiểm lại cột "Căn cứ" rồi bỏ dấu (?) ở cột "Đã kiểm".

| Mã | Việc trên CAD | Thao tác spec | Căn cứ (file nguyên văn) | Skill, agent | Đã kiểm |
|---|---|---|---|---|---|
| BC-01 | Chọn loại đầu báo cho từng phòng | `dat` | `knowledge/chon-loai-dau-bao.md` | `tkpccc-tra-quy-chuan`, agent `pccc-he-thong` | (?) |
| BC-02 | Vị trí, khoảng cách đầu báo; đầu báo trong khoang dầm | `dat`, `rai` (`s` = khoảng cách tối đa giữa hai đầu báo) | `knowledge/tcvn7568-14-2025.md`, `skills/MOD_dau_bao_khoi_khoang_dam.md` | `tkpccc-tra-quy-chuan`, agent `pccc-he-thong` | (?) |
| BC-03 | Công trình, khu vực nào phải có báo cháy, chữa cháy tự động | quyết định có vẽ hệ hay không | `knowledge/qcvn10-2025-bao-chay-chua-chay-tu-dong.md` | `tkpccc-tham-dinh`, agent `pccc-he-thong` | (?) |
| NC-01 | Họng nước, cấp nước chữa cháy | `dat`, `ong` | `knowledge/qcvn10-2025-nuoc-chua-chay.md` | `tkpccc-tra-quy-chuan`, agent `pccc-he-thong` | (?) |
| SP-01 | Sprinkler: số liệu đường ống, bơm lấy từ bảng tính thủy lực | `dat`, `ong` (sau này `rai`, `tuyen`) | `knowledge/kiem-bang-tinh-thuy-luc-7336.md` | `tkpccc-thuy-luc` | (?) |
| BC-04 | Chia zone (vùng phát hiện cháy) hệ thường: giới hạn diện tích và số đầu báo một zone; người dùng dặn thêm không quá 25 đầu báo một zone, zone theo khu vực, không lấn sang khu vực khác | việc của AI thiết kế TRƯỚC khi gọi `tuyen` (plugin không chia zone) | `knowledge/tcvn7568-14-2025.md` (mục 5.7), `cad/knowledge/tu-duy-di-day-di-ong.md` (mục 2) | agent `cad-thiet-ke` (chưa tạo) | (?) |
| BC-05 | Đi dây báo cháy: hệ thường rắn bò phòng nối phòng, không rẽ chữ T, cuối zone có điện trở cuối dây; đèn báo phòng thì mỗi đầu báo một nét ra đèn; hệ địa chỉ ZM, CM, RM, mạch vòng | `tuyen` kiểu `noi_tiep`, `tia`; sơ đồ nguyên lý `so_do` (chưa có) | `cad/knowledge/tu-duy-di-day-di-ong.md` (mục 1, 2) | `tkpccc-ve-cad`, agent `cad-thiet-ke` (chưa tạo) | (?) |
| SP-02 | Rải đầu phun: bước tối đa `s` và diện tích bảo vệ tối đa của một đầu phun `dt` theo nhóm nguy cơ cháy; trước khi rải vẽ polyline kín từng khu vực (tường, dầm sàn, vì kèo) lên layer `PCCC_VUNG` | `rai` với `s` + `dt` (hàm `LuoiVung`, `module/plugin/Rai.cs`), lệnh `PCCC_RAI`; ống nhánh `tuyen` `xuong_ca`, lệnh `PCCC_TUYEN` | `knowledge/kiem-bang-tinh-thuy-luc-7336.md` (ô Ω và Bảng 1; chưa có bản nguyên văn bảng khoảng cách đầu phun của TCVN 7336, chép vào `knowledge/` trước khi dùng), `cad/knowledge/tu-duy-di-day-di-ong.md` (mục 3, 4) | `tkpccc-thuy-luc`, `tkpccc-ve-cad` | (?) |
| DN-01 | Đèn sự cố, đèn EXIT, loa | `dat` | `knowledge/qcvn10-2025-den-loa.md` | `tkpccc-tra-quy-chuan`, agent `pccc-he-thong` | (?) |
| BI-01 | Bình chữa cháy, dụng cụ phá dỡ | `dat` | `knowledge/qcvn10-2025-binh-dung-cu-co-gioi.md` | `tkpccc-tra-quy-chuan`, agent `pccc-he-thong` | (?) |
| TN-01 | Tường, cửa đóng kín buồng thang (phương án từ PDF) | `net` (sau này `tuong`, `cua`) | `knowledge/nhan-dien-thang-thoat-nan.md`, `skills/MOD_cau_thang_buong_thang.md` | `tkpccc-khao-sat-tu-van`, `tkpccc-ghi-chu-ban-ve`, agent `pccc-xay-dung` | (?) |
| HK-01 | Hút khói thông tầng | chưa có thao tác | `knowledge/hut-khoi-thong-tang.md`, `skills/MOD_chong_khoi_PL_D.md` | `tkpccc-tham-dinh`, agent `pccc-xay-dung` | (?) |

## Thêm một luật
1. Điều khoản chưa có file nguyên văn: chép vào `knowledge/` gốc trước (skill `tkpccc-tra-quy-chuan`).
2. Thêm 1 dòng vào bảng: mã, việc trên CAD, thao tác, đường dẫn file căn cứ, skill hoặc agent dùng nó.
3. Luật đã thành thuật toán trong plugin (vd bước rải): ghi tên hàm ở cột "Thao tác spec", trị số vẫn truyền từ ngoài vào, không viết cứng trong mã.
