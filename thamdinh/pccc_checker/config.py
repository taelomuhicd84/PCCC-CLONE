"""Nạp cấu hình: config/settings.yaml (bản rút gọn, không .env, không Gemini)."""
from __future__ import annotations

from pathlib import Path
from typing import Any

import yaml

PROJECT_ROOT = Path(__file__).resolve().parents[1]      # thamdinh/
CONFIG_DIR = PROJECT_ROOT / "config"
RULES_DIR = PROJECT_ROOT / "rules"

DEFAULTS: dict[str, Any] = {
    "rules": {"packs": ["qcvn06_2022_sd01_2023.yaml"]},
    "annotate": {"min_severity": "info", "font_size": 7},
}


def _merge(a: dict, b: dict) -> dict:
    out = dict(a)
    for k, v in b.items():
        out[k] = _merge(out[k], v) if isinstance(v, dict) and isinstance(out.get(k), dict) else v
    return out


def load_settings(path: Path | None = None) -> dict[str, Any]:
    path = path or CONFIG_DIR / "settings.yaml"
    data = yaml.safe_load(path.read_text(encoding="utf-8")) if path.exists() else {}
    return _merge(DEFAULTS, data or {})
