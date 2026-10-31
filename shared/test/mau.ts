// Dữ liệu mẫu dùng chung cho test của shared/ và runner/.
import type { DanhMuc, KhoiLuong } from "../src/bao_gia";

export const dm: DanhMuc = {
  he: { BC: "Báo cháy tự động", ONG: "Đường ống" },
  vat_tu: [
    { ma: "BC_KHOI", ten: "Đầu báo khói", dvt: "cái", he: "BC", loai: "block" },
    { ma: "BC_TT", ten: "Tủ trung tâm", dvt: "bộ", he: "BC", loai: "block" },
    { ma: "ONG_DN50", ten: "Ống DN50", dvt: "m", he: "ONG", loai: "tuyen" },
  ],
};
export const kl: KhoiLuong = { du_an: "Thử", dong: [{ ma: "BC_KHOI", sl: 10 }, { ma: "BC_KHOI", sl: 2 }, { ma: "ONG_DN50", sl: 12.345 }, { ma: "BC_TT", sl: 1 }, { ma: "LA", sl: 3 }] };
