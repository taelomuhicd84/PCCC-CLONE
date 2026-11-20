"""Kiem lien ket du lieu giua module, skill, knowledge, agent (0 token, chi doc). Chay: python scripts/kiem_lien_ket.py [--strict]

Moi phep kiem in `file:dong: ly do`. Muc LOI -> exit 1. Muc CANH BAO (loi that da biet, chua sua) -> chi in; --strict doi CANH BAO thanh LOI.
Phep kiem theo ket_qua/review_1115/R2_lien_ket_data.md muc 4a. Duong dan theo cau truc moi: backend/ (src, shared), frontend/.
"""
import glob, importlib.util, json, os, re, subprocess, sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SKIP = {'.git', 'node_modules', 'worktrees', 'ket_qua', 'bin', 'obj', 'file_cad_test', '__pycache__', '.pytest_cache', 'TKPCCC.bundle'}
LOI, CANH_BAO = [], []


def bao(muc, ma, vitri, ly_do):
    (LOI if muc == 'LOI' else CANH_BAO).append(f'[{ma}] {vitri}: {ly_do}')


def doc(rel):
    return (ROOT / rel).read_text(encoding='utf-8')


def tep(*mau):
    return sorted(p.relative_to(ROOT).as_posix() for m in mau for p in ROOT.glob(m)
                  if p.is_file() and not (set(p.relative_to(ROOT).parts) & SKIP))


def mo_ta():  # tai lieu duoc kiem: CLAUDE, STRUCTURE, README, cad/*.md goc, LUAT, TU_VUNG, SKILL.md, agent
    return tep('CLAUDE.md', 'STRUCTURE.md', 'README.md', 'cad/CLAUDE.md', 'cad/README.md', 'cad/rule/LUAT.md', 'cad/skills/TU_VUNG.md',
               '.claude/skills/*/SKILL.md', 'cad/.claude/skills/*/SKILL.md', '.claude/agents/*.md', 'cad/.claude/agents/*.md')


# ---------- 1. duong dan chet ----------
TOP = {'backend', 'frontend', 'cad', 'data', 'knowledge', 'skills', 'scripts', 'thamdinh', 'bang_tinh', 'runner', 'shared', 'web', '.claude'}
EXT = r'(?:md|py|ts|cs|json|jsonl|ps1|bat|yaml|yml|xlsx|dwg|lsp|txt|mjs|js|html|xml|csproj|docx)'
# file sinh khi chay / nam o may khac / ten giu cho: khong bao chet
SINH = {'doi_chieu.json', 'doi_chieu.xlsx', 'quy_tac_agent.md', 'cong_trinh.md', 'bang_doi_chieu.md', 'findings.jsonl', 'don_gia.json', 'khao_sat.json',
        'khoi_luong.json', 'tom_tat.json', 'claude_xet.json', 'so_lieu.json', 'mat_bang.json', 'du_an.json', 'block_alias.json', 'phim_tat.txt',
        'report.md', 'cong_trinh.pdf', 'mcp.json', 'hardcopy.log', 'plot.log', 'thoat_nan.py', 'SKILL_quy_trinh_tham_dinh.md',
        'b1_truc.json', 'b2_tuong.json', 'b3_chi_tiet.json', 'b4_thiet_bi.json', 'b5_tuyen.json', 'b6_so_do.json'}
NGOAI_REPO = {'data/bang', 'data/ban_ve_mau', 'skills/SKILL_quy_trinh_tham_dinh.md'}   # co o repo SKILL-PCCC-NEW, khong chep sang


def phep1():
    ten = {}
    for dp, dns, fns in os.walk(ROOT):
        dns[:] = [d for d in dns if d not in SKIP]
        for f in fns:
            ten.setdefault(f, []).append(Path(dp, f).relative_to(ROOT).as_posix())
    for f in glob.glob(str(ROOT / 'cad/TKPCCC.bundle/**/*'), recursive=True):   # file trong bundle: chi de tra theo ten
        ten.setdefault(os.path.basename(f), []).append(os.path.relpath(f, ROOT).replace(os.sep, '/'))

    def ton_tai(t, d):
        m = re.match(r'((?:cad/)?\.claude/skills/[^/]+)', d)
        goc = ['', os.path.dirname(d), 'cad'] + ([m.group(1), m.group(1) + '/scripts'] if m else [])
        return any((ROOT / os.path.normpath(os.path.join(g, t))).exists() for g in goc)

    for d in mo_ta():
        for ln, line in enumerate(doc(d).splitlines(), 1):
            for span in re.findall(r'`([^`]+)`', line):
                for t in re.split(r'[\s,;()]+', span):
                    t = re.sub(r':\d+(-\d+)?$', '', t.strip('"\'.:')).rstrip('/')
                    if not t or re.search(r'[<>*{}$%…]|^https?|^/|^-|\\', t):
                        continue
                    ten_cuoi = t.rsplit('/', 1)[-1]
                    if ten_cuoi in SINH or t in NGOAI_REPO or t.startswith(('ket_qua', 'ban_ve', 'anh/', 'jobs/', 'phan/')):
                        continue
                    co_dau_cach = '/' in t and t.split('/')[0] in TOP and re.match(r'^[\w.\-/]+$', t)
                    la_tep = re.search(r'\.' + EXT + r'$', t) and re.match(r'^[\w.\-/]+$', t)
                    if not (co_dau_cach or (la_tep and '/' not in t)):
                        continue
                    if ton_tai(t, d) or (('/' not in t) and ten_cuoi in ten):
                        continue
                    bao('LOI', 'duong-dan-chet', f'{d}:{ln}', f'`{t}` khong ton tai' + (f' (co {ten[ten_cuoi][0]})' if ten_cuoi in ten else ''))
    # data/huong_dan.json tai_lieu
    hd = json.loads(doc('data/huong_dan.json'))
    for b in hd['buoc']:
        for t in b.get('tai_lieu', []):
            if not (ROOT / t.split('#')[0]).exists():
                bao('LOI', 'duong-dan-chet', f'data/huong_dan.json ({b["ma"]})', f'tai_lieu `{t}` khong ton tai')


# ---------- 2. ma vat tu ----------
def phep2():
    vt = json.loads(doc('data/vat_tu.json'))
    ma = {x['ma'] for x in vt['vat_tu']}
    ngoai_le = {'SPR_VAN_OSY', 'ONG_GIO', 'ONG_THEP', 'ONG_DN', 'SPR_DAU_DWN', 'DAY_NGUON', 'DAY_NGUON_FR', 'DAY_TH', 'DAY_TH_FR', 'DAY_TUONG_MAX_M', 'SPR_VAN_OSY_DN'}
    pre = ('BC_', 'SPR_', 'HVT_', 'NUOC_', 'BINH_', 'DEN_', 'ONG_', 'DAY_', 'CAP_', 'QUAT_', 'DIEN_', 'CHONG_', 'CUA_', 'BE_', 'HS_')
    re_ma = re.compile(r'(?<![A-Za-z0-9_])((?:' + '|'.join(pre) + r')[A-Z0-9_]*[A-Z0-9])(?![A-Za-z0-9_])')
    for f in tep('backend/shared/src/*.ts', 'backend/src/**/*.ts', 'cad/module/**/*.cs', 'cad/skills/**/*.md', 'cad/data/**/*.json', 'frontend/index.html'):
        for ln, line in enumerate(doc(f).splitlines(), 1):
            for m in re_ma.findall(line):
                if m not in ma and m not in ngoai_le and not any(m.startswith(n) for n in ngoai_le if n.endswith('_')):
                    bao('CANH_BAO', 'ma-vat-tu', f'{f}:{ln}', f'mã `{m}` không có trong data/vat_tu.json')


# ---------- 3. he vat tu <-> huong_dan ----------
def phep3():
    he = set(json.loads(doc('data/vat_tu.json'))['he'])
    for b in json.loads(doc('data/huong_dan.json'))['buoc']:
        for h in b.get('he', []):
            if h not in he:
                bao('LOI', 'he-huong-dan', f'data/huong_dan.json ({b["ma"]})', f'he `{h}` không có trong data/vat_tu.json.he')


# ---------- 4. khoang_cach_rai nguyen van ----------
def phep4():
    k = json.loads(doc('data/khoang_cach_rai.json'))
    vt = {x['ma'] for x in json.loads(doc('data/vat_tu.json'))['vat_tu']}
    chuan = lambda s: re.sub(r'\s+', ' ', s).strip()
    cache = {}
    for t in k['thiet_bi'] + k.get('cu_ly', []):
        ten = ','.join(t['ma'])
        for m in t['ma']:
            if m not in vt:
                bao('LOI', 'khoang-cach-rai', 'data/khoang_cach_rai.json', f'mã `{m}` không có trong vat_tu.json')
        nguon = t.get('nguon', '')
        if not (ROOT / nguon).exists():
            bao('LOI', 'khoang-cach-rai', 'data/khoang_cach_rai.json', f'{ten}: nguon `{nguon}` không tồn tại')
            continue
        van = cache.setdefault(nguon, chuan(doc(nguon)))
        cau = t.get('nguyen_van', [])
        cau = list(cau) if isinstance(cau, list) else [cau]
        if 'dam' in t:
            cau.append(t['dam'].get('nguyen_van', ''))
        for s in cau:
            for ph in chuan(s).split('...'):
                ph = re.sub(r'^\d+(\.\d+)+\s*([a-z]\)\s*)?', '', ph.strip())   # bo so dieu va "a)" o dau
                if len(ph) > 15 and ph not in van:
                    bao('LOI', 'khoang-cach-rai', 'data/khoang_cach_rai.json', f'{ten}: câu nguyên văn không có trong {nguon}: "{ph[:80]}"')
        nv = ' '.join(cau).replace(',', '.')
        for kh in ('s', 'tuong', 'tuong_min', 'r', 'h_max', 'd'):
            if kh in t:
                v = ('%g' % t[kh])
                if not re.search(r'(?<![\d.])' + re.escape(v) + r'(?![\d])', nv):
                    bao('CANH_BAO', 'khoang-cach-rai', 'data/khoang_cach_rai.json', f'{ten}: `{kh}`={v} không thấy trong nguyên văn của dòng')


# ---------- 5 + 6. bang doi chieu ----------
def nap_bdc():
    p = ROOT / '.claude/skills/tkpccc-bang-doi-chieu/scripts/bang_doi_chieu.py'
    sp = importlib.util.spec_from_file_location('bdc', p)
    m = importlib.util.module_from_spec(sp)
    sp.loader.exec_module(m)
    return m


def phep5_6():
    d = '.claude/skills/tkpccc-bang-doi-chieu/data/'
    idx = json.loads(doc(d + 'index.json'))
    ma_idx = {x['ma'] for x in idx}
    ma_file = {Path(f).stem for f in tep(d + '*.json')} - {'index'}
    for m in sorted(ma_idx - ma_file):
        bao('LOI', 'bang-doi-chieu', d + 'index.json', f'bảng {m} có trong index nhưng thiếu file')
    for m in sorted(ma_file - ma_idx):
        bao('LOI', 'bang-doi-chieu', d + m + '.json', 'file không có trong index.json')
    bang = {}
    for x in idx:
        if x['ma'] in ma_file:
            bang[x['ma']] = json.loads(doc(d + x['ma'] + '.json'))
            n = len(bang[x['ma']].get('dong', []))
            if n != x['so_dong']:
                bao('LOI', 'bang-doi-chieu', d + x['ma'] + '.json', f'so_dong index={x["so_dong"]} nhưng có {n} dòng')
    try:
        bdc = nap_bdc()
    except Exception as e:
        bao('CANH_BAO', 'bang-doi-chieu', 'bang_doi_chieu.py', f'không nạp được để kiểm chọn bảng / trích điều: {e}')
        return
    cac = [m for _, m in getattr(bdc, 'LUAT_A', [])] + list(getattr(bdc, 'HE_THONG_BANG', {}).values()) + list(getattr(bdc, 'BANG_LUON_XET', []))
    for _, m in getattr(bdc, 'LUAT_KT', []):
        cac += m[1] if isinstance(m, tuple) else [m]
    for m in sorted(set(cac) - ma_file):
        bao('LOI', 'bang-doi-chieu', 'bang_doi_chieu.py', f'luật chọn bảng trỏ `{m}` không có file')
    # 6. trich dieu: file trich duoc phai thuoc van ban cua dieu
    nhan = {'QCVN 10': 'qcvn10', 'TCVN 7568-14': '7568', 'NĐ 105/2025': 'nghi-dinh'}
    khac = re.compile(r'(TCVN|QCVN)\s*(?!0?6\b)\d')
    for ma, t in bang.items():
        for r in t.get('dong', []):
            kd = r.get('khoan_dieu') or ''
            for so, vb in bdc._dieu_trong(kd):
                kq = bdc._trich_dieu(so, bdc._file_tim(r.get('tra_o'), vb))
                if not kq:
                    continue
                sai = (vb in nhan and nhan[vb] not in kq[0]) or (vb is None and khac.search(kd))
                if sai:
                    bao('CANH_BAO', 'trich-dieu', f'{d}{ma}.json (TT {r.get("tt")}, idx {r["idx"]})', f'điều {so} ({vb or "văn bản khác"}) trích nhầm {kq[0]}, tra_o={r.get("tra_o")!r}')
    # 7. van ban cu
    cu = [(r'TCVN\s*7336\s*:\s*2003', 'TCVN 7336:2021'), (r'7568-14\s*:\s*2015', 'TCVN 7568-14:2025'), (r'QCVN\s*06\s*:\s*20(10|20|21)', 'QCVN 06:2022 sửa đổi 2023'),
          (r'136/2020', 'NĐ 105/2025 sửa bởi NĐ 347/2026'), (r'TCVN\s*4513\s*:\s*1988', 'CẦN NGUỒN')]
    for ma, t in bang.items():
        for r in t.get('dong', []):
            for cot in ('khoan_dieu', 'quy_dinh'):
                for mau, moi in cu:
                    if re.search(mau, r.get(cot) or ''):
                        bao('CANH_BAO', 'van-ban-cu', f'{d}{ma}.json (TT {r.get("tt")}, idx {r["idx"]}, {cot})', f'trích văn bản cũ /{mau}/ → thay: {moi} (CHƯA KIỂM hiệu lực trong knowledge/)')
                        break


# ---------- 8 + 9. lenh CAD, op spec ----------
def phep8_9():
    cs = ''.join(doc(f) for f in tep('cad/module/**/*.cs'))
    lenh = set(re.findall(r'CommandMethod\(\s*"([A-Z_0-9]+)"', cs))
    layer = set(re.findall(r'"(PCCC_[A-Z0-9_]+)', cs))
    xml = ROOT / 'cad/TKPCCC.bundle/PackageContents.xml'
    if xml.exists():
        # 3 plugin (PCCC, YouTube, Bot chat), moi bundle mot PackageContents.xml
        kb = set(re.findall(r'Global="([A-Z_0-9]+)"', ''.join(f.read_text(encoding='utf-8') for f in ROOT.glob('cad/*.bundle/PackageContents.xml'))))
        for l in sorted(lenh - kb):
            bao('CANH_BAO', 'lenh-cad', 'cad/TKPCCC.bundle/PackageContents.xml', f'lệnh {l} có CommandMethod nhưng chưa khai báo')
        for l in sorted(kb - lenh):
            bao('LOI', 'lenh-cad', 'cad/TKPCCC.bundle/PackageContents.xml', f'khai báo {l} nhưng không có CommandMethod')
    for f in mo_ta() + ['data/huong_dan.json'] + tep('cad/skills/*/*.md', 'cad/skills/*/*/*.md'):
        for ln, line in enumerate(doc(f).splitlines(), 1):
            for m in re.findall(r'(?<![A-Za-z0-9_])(PCCC_[A-Z0-9_]+)', line):
                if m not in lenh and m not in layer and not m.endswith('_'):
                    bao('CANH_BAO', 'lenh-cad', f'{f}:{ln}', f'`{m}` không phải lệnh hay layer có trong cad/module')
    sp = ROOT / 'cad/module/plugin/Spec.cs'
    if sp.exists():
        ops = set(re.findall(r'case\s+"([a-z_]+)"\s*:', sp.read_text(encoding='utf-8')))
        tv = set(re.findall(r'^\|?\s*-?\s*`([a-z_]+)`', doc('cad/skills/TU_VUNG.md'), re.M))
        rieng = {'anh', 'pdf'}   # Spec.cs xử lý ngoài vòng case
        for o in sorted(ops - tv):
            bao('CANH_BAO', 'op-spec', 'cad/skills/TU_VUNG.md', f'op `{o}` có trong Spec.cs nhưng chưa có trong từ vựng')
        for o in sorted(tv - ops - rieng):
            if o in ('luoi_truc', 'tuong', 'cua', 'dat', 'ong', 'net', 'tron', 'chu', 'kt', 'kt_chuoi', 'bang', 'xoa', 'doc', 'dem', 'rai', 'vung_tu_dong', 'tuyen', 'so_do'):
                bao('LOI', 'op-spec', 'cad/skills/TU_VUNG.md', f'op `{o}` trong từ vựng nhưng không có `case` trong Spec.cs')


# ---------- 10. skill / agent ----------
def khung(f):
    m = re.match(r'---\n(.*?)\n---', doc(f), re.S)
    return dict(re.findall(r'^(\w+):\s*(.*)$', m.group(1), re.M)) if m else {}


def phep10():
    phu_tro = {'tkpccc-bang-doi-chieu', 'tkpccc-ghi-chu-ban-ve', 'tkpccc-tra-quy-chuan', 'tkpccc-phoi-hop-model'}  # CLAUDE.md: không đặt model
    for f in tep('.claude/skills/*/SKILL.md', 'cad/.claude/skills/*/SKILL.md', '.claude/agents/*.md', 'cad/.claude/agents/*.md'):
        kh = khung(f)
        ten = kh.get('name', '')
        if not ten:
            bao('LOI', 'skill-agent', f, 'thiếu frontmatter name')
            continue
        if f.endswith('SKILL.md') and ten in phu_tro:
            continue
        for k in ('model', 'effort'):
            if k not in kh:
                bao('LOI', 'skill-agent', f, f'`{ten}` thiếu frontmatter {k}')
    cl = doc('CLAUDE.md') + doc('cad/CLAUDE.md') + doc('cad/rule/LUAT.md')
    co = {khung(f).get('name') for f in tep('.claude/skills/*/SKILL.md', 'cad/.claude/skills/*/SKILL.md', '.claude/agents/*.md', 'cad/.claude/agents/*.md')}
    for t in sorted(set(re.findall(r'`((?:tkpccc|pccc|cad|web)-[a-z-]+)`', cl))):
        if t not in co:
            bao('LOI', 'skill-agent', 'CLAUDE.md / cad/CLAUDE.md / LUAT.md', f'nhắc `{t}` nhưng không có skill hay agent tên đó')
    sc = ROOT / '.claude/skills/tkpccc-bang-doi-chieu/scripts/bang_doi_chieu.py'
    if sc.exists():
        for ten, muc in re.findall(r'\("(tkpccc-[a-z-]+)", \[([^\]]*)\]\)', sc.read_text(encoding='utf-8')):
            p = ROOT / '.claude/skills' / ten / 'SKILL.md'
            for m in re.findall(r'["\']([^"\']+)["\']', muc):
                if p.exists() and not re.search(r'^## ' + re.escape(m), doc(p.relative_to(ROOT).as_posix()), re.M):
                    bao('LOI', 'skill-agent', f'bang_doi_chieu.py QUY_TAC', f'{ten}/SKILL.md không còn mục "## {m}"')


# ---------- 11. ban chep bundle ----------
def phep11():
    cap = [('data/vat_tu.json', 'vat_tu.json'), ('data/khoang_cach_rai.json', 'khoang_cach_rai.json'), ('knowledge/chon-loai-dau-bao.md', 'chon-loai-dau-bao.md'),
           ('cad/data/ky_hieu/ky_hieu.json', 'ky_hieu.json'), ('cad/data/ky_hieu/hinh_bc.json', 'hinh_bc.json'), ('cad/skills/ve/mau-thuyet-minh-pccc.md', 'mau-thuyet-minh-pccc.md')]
    for nguon, ban in cap:
        b = ROOT / 'cad/TKPCCC.bundle/Contents' / ban
        if b.exists() and (ROOT / nguon).exists() and b.read_bytes() != (ROOT / nguon).read_bytes():
            bao('CANH_BAO', 'ban-chep-bundle', str(b.relative_to(ROOT).as_posix()), f'khác {nguon}: chạy cad\\build.bat')


# ---------- 12. file nguoi dung sua chua track ----------
def phep12():
    for f in ('cad/TKPCCC.bundle/Contents/block_alias.json', 'cad/TKPCCC.bundle/Contents/phim_tat.txt'):
        if (ROOT / f).exists():
            r = subprocess.run(['git', 'ls-files', '--error-unmatch', f], cwd=ROOT, capture_output=True)
            if r.returncode != 0:
                bao('CANH_BAO', 'chua-track', f, 'file người dùng sửa có trên đĩa nhưng chưa `git add` (máy khác mất)')


PHEP = [phep1, phep2, phep3, phep4, phep5_6, phep8_9, phep10, phep11, phep12]


def main(strict=False):
    LOI.clear(); CANH_BAO.clear()
    for p in PHEP:
        try:
            p()
        except Exception as e:   # phep hong khong duoc nuot am tham
            bao('LOI', 'kiem-lien-ket', p.__name__, f'phép kiểm tự lỗi: {type(e).__name__}: {e}')
    return (LOI + CANH_BAO) if strict else LOI, CANH_BAO


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    loi, canh = main('--strict' in sys.argv)
    for x in loi:
        print('LOI      ' + x)
    for x in canh:
        if '--strict' not in sys.argv:
            print('CANH BAO ' + x)
    print(f'{len(loi)} lỗi, {len(canh)} cảnh báo')
    sys.exit(1 if loi else 0)
