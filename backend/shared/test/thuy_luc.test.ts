import { describe, expect, it } from "vitest";
import { kiemBoTri, nhanDienMang, kTuGpm, thongSoNhom, tinhThuyLuc, soBoThuyLuc, toiUuThuyLuc, q1DatQmin, type SoLieuThuyLuc } from "../src/thuy_luc";

// Bảng mẫu bang_tinh/thuy_luc/MAU_THUY_LUC_7336.xlsx (repo SKILL-PCCC-NEW, 8 hàng, đã sửa công thức); số đối chiếu từ Excel người dùng 27/10.
const mau: SoLieuThuyLuc = {
  K_gpm: 8, q1: 2.88, Q_min: 60, Z: 0.15, doi_xung: "Không đối xứng với nhánh phải",
  nhanh_trai: [{ dn: 32, l: 3 }, { dn: 40, l: 2.5 }, { dn: 40, l: 1.2 }],
  nhanh_phai: [{ dn: 32, l: 2.4 }, { dn: 40, l: 2 }, { dn: 40, l: 1 }],
  ong_chinh: [{ dn: 125, l: 3.55 }, { dn: 125, l: 4 }, { dn: 125, l: 3 }, { dn: 125, l: 3 }, { dn: 100, l: 3 }, { dn: 100, l: 2.5 }, { dn: 100, l: 0.75 }],
  hong: { so_tia: 2, q_tia: 2.6, P_lang: 0.21, L_voi: 40, Kp: 0.012, L_1: 4.2, A_1: 0.001108, L_2: 20, A_2: 8.623e-5 },
  vong: [{ dn: 125, l: 85 }, { dn: 125, l: 44.3 }, { dn: 125, l: 34 }, { dn: 125, l: 4, ca: true }],
};

describe("thủy lực TCVN 7336", () => {
  it("đổi K", () => {
    expect(kTuGpm(8)).toBeCloseTo(6.078, 2);
    expect(kTuGpm(16.8)).toBeCloseTo(12.765, 2);
  });
  it("khớp bảng Excel mẫu (8 hàng)", () => {
    const kq = tinhThuyLuc(mau);
    expect(kq.hang[1].Q).toBeCloseTo(18.1 + 18.1, 0); // hàng I + II ≈ 36,2
    expect(kq.Qsp).toBeCloseTo(155.4969, 1);
    expect(kq.PJ).toBeCloseTo(1.1277, 2);
    expect(kq.PVT).toBeCloseTo(0.2429, 3);
    expect(kq.canhBao.join()).toMatch(/1 MPa/);
  });
  it("khớp bản vẽ kho lạnh PCCC-18 sau khi sửa cân bằng nhánh", () => {
    const kq = tinhThuyLuc({
      K: 12.752, q1: 5.84, Q_min: 58.45, Z: 0.067, PH: -0.04, doi_xung: false,
      nhanh_trai: [{ dn: 40, l: 3, kt: 28.7 }, { dn: 50, l: 3, kt: 110 }, { dn: 50, l: 1.5, kt: 110 }],
      nhanh_phai: [{ dn: 40, l: 3, kt: 28.7 }, { dn: 50, l: 1.5, kt: 110 }],
      ong_chinh: [{ dn: 125, l: 3, kt: 13190 }],
      vong: [65.25, 26.3, 15, 5.8, 7, 4.15, 4.1].map((l) => ({ dn: 125, l, kt: 13190 })),
    });
    expect(kq.QI).toBeCloseTo(32.62, 1);
    expect(kq.Qsp).toBeCloseTo(65.36, 1);
    expect(kq.PB).toBeCloseTo(0.5937, 3);
  });
  it("Bảng 1 / Bảng 3", () => {
    expect(thongSoNhom("3", 8).Q).toBe(60);
    expect(thongSoNhom("3", 10).Q).toBe(70);
    expect(thongSoNhom("2", 14.9).i).toBe(0.16);
  });
  it("q1 đạt Qmin", () => {
    const sl = { ...mau, ong_chinh: mau.ong_chinh.slice(0, 2) }; // 3 hàng: 54,4 l/s < 60
    expect(tinhThuyLuc(sl).dat).toBe(false);
    const q1 = q1DatQmin(sl);
    expect(q1).toBeGreaterThan(3.1);
    expect(tinhThuyLuc({ ...sl, q1 }).Qsp).toBeGreaterThanOrEqual(60);
  });
  it("sơ bộ chỉ có mặt bằng và tối ưu phương án", () => {
    const sb = soBoThuyLuc({ nhom: "2", cao_phong: 4, L_ong_chinh_den_bom: 60, Z_m: 8 });
    expect(sb.ket_qua.Qsp).toBeGreaterThanOrEqual(30);
    const pa = toiUuThuyLuc(sb.so_lieu);
    expect(pa).toHaveLength(4);
    expect(pa[0].PB).toBeLessThanOrEqual(pa[3].PB);
    for (const p of pa) expect(p.Qsp).toBeGreaterThanOrEqual(30);
  });
  it("nhận diện đối xứng, vòng/cụt", () => {
    expect(nhanDienMang({ ...mau, doi_xung: undefined }).doi_xung).toBe(false);
    expect(nhanDienMang({ ...mau, doi_xung: undefined, nhanh_phai: mau.nhanh_trai.map((d) => ({ ...d })) }).doi_xung).toBe(true);
    const vong = tinhThuyLuc(mau), cut = tinhThuyLuc({ ...mau, mang: "cut" });
    expect(cut.PJ).toBeGreaterThan(vong.PJ); // cụt: cả lưu lượng qua ống, tổn thất gấp 4 lần đoạn chia đôi
    expect(vong.nhan_dien.mang).toBe("vong");
  });
  it("bố trí đầu phun theo tập huấn C07", () => {
    expect(kiemBoTri("2", 3, 3)).toEqual([]);
    expect(kiemBoTri("4.1", 3.4, 3.4).join()).toMatch(/9 m2/);
    expect(kiemBoTri("2", 1.2, 3).join()).toMatch(/1,5 m/);
    expect(soBoThuyLuc({ nhom: "4.1", cao_phong: 6, L_ong_chinh_den_bom: 50, Z_m: 6 }).loi_bo_tri).toEqual([]);
  });
  it("nhánh một phía: Q hàng = Q nhánh, không nhân đôi", () => {
    const sl = { ...mau, nhanh_phai: undefined, doi_xung: undefined };
    const hai = tinhThuyLuc(sl), mot = tinhThuyLuc({ ...sl, mot_phia: true });
    expect(mot.QI).toBeCloseTo(hai.QI / 2, 6);
    expect(mot.nhan_dien.mot_phia).toBe(true);
  });
  it("họng: QBmin theo tối thiểu H.5/H.6 (mặc định 2,5 l/s/tia), tính toán theo lăng H.7 (TKPCCC, người dùng chốt 2026-11-01)", () => {
    const toiThieu = tinhThuyLuc({ ...mau, Q_min: 200 }); // nhánh Qmin + QBmin lớn hơn Qsp + QVT
    expect(toiThieu.Qbom).toBeCloseTo(200 + 2 * 2.5, 9);
    expect(tinhThuyLuc({ ...mau, Q_min: 200, hong: { ...mau.hong!, q_tia_h6: 5 } }).Qbom).toBeCloseTo(200 + 2 * 5, 9);
    expect(tinhThuyLuc(mau).QVT).toBeCloseTo(2 * 2.6, 9); // ống vòng, bơm, bể: số tia × lăng H.7
    expect(tinhThuyLuc({ ...mau, hong: { ...mau.hong!, q_tia_h6: 5 } }).canhBao.join()).toMatch(/H\.5\/H\.6/);
    expect(tinhThuyLuc(mau).canhBao.join()).not.toMatch(/H\.5\/H\.6/);
  });
  it("họng vách tường mạch riêng: ống vòng chỉ mang Q sprinkler, QVT cộng ở bơm", () => {
    const chung = tinhThuyLuc(mau), rieng = tinhThuyLuc({ ...mau, vt_rieng: true });
    expect(rieng.Qsp).toBeCloseTo(chung.Qsp, 9);
    expect(rieng.Qbom).toBeCloseTo(chung.Qbom, 9);
    expect(rieng.PJ).toBeLessThan(chung.PJ);
    expect(rieng.canhBao.join()).toMatch(/mạch họng/);
  });
});
