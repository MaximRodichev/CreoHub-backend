/* ═══════════════════════════════════════════════════════════════════
   AutoSlot — presetsUI.js  (v2 redesign)
   Win-combo quick presets: built-in pills + custom (localStorage).
   Renders into #preset-quick-row in Settings tab.
   Save-preset input lives in #cell-sel-panel (#btn-save-preset).
═══════════════════════════════════════════════════════════════════ */

/* ── Custom presets state ───────────────────────────────────── */
var customPresets = JSON.parse(localStorage.getItem('autoSlot_winPresets') || '[]');

/* ── Built-in generators ────────────────────────────────────── */
var BUILTIN_PRESETS = [
    { name:'Bottom',  fn:function(c,r){ return Array.from({length:c},function(_,x){ return [x,r-1]; }); } },
    { name:'Middle',  fn:function(c,r){ return Array.from({length:c},function(_,x){ return [x,Math.floor(r/2)]; }); } },
    { name:'Top',     fn:function(c,r){ return Array.from({length:c},function(_,x){ return [x,0]; }); } },
    { name:'↗ Diag', fn:function(c,r){ return Array.from({length:Math.min(c,r)},function(_,i){ return [i,r-1-i]; }); } },
    { name:'↘ Diag', fn:function(c,r){ return Array.from({length:Math.min(c,r)},function(_,i){ return [i,i]; }); } },
    { name:'V',       fn:function(c,r){ var h=Math.floor(c/2); return Array.from({length:r},function(_,y){ return [y<Math.floor(r/2)?h-y:h+(y-Math.floor(r/2)),y]; }); } },
    { name:'∧',  fn:function(c,r){ var h=Math.floor(c/2); return Array.from({length:r},function(_,y){ return [y<Math.floor(r/2)?h+y:h+(Math.floor(r/2)-y+(r-1-y-Math.floor(r/2))),y]; }).filter(function(p){ return p[0]>=0&&p[0]<c; }); } },
    { name:'Zigzag',  fn:function(c,r){ return Array.from({length:c},function(_,x){ return [x,x%2===0?0:r-1]; }); } },
    { name:'Col L',   fn:function(c,r){ return Array.from({length:r},function(_,y){ return [0,y]; }); } },
    { name:'Col R',   fn:function(c,r){ return Array.from({length:r},function(_,y){ return [c-1,y]; }); } },
    { name:'Cross H', fn:function(c,r){ return Array.from({length:c},function(_,x){ return [x,Math.floor(r/2)]; }); } },
    { name:'All',     fn:function(c,r){ return Array.from({length:c*r},function(_,i){ return [i%c,Math.floor(i/c)]; }); } },
];

/* ── Helpers ────────────────────────────────────────────────── */
function getActiveSpinSize() {
    if (!currentSpin || !MAINDATA || !MAINDATA.Spins || !MAINDATA.Spins[currentSpin])
        return { cols: 5, rows: 6 };
    var s = MAINDATA.Spins[currentSpin];
    return { cols: s.elsByWidth || 5, rows: s.elsByHeight || 6 };
}

function applyPresetCells(cells) {
    if (!cells || !cells.length) return;
    var size = getActiveSpinSize();
    var valid = cells.filter(function(p) {
        return p[0] >= 0 && p[0] < size.cols && p[1] >= 0 && p[1] < size.rows;
    });
    selectedCells = new Set(valid.map(function(p) { return cellKey(p[0], p[1]); }));
    if (typeof renderGrid === 'function')          renderGrid(currentSpin);
    if (typeof applySelectedToGrid === 'function') applySelectedToGrid();
    if (typeof updateSelectionPanel === 'function') updateSelectionPanel();
}

/* ── Render all presets (custom + built-in) ─────────────────── */
function renderAllPresets() {
    var el = document.getElementById('preset-quick-row');
    if (!el) return;

    var customHTML = customPresets.map(function(p, i) {
        return '<button class="pq-btn pq-custom" data-custom-idx="' + i + '">' + p.name + ' <span class="pq-rm">\xd7</span></button>';
    }).join('');

    var builtinHTML = BUILTIN_PRESETS.map(function(p, i) {
        return '<button class="pq-btn" data-preset-idx="' + i + '">' + p.name + '</button>';
    }).join('');

    el.innerHTML = customHTML + builtinHTML;

    // Custom: click applies, × removes
    el.querySelectorAll('.pq-custom').forEach(function(btn) {
        btn.addEventListener('click', function(e) {
            if (e.target.classList.contains('pq-rm')) {
                var idx = parseInt(btn.dataset.customIdx);
                customPresets.splice(idx, 1);
                localStorage.setItem('autoSlot_winPresets', JSON.stringify(customPresets));
                renderAllPresets();
                return;
            }
            el.querySelectorAll('.pq-btn').forEach(function(b) { b.classList.remove('applied'); });
            btn.classList.add('applied');
            var idx = parseInt(btn.dataset.customIdx);
            applyPresetCells(customPresets[idx].cells);
        });
    });

    // Built-in: click applies pattern
    el.querySelectorAll('.pq-btn:not(.pq-custom)').forEach(function(btn) {
        btn.addEventListener('click', function() {
            el.querySelectorAll('.pq-btn').forEach(function(b) { b.classList.remove('applied'); });
            btn.classList.add('applied');
            var size = getActiveSpinSize();
            var cells = BUILTIN_PRESETS[parseInt(btn.dataset.presetIdx)]
                .fn(size.cols, size.rows)
                .filter(function(p) { return p[0]>=0&&p[0]<size.cols&&p[1]>=0&&p[1]<size.rows; });
            applyPresetCells(cells);
        });
    });
}

/* Aliases for legacy calls */
function renderPresetsPanel()   { renderAllPresets(); }
function renderCustomPresets()  { renderAllPresets(); }
function renderBuiltinPresets() { renderAllPresets(); }

/* ── Save preset handler ────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', function() {
    var saveBtn = document.getElementById('btn-save-preset');
    var nameInp = document.getElementById('preset-name-input');
    if (!saveBtn || !nameInp) return;

    saveBtn.addEventListener('click', function() {
        var name = nameInp.value.trim();
        if (!name) {
            if (typeof statusMsg === 'function')
                statusMsg(document.getElementById('combo-status'), 'Enter preset name', '#fd7e14');
            return;
        }
        if (!selectedCells || !selectedCells.size) {
            if (typeof statusMsg === 'function')
                statusMsg(document.getElementById('combo-status'), 'Select cells first', '#fd7e14');
            return;
        }
        var cells = Array.from(selectedCells).map(function(k) { return k.split(',').map(Number); });
        customPresets.push({ name: name, cells: cells });
        localStorage.setItem('autoSlot_winPresets', JSON.stringify(customPresets));
        nameInp.value = '';
        renderAllPresets();
        if (typeof statusMsg === 'function')
            statusMsg(document.getElementById('combo-status'), 'Preset "' + name + '" saved', '#28a745');
    });

    /* Wheel scroll on preset row */
    var pqRow = document.getElementById('preset-quick-row');
    if (pqRow) {
        pqRow.addEventListener('wheel', function(e) {
            e.preventDefault();
            pqRow.scrollLeft += e.deltaY + e.deltaX;
        }, { passive: false });
    }
});
