import { describe, expect, it } from "vitest";
import ExcelJS from "exceljs";
import { spawnSync } from "node:child_process";
import { mkdtempSync, readFileSync } from "node:fs";
import os from "node:os";
import path from "node:path";
import { soBoThuyLuc, type SoLieuThuyLuc } from "../../shared/src/thuy_luc";
import { chuanHoaMatBang, soLieuExcel, xuatThuyLucExcel } from "../src/thuy_luc";

const GOC = path.resolve(__dirname, "..", "..");
const viDu: SoLieuThuyLuc = JSON.parse(readFileSync(path.join(GOC, "bang_tinh", "thuy_luc", "vi_du", "so_lieu_nha_may_nhua.json"), "utf8"));
const khoa = Object.keys(JSON.parse(readFileSync(path.join(GOC, "bang_tinh", "thuy_luc", "o_nhap.json"), "utf8"))).filter((k) => !k.startsWith("_"));
const coPython = spawnSync("python", ["-c", "import json, zipfile"], { windowsHide: true }).status === 0;

describe("thủy lực trên web", () => {
  it("ô sơ bộ: số kiểu Việt Nam, họng chỉ khi có số tia, nhóm ngoài bảng báo lỗi", () => {
    const mb = chuanHoaMatBang({ nhom: "3", cao_phong: "8", L_ong_chinh_den_bom: "120", Z_m: "15,5", vong: "true", hong: { so_tia: "", q_tia: "2,6" } });
    expect(mb).toMatchObject({ nhom: "3", cao_phong: 8, L_ong_chinh_den_bom: 120, Z_m: 15.5, vong: true, hong: undefined });
    expect(chuanHoaMatBang({ nhom: "2", cao_phong: 4, L_ong_chinh_den_bom: 60, Z_m: 8, hong: { so_tia: 2, q_tia: "2,6", P_lang: "0,21" } }).hong)
      .toEqual({ so_tia: 2, q_tia: 2.6, P_lang: 0.21, L_voi: 0, Kp: 0 });
    expect(chuanHoaMatBang({ nhom: "2", cao_phong: 4, L_ong_chinh_den_bom: 60, Z_m: 8, hong: { so_tia: 2, q_tia: "5,2", P_lang: "0,24", q_tia_h6: "5" } }).hong?.q_tia_h6).toBe(5);
    expect(() => chuanHoaMatBang({ nhom: "5", cao_phong: 8, L_ong_chinh_den_bom: 1, Z_m: 1 })).toThrow(/4.1/);
    expect(() => chuanHoaMatBang({ nhom: "1", cao_phong: "", L_ong_chinh_den_bom: 1, Z_m: 1 })).toThrow(/chiều cao phòng/);
  });

  it("số liệu -> ô Excel mẫu: chữ ô chọn, 4 dòng vòng, đổi K, bỏ khóa không có ô, chặn vượt khung", () => {
    const { json, ghi_chu } = soLieuExcel({ ...viDu, P_van: 0.03 }, khoa);
    expect(json).toMatchObject({ doi_xung: "Không đối xứng với nhánh phải", so_nhanh: "2 nhánh", mach_vt: "SP + VT chung", mang: "Mạch vòng" });
    expect(json.vong).toEqual([{ dn: 125, l: 85 }, { dn: 125, l: 44.3 }, { dn: 125, l: 34 }, { dn: 125, l: 4 }]);
    expect(json).not.toHaveProperty("P_van");
    expect(ghi_chu.join()).toMatch(/P_van.*HCB2/);
    const k = soLieuExcel({ ...viDu, K_gpm: undefined, K: 6.078 }, khoa);
    expect(k.json.K_gpm).toBeCloseTo(8, 2);
    expect(() => soLieuExcel({ ...viDu, ong_chinh: Array.from({ length: 8 }, () => ({ dn: 100, l: 3 })) }, khoa)).toThrow(/7 dòng ong_chinh/);
    // số liệu sơ bộ cũng phải ghi được vào mẫu (không còn khóa lạ làm script dừng)
    const sb = soBoThuyLuc({ nhom: "3", cao_phong: 8, L_ong_chinh_den_bom: 120, Z_m: 15, vong: true });
    const sbx = soLieuExcel(sb.so_lieu, khoa).json;
    expect(Object.keys(sbx).every((x) => khoa.includes(x) || ["doi_xung", "so_nhanh", "mach_vt", "mang"].includes(x))).toBe(true);
    // đối xứng không ghi nhánh phải: chép nhánh trái sang, không để số cũ của file mẫu
    expect(sbx.nhanh_phai).toEqual(sb.so_lieu.nhanh_trai.map(({ dn, l }) => ({ dn, l })));
  });

  it.skipIf(!coPython)("xuất Excel mẫu: ô nhập mang đúng số liệu, công thức giữ nguyên", async () => {
    const ra = path.join(mkdtempSync(path.join(os.tmpdir(), "tl-")), "x.xlsx");
    await xuatThuyLucExcel(GOC, viDu, ra, false);
    const wb = new ExcelJS.Workbook();
    await wb.xlsx.readFile(ra);
    const ws = wb.getWorksheet("SP chung voi HR")!;
    expect(ws.getCell("H17").value).toBe(8);
    expect([ws.getCell("F61").value, ws.getCell("I61").value]).toEqual([32, 3]);
    expect(ws.getCell("D97").value).toBe("Không đối xứng với nhánh phải");
    expect(ws.getCell("F100").value).toBe(125);
    expect(ws.getCell("F116").value).toBe(125);
    expect(ws.getCell("F124").value).toBe(null); // ví dụ có 3 đoạn ống chính: dòng thứ 4 bỏ trống để chuỗi dừng
    expect(ws.getCell("D61").formula).toBeTruthy();
    expect(ws.getCell("F105").formula).toMatch(/SQRT\(G99\*N102\)/); // TCVN 7336 B.2.10.12: QII theo áp nút b, không phải nút a (L100)
    expect(ws.getCell("D183").formula).toMatch(/I161/); // ống vòng mang lưu lượng họng tính toán H.7 (I161), không phải tối thiểu H.6 (H21)
    expect(ws.getCell("D183").formula).not.toMatch(/H21/);
  }, 60_000);
});
