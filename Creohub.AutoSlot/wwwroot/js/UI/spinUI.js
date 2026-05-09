/* ═══════════════════════════════════════════════════════════════════
   AutoSlot — spinUI.js  (v2 redesign)
   Manages Settings tab: spin picker, grid, combo list, timeline, presets.
   Uses MAINDATA (global), winCombosMap, spinPlayModes for AE interop.
═══════════════════════════════════════════════════════════════════ */

/* ── Constants ───────────────────────────────────────────────── */
const TL_MAX = 6;
function snap(v) { return Math.round(v * 10) / 10; }

const COMBO_COLORS = ['#007BFF','#28a745','#fd7e14','#9c27b0','#e91e63','#00bcd4','#ffcc00','#ff4444'];

/* ── Global state ───────────────────────────────────────────── */
/** Currently active spin name */
var currentSpin    = null;
/** Set of "x,y" strings for new-combo selection */
var selectedCells  = new Set();
/** Symbol name chosen in picker */
var selectedSymbol = null;
/** rAF ID for preview animation */
var previewRaf     = null;
var previewT0      = null;

/** { [spinName]: [{winElement, winElements, startOffset, duration}] } */
var winCombosMap   = {};
/** { [spinName]: 'sequential'|'fixedEnd'|'loop' } */
var spinPlayModes  = {};

/* ── Data helpers ───────────────────────────────────────────── */
function getSpinCfg(name) {
    return MAINDATA && MAINDATA.Spins ? MAINDATA.Spins[name] : null;
}
function getAllSpins() {
    return MAINDATA && MAINDATA.Spins ? MAINDATA.Spins : {};
}
/** GIF URL for a symbol name */
function symUrl(name) {
    if (!MAINDATA || !MAINDATA.Elements) return '';
    var el = MAINDATA.Elements[name];
    if (!el || !el.filePath) return '';
    return '/autoslot/local-preview?path=' + encodeURIComponent(el.filePath);
}
/** Symbol list as [{name, file}] */
function getSymbolsList() {
    if (!MAINDATA || !MAINDATA.Elements) return [];
    return Object.values(MAINDATA.Elements).map(function(el) {
        return { name: el.name, file: symUrl(el.name) };
    });
}
/** Ensure combos for a spin have a duration field */
function ensureDuration(combos) {
    if (!combos) return [];
    combos.forEach(function(c) { if (c.duration == null) c.duration = 1.4; });
    return combos;
}

/* ── Compat: getActiveGrid (for legacy spinActions.js calls) ─── */
function getActiveGrid() {
    return {
        choosenGrid: document.getElementById('spin-grid'),
        selectedSpin: currentSpin
    };
}
/** getGrid: returns selected cells from the new grid */
function getGrid(/* unused */) {
    return [...selectedCells].map(function(k) { return k.split(',').map(Number); });
}

/* ── Status message ─────────────────────────────────────────── */
function statusMsg(el, text, color) {
    if (!el) return;
    el.textContent = text;
    el.style.color = color || '#28a745';
    clearTimeout(el._t);
    el._t = setTimeout(function() { el.textContent = ''; }, 2500);
}
function showComboStatus(spin, text) {
    statusMsg(document.getElementById('combo-status'), text);
}

/* ── Dirty tracking ─────────────────────────────────────────── */
function markDirty() {
    var btn = document.getElementById('btn-submit');
    if (btn) btn.classList.add('dirty');
}
function clearDirty() {
    var btn = document.getElementById('btn-submit');
    if (btn) btn.classList.remove('dirty');
}

/* ── Cell key ───────────────────────────────────────────────── */
function cellKey(x, y) { return x + ',' + y; }
function getCellCombo(spin, x, y) {
    var list = winCombosMap[spin] || [];
    for (var i = 0; i < list.length; i++) {
        var elems = list[i].winElements || [];
        for (var j = 0; j < elems.length; j++) {
            if (elems[j][0] === x && elems[j][1] === y)
                return { idx: i, combo: list[i] };
        }
    }
    return null;
}

/* ═══════════════════════════════════════════════════════════
   GIF helpers
═══════════════════════════════════════════════════════════ */
function freezeToCanvas(src, canvas, w, h) {
    var W = w || 52, H = h || 52;
    canvas.width = W; canvas.height = H;
    if (!src) return;
    var img = new Image();
    img.onload = function() {
        var iw = img.naturalWidth || W, ih = img.naturalHeight || H;
        var scale = Math.min(W / iw, H / ih);
        var dx = (W - iw * scale) / 2, dy = (H - ih * scale) / 2;
        var ctx = canvas.getContext('2d');
        ctx.clearRect(0, 0, W, H);
        try { ctx.drawImage(img, dx, dy, iw * scale, ih * scale); } catch(e) {}
    };
    img.onerror = function() {
        var ctx = canvas.getContext('2d');
        ctx.fillStyle = '#1a1a1a'; ctx.fillRect(0, 0, W, H);
    };
    img.src = src;
}

function addGifToCell(cell, gifPath) {
    var cv = document.createElement('canvas');
    cv.className = 'cell-static';
    cv.width = 64; cv.height = 64;
    freezeToCanvas(gifPath, cv, 64, 64);
    cell.appendChild(cv);

    var im = document.createElement('img');
    im.className = 'cell-gif';
    im.dataset.src = gifPath;
    im.src = gifPath;
    cell.appendChild(im);

    cell.addEventListener('mouseenter', function() {
        if (cell.classList.contains('preview-active')) return;
        setCellAnimated(cell, true);
        cell.classList.add('hover-active');
    });
    cell.addEventListener('mouseleave', function() {
        cell.classList.remove('hover-active');
    });
}

function setCellAnimated(cell, on) {
    var im = cell.querySelector('img.cell-gif');
    if (!im) return;
    if (on) { var s = im.dataset.src; im.src = ''; im.src = s; }
}

/* ═══════════════════════════════════════════════════════════
   Custom Spin Picker
═══════════════════════════════════════════════════════════ */
function renderSpinPicker() {
    var menu  = document.getElementById('spin-picker-menu');
    var label = document.getElementById('spin-picker-label');
    if (!menu || !label) return;
    menu.innerHTML = '';

    var spins = getAllSpins();
    Object.keys(spins).forEach(function(name) {
        var row = document.createElement('div');
        row.className = 'spin-picker-opt' + (name === currentSpin ? ' active' : '');

        var nm = document.createElement('span');
        nm.className = 'spin-picker-opt-name';
        nm.textContent = name;
        nm.addEventListener('click', function() {
            switchSpin(name);
            closePicker();
        });

        var rm = document.createElement('button');
        rm.className = 'spin-picker-opt-rm';
        rm.textContent = '×';
        rm.title = 'Remove spin';
        rm.addEventListener('click', function(e) {
            e.stopPropagation();
            if (Object.keys(getAllSpins()).length <= 1) return;
            // delegate to spinActions.js
            removeSpin(name);
        });

        row.appendChild(nm); row.appendChild(rm);
        menu.appendChild(row);
    });

    // ＋ Add Spin row
    var addRow = document.createElement('div');
    addRow.className = 'spin-picker-add';
    addRow.innerHTML = '<span>＋</span><span>Add Spin</span>';
    addRow.addEventListener('click', function() {
        addSpin();
        closePicker();
    });
    menu.appendChild(addRow);

    if (currentSpin) {
        label.textContent = currentSpin;
    } else {
        label.textContent = Object.keys(spins).length ? Object.keys(spins)[0] : '—';
    }
}

function switchSpin(name) {
    currentSpin = name;
    selectedCells = new Set();
    selectedSymbol = null;
    stopPreview();
    updateSelectionPanel();
    renderSpinPicker();
    loadSpinConfig(currentSpin);
    renderGrid(currentSpin);
    renderComboList(currentSpin);
    renderTimeline(currentSpin);
    renderAllPresets();
    document.getElementById('combo-status').textContent = '';
    // AE viewer: open the spin comp
    if (typeof csInterface !== 'undefined') {
        try {
            csInterface.evalScript('Helper.findCompByName("' + name + '", Helper.findFolderByName("Spins")).openInViewer().setActive();');
        } catch(e) {}
    }
}

function closePicker() {
    var p = document.getElementById('spin-picker');
    if (p) p.classList.remove('open');
}

/* Init picker toggle */
document.addEventListener('DOMContentLoaded', function() {
    var btn = document.getElementById('spin-picker-btn');
    if (btn) {
        btn.addEventListener('click', function(e) {
            e.stopPropagation();
            document.getElementById('spin-picker').classList.toggle('open');
        });
    }
    document.addEventListener('click', function() { closePicker(); });
});

/* ═══════════════════════════════════════════════════════════
   Spin config load / sync
═══════════════════════════════════════════════════════════ */
function loadSpinConfig(spin) {
    var s = getSpinCfg(spin);
    if (!s) return;
    var set = function(id, v) { var el = document.getElementById(id); if (el) el.value = v; };
    set('cfg-width',      s.width);
    set('cfg-height',     s.height);
    set('cfg-cols',       s.elsByWidth);
    set('cfg-rows',       s.elsByHeight);
    set('cfg-sph',        s.spacingHorizontal);
    set('cfg-spv',        s.spacingVertical);
    set('cfg-slow-win',   s.slowWin);
    set('cfg-slow-lines', s.slowLines);
    set('cfg-slow-main',  s.slowMain);
    var pmBtn = document.getElementById('play-mode-btn');
    if (pmBtn) pmBtn.textContent = spinPlayModes[spin] || 'sequential';
    var szLbl = document.getElementById('grid-size-label');
    if (szLbl) szLbl.textContent = (s.elsByWidth || '?') + '×' + (s.elsByHeight || '?');
}

function syncSpinConfig() {
    if (!currentSpin) return;
    var s = getSpinCfg(currentSpin);
    if (!s) return;
    var get = function(id, fallback) {
        var el = document.getElementById(id);
        return el ? (+el.value || fallback) : fallback;
    };
    s.width             = get('cfg-width',      s.width);
    s.height            = get('cfg-height',     s.height);
    s.elsByWidth        = get('cfg-cols',       s.elsByWidth);
    s.elsByHeight       = get('cfg-rows',       s.elsByHeight);
    s.spacingHorizontal = get('cfg-sph',        s.spacingHorizontal);
    s.spacingVertical   = get('cfg-spv',        s.spacingVertical);
    s.slowWin           = get('cfg-slow-win',   s.slowWin);
    s.slowLines         = get('cfg-slow-lines', s.slowLines);
    s.slowMain          = get('cfg-slow-main',  s.slowMain);
    var szLbl = document.getElementById('grid-size-label');
    if (szLbl) szLbl.textContent = s.elsByWidth + '×' + s.elsByHeight;
    selectedCells = new Set();
    renderGrid(currentSpin);
    renderComboList(currentSpin);
    markDirty();
}

/* getUpdatesOfSpin — used by main.js submit handler */
function getUpdatesOfSpin() {
    var spin = currentSpin;
    var prevData = getSpinCfg(spin);
    var result = {
        name:           spin,
        isConfigUpdate: false,
        isResizeUpdate: false,
        resizeData:     null,
        configData:     null
    };
    if (!prevData) return result;

    var get = function(id, fallback) {
        var el = document.getElementById(id);
        return el ? (+el.value || fallback) : fallback;
    };
    var width      = get('cfg-width',      prevData.width);
    var height     = get('cfg-height',     prevData.height);
    var cols       = get('cfg-cols',       prevData.elsByWidth);
    var rows       = get('cfg-rows',       prevData.elsByHeight);
    var spacingH   = get('cfg-sph',        prevData.spacingHorizontal);
    var spacingV   = get('cfg-spv',        prevData.spacingVertical);
    var slowWin    = get('cfg-slow-win',   prevData.slowWin);
    var slowLines  = get('cfg-slow-lines', prevData.slowLines);
    var slowMain   = get('cfg-slow-main',  prevData.slowMain);

    if (width !== prevData.width || height !== prevData.height ||
        cols  !== prevData.elsByWidth || rows !== prevData.elsByHeight) {
        result.isResizeUpdate = true;
        result.resizeData = new ResizeDataType(width, height, rows, cols);
    }
    if (spacingH !== prevData.spacingHorizontal || spacingV !== prevData.spacingVertical ||
        slowMain !== prevData.slowMain || slowLines !== prevData.slowLines || slowWin !== prevData.slowWin) {
        result.isConfigUpdate = true;
        result.configData = new ConfigDataType(spacingH, spacingV, slowMain, slowLines, slowWin);
    }
    return result;
}

/* ── Spin settings input listeners ─────────────────────────── */
document.addEventListener('DOMContentLoaded', function() {
    ['cfg-width','cfg-height','cfg-cols','cfg-rows','cfg-sph','cfg-spv',
     'cfg-slow-win','cfg-slow-lines','cfg-slow-main'].forEach(function(id) {
        var el = document.getElementById(id);
        if (el) el.addEventListener('change', function() { syncSpinConfig(); });
    });
});

/* ═══════════════════════════════════════════════════════════
   Play mode
═══════════════════════════════════════════════════════════ */
document.addEventListener('DOMContentLoaded', function() {
    var pmBtn = document.getElementById('play-mode-btn');
    if (!pmBtn) return;
    pmBtn.addEventListener('click', function(e) {
        e.stopPropagation();
        stopPreview();
        var modes = ['sequential', 'fixedEnd', 'loop'];
        var cur   = spinPlayModes[currentSpin] || 'sequential';
        var next  = modes[(modes.indexOf(cur) + 1) % modes.length];
        spinPlayModes[currentSpin] = next;
        pmBtn.textContent = next;
        applyPlayMode(currentSpin, next);
        renderComboList(currentSpin);
        // force-open timeline
        var tlBody = document.getElementById('tl-body');
        var tlHdr  = document.querySelector('[data-target="tl-body"]');
        if (tlBody && !tlBody.classList.contains('open')) {
            if (tlHdr) tlHdr.classList.add('open');
            tlBody.classList.add('open');
        }
        renderTimeline(currentSpin);
        statusMsg(document.getElementById('combo-status'), 'Mode: ' + next, '#007BFF');
    });
});

function applyPlayMode(spin, mode) {
    var list = winCombosMap[spin] || [];
    if (!list.length) return;

    if (mode === 'sequential' || mode === 'loop') {
        list.forEach(function(c) {
            if (c._origDuration != null) { c.duration = c._origDuration; delete c._origDuration; }
        });
        var t = 0;
        // Add a 0.1s gap between sequential combos so the first combo's
        // AE fade-out doesn't visually overlap the next combo's start.
        var GAP = (mode === 'sequential') ? 0.1 : 0;
        list.forEach(function(c) {
            c.startOffset = snap(Math.min(t, TL_MAX - 0.1));
            c.duration    = snap(Math.max(0.1, Math.min(c.duration || 1.4, TL_MAX - c.startOffset)));
            t = snap(c.startOffset + c.duration + GAP);
        });
    } else if (mode === 'fixedEnd') {
        list.forEach(function(c) {
            if (c._origDuration == null) c._origDuration = c.duration;
            c.duration = Math.round(Math.max(0.1, TL_MAX - c.startOffset) * 10) / 10;
        });
    }
}

/* ═══════════════════════════════════════════════════════════
   Grid render
═══════════════════════════════════════════════════════════ */
function renderGrid(spin) {
    var s    = getSpinCfg(spin);
    var grid = document.getElementById('spin-grid');
    if (!s || !grid) return;
    var cols = s.elsByWidth  || 5;
    var rows = s.elsByHeight || 3;
    var gap  = 3;
    var maxW = 300, maxH = 250;
    var cellSize = Math.max(18, Math.min(72, Math.floor(Math.min(
        (maxW - gap * (cols - 1)) / cols,
        (maxH - gap * (rows - 1)) / rows
    ))));
    grid.style.gridTemplateColumns = 'repeat(' + cols + ', ' + cellSize + 'px)';
    grid.style.gridTemplateRows    = 'repeat(' + rows + ', ' + cellSize + 'px)';
    grid.innerHTML = '';

    // y=0 is the BOTTOM row (matching AE), so render rows top-to-bottom visually
    // by iterating y from (rows-1) down to 0.
    for (var y = rows - 1; y >= 0; y--) {
        for (var x = 0; x < cols; x++) {
            var cell = document.createElement('div');
            cell.className  = 'grid-item';
            cell.dataset.x  = x;
            cell.dataset.y  = y;

            var ci = getCellCombo(spin, x, y);
            if (ci) {
                var color = COMBO_COLORS[ci.idx % COMBO_COLORS.length];
                cell.style.borderColor = color;
                cell.dataset.comboIdx  = ci.idx;
                var gif = symUrl(ci.combo.winElement);
                if (gif) addGifToCell(cell, gif);
            }
            if (selectedCells.has(cellKey(x, y))) cell.classList.add('selected');

            (function(cx, cy, c) {
                c.addEventListener('click', function() { onCellClick(spin, cx, cy, c); });
            })(x, y, cell);

            grid.appendChild(cell);
        }
    }
}

/* Legacy alias used by spinActions.js */
function createGrid(spinData) {
    if (spinData && spinData.name) {
        if (!winCombosMap[spinData.name])  winCombosMap[spinData.name]  = ensureDuration(spinData.winCombos || []);
        if (!spinPlayModes[spinData.name]) spinPlayModes[spinData.name] = 'sequential';
        if (!currentSpin) {
            currentSpin = spinData.name;
            renderSpinPicker();
            loadSpinConfig(currentSpin);
        }
    }
    return document.getElementById('spin-grid');
}

function onCellClick(spin, x, y, cell) {
    var key = cellKey(x, y);
    // If the cell is already in a combo and we have nothing selected yet,
    // clicking it highlights that combo. If we already have cells selected
    // (building a new combo), allow toggling the cell regardless.
    if (cell.dataset.comboIdx !== undefined && selectedCells.size === 0) {
        highlightCombo(spin, parseInt(cell.dataset.comboIdx));
        return;
    }
    if (selectedCells.has(key)) {
        selectedCells.delete(key);
        cell.classList.remove('selected');
    } else {
        selectedCells.add(key);
        cell.classList.add('selected');
    }
    updateSelectionPanel();
}

function highlightCombo(spin, idx) {
    selectedCells = new Set();
    document.querySelectorAll('.grid-item').forEach(function(c) { c.classList.remove('selected'); });
    var combo = (winCombosMap[spin] || [])[idx];
    if (!combo) return;
    (combo.winElements || []).forEach(function(pos) {
        var cell = document.querySelector('.grid-item[data-x="' + pos[0] + '"][data-y="' + pos[1] + '"]');
        if (cell) cell.classList.add('selected');
    });
    document.querySelectorAll('.combo-item').forEach(function(el, i) {
        el.style.background = (i === idx) ? '#454545' : '';
    });
}

function applySelectedToGrid() {
    selectedCells.forEach(function(k) {
        var parts = k.split(',');
        var x = Number(parts[0]), y = Number(parts[1]);
        var cell = document.querySelector('.grid-item[data-x="' + x + '"][data-y="' + y + '"]');
        if (cell && !cell.dataset.comboIdx) cell.classList.add('selected');
    });
}

/* ── refreshGridColors (legacy compat for old spinActions) ─── */
function refreshGridColors(choosenGrid, spinName) {
    renderGrid(spinName || currentSpin);
}

/* ═══════════════════════════════════════════════════════════
   Selection panel
═══════════════════════════════════════════════════════════ */
function updateSelectionPanel() {
    var panel    = document.getElementById('cell-sel-panel');
    var countEl  = document.getElementById('sel-count-txt');
    var confirmBtn = document.getElementById('btn-confirm-combo');
    if (!panel) return;
    var n = selectedCells.size;
    if (n > 0) {
        panel.classList.add('visible');
        if (countEl) countEl.textContent = n + ' cell' + (n !== 1 ? 's' : '');
    } else {
        panel.classList.remove('visible');
        selectedSymbol = null;
        var pickRow = document.getElementById('sym-pick-row');
        if (pickRow) pickRow.querySelectorAll('.sym-pick-item').forEach(function(x) { x.classList.remove('sel'); });
        if (confirmBtn) confirmBtn.disabled = true;
    }
}

/* ═══════════════════════════════════════════════════════════
   Symbol picker (in cell-sel-panel)
═══════════════════════════════════════════════════════════ */
function renderSymbolPicker() {
    var row = document.getElementById('sym-pick-row');
    if (!row) return;
    row.innerHTML = '';
    var syms = getSymbolsList();
    syms.forEach(function(s) {
        var item = document.createElement('div');
        item.className   = 'sym-pick-item';
        item.dataset.name = s.name;

        var cv = document.createElement('canvas');
        freezeToCanvas(s.file, cv, 42, 42);

        var im = document.createElement('img');
        im.dataset.src = s.file;
        im.src = s.file;
        im.style.cssText = 'display:none;width:42px;height:42px;object-fit:contain;';

        var lbl = document.createElement('div');
        lbl.className   = 'sym-pick-lbl';
        lbl.textContent = s.name;

        item.appendChild(cv); item.appendChild(im); item.appendChild(lbl);
        row.appendChild(item);

        item.addEventListener('mouseenter', function() {
            cv.style.display = 'none'; im.style.display = 'block';
            var src = im.dataset.src; im.src = ''; im.src = src;
        });
        item.addEventListener('mouseleave', function() {
            im.style.display = 'none'; cv.style.display = '';
        });
        item.addEventListener('click', function() {
            row.querySelectorAll('.sym-pick-item').forEach(function(x) { x.classList.remove('sel'); });
            item.classList.add('sel');
            selectedSymbol = s.name;
            var btn = document.getElementById('btn-confirm-combo');
            if (btn) btn.disabled = false;
        });
    });
    row.addEventListener('wheel', function(e) {
        e.preventDefault(); row.scrollLeft += e.deltaY + e.deltaX;
    }, { passive: false });
}

/* ═══════════════════════════════════════════════════════════
   Combo list
═══════════════════════════════════════════════════════════ */
function renderComboList(spin) {
    var list = winCombosMap[spin] || [];
    var el   = document.getElementById('combo-list');
    if (!el) return;
    if (!list.length) {
        el.innerHTML = '<div style="font-size:13px;color:#888;padding:6px 2px;font-family:FugarReg,sans-serif">No combos yet</div>';
        return;
    }
    el.innerHTML = list.map(function(c, i) {
        var color   = COMBO_COLORS[i % COMBO_COLORS.length];
        var gifPath = symUrl(c.winElement);
        var cells   = (c.winElements || []).map(function(p) { return '(' + p[0] + ',' + p[1] + ')'; }).join(' ');
        var dur     = c.duration != null ? c.duration : 1.4;
        return [
            '<div class="combo-item" data-combo-idx="' + i + '">',
            '<span class="combo-dot" style="background:' + color + '"></span>',
            gifPath ? '<img class="combo-thumb" src="' + gifPath + '" onerror="this.style.display=\'none\'">' : '',
            '<span class="combo-text" title="' + cells + '">' + c.winElement + ' (' + (c.winElements || []).length + ')</span>',
            '<div class="combo-offset-wrap">',
            '<span class="combo-offset" data-idx="' + i + '" data-field="startOffset" title="Start">' + c.startOffset + 's</span>',
            '<span class="combo-dur-arrow">&#8594;</span>',
            '<span class="combo-dur"    data-idx="' + i + '" data-field="duration"    title="Duration">' + dur + 's</span>',
            '</div>',
            '<button class="combo-remove" data-idx="' + i + '">×</button>',
            '</div>'
        ].join('');
    }).join('');

    el.querySelectorAll('.combo-text').forEach(function(t, i) {
        t.addEventListener('click', function() { highlightCombo(spin, i); });
    });
    el.querySelectorAll('.combo-remove').forEach(function(btn) {
        btn.addEventListener('click', function(e) {
            e.stopPropagation();
            removeCombo(spin, parseInt(btn.dataset.idx));
        });
    });
    el.querySelectorAll('.combo-offset, .combo-dur').forEach(function(span) {
        span.addEventListener('click', function() { startInlineEdit(span, spin); });
    });
}

function startInlineEdit(span, spin) {
    var idx   = parseInt(span.dataset.idx);
    var field = span.dataset.field;
    var combo = (winCombosMap[spin] || [])[idx];
    if (!combo) return;
    var cur = combo[field] != null ? combo[field] : 1.4;

    var inp = document.createElement('input');
    inp.type = 'number'; inp.step = '0.1'; inp.min = '0'; inp.max = String(TL_MAX);
    inp.className = 'combo-inline-input';
    inp.value = cur;
    span.replaceWith(inp);
    inp.focus(); inp.select();

    function commit() {
        var val = Math.round(Math.max(0, Math.min(TL_MAX, parseFloat(inp.value) || 0)) * 10) / 10;
        combo[field] = val;
        renderComboList(spin);
        renderTimeline(spin);
    }
    inp.addEventListener('blur', commit);
    inp.addEventListener('keydown', function(e) {
        if (e.key === 'Enter')  { inp.blur(); }
        if (e.key === 'Escape') { renderComboList(spin); }
    });
}

function removeCombo(spin, idx) {
    stopPreview();
    var combos = winCombosMap[spin] || [];
    combos.splice(idx, 1);
    winCombosMap[spin] = combos;
    renderGrid(spin);
    renderComboList(spin);
    renderTimeline(spin);
    markDirty();
    statusMsg(document.getElementById('combo-status'), 'Combo ' + (idx + 1) + ' removed', '#f44');
}

function syncComboOffset(spin, idx, offset) {
    var offEl = document.querySelector('.combo-item[data-combo-idx="' + idx + '"] .combo-offset');
    if (offEl) offEl.textContent = offset + 's';
    markDirty();
}
function syncComboDuration(spin, idx, dur) {
    var durEl = document.querySelector('.combo-item[data-combo-idx="' + idx + '"] .combo-dur');
    if (durEl) durEl.textContent = dur + 's';
    markDirty();
}

/* ── Legacy addCombo wrapper (for old spinActions compat) ─── */
function addCombo() {
    if (!currentSpin) return;
    if (!selectedCells.size) { showComboStatus(currentSpin, '⚠ Select cells on the grid'); return; }
    if (!selectedSymbol)     { showComboStatus(currentSpin, '⚠ Choose a symbol');          return; }
    var cells = [...selectedCells].map(function(k) { return k.split(',').map(Number); });

    // Strip the selected cells from any existing combos so getCellCombo
    // always resolves to the new combo (fixes visual re-render bug).
    if (!winCombosMap[currentSpin]) winCombosMap[currentSpin] = [];
    winCombosMap[currentSpin].forEach(function(existing) {
        existing.winElements = (existing.winElements || []).filter(function(pos) {
            return !selectedCells.has(cellKey(pos[0], pos[1]));
        });
    });
    // Drop combos that lost all their cells
    winCombosMap[currentSpin] = winCombosMap[currentSpin].filter(function(c) {
        return (c.winElements || []).length > 0;
    });

    var prevList = winCombosMap[currentSpin];
    var autoOffset = prevList.length
        ? snap((prevList[prevList.length-1].startOffset + (prevList[prevList.length-1].duration || 1.4)) + 0.1)
        : 0;
    winCombosMap[currentSpin].push({ winElement: selectedSymbol, winElements: cells, startOffset: autoOffset, duration: 1.4 });
    selectedCells = new Set(); selectedSymbol = null;
    renderGrid(currentSpin);
    renderComboList(currentSpin);
    renderTimeline(currentSpin);
    updateSelectionPanel();
    markDirty();
    statusMsg(document.getElementById('combo-status'), 'Combo ' + winCombosMap[currentSpin].length + ' added');
}

/* ═══════════════════════════════════════════════════════════
   Button handlers (cell-sel-panel, bottom row)
═══════════════════════════════════════════════════════════ */
document.addEventListener('DOMContentLoaded', function() {

    /* Unselect all */
    var unsBtn = document.getElementById('btn-unselect-cells');
    if (unsBtn) {
        unsBtn.addEventListener('click', function() {
            selectedCells = new Set();
            document.querySelectorAll('.grid-item.selected').forEach(function(c) { c.classList.remove('selected'); });
            updateSelectionPanel();
        });
    }

    /* Add combo confirm */
    var confBtn = document.getElementById('btn-confirm-combo');
    if (confBtn) {
        confBtn.addEventListener('click', function() {
            if (!selectedCells.size || !selectedSymbol) return;
            addCombo();
        });
    }

    /* Clear all combos */
    var clearBtn = document.getElementById('btn-clear-combos');
    if (clearBtn) {
        clearBtn.addEventListener('click', function() {
            if (!currentSpin) return;
            if (!(winCombosMap[currentSpin] || []).length) return;
            stopPreview();
            winCombosMap[currentSpin] = [];
            selectedCells = new Set();
            renderGrid(currentSpin);
            renderComboList(currentSpin);
            renderTimeline(currentSpin);
            updateSelectionPanel();
            markDirty();
            statusMsg(document.getElementById('combo-status'), 'Cleared', '#fd7e14');
        });
    }

    /* Collapsible sections */
    document.querySelectorAll('.collapsible-hdr').forEach(function(hdr) {
        hdr.addEventListener('click', function() {
            hdr.classList.toggle('open');
            var body = document.getElementById(hdr.dataset.target);
            if (!body) return;
            body.classList.toggle('open');
            if (hdr.dataset.target === 'tl-body' && body.classList.contains('open'))
                renderTimeline(currentSpin);
        });
    });

    /* Preview button */
    var preBtn = document.getElementById('btn-preview');
    if (preBtn) {
        preBtn.addEventListener('click', function(e) {
            e.stopPropagation();
            if (previewRaf) stopPreview(); else startPreview();
        });
    }

});

/* ═══════════════════════════════════════════════════════════
   Timeline render
═══════════════════════════════════════════════════════════ */
function renderTimeline(spin) {
    var wrap = document.getElementById('timeline-wrap');
    var list = winCombosMap[spin] || [];
    if (!wrap) return;
    wrap.innerHTML = '';
    ensureDuration(list);

    // Ruler
    var ruler = document.createElement('div');
    ruler.className = 'timeline-ruler';
    for (var t = 0; t <= TL_MAX; t++) {
        var m = document.createElement('span');
        m.className = 'timeline-mark';
        m.style.left = (t / TL_MAX * 100) + '%';
        m.textContent = t + 's';
        ruler.appendChild(m);
    }
    wrap.appendChild(ruler);

    if (!list.length) {
        var empty = document.createElement('div');
        empty.style.cssText = 'font-size:10px;color:#777;padding:4px;font-family:FugarReg,sans-serif';
        empty.textContent = 'No combos';
        wrap.appendChild(empty);
        return;
    }

    var dragLabel = document.createElement('div');
    dragLabel.className = 'tl-drag-label';
    wrap.appendChild(dragLabel);

    var tlCursor = document.createElement('div');
    tlCursor.className = 'tl-cursor'; tlCursor.id = 'tl-cursor';
    wrap.appendChild(tlCursor);

    var rowEls = [];

    list.forEach(function(combo, i) {
        var color = COMBO_COLORS[i % COMBO_COLORS.length];
        var row   = document.createElement('div');
        row.className = 'timeline-row';
        rowEls.push({ row: row, combo: combo, color: color });

        var block = document.createElement('div');
        block.className = 'timeline-block';
        block.style.background  = color + '44';
        block.style.borderColor = color;

        var txt = document.createElement('span');
        txt.style.cssText = 'overflow:hidden;text-overflow:ellipsis;flex:1;pointer-events:none';
        txt.textContent = combo.winElement;
        block.appendChild(txt);

        var handle = document.createElement('div');
        handle.className = 'tl-resize';
        block.appendChild(handle);

        row.appendChild(block);
        wrap.appendChild(row);

        function pct(v) { return (v / TL_MAX * 100) + '%'; }
        function updateBlock() {
            block.style.left  = pct(Math.max(0, combo.startOffset));
            block.style.width = pct(Math.max(0.05, combo.duration));
        }
        updateBlock();

        function showLabel(text, blockEl) {
            dragLabel.textContent   = text;
            dragLabel.style.display = 'block';
            dragLabel.style.left    = (blockEl.offsetLeft + blockEl.offsetWidth / 2) + 'px';
            dragLabel.style.top     = (row.offsetTop - 22) + 'px';
        }

        // Drag to move
        block.addEventListener('mousedown', function(e) {
            if (e.target === handle) return;
            e.preventDefault();
            var wrapW   = wrap.getBoundingClientRect().width;
            var startX  = e.clientX;
            var origOff = combo.startOffset;
            block.classList.add('dragging');

            function onMove(e) {
                var dT = ((e.clientX - startX) / wrapW) * TL_MAX;
                combo.startOffset = snap(Math.max(0, Math.min(TL_MAX - combo.duration, origOff + dT)));
                if (combo.startOffset + combo.duration > TL_MAX)
                    combo.startOffset = snap(TL_MAX - combo.duration);
                updateBlock();
                showLabel(combo.startOffset + 's — ' + snap(combo.startOffset + combo.duration) + 's', block);
                syncComboOffset(spin, i, combo.startOffset);
            }
            function onUp() {
                document.removeEventListener('mousemove', onMove);
                document.removeEventListener('mouseup', onUp);
                block.classList.remove('dragging');
                dragLabel.style.display = 'none';
            }
            document.addEventListener('mousemove', onMove);
            document.addEventListener('mouseup', onUp);
        });

        // Resize
        handle.addEventListener('mousedown', function(e) {
            e.preventDefault(); e.stopPropagation();
            var wrapW   = wrap.getBoundingClientRect().width;
            var startX  = e.clientX;
            var origDur = combo.duration;

            function onMove(e) {
                var dT = ((e.clientX - startX) / wrapW) * TL_MAX;
                combo.duration = snap(Math.max(0.1, Math.min(TL_MAX - combo.startOffset, origDur + dT)));
                if (combo.startOffset + combo.duration > TL_MAX)
                    combo.duration = snap(TL_MAX - combo.startOffset);
                updateBlock();
                showLabel(combo.startOffset + 's — ' + snap(combo.startOffset + combo.duration) + 's', block);
                syncComboDuration(spin, i, combo.duration);
            }
            function onUp() {
                document.removeEventListener('mousemove', onMove);
                document.removeEventListener('mouseup', onUp);
                dragLabel.style.display = 'none';
            }
            document.addEventListener('mousemove', onMove);
            document.addEventListener('mouseup', onUp);
        });
    });

    // Loop ghost blocks
    var mode = spinPlayModes[spin] || 'sequential';
    if (mode === 'loop' && list.length) {
        var loopDur = Math.max.apply(null, list.map(function(c) { return c.startOffset + (c.duration || 1.4); }));
        if (loopDur < TL_MAX) {
            var loopPct = (loopDur / TL_MAX) * 100;
            var loopMarker = document.createElement('div');
            loopMarker.style.cssText = 'position:absolute;top:14px;bottom:0;left:' + loopPct + '%;width:1px;pointer-events:none;z-index:4;background:repeating-linear-gradient(to bottom,#888 0,#888 4px,transparent 4px,transparent 8px);';
            var loopTick = document.createElement('span');
            loopTick.style.cssText = 'position:absolute;top:-14px;left:50%;transform:translateX(-50%);font-size:9px;color:#999;white-space:nowrap;font-family:FugarReg,sans-serif;';
            loopTick.textContent = '↻';
            loopMarker.appendChild(loopTick);
            wrap.appendChild(loopMarker);
        }
        rowEls.forEach(function(re) {
            var ghostStart = loopDur + re.combo.startOffset;
            if (ghostStart >= TL_MAX) return;
            var ghostW = Math.min(re.combo.duration || 1.4, TL_MAX - ghostStart);
            var ghost  = document.createElement('div');
            ghost.style.cssText = 'position:absolute;height:100%;box-sizing:border-box;left:' + (ghostStart / TL_MAX * 100) + '%;width:' + (ghostW / TL_MAX * 100) + '%;background:' + re.color + '1a;border:1px dashed ' + re.color + '88;border-radius:3px;pointer-events:none;font-size:8px;color:' + re.color + '99;display:flex;align-items:center;padding:0 4px;overflow:hidden;white-space:nowrap;font-family:FugarReg,sans-serif;';
            ghost.textContent = re.combo.winElement;
            re.row.appendChild(ghost);
        });
    }
}

/* ═══════════════════════════════════════════════════════════
   Preview animation
═══════════════════════════════════════════════════════════ */
function startPreview() {
    stopPreview();
    var spin = currentSpin;
    var list = winCombosMap[spin] || [];
    if (!list.length) {
        statusMsg(document.getElementById('combo-status'), 'No combos to preview', '#fd7e14');
        return;
    }
    var tlBody = document.getElementById('tl-body');
    var tlHdr  = document.querySelector('[data-target="tl-body"]');
    if (tlBody && !tlBody.classList.contains('open')) {
        if (tlHdr) tlHdr.classList.add('open');
        tlBody.classList.add('open');
    }
    renderTimeline(spin);

    var btn = document.getElementById('btn-preview');
    if (btn) { btn.textContent = '■ Stop'; btn.classList.add('playing'); }

    var mode    = spinPlayModes[spin] || 'sequential';
    var loopDur = Math.max.apply(null, list.map(function(c) { return c.startOffset + (c.duration || 1.4); }));
    previewT0   = performance.now();

    function tick(now) {
        var elapsed = (now - previewT0) / 1000;
        var t = (mode === 'loop') ? elapsed % loopDur : elapsed;
        var cur = document.getElementById('tl-cursor');
        if (cur) { cur.style.display = 'block'; cur.style.left = Math.min(t / TL_MAX * 100, 100) + '%'; }
        updateGridPreview(spin, t, list);
        if (mode !== 'loop' && elapsed >= TL_MAX) { stopPreview(); return; }
        previewRaf = requestAnimationFrame(tick);
    }
    previewRaf = requestAnimationFrame(tick);
}

function stopPreview() {
    if (previewRaf) { cancelAnimationFrame(previewRaf); previewRaf = null; }
    previewT0 = null;
    var btn = document.getElementById('btn-preview');
    if (btn) { btn.textContent = '▶ Play'; btn.classList.remove('playing'); }
    var cur = document.getElementById('tl-cursor');
    if (cur) cur.style.display = 'none';
    document.querySelectorAll('.grid-item.preview-active').forEach(function(cell) {
        cell.classList.remove('preview-active');
        cell.style.removeProperty('--cc');
        setCellAnimated(cell, false);
    });
}

function updateGridPreview(spin, t, list) {
    var grid = document.getElementById('spin-grid');
    if (!grid) return;
    grid.querySelectorAll('.grid-item[data-combo-idx]').forEach(function(cell) {
        var i = parseInt(cell.dataset.comboIdx);
        var c = list[i];
        if (!c) return;
        var active    = t >= c.startOffset && t < c.startOffset + (c.duration || 1.4);
        var wasActive = cell.classList.contains('preview-active');
        if (active && !wasActive) {
            cell.style.setProperty('--cc', COMBO_COLORS[i % COMBO_COLORS.length]);
            cell.classList.add('preview-active');
            setCellAnimated(cell, true);
        } else if (!active && wasActive) {
            cell.classList.remove('preview-active');
            cell.style.removeProperty('--cc');
            setCellAnimated(cell, false);
        }
    });
}

/* ═══════════════════════════════════════════════════════════
   Compat helpers for old spinUI calls
═══════════════════════════════════════════════════════════ */
function createOrUpdateDropdownSpinItems() { renderSpinPicker(); }
function createOrUpdateDropdownWinItems()  { renderSymbolPicker(); }
function setSpinData(allData)              { /* handled by loadSpinConfig */ }
function previewCombo(spin, idx)           { highlightCombo(spin, idx); }

/* getActiveCombos / setActiveCombos (legacy compat) */
function getActiveCombos() { return winCombosMap[currentSpin] || []; }
function setActiveCombos(spinName, combos) { winCombosMap[spinName] = combos; }
