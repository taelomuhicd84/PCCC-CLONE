"""Gọi Gemini CLI không giao diện để bóc số liệu từ PDF/ảnh dài, trả về file kết quả nhỏ cho Claude đọc.

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


def gemini() -> str:
    g = shutil.which("gemini")
    if not g:
        sys.exit("Chưa có Gemini CLI: npm i -g @google/gemini-cli, rồi gõ `gemini` trong terminal để đăng nhập Google.")
    return g


def tach_trang(src: str, trang: str, dich: str) -> None:
    import fitz  # pymupdf, đã có cho thamdinh/

    a, _, b = trang.partition("-")
    d, c = int(a), int(b or a)
    with fitz.open(src) as goc, fitz.open() as moi:
        moi.insert_pdf(goc, from_page=d - 1, to_page=c - 1)
        moi.save(dich)


def chay(cau: str, thu_muc: str, model: str | None, timeout: int) -> dict:
    # Lời nhắn qua stdin (UTF-8, chữ Việt); -p chỉ để bật chế độ không giao diện, CLI nối nó vào sau stdin.
    lenh = [gemini(), "-p", "Lam dung yeu cau o tren.", "-o", "json", "--approval-mode", "plan"]
    if model:
        lenh += ["-m", model]
    env = dict(os.environ, NO_COLOR="1")
    r = subprocess.run(lenh, input=cau.encode("utf-8"), capture_output=True, cwd=thu_muc, env=env, timeout=timeout)
    ra = r.stdout.decode("utf-8", "replace")
    try:
        return json.loads(ra[ra.index("{"):])
    except ValueError:
        sys.exit(f"Gemini thoát mã {r.returncode}: {(r.stderr.decode('utf-8', 'replace') or ra)[:600]}")


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
    p.add_argument("--model", help="vd model Flash cho việc bóc; bỏ trống = CLI tự chọn")
    p.add_argument("--timeout", type=int, default=600)
    p.add_argument("--kiem", action="store_true", help="thử đăng nhập, in model và token")
    a = p.parse_args()

    with tempfile.TemporaryDirectory(prefix="tkpccc_gemini_") as tm:
        if a.kiem:
            j = chay("Tra loi dung mot tu: ok", tm, a.model, 120)
        else:
            if not (a.file and a.yeu_cau):
                p.error("cần --file và --yeu-cau")
            ten = os.path.basename(a.file)
            dich = os.path.join(tm, ten)
            if a.trang and ten.lower().endswith(".pdf"):
                tach_trang(a.file, a.trang, dich)
            else:
                shutil.copy(a.file, dich)
            cau = f"@{ten}\n{a.yeu_cau}\n"
            if a.json:
                cau += "Chỉ trả về MỘT khối JSON hợp lệ, không giải thích. Không đoán: không đọc được thì ghi null.\n"
            j = chay(cau, tm, a.model, a.timeout)

    if "error" in j:
        sys.exit("Gemini báo lỗi: " + str(j["error"].get("message", j["error"]))[:600])
    van = j.get("response", "")
    if a.json and not a.kiem:
        van = json.dumps(doc_json(van), ensure_ascii=False, indent=1)
    tk = {m: v.get("tokens", {}).get("total") for m, v in j.get("stats", {}).get("models", {}).items()}
    if a.out:
        with open(a.out, "w", encoding="utf-8") as f:
            f.write(van)
        print(f"OK {a.out} ({len(van)} ký tự), token Gemini: {tk}")
    else:
        sys.stdout.reconfigure(encoding="utf-8")
        print(van)
        print(f"-- token Gemini: {tk}")


if __name__ == "__main__":
    main()
