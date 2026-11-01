"""python -m pytest thamdinh/test -q  (cần pymupdf, pyyaml)"""
import json
import sys
from pathlib import Path

import pymupdf

GOC = Path(__file__).resolve().parents[1]
sys.path[:0] = [str(GOC), str(GOC / "scripts")]

from make_sample_pdf import FONT_CANDIDATES, make  # noqa: E402
from pccc_checker.cli import main  # noqa: E402
from pccc_checker.pipeline.consistency import read_jsonl  # noqa: E402


def chay(pdf, out, capsys, *them):
    main(["run", str(pdf), "--out", str(out), "--pages", "all", *them])
    return json.loads(capsys.readouterr().out.strip().splitlines()[-1]), read_jsonl(out / "findings.jsonl")


def test_pdf_mau_co_loi_cai_san(tmp_path, capsys):
    tom_tat, rows = chay(make(tmp_path / "mau.pdf"), tmp_path / "kq", capsys)
    assert (tmp_path / "kq" / tom_tat["pdf_ghi_chu"]).exists()
    kd = {r["rule"]: r for r in rows if r["tt"] == "KD"}
    assert {"STAIR-FLIGHT-WIDTH", "STAIR-TREAD", "EXIT-DOOR-WIDTH"} <= kd.keys()
    thang = kd["STAIR-FLIGHT-WIDTH"]
    assert thang["trang"] == 1 and thang["bbox"] and thang["evid"] and "3.4.1" in thang["dieu_khoan"]
    assert (thang["yc"], thang["ht"]) == (0.9, 0.85)
    assert all(r["trang"] and r["bbox"] and r["evid"] for r in kd.values())
    assert any(r["tt"] == "D" and r["rule"] == "EXIT-DOOR-HEIGHT" for r in rows)
    assert tom_tat["thong_tin"]["height_pccc_m"] == 24.5
    assert not any("height_pccc_m" in a for a in tom_tat["thong_tin"].get("assumptions", []))   # 24.5 có trên bản vẽ


def test_khong_doc_duoc_so_lieu_la_xm(tmp_path, capsys):
    doc = pymupdf.open()
    page = doc.new_page(width=1191, height=842)
    font = next((f for f in FONT_CANDIDATES if Path(f).exists()), None)
    kw = {"fontfile": font, "fontname": "vn"} if font else {}
    page.draw_rect(pymupdf.Rect(120, 380, 220, 580), width=1)
    page.insert_text((125, 470), "THANG BỘ TB2", fontsize=9, **kw)
    page.insert_text((960, 670), "MẶT BẰNG TẦNG 2", fontsize=10, **kw)
    page.insert_text((960, 690), "TỈ LỆ 1:100", fontsize=9, **kw)
    doc.save(tmp_path / "thieu.pdf")

    tom_tat, rows = chay(tmp_path / "thieu.pdf", tmp_path / "kq", capsys)
    assert not any(r["tt"] == "KD" for r in rows if r["rule"] in ("STAIR-DATA", "BUILDING-INFO"))
    assert any(r["tt"] == "XM" and r["rule"] == "STAIR-DATA" for r in rows)
    # thiếu chiều cao PCCC -> luật phụ thuộc chiều cao thành XM, không im lặng; dòng này không được vẽ lên PDF nên không có số #
    thieu_cao = [r for r in rows if r["tt"] == "XM" and "chiều cao PCCC" in r["ly_do"]]
    assert thieu_cao and all("so" not in r for r in thieu_cao)
    assert any(r.get("so") for r in rows if r["rule"] == "STAIR-DATA")

    tom_tat, rows = chay(tmp_path / "thieu.pdf", tmp_path / "kq2", capsys, "--set", "height_pccc_m=12")
    assert tom_tat["thong_tin"]["height_pccc_m"] == 12
    assert not any("chiều cao PCCC (height_pccc_m)" in r["ly_do"] for r in rows)
