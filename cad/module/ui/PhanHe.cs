// Thẻ của các phân hệ dạng lưới (spec addin mẫu 14/11: Chữa cháy, Đường ống, Sơ đồ, Thống kê, Hồ sơ gộp thành 2 phân hệ Chữa cháy, Thống kê).
// Mỗi dòng { nhóm, mã tắt, tên, mô tả, lệnh }: lệnh PCCC_* có sẵn hoặc "#..." việc của giao diện (BangTienIch.Chay). Thêm thẻ = thêm 1 dòng.
namespace TKPCCC
{
    static class PhanHe
    {
        public static readonly string[][] CHUA_CHAY = {
            new[] { "1", "SPK", "Rải đầu phun", "Rải sprinkler theo phòng (nhập S, m² theo TCVN 7336)", "PCCC_RAI\n" },
            new[] { "1", "DAT", "Đặt đầu phun, thiết bị", "Đặt từng cái theo mã vật tư", "PCCC_DAT\n" },
            new[] { "1", "R", "Vùng bảo vệ R", "Vòng R quanh đầu phun, báo đứng lẻ", "PCCC_VUNGBV\n" },
            new[] { "2", "TUY", "Ống nhánh xương cá", "Chọn đầu phun, ống chính, bấm ống góp", "PCCC_TUYEN\n" },
            new[] { "2", "PD", "Vẽ ống theo DN", "Chọn cỡ ống DN25–DN400, vẽ tuyến", "PCCC_ONG\n" },
            new[] { "2", "DN", "Cập nhật DN", "Đổi cỡ đoạn ống đã chọn", "PCCC_DOIDN\n" },
            new[] { "2", "ADIM", "Dim đầu phun, ống", "Trang 5 bước: chuỗi dim tự động", "#adim" },
            new[] { "3", "SD", "Sơ đồ nguyên lý", "Nhờ bot chat vẽ từ mặt bằng đang mở", "#bot" },
            new[] { "3", "CKC", "Kiểm tra trùng", "Đầu phun đặt trùng điểm", "PCCC_TRUNG\n" },
            new[] { "3", "CKL", "Kiểm tra Layer", "Thiết bị sai layer PCCC_<hệ>", "PCCC_LOPCHUAN\n" } };
        public static readonly string[] NHOM_CC = { "1. BỐ TRÍ ĐẦU PHUN", "2. ĐƯỜNG ỐNG", "3. SƠ ĐỒ, KIỂM TRA" };
        public static readonly string[] NUT_CC = { "Tất cả", "1. Bố trí", "2. Đường ống", "3. Sơ đồ, kiểm tra" };

        public static readonly string[][] THONG_KE = {
            new[] { "1", "BOC", "Bóc khối lượng", "Theo tầng, khu; gửi web báo giá", "PCCC_BOC\n" },
            new[] { "1", "BANG", "Chèn bảng thống kê", "Bảng từ lần bóc gần nhất", "PCCC_BANG\n" },
            new[] { "1", "GAN", "Gán block có sẵn", "Block bản vẽ khác → mã vật tư", "PCCC_GAN\n" },
            new[] { "1", "THEM", "Thêm block vào thư viện", "Block của bạn thành mã vật tư mới", "PCCC_THEMBLOCK\n" },
            new[] { "2", "AN", "Đánh số thiết bị", "Tiền tố + số tăng dần", "PCCC_DANHSO\n" },
            new[] { "2", "CKO", "Kiểm tra vùng mù", "% mù từng phòng theo R", "PCCC_KT\n" },
            new[] { "2", "CKC", "Kiểm tra trùng", "Thiết bị đặt trùng điểm", "PCCC_TRUNG\n" },
            new[] { "2", "BC", "Bố trí báo cháy", "Mở phân hệ Báo cháy", "#botri" } };
        public static readonly string[] NHOM_TK = { "1. KHỐI LƯỢNG, BẢNG", "2. HỒ SƠ, KIỂM TRA" };
        public static readonly string[] NUT_TK = { "Tất cả", "1. Khối lượng", "2. Hồ sơ, kiểm tra" };
    }
}
