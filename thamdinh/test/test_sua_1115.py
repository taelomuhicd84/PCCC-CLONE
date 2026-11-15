"""Ca hồi quy cho các lỗi review 15/11 (R5 mục 2, R1 thang có nhãn). python -m pytest thamdinh/test -q"""
import json
import sys
from pathlib import Path

import pymupdf
import pytest

GOC = Path(__file__).resolve().parents[1]
sys.path[:0] = [str(GOC), str(GOC / "scripts")]

from make_sample_pdf import FONT_CANDIDATES  # noqa: E402
from pccc_checker.cli import main  # noqa: E402
from pccc_checker.pdf.classify import classify_page  # noqa: E402
from pccc_checker.pipeline.consistency import read_jsonl  # noqa: E402

FONT = next((f for f in FONT_CANDIDATES if Path(f).exists()), None)
KW = {"fontfile": FONT, "fontname": "vn"} if FONT else {}
KHUNG = [(960, 670, "MẶT BẰNG TẦNG 3"), (960, 690, "TỈ LỆ 1:100")] + \
        [(400, 150 + 14 * i, f"PHÒNG LÀM VIỆC SỐ {i}") for i in range(1, 6)]   # đủ chữ để phân loại trang
CONG_TRINH = ["fire_resistance_level=II", "structural_hazard_class=S0", "floors_above=5", "basements=0"]


def pdf(path: Path, texts, w=1191, h=842, rot=0, rect=(100, 100, 900, 600)) -> Path:
    doc = pymupdf.open()
    page = doc.new_page(width=w, height=h)
    page.draw_rect(pymupdf.Rect(*rect), width=2)
    for x, y, s in texts:
        page.insert_text((x, y), s, fontsize=9, **KW)
    if rot:
        page.set_rotation(rot)
    doc.save(path)
    return path


def chay(tmp_path, capsys, ten, texts, *dat, **kw):
    f = pdf(tmp_path / f"{ten}.pdf", KHUNG + list(texts), **kw)
    them = [x for kv in dat for x in ("--set", kv)]
    main(["run", str(f), "--out", str(tmp_path / ten), "--pages", "all", *them])
    tom_tat = json.loads(capsys.readouterr().out.strip().splitlines()[-1])
    return tom_tat, read_jsonl(tmp_path / ten / "findings.jsonl")


def tt(rows, rule):
    return [r["tt"] for r in rows if r["rule"] == rule]


# ---------------------------------------------------------------- thiếu số người -> XM, không D
def test_thieu_so_nguoi_khong_ket_luan_dat(tmp_path, capsys):
    _, rows = chay(tmp_path, capsys, "f32", [(905, 345, "D1 900x2200"), (905, 357, "LỐI THOÁT NẠN"),
                                            (450, 352, "HÀNH LANG B=1100")],
                   *CONG_TRINH, "function_group=F3.2", "height_pccc_m=10")
    assert tt(rows, "EXIT-DOOR-WIDTH") == ["XM"]      # 0,9 m: đạt 0,8 nhưng > 50 người cần 1,2
    assert tt(rows, "CORRIDOR-WIDTH") == ["XM"]       # 1,1 m: đạt 1,0 nhưng > 50 người cần 1,2
    _, rows = chay(tmp_path, capsys, "f11", [(125, 470, "THANG BỘ TB1"), (125, 482, "B=1100")],
                   *CONG_TRINH, "function_group=F1.1", "height_pccc_m=10")
    assert tt(rows, "STAIR-FLIGHT-WIDTH") == ["XM"]   # F1.1: 1,0 m nếu <= 15 người/tầng, 1,2 m nếu > 15


def test_du_moi_nguong_van_ra_dat_va_duoi_moi_nguong_ra_khong_dat(tmp_path, capsys):
    _, rows = chay(tmp_path, capsys, "rong", [(905, 345, "D1 1300x2200"), (905, 357, "LỐI THOÁT NẠN"),
                                             (905, 400, "D2 700x2200"), (905, 412, "LỐI THOÁT NẠN")],
                   *CONG_TRINH, "function_group=F3.2", "height_pccc_m=10")
    w = {r["ht"] if r["tt"] == "KD" else r["tt"]: r for r in rows if r["rule"] == "EXIT-DOOR-WIDTH"}
    assert 0.7 in w and w[0.7]["yc"] == 0.8          # dưới mọi ngưỡng -> KD theo ngưỡng thấp nhất
    assert "D" not in w                                # D2 kéo luật thành có phát hiện -> không có dòng D gộp


def test_so_nguoi_du_thi_ket_luan_ro(tmp_path, capsys):
    _, rows = chay(tmp_path, capsys, "f32n", [(905, 345, "D1 900x2200"), (905, 357, "LỐI THOÁT NẠN")],
                   *CONG_TRINH, "function_group=F3.2", "height_pccc_m=10", "max_occupants_per_floor=30")
    assert tt(rows, "EXIT-DOOR-WIDTH") == ["XM"]      # số người của TẦNG chưa phải số người thoát qua cửa này


# ---------------------------------------------------------------- warning -> XM
def test_canh_bao_nhac_viec_la_xm(tmp_path, capsys):
    _, rows = chay(tmp_path, capsys, "l3", [(125, 470, "THANG BỘ TB1"), (125, 482, "B=1200"),
                                           (125, 520, "CẦU THANG BỘ LOẠI 3 - THANG THÉP NGOÀI NHÀ")],
                   *CONG_TRINH, "function_group=F4.3", "height_pccc_m=40")
    assert tt(rows, "STAIR-TYPE3-FALL") == ["XM"]


# ---------------------------------------------------------------- số lối ra theo 3.2.6.1 / 3.2.6.2
MOT_THANG = [(125, 470, "THANG BỘ TB1"), (125, 482, "B=1200")]


@pytest.mark.parametrize("ten, dat, mong", [
    ("f13", ["function_group=F1.3", "height_pccc_m=12", "floor_area_m2=200", "max_occupants_per_floor=16"], []),
    ("f14", ["function_group=F1.4", "height_pccc_m=9", "floor_area_m2=80", "max_occupants_per_floor=6"], []),
    ("f43", ["function_group=F4.3", "height_pccc_m=20", "floor_area_m2=500"], ["KD"]),
    ("f32_thieu", ["function_group=F3.2", "height_pccc_m=10"], ["XM"]),
    ("f32_du", ["function_group=F3.2", "height_pccc_m=10", "floor_area_m2=250", "max_occupants_per_floor=18"], ["XM"]),
])
def test_so_loi_ra_theo_3_2_6(tmp_path, capsys, ten, dat, mong):
    _, rows = chay(tmp_path, capsys, ten, MOT_THANG, *CONG_TRINH, *dat)
    assert tt(rows, "MIN-EXITS-PER-FLOOR") == mong


def test_bac_chiu_lua_v_khong_duoc_mot_loi_ra(tmp_path, capsys):
    dat = ["fire_resistance_level=V", "structural_hazard_class=S3", "floors_above=2", "basements=0",
           "function_group=F2.2", "height_pccc_m=6", "floor_area_m2=250", "max_occupants_per_floor=18"]
    _, rows = chay(tmp_path, capsys, "v", MOT_THANG, *dat)
    assert tt(rows, "MIN-EXITS-PER-FLOOR") == ["KD"]


# ---------------------------------------------------------------- đếm lối ra
def test_cua_vao_thang_va_thang_la_mot_loi(tmp_path, capsys):
    _, rows = chay(tmp_path, capsys, "cung", MOT_THANG + [(230, 470, "D2 900x2200"), (230, 482, "LỐI THOÁT NẠN")],
                   *CONG_TRINH, "function_group=F4.3", "height_pccc_m=20", "floor_area_m2=500")
    assert tt(rows, "EXIT-SEPARATION") == []
    assert tt(rows, "MIN-EXITS-PER-FLOOR") == ["KD"]


def test_chu_giai_ngoai_mat_bang_khong_thanh_loi_ra(tmp_path, capsys):
    _, rows = chay(tmp_path, capsys, "chu", MOT_THANG + [(960, 760, "LỐI THOÁT NẠN")],
                   *CONG_TRINH, "function_group=F4.3", "height_pccc_m=20", "floor_area_m2=500")
    assert tt(rows, "MIN-EXITS-PER-FLOOR") == ["KD"]


def test_thang_chua_xet_hinh_dang_thi_xm(tmp_path, capsys):
    hai = MOT_THANG + [(700, 470, "THANG BỘ TB2"), (700, 482, "B=1200")]
    _, rows = chay(tmp_path, capsys, "hai", hai, *CONG_TRINH, "function_group=F4.3", "height_pccc_m=20",
                   "floor_area_m2=500")
    r = [r for r in rows if r["rule"] == "MIN-EXITS-PER-FLOOR"]
    assert [x["tt"] for x in r] == ["XM"] and "hình dạng" in r[0]["ly_do"]


# ---------------------------------------------------------------- trang xoay
def test_trang_xoay_90_khong_sap(tmp_path, capsys):
    f = pdf(tmp_path / "xoay.pdf", [(100, 1000, "D1 700x2100"), (100, 1012, "LỐI THOÁT NẠN"),
                                    (300, 560, "MẶT BẰNG TẦNG 3"), (300, 575, "TỈ LỆ 1:100")] + KHUNG[2:],
            w=842, h=1191, rot=90, rect=(50, 50, 800, 1150))
    main(["run", str(f), "--out", str(tmp_path / "x"), "--pages", "all",
          *[x for kv in CONG_TRINH + ["function_group=F4.3", "height_pccc_m=20"] for x in ("--set", kv)]])
    tom_tat = json.loads(capsys.readouterr().out.strip().splitlines()[-1])
    rows = read_jsonl(tmp_path / "x" / "findings.jsonl")
    assert tt(rows, "EXIT-DOOR-WIDTH") == ["KD"]
    with pymupdf.open(tmp_path / "x" / tom_tat["pdf_ghi_chu"]) as doc:
        p = doc[0]
        khung = [a.rect for a in p.annots() if a.type[1] == "Square"]
        cho = pymupdf.Rect(100, 990, 170, 1015)          # chữ "D1 700x2100" theo toạ độ trang chưa xoay
        assert khung and any(k.intersects(cho) for k in khung)


# ---------------------------------------------------------------- phân loại trang
def test_khung_ten_co_bo_mon_ket_cau_van_la_mat_bang(tmp_path):
    f = pdf(tmp_path / "kc.pdf", KHUNG + [(960, 800, "KẾT CẤU"), (960, 812, "KẾT CẤU: KS. A")])
    assert classify_page(f, 1)[0] == "plan"


def test_mat_bang_co_bang_thong_ke_cua_van_la_mat_bang(tmp_path, capsys):
    _, rows = chay(tmp_path, capsys, "bc", [(500, 650, "BẢNG THỐNG KÊ CỬA"), (905, 345, "D1 700x2100"),
                                           (905, 357, "LỐI THOÁT NẠN")],
                   *CONG_TRINH, "function_group=F4.3", "height_pccc_m=20")
    assert classify_page(tmp_path / "bc.pdf", 1)[0] == "plan"
    assert tt(rows, "EXIT-DOOR-WIDTH") == ["KD"]


# ---------------------------------------------------------------- nhóm F suy đoán không dùng để kết luận
def test_nhom_f_suy_doan_khong_dung_ket_luan(tmp_path, capsys):
    dat = ["fire_resistance_level=II", "structural_hazard_class=S0", "floors_above=5", "basements=0",
           "height_pccc_m=10"]
    tom_tat, rows = chay(tmp_path, capsys, "nf", [(960, 720, "VĂN PHÒNG KIẾN TRÚC ABC"),
                                                 (960, 735, "CÔNG TRÌNH: KARAOKE XYZ"),
                                                 (905, 345, "D1 900x2200"), (905, 357, "LỐI THOÁT NẠN")], *dat)
    assert "function_group" not in tom_tat["thong_tin"]
    assert not any("F4.3" in a for a in tom_tat["thong_tin"].get("assumptions", []))   # tên đơn vị thiết kế không là nguồn
    assert "D" not in tt(rows, "EXIT-DOOR-WIDTH")
    assert tt(rows, "BUILDING-INFO") == ["XM"]


@pytest.mark.parametrize("dong, goi_y", [
    ("CÔNG NĂNG: CĂN HỘ DỊCH VỤ", None),            # lưu trú F1.2 hay ở F1.3 tuỳ vận hành: không đoán
    ("CÔNG NĂNG: NHÀ Ở KẾT HỢP KINH DOANH", None),   # hỗn hợp: không đoán
    ("CÔNG NĂNG: KHÁCH SẠN", "F1.2"),
])
def test_goi_y_nhom_f_chi_tu_dong_cong_nang(tmp_path, capsys, dong, goi_y):
    dat = ["fire_resistance_level=II", "structural_hazard_class=S0", "floors_above=5", "basements=0", "height_pccc_m=10"]
    tom_tat, rows = chay(tmp_path, capsys, "gy", [(960, 720, "TRỤ SỞ CÔNG TY TNHH THIẾT KẾ XYZ"), (960, 735, dong)], *dat)
    tt_ = tom_tat["thong_tin"]
    assert "function_group" not in tt_ and tt(rows, "BUILDING-INFO") == ["XM"]
    goi = [a for a in tt_.get("assumptions", []) if a.startswith("function_group")]
    assert len(goi) == 1 and "F4.3" not in goi[0]
    assert (f"gợi ý {goi_y}" in goi[0]) if goi_y else ("không suy được" in goi[0])
