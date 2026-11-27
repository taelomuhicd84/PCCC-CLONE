# Bản quyền plugin TKPCCC — hướng dẫn cho chủ plugin

Làm MỘT lần theo thứ tự dưới đây. Không cần biết lập trình. Mỗi bước chỉ bấm và dán.

## Cách hoạt động (đọc 1 phút)
- Mỗi máy có một **mã máy** (dạng `D434-5958-...`). Khách gõ `PCCC_BANQUYEN` trong AutoCAD là thấy mã máy của họ.
  Từ 15/11/2026 mã máy lấy từ **số seri ổ cứng chứa Windows** (bắt đầu bằng `D`): cài lại Windows không đổi mã, thay ổ cứng thì đổi mã (cấp key mới).
  Máy không đọc được seri (vài máy ảo, RAID) dùng mã cũ theo Windows. Key cấp theo mã cũ trước ngày đó vẫn dùng được, không phải cấp lại.
- Lùi đồng hồ Windows không kéo dài được key: plugin nhớ ngày lớn nhất đã thấy.
- Bạn có **cặp khoá**: khoá RIÊNG để ký key (GIỮ KÍN, chỉ bạn có), khoá CÔNG KHAI nằm trong plugin để kiểm key.
- **Key** = chữ ký của bạn cho bộ (email, mã máy, hạn). Key đúng máy, đúng email, còn hạn thì plugin mở khoá.
- Chỉ bản build `07_CAD\build.bat phat_hanh` mới khoá lệnh. Bản build thường (`07_CAD\build.bat`) dùng nội bộ, không khoá.

## Bước 1. Tạo cặp khoá
1. Chạy `07_CAD\build.bat` (một lần) để có `07_CAD\17_KEYGEN\TKPCCC.KeyGen.exe`.
2. Mở `07_CAD\17_KEYGEN\TKPCCC.KeyGen.exe`, bấm **Tạo cặp khoá mới**.
   - Khoá công khai ghi vào `07_CAD\03_DU_LIEU_VE\ban_quyen\khoa_cong_khai.xml` (được commit lên GitHub, không sao).
   - Khoá riêng ghi vào `C:\Users\<bạn>\TKPCCC_KHOA\` (`khoa_rieng.xml`, `khoa_rieng.pem`). **Sao lưu thư mục này ra USB.**
     KHÔNG chép vào repo, không gửi ai. Mất khoá riêng = không cấp thêm key được; lộ khoá riêng = ai cũng tự cấp key được.
3. Đã có khoá mà bấm tạo lại: app hỏi xác nhận. Tạo lại thì MỌI key cũ mất hiệu lực.

## Bước 2. Google Sheet nhận đăng ký
1. Vào Google Drive, tạo Google Sheet mới (vd tên "TKPCCC ban quyen").
2. Hàng 1 gõ tiêu đề: `Email` | `Mã máy` | `Hạn` | `Key` | `Trạng thái` (thiếu cột nào script tự thêm).
3. (Tuỳ chọn) Muốn khách điền Google Form: tạo Form có 2 câu hỏi tên đúng `Email` và `Mã máy`,
   tab **Câu trả lời** → **Liên kết với Trang tính** → chọn Sheet ở trên. Script tìm cột theo tiêu đề nên dùng được trang phản hồi của Form
   (nếu sheet có nhiều trang, đặt Script Property `TEN_TRANG` = tên trang phản hồi, vd `Câu trả lời biểu mẫu 1`).

## Bước 3. Dán script
1. Trong Sheet: menu **Tiện ích mở rộng → Apps Script**.
2. Xoá hết mã mẫu, dán toàn bộ nội dung file `07_CAD/11_BAN_QUYEN/keygen/apps_script/Code.gs`, bấm **Lưu**.
3. Bánh răng **Cài đặt dự án** (bên trái) → kéo xuống **Thuộc tính tập lệnh** → **Thêm thuộc tính**:
   | Thuộc tính | Giá trị |
   |---|---|
   | `KHOA_RIENG` | mở KeyGen, bấm **Chép khoá riêng PEM**, dán vào (mất xuống dòng cũng được) |
   | `TU_DONG_CAP` | `true` = ai đăng ký cũng được cấp ngay (người lạ cũng có key: chỉ bật khi phát dùng thử); bỏ trống (KHUYÊN DÙNG) = chỉ cấp hàng bạn gõ `Duyệt` ở cột Trạng thái |
   | `MAX_DANG_KY` | số đăng ký mới tối đa mỗi giờ, chống spam (mặc định 30) |
   | `SO_NGAY` | số ngày dùng khi ô Hạn trống (mặc định 365) |
   | `GUI_MAIL` | `true` = gửi key vào email khách (tuỳ chọn) |
4. Quay lại trình soạn mã, chọn hàm **caiDat** ở thanh trên, bấm **Chạy**. Lần đầu Google hỏi quyền: chọn tài khoản →
   **Nâng cao** → **Đi tới ... (không an toàn)** → **Cho phép** (script của chính bạn). Từ đây cứ 5 phút script quét Sheet một lần.

## Bước 4. Web app (để nút "Đăng ký", "Lấy key tự động" trong plugin chạy)
1. Trong Apps Script: **Triển khai → Tùy chọn triển khai mới** → loại **Ứng dụng web**.
2. **Thực thi dưới tư cách**: Tôi. **Người có quyền truy cập**: Bất kỳ ai. Bấm **Triển khai**, chép **URL ứng dụng web** (đuôi `/exec`).
3. Mở `07_CAD\03_DU_LIEU_VE\ban_quyen\cau_hinh_ban_quyen.json`, dán URL vào `"web_app"`. Có Google Form thì dán link Form vào `"google_form"` (nút "Mở form").
4. Sửa Code.gs sau này: **Triển khai → Quản lý triển khai → sửa → Phiên bản mới** (URL giữ nguyên).

## Bước 5. Build bản phát hành
1. Chạy `07_CAD\build.bat phat_hanh` (thiếu khoá công khai thì báo lỗi, quay lại Bước 1).
2. Gửi khách thư mục `07_CAD\TKPCCC.bundle` (KHÔNG gửi `07_CAD\17_KEYGEN\TKPCCC.KeyGen.exe`, không gửi thư mục `TKPCCC_KHOA`).

## Hằng ngày
- Khách: `PCCC_BANQUYEN` → nhập email → **Đăng ký** → chờ vài phút → **Lấy key tự động** (hoặc dán key bạn gửi, bấm **Kích hoạt**).
- Bạn: không bật `TU_DONG_CAP` thì gõ `Duyệt` vào cột Trạng thái hàng muốn cấp; muốn chặn gõ `Khoá`. Gõ ngày vào cột Hạn trước khi duyệt nếu muốn hạn khác mặc định.
- Cấp tay không cần Google: KeyGen phần **2. Ký key tay** (email, mã máy, hạn) → **Ký key** → **Chép key** gửi khách.
- Dùng Python thay KeyGen: `python 07_CAD\11_BAN_QUYEN\keygen\keygen.py ky <email> <mã máy> <yyyy-MM-dd>` (chỉ thư viện chuẩn;
  khoá riêng mặc định `C:\Users\<bạn>\TKPCCC_KHOA\khoa_rieng.pem`, đổi bằng `--khoa <file>`). `python keygen.py may` in mã máy của máy đang chạy.
  Key từ KeyGen.exe, keygen.py và Google Sheet giống nhau từng ký tự.
- Gia hạn: xoá ô Key của hàng đó, sửa Hạn, (gõ `Duyệt`) → 5 phút sau có key mới; khách bấm **Lấy key tự động**.
- Khi chưa có bản quyền, bản phát hành chặn mọi lệnh `PCCC*` (trừ `PCCC`, `PCCC_BANQUYEN`, `PCCC_CAIDAT`, `PCCC_HELP`) và việc vẽ qua MCP trả lỗi "Chưa có bản quyền".
- Web app từ chối email lạ (có `=`, ngoặc, dấu cách...): chặn chèn công thức vào Sheet làm lộ email khách.
- Giới hạn: mã nguồn plugin công khai trên GitHub nên ai biết build vẫn tạo được bản không khoá; bản quyền chỉ chặn người dùng thông thường.
