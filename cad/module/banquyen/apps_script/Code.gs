// Bản quyền TKPCCC trên Google Sheet (Apps Script gắn với Sheet). Hướng dẫn: cad/module/banquyen/HUONG_DAN.md.
// Hợp đồng key (phải khớp plugin cad/module/plugin/BanQuyen.cs từng byte):
//   nội dung = "TKPCCC1|<email chữ thường>|<mã máy>|<hạn yyyy-MM-dd>" (UTF-8); chữ ký RSA SHA-256 PKCS#1 v1.5;
//   key = base64url(nội dung) + "." + base64url(chữ ký).
// Script Properties: KHOA_RIENG (PEM "-----BEGIN PRIVATE KEY-----", từ TKPCCC.KeyGen.exe), TU_DONG_CAP ("true" = cấp không cần duyệt),
//   SO_NGAY (hạn mặc định, 365), GUI_MAIL ("true" = gửi key qua mail), TEN_TRANG (tên trang tính, bỏ trống = trang đầu).

var TEN_COT = { email: 'Email', may: 'Mã máy', han: 'Hạn', key: 'Key', tt: 'Trạng thái' };

// ---- Hàm thuần (thử được bằng Node: cad/test/thu_ban_quyen.ps1) ----

function noiDungKy(email, may, han) {
  return 'TKPCCC1|' + String(email).trim().toLowerCase() + '|' + String(may).trim().toUpperCase() + '|' + han;
}

/** U = Utilities (Node truyền bản giả). */
function taoKey(email, may, han, khoaPem, U) {
  var nd = noiDungKy(email, may, han);
  var ky = U.computeRsaSha256Signature(nd, khoaPem, U.Charset.UTF_8);
  return U.base64EncodeWebSafe(nd, U.Charset.UTF_8) + '.' + U.base64EncodeWebSafe(ky);
}

/** Ô Script Property có thể mất xuống dòng khi dán: dựng lại PEM chuẩn. */
function chuanPem(s) {
  var than = String(s || '').replace(/\\n/g, '\n').replace(/-----[A-Z ]+-----/g, '').replace(/\s+/g, '');
  if (!than) return '';
  return '-----BEGIN PRIVATE KEY-----\n' + than.match(/.{1,64}/g).join('\n') + '\n-----END PRIVATE KEY-----\n';
}

function hai(n) { return (n < 10 ? '0' : '') + n; }
function dinhDang(d) { return d.getFullYear() + '-' + hai(d.getMonth() + 1) + '-' + hai(d.getDate()); }

/** Ô Hạn -> "yyyy-MM-dd". Trống: homNay + soNgay. Nhận ô ngày, "yyyy-MM-dd", "dd/MM/yyyy". Sai dạng: "". */
function chuanHan(v, soNgay, homNay) {
  if (Object.prototype.toString.call(v) === '[object Date]') return isNaN(v.getTime()) ? '' : dinhDang(v);
  var s = String(v == null ? '' : v).trim(), m;
  if (!s) { var d = new Date(homNay.getTime()); d.setDate(d.getDate() + soNgay); return dinhDang(d); }
  if ((m = s.match(/^(\d{4})-(\d{1,2})-(\d{1,2})$/))) return m[1] + '-' + hai(+m[2]) + '-' + hai(+m[3]);
  if ((m = s.match(/^(\d{1,2})\/(\d{1,2})\/(\d{4})$/))) return m[3] + '-' + hai(+m[2]) + '-' + hai(+m[1]);
  return '';
}

/** Chỉ số cột (0-based) theo tiêu đề; cột email của Google Form ("Địa chỉ email", "Email Address") cũng nhận. -1 = không có. */
function timCot(tieuDe, k) {
  var t = tieuDe.map(function (x) { return String(x).trim().toLowerCase(); });
  var i = t.indexOf(TEN_COT[k].toLowerCase());
  if (i < 0 && k === 'email') for (var j = 0; j < t.length; j++) if (t[j].indexOf('email') >= 0) return j;
  return i;
}

function boDau(s) { return String(s || '').trim().toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/đ/g, 'd'); }

// ---- Phần chạy trên Google ----

function prop(k) { return PropertiesService.getScriptProperties().getProperty(k) || ''; }

function trang() {
  var ss = SpreadsheetApp.getActiveSpreadsheet(), ten = prop('TEN_TRANG');
  var sh = ten ? ss.getSheetByName(ten) : ss.getSheets()[0];
  if (!sh) throw new Error('Không thấy trang tính "' + ten + '"');
  return sh;
}

/** Tìm cột theo tiêu đề hàng 1, thiếu thì thêm ở cuối. */
function cot(sh) {
  var n = sh.getLastColumn();
  var tieuDe = n ? sh.getRange(1, 1, 1, n).getValues()[0] : [];
  var c = {};
  for (var k in TEN_COT) {
    var i = timCot(tieuDe, k);
    if (i < 0) { tieuDe.push(TEN_COT[k]); i = tieuDe.length - 1; sh.getRange(1, i + 1).setValue(TEN_COT[k]); }
    c[k] = i;
  }
  return c;
}

/** Chạy MỘT lần bằng tay: tạo trình kích hoạt 5 phút gọi quet(), thêm cột thiếu. */
function caiDat() {
  ScriptApp.getProjectTriggers().forEach(function (t) { if (t.getHandlerFunction() === 'quet') ScriptApp.deleteTrigger(t); });
  ScriptApp.newTrigger('quet').timeBased().everyMinutes(5).create();
  cot(trang());
  if (!chuanPem(prop('KHOA_RIENG'))) throw new Error('Đã tạo trình kích hoạt, nhưng CHƯA đặt Script Property KHOA_RIENG.');
}

/** Cấp key cho hàng có email + mã máy, chưa có key, không "Khoá"; cần TU_DONG_CAP = "true" hoặc trạng thái "Duyệt". */
function quet() {
  var lock = LockService.getScriptLock();
  if (!lock.tryLock(30000)) return;
  try {
    var khoa = chuanPem(prop('KHOA_RIENG'));
    if (!khoa) throw new Error('Chưa đặt Script Property KHOA_RIENG');
    var tuDong = prop('TU_DONG_CAP') === 'true', guiMail = prop('GUI_MAIL') === 'true';
    var soNgay = Number(prop('SO_NGAY')) || 365;
    var sh = trang(), c = cot(sh), cuoi = sh.getLastRow();
    if (cuoi < 2) return;
    var v = sh.getRange(2, 1, cuoi - 1, sh.getLastColumn()).getValues();
    for (var i = 0; i < v.length; i++) {
      var r = v[i], email = String(r[c.email]).trim(), may = String(r[c.may]).trim(), tt = boDau(r[c.tt]);
      if (!email || !may || String(r[c.key]).trim() || tt === 'khoa') continue;
      if (!tuDong && tt !== 'duyet') continue;
      var hang = i + 2, han = chuanHan(r[c.han], soNgay, new Date());
      if (!han) { sh.getRange(hang, c.tt + 1).setValue('Lỗi: Hạn sai dạng (yyyy-MM-dd)'); continue; }
      var key = taoKey(email, may, han, khoa, Utilities);
      sh.getRange(hang, c.han + 1).setNumberFormat('@').setValue(han);
      sh.getRange(hang, c.key + 1).setValue(key);
      sh.getRange(hang, c.tt + 1).setValue('Đã cấp');
      if (guiMail) MailApp.sendEmail(email, 'Key bản quyền TKPCCC',
        'Mã máy: ' + may + '\nHạn: ' + han + '\n\nKey:\n' + key + '\n\nTrong AutoCAD gõ PCCC_BANQUYEN, dán key, bấm Kích hoạt (hoặc bấm Lấy key tự động).');
    }
  } finally { lock.releaseLock(); }
}

function traJson(o) { return ContentService.createTextOutput(JSON.stringify(o)).setMimeType(ContentService.MimeType.JSON); }

/** ?hd=dang_ky&email=..&may=..  |  ?hd=lay_key&email=..&may=.. */
function doGet(e) {
  var q = (e && e.parameter) || {};
  var email = String(q.email || '').trim().toLowerCase(), may = String(q.may || '').trim().toUpperCase();
  if (!/^[^@\s]+@[^@\s]+\.[^@\s]+$/.test(email) || !/^[0-9A-F]{4}(-[0-9A-F]{4}){3}$/.test(may))
    return traJson({ ok: false, tb: 'Email hoặc mã máy sai dạng.' });
  if (q.hd !== 'dang_ky' && q.hd !== 'lay_key') return traJson({ ok: false, tb: 'Thiếu hd.' });
  var lock = LockService.getScriptLock();
  if (!lock.tryLock(20000)) return traJson({ ok: false, tb: 'Máy chủ bận, thử lại.' });
  try {
    var sh = trang(), c = cot(sh), n = sh.getLastColumn(), cuoi = sh.getLastRow();
    var v = cuoi < 2 ? [] : sh.getRange(2, 1, cuoi - 1, n).getValues(), r = null;
    for (var i = 0; i < v.length; i++)
      if (String(v[i][c.email]).trim().toLowerCase() === email && String(v[i][c.may]).trim().toUpperCase() === may) r = v[i];
    if (q.hd === 'lay_key') {
      var key = r ? String(r[c.key]).trim() : '';
      if (key && boDau(r[c.tt]) !== 'khoa') return traJson({ ok: true, key: key });
      return traJson({ ok: false, tb: r ? 'Đã đăng ký, chưa được cấp key. Thử lại sau ít phút.' : 'Chưa đăng ký: bấm Đăng ký trước.' });
    }
    if (r) return traJson({ ok: true, tb: String(r[c.key]).trim() ? 'Đã cấp key: bấm Lấy key tự động.' : 'Đã đăng ký trước đó, chờ cấp key.' });
    var hang = []; for (var j = 0; j < n; j++) hang.push('');
    hang[c.email] = email; hang[c.may] = may;
    sh.appendRow(hang);
    return traJson({ ok: true, tb: 'Đã gửi đăng ký. Key được cấp trong khoảng 5 phút (hoặc khi chủ duyệt): bấm Lấy key tự động.' });
  } finally { lock.releaseLock(); }
}
