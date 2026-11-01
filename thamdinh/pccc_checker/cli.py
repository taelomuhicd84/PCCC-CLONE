"""CLI rút gọn cho TKPCCC (offline, không API key):

  python -m pccc_checker run <pdf> --out <thư mục> [--pages auto|all|1-3,5] [--set height_pccc_m=24 ...]

extract (chữ vector) -> check (luật YAML) -> <tên>_annotated.pdf -> findings.jsonl (D / KD / XM).
In 1 dòng JSON tóm tắt ra stdout; nhật ký ra stderr.
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path

import yaml

from .config import RULES_DIR, load_settings
from .models import DrawingExtraction, Finding

TEN = {"function_group": "nhóm F (công năng)", "height_pccc_m": "chiều cao PCCC", "fire_resistance_level": "bậc chịu lửa",
       "structural_hazard_class": "cấp nguy hiểm cháy kết cấu (S)", "floors_above": "số tầng nổi", "basements": "số tầng hầm"}
D_CHECKS = {"min_attr", "max_attr", "bool_attr", "required_attr", "min_fire_rating", "compare_attrs"}
KHONG_DOC_DUOC = {"missing_data", "building_missing"}     # máy không đọc được số liệu, chưa phải vi phạm -> XM
TEN_THUOC_TINH = {"fire_rating": "giới hạn chịu lửa (EI)", "riser_m": "chiều cao bậc", "tread_m": "mặt bậc",
                  "flight_width_m": "bản thang", "landing_width_m": "chiếu thang"}
DAU = {">=": "≥", "<=": "≤"}


def _so(v) -> str:
    """Giá trị hiện trên web: số kiểu Việt Nam (0,9), đúng/sai -> có/không."""
    if isinstance(v, bool):
        return "có" if v else "không"
    return f"{v:g}".replace(".", ",") if isinstance(v, (int, float)) else str(v)


def _pages(spec: str | None) -> list[int] | str | None:
    if not spec or spec == "all":
        return None
    if spec == "auto":
        return "auto"
    res: list[int] = []
    for part in spec.split(","):
        a, _, b = part.partition("-")
        res.extend(range(int(a), int(b or a) + 1))
    return res


def _building(kv: list[str] | None) -> dict:
    data = {}
    for item in kv or []:
        k, v = item.split("=", 1)
        data[k.strip()] = yaml.safe_load(v)
    return data


def _write_json(path: Path, obj) -> None:
    path.write_text(json.dumps(obj, ensure_ascii=False, indent=2), encoding="utf-8")


def _attrs(rule: dict) -> list[str]:
    return [rule[k] for k in ("left", "right") if k in rule] or [rule.get("attr", "fire_rating")]


def dong_dat(rules: list[dict], d: DrawingExtraction, findings: list[Finding]) -> list[dict]:
    """Luật có phần tử đo được số liệu mà không sinh kết quả nào -> D. Luật im lặng vì không khớp điều kiện
    hoặc thiếu số liệu KHÔNG phải D."""
    from .pipeline.consistency import cd_of
    from .rules.engine import context, resolve_value, targets
    co_ket_qua = {f.rule_id for f in findings}
    rows = []
    for r in rules:
        if not r.get("enabled", True) or r["check"] not in D_CHECKS or r["id"] in co_ket_qua:
            continue
        attrs = _attrs(r)
        els = [e for e in targets(r, d) if all(e.get(a) is not None for a in attrs)]
        if not els:
            continue
        if r["check"] == "compare_attrs":
            ten = [TEN_THUOC_TINH.get(a, a) for a in attrs]
            yc = f"{ten[0]} {DAU.get(r.get('op', '>='), r.get('op'))} {ten[1]}"
        elif r["check"] == "required_attr":
            yc = f"ghi {TEN_THUOC_TINH.get(r['attr'], r['attr'])}"
        else:
            yc = "; ".join(sorted({_so(resolve_value(r, context(e, d))[0]) for e in els}))
        rows.append({"id": None, "hm": "HM01", "tang": None, "cd": cd_of(r.get("clause", "")), "rule": r["id"],
                     "yc": yc, "ht": "; ".join(f"{e.label or e.id}: " + ", ".join(_so(e.get(a)) for a in attrs) for e in els),
                     "tt": "D", "ly_do": f"{r.get('title', r['id'])}: đạt", "truc": None, "trang": els[0].page,
                     "bbox": None, "conf": "H", "evid": " / ".join(e.label for e in els if e.label) or None,
                     "dieu_khoan": r.get("clause")})
    return rows


def ket_luan(rules: list[dict], d: DrawingExtraction, findings: list[Finding]) -> tuple[list[dict], list[str]]:
    """findings của rule engine -> dòng findings.jsonl (D/KD/XM) đã qua validate (KD thiếu bằng chứng -> XM)."""
    from .pipeline.consistency import from_engine, validate
    luat = {r["id"]: r for r in rules}
    rows = from_engine([f.to_dict() for f in findings], d.to_dict())
    for i, (row, f) in enumerate(zip(rows, findings), 1):   # không finding nào bị bác bỏ nên khớp 1-1
        if f.status != "cannot_check":
            row["so"] = i      # = nhãn #i trên PDF ghi chú (annotate bỏ qua dòng cannot_check)
        r = luat.get(row["rule"], {})
        if row["tt"] == "KD" and r.get("check") in KHONG_DOC_DUOC:
            row["tt"] = "XM"
            row["ly_do"] += " [máy không đọc được số liệu, chưa phải vi phạm]"
        if r.get("check") == "required_attr":
            row["yc"], row["ht"] = f"ghi {TEN_THUOC_TINH.get(r['attr'], r['attr'])}", "chưa ghi"
    return validate(rows + dong_dat(rules, d, findings))


def cmd_run(a) -> dict:
    from .extractors import make_extractor
    from .output.annotate_pdf import annotate
    from .output.report_md import extraction_md, findings_md
    from .pipeline.consistency import TT, write_jsonl
    from .rules import load_rules, run_rules
    from .rules.engine import BUILDING_KEYS

    settings = load_settings()
    pdf = Path(a.pdf)
    out = Path(a.out)
    out.mkdir(parents=True, exist_ok=True)
    log = lambda m: print(m, file=sys.stderr)  # noqa: E731

    d = make_extractor("vector").extract(pdf, _pages(a.pages), _building(a.set), log=log)
    _write_json(out / "extraction.json", d.to_dict())
    (out / "extraction.md").write_text(extraction_md(d), encoding="utf-8")

    rules = load_rules([RULES_DIR / p for p in settings["rules"]["packs"]])
    blocked = {k: f"chưa có {TEN.get(k, k)} ({k})" for k in sorted(BUILDING_KEYS) if getattr(d.building, k) is None}
    findings = run_rules(rules, d, blocked=blocked)
    pdf_out = out / f"{pdf.stem}_annotated.pdf"
    annotate(pdf, findings, pdf_out, settings["annotate"]["min_severity"])
    _write_json(out / "findings.json", [f.to_dict() for f in findings])
    (out / "report.md").write_text(findings_md(d, findings, pdf_out.name), encoding="utf-8")

    rows, changes = ket_luan(rules, d, findings)
    write_jsonl(out / "findings.jsonl", rows)
    for x in changes:
        log(f"  {x}")
    building = {k: v for k, v in vars(d.building).items() if v not in (None, [])}
    return {"ok": True, "pdf_ghi_chu": pdf_out.name, "trang_da_doc": [p.page for p in d.pages],
            "thong_tin": building, "dem": {t: sum(r["tt"] == t for r in rows) for t in TT if t != "NA"}}


def main(argv: list[str] | None = None) -> None:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
        sys.stderr.reconfigure(encoding="utf-8")
    p = argparse.ArgumentParser(prog="pccc_checker", description=__doc__,
                                formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = p.add_subparsers(dest="cmd", required=True)
    sp = sub.add_parser("run", help="thẩm định 1 file PDF")
    sp.add_argument("pdf")
    sp.add_argument("--out", required=True, help="thư mục kết quả")
    sp.add_argument("--pages", default="auto", help="auto (chỉ trang liên quan thoát nạn) | all | vd 1-3,5")
    sp.add_argument("--set", action="append", help="thông tin công trình, vd --set height_pccc_m=24 --set function_group=F1.3")
    a = p.parse_args(argv)
    print(json.dumps(cmd_run(a), ensure_ascii=False))


if __name__ == "__main__":
    main()
