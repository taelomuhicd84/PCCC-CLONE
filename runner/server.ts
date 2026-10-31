// Runner nội bộ: nhận khối lượng từ plugin AutoCAD, quản lý đơn giá, xuất báo giá, hỏi Claude; phục vụ web/.
// Chạy: npm start  (mặc định chỉ máy này; LAN: npm start -- --host 0.0.0.0)
import { createServer, type IncomingMessage, type ServerResponse } from "node:http";
import { existsSync, mkdirSync, readFileSync, readdirSync, writeFileSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { lapBaoGia, type DanhMuc, type DonGia, type KhoiLuong } from "./bao_gia";
import { docDonGia, taoMauDonGia, xuatBaoGia } from "./excel";
import { hoiClaude } from "./claude";
import { khaoSatTrong, khoiLuongSoBo, tachJsonKhaoSat, HE_THONG, type KhaoSat } from "./khao_sat";
import { xuatKhaoSat } from "./word";
import { marked } from "marked";

const GOC = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const DU_AN = path.join(GOC, "du_an");
const FILE_DON_GIA = path.join(GOC, "data", "don_gia.json");
const KNOWLEDGE = path.join(GOC, "knowledge");
const DOCX = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
const XLSX = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
const arg = (k: string, d: string) => { const i = process.argv.indexOf(k); return i > 0 ? process.argv[i + 1] : d; };
const PORT = Number(arg("--port", "8787"));
const HOST = arg("--host", "127.0.0.1");

const danhMuc = (): DanhMuc => JSON.parse(readFileSync(path.join(GOC, "data", "vat_tu.json"), "utf8"));
const donGia = (): DonGia => (existsSync(FILE_DON_GIA) ? JSON.parse(readFileSync(FILE_DON_GIA, "utf8")) : {});

/** Tên dự án thành tên thư mục an toàn (chặn ../, ký tự cấm Windows). */
export function tenThuMuc(ten: unknown): string {
  const s = String(ten ?? "").replace(/[<>:"/\\|?*\x00-\x1f]/g, "_").replace(/^\.+/, "").trim().slice(0, 100);
  if (!s) throw new LoiNguoiDung("Thiếu tên dự án");
  return s;
}
class LoiNguoiDung extends Error {}

// nguon: "cad" = bóc từ bản vẽ (PCCC_BOC), "so_bo" = ước theo khảo sát.
const fileKL = (duAn: unknown, nguon: unknown = "cad") => path.join(DU_AN, tenThuMuc(duAn), nguon === "so_bo" ? "khoi_luong_so_bo.json" : "khoi_luong.json");
const fileKS = (duAn: unknown) => path.join(DU_AN, tenThuMuc(duAn), "khao_sat.json");
function docKL(duAn: unknown, nguon: unknown = "cad"): KhoiLuong {
  const f = fileKL(duAn, nguon);
  if (!existsSync(f)) throw new LoiNguoiDung(nguon === "so_bo" ? "Chưa có khối lượng sơ bộ: lưu khảo sát rồi bấm Ước khối lượng" : "Dự án chưa có khối lượng: chạy PCCC_BOC trong AutoCAD");
  return JSON.parse(readFileSync(f, "utf8"));
}
const docKS = (duAn: unknown): KhaoSat => (existsSync(fileKS(duAn)) ? JSON.parse(readFileSync(fileKS(duAn), "utf8")) : khaoSatTrong(tenThuMuc(duAn)));
function ghiJson(f: string, x: unknown) {
  mkdirSync(path.dirname(f), { recursive: true });
  writeFileSync(f, JSON.stringify(x, null, 1), "utf8");
}
/** Chỉ cho đọc file .md nằm trong knowledge/ (chặn ../). */
function fileTaiLieu(ten: unknown): string {
  const f = path.resolve(KNOWLEDGE, String(ten ?? ""));
  if (!f.startsWith(KNOWLEDGE + path.sep) || !f.endsWith(".md") || !existsSync(f)) throw new LoiNguoiDung("Không có tài liệu " + ten);
  return f;
}

function docBody(req: IncomingMessage, max = 20 * 1024 * 1024): Promise<Buffer> {
  return new Promise((resolve, reject) => {
    const parts: Buffer[] = [];
    let n = 0;
    req.on("data", (c: Buffer) => { n += c.length; if (n > max) { reject(new LoiNguoiDung("File quá lớn")); req.destroy(); } else parts.push(c); });
    req.on("end", () => resolve(Buffer.concat(parts)));
    req.on("error", reject);
  });
}
const json = (res: ServerResponse, x: unknown, code = 200) => { res.writeHead(code, { "content-type": "application/json; charset=utf-8" }); res.end(JSON.stringify(x)); };
const tai = (res: ServerResponse, buf: Buffer, ten: string) => {
  res.writeHead(200, { "content-type": XLSX, "content-disposition": `attachment; filename*=UTF-8''${encodeURIComponent(ten)}` });
  res.end(buf);
};

async function xuLy(req: IncomingMessage, res: ServerResponse) {
  const url = new URL(req.url ?? "/", "http://x");
  const q = url.searchParams;
  const r = `${req.method} ${url.pathname}`;

  if (r === "GET /" || r === "GET /index.html") {
    res.writeHead(200, { "content-type": "text/html; charset=utf-8" });
    return res.end(readFileSync(path.join(GOC, "web", "index.html")));
  }
  if (r === "GET /api/vat-tu") return json(res, danhMuc());
  if (r === "GET /api/du-an") return json(res, existsSync(DU_AN) ? readdirSync(DU_AN).filter((d) => existsSync(fileKL(d)) || existsSync(fileKL(d, "so_bo")) || existsSync(fileKS(d))) : []);
  if (r === "GET /api/huong-dan") return json(res, JSON.parse(readFileSync(path.join(GOC, "data", "huong_dan.json"), "utf8")));
  if (r === "GET /api/tai-lieu") {
    res.writeHead(200, { "content-type": "text/html; charset=utf-8" });
    return res.end(await marked.parse(readFileSync(fileTaiLieu(q.get("f")), "utf8")));
  }
  if (r === "GET /api/khao-sat") return json(res, { he_thong_mac_dinh: HE_THONG.map((h) => h.ten), khao_sat: docKS(q.get("du_an")) });
  if (r === "POST /api/khao-sat") {
    const ks = JSON.parse((await docBody(req)).toString("utf8")) as KhaoSat;
    ks.du_an = tenThuMuc(ks.du_an);
    ghiJson(fileKS(ks.du_an), ks);
    return json(res, { ok: true, du_an: ks.du_an });
  }
  if (r === "POST /api/khao-sat/goi-y") {
    const ks = JSON.parse((await docBody(req)).toString("utf8")) as KhaoSat;
    const cauHoi = [
      "Đọc knowledge/khao-sat-bao-gia.md và làm đúng hướng dẫn trong đó. Tri thức quy chuẩn nằm trong thư mục knowledge/ (QCVN 06, QCVN 10:2025, nghị định thẩm định, MOD_*, SKILL_thoat_nan); chỉ trích điều khoản có trong các file này.",
      "Không có bản vẽ, chỉ có số liệu dưới đây (đầu vào loại 3). Số liệu đã có thì dùng nguyên, ô trống thì ước lượng có nhãn \"ước lượng\" hoặc ghi CẦN XÁC NHẬN.",
      "Mục he_thong trả đúng các tên sau, đúng thứ tự: " + HE_THONG.map((h) => h.ten).join("; ") + ".",
      "Số liệu hiện có (JSON): " + JSON.stringify(ks),
    ].join("\n\n");
    const traLoi = await hoiClaude(cauHoi, GOC, 10 * 60_000);
    const goiY = tachJsonKhaoSat(traLoi);
    if (!goiY) throw new Error("Claude không trả khối json khao_sat. Trả lời: " + traLoi.slice(0, 300));
    return json(res, { khao_sat: { ...ks, ...goiY, du_an: ks.du_an }, tra_loi: traLoi });
  }
  if (r === "GET /api/khao-sat.docx") {
    const ks = docKS(q.get("du_an"));
    res.writeHead(200, { "content-type": DOCX, "content-disposition": `attachment; filename*=UTF-8''${encodeURIComponent("bao_cao_khao_sat_" + ks.du_an + ".docx")}` });
    return res.end(await xuatKhaoSat(ks));
  }
  if (r === "POST /api/khao-sat/so-bo") {
    const kl = khoiLuongSoBo(docKS(q.get("du_an")));
    if (!kl.dong.length) throw new LoiNguoiDung("Không ước được: cần diện tích và ít nhất một hệ thống CÓ / CẦN XÁC NHẬN");
    ghiJson(fileKL(q.get("du_an"), "so_bo"), kl);
    return json(res, { ok: true, so_dong: kl.dong.length });
  }
  if (r === "POST /api/khoi-luong") {
    const kl = JSON.parse((await docBody(req)).toString("utf8")) as KhoiLuong;
    if (!Array.isArray(kl.dong)) throw new LoiNguoiDung("Thiếu danh sách dong");
    kl.dong = kl.dong.map((d) => ({ ma: String(d.ma), sl: Number(d.sl) || 0 }));
    const f = fileKL(kl.du_an);
    mkdirSync(path.dirname(f), { recursive: true });
    writeFileSync(f, JSON.stringify({ ...kl, luc: kl.luc ?? new Date().toISOString() }, null, 1), "utf8");
    return json(res, { ok: true, du_an: tenThuMuc(kl.du_an), so_dong: kl.dong.length });
  }
  if (r === "GET /api/khoi-luong") return json(res, docKL(q.get("du_an"), q.get("nguon")));
  if (r === "GET /api/don-gia/mau") return tai(res, await taoMauDonGia(danhMuc(), donGia()), "don_gia.xlsx");
  if (r === "POST /api/don-gia") {
    const moi = await docDonGia(await docBody(req));
    writeFileSync(FILE_DON_GIA, JSON.stringify({ ...donGia(), ...moi }, null, 1), "utf8");
    return json(res, { ok: true, so_ma: Object.keys(moi).length });
  }
  if (r === "GET /api/bao-gia") return json(res, lapBaoGia(docKL(q.get("du_an"), q.get("nguon")), danhMuc(), donGia()));
  if (r === "GET /api/bao-gia.xlsx") {
    const kl = docKL(q.get("du_an"), q.get("nguon"));
    return tai(res, await xuatBaoGia(lapBaoGia(kl, danhMuc(), donGia()), kl), `bao_gia_${tenThuMuc(kl.du_an)}.xlsx`);
  }
  if (r === "POST /api/claude") {
    const { cau_hoi, tai_lieu } = JSON.parse((await docBody(req, 100_000)).toString("utf8"));
    if (!cau_hoi) throw new LoiNguoiDung("Thiếu cau_hoi");
    const doc = Array.isArray(tai_lieu) && tai_lieu.length
      ? "Đọc các file sau trước khi trả lời: " + tai_lieu.map((f: unknown) => path.relative(GOC, fileTaiLieu(f)).replace(/\\/g, "/")).join(", ") + ". Chỉ trích điều khoản có trong file; không có thì nói rõ cần tra văn bản nào.\n\n"
      : "";
    return json(res, { tra_loi: await hoiClaude(doc + String(cau_hoi), GOC) });
  }
  json(res, { loi: "Không có đường dẫn " + r }, 404);
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  createServer((req, res) => {
    xuLy(req, res).catch((e) => json(res, { loi: e instanceof Error ? e.message : String(e) }, e instanceof LoiNguoiDung || e instanceof SyntaxError ? 400 : 500));
  }).listen(PORT, HOST, () => console.log(`TKPCCC runner: http://${HOST === "0.0.0.0" ? "<IP-máy>" : HOST}:${PORT}`));
}
