import { describe, expect, it } from "vitest";
import path from "node:path";
import { docModule, maQuyChuan, thongKe } from "../src/thong_ke_du_an";

describe("thống kê dự án", () => {
  it("gom mã quy chuẩn: bỏ hậu tố, mỗi mã 1 lần", () => {
    const s = maQuyChuan("QCVN 06:2022/BXD và QCVN 06:2022; TCVN 7568-14:2025, TCVN 7336, Nghị định 105/2025/NĐ-CP, NĐ 347/2026, Thông tư 17/2021/TT-BCA");
    expect([...s].sort()).toEqual(["NĐ 105/2025", "NĐ 347/2026", "QCVN 06:2022", "TCVN 7336", "TCVN 7568-14:2025", "TT 17/2021"]);
  });
  it("đọc bảng module và đếm tệp thật", () => {
    expect(docModule("## Mỗi module là một skill (x)\n| Module | Skill | Agent |\n|---|---|---|\n| Thủy lực | `tkpccc-thuy-luc` | |\n\n## Quy tắc")).toEqual([{ ten: "Thủy lực", skill: "tkpccc-thuy-luc" }]);
    const k = thongKe(path.resolve(__dirname, "..", ".."));
    expect(k.tong.tep).toBeGreaterThan(100);
    expect(k.skill.length).toBeGreaterThanOrEqual(12);
    expect(k.hang_muc.find((h) => h.skill === "tkpccc-thuy-luc")!.tep).toBeGreaterThan(0);
    expect(k.hang_muc.reduce((a, h) => a + h.tep, 0)).toBe(k.tong.tep); // mỗi tệp đúng 1 hạng mục
    expect(k.quy_chuan.some((q) => q.ma === "QCVN 06:2022")).toBe(true);
  });
});
