// Tính thủy lực sprinkler theo TCVN 7336:2021 Phụ lục B (mạng cụt, cân bằng nhánh, hàng, vòng, bơm, bể).
// Chép nguyên từ taelomuhicd84/SKILL-PCCC-NEW shared/src/thuy_luc.ts (commit 24743f9), cùng logic với Excel mẫu
// bang_tinh/thuy_luc/MAU_THUY_LUC_7336.xlsx của repo đó (khóa số liệu trùng o_nhap.json để xuất Excel). Sửa ở đây thì báo người dùng để đồng bộ.
// Đơn vị: Q l/s, P MPa, L m, DN mm, KT l6/s2. Ba việc: tinhThuyLuc (kiểm/tính theo bản vẽ), soBoThuyLuc (chỉ có mặt bằng), toiUuThuyLuc (so phương án).

export interface Doan { dn: number; l: number; kt?: number; ca?: boolean; nut?: string } // nut: nhãn nút theo bản vẽ (vd "1-2"), bỏ trống thì Excel tự đánh; kt: ghi đè KT (vd ống hàn điện); ca: đoạn vòng mang cả lưu lượng (không chia đôi)
export interface HongNuoc { so_tia: number; q_tia: number; q_tia_h6?: number; // q_tia: Bảng H.7 (bơm, bể); q_tia_h6: Bảng H.6 (QBmin)
  P_lang: number; L_voi: number; Kp: number; L_1?: number; A_1?: number; L_2?: number; A_2?: number }
export interface SoLieuThuyLuc {
  K_gpm?: number; K?: number; // K đầu phun: gpm/psi^0,5 hoặc l/(s·MPa^0,5) dạng q = K√P (= 10·K của B.2.2)
  q1: number; Q_min: number; Z: number; PH?: number; Q_ngoai_nha?: number;
  hang_cuoi?: { nhanh_trai: Doan[]; nhanh_phai?: Doan[] }; // hàng cuối của vùng tính chỉ lấy một phần đầu (các đầu gần nút nhất), xem chonSoDau
  hang_rieng?: { chi_so: number; nhanh_trai: Doan[]; nhanh_phai?: Doan[] }[]; // hàng có cấu trúc khác hàng I (chi_so = chỉ số đoạn ống chính dẫn tới hàng đó: 0 = hàng II), vd hàng I thiếu đầu vì vướng ram dốc
  P_van?: number; // HCB2 (tập huấn C07): tổn thất cụm van báo động/van tín hiệu/van cổng theo catalogue, MPa
  vt_rieng?: boolean; // Excel H182 "SP và VT riêng": họng vách tường mạch riêng, ống sprinkler chỉ mang Q sprinkler, QVT cộng ở bơm
  nhom?: string; // Excel H6 "Nhóm 1"…"Nhóm 7": diện tích bảo vệ 1 đầu 12 m2 (nhóm 1–3) hoặc 9 m2
  S?: number; i_min?: number; // diện tích tính toán (m2), cường độ tối thiểu (l/s.m2), dùng cho chonSoDau
  doi_xung?: boolean | string; // bỏ trống: tự nhận diện (nhanDienMang)
  mot_phia?: boolean;          // đầu phun chỉ ở một phía ống phân phối (không có nhánh phải): QI = Q nhánh trái
  mang?: "vong" | "cut";       // ống từ vùng tính toán về bơm: vòng (mỗi nửa ½ lưu lượng, B.2.12) hay cụt (cả lưu lượng); bỏ trống = vòng
  nhanh_trai: Doan[]; nhanh_phai?: Doan[]; ong_chinh: Doan[]; vong?: Doan[]; hong?: HongNuoc;
  [k: string]: unknown;
}

// TCVN 7336 Bảng B.2, đường ống thép dẫn khí và nước (d = đường kính trong). Ống khác: ghi kt vào Doan.
export const ONG_THEP: Record<number, { d: number; kt: number }> = {
  15: { d: 16.3, kt: 0.18 }, 20: { d: 21.8, kt: 0.926 }, 25: { d: 27.9, kt: 3.65 }, 32: { d: 36.7, kt: 16.5 }, 40: { d: 42, kt: 34.5 },
  50: { d: 54, kt: 135 }, 65: { d: 69.1, kt: 517 }, 80: { d: 81.5, kt: 1262 }, 90: { d: 94, kt: 2725 }, 100: { d: 106, kt: 5205 },
  125: { d: 132, kt: 16940 }, 150: { d: 157, kt: 43000 },
};
const DN = Object.keys(ONG_THEP).map(Number);

// Bảng 1 (phòng cao < 10 m) và Bảng 3 (10–20 m), chữa cháy bằng nước, nhóm 1–4.1.
// shortcut: chưa có Bảng 2 (kho nhóm 5–7) và TCVN 14496 (giá kệ > 5,5 m); thêm khi làm tính năng kho.
const BANG1: Record<string, { i: number; Q: number; S: number; t: number; L: number }> = {
  "1": { i: 0.08, Q: 10, S: 60, t: 30, L: 4 }, "2": { i: 0.12, Q: 30, S: 120, t: 60, L: 4 },
  "3": { i: 0.24, Q: 60, S: 120, t: 60, L: 4 }, "4.1": { i: 0.3, Q: 110, S: 180, t: 60, L: 4 },
};
const BANG3: Record<string, { i: number[]; Q: number[]; S: number[] }> = { // dải cao: 10–12, 12–14, 14–16, 16–18, 18–20 m
  "1": { i: [0.09, 0.1, 0.11, 0.12, 0.13], Q: [12, 14, 17, 20, 24], S: [66, 72, 78, 84, 90] },
  "2": { i: [0.13, 0.14, 0.16, 0.17, 0.18], Q: [35, 40, 50, 57, 65], S: [132, 144, 156, 168, 180] },
  "3": { i: [0.26, 0.29, 0.31, 0.34, 0.36], Q: [70, 85, 95, 115, 130], S: [132, 144, 156, 168, 180] },
  "4.1": { i: [0.33, 0.36, 0.39, 0.42, 0.45], Q: [130, 155, 180, 215, 240], S: [198, 216, 230, 252, 270] },
};
export function thongSoNhom(nhom: string, caoPhong: number) {
  const b1 = BANG1[nhom];
  if (!b1) throw new Error(`Nhóm ${nhom} chưa có trong bảng (chỉ 1, 2, 3, 4.1)`);
  if (caoPhong < 10) return { ...b1, nguon: "TCVN 7336 Bảng 1" };
  if (caoPhong > 20) throw new Error("Phòng cao trên 20 m: ngoài phạm vi Bảng 3");
  const k = Math.min(4, Math.max(0, Math.ceil((caoPhong - 10) / 2) - 1));
  const b3 = BANG3[nhom];
  return { i: b3.i[k], Q: b3.Q[k], S: b3.S[k], t: b1.t, L: b1.L, nguon: "TCVN 7336 Bảng 3" };
}

/** Bố trí đầu phun (TCVN 7336 Bảng 1, 5.2.22; tài liệu tập huấn C07): a <= amax, a >= 1,5 m, Ω = an x ad <= 12 m2 (nhóm 1–3), <= 9 m2 (nhóm 4–7). */
export const omegaMax = (nhom: string) => (parseFloat(nhom) < 4 ? 12 : 9);
export function kiemBoTri(nhom: string, an: number, ad: number, amax = BANG1[nhom]?.L ?? 3) {
  const loi: string[] = [];
  if (Math.max(an, ad) > amax) loi.push(`Khoảng cách đầu phun ${Math.max(an, ad)} m > amax ${amax} m (Bảng 1)`);
  if (Math.min(an, ad) < 1.5) loi.push(`Khoảng cách đầu phun ${Math.min(an, ad)} m < 1,5 m (5.2.22)`);
  if (an * ad > omegaMax(nhom) + 1e-9) loi.push(`Ω = ${(an * ad).toFixed(2)} m2 > ${omegaMax(nhom)} m2 cho nhóm ${nhom}`);
  return loi;
}

export const kTuGpm = (k: number) => (k * 0.0630902) / Math.sqrt(0.00689476);
const kt = (d: Doan) => {
  const v = d.kt ?? ONG_THEP[d.dn]?.kt;
  if (!v) throw new Error(`Không có KT cho DN${d.dn}: ghi kt vào đoạn ống`);
  return v;
};
export const tonThat = (Q: number, d: Doan) => (Q * Q * d.l) / (100 * kt(d)); // B.2.5
export const vanToc = (Q: number, dn: number) => (4 * Q) / 1000 / (Math.PI * ((ONG_THEP[dn]?.d ?? dn) / 1000) ** 2);

/** Nhánh cụt: đầu 1 áp P1, đoạn i nối đầu i với đầu i+1 (đoạn cuối tới nút). Trả áp tại nút, lưu lượng nhánh. */
export function nhanh(K: number, P1: number, doan: Doan[]) {
  let P = P1, Q = 0;
  const q: number[] = [];
  for (const d of doan) { const qi = K * Math.sqrt(P); q.push(qi); Q += qi; P += tonThat(Q, d); }
  return { P, Q, q };
}

const giongNhau = (a: Doan[], b: Doan[]) => a.length === b.length && a.every((d, i) => d.dn === b[i].dn && Math.abs(d.l - b[i].l) < 0.05 && d.kt === b[i].kt);
/** Nhận diện loại tính: nhánh đối xứng hay không (B.2.10 / B.2.11), ống về bơm mạch vòng hay cụt (B.2.12). Số liệu ghi rõ thì theo số liệu. */
export function nhanDienMang(sl: SoLieuThuyLuc) {
  const ly_do: string[] = [];
  let doi_xung: boolean;
  if (sl.mot_phia) return { doi_xung: false, mot_phia: true, mang: sl.mang ?? "vong", ly_do: ["nhánh một phía: đầu phun chỉ ở một bên ống phân phối, Q hàng = Q nhánh", sl.mang ? `mạng: theo số liệu (${sl.mang === "cut" ? "mạch cụt" : "mạch vòng"})` : "mạng: không ghi, mặc định mạch vòng như Excel mẫu"] };
  if (typeof sl.doi_xung === "boolean") { doi_xung = sl.doi_xung; ly_do.push("đối xứng: theo số liệu ghi"); }
  else if (typeof sl.doi_xung === "string") { doi_xung = /^đối xứng/i.test(sl.doi_xung.trim()); ly_do.push(`đối xứng: theo ô "${sl.doi_xung}"`); }
  else if (!sl.nhanh_phai?.length) { doi_xung = true; ly_do.push("đối xứng: chỉ có nhánh trái, coi nhánh phải giống hệt"); }
  else { doi_xung = giongNhau(sl.nhanh_trai, sl.nhanh_phai); ly_do.push(doi_xung ? "đối xứng: hai nhánh cùng số đầu, cùng DN và chiều dài" : "không đối xứng: hai nhánh khác số đầu, DN hoặc chiều dài"); }
  const mang = sl.mang ?? "vong";
  ly_do.push(sl.mang ? `mạng: theo số liệu (${sl.mang === "cut" ? "mạch cụt" : "mạch vòng"})` : "mạng: không ghi, mặc định mạch vòng như Excel mẫu");
  return { doi_xung, mot_phia: false, mang, ly_do };
}

/** Một hàng (nhánh qua nút ống chính): áp tại nút PA, lưu lượng QI, đặc tính Bp = QI²/PA (B.2.10.9, B.2.11.3). */
function hangNhanh(K: number, P1: number, trai: Doan[], phai: Doan[] | undefined, mot_phia: boolean, doi_xung: boolean) {
  const t = nhanh(K, P1, trai);
  let PA = t.P, QI = mot_phia ? t.Q : 2 * t.Q;
  if (!doi_xung && !mot_phia) { // nhánh áp thấp hơn được điều chỉnh về áp lớn hơn tại nút
    const ph = nhanh(K, P1, phai!);
    PA = Math.max(t.P, ph.P);
    QI = t.Q * Math.sqrt(PA / t.P) + ph.Q * Math.sqrt(PA / ph.P);
  }
  return { PA, QI, Bp: QI ** 2 / PA, trai: t };
}

export function tinhThuyLuc(sl: SoLieuThuyLuc) {
  const K = sl.K ?? kTuGpm(sl.K_gpm ?? NaN);
  if (!(K > 0)) throw new Error("Thiếu K hoặc K_gpm");
  const P1 = (sl.q1 / K) ** 2;
  const nd = nhanDienMang(sl);
  const h0 = hangNhanh(K, P1, sl.nhanh_trai, sl.nhanh_phai, nd.mot_phia, nd.doi_xung);
  const { trai, PA, QI, Bp } = h0;
  const bpHang = (c: { nhanh_trai: Doan[]; nhanh_phai?: Doan[] }, mot: boolean) => hangNhanh(K, P1, c.nhanh_trai, c.nhanh_phai, mot, !mot && giongNhau(c.nhanh_trai, c.nhanh_phai!)).Bp;
  const bpRieng = new Map((sl.hang_rieng ?? []).map((c) => [c.chi_so, bpHang(c, !c.nhanh_phai?.length)] as const)); // Bp không phụ thuộc áp khởi đầu
  const cuoi = sl.ong_chinh.length - 1;
  if (sl.hang_cuoi) bpRieng.set(cuoi, bpHang(sl.hang_cuoi, nd.mot_phia || !sl.hang_cuoi.nhanh_phai?.length)); // hàng cuối thiếu đầu có đặc tính riêng
  const Bc = bpRieng.get(cuoi) ?? Bp;
  let Q = QI, P = PA;
  const hang = [{ Q: QI, P: PA }];
  sl.ong_chinh.forEach((d, i) => { P += tonThat(Q, d); Q += Math.sqrt((bpRieng.get(i) ?? Bp) * P); hang.push({ Q, P }); }); // B.2.10.10–13
  const Qsp = Q, Pvung = P;
  const h = sl.hong && hongMacDinh(sl.hong); // L_voi hoặc Kp bằng 0 thì lấy mặc định (20 m, 0,012) thay vì tổn thất cuộn vòi = 0
  const QVT = h ? h.so_tia * h.q_tia : 0;
  const PVT = h ? h.P_lang + (h.L_voi * h.q_tia ** 2 * h.Kp + ((h.A_1 ?? 0) * (h.L_1 ?? 0) + (h.A_2 ?? 0) * (h.L_2 ?? 0)) * h.q_tia ** 2) / 100 : 0;
  // Vòng/ống tới bơm: lưu lượng (sprinkler + họng) chia đôi mỗi nửa vòng (B.2.12); họng là nhánh song song: áp nút lấy max, không cộng.
  const Qcap = Qsp + (sl.vt_rieng ? 0 : QVT);
  P = Pvung;
  const caDoan = (d: Doan) => sl.mang === "cut" || !!d.ca;
  (sl.vong ?? []).forEach((d, i) => { P += tonThat(caDoan(d) ? Qcap : Qcap / 2, d); if (i === 0 && !sl.vt_rieng) P = Math.max(P, PVT); });
  if (!sl.vong?.length && !sl.vt_rieng) P = Math.max(P, PVT);
  const PJ = P;
  const PB = PJ + 0.2 * (PJ - P1) + (sl.P_van ?? 0) + sl.Z - (sl.PH ?? 0); // B.3.9, cục bộ 20% tổn thất dọc đường (B.3.13), HCB2 cụm van (C07)
  const QHR6 = h ? h.so_tia * (h.q_tia_h6 ?? h.q_tia) : 0; // QBmin theo H.6 (Excel H18)
  const Qbom = Math.max(Qsp + QVT, sl.Q_min + QHR6 + (sl.Q_ngoai_nha ?? 0));
  const doan = [...sl.nhanh_trai.map((d, i) => ({ ten: `trái ${i + 1}`, d, Q: trai.q.slice(0, i + 1).reduce((a, b) => a + b, 0) })),
    ...sl.ong_chinh.map((d, i) => ({ ten: `ống chính ${i + 1}`, d, Q: hang[i].Q })),
    ...(sl.vong ?? []).map((d, i) => ({ ten: `vòng ${i + 1}`, d, Q: caDoan(d) ? Qcap : Qcap / 2 }))];
  const vMax = Math.max(...doan.map((x) => vanToc(x.Q, x.d.dn)));
  const canhBao: string[] = [];
  if (Qsp < sl.Q_min) canhBao.push(`KHÔNG ĐẠT: Q sprinkler ${Qsp.toFixed(2)} < Qmin ${sl.Q_min} (B.3.1)`);
  if (sl.vt_rieng && QVT) canhBao.push("Mạch họng vách tường riêng: kiểm riêng cột áp bơm cho mạch họng (PVT + tổn thất mạch họng + Z)");
  if (sl.P_van === undefined) canhBao.push("Chưa cộng tổn thất cụm van báo động HCB2 (catalogue van, tập huấn C07): ghi P_van");
  if (vMax > 10) canhBao.push(`Vận tốc ${vMax.toFixed(2)} m/s > 10 m/s (B.1.9)`);
  if (PB > 1) canhBao.push(`PB ${PB.toFixed(3)} MPa > 1 MPa: kiểm áp tại bộ điều khiển (B.3.14)`);
  if (P1 < 0.05) canhBao.push(`P1 ${P1.toFixed(3)} MPa < 0,05 MPa (Hmin 5 m.c.n theo catalogue): kiểm áp tối thiểu của hãng`);
  if (P1 > 1) canhBao.push(`P1 ${P1.toFixed(3)} MPa > 1 MPa (Hmax 100 m.c.n, 5.1.4)`);
  return {
    K, P1, QI, PA, Bp, Bc, hang, Qsp, Pvung, QVT, PVT, PJ, PB, PB_m: PB * 101.972, Qbom,
    // điểm kiểm đường đặc tính bơm (tập huấn C07): tại 150% Qtk cột áp >= 65% Htk; tại Q = 0 cột áp <= 140% Htk
    bom_kiem: { Q_150_m3h: Qbom * 3.6 * 1.5, H_toi_thieu_tai_Q150_m: PB * 101.972 * 0.65, H_toi_da_tai_Q0_m: PB * 101.972 * 1.4 },
    be_m3: Qbom * 3.6 + (sl.Q_ngoai_nha ?? 0) * 2 * 3.6, // như mẫu Excel: 60 phút + ngoài nhà
    vMax, dat: Qsp >= sl.Q_min && vMax <= 10, canhBao, nhan_dien: nhanDienMang(sl),
  };
}

/** q1 nhỏ nhất (không dưới q1 đã cho) để Q sprinkler >= Qmin; tăng q1 = tăng áp đầu chủ đạo. */
export function q1DatQmin(sl: SoLieuThuyLuc) {
  const f = (q1: number) => tinhThuyLuc({ ...sl, q1 }).Qsp - sl.Q_min;
  if (f(sl.q1) >= 0) return sl.q1;
  let a = sl.q1, b = sl.q1 * 2;
  while (f(b) < 0) b *= 2;
  for (let i = 0; i < 50; i++) { const m = (a + b) / 2; f(m) >= 0 ? (b = m) : (a = m); }
  return Math.ceil(b * 100) / 100;
}

const lenDn = (dn: number) => DN.find((x) => x > dn) ?? dn;
/** TCVN 7336 Bảng B.3 (áp đến 0,5 MPa): DN theo số đầu phun đoạn ống mang. */
export const dnTheoSoDau = (n: number) => (n <= 2 ? 25 : n <= 3 ? 32 : n <= 5 ? 40 : n <= 10 ? 50 : n <= 20 ? 65 : n <= 36 ? 80 : n <= 75 ? 100 : n <= 140 ? 125 : 150);

/** Cuộn vòi họng vách tường (người dùng dặn 2026-10-29): bản vẽ thiết kế có bán kính phục vụ của tủ vách tường thì lấy số đó làm chiều dài cuộn vòi; chỉ có mặt bằng chưa thiết kế PCCC (làm sơ bộ) thì tự chọn 20 m hoặc 40 m cho thuận tiện. Kp mặc định 0,012 (vòi DN50, TCVN 4513-1988 mục 6.19, như mẫu Excel); L_voi hoặc Kp = 0 là thiếu số liệu, không để 0. */
export const hongMacDinh = (h: HongNuoc, banKinhTu?: number): HongNuoc => ({ ...h, L_voi: h.L_voi > 0 ? h.L_voi : banKinhTu ?? 20, Kp: h.Kp > 0 ? h.Kp : 0.012 });

export interface MatBang {
  nhom: string; cao_phong: number; K_gpm?: number; khoang_cach?: number; dau_moi_nhanh?: number;
  L_ong_chinh_den_bom: number; Z_m: number; PH?: number; hong?: HongNuoc; vong?: boolean;
}
/** Phương án sơ bộ khi chỉ có mặt bằng: tự bố trí lưới đầu phun, chọn DN theo B.3, tìm q1 đạt Qmin. Mọi số là ước lượng. */
export function soBoThuyLuc(mb: MatBang) {
  const ts = thongSoNhom(mb.nhom, mb.cao_phong);
  const a = mb.khoang_cach ?? Math.min(ts.L, Math.floor(Math.sqrt(omegaMax(mb.nhom)) * 10) / 10);
  const loiBoTri = kiemBoTri(mb.nhom, a, a, ts.L);
  const om = a * a;
  const qe = 1.15 * ts.i * om; // lưu lượng trung bình mỗi đầu ước lượng (đầu sau áp cao hơn đầu chủ đạo)
  const n = Math.max(Math.ceil(ts.S / om - 1e-9), Math.ceil(ts.Q / qe)); // đủ diện tích S và đủ Qmin
  const m = mb.dau_moi_nhanh ?? 3;
  const soHang = Math.ceil(n / (2 * m));
  // DN: không nhỏ hơn Bảng B.3 và giữ vận tốc ước lượng <= 6 m/s ở nhánh, <= 4 m/s ở ống chính
  const dnV = (Q: number, v: number) => DN.find((d) => vanToc(Q, d) <= v) ?? 150;
  const nhanhDoan: Doan[] = Array.from({ length: m }, (_, i) => ({ dn: Math.max(dnTheoSoDau(i + 1), dnV((i + 1) * qe, 6)), l: i === m - 1 ? a / 2 : a }));
  const ong: Doan[] = Array.from({ length: soHang - 1 }, (_, r) => ({ dn: Math.max(65, dnTheoSoDau(2 * m * (r + 1)), dnV(2 * m * (r + 1) * qe, 4)), l: a }));
  const Qc = ts.Q + (mb.hong ? mb.hong.so_tia * mb.hong.q_tia : 0);
  const dnChinh = Math.max(100, dnTheoSoDau(2 * m * soHang), dnV(mb.vong ? Qc / 2 : Qc, 4));
  const vong: Doan[] = mb.vong
    ? [{ dn: dnChinh, l: mb.L_ong_chinh_den_bom }]
    : [{ dn: dnChinh, l: mb.L_ong_chinh_den_bom, ca: true }];
  const sl: SoLieuThuyLuc = {
    K_gpm: mb.K_gpm ?? 8, q1: +(ts.i * om).toFixed(3), Q_min: ts.Q, Z: mb.Z_m / 100, PH: mb.PH,
    doi_xung: "Đối xứng với nhánh phải", nhanh_trai: nhanhDoan, ong_chinh: ong, vong, hong: mb.hong && hongMacDinh(mb.hong),
    S: ts.S, i_min: ts.i, n: 2 * m * soHang, nhom: `Nhóm ${mb.nhom}`,
    mo_ta: `Sơ bộ: nhóm nguy cơ phát sinh cháy ${mb.nhom} (TCVN 7336 Phụ lục A), phòng cao ${mb.cao_phong} m; số liệu ước lượng, chờ bản vẽ`,
  };
  sl.q1 = q1DatQmin(sl); // vận tốc > 10 m/s thì tinhThuyLuc cảnh báo; dùng toiUuThuyLuc để nâng DN
  return {
    so_lieu: sl, ket_qua: tinhThuyLuc(sl), loi_bo_tri: loiBoTri,
    gia_dinh: [`${ts.nguon}: i = ${ts.i} l/s.m2, Qmin = ${ts.Q} l/s, S = ${ts.S} m2, thời gian ${ts.t} phút`,
      `Lưới đầu phun ${a} x ${a} m (Ω = ${om.toFixed(2)} m2, tối đa ${ts.L} m), n = max(S/Ω, Qmin/q tb) = ${n}, bố trí ${soHang} hàng x ${2 * m} đầu (đối xứng ${m} đầu mỗi nhánh)`,
      "DN không nhỏ hơn Bảng B.3 TCVN 7336 và giữ vận tốc ước lượng <= 6 m/s (nhánh), <= 4 m/s (ống chính); ống thép dẫn khí và nước (Bảng B.2)",
      `Ống chính từ vùng tính toán về bơm ước ${mb.L_ong_chinh_den_bom} m${mb.vong ? " (mạng vòng, chia đôi)" : ""}, Z = ${mb.Z_m} m`,
      "Sơ bộ: vị trí, chiều dài thật lấy theo bản vẽ khi có"],
  };
}

/** So phương án cho cùng một bố trí: (A) giữ ống, tăng q1 tới Qmin; (B) nâng DN ống chính/vòng 1 cấp; (C) nâng DN nhánh 1 cấp. Xếp theo cột áp bơm. */
export function toiUuThuyLuc(sl: SoLieuThuyLuc) {
  const len = (ds?: Doan[]) => ds?.map((d) => (d.kt ? d : { ...d, dn: lenDn(d.dn) }));
  const pa = [
    { mo_ta: "Giữ nguyên ống", sl: { ...sl } },
    { mo_ta: "Nâng ống chính và vòng 1 cấp DN", sl: { ...sl, ong_chinh: len(sl.ong_chinh)!, vong: len(sl.vong) } },
    { mo_ta: "Nâng ống nhánh 1 cấp DN", sl: { ...sl, nhanh_trai: len(sl.nhanh_trai)!, nhanh_phai: len(sl.nhanh_phai) } },
    { mo_ta: "Nâng cả nhánh, ống chính, vòng 1 cấp DN", sl: { ...sl, nhanh_trai: len(sl.nhanh_trai)!, nhanh_phai: len(sl.nhanh_phai), ong_chinh: len(sl.ong_chinh)!, vong: len(sl.vong) } },
  ];
  return pa.map((p) => {
    const q1 = q1DatQmin(p.sl as SoLieuThuyLuc);
    const so_lieu = { ...p.sl, q1 } as SoLieuThuyLuc;
    const kq = tinhThuyLuc(so_lieu);
    return { mo_ta: p.mo_ta, q1, P1: kq.P1, Qsp: kq.Qsp, PB: kq.PB, Qbom: kq.Qbom, be_m3: kq.be_m3, vMax: kq.vMax, canhBao: kq.canhBao, so_lieu };
  }).sort((x, y) => x.PB - y.PB);
}

/** Chọn số đầu vùng tính theo tập huấn C07: n >= S/Ω; hàng đầy đủ ở đầu xa, hàng cuối chỉ lấy các đầu gần nút nhất; tăng n tới khi Q sprinkler >= Qmin.
 * sl: nhanh_trai/nhanh_phai là NHÁNH ĐẦY ĐỦ, ong_chinh[0] là đoạn ống chính giữa hai hàng; q1 mặc định Ω·i. */
export function chonSoDau(sl: SoLieuThuyLuc, soHangToiDa = 8) {
  const om = /nhóm\s*[123](?!\d)/i.test(sl.nhom ?? "") ? 12 : 9, q1 = sl.q1 ?? om * sl.i_min!;
  const nMin = Math.ceil(sl.S! / om - 1e-9);
  const A = sl.nhanh_trai, B = sl.mot_phia ? [] : sl.nhanh_phai?.length ? sl.nhanh_phai : A;
  const khoang = (L: Doan[], j: number) => L.slice(j).reduce((t, d) => t + d.l, 0);
  const dau = [...A.map((_, j) => ({ ben: "A", d: khoang(A, j) })), ...B.map((_, j) => ({ ben: "B", d: khoang(B, j) }))].sort((x, y) => x.d - y.d);
  const moiHang = A.length + B.length;
  const kq: { n: number; hang_day_du: number; dau_hang_cuoi: number; Qsp: number; Qbom: number; PB_m: number; vMax: number; dat: boolean; so_lieu: SoLieuThuyLuc }[] = [];
  for (let n = nMin; n <= soHangToiDa * moiHang; n++) {
    const day = Math.floor(n / moiHang), r = n % moiHang, hang = day + (r ? 1 : 0);
    if (hang < 1) continue;
    const gan = dau.slice(0, r), nA = gan.filter((x) => x.ben === "A").length, nB = r - nA;
    const phanA = A.slice(A.length - nA), phanB = B.slice(B.length - nB);
    const hang_cuoi = r ? (nA ? { nhanh_trai: phanA, nhanh_phai: nB ? phanB : undefined } : { nhanh_trai: phanB }) : undefined;
    const x = { ...sl, q1, n, ong_chinh: Array.from({ length: hang - 1 }, () => ({ ...sl.ong_chinh[0] })), hang_cuoi };
    const k = tinhThuyLuc(x);
    kq.push({ n, hang_day_du: day, dau_hang_cuoi: r, Qsp: k.Qsp, Qbom: k.Qbom, PB_m: k.PB_m, vMax: k.vMax, dat: k.dat, so_lieu: x });
  }
  return { omega: om, n_min: nMin, q1, chon: kq.find((x) => x.dat), phuong_an: kq };
}
