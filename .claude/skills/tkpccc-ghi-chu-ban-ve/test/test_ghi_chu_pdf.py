"""Kiểm ghi_chu_pdf.py: đủ kiểu annotation, đúng vị trí trên trang xoay, neo theo chữ, báo lỗi theo từng mục.
Chạy: python -m pytest .claude/skills/tkpccc-ghi-chu-ban-ve/test -q"""
import importlib.util
import json
from pathlib import Path

import pymupdf
import pytest

_spec = importlib.util.spec_from_file_location("ghi_chu_pdf", Path(__file__).parents[1] / "scripts" / "ghi_chu_pdf.py")
gc = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(gc)


@pytest.fixture
def ban_ve(tmp_path):
    doc = pymupdf.open()
    p1 = doc.new_page(width=600, height=400)
    p1.insert_text((100, 100), "CT1 thang", fontsize=12)
    p2 = doc.new_page(width=600, height=400)
    p2.insert_text((300, 200), "FD1", fontsize=12)
    p2.set_rotation(270)                        # bản vẽ khổ ngang hay bị xoay như vậy
    f = tmp_path / "ban_ve.pdf"
    doc.save(f)
    return f


MUC = [
    {"khung": 11, "trang": 1, "kieu": "khung", "neo": {"chu": "CT1", "le": 5}, "mau": "do", "noi_dung": "Thang rẽ quạt"},
    {"khung": 11, "trang": 1, "kieu": "callout", "hop": [300, 40, 560, 90], "den": [120, 95], "mau": "do",
     "noi_dung": "THANG GIỮA: rẽ quạt, chưa đóng kín → KHÔNG ĐẠT (3.4.4)."},
    {"khung": 12, "trang": 1, "kieu": "leader", "tu": [400, 300], "den": [130, 110], "mau": "cam",
     "noi_dung": "Thiếu bề rộng vế: phải ≥ 0,9 m (3.4.1)"},
    {"trang": 1, "kieu": "net", "diem": [[50, 200], [200, 200], [200, 300]], "mau": "xanh", "mui_ten": True,
     "noi_dung": "Đường thoát nạn"},
    {"trang": 1, "kieu": "tuong", "diem": [[80, 120], [80, 180]], "mau": "tim", "phuong_an": "PA1",
     "nhan": "PA1 tường REI 150", "noi_dung": "Tường gạch đặc 220"},
    {"trang": 1, "kieu": "cua", "tam": [80, 180], "den": [80, 210], "chieu": 1, "loai": "EI 60", "mau": "tim",
     "phuong_an": "PA1", "noi_dung": "Cửa ngăn cháy tự đóng"},
    {"trang": 1, "kieu": "cung", "tam": [150, 250], "den": [118, 250], "chieu": 1, "mau": "den", "khung": 11, "nhan": "R = vế",
     "noi_dung": "Cung vế thang: chiếu nghỉ phải chứa trọn cung"},
    {"trang": 2, "kieu": "khung", "khung": 3, "hop": [40, 50, 140, 120], "mau": "cam", "may": True,
     "noi_dung": "Nghi vấn trên trang xoay"},
    {"trang": 2, "kieu": "chu", "hop": [20, 400, 380, 440], "mau": "xanh", "khung": [19, 20], "noi_dung": "TƯ VẤN SƠ BỘ PCCC"},
    {"trang": 2, "kieu": "khung", "khung": 3, "hop": [200, 50, 300, 120], "mau": "xanh_la", "phuong_an": "PA2", "noi_dung": "Buồng thang mở rộng"},
]


def test_ve_du_kieu(ban_ve, tmp_path):
    spec = tmp_path / "doi_chieu.json"
    spec.write_text(json.dumps({"ghi_chu": MUC}, ensure_ascii=False), encoding="utf-8")
    ra = tmp_path / "ra.pdf"
    gc.main(["ve", str(ban_ve), str(spec), "-o", str(ra), "--anh", str(tmp_path / "anh")])
    doc = pymupdf.open(ra)
    p1 = doc[0]                                 # giữ page sống khi đọc annot
    a1 = list(p1.annots())
    kieu = [a.type[1] for a in a1]
    # khung = Square + số; callout = FreeText; leader = Line + nhãn; net 3 điểm = PolyLine; tường 2 điểm = Line + nhãn;
    # cửa = Line (cánh) + PolyLine (cung) + nhãn
    assert kieu.count("Square") == 1 and kieu.count("Line") == 4 and kieu.count("PolyLine") == 2
    assert kieu.count("FreeText") == 6 and kieu.count("Polygon") == 1          # cung vế: vùng 1/4 tròn + bán kính + nhãn
    tieu_de = [a.info["title"] for a in a1]
    assert tieu_de.count("PCCC [11]") == 6 and "PCCC [12]" in tieu_de
    cung = next(a for a in a1 if a.type[1] == "Polygon")
    assert abs(cung.rect.width - 32) < 3 and abs(cung.rect.height - 32) < 3 and cung.opacity < 0.5
    nd = " ".join(a.info["content"] for a in a1)
    assert "PA1: Tường gạch đặc 220" in nd and "KHÔNG ĐẠT (3.4.4)" in nd
    # neo theo chữ: khung bao chữ CT1
    vuong = next(a for a in a1 if a.type[1] == "Square")
    assert vuong.rect.contains(p1.search_for("CT1")[0])
    # trang xoay: khung nằm đúng chỗ NHÌN THẤY [40, 50, 140, 120]
    p2 = doc[1]
    v = [a for a in p2.annots() if a.type[1] == "Square"][0].rect * p2.rotation_matrix
    tam = (v.tl + v.br) / 2                     # viền mây làm khung to ra vài pt: so tâm
    assert abs(tam.x - 90) < 2 and abs(tam.y - 85) < 2 and v.width < 100 + 16
    assert sum(a.type[1] == "FreeText" for a in p2.annots()) == 2      # khung phương án không có thẻ số
    chu = [a for a in p2.annots() if a.info["title"] == "PCCC [19,20]"]
    assert chu and chu[0].info["content"].startswith("[19,20] TƯ VẤN")
    assert (tmp_path / "anh" / "trang_1.png").exists() and (tmp_path / "anh" / "trang_2.png").exists()


def test_zoom_doi_px_sang_point(ban_ve, tmp_path):
    ra = tmp_path / "ra.pdf"
    gc.ve(ban_ve, [{"trang": 1, "kieu": "khung", "hop": [160, 160, 320, 320], "noi_dung": "x"}], ra, zoom=1.6)
    doc = pymupdf.open(ra)                      # giữ doc, page sống: annot của page đã thu hồi thì pymupdf sập
    page = doc[0]
    r = next(page.annots()).rect
    assert abs(r.x0 - 100) < 2 and abs(r.x1 - 200) < 2


def test_bao_loi_tung_muc(ban_ve, tmp_path):
    with pytest.raises(SystemExit) as e:
        gc.ve(ban_ve, [{"trang": 1, "kieu": "hinh_tron"}, {"trang": 9, "kieu": "khung", "hop": [0, 0, 1, 1]},
                       {"trang": 1, "kieu": "khung", "neo": {"chu": "KHONG_CO"}}], tmp_path / "ra.pdf")
    msg = str(e.value)
    assert "ghi_chu[0]" in msg and "ghi_chu[1]" in msg and "ghi_chu[2]" in msg
    assert not (tmp_path / "ra.pdf").exists()


def test_tim_va_luoi(ban_ve, tmp_path):
    kq = gc.tim(ban_ve, "FD1", None, 2.0)
    assert kq and kq[0]["trang"] == 2
    p2 = pymupdf.open(ban_ve)[1]
    v = p2.search_for("FD1")[0] * p2.rotation_matrix
    assert abs(kq[0]["hop"][0] - v.x0 * 2) <= 1
    info = gc.luoi(ban_ve, 2, 1.0, 100, tmp_path / "luoi.png")
    assert (tmp_path / "luoi.png").exists() and info["rong_px"] == 400 and info["xoay"] == 270
    cat = gc.luoi(ban_ve, 2, 1.0, 50, tmp_path / "cat.png", [100, 300, 250, 420], 2.0)   # vùng trên trang xoay, phóng 2
    assert (cat["rong_px"], cat["cao_px"]) == (300, 240)
