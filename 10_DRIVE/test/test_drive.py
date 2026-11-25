import importlib.util, json, os, sys
from pathlib import Path

_spec = importlib.util.spec_from_file_location("drive", Path(__file__).parents[1] / "drive.py")
drive = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(drive)


def test_so_sanh_them_sua_xoa_doi_cho():
    cu = {"a.pdf": [10, 1], "b.dwg": [20, 1], "c.xlsx": [30, 1]}
    moi = {"a.pdf": [11, 2], "moi/b.dwg": [20, 1], "d.docx": [5, 3]}
    ds = {(e["loai"], e["p"], e.get("tu")) for e in drive.so_sanh(cu, moi, "k", "t")}
    assert ds == {("SUA", "a.pdf", None), ("DOI_CHO", "moi/b.dwg", "b.dwg"), ("THEM", "d.docx", None), ("XOA", "c.xlsx", None)}


def test_quet_lan_dau_roi_thay_doi(tmp_path, monkeypatch):
    kho = tmp_path / "kho"
    (kho / "1_Claude").mkdir(parents=True)
    (kho / "1_Claude" / "bo_qua.txt").write_text("x")
    (kho / "a.pdf").write_text("x")
    monkeypatch.setattr(drive, "DATA", tmp_path / "data")
    drive.ghi_json(drive.DATA / "cau_hinh.json", {"kho": {"k": str(kho)}, "bo_qua": drive.BO_QUA})
    drive.lenh_quet([])
    (kho / "b.dwg").write_text("y")
    drive.lenh_quet([])
    ds = drive.doc_su_kien(1)
    assert [e["loai"] for e in ds] == ["LAN_DAU", "THEM"] and ds[0]["so_file"] == 1 and ds[1]["p"] == "b.dwg"


def test_quy_hoach_lam_roi_hoan_tac(tmp_path, monkeypatch):
    monkeypatch.setattr(drive, "DATA", tmp_path / "data")
    tm = tmp_path / "up"
    tm.mkdir()
    for f in ("ban ve.DWG", "bao gia.xlsx", "ghi chu.txt"):
        (tm / f).write_text("x")
    kh = drive.ke_hoach(tm, drive.SAP_XEP)
    assert sorted(os.path.relpath(d, tm) for _, d in kh) == [os.path.join("BANG_TINH", "bao gia.xlsx"), os.path.join("CAD", "ban ve.DWG")]
    drive.lenh_quy_hoach([str(tm), "--lam"])
    assert (tm / "CAD" / "ban ve.DWG").exists() and not (tm / "ban ve.DWG").exists() and (tm / "ghi chu.txt").exists()
    drive.lenh_hoan_tac([])
    assert (tm / "ban ve.DWG").exists() and (tm / "bao gia.xlsx").exists()
    assert [e["loai"] for e in drive.doc_su_kien(1)].count("HOAN_TAC") == 2


def test_tao_cay(tmp_path, monkeypatch):
    monkeypatch.setattr(drive, "DATA", tmp_path / "data")
    drive.lenh_tao_cay([str(tmp_path), "DA-99"])
    cay = json.loads(drive.CAY.read_text(encoding="utf-8"))["cay"]
    assert all((tmp_path / "DA-99" / cha / con).is_dir() for cha, cons in cay.items() for con in cons)
