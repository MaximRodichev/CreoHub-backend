/**
 * Создание нового спина, работа с middleWare и UI (v2 redesign)
 */
async function addSpin(){
    if(Object.keys(MAINDATA.Elements).length > 0){
        if(Object.keys(MAINDATA.Spins).length > 0){
            await copySpin();
        }
        else{
            await adobeMiddleWare_.addSpin();
        }
        await adobeMiddleWare_.Analyze();

        // Init new spin(s) in winCombosMap / spinPlayModes
        Object.keys(MAINDATA.Spins || {}).forEach(function(name) {
            if (!winCombosMap[name])   winCombosMap[name]   = [];
            if (!spinPlayModes[name])  spinPlayModes[name]  = 'sequential';
        });

        // Switch to last spin
        var spinNames = Object.keys(MAINDATA.Spins || {});
        if (spinNames.length) {
            var lastSpin = spinNames[spinNames.length - 1];
            if (typeof switchSpin === 'function') {
                switchSpin(lastSpin);
            } else {
                currentSpin = lastSpin;
                renderSpinPicker();
                loadSpinConfig(currentSpin);
                renderGrid(currentSpin);
            }
        }
    }
    else{
        CEPException.SymbolCompsNotFound();
    }
}

async function copySpin(){
    let lastspin = MAINDATA.Spins["Spin_"+(Object.keys(MAINDATA.Spins).length-1)];
    await adobeMiddleWare_.copySpin(lastspin);
}

/**
 * removeSpin — accepts optional spinName (from new picker).
 * If no name given, removes currentSpin.
 */
async function removeSpin(spinName){
    var name = spinName || currentSpin;
    if (!name || !MAINDATA.Spins[name]) {
        CEPException.NoFoundSpinToRemove();
        return;
    }
    if(Object.keys(MAINDATA.Spins).length <= 1) {
        CEPException.NoFoundSpinToRemove();
        return;
    }

    await adobeMiddleWare_.removeSpin(name);
    await adobeMiddleWare_.Analyze();

    // Clean up state
    delete winCombosMap[name];
    delete spinPlayModes[name];

    // Switch to another spin
    var remaining = Object.keys(MAINDATA.Spins || {});
    var nextSpin  = remaining.length ? remaining[0] : null;
    if (typeof switchSpin === 'function' && nextSpin) {
        switchSpin(nextSpin);
    } else {
        currentSpin = nextSpin;
        renderSpinPicker();
        if (currentSpin) {
            loadSpinConfig(currentSpin);
            renderGrid(currentSpin);
            renderComboList(currentSpin);
        }
    }
}
