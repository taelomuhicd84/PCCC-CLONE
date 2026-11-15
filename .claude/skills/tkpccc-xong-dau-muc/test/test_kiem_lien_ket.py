"""Gọi scripts/kiem_lien_ket.py: mục LỖI (đường dẫn chết trong tài liệu, mã bảng, frontmatter, nguyên văn rải...) làm hỏng test;
CẢNH BÁO (lỗi thật đã biết, chưa sửa: văn bản cũ, lệnh CAD chưa khai báo...) chỉ in."""
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "scripts"))
import kiem_lien_ket as kl  # noqa: E402


def test_khong_co_loi_lien_ket():
    loi, canh = kl.main()
    for c in canh:
        print("CANH BAO", c)
    assert not loi, "\n".join(loi)


def test_bat_duoc_duong_dan_chet(tmp_path, monkeypatch):
    f = tmp_path / "CLAUDE.md"
    f.write_text("xem `knowledge/khong-co-file-nay.md`\n", encoding="utf-8")
    for ten in ("STRUCTURE.md", "README.md"):
        (tmp_path / ten).write_text("", encoding="utf-8")
    monkeypatch.setattr(kl, "ROOT", tmp_path)
    monkeypatch.setattr(kl, "mo_ta", lambda: ["CLAUDE.md"])
    (tmp_path / "data").mkdir()
    (tmp_path / "data/huong_dan.json").write_text('{"buoc": []}', encoding="utf-8")
    kl.LOI.clear()
    kl.phep1()
    assert any("khong-co-file-nay.md" in x for x in kl.LOI)
