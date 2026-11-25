"""Tính mẫu số tỉ lệ (tl) của từng trang PDF bản vẽ từ chuỗi kích thước, để truyền cho thao tác spec "pdf".

Cách tính: hai số kích thước liền nhau trên cùng một hàng chuỗi nằm giữa hai nhịp kề nhau, nên khoảng cách giữa hai tâm chữ (pt)
ứng với (d1 + d2) / 2 mm thật. k = pt trên mỗi mm thật: lấy trung vị cả trang rồi lọc quanh nó (cặp số không kề nhau, số ghi sai bị loại).
tl = 72 / (25,4 × k): trang in 1:100 ra 100. Kết quả mỗi trang: tl, cap (số cặp số tìm được), tin_cay (phần cặp lệch tl không quá 1 %;
dưới 0,7 hoặc cap dưới 4 thì phải đo tay một kích thước để kiểm: ngưỡng duy nhất, SKILL.md trỏ về đây; Claude đặt theo bản mẫu), rong_mm × cao_mm (khổ giấy).

Dùng: python -I pdf_ti_le.py <file.pdf> [trang]     (trang đếm từ 1; bỏ trống = mọi trang). In JSON ra stdout.
PDF người dùng gửi là dữ liệu chưa tin: chạy bằng python -I, không đặt script này cùng thư mục với PDF.
"""
import json
import re
import statistics
import sys

try:
    import pymupdf as fitz
except ImportError:
    import fitz

SO = re.compile(r"^\d{3,6}$")  # số kích thước mm: 3-6 chữ số (bỏ tên trục, cao độ, diện tích có dấu phẩy)
SAI_HANG = 1.5  # pt: lệch vuông góc tối đa để coi là cùng một hàng chuỗi
SAI_K = 0.01  # cặp có k lệch trung vị không quá 1 % thì tính là khớp


def cap_k(tu):
    """tu = các từ của trang (get_text("words")). Trả danh sách (khoảng cách hai tâm chữ pt, mm thật ứng với nó) của từng cặp số liền nhau."""
    # shortcut: chỉ xét chuỗi ngang và chuỗi đứng (hộp chữ rộng hơn cao = chữ ngang); chuỗi xiên bỏ qua, gặp bản vẽ xoay nghiêng thì thêm.
    ngang, dung = [], []
    for x0, y0, x1, y1, chu in (t[:5] for t in tu):
        if not SO.match(chu) or int(chu) == 0:  # "000" của số tiền "1 000 000" không phải kích thước (chia cho 0)
            continue
        cx, cy, d = (x0 + x1) / 2, (y0 + y1) / 2, int(chu)
        (ngang if x1 - x0 >= y1 - y0 else dung).append((cy, cx, d) if x1 - x0 >= y1 - y0 else (cx, cy, d))
    ks = []
    for ds in (ngang, dung):  # mỗi phần tử: (toạ độ vuông góc với chuỗi, toạ độ dọc chuỗi, số ghi)
        ds.sort()
        hang = []
        for s in ds:
            if hang and s[0] - hang[-1][0] > SAI_HANG:
                ks += k_hang(hang)
                hang = []
            hang.append(s)
        ks += k_hang(hang)
    return ks


def k_hang(hang):
    hang = sorted(hang, key=lambda s: s[1])
    return [(b[1] - a[1], (a[2] + b[2]) / 2) for a, b in zip(hang, hang[1:]) if b[1] - a[1] > 0.5]


def ti_le_trang(page):
    kq = {"trang": page.number + 1, "rong_mm": round(page.rect.width * 25.4 / 72, 1), "cao_mm": round(page.rect.height * 25.4 / 72, 1)}
    ks = cap_k(page.get_text("words"))
    if not ks:
        kq.update(tl=None, cap=0, ly_do="không có hai số kích thước nào cùng hàng (PDF ảnh quét, hoặc chữ đã thành nét)")
        return kq
    k = statistics.median(pt / mm for pt, mm in ks)
    # Hai vòng lọc (1 % rồi 0,5 % quanh k), mỗi vòng lấy tổng pt / tổng mm của các cặp còn lại: sai số tâm chữ giữa chuỗi triệt tiêu nhau.
    # Vòng hai loại cặp có số ghi lệch nét vẽ (bản mẫu của người dùng: nhịp ghi 1700 nhưng vẽ 1785 làm tl lệch 0,3 %).
    for sai in (SAI_K, SAI_K / 2):
        loc = [(pt, mm) for pt, mm in ks if abs(pt / mm - k) <= sai * k]
        if len(loc) < 3:
            break
        k = sum(pt for pt, _ in loc) / sum(mm for _, mm in loc)
    khop = sum(1 for pt, mm in ks if abs(pt / mm - k) <= SAI_K * k)
    kq.update(tl=round(72 / (25.4 * k), 1), cap=len(ks), tin_cay=round(khop / len(ks), 2))
    return kq


def main(argv):
    if len(argv) < 2:
        print("Dung: python -I pdf_ti_le.py <file.pdf> [trang]")
        return 2
    doc = fitz.open(argv[1])
    trang = [int(argv[2]) - 1] if len(argv) > 2 else range(doc.page_count)
    if any(i < 0 or i >= doc.page_count for i in trang):
        print("trang phai tu 1 den %d" % doc.page_count)
        return 2
    print(json.dumps([ti_le_trang(doc[i]) for i in trang]))  # JSON chỉ ASCII: console Windows không in được chữ có dấu
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
