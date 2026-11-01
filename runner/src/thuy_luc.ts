// Thủy lực TCVN 7336 trên web: chuẩn hoá ô sơ bộ, xuất Excel mẫu bang_tinh/thuy_luc/MAU_THUY_LUC_7336.xlsx qua
// scripts/xuat_thuy_luc_excel.py (ghi thẳng ô của mẫu, giữ công thức; Excel tự tính khi mở; bước hoàn thiện dùng Excel COM).
// Phần đổi số liệu sang khóa o_nhap.json chép từ SKILL-PCCC-NEW scripts/thuy_luc.ts (--excel); TKPCCC thêm: chặn vượt khung mẫu,
// đổi K -> K_gpm, báo khóa không có ô trong mẫu thay vì để script dừng.
import { spawn } from "node:child_process";
import { readFileSync, writeFileSync } from "node:fs";
import path from "node:path";
import { docSoVN } from "../../shared/src/bao_gia";
import { nhanDienMang, ONG_THEP, type Doan, type MatBang, type SoLieuThuyLuc } from "../../shared/src/thuy_luc";
import { HUONG_DAN_PYTHON } from "./thamdinh";

const NHOM = ["1", "2", "3", "4.1"]; // Bảng 1/3 đã có trong lõi; kho nhóm 5–7 (Bảng 2) chưa có

/** Ô sơ bộ trên web -> MatBang. Số gõ kiểu Việt Nam ("3,5"). Họng vách tường chỉ có khi nhập số tia. */
export function chuanHoaMatBang(x: Record<string, unknown>): MatBang {
  const nhom = String(x?.nhom ?? "").trim();
  if (!NHOM.includes(nhom)) throw new Error("Nhóm nguy cơ phải là 1, 2, 3 hoặc 4.1 (TCVN 7336 Phụ lục A); kho nhóm 5–7 chưa có");
  const tuy = (v: unknown) => docSoVN(v) ?? undefined;
  const can = (v: unknown, ten: string) => { const n = docSoVN(v); if (n === null) throw new Error("Thiếu " + ten); return n; };
  const h = (x?.hong ?? {}) as Record<string, unknown>;
  const soTia = tuy(h.so_tia);
  return {
    nhom, cao_phong: can(x.cao_phong, "chiều cao phòng"), K_gpm: tuy(x.K_gpm), khoang_cach: tuy(x.khoang_cach),
    dau_moi_nhanh: tuy(x.dau_moi_nhanh), L_ong_chinh_den_bom: can(x.L_ong_chinh_den_bom, "chiều dài ống chính về bơm"),
    Z_m: can(x.Z_m, "chênh cao Z"), PH: tuy(x.PH), vong: !!x.vong,
    hong: soTia ? { so_tia: soTia, q_tia: can(h.q_tia, "lưu lượng 1 tia (QCVN 10 Bảng H.7)"), P_lang: can(h.P_lang, "áp lăng phun (QCVN 10 Bảng H.7)"),
      L_voi: tuy(h.L_voi) ?? 0, Kp: tuy(h.Kp) ?? 0 } : undefined, // 0 = lấy mặc định 20 m, 0,012 (hongMacDinh)
  };
}

// Excel mẫu có đúng 4 dòng vòng nối cứng: 3 dòng chia đôi lưu lượng rồi 1 dòng cả lưu lượng (F-J). Dòng thiếu ghi L = 0.
// Thừa đoạn thì quy về chiều dài tương đương cùng DN (cùng Q nên ΔP = Q²·L/(100·KT) không đổi: L' = L·KT(DN')/KT(DN)).
function vongMau(vong: Doan[], ghiChu: string[]) {
  const kt = (d: { dn: number; kt?: number }) => d.kt ?? ONG_THEP[d.dn].kt;
  const quy = (ds: Doan[]) => (ds.length ? [{ dn: ds[0].dn, l: +ds.reduce((t, d) => t + (d.l * kt(ds[0])) / kt(d), 0).toFixed(2) }] : []);
  const tho = vong.filter((d) => !d.ca).map(({ dn, l, kt }) => ({ dn, l, kt }));
  const nua = tho.length <= 3 ? tho : quy(tho), ca = vong.filter((d) => d.ca);
  const caMau = ca.length > 1 ? quy([...ca].sort((x, y) => kt(y) - kt(x))) : ca;
  if (ca.length > 1) ghiChu.push(`${ca.length} đoạn cả lưu lượng gộp thành DN${caMau[0].dn} dài tương đương ${caMau[0].l} m trong Excel (cùng tổn thất).`);
  if (tho.length > 3) ghiChu.push(`${tho.length} đoạn chia đôi gộp thành DN${nua[0].dn} dài tương đương ${nua[0].l} m trong Excel.`);
  const dn0 = nua[0]?.dn ?? caMau[0]?.dn ?? 125;
  return [0, 1, 2].map((i) => (nua[i] ? { dn: nua[i].dn, l: nua[i].l } : { dn: dn0, l: 0 })).concat(caMau[0] ? { dn: caMau[0].dn, l: caMau[0].l } : { dn: dn0, l: 0 });
}

const TOI_DA: Record<string, number> = { nhanh_trai: 5, nhanh_phai: 4, ong_chinh: 7 }; // số dòng của mẫu (o_nhap.json)
const sach = (ds?: Doan[]) => ds?.map(({ dn, l, nut }) => (nut ? { dn, l, nut } : { dn, l }));

/** Số liệu tính -> số liệu theo khóa o_nhap.json của Excel mẫu (`khoa`: khóa có ô trong mẫu), kèm ghi chú phần Excel không thể hiện được. */
export function soLieuExcel(soLieu: SoLieuThuyLuc, khoa: string[]): { json: Record<string, unknown>; ghi_chu: string[] } {
  for (const [k, n] of Object.entries(TOI_DA)) {
    const ds = soLieu[k] as Doan[] | undefined;
    if ((ds?.length ?? 0) > n) throw new Error(`Excel mẫu chỉ có ${n} dòng ${k}, số liệu có ${ds!.length}: tính trên web, chưa xuất Excel được`);
  }
  const ghi_chu: string[] = [];
  const { K, PH, ...x } = soLieu;
  const json: Record<string, unknown> = { ...x, nhanh_trai: sach(x.nhanh_trai), nhanh_phai: sach(x.nhanh_phai), ong_chinh: sach(x.ong_chinh), vong: vongMau(x.vong ?? [], ghi_chu) };
  if (K) { // lõi tính theo K khi có, nên Excel cũng phải dùng đúng K đó
    json.K_gpm = +((K * Math.sqrt(0.00689476)) / 0.0630902).toFixed(3);
    ghi_chu.push(`K = ${K} l/(s·MPa^0,5) đổi thành K = ${json.K_gpm} gpm/psi^0,5 (ô H14).`);
  }
  if (PH) ghi_chu.push(`Excel mẫu chưa có ô áp lực đầu vào PH = ${PH} MPa: cột áp bơm trong Excel chưa trừ PH.`);
  if ([...x.nhanh_trai, ...(x.nhanh_phai ?? []), ...x.ong_chinh, ...(x.vong ?? [])].some((d) => d.kt)) {
    ghi_chu.push("Có đoạn ghi KT riêng: Excel mẫu tra KT theo DN (TCVN 7336 Bảng B.2), số có thể lệch kết quả trên web.");
  }
  const nd = nhanDienMang(soLieu);
  json.doi_xung = nd.doi_xung && !nd.mot_phia ? "Đối xứng với nhánh phải" : "Không đối xứng với nhánh phải";
  json.so_nhanh = nd.mot_phia ? "1 nhánh" : "2 nhánh";
  if (nd.mot_phia) json.nhanh_phai = [];
  // Đối xứng mà không ghi nhánh phải: chép nhánh trái sang, nếu không bảng nhánh phải (dòng 77–91) giữ số cũ của file mẫu.
  // Mẫu chỉ có 4 dòng nhánh phải: nhánh 5 đầu thì để trống (Excel tự ẩn), QI = 2 × nhánh trái vẫn đúng.
  else if (nd.doi_xung && !x.nhanh_phai?.length) json.nhanh_phai = x.nhanh_trai.length <= TOI_DA.nhanh_phai ? sach(x.nhanh_trai) : [];
  if (x.hang_cuoi || x.hang_rieng) { // hàng có cấu trúc riêng (thiếu đầu): xuat_thuy_luc_excel.py chèn dòng tính ngay tại nút hàng đó (Excel COM)
    const cuoi = x.ong_chinh.length - 1;
    const ds = (x.hang_rieng ?? []).filter((h) => !(x.hang_cuoi && h.chi_so === cuoi)).map((h) => ({ chi_so: h.chi_so, trai: sach(h.nhanh_trai), phai: sach(h.nhanh_phai) ?? [] }));
    if (x.hang_cuoi) ds.push({ chi_so: cuoi, trai: sach(x.hang_cuoi.nhanh_trai), phai: sach(x.hang_cuoi.nhanh_phai) ?? [] });
    json.hang_rieng_bp = ds;
  }
  json.mach_vt = soLieu.vt_rieng ? "SP và VT riêng" : "SP + VT chung";
  json.mang = soLieu.mang === "cut" ? "Mạch cụt" : "Mạch vòng";
  const giu = new Set([...khoa, "o", "hang_rieng_bp", "doi_xung", "so_nhanh", "mach_vt", "mang"]);
  for (const k of Object.keys(json)) {
    if (json[k] === undefined || ["mot_phia", "vt_rieng", "hang_cuoi", "hang_rieng"].includes(k)) delete json[k];
    else if (!giu.has(k)) {
      delete json[k];
      ghi_chu.push(`"${k}" chưa có ô trong Excel mẫu, bỏ qua${k === "P_van" ? " (Excel chưa cộng tổn thất cụm van HCB2)" : ""}.`);
    }
  }
  return { json, ghi_chu };
}

/** Ghi file Excel `ra` từ số liệu; trả ghi chú + nhật ký script. hoanThien = false: bỏ bước Excel COM (test, máy không có Excel). */
export function xuatThuyLucExcel(goc: string, soLieu: SoLieuThuyLuc, ra: string, hoanThien = true, timeoutMs = 3 * 60_000): Promise<string[]> {
  const khoa = Object.keys(JSON.parse(readFileSync(path.join(goc, "bang_tinh", "thuy_luc", "o_nhap.json"), "utf8"))).filter((k) => !k.startsWith("_"));
  const { json, ghi_chu } = soLieuExcel(soLieu, khoa);
  const tam = ra.replace(/\.xlsx$/i, "") + "_so_lieu.json";
  writeFileSync(tam, JSON.stringify(json, null, 2), "utf8");
  const args = [path.join(goc, "scripts", "xuat_thuy_luc_excel.py"), tam, ra, ...(hoanThien ? [] : ["--khong-hoan-thien"])];
  return new Promise((resolve, reject) => {
    const p = spawn("python", args, { cwd: goc, windowsHide: true, env: { ...process.env, PYTHONIOENCODING: "utf-8", PYTHONUTF8: "1" } });
    let out = "";
    const t = setTimeout(() => { p.kill(); reject(new Error("Xuất Excel quá thời gian")); }, timeoutMs);
    p.stdout.on("data", (d) => (out += d));
    p.stderr.on("data", (d) => (out += d));
    p.on("error", () => { clearTimeout(t); reject(new Error(HUONG_DAN_PYTHON)); });
    p.on("close", (code) => {
      clearTimeout(t);
      if (code === 0) return resolve([...ghi_chu, ...out.split(/\r?\n/).map((l) => l.trim()).filter(Boolean)]);
      if (code === 9009 || /was not found/i.test(out)) return reject(new Error(HUONG_DAN_PYTHON));
      if (/PermissionError/.test(out)) return reject(new Error("File Excel kết quả đang mở: đóng file rồi xuất lại"));
      reject(new Error(`Xuất Excel lỗi (mã ${code}): ${out.trim().split(/\r?\n/).slice(-2).join(" | ").slice(0, 400)}`));
    });
  });
}
