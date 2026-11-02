"""Bảng đối chiếu PCCC: chọn bảng, đọc dòng tiết kiệm token, lập file kết quả, gộp phần của agent, kiểm, xuất.

  python -X utf8 bang_doi_chieu.py danhmuc
  python -X utf8 bang_doi_chieu.py chon --cong-nang "khách sạn" [--cao 18.4] [--co bao_chay=1,sprinkler=0]
  python -X utf8 bang_doi_chieu.py khung KT7.1 [--tu 40] [--den 80]
  python -X utf8 bang_doi_chieu.py dong KT7.1 "#85"        (hoặc theo TT: "6.4")
  python -X utf8 bang_doi_chieu.py tim KT7.1 "hành lang"
  python -X utf8 bang_doi_chieu.py lamviec KS -o ket_qua/<dự án>/doi_chieu.json [--loai khao_sat|tham_dinh]
  python -X utf8 bang_doi_chieu.py job ket_qua/<dự án>/doi_chieu.json KT7.1 [--tu 0 --den 70]   (-> jobs/, phan/)
  python -X utf8 bang_doi_chieu.py gop ket_qua/<dự án>/doi_chieu.json phan1.json [phan2.json …] [--giu-so]
  python -X utf8 bang_doi_chieu.py kiem ket_qua/<dự án>/doi_chieu.json
  python -X utf8 bang_doi_chieu.py xlsx ket_qua/<dự án>/doi_chieu.json [-o ra.xlsx]
  python -X utf8 bang_doi_chieu.py md ket_qua/<dự án>/doi_chieu.json [--ma KS] [--chi-loi] [--cat 300]

Bảng A1–A26 (hệ thống theo công năng), B1–B14 (theo hệ thống), KT (kiến trúc theo công năng + chiều cao) chép từ
SKILL-PCCC-NEW 24743f9 (data/bang); KS là khung khảo sát – tư vấn của TKPCCC. Cách dùng: SKILL.md thư mục cha.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path

DATA = Path(__file__).resolve().parents[1] / "data"
KET_LUAN = {"DAT": "ĐẠT", "CHUA_DAT": "CHƯA ĐẠT", "CAN_XAC_NHAN": "CẦN XÁC NHẬN", "KHONG_AP_DUNG": "KHÔNG ÁP DỤNG"}
LOI_KL = ("CHUA_DAT", "CAN_XAC_NHAN")
SUA = ("thiet_ke", "ket_luan", "ly_do", "phuong_an", "khung", "nguon")   # trường agent được điền

# Bảng hệ thống theo công năng (A) — thứ tự quan trọng: mẫu cụ thể trước mẫu chung (chép chon_bang.ts).
LUAT_A = [(r"karaoke|vũ trường|F2\.1", "A12"), (r"hỗn hợp", "A3"), (r"chung cư|nhà ở tập thể|F1\.3", "A2"),
          (r"khách sạn|nhà nghỉ|F1\.2", "A5"), (r"văn phòng|F4\.3", "A4"), (r"nhà trẻ|mẫu giáo|mầm non", "A6"),
          (r"trường|tiểu học|THCS|THPT|phổ thông", "A7"), (r"bệnh viện|phòng khám|y tế", "A8"),
          (r"trung tâm thương mại|TTTM|siêu thị|chợ", "A9"), (r"sân vận động|nhà thi đấu|thể thao", "A10"),
          (r"rạp chiếu|rạp hát|cinema", "A11"), (r"gara|bãi đỗ xe|nhà để xe", "A13"), (r"cửa hàng xăng|cây xăng", "A16"),
          (r"kho xăng", "A18"), (r"LPG", "A19"), (r"LNG", "A20"), (r"trạm chiết|san chiết|khí đốt", "A17"),
          (r"vật liệu nổ", "A21"), (r"trạm biến áp", "A22"), (r"hầm đường bộ", "A23"), (r"tàu biển", "A24"),
          (r"thủy nội địa.*hàng", "A25"), (r"thủy nội địa|tàu khách", "A26"), (r"sản xuất|nhà xưởng|xưởng|F5\.1", "A14"),
          (r"kho|F5\.2", "A15"), (r"hạ tầng", "A1")]
# Bảng kiến trúc (thẩm định xây dựng): theo công năng, có bảng chia theo mốc chiều cao PCCC.
LUAT_KT = [(r"karaoke|vũ trường|F2\.1", "KT15"), (r"hỗn hợp", ([28, 50], ["KT5.1", "KT5.2", "KT5.3"])),
           (r"chung cư|nhà ở tập thể|F1\.3", ([28, 75], ["KT4.1", "KT4.2", "KT4.3"])),
           (r"khách sạn|nhà nghỉ|F1\.2", ([28, 50], ["KT7.1", "KT7.2", "KT7.3"])),
           (r"văn phòng|F4\.3", ([28, 50], ["KT6.1", "KT6.2", "KT6.3"])), (r"nhà trẻ|mẫu giáo|mầm non", "KT8"),
           (r"trường|tiểu học|THCS|THPT|phổ thông", "KT9"), (r"bệnh viện|phòng khám|y tế", "KT10"),
           (r"trung tâm thương mại|TTTM|siêu thị", "KT12"), (r"chợ", "KT11"), (r"sân vận động|nhà thi đấu|thể thao", "KT13"),
           (r"rạp chiếu|rạp hát|cinema", "KT14"), (r"gara|bãi đỗ xe|nhà để xe", "KT16"), (r"cửa hàng xăng|cây xăng", "KT19"),
           (r"kho xăng", "KT20"), (r"LPG", "KT24"), (r"LNG", "KT25"), (r"vật liệu nổ", "KT26"), (r"điện mặt trời", "KT28"),
           (r"trạm biến áp", "KT29"), (r"hầm đường bộ", "KT30"), (r"sản xuất|nhà xưởng|xưởng|F5\.1", "KT17"),
           (r"kho|F5\.2", "KT18")]
HE_THONG_BANG = {"tram_bom": "B3", "ngoai_nha": "B4", "hong_nuoc": "B5", "sprinkler": "B6", "bot_co_dinh": "B7",
                 "khi_hoa_long": "B8", "khi_nen": "B9", "khi": "B10", "sol_khi": "B11"}
BANG_LUON_XET = ["B12", "B13", "B14"]   # bình + dụng cụ, đèn sự cố + loa, điện PCCC: mọi công trình


def doc_json(p: Path):
    return json.loads(p.read_text(encoding="utf-8"))


def ghi_json(p: Path, obj) -> None:
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(json.dumps(obj, ensure_ascii=False, indent=1), encoding="utf-8")


def doc_bang(ma: str) -> dict:
    f = {p.stem.lower(): p for p in DATA.glob("*.json") if p.stem != "index"}.get(ma.lower())
    if not f:
        raise SystemExit(f"Không có bảng {ma}; xem lệnh danhmuc")
    return doc_json(f)


def phan_cua(ma: str) -> str:
    return "khao_sat" if ma == "KS" else "xay_dung" if ma.startswith("KT") else "he_thong"


def la_so_cot(r: dict) -> bool:
    """Dòng đánh số cột (1, 2, 3, 5, 6) ở đầu một số bảng gốc."""
    return str(r.get("noi_dung", "")).strip().isdigit() and str(r.get("quy_dinh", "")).strip().isdigit()


def can_xet(ma: str, r: dict) -> bool:
    if r.get("nhom") or la_so_cot(r):
        return False
    return ma == "KS" or bool(str(r.get("quy_dinh", "")).strip() or str(r.get("khoan_dieu", "")).strip())


def chon(cong_nang: str, cao: float | None, co: dict) -> dict:
    tim = lambda luat: next((ma for re_, ma in luat if re.search(re_, cong_nang, re.I)), None)  # noqa: E731
    kt = tim(LUAT_KT)
    if isinstance(kt, tuple):
        moc, ds = kt
        kt = ds[0] if cao is None else next((m for h, m in zip(moc, ds) if cao <= h), ds[-1])
    ht = [{"ma": m, "ly_do": "xét cho mọi công trình"} for m in BANG_LUON_XET]
    if co.get("bao_chay_dia_chi") is True:
        ht.append({"ma": "B2", "ly_do": "thiết kế có báo cháy địa chỉ"})
    elif co.get("bao_chay") is not False:
        ht.append({"ma": "B1", "ly_do": "thiết kế có báo cháy" if co.get("bao_chay") else "chưa rõ có hay không: xét để kết luận đối tượng trang bị"})
    for k, m in HE_THONG_BANG.items():
        if co.get(k) is True:
            ht.append({"ma": m, "ly_do": "thiết kế có hệ thống"})
        elif co.get(k) is None and k in ("sprinkler", "hong_nuoc"):
            ht.append({"ma": m, "ly_do": "chưa rõ có hay không: xét để kết luận đối tượng trang bị"})
    ht.sort(key=lambda x: int(x["ma"][1:]))
    a = tim(LUAT_A)
    ra = {"xay_dung": {"ma": kt, "ly_do": f"công năng {cong_nang!r}, cao PCCC {cao if cao is not None else 'chưa rõ (lấy mức thấp nhất)'}"
                       if kt else "chưa có bảng kiến trúc: chọn tay"},
          "cong_nang": {"ma": a, "ly_do": f"công năng {cong_nang!r}" if a else "chưa nhận ra công năng: chọn tay"},
          "he_thong": ht}
    ra["lenh_lamviec"] = ",".join(m for m in [kt, a] + [x["ma"] for x in ht] if m)
    return ra


def _co(s: str | None) -> dict:
    ra = {}
    for kv in filter(None, (s or "").split(",")):
        k, _, v = kv.partition("=")
        ra[k.strip()] = {"1": True, "0": False}.get(v.strip())
    return ra


def _ngan(s, n: int) -> str:
    s = " ".join(str(s or "").split())
    return s if len(s) <= n else s[: n - 1] + "…"


def lamviec(mas: list[str], ra: Path, loai: str) -> dict:
    dc = doc_json(ra) if ra.exists() else {
        "loai": loai, "ban_ve": "ban_ve.pdf", "zoom": 1.6,
        "cong_trinh": {"ten": "", "dia_diem": "", "chu_dau_tu": "", "cong_nang": "", "nhom_f": "", "so_tang_noi": None,
                       "so_tang_ham": None, "chieu_cao_pccc_m": None, "dien_tich_san_m2": None, "khoi_tich_m3": None,
                       "bac_chiu_lua": "", "cap_s": "", "he_thong": {}, "nguon": {}, "thieu": []},
        "bang": [], "ghi_chu": []}
    co = {b["ma"] for b in dc["bang"]}
    for ma in mas:
        b = doc_bang(ma)
        if b["ma"] in co:
            continue
        dong = [{"idx": r["idx"], "tt": r.get("tt", ""), "noi_dung": r.get("noi_dung", ""), "quy_dinh": r.get("quy_dinh", ""),
                 "khoan_dieu": r.get("khoan_dieu", ""), "goi_y": r.get("goi_y", ""), "tra_o": r.get("tra_o", ""),
                 "nhom": bool(r.get("nhom")), "xet": can_xet(b["ma"], r), "thiet_ke": "", "ket_luan": "", "ly_do": "",
                 "phuong_an": [], "khung": [], "nguon": ""}
                for r in b["dong"] if not la_so_cot(r)]
        dc["bang"].append({"ma": b["ma"], "ten": b["ten"], "phan": phan_cua(b["ma"]), "dau_bang": b.get("dau_bang", []),
                           "dong": dong})
    ghi_json(ra, dc)
    return {"ok": True, "file": str(ra), "bang": [(b["ma"], sum(r["xet"] for r in b["dong"])) for b in dc["bang"]]}


def gop(ra: Path, phan: list[Path], giu_so: bool) -> dict:
    """Gộp file phần (agent điền) vào doi_chieu.json theo idx; số khung đánh lại nối tiếp (trừ --giu-so)."""
    dc = doc_json(ra)
    bang = {b["ma"]: b for b in dc["bang"]}
    tiep = max([k for b in dc["bang"] for r in b["dong"] for k in r["khung"]] + [g.get("khung") or 0 for g in dc["ghi_chu"]] + [0]) + 1
    dem = 0
    for f in phan:
        p = doc_json(f)
        if p.get("ma") not in bang:
            raise SystemExit(f"{f}: bảng {p.get('ma')!r} chưa có trong {ra.name} (chạy lamviec trước)")
        doi = {}
        if not giu_so:
            for k in sorted({k for r in p.get("dong", []) for k in r.get("khung", [])} | {g["khung"] for g in p.get("ghi_chu", []) if g.get("khung") is not None}):
                doi[k], tiep = tiep, tiep + 1
        theo_idx = {r["idx"]: r for r in bang[p["ma"]]["dong"]}
        for r in p.get("dong", []):
            if r.get("idx") not in theo_idx:
                raise SystemExit(f"{f}: bảng {p['ma']} không có dòng idx {r.get('idx')}")
            for k in SUA:
                if k in r:
                    theo_idx[r["idx"]][k] = [doi.get(x, x) for x in r[k]] if k == "khung" else r[k]
            dem += 1
        for g in p.get("ghi_chu", []):
            dc["ghi_chu"].append({**g, "khung": doi.get(g.get("khung"), g.get("khung"))})
        for k, v in p.get("cong_trinh", {}).items():          # agent bổ sung thông tin công trình còn trống
            if dc["cong_trinh"].get(k) in (None, "", [], {}):
                dc["cong_trinh"][k] = v
    ghi_json(ra, dc)
    return {"ok": True, "dong_da_gop": dem, "ghi_chu": len(dc["ghi_chu"])}


def job(f: Path, ma: str, tu: int, den: int, ra: Path, phan: Path) -> dict:
    """File giao việc cho agent: thông tin công trình + các dòng chưa kết luận của một bảng (khoảng idx)."""
    dc = doc_json(f)
    b = next((b for b in dc["bang"] if b["ma"].lower() == ma.lower()), None)
    if not b:
        raise SystemExit(f"{f.name} chưa có bảng {ma} (chạy lamviec trước)")
    dong = [r for r in b["dong"] if r["xet"] and not r["ket_luan"] and tu <= r["idx"] <= den]
    phan_ten = {"xay_dung": "XÂY DỰNG", "he_thong": "HỆ THỐNG PCCC", "khao_sat": "KHẢO SÁT"}[b["phan"]]
    cmd = ".claude/skills/tkpccc-bang-doi-chieu/scripts/bang_doi_chieu.py"
    t = [f"# Job {b['ma']} – {b['ten']} – phần {phan_ten} (idx {tu}–{min(den, b['dong'][-1]['idx'])})", "",
         "## Thông tin công trình đã biết", "```json", json.dumps(dc["cong_trinh"], ensure_ascii=False, indent=1), "```",
         f"Bản vẽ: `{f.parent / dc.get('ban_ve', 'ban_ve.pdf')}`. Ảnh trang có lưới: `{f.parent / 'anh'}/luoi_<trang>.png` "
         f"(zoom {dc.get('zoom', 1.6)}; thiếu thì tạo bằng lệnh `luoi` của skill tkpccc-ghi-chu-ban-ve).", "",
         f"## Dòng cần xét ({len(dong)}) — toàn văn từng dòng: `python -X utf8 {cmd} dong {b['ma']} \"#<idx>\"`", "",
         "| idx | TT | Nội dung đối chiếu | Khoản Điều | Tra ở |", "|---|---|---|---|---|"]
    t += [f"| {r['idx']} | {r['tt']} | {_ngan(r['noi_dung'], 70)} | {_ngan(r['khoan_dieu'], 35)} | {_ngan(r['tra_o'], 35)} |" for r in dong]
    t += ["", "## Trả lời", f"Ghi ĐÚNG MỘT JSON vào `{phan}` theo mục \"File phần\" của `.claude/skills/tkpccc-tham-dinh/SKILL.md` "
          f"(\"ma\": \"{b['ma']}\"; số khung đánh từ 1 trong file này, lệnh gop tự đánh lại)."]
    ra.parent.mkdir(parents=True, exist_ok=True)
    ra.write_text("\n".join(t) + "\n", encoding="utf-8")
    return {"ok": True, "job": str(ra), "phan": str(phan), "so_dong": len(dong)}


def kiem(dc: dict) -> dict:
    loi, canh_bao, dem = [], [], {}
    khung_ve = {g.get("khung") for g in dc.get("ghi_chu", []) if g.get("khung") is not None}
    khung_dong = set()
    for b in dc["bang"]:
        d = dem.setdefault(b["ma"], {k: 0 for k in list(KET_LUAN) + ["chua_xet"]})
        for r in b["dong"]:
            ten = f"{b['ma']} #{r['idx']} (TT {r['tt'] or '-'})"
            kl = r.get("ket_luan", "")
            if kl and kl not in KET_LUAN:
                loi.append(f"{ten}: ket_luan {kl!r} không hợp lệ ({', '.join(KET_LUAN)})")
                continue
            khung_dong |= set(r.get("khung", []))
            if not r.get("xet") and not kl:
                continue
            if not kl:
                d["chua_xet"] += 1
                continue
            d[kl] += 1
            if kl != "DAT" and not str(r.get("ly_do", "")).strip():
                loi.append(f"{ten}: {KET_LUAN[kl]} nhưng thiếu lý do")
            if kl in LOI_KL and not r.get("khung"):
                canh_bao.append(f"{ten}: {KET_LUAN[kl]} chưa có khung trên bản vẽ")
            if kl == "CHUA_DAT" and not r.get("phuong_an"):
                canh_bao.append(f"{ten}: CHƯA ĐẠT chưa có phương án điều chỉnh")
    for k in sorted(khung_dong - khung_ve):
        canh_bao.append(f"khung {k}: dòng có ghi nhưng chưa vẽ trên PDF (ghi_chu)")
    for k in sorted(khung_ve - khung_dong):
        canh_bao.append(f"khung {k}: có trên PDF nhưng không dòng nào ghi")
    return {"ok": not loi, "dem": dem, "loi": loi, "canh_bao": canh_bao}


def o_ket_luan(r: dict) -> str:
    if not r.get("ket_luan"):
        return "" if not r.get("xet") else "(chưa xét)"
    t = KET_LUAN[r["ket_luan"]] + (f": {r['ly_do']}" if r.get("ly_do") else "")
    return "\n".join([t] + [str(x) for x in r.get("phuong_an", [])])


def md(dc: dict, ma: str | None, chi_loi: bool, cat: int) -> str:
    ra = []
    for b in dc["bang"]:
        if ma and b["ma"].lower() != ma.lower():
            continue
        ra += [f"### {b['ma']} – {b['ten']}", "",
               "| TT | Nội dung đối chiếu | Nội dung thiết kế | Quy định | Khoản Điều | Kết luận | Khung |", "|---|---|---|---|---|---|---|"]
        o = lambda s: _ngan(s, cat).replace("|", "/") if cat else " ".join(str(s or "").split()).replace("|", "/")  # noqa: E731
        for r in b["dong"]:
            if chi_loi and r.get("ket_luan") not in LOI_KL:
                continue
            if r.get("nhom"):
                ra.append(f"| **{r['tt']}** | **{r['noi_dung']}** | | | | | |")
                continue
            if not r.get("xet") and not r.get("ket_luan") and not chi_loi:
                ra.append(f"| {r['tt']} | {o(r['noi_dung'])} | | | | | |")
                continue
            kl = o_ket_luan(r).replace("\n", " · ")
            ra.append(f"| {r['tt']} | {o(r['noi_dung'])} | {o(r.get('thiet_ke'))} | {o(r.get('quy_dinh'))} | {o(r.get('khoan_dieu'))} | "
                      f"{kl.replace('|', '/')} | {', '.join(map(str, r.get('khung', [])))} |")
        ra.append("")
    return "\n".join(ra)


def xlsx(dc: dict, ra: Path) -> dict:
    from openpyxl import Workbook
    from openpyxl.styles import Alignment, Border, Font, PatternFill, Side

    nen = {"CHUA_DAT": "F8CBAD", "CAN_XAC_NHAN": "FFE699", "DAT": "C6EFCE", "KHONG_AP_DUNG": "EDEDED"}
    vien = Border(*(Side(style="thin", color="999999"),) * 4)
    boc = Alignment(wrap_text=True, vertical="top")
    wb = Workbook()
    ws = wb.active
    ws.title = "Tóm tắt"
    ws.append(["BẢNG ĐỐI CHIẾU PCCC – " + ("KHẢO SÁT, TƯ VẤN" if dc.get("loai") == "khao_sat" else "THẨM ĐỊNH (sơ bộ)")])
    ws["A1"].font = Font(bold=True, size=13)
    for k, v in dc.get("cong_trinh", {}).items():
        if v not in (None, "", [], {}):
            ws.append([k, json.dumps(v, ensure_ascii=False) if isinstance(v, (dict, list)) else v])
    ws.append([])
    ws.append(["Bảng", "Tên"] + list(KET_LUAN.values()) + ["Chưa xét"])
    dem = kiem(dc)["dem"]
    for b in dc["bang"]:
        ws.append([b["ma"], b["ten"]] + [dem[b["ma"]][k] for k in list(KET_LUAN) + ["chua_xet"]])
    ws.column_dimensions["A"].width, ws.column_dimensions["B"].width = 22, 60
    for b in dc["bang"]:
        s = wb.create_sheet(b["ma"][:31])
        s.append([f"{b['ma']} – {b['ten']}"])
        s["A1"].font = Font(bold=True, size=12)
        s.append(["TT", "Nội dung đối chiếu", "Nội dung thiết kế", "Quy định", "Khoản Điều", "Kết luận", "Khung"])
        for c in s[2]:
            c.font, c.fill, c.border, c.alignment = Font(bold=True), PatternFill("solid", fgColor="D9E1F2"), vien, boc
        for r in b["dong"]:
            s.append([r["tt"], r["noi_dung"], r.get("thiet_ke", ""), r.get("quy_dinh", ""), r.get("khoan_dieu", ""),
                      o_ket_luan(r), ", ".join(map(str, r.get("khung", [])))])
            for c in s[s.max_row]:
                c.border, c.alignment = vien, boc
                if r.get("nhom"):
                    c.font, c.fill = Font(bold=True), PatternFill("solid", fgColor="EDEDED")
            if r.get("ket_luan") in nen:
                s.cell(s.max_row, 6).fill = PatternFill("solid", fgColor=nen[r["ket_luan"]])
        for col, w in zip("ABCDEFG", (7, 34, 40, 60, 22, 55, 8)):
            s.column_dimensions[col].width = w
        s.freeze_panes = "A3"
    ra.parent.mkdir(parents=True, exist_ok=True)
    wb.save(ra)
    return {"ok": True, "xlsx": str(ra), "sheet": len(wb.sheetnames)}


def main(argv=None):
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    ap = argparse.ArgumentParser(prog="bang_doi_chieu", description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="lenh", required=True)
    sub.add_parser("danhmuc")
    c = sub.add_parser("chon")
    c.add_argument("--cong-nang", required=True)
    c.add_argument("--cao", type=float)
    c.add_argument("--co")
    for ten in ("khung", "dong", "tim"):
        x = sub.add_parser(ten)
        x.add_argument("ma")
        if ten != "khung":
            x.add_argument("khoa")
        else:
            x.add_argument("--tu", type=int, default=0)
            x.add_argument("--den", type=int, default=10**6)
    lv = sub.add_parser("lamviec")
    lv.add_argument("ma")
    lv.add_argument("-o", "--ra", required=True)
    lv.add_argument("--loai", choices=("khao_sat", "tham_dinh"))
    g = sub.add_parser("gop")
    g.add_argument("file")
    g.add_argument("phan", nargs="+")
    g.add_argument("--giu-so", action="store_true")
    j = sub.add_parser("job")
    j.add_argument("file")
    j.add_argument("ma")
    j.add_argument("--tu", type=int, default=0)
    j.add_argument("--den", type=int, default=10**6)
    k = sub.add_parser("kiem")
    k.add_argument("file")
    x = sub.add_parser("xlsx")
    x.add_argument("file")
    x.add_argument("-o", "--ra")
    m = sub.add_parser("md")
    m.add_argument("file")
    m.add_argument("--ma")
    m.add_argument("--chi-loi", action="store_true")
    m.add_argument("--cat", type=int, default=0)
    a = ap.parse_args(argv)

    if a.lenh == "danhmuc":
        for b in doc_json(DATA / "index.json"):
            print(f"{b['ma']:6} {b['so_dong']:4} dòng  {b['ten']}")
        return
    if a.lenh == "chon":
        print(json.dumps(chon(a.cong_nang, a.cao, _co(a.co)), ensure_ascii=False, indent=1))
        return
    if a.lenh in ("khung", "dong", "tim"):
        b = doc_bang(a.ma)
        for r in b["dong"]:
            if a.lenh == "khung":
                if a.tu <= r["idx"] <= a.den and not la_so_cot(r):
                    print(f"#{r['idx']:<4} {r.get('tt', ''):6} {_ngan(r['noi_dung'], 80):80} | {_ngan(r.get('khoan_dieu'), 40):40} | {_ngan(r.get('tra_o'), 40)}")
            elif (a.lenh == "dong" and (a.khoa == f"#{r['idx']}" or a.khoa == r.get("tt"))) or \
                    (a.lenh == "tim" and a.khoa.lower() in json.dumps(r, ensure_ascii=False).lower()):
                print(json.dumps(r, ensure_ascii=False, indent=1) if a.lenh == "dong" else
                      f"#{r['idx']:<4} {r.get('tt', ''):6} {_ngan(r['noi_dung'], 90)} | {_ngan(r.get('khoan_dieu'), 40)}")
        return
    if a.lenh == "lamviec":
        mas = [s.strip() for s in a.ma.split(",") if s.strip()]
        loai = a.loai or ("khao_sat" if mas == ["KS"] else "tham_dinh")
        print(json.dumps(lamviec(mas, Path(a.ra), loai), ensure_ascii=False))
        return
    f = Path(a.file)
    if a.lenh == "gop":
        print(json.dumps(gop(f, [Path(p) for p in a.phan], a.giu_so), ensure_ascii=False))
    elif a.lenh == "job":
        ten = a.ma + (f"_{a.tu}-{a.den}" if a.tu or a.den < 10**6 else "")
        print(json.dumps(job(f, a.ma, a.tu, a.den, f.parent / "jobs" / f"{ten}.md", f.parent / "phan" / f"{ten}.json"),
                         ensure_ascii=False))
    elif a.lenh == "kiem":
        kq = kiem(doc_json(f))
        print(json.dumps(kq, ensure_ascii=False, indent=1))
        if not kq["ok"]:
            sys.exit(1)
    elif a.lenh == "xlsx":
        print(json.dumps(xlsx(doc_json(f), Path(a.ra) if a.ra else f.with_suffix(".xlsx")), ensure_ascii=False))
    else:
        print(md(doc_json(f), a.ma, a.chi_loi, a.cat))


if __name__ == "__main__":
    main()
