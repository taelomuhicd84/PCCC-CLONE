import { describe, expect, it } from "vitest";
import { chuanHoaKhaoSat, ghepGoiY, khaoSatTrong, khoiLuongSoBo, maHeThong, tachJsonKhaoSat, uocKhoiLuong, HE_THONG } from "../src/khao_sat";

function mau() {
  const ks = khaoSatTrong("Thử");
  ks.dien_tich_m2 = 1200; ks.so_tang = 2; ks.so_loi_thoat_nan = 2; ks.chieu_cao_m = 8;
  ks.he_thong[0].co = "CÓ";            // báo cháy
  ks.he_thong[1].co = "CẦN XÁC NHẬN";  // họng nước
  ks.he_thong[2].co = "KHÔNG";         // sprinkler
  return ks;
}

describe("maHeThong", () => {
  it("mỗi tên chuẩn khớp đúng mã của nó", () => {
    for (const h of HE_THONG) expect(maHeThong(h.ten)).toBe(h.ma);
  });
  it.each([
    ["Hệ thống báo cháy tự động", "bao_chay"], ["Chữa cháy vách tường", "hvt"], ["Hệ thống sprinkler", "sprinkler"],
    ["Tăng áp buồng thang", "hut_khoi"], ["Đèn EXIT", "den"], ["Cửa chống cháy", "ngan_chay"], ["Bể nước chữa cháy", null],
  ])("%s -> %s", (ten, ma) => expect(maHeThong(ten)).toBe(ma));
});

describe("chuanHoaKhaoSat + ghepGoiY", () => {
  it("chuẩn hoá số, trạng thái, xếp hệ thống theo thứ tự, giữ hệ lạ và chữ không nhận ra", () => {
    const k = chuanHoaKhaoSat({
      dien_tich_m2: "1.200", so_tang: "2 tầng", dien_tham_dinh: "có (Phụ lục III mục 9)", thoat_nan_danh_gia: "Chưa đạt",
      he_thong: [{ ten: "Bình chữa cháy", co: "Có" }, { ten: "Hệ thống báo cháy", co: "cần xác nhận", can_cu: "Bảng A.1" }, { ten: "Bể nước chữa cháy", co: "CÓ" }, { ten: "Chống sét", co: "tuỳ" }],
    }, "Thử");
    expect(k).toMatchObject({ du_an: "Thử", dien_tich_m2: 1200, so_tang: 2, dien_tham_dinh: "CÓ", dien_tham_dinh_can_cu: "có (Phụ lục III mục 9)", thoat_nan_danh_gia: "CHƯA ĐẠT", thoat_nan_ghi_chu: "", hang_muc: "" });
    expect(k.he_thong.map((h) => h.ten).slice(0, HE_THONG.length)).toEqual(HE_THONG.map((h) => h.ten));
    expect(k.he_thong[0]).toEqual({ ten: "Báo cháy tự động", co: "CẦN XÁC NHẬN", can_cu: "Bảng A.1", ghi_chu: "" });
    expect(k.he_thong.find((h) => h.ten === "Bình chữa cháy")!.co).toBe("CÓ");
    expect(k.he_thong.find((h) => h.ten === "Chống sét")).toMatchObject({ co: "", ghi_chu: "tuỳ" });
    expect(k.he_thong.at(-1)).toMatchObject({ ten: "Bể nước chữa cháy", co: "CÓ" });
    expect(chuanHoaKhaoSat(null, "X").he_thong).toHaveLength(HE_THONG.length);
  });
  it("gợi ý Claude: giữ số liệu người dùng, lấy đánh giá của Claude, Claude trống thì giữ của người dùng", () => {
    const nd = mau();
    nd.hang_muc = "Nhà xưởng"; nd.he_thong[9].co = "CÓ"; nd.he_thong.push({ ten: "Hạng mục riêng", co: "CÓ", can_cu: "", ghi_chu: "" });
    const cl = chuanHoaKhaoSat({ hang_muc: "Xưởng may", dien_tich_m2: 999, cong_nang: "F5.1", he_thong: [{ ten: "Báo cháy tự động", co: "KHÔNG", can_cu: "x" }] }, "Thử");
    const k = ghepGoiY(nd, cl);
    expect(k).toMatchObject({ hang_muc: "Nhà xưởng", dien_tich_m2: 1200, cong_nang: "F5.1", so_tang: 2 });
    expect(k.he_thong[0]).toMatchObject({ co: "KHÔNG", can_cu: "x" });   // đánh giá của Claude
    expect(k.he_thong[1].co).toBe("CẦN XÁC NHẬN");                        // Claude trống -> giữ người dùng
    expect(k.he_thong[9].co).toBe("CÓ");
    expect(k.he_thong.at(-1)!.ten).toBe("Hạng mục riêng");
  });
});

describe("uocKhoiLuong", () => {
  it("chỉ ước hệ CÓ / CẦN XÁC NHẬN, làm tròn lên, ghi cách ước", () => {
    const d = uocKhoiLuong(mau());
    const sl = Object.fromEntries(d.map((x) => [x.ma, x.sl]));
    expect(sl).toEqual({ BC_TT: 1, BC_KHOI: 20, BC_TOHOP: 4, DAY_BC: 720, HVT_TU: 4, ONG_DN50: 38, HS_THIET_KE: 1 });
    expect(d.find((x) => x.ma === "HVT_TU")!.cho_xac_nhan).toBe(true);
    expect(khoiLuongSoBo(mau()).ghi_chu.BC_KHOI).toBe("ước: 1.200 m² / 60");
  });
  it("thuộc diện thẩm định thì có gói thẩm duyệt + nghiệm thu; bể nước lấy theo dung tích", () => {
    const ks = mau(); ks.dien_tham_dinh = "CÓ"; ks.be_nuoc_m3 = 150;
    const ma = uocKhoiLuong(ks).map((x) => x.ma);
    expect(ma).toEqual(expect.arrayContaining(["HS_THAM_DUYET", "HS_NGHIEM_THU", "BE_NUOC"]));
  });
  it("chưa có chiều cao thì ước 3,5 m/tầng nổi (không cộng hầm), ghi rõ là ước", () => {
    const ks = mau(); ks.chieu_cao_m = null; ks.so_tang_ham = 1;
    const ong = uocKhoiLuong(ks).find((x) => x.ma === "ONG_DN50")!;
    expect(ong.sl).toBe(7 + 15 * 3);
    expect(ong.cach_uoc).toContain("ước 2 tầng × 3,5");
  });
  it("không có diện tích thì không ước", () => {
    const ks = mau(); ks.dien_tich_m2 = null;
    expect(uocKhoiLuong(ks)).toEqual([]);
  });
});

describe("tachJsonKhaoSat", () => {
  it("lấy khối json khao_sat cuối, bỏ khối hỏng, đọc được JSON không rào", () => {
    expect(tachJsonKhaoSat('chữ\n```json\n{"khao_sat":{"hang_muc":"A"}}\n```\n```json\n{hỏng}\n```')).toEqual({ hang_muc: "A" });
    expect(tachJsonKhaoSat('Kết quả: {"khao_sat": {"hang_muc": "B"}} hết.')).toEqual({ hang_muc: "B" });
    expect(tachJsonKhaoSat("không có")).toBeNull();
  });
});
