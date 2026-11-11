# cad/rule — luật vẽ, nối tới quy chuẩn

Mỗi dòng nối MỘT việc trên CAD với file quy chuẩn nguyên văn và skill, agent áp dụng nó.
Trị số và điều khoản KHÔNG chép vào đây: khi dùng luật nào thì mở file căn cứ và đọc nguyên văn ở đó (đường dẫn tính từ gốc repo).

Bảng dưới lập ngày 2026-11-11 theo TÊN file trong `knowledge/` và `skills/`, chưa mở từng file để đối chiếu phạm vi. Dòng nào dùng lần đầu thì kiểm lại cột "Căn cứ" rồi bỏ dấu (?) ở cột "Đã kiểm".

| Mã | Việc trên CAD | Thao tác spec | Căn cứ (file nguyên văn) | Skill, agent | Đã kiểm |
|---|---|---|---|---|---|
| BC-01 | Chọn loại đầu báo cho từng phòng | `dat` | `knowledge/chon-loai-dau-bao.md` | `tkpccc-tra-quy-chuan`, agent `pccc-he-thong` | (?) |
| BC-02 | Vị trí, khoảng cách đầu báo; đầu báo trong khoang dầm | `dat` (sau này `rai`) | `knowledge/tcvn7568-14-2025.md`, `skills/MOD_dau_bao_khoi_khoang_dam.md` | `tkpccc-tra-quy-chuan`, agent `pccc-he-thong` | (?) |
| BC-03 | Công trình, khu vực nào phải có báo cháy, chữa cháy tự động | quyết định có vẽ hệ hay không | `knowledge/qcvn10-2025-bao-chay-chua-chay-tu-dong.md` | `tkpccc-tham-dinh`, agent `pccc-he-thong` | (?) |
| NC-01 | Họng nước, cấp nước chữa cháy | `dat`, `ong` | `knowledge/qcvn10-2025-nuoc-chua-chay.md` | `tkpccc-tra-quy-chuan`, agent `pccc-he-thong` | (?) |
| SP-01 | Sprinkler: số liệu đường ống, bơm lấy từ bảng tính thủy lực | `dat`, `ong` (sau này `rai`, `tuyen`) | `knowledge/kiem-bang-tinh-thuy-luc-7336.md` | `tkpccc-thuy-luc` | (?) |
| DN-01 | Đèn sự cố, đèn EXIT, loa | `dat` | `knowledge/qcvn10-2025-den-loa.md` | `tkpccc-tra-quy-chuan`, agent `pccc-he-thong` | (?) |
| BI-01 | Bình chữa cháy, dụng cụ phá dỡ | `dat` | `knowledge/qcvn10-2025-binh-dung-cu-co-gioi.md` | `tkpccc-tra-quy-chuan`, agent `pccc-he-thong` | (?) |
| TN-01 | Tường, cửa đóng kín buồng thang (phương án từ PDF) | `net` (sau này `tuong`, `cua`) | `knowledge/nhan-dien-thang-thoat-nan.md`, `skills/MOD_cau_thang_buong_thang.md` | `tkpccc-khao-sat-tu-van`, `tkpccc-ghi-chu-ban-ve`, agent `pccc-xay-dung` | (?) |
| HK-01 | Hút khói thông tầng | chưa có thao tác | `knowledge/hut-khoi-thong-tang.md`, `skills/MOD_chong_khoi_PL_D.md` | `tkpccc-tham-dinh`, agent `pccc-xay-dung` | (?) |

## Thêm một luật
1. Điều khoản chưa có file nguyên văn: chép vào `knowledge/` gốc trước (skill `tkpccc-tra-quy-chuan`).
2. Thêm 1 dòng vào bảng: mã, việc trên CAD, thao tác, đường dẫn file căn cứ, skill hoặc agent dùng nó.
3. Luật đã thành thuật toán trong plugin (vd bước rải): ghi tên hàm ở cột "Thao tác spec", trị số vẫn truyền từ ngoài vào, không viết cứng trong mã.
