// Ghép khối lượng (từ plugin CAD) với danh mục vật tư và đơn giá -> bảng báo giá theo hệ.
export type VatTu = { ma: string; ten: string; dvt: string; he: string; loai: "block" | "tuyen" | "khac"; ky_hieu?: string };
export type DanhMuc = { he: Record<string, string>; vat_tu: VatTu[] };
export type KhoiLuong = {
  du_an: string; ban_ve?: string; luc?: string; dong: { ma: string; sl: number }[];
  ghi_chu?: Record<string, string>; // theo mã, vd cách ước của khối lượng sơ bộ
  khu_vuc?: { ten: string; dong: { ma: string; sl: number }[] }[]; // theo tầng/khu từ PCCC_BOC
  ban_ves?: BanVe[]; // phần của từng bản vẽ DWG; dong/khu_vuc/ban_ve ở trên là TỔNG của các phần này
};
export type BanVe = { ban_ve: string; luc?: string; dong: { ma: string; sl: number }[]; khu_vuc?: KhoiLuong["khu_vuc"] };
export type DonGia = Record<string, { vt: number | null; nc: number | null }>;

export type DongBaoGia = { ma: string; ten: string; dvt: string; sl: number; vt: number | null; nc: number | null; thanh_tien: number | null; ghi_chu: string };
export type BaoGia = {
  nhom: { he: string; ten_he: string; dong: DongBaoGia[]; cong: number }[];
  tong: number; vat: number; thue: number; tong_cong: number;
  thieu_gia: string[]; // mã có khối lượng nhưng chưa có đơn giá (tính 0 vào tổng)
};

/**
 * Đọc số kiểu Việt Nam lẫn quốc tế: "1.200" = 1200, "3,5" = 3.5, "1.200,5" = 1200.5, "350.000đ" = 350000, "3.5" = 3.5.
 * Một dấu chấm theo sau đúng 3 chữ số là phân cách hàng nghìn; một dấu phẩy luôn là thập phân. Không có số thì null.
 */
export function docSoVN(x: unknown): number | null {
  if (typeof x === "number") return Number.isFinite(x) ? x : null;
  const m = String(x ?? "").match(/-?\d[\d.,\s]*/);
  if (!m) return null;
  let t = m[0].replace(/\s/g, "").replace(/[.,]+$/, "");
  const cham = t.split(".").length - 1, phay = t.split(",").length - 1;
  if (cham && phay) {
    const thapPhan = t.lastIndexOf(".") > t.lastIndexOf(",") ? "." : ",";
    t = t.replace(thapPhan === "." ? /,/g : /\./g, "").replace(",", ".");
  } else if (cham + phay) {
    const d = cham ? "." : ",";
    // Phẩy đơn luôn là thập phân (quy ước VN: "12,375" = 12.375); chỉ chấm đơn + đúng 3 chữ số mới là nghìn.
    const nghin = cham + phay > 1 || (cham === 1 && /\.\d{3}$/.test(t) && !/^-?0\./.test(t));
    t = nghin ? t.split(d).join("") : t.replace(d, ".");
  }
  const n = Number(t);
  return Number.isFinite(n) ? n : null;
}

export function lapBaoGia(kl: KhoiLuong, dm: DanhMuc, gia: DonGia, vat = 0.1): BaoGia {
  const sl = new Map<string, number>();
  for (const d of kl.dong) if (d.sl > 0) sl.set(d.ma, (sl.get(d.ma) ?? 0) + d.sl);
  const theoMa = new Map(dm.vat_tu.map((v) => [v.ma, v]));
  const thuTu = [...dm.vat_tu.map((v) => v.ma), ...[...sl.keys()].filter((m) => !theoMa.has(m)).sort()];
  const nhom = new Map<string, BaoGia["nhom"][number]>();
  const thieu: string[] = [];
  for (const ma of thuTu) {
    const q = sl.get(ma);
    if (!q) continue;
    const v = theoMa.get(ma) ?? { ma, ten: ma + " (ngoài danh mục)", dvt: "", he: "KHAC", loai: "block" as const };
    const g = gia[ma] ?? { vt: null, nc: null };
    const coGia = g.vt != null || g.nc != null;
    if (!coGia) thieu.push(ma);
    const soLuong = v.loai === "tuyen" ? Math.round(q * 100) / 100 : q;
    const tt = coGia ? Math.round(soLuong * ((g.vt ?? 0) + (g.nc ?? 0))) : null;
    const n = nhom.get(v.he) ?? { he: v.he, ten_he: dm.he[v.he] ?? "Khác", dong: [], cong: 0 };
    n.dong.push({ ma, ten: v.ten, dvt: v.dvt, sl: soLuong, vt: g.vt, nc: g.nc, thanh_tien: tt, ghi_chu: kl.ghi_chu?.[ma] ?? "" });
    n.cong += tt ?? 0;
    nhom.set(v.he, n);
  }
  const tong = [...nhom.values()].reduce((s, n) => s + n.cong, 0);
  const thue = Math.round(tong * vat);
  return { nhom: [...nhom.values()], tong, vat, thue, tong_cong: tong + thue, thieu_gia: thieu };
}

const congDong = (ds: { ma: string; sl: number }[][]) => {
  const m = new Map<string, number>();
  for (const d of ds.flat()) m.set(d.ma, (m.get(d.ma) ?? 0) + d.sl);
  return [...m].map(([ma, sl]) => ({ ma, sl: Math.round(sl * 1000) / 1000 }));
};
const cungKhoa = (a: string, b: string) => a.toLowerCase() === b.toLowerCase();

/** Phần của từng bản vẽ; file cũ chưa có ban_ves coi là một bản vẽ. */
function cacBanVe(kl: KhoiLuong): BanVe[] {
  return kl.ban_ves ?? [{ ban_ve: kl.ban_ve ?? "", luc: kl.luc, dong: kl.dong, khu_vuc: kl.khu_vuc }];
}
/** Dựng lại tổng (dong, khu_vuc, ban_ve) từ các phần bản vẽ. */
function tongHop(du_an: string, bv: BanVe[], luc?: string): KhoiLuong {
  const khu = new Map<string, { ma: string; sl: number }[][]>();
  for (const b of bv) for (const k of b.khu_vuc ?? []) khu.set(k.ten, [...(khu.get(k.ten) ?? []), k.dong]);
  const r: KhoiLuong = { du_an, luc, ban_ve: bv.map((b) => b.ban_ve).filter(Boolean).join(", ") || undefined, dong: congDong(bv.map((b) => b.dong)), ban_ves: bv };
  if (khu.size) r.khu_vuc = [...khu].map(([ten, ds]) => ({ ten, dong: congDong(ds) }));
  return r;
}
/** PCCC_BOC gửi khối lượng một bản vẽ: thay phần cùng tên (không phân biệt hoa/thường), giữ bản vẽ khác. Không có ban_ve (plugin rất cũ): thay toàn bộ. */
export function gopBanVe(cu: KhoiLuong | null, moi: KhoiLuong): KhoiLuong {
  const phan: BanVe = { ban_ve: moi.ban_ve ?? "", luc: moi.luc, dong: moi.dong, khu_vuc: moi.khu_vuc };
  const giu = moi.ban_ve && cu ? cacBanVe(cu).filter((b) => b.ban_ve && !cungKhoa(b.ban_ve, moi.ban_ve!)) : [];
  const da = cu ? cacBanVe(cu).findIndex((b) => moi.ban_ve && cungKhoa(b.ban_ve, moi.ban_ve)) : -1;
  const ds = da >= 0 ? cacBanVe(cu!).map((b, i) => (i === da ? phan : b)) : [...giu, phan];
  return { ...tongHop(moi.du_an, ds, moi.luc), ...(moi.ghi_chu ? { ghi_chu: moi.ghi_chu } : {}) };
}
/** Bỏ phần một bản vẽ; hết bản vẽ thì null. */
export function boBanVe(kl: KhoiLuong, banVe: string): KhoiLuong | null {
  const con = cacBanVe(kl).filter((b) => !cungKhoa(b.ban_ve, banVe));
  return con.length ? tongHop(kl.du_an, con, kl.luc) : null;
}
