# Lấy hình ký hiệu thiết bị báo cháy từ nét vector một trang PDF (bảng ký hiệu của hồ sơ thật) ra cad/data/ky_hieu/hinh_bc.json.
# Plugin (Lenh.TaoBlockMacDinh) vẽ block mặc định của mã có trong file này theo đúng hình đó thay cho vòng tròn + chữ.
# Chạy: python -I hinh_tu_pdf.py <file.pdf> <số trang, đếm từ 1> <ra.json>
# Hộp từng ký hiệu (toạ độ trang, pt) đo trên trang 11 của hồ sơ mẫu (cad/knowledge/mau-so-do-that.md); PDF khác thì đo lại hộp.
import json
import sys

import fitz

# mã -> (hộp x0, y0, x1, y1), điểm gốc block: "giua" (tâm hộp nét), "tron" (tâm vòng tròn lớn nhất), "chan_trai" (đầu trái chân dưới)
HOP = {
    "BC_NHIET": ((55, 45, 75, 63), "tron"),
    "BC_KHOI_TRAN": ((55, 67, 75, 86), "tron"),
    "BC_KHOI": ((55, 90, 75, 108), "tron"),
    "BC_CHI_THI": ((55, 115, 75, 131), "giua"),
    "BC_COI_DEN": ((53, 134, 78, 153), "giua"),
    "SPR_VAN_OSY": ((54, 158, 74, 178), "than_van"),
    "SPR_CTDC": ((55, 183, 73, 197), "giua"),
    "SPR_VAN_BD": ((53, 199, 83, 226), "giua"),
    "BC_NUTAN": ((286, 45, 306, 63), "giua"),
    "BC_DTCD": ((283, 69, 308, 84), "chan_trai"),
    "BC_CHI_THI_HL": ((286, 88, 313, 108), "giua_khung"),
    "BC_DIEN_THOAI": ((492, 612, 528, 666), "giua"),
}
TL = 400 / 12.12  # mm trên 1 pt: vòng tròn đầu báo H của bảng ký hiệu (12,12 pt) thành đường kính 400 mm như block mặc định cũ


def bez(p0, p1, p2, p3, n=6):
    out = []
    for i in range(1, n + 1):
        t = i / n
        a, b, c, d = (1 - t) ** 3, 3 * t * (1 - t) ** 2, 3 * t * t * (1 - t), t ** 3
        out.append((a * p0.x + b * p1.x + c * p2.x + d * p3.x, a * p0.y + b * p1.y + c * p2.y + d * p3.y))
    return out


def chuoi(items):
    """Các đoạn nối tiếp nhau thành một đường; trả list (điểm, kín)."""
    ds, cur = [], []
    for it in items:
        k = it[0]
        if k == "re":
            r = it[1]
            ds.append(([(r.x0, r.y0), (r.x1, r.y0), (r.x1, r.y1), (r.x0, r.y1)], True))
            continue
        if k == "qu":
            q = it[1]
            ds.append(([(q.ul.x, q.ul.y), (q.ur.x, q.ur.y), (q.lr.x, q.lr.y), (q.ll.x, q.ll.y)], True))
            continue
        a = it[1]
        if not cur or abs(cur[-1][0] - a.x) > 0.01 or abs(cur[-1][1] - a.y) > 0.01:
            if len(cur) > 1:
                ds.append((cur, False))
            cur = [(a.x, a.y)]
        if k == "l":
            cur.append((it[2].x, it[2].y))
        elif k == "c":
            cur += bez(it[1], it[2], it[3], it[4])
    if len(cur) > 1:
        ds.append((cur, False))
    out = []
    for p, kin in ds:
        if not kin and len(p) > 2 and abs(p[0][0] - p[-1][0]) < 0.01 and abs(p[0][1] - p[-1][1]) < 0.01:
            p, kin = p[:-1], True
        out.append((p, kin))
    return out


def main():
    doc = fitz.open(sys.argv[1])
    trang = doc[int(sys.argv[2]) - 1]
    ve = trang.get_drawings()
    chu = trang.get_text("words")
    kq = {"_ghi_chu": "Sinh bằng cad/.claude/skills/tkpccc-cad-console/scripts/hinh_tu_pdf.py từ bảng ký hiệu trang 11 hồ sơ mẫu. Đơn vị mm, gốc = điểm chèn block. "
                      "pl: polyline (kin = kín), c: vòng tròn [x, y, r], to: miền tô đặc (Solid), t: chữ {t, p, h}. Khoá SPR_VAN_OSY dùng cho mọi SPR_VAN_OSY_DN*.",
          "hinh": {}}
    for ma, (hop, kieu) in HOP.items():
        R = fitz.Rect(*hop)
        cac = [x for x in ve if R.contains(x["rect"])]
        nen = [x for x in cac if not (x.get("fill") and min(x["fill"]) > 0.95)]  # bỏ nền trắng (mặt nạ che nét phía sau)
        U = fitz.Rect(nen[0]["rect"])
        for x in nen:
            U |= x["rect"]
        tron = [x for x in nen if [i[0] for i in x["items"]] == ["c"] * 4 and abs(x["rect"].width - x["rect"].height) < 0.3]
        khung = [x for x in nen if [i[0] for i in x["items"]] in (["re"], ["qu"])]
        if kieu == "tron" and tron:
            r = max(tron, key=lambda x: x["rect"].width)["rect"]
            gx, gy = (r.x0 + r.x1) / 2, (r.y0 + r.y1) / 2
        elif kieu == "chan_trai":
            gx, gy = U.x0, U.y1
        elif kieu == "giua_khung" and khung:
            r = max(khung, key=lambda x: x["rect"].width)["rect"]
            gx, gy = (r.x0 + r.x1) / 2, (r.y0 + r.y1) / 2
        elif kieu == "than_van":  # tâm hai tam giác (nét to nhất phía dưới), không tính tay quay phía trên
            r = max((x for x in nen if x["rect"].width > 10), key=lambda x: x["rect"].y1)["rect"]
            gx, gy = (r.x0 + r.x1) / 2, (r.y0 + r.y1) / 2
        else:
            gx, gy = (U.x0 + U.x1) / 2, (U.y0 + U.y1) / 2
        f = lambda p: [round((p[0] - gx) * TL, 1), round((gy - p[1]) * TL, 1)]
        hinh = []
        for x in nen:
            loai = [i[0] for i in x["items"]]
            to = x.get("fill") and min(x["fill"]) < 0.95
            if x in tron and not to:
                r = x["rect"]
                hinh.append({"c": f(((r.x0 + r.x1) / 2, (r.y0 + r.y1) / 2)) + [round(r.width / 2 * TL, 1)]})
                continue
            for p, kin in chuoi(x["items"]):
                if to:
                    hinh.append({"to": [f(q) for q in p]})
                if not to or ("s" in x["type"] and (x.get("width") or 0) > 0.1):  # miền tô: chỉ vẽ viền khi PDF có nét viền thật
                    hinh.append({"pl": [f(q) for q in p], "kin": kin})
        for w in chu:
            if R.contains(fitz.Rect(w[:4])):
                cao = (w[3] - w[1]) * 0.62  # chiều cao chữ hoa ~ 0,62 hộp chữ
                hinh.append({"t": w[4], "p": f(((w[0] + w[2]) / 2, (w[1] + w[3]) / 2)), "h": round(cao * TL, 1)})
        kq["hinh"][ma] = hinh
    with open(sys.argv[3], "w", encoding="utf-8") as fo:
        json.dump(kq, fo, ensure_ascii=False, separators=(",", ":"))
    for ma, h in kq["hinh"].items():
        print(ma, len(h))


if __name__ == "__main__":
    main()
