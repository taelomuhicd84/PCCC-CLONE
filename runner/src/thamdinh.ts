// Thẩm định PDF: gọi lõi Python thamdinh/ (pccc_checker, đọc chữ vector + luật QCVN 06, offline, không API key).
import { spawn } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import path from "node:path";
import { docSoVN } from "../../shared/src/bao_gia";

export type TrangThai = "D" | "KD" | "XM" | "NA";
export type DongThamDinh = {
  id: string; so?: number; tt: TrangThai; rule: string; dieu_khoan?: string | null; trang?: number | null;
  bbox?: number[] | null; yc?: unknown; ht?: unknown; ly_do?: string | null; evid?: string | null;
};
export type TomTat = { ok: boolean; pdf_ghi_chu: string; trang_da_doc: number[]; thong_tin: Record<string, unknown>; dem: Record<string, number> };

/** Thông tin công trình người dùng nhập (ô trên web) -> `--set`. Khoá ngoài danh sách bị bỏ. */
export const KHOA_SO = ["height_pccc_m", "floors_above", "basements", "floor_area_m2", "max_occupants_per_floor"];
export const KHOA_CHU = ["function_group", "fire_resistance_level", "structural_hazard_class"];
export const KHOA_CO_KHONG = ["has_auto_sprinkler", "has_auto_fire_alarm"];

export const HUONG_DAN_PYTHON = "Máy chưa có Python 3: cài từ python.org (tick \"Add python.exe to PATH\"), rồi mở terminal chạy: python -m pip install pymupdf pyyaml";
const HUONG_DAN_THU_VIEN = "Python thiếu thư viện: mở terminal chạy: python -m pip install pymupdf pyyaml";

export function thamSoSet(thongTin: Record<string, unknown>): string[] {
  const out: string[] = [];
  for (const [k, v] of Object.entries(thongTin ?? {})) {
    if (v === null || v === undefined || String(v).trim() === "") continue;
    let s: string | null = null;
    if (KHOA_SO.includes(k)) { const n = docSoVN(v); s = n === null ? null : String(n); }
    else if (KHOA_CHU.includes(k)) s = String(v).trim().toUpperCase().replace(/\s+/g, "").slice(0, 10) || null;
    else if (KHOA_CO_KHONG.includes(k)) s = /^(1|true|co|có|yes)$/i.test(String(v).trim()) ? "true" : "false";
    if (s !== null) out.push("--set", `${k}=${s}`);
  }
  return out;
}

const THU_TU: Record<string, number> = { KD: 0, XM: 1, D: 2, NA: 3 };
/** findings.jsonl -> các dòng, xếp KD, XM, D rồi theo trang. */
export function docKetLuan(file: string): DongThamDinh[] {
  if (!existsSync(file)) return [];
  return readFileSync(file, "utf8").split(/\r?\n/).filter((l) => l.trim()).map((l) => JSON.parse(l) as DongThamDinh)
    .sort((a, b) => (THU_TU[a.tt] ?? 9) - (THU_TU[b.tt] ?? 9) || (a.trang ?? 0) - (b.trang ?? 0));
}

/** Câu hỏi cho Claude xét 1 dòng XM. `thuMuc`: thư mục kết quả tính từ gốc dự án (ket_qua/<dự án>/thamdinh). */
export function cauHoiXet(dong: DongThamDinh, thongTin: Record<string, unknown>, thuMuc: string): string {
  const trang = dong.trang ? `đúng trang ${dong.trang} của ${thuMuc}/ban_ve.pdf bằng Read (tham số pages: "${dong.trang}")` : `trang cần xem của ${thuMuc}/ban_ve.pdf bằng Read (tham số pages)`;
  return [
    "Bạn là kỹ sư thẩm định PCCC. Máy kiểm bản vẽ PDF theo QCVN 06:2022 (sửa đổi 1:2023) và xếp dòng dưới đây là CẦN XÁC MINH. Hãy xét dòng này.",
    "- Đọc .claude/skills/tkpccc-tra-quy-chuan/SKILL.md để chọn file quy chuẩn (thoát nạn: skills/SKILL_thoat_nan.md rồi skills/MOD_*.md). Grep theo số điều trước, chỉ Read đoạn cần.",
    "- CHỈ trích nguyên văn điều khoản có trong knowledge/ hoặc skills/, ghi file + mục. Không có thì ghi \"cần tra <văn bản>\". Không viết theo trí nhớ.",
    `- Số liệu máy trích từ bản vẽ: ${thuMuc}/extraction.md. Cần xem hình thì mở ${trang}, không đọc cả file.`,
    "- Không đoán số liệu: bản vẽ không thể hiện thì giữ CẦN XÁC MINH và nói rõ cần kiểm gì.",
    "Trả lời tiếng Việt, ngắn, đúng mẫu:\nĐỀ XUẤT: ĐẠT | KHÔNG ĐẠT | CẦN XÁC MINH\nLÝ DO: …\nCĂN CỨ: \"<nguyên văn>\" (<file>, <điều khoản>)\nCẦN KIỂM TRÊN BẢN VẼ: …",
    "Dòng (JSON): " + JSON.stringify(dong),
    "Thông tin công trình máy đọc được (JSON): " + JSON.stringify(thongTin ?? {}),
  ].join("\n\n");
}

const DE_XUAT: Record<string, TrangThai> = { "KHÔNG ĐẠT": "KD", "CẦN XÁC MINH": "XM", "CẦN XÁC NHẬN": "XM", "ĐẠT": "D" };
/** "ĐỀ XUẤT: KHÔNG ĐẠT" -> "KD"; không thấy dòng đề xuất -> "XM". */
export function tachDeXuat(traLoi: string): TrangThai {
  const m = traLoi.match(/ĐỀ\s*XUẤT\s*\**\s*[:：]\s*\**\s*(KHÔNG\s+ĐẠT|CẦN\s+XÁC\s+MINH|CẦN\s+XÁC\s+NHẬN|ĐẠT)/iu);
  return m ? DE_XUAT[m[1].toUpperCase().replace(/\s+/g, " ")] : "XM";
}

/** Chạy `python -m pccc_checker run <pdf> --out <out>` trong thư mục `thamdinh/`; trả dòng JSON tóm tắt cuối stdout. */
export function chayThamDinh(goc: string, pdf: string, out: string, thongTin: Record<string, unknown> = {}, timeoutMs = 10 * 60_000): Promise<TomTat> {
  const args = ["-m", "pccc_checker", "run", pdf, "--out", out, "--pages", "auto", ...thamSoSet(thongTin)];
  const env = { ...process.env, PYTHONIOENCODING: "utf-8", PYTHONUTF8: "1" };
  return new Promise((resolve, reject) => {
    const p = spawn("python", args, { cwd: path.join(goc, "thamdinh"), windowsHide: true, env });
    let so = "", loi = "";
    const t = setTimeout(() => { p.kill(); reject(new Error("Thẩm định quá thời gian")); }, timeoutMs);
    p.stdout.on("data", (d) => (so += d));
    p.stderr.on("data", (d) => (loi += d));
    p.on("error", () => { clearTimeout(t); reject(new Error(HUONG_DAN_PYTHON)); });
    p.on("close", (code) => {
      clearTimeout(t);
      if (code === 0) {
        try { return resolve(JSON.parse(so.trim().split(/\r?\n/).pop() ?? "")); } catch { /* rơi xuống báo lỗi */ }
      }
      // Windows: "python" có thể là lối tắt Microsoft Store (mã 9009, "Python was not found")
      if (code === 9009 || /was not found|không tìm thấy/i.test(loi)) return reject(new Error(HUONG_DAN_PYTHON));
      if (/No module named '?(pymupdf|fitz|yaml)/.test(loi)) return reject(new Error(HUONG_DAN_THU_VIEN));
      reject(new Error(`Lõi thẩm định lỗi (mã ${code}): ${(loi || so).trim().split(/\r?\n/).slice(-3).join(" | ").slice(0, 500)}`));
    });
  });
}
