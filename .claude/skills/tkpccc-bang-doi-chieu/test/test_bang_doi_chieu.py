"""Kiểm bang_doi_chieu.py: chọn bảng, lập file kết quả, gộp phần agent, kiểm, xuất Excel/markdown.
Chạy: python -m pytest .claude/skills/tkpccc-bang-doi-chieu/test -q"""
import importlib.util
import json
from pathlib import Path

import pytest

_spec = importlib.util.spec_from_file_location("bdc", Path(__file__).parents[1] / "scripts" / "bang_doi_chieu.py")
bdc = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(bdc)


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

    phan = tmp_path / "phan_kt.json"
    phan.write_text(json.dumps({"ma": "KT7.1", "cong_trinh": {"nhom_f": "F1.2"},
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
    ks[3]["ket_luan"] = "SAI"
    assert any("không hợp lệ" in x for x in bdc.kiem(dc)["loi"])
    ks[3]["ket_luan"] = ""
    k = bdc.kiem(dc)
    assert k["ok"] and any("chưa có khung" in x for x in k["canh_bao"])

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


def test_gop_bao_loi_idx(tmp_path):
    f = tmp_path / "dc.json"
    bdc.lamviec(["KS"], f, "khao_sat")
    phan = tmp_path / "p.json"
    phan.write_text(json.dumps({"ma": "KS", "dong": [{"idx": 999, "ket_luan": "DAT"}]}), encoding="utf-8")
    with pytest.raises(SystemExit):
        bdc.gop(f, [phan], True)
