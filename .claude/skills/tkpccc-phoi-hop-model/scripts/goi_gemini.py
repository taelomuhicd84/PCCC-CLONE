"""Gọi Gemini không giao diện (Antigravity CLI `agy`, dự phòng Gemini CLI) để bóc số liệu từ PDF/ảnh dài, trả về file kết quả nhỏ cho Claude đọc.

Claude chỉ đọc file --out (vài KB), không đọc PDF gốc: tiết kiệm hạn mức Claude.
Gemini chạy trong thư mục tạm chỉ chứa file cần đọc, chế độ plan (chỉ đọc), nên không thấy repo hay hồ sơ khác.

Dùng:
  python goi_gemini.py --file ban_ve.pdf --trang 3-5 --yeu-cau "Liệt kê tên trục và chuỗi kích thước" --json --out ra.json
  python goi_gemini.py --kiem          (thử đăng nhập + liệt kê model)
"""
import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import tempfile


def cong_cu() -> tuple[str, str]:
    """Ưu tiên Antigravity CLI (agy, đăng nhập Google AI của người dùng); không có thì Gemini CLI (cần GEMINI_API_KEY)."""
    agy = shutil.which("agy") or os.path.join(os.environ.get("LOCALAPPDATA", ""), "agy", "bin", "agy.exe")
    if os.path.isfile(agy):
        return "agy", agy
    g = shutil.which("gemini")
    if g:
        return "gemini", g
    sys.exit("Chưa có Antigravity CLI (agy) hay Gemini CLI. Cài Antigravity rồi gõ `agy` một lần để đăng nhập.")


def anh_trang(src: str, trang: str | None, thu_muc: str, dpi: int) -> list[str]:
    """Agy không đọc được PDF nếu không chạy lệnh shell (bị chặn ở chế độ chỉ đọc): đổi từng trang ra PNG cho công cụ view_file."""
    import pymupdf  # đã có cho thamdinh/

    goc = os.path.splitext(os.path.basename(src))[0]
    ten = []
    with pymupdf.open(src) as d:
        a, _, b = (trang or f"1-{d.page_count}").partition("-")
        for i in range(int(a), int(b or a) + 1):
            t = f"{goc}_trang{i}.png"
            d[i - 1].get_pixmap(dpi=dpi).save(os.path.join(thu_muc, t))
            ten.append(t)
    return ten


def chay(cau: str, thu_muc: str, model: str | None, timeout: int) -> dict:
    """Trả {"response", "token", "loi"}. Chạy trong thư mục tạm, chế độ chỉ đọc."""
    loai, exe = cong_cu()
    env = dict(os.environ, NO_COLOR="1")
    if loai == "agy":
        lenh = [exe, "-p", cau, "--output-format", "json", "--mode", "plan", "--disable-slash-commands"]
        if model:
            lenh += ["--model", model]
        r = subprocess.run(lenh, capture_output=True, cwd=thu_muc, env=env, timeout=timeout)
    else:
        # Gemini CLI: lời nhắn qua stdin (UTF-8), -p chỉ để bật chế độ không giao diện, CLI nối nó vào sau stdin.
        lenh = [exe, "-p", "Lam dung yeu cau o tren.", "-o", "json", "--approval-mode", "plan"] + (["-m", model] if model else [])
        r = subprocess.run(lenh, input=cau.encode("utf-8"), capture_output=True, cwd=thu_muc, env=env, timeout=timeout)
    ra = r.stdout.decode("utf-8", "replace")
    try:
        j = json.loads(ra[ra.index("{"):])
    except ValueError:
        sys.exit(f"{loai} thoát mã {r.returncode}: {(r.stderr.decode('utf-8', 'replace') or ra)[:600]}")
    if loai == "agy":
        ok = j.get("status") == "SUCCESS"
        return {"response": j.get("response", ""), "token": {model or "mac_dinh": j.get("usage", {}).get("total_tokens")},
                "loi": None if ok else str(j.get("error") or j.get("status"))}
    tk = {m: v.get("tokens", {}).get("total") for m, v in j.get("stats", {}).get("models", {}).items()}
    return {"response": j.get("response", ""), "token": tk, "loi": (j.get("error") or {}).get("message") if "error" in j else None}


def doc_json(van: str):
    m = re.search(r"```(?:json)?\s*(.*?)```", van, re.S)
    return json.loads(m.group(1) if m else van)


def main() -> None:
    p = argparse.ArgumentParser()
    p.add_argument("--file", help="PDF, ảnh hoặc file chữ cần đọc")
    p.add_argument("--trang", help="chỉ gửi các trang này của PDF, vd 3 hoặc 3-5")
    p.add_argument("--yeu-cau", help="việc cần bóc, ghi rõ trường cần lấy")
    p.add_argument("--json", action="store_true", help="bắt trả JSON hợp lệ (kiểm bằng json.loads)")
    p.add_argument("--out", help="file ghi kết quả (mặc định in ra màn hình)")
    p.add_argument("--model", help="vd gemini-3.8-flash-medium (xem `agy models`); bỏ trống = CLI tự chọn")
    p.add_argument("--timeout", type=int, default=600)
    p.add_argument("--dpi", type=int, default=200, help="độ phân giải ảnh trang PDF gửi đi")
    p.add_argument("--kiem", action="store_true", help="thử đăng nhập, in model và token")
    a = p.parse_args()
    sys.stdout.reconfigure(encoding="utf-8")

    with tempfile.TemporaryDirectory(prefix="tkpccc_gemini_") as tm:
        if a.kiem:
            j = chay("Tra loi dung mot tu: ok", tm, a.model, 120)
        else:
            if not (a.file and a.yeu_cau):
                p.error("cần --file và --yeu-cau")
            if a.file.lower().endswith(".pdf"):
                ten = anh_trang(a.file, a.trang, tm, a.dpi)
            else:
                ten = [os.path.basename(a.file)]
                shutil.copy(a.file, os.path.join(tm, ten[0]))
            # @ cho Gemini CLI; agy cần được bảo rõ dùng view_file, không thì nó chạy lệnh shell và bị chặn, trả rỗng.
            cau = " ".join("@" + t for t in ten) + "\nDùng công cụ view_file xem các ảnh: " + ", ".join(ten) + \
                ". KHÔNG dùng run_command.\n" + a.yeu_cau + "\n"
            if a.json:
                cau += "Chỉ trả về MỘT khối JSON hợp lệ, không giải thích. Không đoán: không đọc được thì ghi null.\n"
            j = chay(cau, tm, a.model, a.timeout)

    if j["loi"]:
        sys.exit("Gemini báo lỗi: " + j["loi"][:600])
    van = j["response"]
    if a.json and not a.kiem:
        van = json.dumps(doc_json(van), ensure_ascii=False, indent=1)
    tk = j["token"]
    if a.out:
        with open(a.out, "w", encoding="utf-8") as f:
            f.write(van)
        print(f"OK {a.out} ({len(van)} ký tự), token Gemini: {tk}")
    else:
        print(van)
        print(f"-- token Gemini: {tk}")


if __name__ == "__main__":
    main()
