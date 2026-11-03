"""Ghi comment + vẽ phương án lên PDF bản vẽ bằng annotation thật (sửa, xoá được trong Foxit/Acrobat).

  python -X utf8 ghi_chu_pdf.py ve   <ban_ve.pdf> <spec.json> [-o ra.pdf] [--anh <thư mục png>]
  python -X utf8 ghi_chu_pdf.py luoi <ban_ve.pdf> --trang 3 [--zoom 1.6] [--buoc 100] [-o luoi.png]
  python -X utf8 ghi_chu_pdf.py tim  <ban_ve.pdf> "CT1" [--trang 3] [--zoom 1.6]

spec.json: danh sách mục ghi chú, hoặc đối tượng có khoá "ghi_chu" (vd doi_chieu.json) và "zoom" (tuỳ chọn).
Tọa độ là tọa độ NHÌN THẤY trên ảnh render (đã xoay theo /Rotate), đơn vị px của ảnh render ở `zoom`
(mặc định zoom 1 = point PDF). Cách dùng, các kiểu, màu: xem SKILL.md cùng thư mục cha.
"""
from __future__ import annotations

import argparse
import json
import math
import sys
from pathlib import Path

import pymupdf

MAU = {"do": (0.85, 0.1, 0.1), "cam": (0.95, 0.5, 0.0), "xanh": (0.1, 0.3, 0.85),
       "tim": (0.5, 0.15, 0.7), "xanh_la": (0.05, 0.6, 0.2), "den": (0.0, 0.0, 0.0)}
KIEU = ("khung", "callout", "leader", "net", "tuong", "cua", "cung", "chu")
MUI_TEN = pymupdf.PDF_ANNOT_LE_OPEN_ARROW


class Loi(Exception):
    pass


def _so_khung(muc: dict) -> str | None:
    """Số khung hiện trên bản vẽ; danh sách = một ghi chú chung cho nhiều dòng bảng."""
    k = muc.get("khung")
    return None if k is None else ",".join(map(str, k)) if isinstance(k, list) else str(k)


def _diem(v, k: float) -> pymupdf.Point:
    if not (isinstance(v, (list, tuple)) and len(v) == 2):
        raise Loi(f"điểm phải là [x, y], nhận {v!r}")
    return pymupdf.Point(float(v[0]) * k, float(v[1]) * k)


def _hop(v, k: float) -> pymupdf.Rect:
    if not (isinstance(v, (list, tuple)) and len(v) == 4):
        raise Loi(f"hop phải là [x0, y0, x1, y1], nhận {v!r}")
    return pymupdf.Rect(*(float(x) * k for x in v)).normalize()


def _neo(page: pymupdf.Page, neo: dict) -> pymupdf.Rect:
    """Khung quanh chữ tìm thấy trên trang (tọa độ nhìn thấy, point)."""
    thay = page.search_for(str(neo["chu"]))
    thu = int(neo.get("thu", 1))
    if len(thay) < thu:
        raise Loi(f"không thấy chữ {neo['chu']!r} lần thứ {thu} trên trang {page.number + 1}")
    le = float(neo.get("le", 6))
    return (thay[thu - 1] * page.rotation_matrix) + (-le, -le, le, le)


def _co_chu(text: str, rect: pymupdf.Rect, co: float) -> tuple[float, pymupdf.Rect]:
    """Thu cỡ chữ tới khi ước lượng vừa khung; nhỏ nhất 5 pt thì kéo dài khung xuống."""
    def cao_can(s: float) -> float:
        rong = max(rect.width - 4, 10)
        dong = 0
        for doan in text.split("\n"):
            dong += max(1, math.ceil(pymupdf.get_text_length(doan, fontname="helv", fontsize=s) * 1.12 / rong))
        return dong * s * 1.25 + 4
    while co > 5 and cao_can(co) > rect.height:
        co -= 0.5
    if cao_can(co) > rect.height:
        rect = pymupdf.Rect(rect.x0, rect.y0, rect.x1, rect.y0 + cao_can(co))
    return co, rect


class But:
    """Vẽ trên một trang theo tọa độ nhìn thấy (point); tự đổi sang tọa độ trang gốc khi trang có /Rotate."""

    def __init__(self, page: pymupdf.Page):
        self.p = page
        self.m = page.derotation_matrix

    def _info(self, a, muc: dict, kieu: str):
        so = _so_khung(muc)
        tieu_de = "PCCC" + (f" [{so}]" if so is not None else "")
        nd = str(muc.get("noi_dung", ""))
        if muc.get("phuong_an"):
            nd = f"{muc['phuong_an']}: {nd}"
        if a.type[1] == "FreeText":         # content của FreeText chính là chữ hiện trên trang: giữ nguyên
            a.set_info(title=tieu_de, subject=kieu)
        else:
            a.set_info(title=tieu_de, subject=kieu, content=nd)

    def chu(self, rect, text, mau, co=7.5, vien=True, tro=None):
        """FreeText; `tro` = [đích, khuỷu, mép hộp] (nhìn thấy) để thành callout."""
        co, rect = _co_chu(text, rect, co)
        kw = {}
        if tro:
            kw = {"callout": [p * self.m for p in tro], "line_end": MUI_TEN}
        a = self.p.add_freetext_annot(rect * self.m, text, fontsize=co, text_color=mau, fill_color=(1, 1, 1),
                                      border_width=1 if vien else 0, rotate=self.p.rotation, **kw)
        return a

    def khung(self, rect, mau, net_dut=True, may=False):
        a = self.p.add_rect_annot(rect * self.m)
        a.set_colors(stroke=mau)
        a.set_border(width=1.5, dashes=[4, 3] if net_dut and not may else None, clouds=1 if may else -1)
        return a

    def net(self, diem, mau, day=1.5, net_dut=False, mui_ten=False):
        if len(diem) == 2:
            a = self.p.add_line_annot(diem[0] * self.m, diem[1] * self.m)
        else:
            a = self.p.add_polyline_annot([d * self.m for d in diem])
        if mui_ten:
            a.set_line_ends(pymupdf.PDF_ANNOT_LE_NONE, MUI_TEN)
        a.set_colors(stroke=mau)
        a.set_border(width=day, dashes=[5, 3] if net_dut else None)
        return a


def _nhan(but: But, muc: dict, goc: pymupdf.Point, mau, k: float) -> list:
    """Nhãn ngắn nhìn thấy trên bản vẽ (vd "PA1 tường REI 150", "FD EI 60")."""
    if not muc.get("nhan"):
        return []
    dx, dy = (float(v) * k for v in muc.get("nhan_lech", [4 / k, -14 / k]))
    t = str(muc["nhan"])
    r = pymupdf.Rect(goc.x + dx, goc.y + dy, goc.x + dx + 6 + 4.2 * len(t), goc.y + dy + 11)
    return [but.chu(r, t, mau, co=7, vien=False)]


def ve_muc(page: pymupdf.Page, muc: dict, k: float) -> list:
    kieu = muc.get("kieu")
    if kieu not in KIEU:
        raise Loi(f"kieu {kieu!r} không có; dùng một trong {', '.join(KIEU)}")
    mau = MAU.get(muc.get("mau", "do"))
    if mau is None:
        raise Loi(f"mau {muc.get('mau')!r} không có; dùng một trong {', '.join(MAU)}")
    but = But(page)
    rect = _neo(page, muc["neo"]) if muc.get("neo") else (_hop(muc["hop"], k) if "hop" in muc else None)
    nd = str(muc.get("noi_dung", ""))
    so = _so_khung(muc)
    tao = []
    if kieu == "khung":
        if rect is None:
            raise Loi("khung cần 'hop' hoặc 'neo'")
        tao.append(but.khung(rect, mau, muc.get("net_dut", True), muc.get("may", False)))
        if so is not None and not muc.get("phuong_an"):     # khung phương án: chỉ hiện nhãn PA, không hiện số
            t = so
            tao.append(but.chu(pymupdf.Rect(rect.x0, rect.y0 - 12, rect.x0 + 8 + 5 * len(t), rect.y0), t, mau, co=8))
        tao += _nhan(but, muc, rect.tl, mau, k)
    elif kieu in ("callout", "chu"):
        if rect is None:
            raise Loi(f"{kieu} cần 'hop' (khung chữ)")
        text = (f"[{so}] " if so is not None else "") + (f"{muc['phuong_an']}: " if muc.get("phuong_an") else "") + nd
        tro = None
        if kieu == "callout":
            if "den" not in muc:
                raise Loi("callout cần 'den' (điểm được chỉ tới)")
            dich = _diem(muc["den"], k)
            canh = pymupdf.Point(rect.x1 if dich.x > rect.x1 else rect.x0 if dich.x < rect.x0 else (rect.x0 + rect.x1) / 2,
                                 (rect.y0 + rect.y1) / 2)
            khuyu = _diem(muc["khuyu"], k) if "khuyu" in muc else pymupdf.Point((canh.x + dich.x) / 2, canh.y)
            tro = [dich, khuyu, canh]
        tao.append(but.chu(rect, text, mau, co=float(muc.get("co", 7.5)), vien=muc.get("vien", True), tro=tro))
    elif kieu == "leader":
        if "tu" not in muc or "den" not in muc:
            raise Loi("leader cần 'tu' (đuôi, chỗ ghi chữ) và 'den' (mũi tên)")
        tu, den = _diem(muc["tu"], k), _diem(muc["den"], k)
        tao.append(but.net([tu, den], mau, day=1.2, mui_ten=True))
        muc = {**muc, "nhan": muc.get("nhan") or (f"[{so}]" if so is not None else None)}
        tao += _nhan(but, muc, tu, mau, k)
    elif kieu in ("net", "tuong"):
        if len(muc.get("diem", [])) < 2:
            raise Loi(f"{kieu} cần 'diem' có ít nhất 2 điểm")
        diem = [_diem(d, k) for d in muc["diem"]]
        if kieu == "tuong":
            tao.append(but.net(diem, mau, day=float(muc.get("day", 3.5))))
        else:
            tao.append(but.net(diem, mau, day=float(muc.get("day", 1.5)), net_dut=muc.get("net_dut", False),
                               mui_ten=muc.get("mui_ten", False)))
        tao += _nhan(but, muc, diem[0], mau, k)
    elif kieu == "cung":
        # cung vế thang (người dùng train 2026-11-02): tâm = đầu mép bậc sát giếng thang, bán kính = bề rộng vế;
        # vùng 1/4 tròn này là chiếu nghỉ phải trống, cánh cửa không được quét vào
        if "tam" not in muc or "den" not in muc:
            raise Loi("cung cần 'tam' (đầu mép bậc sát giếng thang) và 'den' (đầu mép bậc phía tường)")
        tam, den = _diem(muc["tam"], k), _diem(muc["den"], k)
        v = den - tam
        r, g0 = math.hypot(v.x, v.y), math.atan2(v.y, v.x)
        goc = math.radians(float(muc.get("goc", 90))) * (1 if muc.get("chieu", 1) >= 0 else -1)
        cung = [tam + (r * math.cos(g0 + goc * i / 12), r * math.sin(g0 + goc * i / 12)) for i in range(13)]
        a = but.p.add_polygon_annot([p * but.m for p in [tam] + cung])
        a.set_colors(stroke=mau, fill=mau)
        a.set_border(width=0.8)
        a.set_opacity(0.3)
        tao.append(a)
        tao.append(but.net([tam, den], mau, day=0.8, net_dut=True))     # bán kính = bề rộng vế
        tao += _nhan(but, muc, cung[6], mau, k)
    elif kieu == "cua":
        if "tam" not in muc or "den" not in muc:
            raise Loi("cua cần 'tam' (bản lề) và 'den' (mép cánh khi đóng)")
        tam, den = _diem(muc["tam"], k), _diem(muc["den"], k)
        v = den - tam
        r, g0 = math.hypot(v.x, v.y), math.atan2(v.y, v.x)
        chieu = 1 if muc.get("chieu", 1) >= 0 else -1
        cung = [tam + (r * math.cos(g0 + chieu * i * math.pi / 24), r * math.sin(g0 + chieu * i * math.pi / 24))
                for i in range(13)]
        tao.append(but.net([tam, cung[-1]], mau, day=1.5))          # cánh mở 90°
        tao.append(but.net(cung, mau, day=0.8, net_dut=True))       # cung quét
        muc = {**muc, "nhan": muc.get("nhan") or muc.get("loai")}
        tao += _nhan(but, muc, cung[6], mau, k)
    for a in tao:
        but._info(a, muc, kieu)
        a.update()
    return tao


def doc_spec(path: Path) -> tuple[list, float]:
    data = json.loads(path.read_text(encoding="utf-8"))
    if isinstance(data, list):
        return data, 1.0
    return data.get("ghi_chu", []), float(data.get("zoom", 1.0))


def ve(pdf_vao: Path, muc_list: list, pdf_ra: Path, zoom: float = 1.0, anh: Path | None = None) -> dict:
    doc = pymupdf.open(pdf_vao)
    loi, dem, trang = [], 0, set()
    for i, muc in enumerate(muc_list):
        try:
            if not isinstance(muc, dict):
                raise Loi("mỗi mục phải là đối tượng JSON")
            t = int(muc.get("trang", 0))
            if not 1 <= t <= doc.page_count:
                raise Loi(f"trang {t} ngoài 1..{doc.page_count}")
            k = 1 / float(muc.get("zoom", zoom))
            dem += len(ve_muc(doc[t - 1], muc, k))
            trang.add(t)
        except (Loi, KeyError, TypeError, ValueError) as e:
            loi.append(f"ghi_chu[{i}]: {e}")
    if loi:
        raise SystemExit("\n".join(loi))
    pdf_ra.parent.mkdir(parents=True, exist_ok=True)
    try:
        doc.save(pdf_ra, garbage=3, deflate=True)
    except Exception as e:              # hay gặp: file đích đang mở trong trình xem PDF
        raise SystemExit(f"không ghi được {pdf_ra}: {e}. File đang mở thì đóng lại hoặc dùng -o tên khác")
    if anh:
        anh.mkdir(parents=True, exist_ok=True)
        ra = pymupdf.open(pdf_ra)
        for t in sorted(trang):
            ra[t - 1].get_pixmap(matrix=pymupdf.Matrix(zoom, zoom), annots=True).save(anh / f"trang_{t}.png")
    return {"ok": True, "pdf": str(pdf_ra), "annotation": dem, "trang": sorted(trang)}


def luoi(pdf: Path, trang: int, zoom: float, buoc: int, ra: Path, vung: list | None = None, phong: float = 1.0) -> dict:
    """Ảnh trang kèm lưới tọa độ px (ở zoom) để đọc vị trí đặt ghi chú. `vung` [x0,y0,x1,y1] (px ở zoom): chỉ cắt vùng đó,
    phóng `phong` lần cho rõ; nhãn lưới vẫn là tọa độ ở zoom nên dùng thẳng cho ghi chú."""
    page = pymupdf.open(pdf)[trang - 1]
    pix = page.get_pixmap(matrix=pymupdf.Matrix(zoom * phong, zoom * phong))
    w, h = pix.width, pix.height
    tam = pymupdf.open()
    p = tam.new_page(width=w, height=h)     # trang tạm đúng chiều nhìn: cắt theo px, khỏi lo /Rotate
    p.insert_image(p.rect, pixmap=pix)
    x0, y0, x1, y1 = vung or (0, 0, w / phong, h / phong)
    for x in range(-(-int(x0) // buoc) * buoc, int(x1) + 1, buoc):
        p.draw_line((x * phong, y0 * phong), (x * phong, y1 * phong), color=(1, 0, 0), width=0.4, stroke_opacity=0.5)
        p.insert_text((x * phong + 2, y0 * phong + 10), str(x), fontsize=9, color=(1, 0, 0))
    for y in range(-(-int(y0) // buoc) * buoc, int(y1) + 1, buoc):
        p.draw_line((x0 * phong, y * phong), (x1 * phong, y * phong), color=(0, 0, 1), width=0.4, stroke_opacity=0.5)
        p.insert_text((x0 * phong + 2, max(y * phong - 2, y0 * phong + 20)), str(y), fontsize=9, color=(0, 0, 1))
    ra.parent.mkdir(parents=True, exist_ok=True)
    anh = p.get_pixmap(clip=pymupdf.Rect(x0 * phong, y0 * phong, x1 * phong, y1 * phong) if vung else None)
    anh.save(ra)
    return {"png": str(ra), "rong_px": anh.width, "cao_px": anh.height, "zoom": zoom, "phong": phong, "xoay": page.rotation}


def tim(pdf: Path, chu: str, trang: int | None, zoom: float) -> list:
    doc = pymupdf.open(pdf)
    ra = []
    for page in ([doc[trang - 1]] if trang else doc):
        for r in page.search_for(chu):
            v = (r * page.rotation_matrix) * zoom
            ra.append({"trang": page.number + 1, "hop": [round(v.x0), round(v.y0), round(v.x1), round(v.y1)]})
    return ra


def main(argv=None):
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    pymupdf.set_messages(fd=2)      # cảnh báo của pymupdf ra stderr, stdout chỉ còn 1 dòng JSON kết quả
    ap = argparse.ArgumentParser(prog="ghi_chu_pdf", description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="lenh", required=True)
    a = sub.add_parser("ve")
    a.add_argument("pdf")
    a.add_argument("spec")
    a.add_argument("-o", "--ra")
    a.add_argument("--anh")
    b = sub.add_parser("luoi")
    b.add_argument("pdf")
    b.add_argument("--trang", type=int, required=True)
    b.add_argument("--zoom", type=float, default=1.6)
    b.add_argument("--buoc", type=int, default=100)
    b.add_argument("-o", "--ra")
    d = sub.add_parser("cat")       # ảnh một vùng, phóng to, lưới theo tọa độ zoom: xem chi tiết rẻ hơn ảnh cả trang
    d.add_argument("pdf")
    d.add_argument("--trang", type=int, required=True)
    d.add_argument("--vung", required=True, help="x0,y0,x1,y1 px ở zoom")
    d.add_argument("--zoom", type=float, default=1.6)
    d.add_argument("--phong", type=float, default=2.0)
    d.add_argument("--buoc", type=int, default=50)
    d.add_argument("-o", "--ra")
    c = sub.add_parser("tim")
    c.add_argument("pdf")
    c.add_argument("chu")
    c.add_argument("--trang", type=int)
    c.add_argument("--zoom", type=float, default=1.0)
    x = ap.parse_args(argv)
    pdf = Path(x.pdf)
    if x.lenh == "ve":
        spec = Path(x.spec)
        muc, zoom = doc_spec(spec)
        ra = Path(x.ra) if x.ra else spec.parent / f"{pdf.stem}_ghi_chu.pdf"
        kq = ve(pdf, muc, ra, zoom, Path(x.anh) if x.anh else None)
    elif x.lenh == "luoi":
        kq = luoi(pdf, x.trang, x.zoom, x.buoc, Path(x.ra) if x.ra else pdf.parent / f"luoi_trang_{x.trang}.png")
    elif x.lenh == "cat":
        v = [float(s) for s in x.vung.split(",")]
        if len(v) != 4 or v[0] >= v[2] or v[1] >= v[3]:
            raise SystemExit("--vung phải là x0,y0,x1,y1 với x0 < x1, y0 < y1")
        ra = Path(x.ra) if x.ra else pdf.parent / "anh" / f"cat_{x.trang}_{int(v[0])}_{int(v[1])}.png"
        kq = luoi(pdf, x.trang, x.zoom, x.buoc, ra, v, x.phong)
    else:
        kq = tim(pdf, x.chu, x.trang, x.zoom)
    print(json.dumps(kq, ensure_ascii=False))


if __name__ == "__main__":
    main()
