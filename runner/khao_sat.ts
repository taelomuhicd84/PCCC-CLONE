// Báo cáo khảo sát PCCC (8 mục theo knowledge/khao-sat-bao-gia.md) + ước khối lượng sơ bộ theo m² (knowledge/bao-gia-pccc.md).
import type { KhoiLuong } from "./bao_gia";

export type TrangThai = "CÓ" | "KHÔNG" | "CẦN XÁC NHẬN" | "";
export type HeThong = { ten: string; co: TrangThai; can_cu: string; ghi_chu: string };
export type KhaoSat = {
  du_an: string;
  hang_muc: string; cong_nang: string;
  dien_tich_m2: number | null; so_tang: number | null; so_tang_ham: number | null;
  chieu_cao_pccc: string; chieu_cao_m: number | null; khoi_tich: string;
  chu_vi_m: number | null;
  dien_tham_dinh: TrangThai; dien_tham_dinh_can_cu: string;
  so_loi_thoat_nan: number | null; loi_thoat_nan: string; thoat_nan_danh_gia: string; thoat_nan_ghi_chu: string;
  he_thong: HeThong[];
  be_nuoc_pccc: string; be_nuoc_m3: number | null;
};

/** Danh sách hệ thống đúng thứ tự mục 7 của khao-sat-bao-gia.md; `ma` dùng nội bộ để ước khối lượng. */
export const HE_THONG = [
  { ma: "bao_chay", ten: "Báo cháy tự động" },
  { ma: "hvt", ten: "Chữa cháy họng nước vách tường" },
  { ma: "sprinkler", ten: "Chữa cháy tự động (sprinkler, drencher)" },
  { ma: "nuoc_ngoai", ten: "Cấp nước chữa cháy ngoài nhà (trụ, bể)" },
  { ma: "tram_bom", ten: "Trạm bơm" },
  { ma: "hut_khoi", ten: "Hút xả khói, tăng áp thang" },
  { ma: "den", ten: "Chiếu sáng sự cố và chỉ dẫn thoát nạn" },
  { ma: "binh", ten: "Bình chữa cháy" },
  { ma: "cap_dien", ten: "Cấp điện cho PCCC" },
  { ma: "chong_set", ten: "Chống sét" },
  { ma: "thang_may", ten: "Thang máy chữa cháy" },
  { ma: "duong_xe", ten: "Đường và bãi đỗ xe chữa cháy" },
  { ma: "ngan_chay", ten: "Ngăn cháy (cửa, tường, sàn) và vật liệu" },
] as const;

export function khaoSatTrong(du_an = ""): KhaoSat {
  return {
    du_an, hang_muc: "", cong_nang: "", dien_tich_m2: null, so_tang: null, so_tang_ham: null,
    chieu_cao_pccc: "", chieu_cao_m: null, khoi_tich: "", chu_vi_m: null,
    dien_tham_dinh: "", dien_tham_dinh_can_cu: "",
    so_loi_thoat_nan: null, loi_thoat_nan: "", thoat_nan_danh_gia: "", thoat_nan_ghi_chu: "",
    he_thong: HE_THONG.map((h) => ({ ten: h.ten, co: "", can_cu: "", ghi_chu: "" })),
    be_nuoc_pccc: "", be_nuoc_m3: null,
  };
}

const chuan = (s: string) => s.normalize("NFC").toLowerCase().replace(/[^\p{L}\p{N}]+/gu, " ").trim();

/** Trạng thái hệ thống theo mã (khớp tên không phân biệt hoa thường/dấu câu). */
function trangThai(ks: KhaoSat, ma: string): TrangThai {
  const ten = chuan(HE_THONG.find((h) => h.ma === ma)!.ten);
  return ks.he_thong.find((h) => chuan(h.ten) === ten)?.co ?? "";
}

export type DongUoc = { ma: string; sl: number; cach_uoc: string; cho_xac_nhan: boolean };

/**
 * Ước khối lượng sơ bộ theo quy tắc mặc định của knowledge/bao-gia-pccc.md, chỉ cho hệ thống CÓ / CẦN XÁC NHẬN.
 * Đây là ƯỚC LƯỢNG: mỗi dòng ghi cách ước. Hệ CẦN XÁC NHẬN đánh dấu cho_xac_nhan.
 */
export function uocKhoiLuong(ks: KhaoSat): DongUoc[] {
  const S = ks.dien_tich_m2 ?? 0;
  const tang = Math.max(1, (ks.so_tang ?? 1) + (ks.so_tang_ham ?? 0));
  const loi = Math.max(1, ks.so_loi_thoat_nan ?? 1);
  const H = ks.chieu_cao_m ?? tang * 3.5;
  const sTang = S / tang;
  const out: DongUoc[] = [];
  const them = (he: string, ma: string, sl: number, cach: string) => {
    const t = trangThai(ks, he);
    if ((t === "CÓ" || t === "CẦN XÁC NHẬN") && sl > 0) out.push({ ma, sl: Math.ceil(sl), cach_uoc: cach, cho_xac_nhan: t === "CẦN XÁC NHẬN" });
  };
  if (S <= 0) return out;

  them("bao_chay", "BC_TT", 1, "1 bộ; số kênh ≥ số tầng");
  them("bao_chay", "BC_KHOI", S / 60, `${S} m² / 60`);
  them("bao_chay", "BC_TOHOP", loi * tang, `${loi} lối × ${tang} tầng`);
  them("bao_chay", "DAY_BC", S * 0.6, `${S} m² × 0,6 m/m²`);

  them("hvt", "HVT_TU", Math.max(1, Math.ceil(sTang / 500)) * tang, `max(1, ${Math.round(sTang)} m²/tầng / 500) × ${tang} tầng`);
  them("hvt", "ONG_DN50", H + 15 * tang, `cao nhà ${H} m + 15 m/tầng × ${tang}`);

  them("sprinkler", "SPR_DAU_DOWN", S / 10, `${S} m² / 10`);
  them("sprinkler", "ONG_DN32", S * 0.3, `${S} m² × 0,3 m/m² (cỡ ống chờ thiết kế)`);
  them("sprinkler", "SPR_VAN_BD", 1, "1 van báo động / hệ");

  const chuVi = ks.chu_vi_m ?? 0;
  them("nuoc_ngoai", "NUOC_TRU", Math.max(1, Math.ceil(chuVi / 150)), chuVi ? `chu vi ${chuVi} m / 150` : "tối thiểu 1 (chưa có chu vi)");

  them("tram_bom", "NUOC_BOM_DIEN", 1, "bơm điện chính");
  them("tram_bom", "NUOC_BOM_DIESEL", 1, "bơm dự phòng (diesel hoặc điện)");
  them("tram_bom", "NUOC_BOM_BU", 1, "bơm bù áp");
  them("tram_bom", "NUOC_TU_DK", 1, "1 tủ điều khiển");
  if (ks.be_nuoc_m3) them("tram_bom", "BE_NUOC", ks.be_nuoc_m3, "dung tích theo khảo sát");

  them("hut_khoi", "QUAT_TANG_AP", loi, `1 quạt / buồng thang (ước = số lối ${loi})`);
  them("den", "DEN_SUCO", S / 100, `${S} m² / 100 (chưa có diện tích hành lang)`);
  them("den", "DEN_EXIT", loi * tang * 2, `${loi} lối × ${tang} tầng × 2`);
  them("binh", "BINH_BOT", Math.max(tang, S / 150), `max(${tang} tầng, ${S} m² / 150)`);
  them("binh", "BINH_CO2", Math.max(tang, S / 150), "đi kèm bình bột (bộ 2 bình)");
  them("cap_dien", "DIEN_TU_UT", 1, "1 tủ");
  them("cap_dien", "CAP_CC", H + 20, `cao nhà ${H} m + 20 m`);
  them("chong_set", "CHONG_SET", 1, "1 hệ");
  them("ngan_chay", "CUA_CC", loi * tang, `${loi} buồng thang (ước) × ${tang} tầng`);
  return out;
}

/** Khối lượng sơ bộ ở dạng runner dùng chung với khối lượng bóc từ CAD; cách ước nằm trong ghi_chu. */
export function khoiLuongSoBo(ks: KhaoSat): KhoiLuong & { ghi_chu: Record<string, string> } {
  const d = uocKhoiLuong(ks);
  return {
    du_an: ks.du_an, ban_ve: "ước theo khảo sát", luc: new Date().toISOString(),
    dong: d.map((x) => ({ ma: x.ma, sl: x.sl })),
    ghi_chu: Object.fromEntries(d.map((x) => [x.ma, (x.cho_xac_nhan ? "chờ xác nhận; " : "") + "ước: " + x.cach_uoc])),
  };
}

/** Lấy khối ```json cuối cùng trong trả lời của Claude, trả về object khao_sat. */
export function tachJsonKhaoSat(traLoi: string): Partial<KhaoSat> | null {
  const khoi = [...traLoi.matchAll(/```json\s*([\s\S]*?)```/g)].map((m) => m[1]);
  for (const k of khoi.reverse()) {
    try { const j = JSON.parse(k); if (j?.khao_sat) return j.khao_sat; } catch { /* thử khối trước */ }
  }
  return null;
}
