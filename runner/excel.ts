// Excel: file đơn giá mẫu (người dùng điền), đọc lại đơn giá, xuất báo giá có công thức để sửa tiếp trong Excel.
import ExcelJS from "exceljs";
import type { BaoGia, DanhMuc, DonGia, KhoiLuong } from "./bao_gia";

const COT_DON_GIA = ["MA_VT", "Tên vật tư", "ĐVT", "Hệ", "Đơn giá vật tư (VNĐ)", "Đơn giá nhân công (VNĐ)"];

export async function taoMauDonGia(dm: DanhMuc, gia: DonGia = {}): Promise<Buffer> {
  const wb = new ExcelJS.Workbook();
  const ws = wb.addWorksheet("DonGia");
  ws.addRow(COT_DON_GIA).font = { bold: true };
  for (const v of dm.vat_tu) ws.addRow([v.ma, v.ten, v.dvt, dm.he[v.he] ?? v.he, gia[v.ma]?.vt ?? null, gia[v.ma]?.nc ?? null]);
  ws.columns.forEach((c, i) => (c.width = [16, 46, 8, 34, 22, 24][i]));
  [5, 6].forEach((i) => (ws.getColumn(i).numFmt = "#,##0"));
  ws.views = [{ state: "frozen", ySplit: 1 }];
  return Buffer.from(await wb.xlsx.writeBuffer());
}

/** Đọc sheet đầu: cột A = MA_VT, E = giá vật tư, F = giá nhân công. Dòng không có mã bị bỏ qua. */
export async function docDonGia(buf: Buffer): Promise<DonGia> {
  const wb = new ExcelJS.Workbook();
  await wb.xlsx.load(buf);
  const ws = wb.worksheets[0];
  if (!ws) throw new Error("File Excel không có sheet nào.");
  const so = (x: ExcelJS.CellValue): number | null => {
    const v = x && typeof x === "object" && "result" in x ? x.result : x;
    if (v == null || v === "") return null;
    const n = typeof v === "number" ? v : Number(String(v).replace(/[.,\s](?=\d{3}\b)/g, ""));
    return Number.isFinite(n) ? n : null;
  };
  const gia: DonGia = {};
  ws.eachRow((r, i) => {
    const ma = String(r.getCell(1).value ?? "").trim();
    if (i === 1 || !ma) return;
    gia[ma] = { vt: so(r.getCell(5).value), nc: so(r.getCell(6).value) };
  });
  return gia;
}

export async function xuatBaoGia(bg: BaoGia, kl: KhoiLuong): Promise<Buffer> {
  const wb = new ExcelJS.Workbook();
  const ws = wb.addWorksheet("BaoGia");
  ws.addRow([`BÁO GIÁ HỆ THỐNG PCCC — ${kl.du_an}`]).font = { bold: true, size: 14 };
  ws.addRow([`Khối lượng bóc từ bản vẽ: ${kl.ban_ve ?? ""} ${kl.luc ?? ""}`]);
  ws.addRow([]);
  ws.addRow(["STT", "Mã", "Hạng mục", "ĐVT", "Khối lượng", "Đơn giá VT", "Đơn giá NC", "Thành tiền", "Ghi chú"]).font = { bold: true };
  const dongCong: number[] = [];
  bg.nhom.forEach((n, i) => {
    const r = ws.addRow([String.fromCharCode(65 + i), "", n.ten_he]);
    r.font = { bold: true };
    const dau = r.number + 1;
    n.dong.forEach((d, j) => {
      const row = ws.addRow([j + 1, d.ma, d.ten, d.dvt, d.sl, d.vt, d.nc, null, [d.thanh_tien == null ? "chưa có đơn giá" : "", d.ghi_chu].filter(Boolean).join("; ")]);
      row.getCell(8).value = { formula: `E${row.number}*(N(F${row.number})+N(G${row.number}))`, result: d.thanh_tien ?? 0 };
    });
    const cuoi = ws.rowCount;
    const c = ws.addRow(["", "", `Cộng ${n.ten_he}`]);
    c.getCell(8).value = { formula: `SUM(H${dau}:H${cuoi})`, result: n.cong };
    c.font = { italic: true };
    dongCong.push(c.number);
  });
  const tong = ws.addRow(["", "", "TỔNG CỘNG TRƯỚC THUẾ"]);
  tong.getCell(8).value = { formula: dongCong.map((x) => `H${x}`).join("+") || "0", result: bg.tong };
  const thue = ws.addRow(["", "", "Thuế VAT", "", bg.vat]);
  thue.getCell(8).value = { formula: `ROUND(H${tong.number}*E${thue.number},0)`, result: bg.thue };
  const tc = ws.addRow(["", "", "TỔNG CỘNG SAU THUẾ"]);
  tc.getCell(8).value = { formula: `H${tong.number}+H${thue.number}`, result: bg.tong_cong };
  [tong, thue, tc].forEach((r) => (r.font = { bold: true }));
  ws.columns.forEach((c, i) => (c.width = [6, 14, 46, 7, 12, 16, 16, 18, 40][i]));
  [5].forEach((i) => (ws.getColumn(i).numFmt = "#,##0.##"));
  [6, 7, 8].forEach((i) => (ws.getColumn(i).numFmt = "#,##0"));
  thue.getCell(5).numFmt = "0%"; // sau định dạng cột để không bị ghi đè
  return Buffer.from(await wb.xlsx.writeBuffer());
}
