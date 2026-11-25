import { describe, expect, it } from "vitest";
import { spawnSync } from "node:child_process";
import { existsSync, mkdtempSync, writeFileSync } from "node:fs";
import os from "node:os";
import path from "node:path";
import { cauHoiXet, chayThamDinh, docKetLuan, tachDeXuat, thamSoSet, xoaKetQuaCu } from "../src/thamdinh";

const GOC = path.resolve(__dirname, "..", "..");
const coPython = spawnSync("python", ["-c", "import pymupdf, yaml"], { windowsHide: true }).status === 0;

describe("thamdinh", () => {
  it("thông tin công trình -> --set: số kiểu Việt Nam, bỏ khoá lạ và ô trống", () => {
    expect(thamSoSet({ height_pccc_m: "24,5", function_group: " f1.3 ", basements: "", has_auto_sprinkler: "có", la: "x" }))
      .toEqual(["--set", "height_pccc_m=24.5", "--set", "function_group=F1.3", "--set", "has_auto_sprinkler=true"]);
  });

  it("đọc findings.jsonl: KD trước, rồi XM, rồi D", () => {
    const f = path.join(mkdtempSync(path.join(os.tmpdir(), "td-")), "findings.jsonl");
    writeFileSync(f, ['{"id":"a","tt":"D","rule":"R1","trang":1}', '{"id":"b","tt":"XM","rule":"R2","trang":2}',
      '{"id":"c","tt":"KD","rule":"R3","trang":3}', ""].join("\n"));
    expect(docKetLuan(f).map((r) => r.id)).toEqual(["c", "b", "a"]);
    expect(docKetLuan(f + ".khong_co")).toEqual([]);
  });

  it("Claude xét: tách đề xuất, câu hỏi trỏ đúng trang PDF và chỉ trích 01_TRI_THUC/, 02_DIEU_KHOAN/", () => {
    expect(tachDeXuat("ĐỀ XUẤT: KHÔNG ĐẠT\nLÝ DO: …")).toBe("KD");
    expect(tachDeXuat("**Đề xuất:** Đạt")).toBe("D");
    expect(tachDeXuat("ĐỀ XUẤT: CẦN XÁC NHẬN")).toBe("XM");
    expect(tachDeXuat("không theo mẫu")).toBe("XM");
    const q = cauHoiXet({ id: "PC-002", tt: "XM", rule: "STAIR-DATA", trang: 4 }, { height_pccc_m: 24.5 }, "12_KET_QUA/A/thamdinh");
    expect(q).toContain('pages: "4"');
    expect(q).toContain("12_KET_QUA/A/thamdinh/ban_ve.pdf");
    expect(q).toContain("CHỈ trích nguyên văn điều khoản có trong 01_TRI_THUC/ hoặc 02_DIEU_KHOAN/");
  });

  it.skipIf(!coPython)("chạy lõi Python trên PDF mẫu có lỗi cài sẵn", async () => {
    const d = mkdtempSync(path.join(os.tmpdir(), "td-"));
    const pdf = path.join(d, "mau.pdf");
    expect(spawnSync("python", [path.join(GOC, "06_THAM_DINH_PDF", "scripts", "make_sample_pdf.py"), pdf], { windowsHide: true }).status).toBe(0);
    const tt = await chayThamDinh(GOC, pdf, d, { height_pccc_m: "24,5" });
    expect(tt.ok).toBe(true);
    expect(tt.thong_tin.height_pccc_m).toBe(24.5);
    expect(tt.dem.KD).toBeGreaterThanOrEqual(4);
    const dong = docKetLuan(path.join(d, "findings.jsonl"));
    const thang = dong.find((r) => r.rule === "STAIR-FLIGHT-WIDTH")!;
    expect(thang.tt).toBe("KD");
    expect(thang.dieu_khoan).toContain("3.4.1");
    expect(dong.some((r) => r.tt === "D")).toBe(true);
  }, 60_000);
});

describe("kết quả cũ không sống sót khi lõi lỗi", () => {
  it("xoaKetQuaCu xoá findings/tom_tat/pdf ghi chú, giữ ban_ve.pdf; lõi lỗi trên file hỏng không còn kết quả cũ", async () => {
    const d = mkdtempSync(path.join(os.tmpdir(), "td-"));
    for (const f of ["findings.jsonl", "tom_tat.json", "claude_xet.json", "ban_ve_annotated.pdf", "extraction.md", "ban_ve.pdf"]) writeFileSync(path.join(d, f), "cu");
    xoaKetQuaCu(d);
    expect(["findings.jsonl", "tom_tat.json", "claude_xet.json", "ban_ve_annotated.pdf", "extraction.md"].some((f) => existsSync(path.join(d, f)))).toBe(false);
    expect(existsSync(path.join(d, "ban_ve.pdf"))).toBe(true);
    if (!coPython) return;
    await expect(chayThamDinh(GOC, path.join(d, "ban_ve.pdf"), d)).rejects.toThrow(); // "cu" không phải PDF
    expect(docKetLuan(path.join(d, "findings.jsonl"))).toEqual([]);
  });
});
