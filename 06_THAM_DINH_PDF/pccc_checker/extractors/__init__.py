"""Factory chọn extractor. Bản TKPCCC chỉ giữ extractor vector (offline, không API key)."""
from __future__ import annotations

from .base import Extractor
from .vector_extractor import VectorTextExtractor


def make_extractor(kind: str = "vector") -> Extractor:
    if kind == "vector":
        return VectorTextExtractor()
    raise ValueError(f"extractor không hợp lệ: {kind} (chỉ có 'vector')")
