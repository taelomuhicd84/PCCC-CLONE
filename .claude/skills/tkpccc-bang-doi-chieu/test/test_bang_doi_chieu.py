"""Kiểm bang_doi_chieu.py: chọn bảng, lập file kết quả, gộp phần agent, kiểm, xuất Excel/markdown.
Chạy: python -m pytest .claude/skills/tkpccc-bang-doi-chieu/test -q"""
import importlib.util
import json
from pathlib import Path

import pytest

_spec = importlib.util.spec_from_file_location("bdc", Path(__file__).parents[1] / "scripts" / "bang_doi_chieu.py")
bdc = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(bdc)


NGUON = {"trang": 2, "trich": "NHÓM NHÀ: F1.2", "loai": "ban_ve_chu", "do_tin_cay": "cao", "xac_nhan": "claude"}

def test_chon_bang():
    r = bdc.chon("Căn hộ dịch vụ, khách sạn (F1.2)", 18.4, {})
    assert r["xay_dung"]["ma"] == "KT7.1" and r["cong_nang"]["ma"] == "A5"
    ma = [x["ma"] for x in r["he_thong"]]
    assert ma == ["B1", "B5", "B6", "B12", "B13", "B14"]          # báo cháy, họng, sprinkler chưa rõ -> vẫn xét
    assert bdc.chon("khách sạn", 40, {"sprinkler": False, "hong_nuoc": True})["xay_dung"]["ma"] == "KT7.2"
    assert "B6" not in [x["ma"] for x in bdc.chon("khách sạn", 10, {"sprinkler": False})["he_thong"]]
    assert bdc.chon("chung cư", 80, {})["xay_dung"]["ma"] == "KT4.3"
    assert bdc.chon("nhà ở riêng lẻ", None, {})["cong_nang"]["ma"] is None


def test_lamviec_gop_kiem_xuat(tmp_path):
    f = tmp_path / "doi_chieu.json"
    kq = bdc.lamviec(["KS"], f, "khao_sat")
    assert kq["bang"] == [("KS", 27)]                              # 32 dòng, 5 dòng nhóm I–V
    bdc.lamviec(["KS", "KT7.1"], f, "khao_sat")                   # chạy lại: không nhân đôi bảng
    dc = json.loads(f.read_text(encoding="utf-8"))
    assert [b["ma"] for b in dc["bang"]] == ["KS", "KT7.1"]
    idx = next(r["idx"] for r in dc["bang"][1]["dong"] if r["xet"])
    bdc.main(["job", str(f), "KT7.1", "--tu", "0", "--den", "70"])
    jb = (tmp_path / "jobs" / "KT7.1_0-70.md").read_text(encoding="utf-8")
    assert "phần XÂY DỰNG" in jb and f"| {idx} |" in jb and str(tmp_path / "phan" / "KT7.1_0-70.json") in jb
    assert str(tmp_path / "cong_trinh.md") in jb and '"cong_nang"' not in jb      # thông tin công trình chỉ ở file chung
    assert '"cong_nang"' in (tmp_path / "cong_trinh.md").read_text(encoding="utf-8")
    assert "## Điều khoản nguyên văn" in jb and "\n> " in jb                      # điều khoản trích sẵn, agent khỏi tra
    assert (tmp_path / "quy_tac_agent.md").read_text(encoding="utf-8").count("\n## ") >= 6

    phan = tmp_path / "phan_kt.json"
    phan.write_text(json.dumps({"ma": "KT7.1", "de_xuat_thong_so": [{"truong": "nhom_f", "gia_tri": "F1.2", "nguon": NGUON}],
                                "dong": [{"idx": idx, "thiet_ke": "Thang rẽ quạt", "ket_luan": "CHUA_DAT", "ly_do": "cao 18,4 m > 15 m",
                                          "phuong_an": ["PA1: sửa vế thẳng"], "khung": [7]}],
                                "ghi_chu": [{"khung": 7, "trang": 1, "kieu": "khung", "hop": [0, 0, 10, 10]}]}, ensure_ascii=False),
                    encoding="utf-8")
    assert bdc.gop(f, [phan], False)["dong_da_gop"] == 1
    dc = json.loads(f.read_text(encoding="utf-8"))
    r = next(r for r in dc["bang"][1]["dong"] if r["idx"] == idx)
    assert r["khung"] == [1] and dc["ghi_chu"][0]["khung"] == 1     # khung 7 của agent -> số 1 toàn hồ sơ
    assert dc["cong_trinh"]["nhom_f"] == "F1.2"

    ks = dc["bang"][0]["dong"]
    ks[2].update(ket_luan="CAN_XAC_NHAN", ly_do="", khung=[])     # thiếu lý do -> lỗi; chưa khung -> cảnh báo
    k = bdc.kiem(dc)
    assert not k["ok"] and any("thiếu lý do" in x for x in k["loi"])
    assert k["dem"]["KT7.1"]["CHUA_DAT"] == 1 and k["dem"]["KS"]["chua_xet"] == 26
    ks[2]["ly_do"] = "chưa có mặt cắt"
    ks[1].update(ly_do="6 tầng nổi + 1 hầm")                    # dòng thông tin (ks[0] là dòng nhóm I): giá trị là kết luận
    assert ks[1]["thong_tin"] and bdc.o_ket_luan(ks[1]) == "6 tầng nổi + 1 hầm"
    dc["ghi_chu"].append({"khung": [3, "11a"], "trang": 1, "kieu": "chu", "hop": [0, 0, 9, 9]})   # một ghi chú cho nhiều dòng
    ks[7].update(ket_luan="CHUA_DAT", ly_do="x", phuong_an=["y"], khung=[3])   # dòng thường (ks[3] là dòng thông tin)
    ks[3]["ket_luan"] = "SAI"
    assert any("không hợp lệ" in x for x in bdc.kiem(dc)["loi"])
    ks[3]["ket_luan"] = ""
    assert any("cong_trinh.cong_nang: thiếu giá trị" in x for x in bdc.kiem(dc)["loi"])
    dc["cong_trinh"]["thieu"] = ["cong_nang", "so_tang_noi", "chieu_cao_pccc_m"]   # bản vẽ không có: ghi vào thieu
    k = bdc.kiem(dc)
    assert k["ok"] and any("chưa có khung" in x for x in k["canh_bao"])
    assert k["dem"]["KS"]["xac_dinh"] == 1 and not any("khung 3" in x for x in k["canh_bao"])
    assert any("khung 11a: có trên PDF" in x for x in k["canh_bao"])

    m = bdc.md(dc, None, True, 0)
    assert "CHƯA ĐẠT: cao 18,4 m > 15 m · PA1: sửa vế thẳng" in m and "CẦN XÁC NHẬN" in m

    from openpyxl import load_workbook
    out = tmp_path / "dc.xlsx"
    bdc.xlsx(dc, out)
    wb = load_workbook(out)
    assert wb.sheetnames == ["Tóm tắt", "KS", "KT7.1"]
    s = wb["KT7.1"]
    hang = next(row for row in s.iter_rows(min_row=3) if row[5].value and row[5].value.startswith("CHƯA ĐẠT"))
    assert hang[5].fill.fgColor.rgb.endswith("F8CBAD") and hang[6].value == "1"


def test_bcks_dien_mau(tmp_path):
    import pymupdf
    from openpyxl import load_workbook

    doc = pymupdf.open()
    doc.new_page(width=100, height=300)                                    # trang dọc: ảnh cắt rất cao -> phải thu nhỏ
    doc.save(tmp_path / "ban_ve.pdf")
    dc = {"ban_ve": "ban_ve.pdf", "zoom": 1,
          "bcks": {"tieu_de": {"ten_cong_trinh": "Nhà A", "khach_hang": "Ông B", "dia_diem": "Q.4"},
                   "muc": {"thong_tin": "Quy mô: 3 tầng", "thoat_nan": "- dòng\n" * 30},
                   "ghi_chu": {"thoat_nan": {"chu": "- ảnh", "anh": [{"trang": 1, "hop": [0, 0, 100, 300]}]}}}}
    mau7 = bdc.MAU_BCKS.parent / "BCKS_mau_7muc.xlsx"                     # mẫu 7 mục có hàng THOÁT NẠN để thử ảnh
    kq = bdc.bcks(dc, tmp_path / "doi_chieu.json", tmp_path / "BCKS.xlsx", mau7)
    ws = load_workbook(tmp_path / "BCKS.xlsx").worksheets[0]
    assert "Nhà A" in ws["A8"].value and "Ông B" in ws["A8"].value and ws["F10"].value == "Quy mô: 3 tầng"
    assert ws["A2"].value.startswith("CÔNG TY") and kq["anh"] == 1 and len(ws._images) == 2   # logo của mẫu + 1 ảnh
    assert ws.row_dimensions[15].height == 409                             # chữ dài: chạm giới hạn hàng của Excel
    a = next(i.anchor for i in ws._images if i.anchor._from.row == 14)
    assert a._from.col == 8 and a._from.rowOff / 12700 + a.ext.height / 9525 * bdc.PT_ANH <= 409   # ảnh nằm gọn trong ô I15


def test_gop_bao_loi_idx(tmp_path):
    f = tmp_path / "dc.json"
    bdc.lamviec(["KS"], f, "khao_sat")
    phan = tmp_path / "p.json"
    phan.write_text(json.dumps({"ma": "KS", "dong": [{"idx": 999, "ket_luan": "DAT"}]}), encoding="utf-8")
    with pytest.raises(SystemExit):
        bdc.gop(f, [phan], True)


def test_trich_dieu_nguyen_van():
    assert bdc._dieu_trong("Mục G.1 QCVN 06:2022 / Mục G.2a / QCVN 06:2022") == [("G.1", "QCVN 06"), ("G.2a", "QCVN 06")]
    assert bdc._dieu_trong("Bảng B.1 mục 1.2 / QCVN 10:2025") == [("B.1", "QCVN 10")]   # "mục 1.2" là dòng của bảng, không phải điều
    noi, doan = bdc._trich_dieu("6.2.1.1", bdc._file_tim("", "QCVN 06"))
    assert noi.startswith("01_TRI_THUC/qcvn06-dieu-6.md") and doan.startswith("6.2.1.1 ") and "6.2.1.2" not in doan


def test_bcks_mau_4_muc(tmp_path):
    from openpyxl import load_workbook
    dc = {"bcks": {"tieu_de": {"ten_cong_trinh": "Kho A"},
                   "muc": {"thong_tin": "Quy mô: kho", "phap_ly": "- => cần thẩm định", "he_thong": "- Báo cháy",
                           "tu_van": "+ Cung cấp mặt cắt", "thoat_nan": "- dòng"}}}
    kq = bdc.bcks(dc, tmp_path / "doi_chieu.json", tmp_path / "BCKS.xlsx", bdc.MAU_BCKS.parent / "BCKS_mau_4muc.xlsx")   # mẫu 4 mục cũ (kho hoá chất)
    ws = load_workbook(tmp_path / "BCKS.xlsx").worksheets[0]
    assert [ws[f"F{r}"].value for r in (10, 11, 12, 13)] == ["Quy mô: kho", "- => cần thẩm định", "- Báo cháy", "+ Cung cấp mặt cắt"]
    assert kq["muc_mau_khong_co"] == ["thoat_nan"]


def test_bcks_mau_mac_dinh_8_muc(tmp_path):
    from openpyxl import load_workbook
    dc = {"bcks": {"muc": {"thong_tin": "a", "khoang_cach": "b", "giao_thong": "c", "thoat_nan": "d", "ngan_chay": "e", "he_thong": "f", "tu_van": "g"}}}
    kq = bdc.bcks(dc, tmp_path / "doi_chieu.json", tmp_path / "BCKS.xlsx")
    ws = load_workbook(tmp_path / "BCKS.xlsx").worksheets[0]
    assert [ws[f"F{r}"].value for r in (10, 12, 13, 14, 15, 16, 17)] == list("abcdefg") and kq["muc_mau_khong_co"] == []


def _trich(ma: str, idx: int) -> list:
    """[(số điều, văn bản, file:dòng hoặc None)] mà lệnh job sẽ trích cho một dòng của bảng."""
    b = json.loads((Path(__file__).parents[1] / "data" / f"{ma}.json").read_text(encoding="utf-8"))
    r = next(r for r in b["dong"] if r["idx"] == idx)
    ra = []
    for so, vb in bdc._dieu_trong(r.get("khoan_dieu")):
        kq = bdc._trich_dieu(so, bdc._file_tim(r.get("tra_o"), vb)) if vb in bdc.VAN_BAN else None
        ra.append((so, vb, kq[0] if kq else None))
    return ra


@pytest.mark.parametrize("ma, idx", [("B1", 2), ("B13", 3), ("B14", 18), ("B12", 3), ("A18", 74), ("A25", 2),
                                     ("A24", 25), ("KT4.1", 135), ("A17", 88), ("A20", 92)])
def test_job_khong_trich_nham_van_ban(ma, idx):
    # R2/R5 15/11: điều của TCVN 7568-14, TCVN 13456, TCVN 7435, QCVN 10, QCVN 72, QCVN 21, TCVN 6396, QCVN 01
    # từng bị trích sang file QCVN 06 cùng số điều (theo cột "Tra ở" hoặc văn bản của phần kế bên)
    for so, vb, noi in _trich(ma, idx):
        if noi:
            assert vb in bdc.VAN_BAN and any(Path(noi.split(":")[0]).match(g) for g in bdc.VAN_BAN[vb][1]), (so, vb, noi)
        assert not (noi or "").startswith("02_DIEU_KHOAN/") or vb == "QCVN 06", (so, vb, noi)


def test_van_ban_khac_khong_thua_ke():
    assert bdc._dieu_trong("Phụ lục A.4, / Đ 7.1 TCVN / 6396-72:2010 / Đ 6.13 / QCVN 06:2022") == \
        [("A.4", "TCVN 6396-72"), ("7.1", "TCVN 6396-72"), ("6.13", "QCVN 06")]
    assert bdc._dieu_trong("Điều 4.5 / TCVN 13456:2022") == [("4.5", "TCVN 13456")]
    assert bdc._dieu_trong("Đ3.2.6.2 / QCVN06:2022") == [("3.2.6.2", "QCVN 06")]     # số dính liền chữ Đ
    assert ("2.1.2", "QCVN 01") in bdc._dieu_trong("QCVN 10:2025/BCA (tra điều tương ứng) / Điều 2.1.2 / QCVN 01:2019/BCA")


@pytest.mark.parametrize("ma", ["A2", "KS"])
def test_job_trich_dinh_nghia_chieu_cao_pccc(tmp_path, ma):
    # 1.4.9 (chiều cao PCCC: mép dưới cửa sổ tầng trên cùng, tính từ mặt đường xe chữa cháy) nằm ở 01_TRI_THUC/dinh-nghia
    f = tmp_path / "doi_chieu.json"
    bdc.lamviec([ma], f, "khao_sat")
    kq = bdc.job(f, ma, 0, 5, tmp_path / "job.md", tmp_path / "phan.json")
    t = (tmp_path / "job.md").read_text(encoding="utf-8")
    assert "01_TRI_THUC/dinh-nghia/dinh-nghia.md" in t and "mép dưới của lỗ cửa (cửa sổ)" in t, kq
    if ma == "KS":
        ks = json.loads((Path(__file__).parents[1] / "data" / "KS.json").read_text(encoding="utf-8"))
        assert "mép dưới" in next(r for r in ks["dong"] if r["idx"] == 2)["goi_y"]


def test_chon_thieu_chieu_cao_khong_lay_bang_thap():
    r = bdc.chon("khách sạn", None, {})
    assert r["xay_dung"]["ma"] is None and r["xay_dung"]["can_chon"] == ["KT7.1", "KT7.2", "KT7.3"]
    assert "chiều cao PCCC" in r["xay_dung"]["ly_do"] and "KT7.1" not in r["lenh_lamviec"].split(",")
    assert r["thieu"] == ["chieu_cao_pccc_m"]
    assert bdc.chon("khách sạn", (18, 24), {})["xay_dung"]["ma"] == "KT7.1"          # khoảng nằm gọn một mốc
    vat = bdc.chon("khách sạn", (26, 31), {})["xay_dung"]                             # khoảng vắt mốc 28 m
    assert vat["ma"] is None and vat["can_chon"] == ["KT7.1", "KT7.2"]
    assert bdc.chon("nhà trẻ", None, {})["xay_dung"]["ma"] == "KT8"                     # bảng không chia mốc
    assert bdc._cao("18,4") == 18.4 and bdc._cao("15-21") == (15.0, 21.0) and bdc._cao("15,5 – 21") == (15.5, 21.0)
    assert bdc.chon("khách sạn", 28, {})["xay_dung"]["ma"] == "KT7.1"                   # "đến 28m" gồm 28


def test_gop_kiem_thong_so_cong_trinh(tmp_path):
    f = tmp_path / "dc.json"
    bdc.lamviec(["KS"], f, "khao_sat")
    p = tmp_path / "p.json"
    p.write_text(json.dumps({"ma": "KS", "de_xuat_thong_so": [{"truong": "chieu_cao_pccc_m", "gia_tri": 18.4,
                                                               "nguon": {"trang": 5, "trich": ""}}]}), encoding="utf-8")
    with pytest.raises(SystemExit, match="trich"):          # thiếu chữ nguyên văn -> không gộp, file không đổi
        bdc.gop(f, [p], True)
    p.write_text(json.dumps({"ma": "KS", "cong_trinh": {"so_tang_noi": 6}}), encoding="utf-8")
    with pytest.raises(SystemExit, match="so_tang_noi"):    # kiểu cũ ghi thẳng cong_trinh không kèm nguồn
        bdc.gop(f, [p], True)
    assert json.loads(f.read_text(encoding="utf-8"))["cong_trinh"]["so_tang_noi"] is None

    tot = [{"truong": "chieu_cao_pccc_m", "gia_tri": 18.4, "nguon": dict(NGUON, trang=5, trich="+17.200 + 1,0 + 0,2", loai="do_hinh")},
           {"truong": "nhom_f", "gia_tri": "F1.2", "nguon": NGUON}, {"truong": "cong_nang", "gia_tri": "khách sạn", "nguon": NGUON},
           {"truong": "so_tang_noi", "gia_tri": 6, "nguon": dict(NGUON, loai="nguoi_dung", trang=None, xac_nhan="nguoi_dung", trich="người dùng: 6 tầng")}]
    p.write_text(json.dumps({"ma": "KS", "de_xuat_thong_so": tot}, ensure_ascii=False), encoding="utf-8")
    bdc.gop(f, [p], True)
    dc = json.loads(f.read_text(encoding="utf-8"))
    assert dc["cong_trinh"]["chieu_cao_pccc_m"] == 18.4 and dc["cong_trinh"]["nguon"]["chieu_cao_pccc_m"]["trang"] == 5
    assert not bdc.kiem_cong_trinh(dc["cong_trinh"])[0]

    p.write_text(json.dumps({"ma": "KS", "de_xuat_thong_so": [dict(tot[0], gia_tri=21.0)]}), encoding="utf-8")
    bdc.gop(f, [p], True)                                    # giá trị khác giá trị đã có -> xung đột, không ghi đè
    dc = json.loads(f.read_text(encoding="utf-8"))
    assert dc["cong_trinh"]["chieu_cao_pccc_m"] == 18.4
    assert any("xung đột" in x for x in bdc.kiem(dc)["loi"])
    dc["cong_trinh"].pop("xung_dot")
    dc["cong_trinh"]["nguon"]["nhom_f"] = "trang 2"          # nguồn dạng chuỗi cũ: cảnh báo, không lỗi
    l, cb = bdc.kiem_cong_trinh(dc["cong_trinh"])
    assert not l and any("nguồn dạng chuỗi" in x for x in cb)
    dc["cong_trinh"]["nguon"].pop("cong_nang")
    assert any("cong_trinh.cong_nang" in x for x in bdc.kiem_cong_trinh(dc["cong_trinh"])[0])
