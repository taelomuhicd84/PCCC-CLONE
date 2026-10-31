import { describe, expect, it } from "vitest";
import ExcelJS from "exceljs";
import { lapBaoGia, type DanhMuc } from "./bao_gia";
import { docDonGia, taoMauDonGia, xuatBaoGia } from "./excel";
import { tenThuMuc } from "./server";

const dm: DanhMuc = {
  he: { BC: "Báo cháy tự động", ONG: "Đường ống" },
  vat_tu: [
    { ma: "BC_KHOI", ten: "Đầu báo khói", dvt: "cái", he: "BC", loai: "block" },
    { ma: "BC_TT", ten: "Tủ trung tâm", dvt: "bộ", he: "BC", loai: "block" },
    { ma: "ONG_DN50", ten: "Ống DN50", dvt: "m", he: "ONG", loai: "tuyen" },
  ],
};
const kl = { du_an: "Thử", dong: [{ ma: "BC_KHOI", sl: 10 }, { ma: "BC_KHOI", sl: 2 }, { ma: "ONG_DN50", sl: 12.345 }, { ma: "BC_TT", sl: 1 }, { ma: "LA", sl: 3 }] };

describe("lapBaoGia", () => {
  it("cộng trùng mã, làm tròn tuyến, tính tiền + VAT, báo thiếu giá", () => {
    const bg = lapBaoGia(kl, dm, { BC_KHOI: { vt: 300_000, nc: 50_000 }, ONG_DN50: { vt: 100_000, nc: null } });
    expect(bg.nhom.map((n) => n.he)).toEqual(["BC", "ONG", "KHAC"]);
    expect(bg.nhom[0].dong[0]).toMatchObject({ ma: "BC_KHOI", sl: 12, thanh_tien: 4_200_000 });
    expect(bg.nhom[1].dong[0]).toMatchObject({ sl: 12.35, thanh_tien: 1_235_000 });
    expect(bg.thieu_gia).toEqual(["BC_TT", "LA"]);
    expect(bg.tong).toBe(5_435_000);
    expect(bg.tong_cong).toBe(5_435_000 + 543_500);
  });
});

describe("excel", () => {
  it("mẫu đơn giá điền tay đọc lại đúng, báo giá có công thức", async () => {
    const wb = new ExcelJS.Workbook();
    await wb.xlsx.load(await taoMauDonGia(dm));
    const ws = wb.worksheets[0];
    ws.getCell("E2").value = 300000;
    ws.getCell("F2").value = "50.000";
    const gia = await docDonGia(Buffer.from(await wb.xlsx.writeBuffer()));
    expect(gia.BC_KHOI).toEqual({ vt: 300000, nc: 50000 });
    expect(gia.BC_TT).toEqual({ vt: null, nc: null });

    const out = new ExcelJS.Workbook();
    await out.xlsx.load(await xuatBaoGia(lapBaoGia(kl, dm, gia), kl));
    const formulas: string[] = [];
    out.worksheets[0].eachRow((r) => r.eachCell((c) => { if (c.formula) formulas.push(c.formula); }));
    expect(formulas.some((f) => f.startsWith("SUM("))).toBe(true);
  });
});

describe("tenThuMuc", () => {
  it("chặn đường dẫn thoát thư mục", () => {
    expect(tenThuMuc("../../Windows")).toBe("_.._Windows");
    expect(tenThuMuc("Nhà xưởng A")).toBe("Nhà xưởng A");
    expect(() => tenThuMuc("  ")).toThrow();
  });
});
