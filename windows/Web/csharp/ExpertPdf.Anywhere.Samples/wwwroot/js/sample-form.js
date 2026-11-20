// Sample form "busy" feedback.
//
// Each sample converts on a classic form POST whose response is usually a
// FILE DOWNLOAD (FileContentResult). A download does NOT navigate the page and
// fires NO completion event, so without help the "Create PDF" button just sits
// there while the server renders — no feedback, and the user can double-click.
//
// On submit we swap the submitting button's label to "Converting …", block
// re-clicks, and round-trip a one-time token via a hidden field. The server
// (DownloadTokenPageFilter) echoes that token back as a cookie ON the file
// response; we poll for it to know the download was delivered and then restore
// the button — so it returns to normal exactly when the work is done, not on a
// guessed timeout. Pages that return a page result (validation error, the
// email sample, …) navigate/reload and reset the button naturally; a long
// fallback timeout covers anything unexpected.
//
// WHY THE FORM IS SUBMITTED ONLY AFTER THE BUSY LABEL HAS BEEN PAINTED
// --------------------------------------------------------------------
// Safari does not repaint the page while a top-level form submission is in
// flight, so a label swapped *after* the submission has started never reaches
// the screen.
//
// The screen keeps showing the last frame it painted, so the fix is to get the
// busy state into that frame: cancel the native submission, set the label, wait
// two animation frames so it is genuinely on screen, then submit for real via
// form.requestSubmit(). Nothing after that changes — the response is still
// delivered by an ordinary navigation, so downloads download and page results
// render as before.
//
// That reordering has one consequence to handle: for <input type="submit"> the
// value is BOTH the label and the value posted under the button's name, and now
// the label changes before the form is serialized. So the button's name is
// moved to a hidden field carrying the original value, and handed back when the
// button is restored.
(function () {
    'use strict';

    var BUSY_TEXT = 'Converting …';
    var COOKIE = 'downloadToken';
    var POLL_MS = 200;
    var FALLBACK_MS = 60000;   // safety net if the cookie never arrives
    var PAINT_MS = 250;        // safety net if animation frames never arrive

    document.addEventListener('submit', function (e) {
        var form = e.target;
        if (!form || form.tagName !== 'FORM') return;

        var btn = e.submitter ||
            form.querySelector('button[type="submit"], input[type="submit"], .mybutton');
        // Already busy means this is our own re-submit below — let it through.
        if (!btn || btn.getAttribute('data-sample-busy') === '1') return;

        // Round-trip token: added synchronously here so it's part of the POST.
        var token = newToken();
        setHidden(form, COOKIE, token);

        if (typeof form.requestSubmit === 'function') {
            e.preventDefault();
            setBusy(btn);
            afterPaint(function () { submitFor(form, btn); });
        } else {
            // No requestSubmit(): the submission is already under way and cannot
            // be re-issued, so the label can only be swapped a tick later — the
            // browser serializes the form synchronously after this handler.
            window.setTimeout(function () { setBusy(btn); }, 0);
        }

        var fallback = window.setTimeout(function () { stop(); }, FALLBACK_MS);
        var poll = window.setInterval(function () {
            if (readCookie(COOKIE) === token) { stop(); }
        }, POLL_MS);

        function stop() {
            window.clearTimeout(fallback);
            window.clearInterval(poll);
            expireCookie(COOKIE);
            restore(btn);
        }
    }, true);

    // Run cb once the current DOM state has actually been painted: the first
    // animation frame runs before that paint, the second after it. The timeout
    // covers a background tab, where animation frames never come at all and the
    // form must still be submitted.
    function afterPaint(cb) {
        var done = false;
        function go() { if (!done) { done = true; cb(); } }
        if (typeof window.requestAnimationFrame === 'function') {
            window.requestAnimationFrame(function () {
                window.requestAnimationFrame(go);
            });
        }
        window.setTimeout(go, PAINT_MS);
    }

    // Submit with the original submitter where possible, so formaction /
    // formmethod / formtarget on the button still apply.
    function submitFor(form, btn) {
        try {
            if (btn.form === form && (btn.type === 'submit' || btn.type === 'image')) {
                form.requestSubmit(btn);
                return;
            }
        } catch (err) { /* not a usable submitter — fall through */ }
        try { form.requestSubmit(); } catch (err) { form.submit(); }
    }

    function setBusy(btn) {
        btn.setAttribute('data-sample-busy', '1');

        // Preserve what this button posts before its label (== its value) moves.
        if (btn.name && btn.form) {
            var keep = document.createElement('input');
            keep.type = 'hidden';
            keep.name = btn.name;
            keep.value = btn.value;
            keep.setAttribute('data-sample-keep', '1');
            btn.form.appendChild(keep);
            btn.setAttribute('data-sample-name', btn.name);
            btn.removeAttribute('name');
        }

        if (btn.tagName === 'INPUT') {
            btn.setAttribute('data-sample-label', btn.value);
            btn.value = BUSY_TEXT;
        } else {
            btn.setAttribute('data-sample-label', btn.innerHTML);
            btn.textContent = BUSY_TEXT;
        }
        btn.classList.add('is-busy');
        btn.setAttribute('aria-busy', 'true');
        // Block double-submit WITHOUT disabling — a disabled submit button is
        // dropped from the POST body (its name/value won't be sent).
        btn.style.pointerEvents = 'none';
        btn.style.opacity = '0.7';
    }

    function restore(btn) {
        if (btn.getAttribute('data-sample-busy') !== '1') return;
        var label = btn.getAttribute('data-sample-label');
        if (label !== null) {
            if (btn.tagName === 'INPUT') btn.value = label;
            else btn.innerHTML = label;
        }
        var name = btn.getAttribute('data-sample-name');
        if (name !== null) {
            btn.setAttribute('name', name);
            btn.removeAttribute('data-sample-name');
            var keeps = btn.form
                ? btn.form.querySelectorAll('input[data-sample-keep="1"]') : [];
            for (var i = 0; i < keeps.length; i++) {
                if (keeps[i].name === name && keeps[i].parentNode) {
                    keeps[i].parentNode.removeChild(keeps[i]);
                }
            }
        }
        btn.removeAttribute('data-sample-busy');
        btn.removeAttribute('data-sample-label');
        btn.removeAttribute('aria-busy');
        btn.classList.remove('is-busy');
        btn.style.pointerEvents = '';
        btn.style.opacity = '';
    }

    function setHidden(form, name, value) {
        var input = form.querySelector('input[type="hidden"][name="' + name + '"]');
        if (!input) {
            input = document.createElement('input');
            input.type = 'hidden';
            input.name = name;
            form.appendChild(input);
        }
        input.value = value;
    }

    function newToken() {
        if (window.crypto && window.crypto.randomUUID) return window.crypto.randomUUID();
        return 'd' + Date.now().toString(16) + Math.random().toString(16).slice(2);
    }

    function readCookie(name) {
        var m = document.cookie.match('(?:^|; )' + name + '=([^;]*)');
        return m ? decodeURIComponent(m[1]) : null;
    }

    function expireCookie(name) {
        document.cookie = name + '=; Max-Age=0; path=/';
    }

    // Restore on back/forward (bfcache) navigation so a cached page isn't stuck.
    window.addEventListener('pageshow', function (e) {
        if (e.persisted) {
            var busy = document.querySelectorAll('[data-sample-busy="1"]');
            for (var i = 0; i < busy.length; i++) restore(busy[i]);
        }
    });
})();
