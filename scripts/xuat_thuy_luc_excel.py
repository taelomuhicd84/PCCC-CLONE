"""Điền số liệu thủy lực (lấy từ bản vẽ) vào file Excel mẫu, xuất ra file Excel kết quả.
Sửa trực tiếp XML của ô nhập nên giữ nguyên công thức, định dạng, ảnh, ghi chú; Excel tự tính lại khi mở.
Dùng: python scripts/xuat_thuy_luc_excel.py <so_lieu.json> <ra.xlsx> [--mau bang_tinh/thuy_luc/MAU_THUY_LUC_7336.xlsx] [--khong-hoan-thien]
so_lieu.json: khóa theo bang_tinh/thuy_luc/o_nhap.json (vd "K_gpm", "nhanh_trai": [{"dn": 32, "l": 3}, ...]),
và/hoặc "o": {"B7": "chữ", "L199": 0.15} để ghi thẳng vào ô. Giá trị null = xóa ô (đoạn ống bỏ trống thì chuỗi dừng, ra #N/A là chủ ý).
Chép từ repo SKILL-PCCC-NEW (scripts/xuat_thuy_luc_excel.py); TKPCCC thêm --khong-hoan-thien (bỏ bước Excel COM) và
bước hoàn thiện lỗi chỉ cảnh báo (số liệu đã ghi đúng, chỉ chưa ẩn dòng trống).
"""
import json, os, re, sys, zipfile

GOC = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MAU = os.path.join(GOC, 'bang_tinh', 'thuy_luc', 'MAU_THUY_LUC_7336.xlsx')
O_NHAP = os.path.join(GOC, 'bang_tinh', 'thuy_luc', 'o_nhap.json')


def _col(ref):
    c = re.match(r'[A-Z]+', ref).group(0)
    n = 0
    for ch in c:
        n = n * 26 + ord(ch) - 64
    return n


def _esc(t):
    return t.replace('&', '&amp;').replace('<', '&lt;').replace('>', '&gt;')


_FONT = '<sz val="10"/><rFont val="Arial"/><family val="2"/>'


def _runs(parts):
    out = []
    for i, (t, sub) in enumerate(parts):
        rpr = f'<rPr><vertAlign val="subscript"/>{_FONT}</rPr>' if sub else ('' if i == 0 else f'<rPr>{_FONT}</rPr>')
        out.append(f'<r>{rpr}<t xml:space="preserve">{_esc(t)}</t></r>')
    return ''.join(out)


def nhan_nut(nl, nr):
    """Nhãn q, P, Q, ΔP theo SỐ NÚT thật (mẫu ghi cứng q1…q5, P4…): trái nút 1…nl; phải đánh ngược nl+nr…nl+1 như công thức cột B.
    Trả {ô: danh sách đoạn chữ}. Đoạn cuối mỗi nhánh nối về nút A ('-A')."""
    o = {}

    def hang(c, k, r, q, p, ten_q, ten_p, ra_a, dau):
        o[f'C{r}'] = [('q', False), (str(q), True), (' = ', False)]
        o[f'K{r}'] = [('P', False), (str(p), True)] + ([(' =P', False), ('1', True), ('= ', False)] if dau == 'phai1' else [(' = ', False)])
        rq = r + 2
        o[f'C{rq}'] = [('Q', False), (ten_q, True), (' = ', False)]
        o[f'K{r + 2 if dau in ("trai1", "phai1") else r + 1}'] = [('P', False), (ten_p, True), (' = ', False)]
    for i in range(1, 6):  # trái: đầu i ở dòng 61 + 3(i-1)
        r = 61 + 3 * (i - 1)
        t = f'{i}-A' if i == nl else f'{i}-{i + 1}'
        hang('C', 'K', r, i, i, t, t, i == nl, 'trai1' if i == 1 else 'trai')
    for e in range(1, 5):  # phải: đầu e ở dòng 76 + 3(e-1), nút a = nl + nr - e + 1
        r = 76 + 3 * (e - 1)
        a = nl + max(nr, e) - e + 1
        t = f'{a}-A' if e == nr else f'{a}-{a - 1}'
        hang('C', 'K', r, a, a, t, t, e == nr, 'phai1' if e == 1 else 'phai')
    # hàng I đối xứng: chữ 'QI = 2 * Q5-A' (B98) theo nút cuối nhánh trái, mẫu ghi cứng 5
    o['B98'] = [('Lưu lượng tại hàng I:         Q', False), ('I', True), ('= 2 * Q', False), (f'{nl}-A', True), (' =', False)]
    return o


def set_cell(xml, ref, val):
    """Ghi một ô: số, chữ, '=công thức' hoặc None (xóa nội dung, giữ kiểu ô). Tự chèn ô/dòng nếu chưa có."""
    r = int(re.search(r'\d+', ref).group(0))
    m = re.search(r'<c r="%s"((?: [^>/]*)?)(?:/>|>.*?</c>)' % ref, xml, flags=re.S)
    st = re.search(r' s="\d+"', m.group(1)).group(0) if m and re.search(r' s="\d+"', m.group(1)) else ''
    if val is None:
        new = f'<c r="{ref}"{st}/>'
    elif isinstance(val, list):  # chữ có chỉ số dưới: [(chữ, True nếu chỉ số dưới), ...] như nhãn q1, P1-2 của mẫu
        new = f'<c r="{ref}"{st} t="inlineStr"><is>{_runs(val)}</is></c>'
    elif isinstance(val, str) and val.startswith('='):
        new = f'<c r="{ref}"{st}><f>{_esc(val[1:])}</f></c>'
    elif isinstance(val, str):
        new = f'<c r="{ref}"{st} t="inlineStr"><is><t>{_esc(val)}</t></is></c>'
    else:
        new = f'<c r="{ref}"{st}><v>{repr(float(val)) if isinstance(val, float) else val}</v></c>'
    if m:
        return xml[:m.start()] + new + xml[m.end():]
    rm = re.search(r'<row r="%d"[^>]*?(?:/>|>.*?</row>)' % r, xml, flags=re.S)
    if not rm:  # chèn dòng mới đúng thứ tự
        sau = [x for x in re.finditer(r'<row r="(\d+)"', xml) if int(x.group(1)) > r]
        chen = f'<row r="{r}">{new}</row>'
        pos = sau[0].start() if sau else xml.index('</sheetData>')
        return xml[:pos] + chen + xml[pos:]
    row = rm.group(0)
    if row.endswith('/>'):
        row2 = row[:-2] + '>' + new + '</row>'
    else:
        cells = list(re.finditer(r'<c r="([A-Z]+)\d+"', row))
        sau = [c for c in cells if _col(c.group(1)) > _col(ref)]
        pos = sau[0].start() if sau else row.rindex('</row>')
        row2 = row[:pos] + new + row[pos:]
    return xml[:rm.start()] + row2 + xml[rm.end():]


def o_tu_so_lieu(sl, ban_do):
    """Đổi số liệu theo tên (o_nhap.json) thành {ô: giá trị}."""
    # TCVN 7336:2021 B.2.10.12 "QII=√Bp1Pb": lưu lượng hàng II theo áp nút b (N102 = D106), như F114, F122… của mẫu.
    # Mẫu gốc ghi nhầm áp nút a (L100): lệch khi đoạn a-b tổn thất lớn (TKPCCC sửa 2026-11-01).
    # Họng vách tường (người dùng chốt 2026-11-01): Bảng H.5/H.6 chỉ là yêu cầu tối thiểu (H21 = QHR, QBmin ở H23, so ở I202);
    # lưu lượng tính toán chạy trong ống vòng/cụt về bơm theo lăng tra Bảng H.7 (I161 = I159 × I160). Mẫu gốc D183 cộng H21.
    o = {'F105': '=IF(D97="Đối xứng với nhánh phải",ROUND(SQRT(G99*N102),2),ROUND(SQRT(G95*N102),2))',
         'D183': '=IF($D$181="Mạch cụt",F157+IF($H$181<>"SP và VT riêng",I161,0),(F157+IF($H$181<>"SP và VT riêng",I161,0))/2)'}
    for k, v in sl.items():
        if k == 'o':
            o.update(v)
            continue
        if k in ('hang_cuoi_bp', 'hang_rieng_bp'):  # hàng có cấu trúc riêng: chèn dòng tính sau khi ghi (chen_hang_cuoi.ps1); hàng II thì F105 dùng Bp J104
            if any(h['chi_so'] == 0 for h in (v if isinstance(v, list) else [v])):
                o['F105'] = '=ROUND(SQRT(J104*N102),2)'
            continue
        if k not in ban_do or k.startswith('_'):
            raise SystemExit(f'Không có khóa "{k}" trong o_nhap.json')
        bd = ban_do[k]
        if isinstance(bd, dict) and 'o' in bd:  # ô đơn (có thể nhiều ô cùng giá trị)
            for ref in (bd['o'] if isinstance(bd['o'], list) else [bd['o']]):
                o[ref] = v
        elif isinstance(bd, list):  # dãy đoạn ống: phần thiếu thì xóa để chuỗi dừng
            for i, hang in enumerate(bd):
                d = v[i] if i < len(v) else None
                if 'b' in hang and d and d.get('nut'):  # nhãn nút theo bản vẽ; không có thì giữ công thức tự đánh của mẫu
                    o[hang['b']] = str(d['nut'])
                o[hang['dn']] = d['dn'] if d else None
                o[hang['l']] = d['l'] if d else None
        elif isinstance(bd, dict):  # nhóm ô (vd họng nước)
            if k == 'hong':  # cuộn vòi: thiếu thì mặc định DN50, 20 m; Kp để công thức I165 tra theo DN (TCVN 4513-1988 mục 6.19: 50 mm 0,012; 66 mm 0,00385), chỉ ghi Kp khi số liệu có sẵn
                v = {**v, 'DN_voi': v.get('DN_voi') or 50, 'L_voi': v.get('L_voi') or 20}
                if not v.get('Kp'):
                    v.pop('Kp', None)
            for kk, vv in v.items():
                if kk not in bd:
                    raise SystemExit(f'Không có khóa "{k}.{kk}" trong o_nhap.json')
                o[bd[kk]['o']] = bd[kk]['cong_thuc'].format(vv) if 'cong_thuc' in bd[kk] else vv
    return o


def so_nhanh(sl):
    """Số đầu mỗi nhánh theo số liệu: trái = số đoạn nhanh_trai; phải = số đoạn nhanh_phai (trống: đối xứng thì bằng trái, 1 nhánh thì 0)."""
    nl = len([d for d in sl.get('nhanh_trai') or [] if d])
    nr = len([d for d in sl.get('nhanh_phai') or [] if d])
    if not nr and (sl.get('doi_xung') or '').startswith('Đối xứng') and sl.get('so_nhanh') != '1 nhánh':
        nr = nl
    if sl.get('so_nhanh') == '1 nhánh':
        nr = 0
    return nl, nr


def doi_so_nut(xml, drawing, nl, nr):
    """Đổi toàn bộ số nút cứng của mẫu: nhãn q/P/Q ở cột C, K (nhánh trái và phải), chữ M 'Q3-A2', và hình equation 'Q5-A', 'Q6-A'."""
    for ref, parts in nhan_nut(nl, nr).items():
        xml = set_cell(xml, ref, parts)
    xml = xml.replace('Q3-A2/Paitrái', 'Q"&amp;$Q$59&amp;"-A2/Paitrái').replace('Q5-A2/Paiphai', 'Q"&amp;($Q$59+1)&amp;"-A2/Paiphai')
    if drawing:  # hình equation: nút cuối nhánh trái = nl, nút cuối nhánh phải = nl + 1 (mẫu ghi cứng 5 và 6)
        so = lambda m: str(nl if m.group(1) == '5' else nl + 1)
        drawing = re.sub(r'(?<=<m:t>)([56])(?=−</m:t>)', so, drawing)  # equation: số nút và "−" nằm trong m:t riêng, chữ A ở m:t kế tiếp
        drawing = re.sub(r'(?<![\d.])([56])(?=−(?:A|𝐴))', so, drawing)  # chữ dự phòng a:t
    return xml, drawing


def xuat(o, ra, mau=MAU, sheet='SP chung voi HR', nut=None):
    zin = zipfile.ZipFile(mau)
    wb = zin.read('xl/workbook.xml').decode('utf-8')
    rid = re.search(r'<sheet name="%s" sheetId="\d+" r:id="(rId\d+)"' % re.escape(sheet), wb).group(1)
    rels = zin.read('xl/_rels/workbook.xml.rels').decode('utf-8')
    target = re.search(r'Id="%s"[^>]*Target="([^"]+)"' % rid, rels) or re.search(r'Target="([^"]+)"[^>]*Id="%s"' % rid, rels)
    sp = 'xl/' + target.group(1).lstrip('/').replace('xl/', '')
    xml = zin.read(sp).decode('utf-8')
    for ref, val in o.items():
        xml = set_cell(xml, ref, val)
    dpath = dr = None
    if nut:  # (nl, nr): đổi số nút cứng của mẫu theo số đầu phun thật
        srel = 'xl/worksheets/_rels/' + os.path.basename(sp) + '.rels'
        mr = re.search(r'Target="\.\./drawings/(drawing\d+\.xml)"', zin.read(srel).decode('utf-8')) if srel in zin.namelist() else None
        dpath = 'xl/drawings/' + mr.group(1) if mr else None
        xml, dr = doi_so_nut(xml, zin.read(dpath).decode('utf-8') if dpath else None, *nut)
    wb = re.sub(r'<calcPr([^>]*?)/>', lambda m: '<calcPr' + re.sub(r' fullCalcOnLoad="\d"', '', m.group(1)) + ' fullCalcOnLoad="1"/>', wb)
    rels = re.sub(r'<Relationship [^>]*calcChain[^>]*/>', '', rels)
    ct = re.sub(r'<Override[^>]*calcChain[^>]*/>', '', zin.read('[Content_Types].xml').decode('utf-8'))
    os.makedirs(os.path.dirname(os.path.abspath(ra)), exist_ok=True)
    tam = ra + '.tmp'
    with zipfile.ZipFile(tam, 'w', zipfile.ZIP_DEFLATED) as zo:
        for it in zin.infolist():
            if it.filename == 'xl/calcChain.xml':
                continue
            data = {sp: xml, 'xl/workbook.xml': wb, 'xl/_rels/workbook.xml.rels': rels, '[Content_Types].xml': ct, **({dpath: dr} if dpath and dr else {})}.get(it.filename)
            zo.writestr(it, data.encode('utf-8') if data is not None else zin.read(it.filename))
    zin.close()
    os.replace(tam, ra)  # lỗi PermissionError nếu file ra đang mở trong Excel: đổi tên file ra


if __name__ == '__main__':
    a = sys.argv[1:]
    mau = MAU
    if '--mau' in a:
        i = a.index('--mau'); mau = a[i + 1]; del a[i:i + 2]
    hoan_thien = '--khong-hoan-thien' not in a
    a = [x for x in a if x != '--khong-hoan-thien']
    if len(a) != 2:
        raise SystemExit(__doc__)
    sl = json.load(open(a[0], encoding='utf-8'))
    o = o_tu_so_lieu(sl, json.load(open(O_NHAP, encoding='utf-8')))
    xuat(o, a[1], mau, nut=so_nhanh(sl))
    import subprocess

    def ps(ten, *ts):  # bước hoàn thiện bằng Excel COM (cần Excel trên máy)
        r = subprocess.run(['powershell', '-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', os.path.join(GOC, 'scripts', ten), os.path.abspath(a[1]), *ts],
                           capture_output=True, text=True, encoding='utf-8', errors='replace')
        print(r.stdout.strip(), r.stderr.strip())
        return r.returncode == 0
    ds = lambda L: ';'.join(f"{d['dn']}:{d['l']}" + (f":{d['nut']}" if d.get('nut') else '') for d in L or [])
    hr = sl.get('hang_rieng_bp') or ([sl['hang_cuoi_bp']] if sl.get('hang_cuoi_bp') else [])
    if hr and not hoan_thien:
        raise SystemExit('Số liệu có hàng thiếu đầu: cần Excel trên máy để chèn dòng tính (bỏ --khong-hoan-thien)')
    for hc in sorted(hr, key=lambda h: -h['chi_so']):  # chèn từ dưới lên: dòng phía trên không bị dời
        if not ps('chen_hang_cuoi.ps1', str(104 + 8 * hc['chi_so'] if hc['chi_so'] < 6 else 153), ds(hc['trai']), ds(hc.get('phai'))):
            raise SystemExit('chen_hang_cuoi.ps1 lỗi (cần Excel trên máy): hàng thiếu đầu chưa được tính đúng trong file')
    if hoan_thien and not ps('hoan_thien_excel.ps1'):  # hiện dòng có số liệu, ẩn dòng trống/#N/A
        print('CẢNH BÁO: chưa ẩn được dòng trống (cần Excel trên máy); số liệu trong file vẫn đúng')
    print(f'Đã ghi {len(o)} ô vào {a[1]}. Mở bằng Excel để tính lại.')
