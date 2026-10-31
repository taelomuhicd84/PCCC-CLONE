// Ghép khối lượng (từ plugin CAD) với danh mục vật tư và đơn giá -> bảng báo giá theo hệ.
export type VatTu = { ma: string; ten: string; dvt: string; he: string; loai: "block" | "tuyen" | "khac"; ky_hieu?: string };
export type DanhMuc = { he: Record<string, string>; vat_tu: VatTu[] };
export type KhoiLuong = {
  du_an: string; ban_ve?: string; luc?: string; dong: { ma: string; sl: number }[];
  ghi_chu?: Record<string, string>; // theo mã, vd cách ước của khối lượng sơ bộ
  khu_vuc?: { ten: string; dong: { ma: string; sl: number }[] }[]; // theo tầng/khu từ PCCC_BOC
};
export type DonGia = Record<string, { vt: number | null; nc: number | null }>;

export type DongBaoGia = { ma: string; ten: string; dvt: string; sl: number; vt: number | null; nc: number | null; thanh_tien: number | null; ghi_chu: string };
export type BaoGia = {
  nhom: { he: string; ten_he: string; dong: DongBaoGia[]; cong: number }[];
  tong: number; vat: number; thue: number; tong_cong: number;
  thieu_gia: string[]; // mã có khối lượng nhưng chưa có đơn giá (tính 0 vào tổng)
};

/**
 * Đọc số kiểu Việt Nam lẫn quốc tế: "1.200" = 1200, "3,5" = 3.5, "1.200,5" = 1200.5, "350.000đ" = 350000, "3.5" = 3.5.
 * Một dấu chấm/phẩy theo sau đúng 3 chữ số được coi là phân cách hàng nghìn. Không có số thì null.
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
    const nghin = cham + phay > 1 || (new RegExp(`\\${d}\\d{3}$`).test(t) && !/^-?0[.,]/.test(t));
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
