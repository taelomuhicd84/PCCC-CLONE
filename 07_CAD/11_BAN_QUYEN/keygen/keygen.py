"""Tạo key bản quyền TKPCCC bằng Python (tương đương 07_CAD/bin/TKPCCC.KeyGen.exe, cùng hợp đồng key với 07_CAD/11_BAN_QUYEN/cad/BanQuyen.cs).

Chỉ dùng thư viện chuẩn: chữ ký RSA PKCS#1 v1.5 SHA-256 tính thẳng (tất định, trùng từng byte với KeyGen.exe và Apps Script).
Khoá riêng đọc từ file NGOÀI repo (mặc định %USERPROFILE%\\TKPCCC_KHOA\\khoa_rieng.pem, KeyGen.exe tạo); không bao giờ commit khoá riêng.

  python keygen.py ky <email> <mã máy> <yyyy-MM-dd> [--khoa <khoa_rieng.pem|.xml>]   in key
  python keygen.py may                                                               in mã máy của máy này (như plugin)
"""
import base64
import hashlib
import os
import re
import subprocess
import sys

PHIEN_BAN = "TKPCCC1"
# DigestInfo DER của SHA-256 (RFC 8017 mục 9.2, ghi chú 1).
DAU_SHA256 = bytes.fromhex("3031300d060960864801650304020105000420")


def noi_dung(email, may, han):
    return f"{PHIEN_BAN}|{email.strip().lower()}|{may.strip().upper()}|{han}"


def b64u(b):
    return base64.urlsafe_b64encode(b).decode("ascii").rstrip("=")


def _der(b, i):
    """Đọc một phần tử DER tại i: trả (tag, nội dung, vị trí sau phần tử)."""
    tag, n = b[i], b[i + 1]
    i += 2
    if n & 0x80:
        k = n & 0x7F
        n = int.from_bytes(b[i:i + k], "big")
        i += k
    return tag, b[i:i + n], i + n


def _cac_so(seq):
    so, i = [], 0
    while i < len(seq):
        tag, v, i = _der(seq, i)
        if tag == 0x02:
            so.append(int.from_bytes(v, "big"))
    return so


def doc_khoa(duong_dan):
    """Trả (n, d) từ khoá riêng PEM PKCS#8 hoặc XML .NET (cả hai do KeyGen.exe xuất)."""
    s = open(duong_dan, encoding="utf-8").read()
    if "<RSAKeyValue>" in s:
        lay = lambda t: int.from_bytes(base64.b64decode(re.search(f"<{t}>([^<]+)</{t}>", s).group(1)), "big")
        return lay("Modulus"), lay("D")
    than = base64.b64decode("".join(l for l in s.splitlines() if not l.startswith("-----")))
    _, ngoai, _ = _der(than, 0)                      # PrivateKeyInfo
    i = 0
    _, _, i = _der(ngoai, i)                         # version
    _, _, i = _der(ngoai, i)                         # AlgorithmIdentifier
    _, pkcs1, _ = _der(ngoai, i)                     # OCTET STRING chứa RSAPrivateKey
    _, rsa, _ = _der(pkcs1, 0)
    so = _cac_so(rsa)                                # version, n, e, d, p, q, dp, dq, qinv
    return so[1], so[3]


def ky(khoa, email, may, han):
    n, d = doc_khoa(khoa)
    nd = noi_dung(email, may, han).encode("utf-8")
    k = (n.bit_length() + 7) // 8
    t = DAU_SHA256 + hashlib.sha256(nd).digest()
    em = b"\x00\x01" + b"\xff" * (k - len(t) - 3) + b"\x00" + t
    chu_ky = pow(int.from_bytes(em, "big"), d, n).to_bytes(k, "big")
    return b64u(nd) + "." + b64u(chu_ky)


def chuan_seri(s):
    """Như BanQuyen.ChuanSeri: chỉ giữ A-Z 0-9 chữ hoa; toàn số 0 hoặc rỗng = không có seri."""
    r = "".join(c for c in (s or "").upper() if c.isascii() and c.isalnum())
    return "" if r.strip("0") == "" else r


def nhom(x):
    return "-".join(x[i:i + 4] for i in range(0, 16, 4))


def seri_o_dia():
    """Seri ổ chứa Windows (Win32_DiskDrive qua phân vùng của ổ %SystemDrive%), cùng nguồn với plugin."""
    o = os.environ.get("SystemDrive", "C:").rstrip("\\")
    lenh = (f"Get-CimInstance Win32_LogicalDisk -Filter \"DeviceID='{o}'\" | Get-CimAssociatedInstance -ResultClassName Win32_DiskPartition"
            " | Get-CimAssociatedInstance -ResultClassName Win32_DiskDrive | ForEach-Object { $_.SerialNumber }")
    ra = subprocess.run(["powershell", "-NoProfile", "-Command", lenh], capture_output=True, text=True, timeout=60).stdout
    for dong in ra.splitlines():
        s = chuan_seri(dong)
        if s:
            return s
    return ""


def ma_may():
    s = seri_o_dia()
    if s:
        return nhom("D" + hashlib.sha256(f"DISK|{s}|TKPCCC".encode("utf-8")).hexdigest().upper()[:15])
    import winreg  # không đọc được seri (máy ảo, RAID): mã cũ theo MachineGuid, như plugin
    with winreg.OpenKey(winreg.HKEY_LOCAL_MACHINE, r"SOFTWARE\Microsoft\Cryptography", 0, winreg.KEY_READ | winreg.KEY_WOW64_64KEY) as k:
        guid = winreg.QueryValueEx(k, "MachineGuid")[0]
    return nhom(hashlib.sha256(f"{guid}|TKPCCC".encode("utf-8")).hexdigest().upper()[:16])


def main(a):
    if len(a) == 1 and a[0] == "may":
        print(ma_may())
        return 0
    khoa = os.path.join(os.path.expanduser("~"), "TKPCCC_KHOA", "khoa_rieng.pem")
    if "--khoa" in a:
        i = a.index("--khoa")
        khoa = a[i + 1]
        a = a[:i] + a[i + 2:]
    if len(a) == 4 and a[0] == "ky":
        if not re.fullmatch(r"\d{4}-\d{2}-\d{2}", a[3]):
            print("Hạn phải dạng yyyy-MM-dd.", file=sys.stderr)
            return 2
        print(ky(khoa, a[1], a[2], a[3]))
        return 0
    print(__doc__, file=sys.stderr)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
