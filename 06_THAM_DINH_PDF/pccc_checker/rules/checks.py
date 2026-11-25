"""Thư viện check. Mỗi check nhận (rule_yaml, extraction) và yield Finding."""
from __future__ import annotations

import math
import re
from collections import defaultdict
from typing import Any, Iterable

from ..models import DrawingExtraction, Element, Finding, PageExtraction
from .engine import context, make_finding, match3, match_when, possible_values, register, resolve_value, targets, unknown_keys

PT_TO_MM = 25.4 / 72


def _num(v: Any) -> float | None:
    try:
        return float(v)
    except (TypeError, ValueError):
        return None


def _ei(v: Any) -> int | None:
    m = re.search(r"(\d{2,3})", str(v or ""))
    return int(m.group(1)) if m else None


def page_scale_m_per_pt(page: PageExtraction, default: str | None = None) -> float | None:
    """'1:100' -> số mét thực tế ứng với 1 point trên trang."""
    for s in (page.scale, default):
        m = re.search(r"1\s*[:/]\s*(\d+)", s or "")
        if m:
            return PT_TO_MM * int(m.group(1)) / 1000
    return None


# ------------------------------------------------------------ check tổng quát
def _xet_nguong(rule: dict, d: DrawingExtraction, nho_hon_la_loi: bool) -> Iterable[Finding]:
    """min_attr (nho_hon_la_loi) / max_attr. Ngưỡng phụ thuộc số liệu chưa biết (vd số người) -> có nhiều ngưỡng có thể:
    vi phạm MỌI ngưỡng -> KD theo ngưỡng nhẹ nhất; chỉ vi phạm một số -> XM (không được kết luận đạt)."""
    attr = rule["attr"]
    for el in targets(rule, d):
        actual = _num(el.get(attr))
        if actual is None:
            continue
        vals = sorted(possible_values(rule, context(el, d)), key=lambda v: float(v[0]), reverse=not nho_hon_la_loi)
        if not vals:
            continue
        sai = (lambda v: actual + 1e-6 < float(v)) if nho_hon_la_loi else (lambda v: actual - 1e-6 > float(v))
        loi = [v for v in vals if sai(v[0])]
        if not loi:
            continue
        if len(loi) == len(vals):                 # vi phạm cả ngưỡng nhẹ nhất (vals[0])
            yield make_finding(rule, el, actual, vals[0][0], vals[0][1])
            continue
        nang = loi[-1]                            # ngưỡng chặt nhất bị vi phạm
        thieu = sorted(set().union(*(v[2] for v in loi)))
        f = make_finding(rule, el, actual, nang[0],
                         f"chỉ không đạt nếu {nang[1]}; máy chưa biết {', '.join(thieu) or 'trường hợp áp dụng'} — "
                         f"đạt ngưỡng {vals[0][0]} m ({vals[0][1]})")
        f.status = "xm"
        yield f


@register("min_attr")
def min_attr(rule: dict, d: DrawingExtraction) -> Iterable[Finding]:
    yield from _xet_nguong(rule, d, True)


@register("max_attr")
def max_attr(rule: dict, d: DrawingExtraction) -> Iterable[Finding]:
    yield from _xet_nguong(rule, d, False)


@register("bool_attr")
def bool_attr(rule: dict, d: DrawingExtraction) -> Iterable[Finding]:
    """Thuộc tính phải bằng `value` (true/false). Không có dữ liệu -> bỏ qua."""
    attr, want = rule["attr"], bool(rule.get("value", True))
    for el in targets(rule, d):
        v = el.get(attr)
        if v is not None and bool(v) != want:
            yield make_finding(rule, el, v, want)


@register("required_attr")
def required_attr(rule: dict, d: DrawingExtraction) -> Iterable[Finding]:
    """Phần tử phải thể hiện thông tin `attr` trên bản vẽ (vd cửa ngăn cháy phải ghi EI)."""
    for el in targets(rule, d):
        if el.get(rule["attr"]) in (None, "", []):
            yield make_finding(rule, el, None, rule["attr"])


@register("min_fire_rating")
def min_fire_rating(rule: dict, d: DrawingExtraction) -> Iterable[Finding]:
    for el in targets(rule, d):
        actual = _ei(el.get(rule.get("attr", "fire_rating")))
        if actual is None:
            continue
        req, note = resolve_value(rule, context(el, d))
        if req is not None and actual < _ei(req):
            yield make_finding(rule, el, f"EI{actual}", f"EI{_ei(req)}", note)


@register("compare_attrs")
def compare_attrs(rule: dict, d: DrawingExtraction) -> Iterable[Finding]:
    """left (op) right * factor, vd landing_width_m >= flight_width_m; riser_m <= tread_m (độ dốc 1:1)."""
    ops = {">=": lambda a, b: a >= b - 1e-6, "<=": lambda a, b: a <= b + 1e-6}
    for el in targets(rule, d):
        a, b = _num(el.get(rule["left"])), _num(el.get(rule["right"]))
        if a is None or b is None:
            continue
        b *= float(rule.get("factor", 1))
        if not ops[rule.get("op", ">=")](a, b):
            yield make_finding(rule, el, a, round(b, 3))


@register("condition_flag")
def condition_flag(rule: dict, d: DrawingExtraction) -> Iterable[Finding]:
    """Báo khi điều kiện `when` khớp (vd thang loại 3 trong nhà > 50 m)."""
    for el in targets(rule, d):
        if match_when(rule.get("when"), context(el, d)):
            yield make_finding(rule, el)


@register("missing_data")
def missing_data(rule: dict, d: DrawingExtraction) -> Iterable[Finding]:
    """Phần tử quan trọng thiếu số liệu -> nhắc kiểm tra thủ công (không kết luận sai)."""
    for el in targets(rule, d):
        miss = [a for a in rule["attrs"] if el.get(a) is None]
        if miss:
            yield make_finding(rule, el, None, ", ".join(miss), missing=", ".join(miss))


@register("building_missing")
def building_missing(rule: dict, d: DrawingExtraction) -> Iterable[Finding]:
    miss = [a for a in rule["attrs"] if getattr(d.building, a, None) is None]
    if miss:
        yield make_finding(rule, None, None, ", ".join(miss), missing=", ".join(miss))


# ------------------------------------------------------------ check chuyên biệt
CUNG_LOI_M = 3.0      # shortcut: cửa cách nhãn thang <= 3 m coi là cửa vào chính thang đó; nhận được đường bao buồng thang thì đổi


def _gap(a: list[float], b: list[float]) -> float:
    return math.hypot(max(0.0, max(a[0], b[0]) - min(a[2], b[2])), max(0.0, max(a[1], b[1]) - min(a[3], b[3])))


def _exits_by_page(d: DrawingExtraction) -> dict[int, list[Element]]:
    """Lối ra thoát nạn theo trang. Cửa nằm sát nhãn thang là cửa vào chính buồng thang đó: chỉ tính 1 lối (thang)."""
    pages = {p.page: p for p in d.pages}
    stairs: dict[int, list[Element]] = defaultdict(list)
    doors: dict[int, list[Element]] = defaultdict(list)
    for e in d.elements():
        if e.attrs.get("schedule_entry"):
            continue
        if e.type == "exit_door" or (e.type in ("door", "fire_door") and e.get("is_exit")):
            doors[e.page].append(e)
        elif e.type == "stair" and e.get("is_evacuation") is not False and e.get("counts_as_exit", True):
            stairs[e.page].append(e)
    out: dict[int, list[Element]] = defaultdict(list)
    for pno in set(stairs) | set(doors):
        k = page_scale_m_per_pt(pages[pno]) if pno in pages else None
        gan = CUNG_LOI_M / k if k else 85.0        # 85 pt ~ 3 m ở 1:100
        out[pno] = stairs[pno] + [e for e in doors[pno] if not any(_gap(e.bbox, s.bbox) <= gan for s in stairs[pno])]
    return out


def _chac(e: Element) -> bool:
    """Lối ra chắc chắn: cửa có ký hiệu (không phải chữ đứng riêng); thang đã được người/Claude xét hình dạng."""
    if e.type == "stair":
        return e.get("shape_ok") is True
    return not e.get("text_only")


def _edge_dists(a: list[float], b: list[float]) -> tuple[float, float]:
    """(khoảng cách 2 cạnh xa nhất, khoảng cách 2 cạnh gần nhất) giữa 2 lối ra, xấp xỉ theo bbox."""
    far = math.hypot(max(a[2], b[2]) - min(a[0], b[0]), max(a[3], b[3]) - min(a[1], b[1]))
    gx = max(0.0, max(a[0], b[0]) - min(a[2], b[2]))
    gy = max(0.0, max(a[1], b[1]) - min(a[3], b[3]))
    return far, math.hypot(gx, gy)


def _plan_bbox(page: PageExtraction) -> tuple[list[float] | None, bool]:
    """Khung mặt bằng: ưu tiên plan_bbox do extractor cung cấp; nếu không có thì gộp bbox các phần tử (xấp xỉ)."""
    if page.plan_bbox:
        return page.plan_bbox, False
    els = [e for e in page.elements if e.type in ("room", "corridor", "stair", "exit_door", "door", "fire_door", "lobby")]
    if len(els) < 2:
        return None, True
    return [min(e.bbox[0] for e in els), min(e.bbox[1] for e in els),
            max(e.bbox[2] for e in els), max(e.bbox[3] for e in els)], True


@register("exit_separation")
def exit_separation(rule: dict, d: DrawingExtraction) -> Iterable[Finding]:
    """3.2.8 (SĐ1:2023): >= 2 lối ra phải phân tán, cách nhau >= 1/2 đường chéo lớn nhất mặt bằng
    (1/3 nếu toàn nhà có sprinkler). Khoảng cách đo giữa 2 cạnh xa nhất; nếu giá trị này < 7 m thì đo giữa
    2 cạnh gần nhất. Tool lấy cặp lối ra phân tán nhất trên tầng để so sánh."""
    pages = {p.page: p for p in d.pages}
    sprinkler = d.building.has_auto_sprinkler          # True / False / None (chưa rõ)
    r_half, r_third = float(rule.get("ratio", 0.5)), float(rule.get("ratio_sprinkler", 1 / 3))
    ratio = r_third if sprinkler else r_half
    threshold = float(rule.get("measure_threshold_m", 7.0))
    for pno, exits in _exits_by_page(d).items():
        if len(exits) < 2:
            continue
        page = pages[pno]
        dec = page.declared or {}
        if dec.get("exit_distance_m") and dec.get("plan_diagonal_m"):
            # Người thiết kế đã ghi khoảng cách & đường chéo -> kiểm tra trực tiếp số liệu đó
            diag, dist = float(dec["plan_diagonal_m"]), float(dec["exit_distance_m"])
            req = round(diag * ratio, 2)
            if dist + 1e-6 < req:
                if sprinkler is None and dist + 1e-6 >= diag * r_third:
                    sev = "warning"
                    msg = (f"Khoảng cách 2 lối thoát nạn ghi trên bản vẽ {dist} m < {req} m (1/2 đường chéo {diag} m). "
                           f"Chỉ đạt nếu TOÀN BỘ nhà có sprinkler (cần >= {round(diag * r_third, 2)} m) — "
                           "bản vẽ chưa thể hiện sprinkler, cần xác nhận")
                else:
                    sev = rule.get("severity", "error")
                    msg = (f"Khoảng cách 2 lối thoát nạn ghi trên bản vẽ {dist} m < {req} m "
                           f"({'1/3' if sprinkler else '1/2'} đường chéo {diag} m)")
                f = Finding(rule["id"], rule.get("clause", ""), sev, msg, page=pno, actual=dist, required=req,
                            verified_rule=bool(rule.get("verified")))
                f.bbox = dec.get("exit_distance_bbox") or page.plan_bbox
                yield f
            continue
        k = page_scale_m_per_pt(page, rule.get("default_scale"))
        plan, approx = _plan_bbox(page)
        if k is None or plan is None:
            yield Finding(rule["id"], rule.get("clause", ""), "info",
                          "Không xác định được tỉ lệ hoặc khung mặt bằng -> chưa kiểm tra được khoảng cách giữa các lối ra",
                          page=pno, verified_rule=bool(rule.get("verified")))
            continue
        diag_m = math.hypot(plan[2] - plan[0], plan[3] - plan[1]) * k
        required = round(diag_m * ratio, 2)
        best = None
        for i in range(len(exits)):
            for j in range(i + 1, len(exits)):
                far, near = _edge_dists(exits[i].bbox, exits[j].bbox)
                far_m = far * k
                dist = far_m if far_m >= threshold else near * k
                if best is None or dist > best[0]:
                    best = (dist, exits[i], exits[j], "cạnh xa nhất" if far_m >= threshold else "cạnh gần nhất")
        dist, e1, e2, how = best
        if dist + 1e-6 < required:
            note = f"đo theo {how}; đường chéo mặt bằng ~{diag_m:.1f} m" + (" (ước lượng)" if approx else "")                 + (", có sprinkler -> 1/3" if sprinkler else ", 1/2 đường chéo")
            f = make_finding(rule, e1, round(dist, 2), required, note,
                             pair=f"{e1.label or e1.id} ↔ {e2.label or e2.id}")
            f.bbox = [min(e1.bbox[0], e2.bbox[0]), min(e1.bbox[1], e2.bbox[1]),
                      max(e1.bbox[2], e2.bbox[2]), max(e1.bbox[3], e2.bbox[3])]
            f.element_ids = [e1.id, e2.id]
            yield f


@register("min_exits_per_floor")
def min_exits_per_floor(rule: dict, d: DrawingExtraction) -> Iterable[Finding]:
    """Mỗi tầng (trang mặt bằng) cần >= 2 lối ra thoát nạn, trừ khi thoả điều kiện 1 lối ra
    (các `single_exit_cases` trong YAML, gồm trường hợp mới của SĐ1:2023)."""
    exits = _exits_by_page(d)
    ctx = context(None, d)

    def ba(conds) -> tuple[bool | None, set]:
        """Một trong các điều kiện khớp chắc chắn -> True; có điều kiện chưa biết -> None; còn lại False."""
        kq = [match3(c, ctx) for c in conds or []]
        thieu = set().union(set(), *(unknown_keys(c, ctx) for c, r in zip(conds or [], kq) if r is None))
        return (True if True in kq else None if None in kq else False), thieu

    nv = match3(rule.get("no_single_exit"), ctx) if rule.get("no_single_exit") else False
    ngoai_le, thieu_nl = ba(rule.get("single_exit_cases"))
    if nv is True:                                   # nhà bậc chịu lửa V: không có ngoại lệ 1 lối ra
        ngoai_le = False
    elif nv is None and ngoai_le is not False:
        ngoai_le, thieu_nl = None, thieu_nl | {"fire_resistance_level"}
    for p in d.pages:
        is_plan = p.category == "plan" or (not p.category and any(
            e.type in ("room", "corridor", "stair", "exit_door") for e in p.elements))
        if not is_plan or re.search(r"\bMÁI\b", p.sheet_title or "", re.I):
            continue
        dsach = exits.get(p.page, [])
        n, n_chac = len(dsach), sum(_chac(e) for e in dsach)
        can2, thieu = ba(rule.get("need_two"))
        if re.search(r"HẦM", p.sheet_title or "", re.I) and can2 is not True:
            h, th = ba(rule.get("need_two_basement"))
            can2 = True if h is True else (None if None in (h, can2) else False)
            thieu |= th
        if can2 is False:
            co, th = ba(rule.get("need_two_maybe"))
            if co is not False:
                can2, thieu = None, thieu | th | ({"(hạng / diện tích căn hộ)"} if co else set())
        if n == 0:
            yield Finding(rule["id"], rule.get("clause", ""), "warning",
                          "Không nhận diện được lối ra thoát nạn nào trên mặt bằng — cần kiểm tra thủ công",
                          page=p.page, verified_rule=bool(rule.get("verified")))
            continue
        if can2 is False or (n_chac >= 2):
            continue
        if n >= 2:            # đủ 2 lối nếu tính cả lối chưa chắc chắn
            ds = [e.label or e.id for e in dsach if not _chac(e)]
            f = make_finding(rule, dsach[0], n, 2,
                             f"đếm được {n} lối ra nhưng chỉ {n_chac} lối chắc chắn; chưa xét hình dạng thang / lối ra "
                             f"nhận từ chữ đứng riêng: {', '.join(ds)}. Thang rẽ quạt, bậc chéo, thang cong, xoắn không tính "
                             "thoát nạn (trừ 3.4.4) — 01_TRI_THUC/nhan-dien-thang-thoat-nan.md")
            f.status = "xm"
            yield f
            continue
        f = make_finding(rule, dsach[0], n, 2)
        if can2 is None or ngoai_le is not False or (p.declared or {}).get("unoccupied"):
            ly = []
            if can2 is None:
                ly.append(f"chưa xác định tầng có thuộc diện phải có 2 lối ra (3.2.6.1), máy chưa biết: {', '.join(sorted(thieu))}")
            if ngoai_le is True:
                ly.append("thuộc trường hợp được 1 lối ra (3.2.6.2) nếu thoả thêm điều kiện máy không kiểm: không để xe cơ "
                          "giới, không kho hạng A, B, C tại tầng có lối ra ngoài nhà; lối thoát khẩn cấp hoặc lên sân thượng "
                          "thoáng; nhà > 3 tầng hoặc > 9 m: lối ra của phòng và tầng theo quy chuẩn")
            elif ngoai_le is None:
                ly.append(f"chưa đủ số liệu xét trường hợp được 1 lối ra (3.2.6.2): {', '.join(sorted(thieu_nl))}")
            if (p.declared or {}).get("unoccupied"):
                ly.append("tầng có ghi chú 'không có người có mặt thường xuyên' (sân thượng/tầng kỹ thuật, 3.2.6.2 b)")
            f.message += " — " + "; ".join(ly)
            f.status = "xm"
        yield f


@register("basement_smoke_lobby")
def basement_smoke_lobby(rule: dict, d: DrawingExtraction) -> Iterable[Finding]:
    """SĐ1:2023: tại mọi tầng hầm, >= 1 lối vào buồng thang thoát nạn phải qua sảnh ngăn khói
    (vách ngăn cháy loại 1). Tool chỉ kiểm tra có thể hiện sảnh ngăn khói/khoang đệm hay không."""
    if not (d.building.basements or 0) > 0:
        return
    pages = [p for p in d.pages if re.search(r"HẦM", p.sheet_title or "", re.I)] or d.pages
    has_lobby = any(e.get("is_smoke_lobby") for p in pages for e in p.elements)
    if not has_lobby:
        yield make_finding(rule, None)
