import { describe, expect, it } from "vitest";
import { boBanVe, docSoVN, gopBanVe, lapBaoGia } from "../src/bao_gia";
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
    ["1.200", 1200], ["1.200.000", 1200000], ["350,000.5", 350000.5], ["12,375", 12.375], ["25,125", 25.125], ["1,200,000", 1200000], ["3,5", 3.5], ["3.5", 3.5], ["1.200,5", 1200.5],
    ["1,200.5", 1200.5], ["350.000đ", 350000], ["120 000 m²", 120000], ["2 nổi + 1 hầm", 2], ["0.500", 0.5], [12, 12],
    ["", null], ["không rõ", null], [null, null],
  ])("%s -> %s", (vao, ra) => expect(docSoVN(vao)).toBe(ra));
});

describe("gopBanVe (PCCC_BOC nhiều DWG)", () => {
  const k = (ban_ve: string | undefined, dong: [string, number][], khu?: [string, [string, number][]][]) => ({
    du_an: "X", ban_ve, luc: "2026-11-15T10:00:00", dong: dong.map(([ma, sl]) => ({ ma, sl })),
    ...(khu ? { khu_vuc: khu.map(([ten, d]) => ({ ten, dong: d.map(([ma, sl]) => ({ ma, sl })) })) } : {}),
  });
  const sl = (x: { dong: { ma: string; sl: number }[] }, ma: string) => x.dong.find((d) => d.ma === ma)?.sl;

  it("hai bản vẽ cộng dồn; bóc lại một bản thì thay phần của nó, bản kia giữ", () => {
    let kq = gopBanVe(null, k("t1.dwg", [["A", 5], ["B", 2]], [["Tầng 1", [["A", 5]]]]));
    kq = gopBanVe(kq, k("t2.dwg", [["A", 3]], [["Tầng 1", [["A", 1]]], ["Tầng 2", [["A", 2]]]]));
    expect(kq.ban_ves!.map((b) => b.ban_ve)).toEqual(["t1.dwg", "t2.dwg"]);
    expect(sl(kq, "A")).toBe(8); expect(sl(kq, "B")).toBe(2);
    expect(kq.khu_vuc!.map((x) => [x.ten, sl(x, "A")])).toEqual([["Tầng 1", 6], ["Tầng 2", 2]]);
    kq = gopBanVe(kq, k("T1.DWG", [["A", 1]])); // cùng bản vẽ, khác hoa/thường: thay
    expect(kq.ban_ves!.length).toBe(2);
    expect(sl(kq, "A")).toBe(4); expect(sl(kq, "B")).toBeUndefined();
    expect(kq.ban_ve).toBe("T1.DWG, t2.dwg");
  });
  it("plugin cũ không gửi ban_ve: thay toàn bộ như trước; file cũ không có ban_ves coi là một bản vẽ", () => {
    const cu = gopBanVe(null, k("t1.dwg", [["A", 5]]));
    expect(sl(gopBanVe(cu, k(undefined, [["C", 9]])), "A")).toBeUndefined();
    const fileCu = { du_an: "X", ban_ve: "t1.dwg", luc: "x", dong: [{ ma: "A", sl: 5 }] };
    const kq = gopBanVe(fileCu, k("t2.dwg", [["A", 1]]));
    expect(sl(kq, "A")).toBe(6); expect(kq.ban_ves!.length).toBe(2);
  });
  it("bỏ một bản vẽ tính lại tổng; hết bản vẽ trả null", () => {
    const kq = gopBanVe(gopBanVe(null, k("t1.dwg", [["A", 5]])), k("t2.dwg", [["A", 3]]));
    expect(sl(boBanVe(kq, "T2.dwg")!, "A")).toBe(5);
    expect(boBanVe(boBanVe(kq, "t2.dwg")!, "t1.dwg")).toBeNull();
  });
});
