"""Quan ly file tren Google Drive / OneDrive qua thu muc dong bo tren may (khong can API, tru chia se).

Chay: python 10_DRIVE/drive.py <lenh>
  cai                         do thu muc OneDrive + Google Drive (DriveFS) tren may, ghi cau hinh
  quet                        chup danh sach file tung kho, so voi lan truoc -> lich su (THEM / SUA / XOA / DOI_CHO)
  lich_su [--ngay N]          in su kien N ngay gan nhat, ghi LICH_SU_DRIVE.md
  tim <chu>                   tim file trong anh chup moi nhat
  tao_cay <thu_muc> <ma>      dung cay ho so du an danh so (cay_du_an.json) trong <thu_muc>/<ma>
  quy_hoach <thu_muc> [--lam] xep file nam le trong <thu_muc> vao thu muc con theo luat `sap_xep`; khong --lam thi chi in ke hoach
  hoan_tac                    tra lai dot di chuyen gan nhat
  chia_se <file>              tao link chia se qua rclone (can `rclone config` truoc), luu so chia se
  ds_chia_se                  in so chia se
Du lieu (ten file, duong dan may nguoi dung): 12_KET_QUA/drive/ (khong commit; doi bang bien TKPCCC_DRIVE_DATA).
"""
import datetime as dt, fnmatch, json, os, shutil, subprocess, sys
from pathlib import Path

GOC = Path(__file__).resolve().parents[1]
DATA = Path(os.environ.get('TKPCCC_DRIVE_DATA') or GOC / '12_KET_QUA' / 'drive')
CAY = Path(__file__).with_name('cay_du_an.json')
BO_QUA = ['1_Claude', 'node_modules', '.git', '$RECYCLE.BIN', '.tmp.drive*', 'desktop.ini', '~$*', '*.tmp', 'Thumbs.db', '.shortcut-targets-by-id']
SAP_XEP = [  # luat mac dinh: mau ten file (chu thuong) -> thu muc con; dong dau khop thang
    {'khop': ['*.dwg', '*.dxf', '*.bak', '*.rvt'], 'den': 'CAD'},
    {'khop': ['*.pdf'], 'den': 'PDF'},
    {'khop': ['*.xlsx', '*.xls', '*.xlsm', '*.csv'], 'den': 'BANG_TINH'},
    {'khop': ['*.docx', '*.doc'], 'den': 'VAN_BAN'},
    {'khop': ['*.jpg', '*.jpeg', '*.png', '*.heic'], 'den': 'ANH'},
    {'khop': ['*.zip', '*.rar', '*.7z'], 'den': 'NEN'},
]


def bay_gio():
    return dt.datetime.now().isoformat(timespec='seconds')


def doc_json(p, mac_dinh):
    return json.loads(p.read_text(encoding='utf-8')) if p.exists() else mac_dinh


def ghi_json(p, v):
    p.parent.mkdir(parents=True, exist_ok=True)
    p.write_text(json.dumps(v, ensure_ascii=False, indent=1), encoding='utf-8')


def ghi_su_kien(ds):
    if not ds:
        return
    DATA.mkdir(parents=True, exist_ok=True)
    with open(DATA / 'lich_su.jsonl', 'a', encoding='utf-8') as f:
        for e in ds:
            f.write(json.dumps(e, ensure_ascii=False) + '\n')


def cau_hinh():
    p = DATA / 'cau_hinh.json'
    if not p.exists():
        sys.exit('Chua co cau hinh: chay `python 10_DRIVE/drive.py cai`')
    return json.loads(p.read_text(encoding='utf-8'))


def tim_kho():
    kho = {}
    od = os.environ.get('OneDrive') or str(Path.home() / 'OneDrive')
    if os.path.isdir(od):
        kho['onedrive'] = od
    try:
        import winreg
        with winreg.OpenKey(winreg.HKEY_CURRENT_USER, r'Software\Google\DriveFS') as k:
            pref = json.loads(winreg.QueryValueEx(k, 'PerAccountPreferences')[0])
        for i, a in enumerate(pref.get('per_account_preferences', []), 1):
            m = a.get('value', {}).get('mount_point_path')
            if m and os.path.isdir(m):
                kho[f'gdrive{i}'] = m
    except (OSError, ValueError, ImportError):
        pass
    return kho


def lenh_cai(_):
    p = DATA / 'cau_hinh.json'
    ch = doc_json(p, {'bo_qua': BO_QUA, 'sap_xep': SAP_XEP, 'rclone': {}})
    ch['kho'] = {**tim_kho(), **ch.get('kho', {})}
    ghi_json(p, ch)
    print(f'Ghi {p}')
    for k, v in ch['kho'].items():
        print(f'  {k}: {v}')
    if not ch['kho']:
        print('  Khong thay kho nao: them tay vao "kho" trong file tren.')


def bo(ten, mau):
    return any(fnmatch.fnmatch(ten, m) for m in mau)


def chup(goc, mau):
    kq = {}
    for d, dns, fns in os.walk(goc):
        dns[:] = [x for x in dns if not bo(x, mau)]
        for f in fns:
            if bo(f, mau):
                continue
            p = os.path.join(d, f)
            try:
                st = os.stat(p)
            except OSError:
                continue
            kq[os.path.relpath(p, goc).replace('\\', '/')] = [st.st_size, int(st.st_mtime)]
    return kq


def so_sanh(cu, moi, kho, t):
    """Su kien tu 2 anh chup {duong_dan: [kb, mtime]}. Xoa + them cung ten + co = DOI_CHO."""
    them = [p for p in moi if p not in cu]
    xoa = [p for p in cu if p not in moi]
    sua = [p for p in moi if p in cu and moi[p] != cu[p]]
    ds = []
    con_xoa = list(xoa)
    for p in them:
        khop = next((x for x in con_xoa if os.path.basename(x) == os.path.basename(p) and cu[x][0] == moi[p][0]), None)
        if khop:
            con_xoa.remove(khop)
            ds.append({'t': t, 'kho': kho, 'loai': 'DOI_CHO', 'p': p, 'tu': khop, 'byte': moi[p][0]})
        else:
            ds.append({'t': t, 'kho': kho, 'loai': 'THEM', 'p': p, 'byte': moi[p][0]})
    ds += [{'t': t, 'kho': kho, 'loai': 'SUA', 'p': p, 'byte': moi[p][0]} for p in sua]
    ds += [{'t': t, 'kho': kho, 'loai': 'XOA', 'p': p, 'byte': cu[p][0]} for p in con_xoa]
    return ds


def lenh_quet(_):
    ch, t = cau_hinh(), bay_gio()
    for kho, goc in ch['kho'].items():
        if not os.path.isdir(goc):
            print(f'{kho}: khong mo duoc {goc}')
            continue
        f = DATA / f'anh_{kho}.json'
        cu = doc_json(f, None)
        moi = chup(goc, ch.get('bo_qua', BO_QUA))
        if cu is None:
            ds = [{'t': t, 'kho': kho, 'loai': 'LAN_DAU', 'p': '', 'so_file': len(moi)}]
        else:
            ds = so_sanh(cu, moi, kho, t)
        ghi_json(f, moi)
        ghi_su_kien(ds)
        dem = {}
        for e in ds:
            dem[e['loai']] = dem.get(e['loai'], 0) + 1
        print(f'{kho}: {len(moi)} file; ' + (', '.join(f'{k} {v}' for k, v in dem.items()) or 'khong doi'))


def doc_su_kien(ngay):
    p = DATA / 'lich_su.jsonl'
    if not p.exists():
        return []
    moc = (dt.datetime.now() - dt.timedelta(days=ngay)).isoformat(timespec='seconds')
    ds = [json.loads(l) for l in p.read_text(encoding='utf-8').splitlines() if l.strip()]
    return [e for e in ds if e['t'] >= moc]


def lenh_lich_su(a):
    ngay = int(a[a.index('--ngay') + 1]) if '--ngay' in a else 7
    ds = doc_su_kien(ngay)
    dong = [f'# Lịch sử Drive {ngay} ngày (tới {bay_gio()})', '', '| Lúc | Kho | Việc | File | Từ |', '|---|---|---|---|---|']
    for e in ds:
        dong.append(f"| {e['t'].replace('T', ' ')} | {e['kho']} | {e['loai']} | {e.get('p', '')} | {e.get('tu', '')} |")
    (DATA / 'LICH_SU_DRIVE.md').write_text('\n'.join(dong) + '\n', encoding='utf-8')
    for e in ds[-50:]:
        print(e['t'][5:16], e['kho'], e['loai'], e.get('p', ''), ('<- ' + e['tu']) if e.get('tu') else '')
    print(f'{len(ds)} su kien; bang day du: {DATA / "LICH_SU_DRIVE.md"}')


def lenh_tim(a):
    chu = ' '.join(a).lower()
    for f in sorted(DATA.glob('anh_*.json')):
        kho = f.stem[4:]
        for p, (kb, mt) in json.loads(f.read_text(encoding='utf-8')).items():
            if chu in p.lower():
                print(kho, p, f'{kb // 1024} KB', dt.datetime.fromtimestamp(mt).strftime('%Y-%m-%d'))


def lenh_tao_cay(a):
    goc = Path(a[0]) / a[1]
    cay = json.loads(CAY.read_text(encoding='utf-8'))['cay']
    n = 0
    for cha, cons in cay.items():
        for con in cons or ['']:
            d = goc / cha / con
            if not d.exists():
                d.mkdir(parents=True)
                n += 1
    ghi_su_kien([{'t': bay_gio(), 'kho': '-', 'loai': 'TAO_CAY', 'p': str(goc), 'so_thu_muc': n}])
    print(f'Tao {n} thu muc trong {goc}')


def ke_hoach(thu_muc, luat):
    kh = []
    for f in sorted(Path(thu_muc).iterdir()):
        if not f.is_file():
            continue
        dich = next((l['den'] for l in luat if bo(f.name.lower(), l['khop'])), None)
        if dich:
            kh.append((str(f), str(f.parent / dich / f.name)))
    return kh


def lenh_quy_hoach(a):
    thu_muc = a[0]
    ch = doc_json(DATA / 'cau_hinh.json', {})
    kh = ke_hoach(thu_muc, ch.get('sap_xep', SAP_XEP))
    if '--lam' not in a:
        for s, d in kh:
            print(f'{os.path.basename(s)}  ->  {os.path.relpath(d, thu_muc)}')
        print(f'{len(kh)} file se chuyen; them --lam de lam that')
        return
    dot, t = [], bay_gio()
    for s, d in kh:
        if os.path.exists(d):
            print('BO (da co file cung ten):', d)
            continue
        os.makedirs(os.path.dirname(d), exist_ok=True)
        shutil.move(s, d)
        dot.append([s, d])
    DATA.mkdir(parents=True, exist_ok=True)
    with open(DATA / 'di_chuyen.jsonl', 'a', encoding='utf-8') as f:
        f.write(json.dumps({'t': t, 'ds': dot}, ensure_ascii=False) + '\n')
    ghi_su_kien([{'t': t, 'kho': '-', 'loai': 'DI_CHUYEN', 'p': d, 'tu': s} for s, d in dot])
    print(f'Da chuyen {len(dot)} file; tra lai bang `hoan_tac`')


def lenh_hoan_tac(_):
    p = DATA / 'di_chuyen.jsonl'
    dong = p.read_text(encoding='utf-8').splitlines() if p.exists() else []
    if not dong:
        sys.exit('Khong co dot di chuyen nao')
    dot, t, tra = json.loads(dong[-1]), bay_gio(), []
    for s, d in reversed(dot['ds']):
        if os.path.exists(d) and not os.path.exists(s):
            shutil.move(d, s)
            tra.append([d, s])
        else:
            print('BO:', d)
    p.write_text(''.join(l + '\n' for l in dong[:-1]), encoding='utf-8')
    ghi_su_kien([{'t': t, 'kho': '-', 'loai': 'HOAN_TAC', 'p': s, 'tu': d} for d, s in tra])
    print(f'Tra lai {len(tra)} file (dot {dot["t"]})')


def lenh_chia_se(a):
    f = os.path.abspath(a[0])
    ch = cau_hinh()
    kho = next((k for k, g in ch['kho'].items() if f.lower().startswith(os.path.abspath(g).lower() + os.sep)), None)
    remote = ch.get('rclone', {}).get(kho or '')
    if not shutil.which('rclone') or not remote:
        sys.exit('Can rclone: `winget install Rclone.Rclone`, `rclone config` tao remote, roi ghi "rclone": {"' + (kho or '<kho>')
                 + '": "<ten_remote>:"} vao ' + str(DATA / 'cau_hinh.json'))
    rel = os.path.relpath(f, ch['kho'][kho]).replace('\\', '/')
    if kho.startswith('gdrive'):  # mount DriveFS co lop "My Drive" / "Drive của tôi"; remote rclone bat dau tu ben trong
        rel = rel.split('/', 1)[1] if '/' in rel else rel
    r = subprocess.run(['rclone', 'link', remote + rel], capture_output=True, text=True, encoding='utf-8')
    if r.returncode:
        sys.exit(r.stderr.strip())
    link = r.stdout.strip()
    so = doc_json(DATA / 'chia_se.json', [])
    so.append({'t': bay_gio(), 'kho': kho, 'p': rel, 'link': link})
    ghi_json(DATA / 'chia_se.json', so)
    ghi_su_kien([{'t': bay_gio(), 'kho': kho, 'loai': 'CHIA_SE', 'p': rel, 'link': link}])
    print(link)


def lenh_ds_chia_se(_):
    for e in doc_json(DATA / 'chia_se.json', []):
        print(e['t'][:16], e['kho'], e['p'], e['link'])


LENH = {'cai': lenh_cai, 'quet': lenh_quet, 'lich_su': lenh_lich_su, 'tim': lenh_tim, 'tao_cay': lenh_tao_cay,
        'quy_hoach': lenh_quy_hoach, 'hoan_tac': lenh_hoan_tac, 'chia_se': lenh_chia_se, 'ds_chia_se': lenh_ds_chia_se}

if __name__ == '__main__':
    if hasattr(sys.stdout, 'reconfigure'):
        sys.stdout.reconfigure(encoding='utf-8')
    if len(sys.argv) < 2 or sys.argv[1] not in LENH:
        sys.exit(__doc__)
    LENH[sys.argv[1]](sys.argv[2:])
