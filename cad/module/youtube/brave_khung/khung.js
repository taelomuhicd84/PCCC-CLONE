// TKPCCC (người dùng 18/11): bấm toàn màn hình trên YouTube trong cửa sổ Brave của plugin thì chỉ phủ khung cửa sổ đó, không nhảy cả màn hình.
// Chạy ở thế giới MAIN của trang: thay requestFullscreen / exitFullscreen / fullscreenElement bằng bản giả, phần tử được "toàn màn hình"
// phủ kín khung trang (position fixed 100vw x 100vh). Trang (YouTube) vẫn nhận fullscreenchange nên tự đổi bố cục như toàn màn hình thật.
// shortcut: video nhúng trong iframe của trang khác chỉ phủ khung iframe; cần thì cho trang cha phóng iframe.
(function () {
  if (window.__tkpcccKhung) return;
  window.__tkpcccKhung = 1;
  var LOP = 'tkpccc-toan-khung', dang = null;

  function css() {
    if (document.getElementById(LOP)) return;
    var s = document.createElement('style');
    s.id = LOP;
    s.textContent = '.' + LOP + '{position:fixed!important;left:0!important;top:0!important;width:100vw!important;height:100vh!important;' +
      'max-width:none!important;max-height:none!important;margin:0!important;z-index:2147483647!important;background:#000!important}';
    (document.head || document.documentElement).appendChild(s);
  }

  function bao(el) {
    el.dispatchEvent(new Event('fullscreenchange', { bubbles: true }));
    el.dispatchEvent(new Event('webkitfullscreenchange', { bubbles: true }));
    window.dispatchEvent(new Event('resize'));
  }

  function vao() {
    var el = this;
    if (dang === el) return Promise.resolve();
    if (dang) dang.classList.remove(LOP);
    css();
    dang = el;
    if (el !== document.documentElement) el.classList.add(LOP);
    bao(el);
    return Promise.resolve();
  }

  function ra() {
    if (!dang) return Promise.resolve();
    var el = dang;
    el.classList.remove(LOP);
    dang = null;
    bao(el);
    return Promise.resolve();
  }

  Element.prototype.requestFullscreen = Element.prototype.webkitRequestFullscreen = Element.prototype.webkitRequestFullScreen = vao;
  Document.prototype.exitFullscreen = Document.prototype.webkitExitFullscreen = Document.prototype.webkitCancelFullScreen = ra;
  ['fullscreenElement', 'webkitFullscreenElement', 'webkitCurrentFullScreenElement'].forEach(function (k) {
    Object.defineProperty(Document.prototype, k, { configurable: true, get: function () { return dang; } });
  });
  Object.defineProperty(Document.prototype, 'webkitIsFullScreen', { configurable: true, get: function () { return !!dang; } });
  // Toàn màn hình thật thì Esc do trình duyệt thoát; bản giả tự bắt Esc.
  document.addEventListener('keydown', function (e) { if (e.key === 'Escape' && dang) ra(); }, true);
})();
