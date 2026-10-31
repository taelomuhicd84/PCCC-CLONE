import { describe, expect, it } from "vitest";
import { khaoSatTrong, khoiLuongSoBo, tachJsonKhaoSat, uocKhoiLuong } from "./khao_sat";
import { xuatKhaoSat } from "./word";

function mau() {
  const ks = khaoSatTrong("Thử");
  ks.dien_tich_m2 = 1200; ks.so_tang = 2; ks.so_loi_thoat_nan = 2; ks.chieu_cao_m = 8;
  ks.he_thong[0].co = "CÓ";            // báo cháy
  ks.he_thong[1].co = "CẦN XÁC NHẬN";  // họng nước
  ks.he_thong[2].co = "KHÔNG";         // sprinkler
  return ks;
}

describe("uocKhoiLuong", () => {
  it("chỉ ước hệ CÓ / CẦN XÁC NHẬN, làm tròn lên, ghi cách ước", () => {
    const d = uocKhoiLuong(mau());
    const sl = Object.fromEntries(d.map((x) => [x.ma, x.sl]));
    expect(sl).toEqual({ BC_TT: 1, BC_KHOI: 20, BC_TOHOP: 4, DAY_BC: 720, HVT_TU: 4, ONG_DN50: 38 });
    expect(d.find((x) => x.ma === "HVT_TU")!.cho_xac_nhan).toBe(true);
    expect(khoiLuongSoBo(mau()).ghi_chu.BC_KHOI).toBe("ước: 1200 m² / 60");
  });
  it("không có diện tích thì không ước", () => {
    const ks = mau(); ks.dien_tich_m2 = null;
    expect(uocKhoiLuong(ks)).toEqual([]);
  });
  it("tên hệ thống Claude trả khác hoa thường vẫn khớp", () => {
    const ks = mau(); ks.he_thong[0].ten = "báo cháy tự động.";
    expect(uocKhoiLuong(ks).some((x) => x.ma === "BC_KHOI")).toBe(true);
  });
});

describe("tachJsonKhaoSat", () => {
  it("lấy khối json khao_sat cuối, bỏ khối hỏng", () => {
    const t = 'chữ\n```json\n{"khao_sat":{"hang_muc":"A"}}\n```\n```json\n{hỏng}\n```';
    expect(tachJsonKhaoSat(t)).toEqual({ hang_muc: "A" });
    expect(tachJsonKhaoSat("không có")).toBeNull();
  });
});

describe("xuatKhaoSat", () => {
  it("tạo file docx", async () => {
    const b = await xuatKhaoSat(mau());
    expect(b.subarray(0, 2).toString()).toBe("PK");
  });
});
