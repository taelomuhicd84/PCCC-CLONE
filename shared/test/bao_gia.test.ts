import { describe, expect, it } from "vitest";
import { docSoVN, lapBaoGia } from "../src/bao_gia";
import { dm, kl } from "./mau";

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

describe("docSoVN", () => {
  it.each([
    ["1.200", 1200], ["1.200.000", 1200000], ["350,000", 350000], ["3,5", 3.5], ["3.5", 3.5], ["1.200,5", 1200.5],
    ["1,200.5", 1200.5], ["350.000đ", 350000], ["120 000 m²", 120000], ["2 nổi + 1 hầm", 2], ["0.500", 0.5], [12, 12],
    ["", null], ["không rõ", null], [null, null],
  ])("%s -> %s", (vao, ra) => expect(docSoVN(vao)).toBe(ra));
});
