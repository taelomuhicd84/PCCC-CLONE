"""Tao cad/test/mau_kt.pdf: mat bang gia co luoi truc + chuoi kich thuoc, ti le 1:200 tren giay A3 ngang.
Dung cho phep kiem cua pdf_ti_le.py va thao tac spec "pdf" trong cad/test/chay_thu.ps1.
Co y "ban" nhu ban ve that de hai vong loc cua pdf_ti_le.py phai lam viec: mot nhip ghi 6650 nhung ve 7000 (nhu ban mau ghi 1700 ve 1785),
mot so khong ke nhau cung hang voi so tong, mot chu "1 000 000" (chuoi 000 khong phai kich thuoc).
Chay lai khi can doi mau: python cad/test/tao_pdf_mau.py"""
import os

try:
    import pymupdf as fitz
except ImportError:
    import fitz

TL = 200.0
K = 72 / 25.4 / TL  # pt tren moi mm that
NHIP_X = [6000, 7000, 7000, 7000, 7000, 7000]  # tong 41000 nhu ban mau
NHIP_Y = [5000, 5300, 5000, 5000]  # tong 20300
GHI_SAI = {2: "6650"}  # nhip thu 3 ve 7000 nhung ghi 6650
KHUNG = (50, 50, 1140, 790)  # khung ngoai cung, rong 1090 pt: chay_thu.ps1 do lai be rong nay sau khi nhap


def chu_giua(page, cx, cy, t, co=7, dung=False):
    w = fitz.get_text_length(t, "helv", co)
    if dung:  # chu xoay 90 do, doc tu duoi len
        page.insert_text((cx + co * 0.35, cy + w / 2), t, fontsize=co, fontname="helv", rotate=90)
    else:
        page.insert_text((cx - w / 2, cy + co * 0.35), t, fontsize=co, fontname="helv")


def tao(duong_dan):
    doc = fitz.open()
    page = doc.new_page(width=1190.55, height=841.89)
    page.draw_rect(fitz.Rect(*KHUNG), width=1)
    x0, y1 = 250.0, 650.0  # goc duoi trai cua luoi (toa do PyMuPDF: y huong xuong)
    xs, ys = [x0], [y1]
    for n in NHIP_X:
        xs.append(xs[-1] + n * K)
    for n in NHIP_Y:
        ys.append(ys[-1] - n * K)
    for i, x in enumerate(xs):
        page.draw_line((x, ys[-1] - 30), (x, y1 + 30), width=0.3)
        chu_giua(page, x, y1 + 45, str(i + 1), 9)
    for j, y in enumerate(ys):
        page.draw_line((x0 - 30, y), (xs[-1] + 30, y), width=0.3)
        chu_giua(page, x0 - 45, y, "ABCDE"[j], 9)
    yk = ys[-1] - 50  # chuoi kich thuoc nhip phia tren luoi, hang tong o tren nua
    page.draw_line((xs[0], yk), (xs[-1], yk), width=0.3)
    for i, n in enumerate(NHIP_X):
        chu_giua(page, (xs[i] + xs[i + 1]) / 2, yk - 5, GHI_SAI.get(i, str(n)))
    page.draw_line((xs[0], yk - 20), (xs[-1], yk - 20), width=0.3)
    chu_giua(page, (xs[0] + xs[-1]) / 2, yk - 25, str(sum(NHIP_X)))
    chu_giua(page, xs[0] + 30, yk - 25, "2500")  # so khong ke voi so tong nhung cung hang: cap rac
    xk = x0 - 80  # chuoi kich thuoc dung ben trai luoi
    page.draw_line((xk, ys[0]), (xk, ys[-1]), width=0.3)
    for j, n in enumerate(NHIP_Y):
        chu_giua(page, xk - 5, (ys[j] + ys[j + 1]) / 2, str(n), dung=True)
    chu_giua(page, 600, 760, "MAT BANG TANG 1 - TL 1:200", 10)
    chu_giua(page, 600, 775, "DON GIA 1 000 000 DONG", 7)
    doc.save(duong_dan, garbage=4, deflate=True)


if __name__ == "__main__":
    f = os.path.join(os.path.dirname(os.path.abspath(__file__)), "mau_kt.pdf")
    tao(f)
    print("Da tao", f)
