// Báo cáo khảo sát PCCC (8 mục theo knowledge/khao-sat-bao-gia.md) + ước khối lượng sơ bộ theo m² (knowledge/bao-gia-pccc.md).
import { docSoVN, type KhoiLuong } from "./bao_gia";

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

/**
 * Hệ thống đúng thứ tự mục 7 của khao-sat-bao-gia.md. `tu` = từ khoá (không dấu) để nhận tên Claude/người dùng viết khác đi;
 * xét theo thứ tự, mục đầu tiên khớp thì nhận.
 */
export const HE_THONG = [
  { ma: "bao_chay", ten: "Báo cháy tự động", tu: ["bao chay"] },
  { ma: "hvt", ten: "Chữa cháy họng nước vách tường", tu: ["hong nuoc", "vach tuong"] },
  { ma: "sprinkler", ten: "Chữa cháy tự động (sprinkler, drencher)", tu: ["sprinkler", "drencher", "chua chay tu dong"] },
  { ma: "nuoc_ngoai", ten: "Cấp nước chữa cháy ngoài nhà (trụ, bể)", tu: ["ngoai nha"] },
  { ma: "tram_bom", ten: "Trạm bơm", tu: ["tram bom", "may bom"] },
  { ma: "hut_khoi", ten: "Hút xả khói, tăng áp thang", tu: ["khoi", "tang ap"] },
  { ma: "den", ten: "Chiếu sáng sự cố và chỉ dẫn thoát nạn", tu: ["chieu sang", "chi dan", "exit"] },
  { ma: "binh", ten: "Bình chữa cháy", tu: ["binh chua chay", "binh bot", "binh co2"] },
  { ma: "cap_dien", ten: "Cấp điện cho PCCC", tu: ["cap dien", "nguon dien"] },
  { ma: "chong_set", ten: "Chống sét", tu: ["chong set"] },
  { ma: "thang_may", ten: "Thang máy chữa cháy", tu: ["thang may"] },
  { ma: "duong_xe", ten: "Đường và bãi đỗ xe chữa cháy", tu: ["bai do", "duong cho xe", "duong va bai", "xe chua chay"] },
  { ma: "ngan_chay", ten: "Ngăn cháy (cửa, tường, sàn) và vật liệu", tu: ["ngan chay", "chong chay"] },
] as const;

const SO = ["dien_tich_m2", "so_tang", "so_tang_ham", "chieu_cao_m", "chu_vi_m", "so_loi_thoat_nan", "be_nuoc_m3"] as const;
const CHU = ["hang_muc", "cong_nang", "chieu_cao_pccc", "khoi_tich", "dien_tham_dinh_can_cu", "loi_thoat_nan", "thoat_nan_ghi_chu", "be_nuoc_pccc"] as const;

export function khaoSatTrong(du_an = ""): KhaoSat {
  return {
    du_an, hang_muc: "", cong_nang: "", dien_tich_m2: null, so_tang: null, so_tang_ham: null,
    chieu_cao_pccc: "", chieu_cao_m: null, khoi_tich: "", chu_vi_m: null,
    dien_tham_dinh: "", dien_tham_dinh_can_cu: "",
    so_loi_thoat_nan: null, loi_thoat_nan: "", thoat_nan_danh_gia: "", thoat_nan_ghi_chu: "",
    he_thong: HE_THONG.map((h) => ({ ten: h.ten, co: "" as TrangThai, can_cu: "", ghi_chu: "" })),
    be_nuoc_pccc: "", be_nuoc_m3: null,
  };
}

/** Bỏ dấu, chữ thường, chỉ giữ chữ số cách nhau 1 dấu cách. */
export const boDau = (s: unknown) =>
  String(s ?? "").normalize("NFD").replace(/[̀-ͯ]/g, "").replace(/[đĐ]/g, "d").toLowerCase().replace(/[^a-z0-9]+/g, " ").trim();

export function maHeThong(ten: unknown): string | null {
  const s = ` ${boDau(ten)} `;
  return HE_THONG.find((h) => h.tu.some((t) => s.includes(` ${t} `)))?.ma ?? null;
}

function chuanTrangThai(x: unknown): TrangThai {
  const s = boDau(x);
  if (s.includes("xac nhan")) return "CẦN XÁC NHẬN";
  if (/^khong\b/.test(s)) return "KHÔNG";
  if (/^co\b/.test(s)) return "CÓ";
  return "";
}

function chuanDanhGia(x: unknown): string {
  const s = boDau(x);
  if (s.includes("xac nhan")) return "CẦN XÁC NHẬN";
  if (s.includes("chua dat") || s.includes("khong dat")) return "CHƯA ĐẠT";
  if (/^dat\b/.test(s)) return "ĐẠT";
  return "";
}

const chu = (x: unknown) => (x == null ? "" : String(x).trim());
/** Trạng thái viết khác dạng chuẩn ("có (PL III mục 9)", "tuỳ") thì giữ nguyên chữ gốc vào ghi chú/căn cứ để không mất ý. */
const kem = (goc: unknown, chuan: string, ghiChu: string) =>
  !chu(goc) || boDau(goc) === boDau(chuan) ? ghiChu : [chu(goc), ghiChu].filter(Boolean).join("; ");

/** Đưa dữ liệu bất kỳ (form, file cũ, JSON của Claude) về đúng khuôn KhaoSat; hệ thống xếp theo HE_THONG, hệ lạ để cuối. */
export function chuanHoaKhaoSat(x: unknown, du_an: string): KhaoSat {
  const v = (x && typeof x === "object" ? x : {}) as Record<string, unknown>;
  const k = khaoSatTrong(du_an);
  for (const f of CHU) k[f] = chu(v[f]);
  for (const f of SO) k[f] = docSoVN(v[f]);
  k.dien_tham_dinh = chuanTrangThai(v.dien_tham_dinh);
  k.dien_tham_dinh_can_cu = kem(v.dien_tham_dinh, k.dien_tham_dinh, k.dien_tham_dinh_can_cu);
  k.thoat_nan_danh_gia = chuanDanhGia(v.thoat_nan_danh_gia);
  k.thoat_nan_ghi_chu = kem(v.thoat_nan_danh_gia, k.thoat_nan_danh_gia, k.thoat_nan_ghi_chu);

  const conLai = (Array.isArray(v.he_thong) ? v.he_thong : []).filter((e) => e && typeof e === "object") as Record<string, unknown>[];
  const doc = (e: Record<string, unknown>, ten: string): HeThong => {
    const co = chuanTrangThai(e.co);
    return { ten, co, can_cu: chu(e.can_cu), ghi_chu: kem(e.co, co, chu(e.ghi_chu)) };
  };
  k.he_thong = HE_THONG.map((h) => {
    const i = conLai.findIndex((e) => maHeThong(e.ten) === h.ma);
    return i < 0 ? { ten: h.ten, co: "", can_cu: "", ghi_chu: "" } : doc(conLai.splice(i, 1)[0], h.ten);
  });
  for (const e of conLai) if (chu(e.ten)) k.he_thong.push(doc(e, chu(e.ten)));
  return k;
}

/**
 * Ghép gợi ý của Claude vào khảo sát của người dùng. Số liệu thực tế người dùng đã nhập (hạng mục, diện tích, số tầng, chiều cao,
 * chu vi, số lối, bể) giữ nguyên; phần đánh giá (công năng/nhóm F, chiều cao PCCC, khối tích, diện thẩm định, thoát nạn, hệ thống,
 * bể nước) lấy của Claude, Claude để trống thì giữ của người dùng.
 */
export function ghepGoiY(nguoiDung: KhaoSat, claude: KhaoSat): KhaoSat {
  const k = { ...claude, du_an: nguoiDung.du_an };
  const giuNguoiDung = ["hang_muc", "dien_tich_m2", "so_tang", "so_tang_ham", "chieu_cao_m", "chu_vi_m", "so_loi_thoat_nan", "be_nuoc_m3", "loi_thoat_nan"] as const;
  const r = k as Record<string, unknown>, u = nguoiDung as Record<string, unknown>;
  for (const f of giuNguoiDung) if (u[f] != null && u[f] !== "") r[f] = u[f];
  for (const f of Object.keys(u)) if (f !== "he_thong" && (r[f] == null || r[f] === "")) r[f] = u[f];
  const khoa = (h: HeThong) => maHeThong(h.ten) ?? h.ten;
  const daGhep = new Set<HeThong>();
  k.he_thong = claude.he_thong.map((h) => {
    const cu = nguoiDung.he_thong.find((x) => !daGhep.has(x) && khoa(x) === khoa(h));
    if (!cu) return h;
    daGhep.add(cu);
    return { ten: h.ten, co: h.co || cu.co, can_cu: h.can_cu || cu.can_cu, ghi_chu: h.ghi_chu || cu.ghi_chu };
  });
  // Hệ người dùng tự thêm mà Claude không nhắc tới: giữ lại.
  for (const x of nguoiDung.he_thong) if (!daGhep.has(x) && (x.co || x.can_cu || x.ghi_chu)) k.he_thong.push(x);
  return k;
}

/** Trạng thái hệ thống theo mã (khớp tên theo từ khoá). */
function trangThai(ks: KhaoSat, ma: string): TrangThai {
  return ks.he_thong.find((h) => maHeThong(h.ten) === ma)?.co ?? "";
}

export type DongUoc = { ma: string; sl: number; cach_uoc: string; cho_xac_nhan: boolean };

/**
 * Ước khối lượng sơ bộ theo quy tắc mặc định của knowledge/bao-gia-pccc.md, chỉ cho hệ thống CÓ / CẦN XÁC NHẬN.
 * Đây là ƯỚC LƯỢNG: mỗi dòng ghi cách ước. Hệ CẦN XÁC NHẬN đánh dấu cho_xac_nhan.
 */
export function uocKhoiLuong(ks: KhaoSat): DongUoc[] {
  const S = ks.dien_tich_m2 ?? 0;
  if (S <= 0) return [];
  const tangNoi = Math.max(1, ks.so_tang ?? 1);
  const tang = tangNoi + Math.max(0, ks.so_tang_ham ?? 0);
  const loi = Math.max(1, ks.so_loi_thoat_nan ?? 1);
  const H = ks.chieu_cao_m ?? tangNoi * 3.5;
  const nhanH = ks.chieu_cao_m != null ? `cao nhà ${H} m` : `cao nhà ước ${tangNoi} tầng × 3,5 = ${H} m`;
  const sTang = S / tang;
  const m2 = S.toLocaleString("vi-VN") + " m²";
  const out: DongUoc[] = [];
  const dong = (ma: string, sl: number, cach: string, cho: boolean) => sl > 0 && out.push({ ma, sl: Math.ceil(sl), cach_uoc: cach, cho_xac_nhan: cho });
  const them = (he: string, ma: string, sl: number, cach: string) => {
    const t = trangThai(ks, he);
    if (t === "CÓ" || t === "CẦN XÁC NHẬN") dong(ma, sl, cach, t === "CẦN XÁC NHẬN");
  };

  them("bao_chay", "BC_TT", 1, "1 bộ; số kênh ≥ số tầng");
  them("bao_chay", "BC_KHOI", S / 60, `${m2} / 60`);
  them("bao_chay", "BC_TOHOP", loi * tang, `${loi} lối × ${tang} tầng`);
  them("bao_chay", "DAY_BC", S * 0.6, `${m2} × 0,6 m/m²`);

  them("hvt", "HVT_TU", Math.max(1, Math.ceil(sTang / 500)) * tang, `max(1, ${Math.round(sTang)} m²/tầng / 500) × ${tang} tầng`);
  them("hvt", "ONG_DN50", H + 15 * tang, `${nhanH} + 15 m/tầng × ${tang} (cỡ ống chờ thiết kế)`);

  them("sprinkler", "SPR_DAU_DOWN", S / 10, `${m2} / 10`);
  them("sprinkler", "ONG_DN32", S * 0.3, `${m2} × 0,3 m/m² (cỡ ống chờ thiết kế)`);
  them("sprinkler", "SPR_VAN_BD", 1, "1 van báo động / hệ");

  const chuVi = ks.chu_vi_m ?? 0;
  them("nuoc_ngoai", "NUOC_TRU", Math.max(1, Math.ceil(chuVi / 150)), chuVi ? `chu vi ${chuVi} m / 150` : "tối thiểu 1 (chưa có chu vi)");

  them("tram_bom", "NUOC_BOM_DIEN", 1, "bơm điện chính");
  them("tram_bom", "NUOC_BOM_DIESEL", 1, "bơm dự phòng (diesel hoặc điện)");
  them("tram_bom", "NUOC_BOM_BU", 1, "bơm bù áp");
  them("tram_bom", "NUOC_TU_DK", 1, "1 tủ điều khiển");
  if (ks.be_nuoc_m3) dong("BE_NUOC", ks.be_nuoc_m3, "dung tích theo khảo sát", false);

  them("hut_khoi", "QUAT_TANG_AP", loi, `1 quạt / buồng thang (ước = số lối ${loi})`);
  them("den", "DEN_SUCO", S / 100, `${m2} / 100 (chưa có diện tích hành lang)`);
  them("den", "DEN_EXIT", loi * tang * 2, `${loi} lối × ${tang} tầng × 2`);
  them("binh", "BINH_BOT", Math.max(tang, S / 150), `max(${tang} tầng, ${m2} / 150)`);
  them("binh", "BINH_CO2", Math.max(tang, S / 150), "đi kèm bình bột (bộ 2 bình)");
  them("cap_dien", "DIEN_TU_UT", 1, "1 tủ");
  them("cap_dien", "CAP_CC", H + 20, `${nhanH} + 20 m`);
  them("chong_set", "CHONG_SET", 1, "1 hệ");
  them("ngan_chay", "CUA_CC", loi * tang, `${loi} buồng thang (ước = số lối) × ${tang} tầng`);

  if (out.length) {
    dong("HS_THIET_KE", 1, "1 gói", false);
    if (ks.dien_tham_dinh === "CÓ" || ks.dien_tham_dinh === "CẦN XÁC NHẬN") {
      dong("HS_THAM_DUYET", 1, "1 gói (thuộc diện thẩm định)", ks.dien_tham_dinh === "CẦN XÁC NHẬN");
      dong("HS_NGHIEM_THU", 1, "1 gói", ks.dien_tham_dinh === "CẦN XÁC NHẬN");
    }
  }
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

/** Lấy object khao_sat trong trả lời của Claude: khối ```json cuối cùng hợp lệ, không có thì tìm {"khao_sat": …} trần. */
export function tachJsonKhaoSat(traLoi: string): Record<string, unknown> | null {
  const thu = (s: string) => { try { const j = JSON.parse(s); return j && typeof j.khao_sat === "object" ? j.khao_sat : null; } catch { return null; } };
  const khoi = [...traLoi.matchAll(/```(?:json)?\s*([\s\S]*?)```/g)].map((m) => m[1]).reverse();
  for (const k of khoi) { const j = thu(k); if (j) return j; }
  const i = traLoi.search(/\{\s*"khao_sat"\s*:/);
  return i < 0 ? null : thu(traLoi.slice(i, traLoi.lastIndexOf("}") + 1));
}
