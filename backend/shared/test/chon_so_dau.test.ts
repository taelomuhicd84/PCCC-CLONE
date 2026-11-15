import { describe, it, expect } from "vitest";
import { tinhThuyLuc, chonSoDau, type SoLieuThuyLuc } from "../src/thuy_luc";

// Vùng tính kho nhóm 5: nhánh 6 đầu (4 + 2) cắt ngang ống chính, ống chính 3 m, q1 = Ω·i.
const goc: SoLieuThuyLuc = {
  nhom: "Nhóm 5", S: 90, i_min: 0.16, n: 10, K_gpm: 8, q1: 1.44, Q_min: 30, Z: 0.08,
  nhanh_trai: [{ dn: 25, l: 3 }, { dn: 32, l: 3 }, { dn: 32, l: 3 }, { dn: 32, l: 2.43 }],
  nhanh_phai: [{ dn: 25, l: 3 }, { dn: 25, l: 0.57 }],
  ong_chinh: [{ dn: 125, l: 3 }], vong: [{ dn: 125, l: 77.2 }, { dn: 125, l: 91, ca: true }],
};
describe("chọn số đầu vùng tính", () => {
  it("hàng cuối lấy đủ đầu thì bằng hàng đầy đủ", () => {
    const hai = tinhThuyLuc({ ...goc, ong_chinh: [{ dn: 125, l: 3 }] });
    const cuoi = tinhThuyLuc({ ...goc, ong_chinh: [{ dn: 125, l: 3 }], hang_cuoi: { nhanh_trai: goc.nhanh_trai, nhanh_phai: goc.nhanh_phai } });
    expect(cuoi.Qsp).toBeCloseTo(hai.Qsp, 9);
  });
  it("hàng cuối ít đầu: Q nằm giữa 1 hàng và 2 hàng đầy đủ", () => {
    const mot = tinhThuyLuc({ ...goc, ong_chinh: [] }), hai = tinhThuyLuc(goc);
    const ba = tinhThuyLuc({ ...goc, hang_cuoi: { nhanh_trai: goc.nhanh_trai.slice(2), nhanh_phai: goc.nhanh_phai.slice(1) } });
    expect(ba.Qsp).toBeGreaterThan(mot.Qsp);
    expect(ba.Qsp).toBeLessThan(hai.Qsp);
  });
  it("chọn n >= S/Ω và Q sprinkler >= Qmin", () => {
    const r = chonSoDau(goc);
    expect(r.n_min).toBe(10);
    expect(r.chon!.n).toBeGreaterThanOrEqual(10);
    expect(r.chon!.Qsp).toBeGreaterThanOrEqual(30);
    expect(r.phuong_an.filter((p) => p.n < r.chon!.n).every((p) => !p.dat)).toBe(true);
  });
  it("hang_rieng: hàng II cấu trúc như hàng I cho cùng kết quả; hàng II ít đầu cho Q nhỏ hơn", () => {
    const hai = tinhThuyLuc(goc);
    const giong = tinhThuyLuc({ ...goc, hang_rieng: [{ chi_so: 0, nhanh_trai: goc.nhanh_trai, nhanh_phai: goc.nhanh_phai }] });
    expect(giong.Qsp).toBeCloseTo(hai.Qsp, 6);
    const it = tinhThuyLuc({ ...goc, hang_rieng: [{ chi_so: 0, nhanh_trai: goc.nhanh_trai.slice(2) }] });
    expect(it.Qsp).toBeLessThan(hai.Qsp);
  });
});
