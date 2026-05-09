/* ═══════════════════════════════════════════════════════════════════
   AutoSlot — elementsUI.js  (v2 redesign)
   Manages Main tab: symbols viewer + size inputs.
═══════════════════════════════════════════════════════════════════ */

var elementsWidth  = 250;
var elementsHeight = 250;

/**
 * Builds the symbols grid in #symbolsScroll from MAINDATA.Elements.
 * Each item shows a canvas (static first GIF frame) + name label.
 * On hover, swaps to animated GIF and back.
 * On click, opens the precomp in AE viewer.
 */
function initializeSymbolsScroll() {
    var scroll = document.getElementById('symbolsScroll');
    if (!scroll) return;
    scroll.innerHTML = '';

    var names = MAINDATA && MAINDATA.Elements ? Object.keys(MAINDATA.Elements) : [];
    var countEl = document.getElementById('sym-count');
    if (countEl) countEl.textContent = names.length;

    if (!names.length) {
        scroll.innerHTML = [
            '<div style="width:100%;height:100%;display:flex;align-items:center;justify-content:center;pointer-events:none;">',
            '<div style="text-align:center;font-family:FugarReg,sans-serif;color:#555;line-height:1.6;font-size:13px;">',
            'Загрузите ваши символы в<br>',
            '<span style="color:#777;font-family:Fugar,sans-serif;">AutoSlot / Symbols / Gifs</span>',
            '</div></div>'
        ].join('');
        return;
    }

    names.forEach(function(name) {
        var item = document.createElement('div');
        item.className      = 'scroll-item';
        item.title          = name;
        item.dataset.symName = name;

        // Canvas: static first frame (filled later by loadSymbolThumbnails)
        var cv = document.createElement('canvas');
        cv.className = 'symbol-thumb';
        cv.width = 52; cv.height = 52;
        cv.dataset.symName = name;

        // Animated GIF (hidden by default)
        var im = document.createElement('img');
        im.className         = 'symbol-thumb symbol-thumb-gif';
        im.alt               = name;
        im.dataset.symName   = name;
        im.style.display     = 'none';

        var lbl = document.createElement('div');
        lbl.className   = 'symbol-label';
        lbl.textContent = name;

        item.appendChild(cv);
        item.appendChild(im);
        item.appendChild(lbl);
        scroll.appendChild(item);

        // Hover: swap canvas ↔ GIF
        item.addEventListener('mouseenter', function() {
            var src = im.src || im.dataset.gifSrc;
            if (src) { im.src = ''; im.src = src; }
            cv.style.display = 'none';
            im.style.display = 'block';
        });
        item.addEventListener('mouseleave', function() {
            im.style.display = 'none';
            cv.style.display = 'block';
        });

        // Click: open precomp in AE viewer
        item.addEventListener('click', function() {
            scroll.querySelectorAll('.scroll-item').forEach(function(x) { x.classList.remove('active'); });
            item.classList.add('active');
            if (typeof csInterface !== 'undefined') {
                try {
                    csInterface.evalScript(
                        'Helper.findCompByName("' + name + '", Helper.findFolderByName("SymbolComps")).openInViewer().setActive();'
                    );
                } catch(e) {}
            }
        });
    });
}

/**
 * Loads GIF previews via /autoslot/local-preview?path=...
 * Draws first frame into canvas, stores src in img for hover animation.
 */
function loadSymbolThumbnails() {
    if (!MAINDATA || !MAINDATA.Elements) return;
    Object.keys(MAINDATA.Elements).forEach(function(name) {
        var el = MAINDATA.Elements[name];
        if (!el || !el.filePath) return;
        var url = '/autoslot/local-preview?path=' + encodeURIComponent(el.filePath);

        // Update animated img
        var im = document.querySelector('img.symbol-thumb[data-sym-name="' + name + '"]');
        if (im) { im.src = url; im.dataset.gifSrc = url; }

        // Draw static canvas
        var cv = document.querySelector('canvas.symbol-thumb[data-sym-name="' + name + '"]');
        if (cv && typeof freezeToCanvas === 'function') {
            freezeToCanvas(url, cv, 52, 52);
        } else if (cv) {
            // fallback: just draw via img
            var tmp = new Image();
            tmp.onload = function() {
                cv.width = 52; cv.height = 52;
                try { cv.getContext('2d').drawImage(tmp, 0, 0, 52, 52); } catch(e) {}
            };
            tmp.src = url;
        }
    });

    // Also update symbol picker canvases in Settings tab
    if (typeof renderSymbolPicker === 'function') renderSymbolPicker();
}

/* ── Size inputs ──────────────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', function() {
    var wInput = document.getElementById('elementsWidth');
    var hInput = document.getElementById('elementsHeight');

    if (wInput) {
        elementsWidth = Number(wInput.value) || 250;
        wInput.addEventListener('change', function() {
            var value = Number(this.value);
            if (value && value !== elementsWidth) {
                elementsWidth = value;
                resizeElements();
            }
        });
    }
    if (hInput) {
        elementsHeight = Number(hInput.value) || 250;
        hInput.addEventListener('change', function() {
            var value = Number(this.value);
            if (value && value !== elementsHeight) {
                elementsHeight = value;
                resizeElements();
            }
        });
    }

    /* Promo strip click */
    var strip = document.getElementById('main-promo-strip');
    if (strip) {
        strip.addEventListener('click', function() {
            if (typeof openURL === 'function') openURL('https://creohub.xyz');
        });
    }
});
