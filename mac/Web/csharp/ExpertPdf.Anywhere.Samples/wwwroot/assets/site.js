/* ExpertPdf site - interactivity (vanilla, no build step). The page works without it. */
(function () {
  'use strict';

  var root = document.documentElement;
  var reduceMotion = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  var THEME_KEY = 'expertpdf-theme-v2';

  /* ---------- Theme toggle (the saved theme is applied by an inline script in <head>) ---------- */
  var themeBtn = document.querySelector('.theme-toggle');
  if (themeBtn) {
    var sync = function () {
      themeBtn.setAttribute('aria-pressed', root.getAttribute('data-theme') === 'dark' ? 'true' : 'false');
    };
    sync();
    themeBtn.addEventListener('click', function () {
      var next = root.getAttribute('data-theme') === 'dark' ? 'light' : 'dark';
      root.setAttribute('data-theme', next);
      try { localStorage.setItem(THEME_KEY, next); } catch (e) { }
      sync();
    });
  }

  /* ---------- Code tabs (click and arrow keys) ---------- */
  document.querySelectorAll('[role="tablist"]').forEach(function (list) {
    var tabs = Array.prototype.slice.call(list.querySelectorAll('[role="tab"]'));
    function select(tab, focus) {
      tabs.forEach(function (t) {
        var on = t === tab;
        t.setAttribute('aria-selected', on ? 'true' : 'false');
        t.tabIndex = on ? 0 : -1;
        var pane = document.getElementById(t.getAttribute('aria-controls'));
        if (pane) { pane.hidden = !on; pane.setAttribute('aria-hidden', on ? 'false' : 'true'); }
      });
      if (focus) tab.focus();
    }
    tabs.forEach(function (t, i) {
      t.addEventListener('click', function () { select(t, false); });
      t.addEventListener('keydown', function (e) {
        var j = e.key === 'ArrowRight' ? i + 1 : e.key === 'ArrowLeft' ? i - 1 : e.key === 'Home' ? 0 : e.key === 'End' ? tabs.length - 1 : null;
        if (j === null) return;
        e.preventDefault();
        select(tabs[(j + tabs.length) % tabs.length], true);
      });
    });
  });

  /* ---------- Mobile nav drawer ---------- */
  var navToggle = document.querySelector('.nav-toggle');
  var navLinks = document.querySelector('.nav-links');
  var navItems = document.querySelectorAll('.nav-item.has-sub');
  function closeSubs(except) {
    navItems.forEach(function (item) {
      if (item === except) return;
      item.setAttribute('data-open', 'false');
      var c = item.querySelector('.nav-caret');
      if (c) c.setAttribute('aria-expanded', 'false');
    });
  }
  if (navToggle && navLinks) {
    navToggle.addEventListener('click', function () {
      var open = navLinks.classList.toggle('open');
      navToggle.setAttribute('aria-expanded', open ? 'true' : 'false');
      if (!open) closeSubs(null);
    });
    navLinks.querySelectorAll('a.nav-link, a.sub-link').forEach(function (a) {
      a.addEventListener('click', function () {
        navLinks.classList.remove('open');
        navToggle.setAttribute('aria-expanded', 'false');
      });
    });
  }

  /* ---------- Sub-menus (Converter, Products) ---------- */
  navItems.forEach(function (item) {
    var caret = item.querySelector('.nav-caret');
    if (!caret) return;
    caret.addEventListener('click', function (e) {
      e.preventDefault();
      e.stopPropagation();
      var willOpen = item.getAttribute('data-open') !== 'true';
      closeSubs(item);
      item.setAttribute('data-open', willOpen ? 'true' : 'false');
      caret.setAttribute('aria-expanded', willOpen ? 'true' : 'false');
    });
  });
  if (window.matchMedia('(hover: hover) and (min-width: 1001px)').matches) {
    navItems.forEach(function (item) {
      var timer;
      item.addEventListener('mouseenter', function () {
        clearTimeout(timer);
        closeSubs(item);
        item.setAttribute('data-open', 'true');
        var c = item.querySelector('.nav-caret');
        if (c) c.setAttribute('aria-expanded', 'true');
      });
      item.addEventListener('mouseleave', function () {
        timer = setTimeout(function () {
          item.setAttribute('data-open', 'false');
          var c = item.querySelector('.nav-caret');
          if (c) c.setAttribute('aria-expanded', 'false');
        }, 120);
      });
    });
  }
  document.addEventListener('click', function (e) {
    navItems.forEach(function (item) { if (!item.contains(e.target)) closeSubs(null); });
  });
  document.addEventListener('keydown', function (e) { if (e.key === 'Escape') closeSubs(null); });

  /* ---------- In-page anchors: smooth scroll under the sticky nav ---------- */
  document.querySelectorAll('a[href^="#"]').forEach(function (a) {
    a.addEventListener('click', function (e) {
      var id = a.getAttribute('href');
      if (!id || id === '#' || id.length < 2) return;
      var el = document.getElementById(id.slice(1));
      if (!el) return;
      e.preventDefault();
      var top = el.getBoundingClientRect().top + window.pageYOffset - 80;
      window.scrollTo({ top: top, behavior: reduceMotion ? 'auto' : 'smooth' });
      history.replaceState(null, '', id);
    });
  });

  /* ---------- Code tab rows stay on one line: when the tabs don't fit, a dropdown replaces them ---------- */
  var tabRows = Array.prototype.slice.call(document.querySelectorAll('.code-wrap > .tabs'));
  tabRows.forEach(function (list) {
    var tabs = Array.prototype.slice.call(list.querySelectorAll('[role="tab"]'));
    var picker = document.createElement('select');
    picker.className = 'tabs-picker';
    picker.setAttribute('aria-label', list.getAttribute('aria-label') || 'Code samples');
    tabs.forEach(function (t, i) {
      var o = document.createElement('option');
      o.value = String(i);
      o.textContent = t.textContent.trim();
      picker.appendChild(o);
      t.addEventListener('click', function () { picker.value = String(i); });
    });
    picker.addEventListener('change', function () { tabs[+picker.value].click(); });
    list.appendChild(picker);
  });
  function fitTabRows() {
    tabRows.forEach(function (list) {
      list.classList.remove('tabs-select');
      if (list.scrollWidth > list.clientWidth + 1) list.classList.add('tabs-select');
    });
  }
  fitTabRows();
  window.addEventListener('resize', fitTabRows);

  /* ---------- C# / VB.NET switch of grouped code samples (the choice applies to every sample and is remembered) ---------- */
  var LANG_KEY = 'expertpdf-code-lang';
  function showLang(lang) {
    document.querySelectorAll('.code-grouped .code-pane').forEach(function (pane) {
      var panes = pane.querySelectorAll('.code-lang-pane');
      var match = Array.prototype.some.call(panes, function (p) { return p.getAttribute('data-lang') === lang; });
      if (!match) return;
      panes.forEach(function (p) { p.hidden = p.getAttribute('data-lang') !== lang; });
    });
  }
  document.querySelectorAll('.lang-btn').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var lang = btn.getAttribute('data-lang');
      showLang(lang);
      try { localStorage.setItem(LANG_KEY, lang); } catch (e) { }
    });
  });
  try { var savedLang = localStorage.getItem(LANG_KEY); if (savedLang) showLang(savedLang); } catch (e) { }

  /* ---------- Line numbers on code blocks ----------
     Each line becomes its own block, so a long line wraps under its own text (not under the numbers) and code
     never scrolls sideways. Highlight spans never cross a line break, so splitting the HTML on newlines is safe. */
  document.querySelectorAll('pre.code').forEach(function (pre) {
    var html = pre.innerHTML.replace(/<span class="ln"[^>]*>\d+<\/span>\s?/g, '').replace(/^\n+/, '').replace(/\n+$/, '');
    var lines = html.split('\n');
    pre.innerHTML = lines.map(function (ln, i) {
      return '<span class="cl"><span class="ln" aria-hidden="true">' + (i + 1) + '</span>' + (ln === '' ? ' ' : ln) + '</span>';
    }).join('');
    pre.classList.add('code-lines');
    // Long lines may break after a dot or an opening parenthesis (zero-width spaces, removed again by Copy).
    var walker = document.createTreeWalker(pre, NodeFilter.SHOW_TEXT);
    var texts = [];
    while (walker.nextNode()) if (!walker.currentNode.parentNode.classList.contains('ln')) texts.push(walker.currentNode);
    texts.forEach(function (t) { t.nodeValue = t.nodeValue.replace(/(\.(?=\w)|\((?=[^)\s]))/g, '$1\u200B'); });
  });

  /* ---------- Copy buttons ---------- */
  function copyText(text, btn, label) {
    var done = function () {
      if (label) {
        var orig = btn.textContent;
        btn.textContent = 'Copied ✓';
        setTimeout(function () { btn.textContent = orig; }, 1300);
      } else {
        btn.classList.add('is-copied');
        setTimeout(function () { btn.classList.remove('is-copied'); }, 1200);
      }
    };
    if (navigator.clipboard) navigator.clipboard.writeText(text).then(done, function () { });
  }
  function codeText(pre) {
    var lines = pre.querySelectorAll('.cl');
    if (!lines.length) {
      var clone = pre.cloneNode(true);
      clone.querySelectorAll('.ln').forEach(function (n) { n.remove(); });
      return clone.innerText.replace(/^[ \t]+/, '');
    }
    return Array.prototype.map.call(lines, function (l) {
      var c = l.cloneNode(true);
      var n = c.querySelector('.ln');
      if (n) n.remove();
      return c.textContent === ' ' ? '' : c.textContent;
    }).join('\n').replace(/\u200B/g, '');
  }
  document.querySelectorAll('.cmd-copy[data-copy]').forEach(function (btn) {
    btn.addEventListener('click', function () { copyText(btn.getAttribute('data-copy'), btn, false); });
  });
  document.querySelectorAll('.code-toolbar .copy-btn, .code-toolbar .ico-btn[data-copy]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var paneId = btn.getAttribute('data-copy');
      var scope = paneId ? document.getElementById(paneId) : btn.closest('.code-wrap');
      if (!scope) return;
      var pre = scope.querySelector('pre.code:not([hidden])') || scope.querySelector('pre.code');
      if (!scope.querySelector('pre.code')) return;
      // With tabs, copy the visible pane.
      var visible = scope.querySelector('.code-pane:not([hidden]) pre.code');
      copyText(codeText(visible || pre), btn, true);
    });
  });

  /* ---------- Free online converters: convert in place, download the PDF ---------- */
  document.querySelectorAll('form[data-convert]').forEach(function (form) {
    var status = form.querySelector('.demo-status');
    var text = status && status.querySelector('.txt');
    var btn = form.querySelector('button[type="submit"]');
    function setStatus(state, msg) {
      if (!status) return;
      status.setAttribute('data-state', state);
      if (text) text.textContent = msg;
    }
    form.addEventListener('submit', function (e) {
      if (!window.fetch || !window.Blob) return; // plain form post still works
      e.preventDefault();
      if (!form.reportValidity()) return;
      setStatus('working', 'Converting…');
      if (btn) btn.disabled = true;
      var started = Date.now();
      fetch(form.action, { method: 'POST', body: new FormData(form), headers: { 'X-Requested-With': 'fetch' } })
        .then(function (r) {
          var type = r.headers.get('Content-Type') || '';
          if (r.ok && type.indexOf('application/pdf') === 0) {
            var pages = r.headers.get('X-Pdf-Pages');
            return r.blob().then(function (blob) {
              var a = document.createElement('a');
              a.href = URL.createObjectURL(blob);
              a.download = form.getAttribute('data-filename') || 'document.pdf';
              document.body.appendChild(a);
              a.click();
              setTimeout(function () { URL.revokeObjectURL(a.href); a.remove(); }, 4000);
              var secs = ((Date.now() - started) / 1000).toFixed(1);
              setStatus('done', 'PDF ready' + (pages ? ' · ' + pages + (pages === '1' ? ' page' : ' pages') : '') + ' · ' + secs + ' s');
              if (window.gtag) window.gtag('event', 'demo_conversion', { tool: form.getAttribute('data-convert') });
            });
          }
          return r.json().then(function (j) { setStatus('error', j && j.error ? j.error : 'The conversion failed.'); },
                               function () { setStatus('error', 'The conversion failed. Please try again.'); });
        })
        .catch(function () { setStatus('error', 'The conversion failed. Please check your connection and try again.'); })
        .finally(function () { if (btn) btn.disabled = false; });
    });
  });

  /* ---------- Analytics events (only when analytics is enabled; Consent Mode gates what is stored) ----------
     One delegated listener in the capture phase, so a handler that stops the click cannot hide it.
     - a[data-track]: a GA4 event named by the attribute (buy_*, product_download, ...).
     - a download zip (downloads.html-to-pdf.net/*.zip) or a nuget.org package link, anywhere on the site: the GA4 event
       download_zip or nuget_click, the GA4 key event trial_download (method zip / nuget), the Google Ads
       "ExpertPdf Download" conversion and the Bing UET event "download" (label zip / nuget). NuGet is the main way to install, so counting zips alone would undercount. */
  document.addEventListener('click', function (e) {
    var a = e.target.closest && e.target.closest('a[href]');
    if (!a) return;
    var gtag = typeof window.gtag === 'function' ? window.gtag : null;
    var track = a.getAttribute('data-track');
    var host = (a.hostname || '').toLowerCase();
    var nuget = (host === 'nuget.org' || host === 'www.nuget.org') && /^\/packages\/[^/]+/i.test(a.pathname);
    var zip = !nuget && /\.zip$/i.test(a.pathname) && (host.indexOf('downloads.') === 0 || host === location.hostname);
    if (gtag && track && track !== 'download_zip') gtag('event', track, { link_url: a.href });
    if (!nuget && !zip) return;
    var file = decodeURIComponent(nuget ? (a.pathname.split('/')[2] || '') : a.pathname.split('/').pop());
    if (gtag) {
      if (nuget) gtag('event', 'nuget_click', { package_id: file, link_url: a.href });
      else gtag('event', 'download_zip', { file_name: file, link_url: a.href });
      gtag('event', 'trial_download', { method: nuget ? 'nuget' : 'zip', file_name: file });
      var conv = window.expertpdfTrack && window.expertpdfTrack.downloadConversion;
      if (conv) gtag('event', 'conversion', { send_to: conv, value: 1 });
    }
    if (window.uetq) window.uetq.push('event', 'download', { event_category: 'download', event_label: nuget ? 'nuget' : 'zip' });
  }, true);
})();
