import { describe, expect, it } from "vitest";
import ExcelJS from "exceljs";
import { lapBaoGia } from "../../shared/src/bao_gia";
import { khaoSatTrong } from "../../shared/src/khao_sat";
import { dm, kl } from "../../shared/test/mau";
import { docDonGia, taoMauDonGia, xuatBaoGia } from "../src/xuat_xlsx";
import { xuatKhaoSat } from "../src/xuat_docx";
import { tenThuMuc } from "../src/server";

describe("xuat_xlsx", () => {
  it("mẫu đơn giá điền tay đọc lại đúng, báo giá có công thức", async () => {
    const wb = new ExcelJS.Workbook();
    await wb.xlsx.load(await taoMauDonGia(dm));
    const ws = wb.worksheets[0];
    ws.getCell("E2").value = 300000;
    ws.getCell("F2").value = "50.000";
    ws.getCell("E3").value = { formula: "100000+50000", result: 150000 };
    ws.getCell("F3").value = "20.000đ";
    ws.getCell("E4").value = { richText: [{ text: "120." }, { text: "000" }] };
    const gia = await docDonGia(Buffer.from(await wb.xlsx.writeBuffer()));
    expect(gia.BC_KHOI).toEqual({ vt: 300000, nc: 50000 });
    expect(gia.BC_TT).toEqual({ vt: 150000, nc: 20000 });
    expect(gia.ONG_DN50).toEqual({ vt: 120000, nc: null });

    const out = new ExcelJS.Workbook();
    await out.xlsx.load(await xuatBaoGia(lapBaoGia(kl, dm, gia), kl));
    const formulas: string[] = [];
    out.worksheets[0].eachRow((r) => r.eachCell((c) => { if (c.formula) formulas.push(c.formula); }));
    expect(formulas.some((f) => f.startsWith("SUM("))).toBe(true);
    expect(out.worksheets.map((w) => w.name)).toEqual(["BaoGia"]);
  });

  it("có khu vực thì thêm sheet khối lượng theo tầng/khu", async () => {
    const klKhu = { ...kl, khu_vuc: [{ ten: "Tầng 1", dong: [{ ma: "BC_KHOI", sl: 7 }] }, { ten: "Tầng 2", dong: [{ ma: "BC_KHOI", sl: 5 }] }] };
    const out = new ExcelJS.Workbook();
    await out.xlsx.load(await xuatBaoGia(lapBaoGia(klKhu, dm, {}), klKhu));
    const ws = out.getWorksheet("TheoKhuVuc")!;
    expect(ws.getRow(1).values).toEqual([, "Mã", "Hạng mục", "ĐVT", "Tầng 1", "Tầng 2", "Tổng"]);
    expect(ws.getRow(2).values).toEqual([, "BC_KHOI", "Đầu báo khói", "cái", 7, 5, 12]);
  });
});

describe("xuat_docx", () => {
  it("tạo file docx báo cáo khảo sát", async () => {
    const b = await xuatKhaoSat(khaoSatTrong("Thử"));
    expect(b.subarray(0, 2).toString()).toBe("PK");
  });
});

describe("tenThuMuc", () => {
  it("chặn đường dẫn thoát thư mục, tên thiết bị Windows, dấu chấm cuối", () => {
    expect(tenThuMuc("../../Windows")).toBe("_.._Windows");
    expect(tenThuMuc("Nhà xưởng A")).toBe("Nhà xưởng A");
    expect(tenThuMuc("CON")).toBe("_CON");
    expect(tenThuMuc("abc. ")).toBe("abc");
    expect(() => tenThuMuc("  ")).toThrow();
    expect(() => tenThuMuc("...")).toThrow();
  });
});

describe("nguonHopLe (CSRF / DNS rebinding)", () => {
  it("máy này: Host localhost ok, Origin trùng ok, Origin lạ chặn, Host lạ chặn, không Origin (plugin CAD) ok", async () => {
    const { nguonHopLe } = await import("../src/server");
    expect(nguonHopLe("127.0.0.1:8787", undefined, true)).toBe(true);
    expect(nguonHopLe("localhost:8787", "http://localhost:8787", true)).toBe(true);
    expect(nguonHopLe("127.0.0.1:8787", "http://evil.example", true)).toBe(false);
    expect(nguonHopLe("evil.example:8787", undefined, true)).toBe(false); // DNS rebinding
    expect(nguonHopLe("192.168.1.5:8787", "http://192.168.1.5:8787", false)).toBe(true); // --host 0.0.0.0
    expect(nguonHopLe("192.168.1.5:8787", "http://evil.example", false)).toBe(false);
  });
});
