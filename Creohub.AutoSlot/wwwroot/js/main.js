/* ═══════════════════════════════════════════════════════════════════
   AutoSlot — main.js  (v2 redesign)
   Entry point: loads AE scripts, runs Analyze, inits UI.
═══════════════════════════════════════════════════════════════════ */

const dev = false;

var MAINDATA = new AutoSlotDataType(
    "AutoSlot",
    {
        "scatter_symbol": new ElementDataType("scatter_symbol", 500, 500),
        "wild_symbol":    new ElementDataType("wild_symbol",    500, 500),
        "simple_Symbol01":new ElementDataType("simple_Symbol01",500, 500),
    },
    {
        // name, width, height, elsByHeight, elsByWidth, spacingH, spacingV, slowWin, slowLines, slowMain
        "Spin_1": new SpinDataType("Spin_1", 1080, 1080, 5, 5, 120, 120, 0.1, 0.1, 0.1),
    }
);

const adobeMiddleWare_ = new iframeMiddleWare("AutoSlot_");

/* ── Load ExtendScript files ────────────────────────────────── */
async function loadJsxScripts() {
    const map = {
        "localization":  "common/localization.jsx",
        "helpers":       "helpers.jsx",
        "SpinDataType":  "types/SpinDataType.jsx",
        "ElementDataType":"types/ElementDataType.jsx",
        "ResizeDataType":"types/ResizeDataType.jsx",
        "ConfigDataType":"types/ConfigDataType.jsx",
        "WinDataType":   "types/WinDataType.jsx",
        "Expressions":   "Expressions.jsx",
        "Line":          "Line.jsx",
        "Spin":          "Spin.jsx",
        "AutoSlot":      "AutoSlot.jsx",
    };
    const promises = Object.entries(map).map(async ([key, filePath]) => {
        try   { return await fetchJSX(filePath); }
        catch (e) { console.error(e); return ""; }
    });
    await Promise.all(promises);
}

/* ── Tab switching (data-tab attribute) ─────────────────────── */
document.addEventListener('DOMContentLoaded', function() {
    document.querySelectorAll('.tab').forEach(function(t) {
        t.addEventListener('click', function() {
            document.querySelectorAll('.tab').forEach(function(x) { x.classList.remove('active'); });
            document.querySelectorAll('.content').forEach(function(x) { x.classList.remove('active'); });
            t.classList.add('active');
            var panel = document.getElementById('tab-' + t.dataset.tab);
            if (panel) panel.classList.add('active');
        });
    });
});

/* ── DOMContentLoaded: full init ────────────────────────────── */
document.addEventListener('DOMContentLoaded', async function() {
    // Show spinner
    var spinner = document.getElementById('loading-spinner');
    if (spinner) spinner.classList.add('visible');

    // Init edu toggle state from localStorage
    var eduTog = document.getElementById('edu-toggle');
    if (eduTog) eduTog.classList.toggle('on', localStorage.getItem('eduMode') !== 'false');

    try {
        await loadJsxScripts();

        try { await adobeMiddleWare_.Analyze(); } catch(e) { console.error(e); }

        // Init win-combos and play modes for each spin
        Object.keys(MAINDATA.Spins || {}).forEach(function(name) {
            if (!winCombosMap[name]) {
                var spinData = MAINDATA.Spins[name];
                winCombosMap[name] = ensureDuration((spinData.winCombos || []).map(function(c) {
                    return { winElement: c.winElement || '', winElements: c.winElements || [], startOffset: c.startOffset || 0, duration: c.duration || 1.4 };
                }));
            }
            if (!spinPlayModes[name]) spinPlayModes[name] = 'sequential';
        });

        // Set first spin as current
        var spinNames = Object.keys(MAINDATA.Spins || {});
        if (spinNames.length) {
            currentSpin = spinNames[0];
        }

        // Build UI
        renderSpinPicker();
        loadSpinConfig(currentSpin);
        renderGrid(currentSpin);
        renderComboList(currentSpin);
        renderAllPresets();
        initializeSymbolsScroll();
        loadSymbolThumbnails();
        renderSymbolPicker();
        checkSubExpiry();

        // Tooltip hint on symbols scroll
        if (typeof showTooltip === 'function') {
            showTooltip(
                document.getElementById('symbolsScroll'),
                'Добавьте ваши элементы!\nAutoSlot/Symbols/Gifs',
                null,
                'right'
            );
        }

    } catch(error) {
        console.error('Ошибка загрузки:', error);
    } finally {
        if (spinner) {
            spinner.style.transition = 'opacity .4s';
            spinner.style.opacity = '0';
            setTimeout(function() { spinner.style.display = 'none'; spinner.style.opacity = ''; spinner.classList.remove('visible'); }, 400);
        }
    }
});

/* ── Submit button ──────────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', function() {
    var submitBtn = document.getElementById('btn-submit');
    if (!submitBtn) return;
    submitBtn.addEventListener('click', async function() {
        if (!currentSpin) return;
        var d = getUpdatesOfSpin();
        console.log('[Submit]', JSON.stringify(d, null, 2));

        // Send win combos to AE
        var activeCombos = winCombosMap[currentSpin] || [];
        console.log('[Submit] spin:', currentSpin, '| combos:', activeCombos.length);
        if (activeCombos.length > 0) {
            var playMode = spinPlayModes[currentSpin] || 'fixedEnd';
            try {
                const winResult = await adobeMiddleWare_.setWinGridMulti(currentSpin, activeCombos, playMode);
                console.log('[setWinGridMulti result]:', winResult);
            } catch(e) {
                console.error('[setWinGridMulti error]:', e);
            }
        } else {
            console.warn('[Submit] activeCombos is EMPTY — setWinGridMulti not called');
        }

        if (d.isResizeUpdate) {
            adobeMiddleWare_.resize(d.name, d.resizeData);
        }
        if (d.isConfigUpdate) {
            adobeMiddleWare_.setConfigSpin(d.name, d.configData);
        }

        await adobeMiddleWare_.Analyze();

        // Refresh UI after Analyze
        var savedCombos = (winCombosMap[currentSpin] || []).slice();
        renderGrid(currentSpin);
        renderComboList(currentSpin);
        winCombosMap[currentSpin] = savedCombos;
        renderComboList(currentSpin);
        clearDirty();
        statusMsg(document.getElementById('combo-status'),
            currentSpin + ': ' + activeCombos.length + ' combo(s) → AE');
    });
});

/* ── Update button (re-Analyze from AE) ────────────────────── */
document.addEventListener('DOMContentLoaded', function() {
    var updBtn = document.getElementById('btn-update');
    if (!updBtn) return;
    updBtn.addEventListener('click', async function() {
        statusMsg(document.getElementById('combo-status'), 'Refreshing…', '#888');
        try {
            await adobeMiddleWare_.Analyze();
            // Re-init spin list
            var spinNames = Object.keys(MAINDATA.Spins || {});
            spinNames.forEach(function(name) {
                if (!winCombosMap[name])   winCombosMap[name]   = [];
                if (!spinPlayModes[name])  spinPlayModes[name]  = 'sequential';
            });
            if (!currentSpin || !MAINDATA.Spins[currentSpin]) {
                currentSpin = spinNames[0] || null;
            }
            renderSpinPicker();
            loadSpinConfig(currentSpin);
            renderGrid(currentSpin);
            renderComboList(currentSpin);
            initializeSymbolsScroll();
            loadSymbolThumbnails();
            renderSymbolPicker();
            renderAllPresets();
            statusMsg(document.getElementById('combo-status'), 'Refreshed from AE', '#28a745');
        } catch(e) {
            console.error(e);
            statusMsg(document.getElementById('combo-status'), 'Refresh failed', '#f44');
        }
    });
});

/* ── Profile tab: subscription status ──────────────────────── */
document.addEventListener('DOMContentLoaded', function() {
    loadProfileData();

    // Edu toggle
    var eduRow = document.getElementById('edu-row');
    var eduTog = document.getElementById('edu-toggle');
    if (eduRow && eduTog) {
        var eduOn = localStorage.getItem('eduMode') !== 'false';
        eduTog.classList.toggle('on', eduOn);
        eduRow.addEventListener('click', function() {
            eduOn = !eduOn;
            localStorage.setItem('eduMode', eduOn);
            eduTog.classList.toggle('on', eduOn);
            if (typeof setEduMode === 'function') setEduMode(eduOn);
        });
    }

    // Promo code
    var promoBtn = document.getElementById('btn-promo');
    if (promoBtn) {
        promoBtn.addEventListener('click', function() { redeemPromo(); });
    }

    // Renew
    var renewBtn = document.getElementById('btn-renew');
    if (renewBtn) {
        renewBtn.addEventListener('click', function() {
            if (typeof openURL === 'function') openURL('https://creohub.xyz');
        });
    }
});

async function loadProfileData() {
    var statusEl = document.getElementById('sub-text');
    var expiryEl = document.getElementById('sub-expiry');
    var dotEl    = document.getElementById('sub-dot');
    var badgeEl  = document.getElementById('sub-badge');
    try {
        const res  = await fetch('/autoslot/me');
        const data = await res.json();
        if (data.hasSubscription) {
            if (statusEl) statusEl.textContent = 'Лицензия активна';
            if (dotEl)   { dotEl.className = 'sub-dot active'; }
            if (badgeEl) { badgeEl.className = 'sub-badge active'; }

            if (data.isLifetime || !data.expiresAt) {
                // Пожизненная — без даты и счётчика дней
                if (expiryEl) expiryEl.textContent = 'навсегда';
            } else {
                var exp = new Date(data.expiresAt).toLocaleDateString('ru-RU', { day:'2-digit', month:'2-digit', year:'numeric' });
                if (expiryEl) expiryEl.textContent = 'до ' + exp + ' · ' + data.daysLeft + ' дней';
                // Expiry warning
                if (data.daysLeft <= 5 && data.daysLeft > 0) {
                    var warn = document.getElementById('sub-expiry-warn');
                    var daysEl = document.getElementById('sub-days-left');
                    if (daysEl) daysEl.textContent = data.daysLeft;
                    if (warn)   warn.style.display = 'inline-flex';
                }
            }
        } else {
            if (statusEl) statusEl.textContent = 'Подписка не активна';
            if (expiryEl) expiryEl.textContent = '';
            if (dotEl)   { dotEl.className = 'sub-dot inactive'; }
            if (badgeEl) { badgeEl.className = 'sub-badge inactive'; }
        }
    } catch(e) {
        if (statusEl) statusEl.textContent = 'Ошибка загрузки';
    }
}

async function redeemPromo() {
    var code     = (document.getElementById('promo-input') || {}).value;
    var statusEl = document.getElementById('promo-status');
    if (!code || !code.trim()) return;
    code = code.trim();
    if (statusEl) { statusEl.style.color = '#bbb'; statusEl.textContent = 'Проверяем...'; }
    try {
        const res = await fetch('/autoslot/redeem', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ code })
        });
        if (res.ok) {
            if (statusEl) { statusEl.style.color = '#4c4'; statusEl.textContent = '✅ Активировано!'; }
            loadProfileData();
        } else {
            if (statusEl) { statusEl.style.color = '#f44'; statusEl.textContent = 'Код недействителен'; }
        }
    } catch(e) {
        if (statusEl) { statusEl.style.color = '#f44'; statusEl.textContent = 'Ошибка сети'; }
    }
}

function checkSubExpiry() {
    // Called after profile data loaded; expiry warning handled there.
    // Kept as no-op since loadProfileData handles it.
}

/* ensureDuration is defined in spinUI.js */
