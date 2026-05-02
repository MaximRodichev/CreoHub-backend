function setSpinData(allData){
    sampleData = Mapper.SpinDataMapping(allData)

    spinSetter = [
        document.getElementById('spinWidth').children,
        document.getElementById('spinHeight').children,
        document.getElementById('spinSlow').children
    ]
    spinSetter.forEach(container=>
        Array.from(container).forEach(child=>
            {
                if(child instanceof HTMLInputElement){
                    child.value = sampleData[child.dataset.marker]
                }
            })
    )
}
function getSpinData(){

    const returnData = {
        
    }
    const spinSetter = [
        document.getElementById('spinWidth').children,
        document.getElementById('spinHeight').children,
        document.getElementById('spinSlow').children
    ]

    spinSetter.forEach(container=>
        Array.from(container).forEach(child=>
            {
                if(child instanceof HTMLInputElement){
                    returnData[child.dataset.marker] = Number(child.value)
                }
            })
    )
    return returnData;
}
function createOrUpdateDropdownWinItems(){
    const data = Object.keys(MAINDATA.Elements)
    const select = document.createElement('select');
    select.id = "symbol-dropdown";

    // Create the default disabled option
    const defaultOption = document.createElement('option');
    defaultOption.value = "";
    defaultOption.disabled = true;
    defaultOption.selected = true;
    defaultOption.textContent = "winElement select";
    select.appendChild(defaultOption);

    // Dynamically create <option> elements from the array
    data.forEach((item, index) => {
        const option = document.createElement('option');
        option.value = `${item}`; // Assign value like option1, option2, etc.
        option.textContent = item; // The visible text for the option
        select.appendChild(option);
    });

    // Проверяем наличие контейнера
    const container = document.getElementById('dropdown-container');
    if (container) {
        // Если внутри контейнера уже есть <select>, удаляем его
        const existingSelect = container.querySelector('select');
        if (existingSelect) {
            container.removeChild(existingSelect);
        }

        // Добавляем новый <select> в контейнер
        container.appendChild(select);
    }
}
function createOrUpdateDropdownSpinItems(){
    const data = Object.keys(MAINDATA.Spins)
    if(document.getElementById("spin-dropdown") !== null){
        const myNode = document.getElementById("spin-dropdown");
        while (myNode.firstChild) {
            myNode.removeChild(myNode.lastChild);
        }
        
        data.unshift("spin select")
        
        data.forEach((item, index) => {
            const option = document.createElement('option');
            option.value = `${item}`; // Assign value like option1, option2, etc.
            option.textContent = item; // The visible text for the option
            myNode.appendChild(option);
        });
        return
    }
    const select = document.createElement('select');
    select.id = "spin-dropdown";

    // Create the default disabled option
    const defaultOption = document.createElement('option');
    defaultOption.value = "";
    defaultOption.disabled = false;
    defaultOption.selected = true;
    defaultOption.textContent = "spin select";
    select.appendChild(defaultOption);

    // Dynamically create <option> elements from the array
    data.forEach((item, index) => {
        const option = document.createElement('option');
        option.value = `${item}`; // Assign value like option1, option2, etc.
        option.textContent = item; // The visible text for the option
        select.appendChild(option);
    });

    // Append the <select> to the div with class 'optionsSpinContainer'
    const container = document.querySelector('.optionsSpinContainer');
    container.appendChild(select);
}
// ─── Win Combos state ────────────────────────────────────────────────────────
/** Цвета комбо — по индексу */
var COMBO_COLORS = ['#007BFF','#28a745','#fd7e14','#9c27b0','#e91e63','#00bcd4','#ffcc00','#ff4444'];

/** { [spinName]: [{winElement: string, winElements: [[x,y],...]}] } */
var winCombosMap = {};

function getActiveCombos() {
    var ag = getActiveGrid();
    return winCombosMap[ag.selectedSpin] || [];
}
function setActiveCombos(spinName, combos) {
    winCombosMap[spinName] = combos;
}

/**
 * Закрашивает ячейки грида цветами всех сохранённых комбо.
 * Ячейки без комбо — сбрасываются в дефолт.
 */
function refreshGridColors(choosenGrid, spinName) {
    if (!choosenGrid) return;
    var grid = choosenGrid.children[0]; // gridContainer (div.grid-container)
    if (!grid) return;
    var combos = winCombosMap[spinName] || [];

    // Сбросить все цвета и флаги
    Array.from(grid.children).forEach(function(item) {
        item.classList.remove('selected');
        item.style.backgroundColor = '';
        item.style.borderColor = '';
        item.removeAttribute('data-combo-idx');
    });

    // Нанести цвет каждого комбо
    combos.forEach(function(combo, idx) {
        var color = COMBO_COLORS[idx % COMBO_COLORS.length];
        combo.winElements.forEach(function(pos) {
            var x = pos[0], y = pos[1];
            Array.from(grid.children).forEach(function(item) {
                if (Number(item.dataset.x) === x && Number(item.dataset.y) === y) {
                    item.style.backgroundColor = color;
                    item.style.borderColor      = color;
                    item.setAttribute('data-combo-idx', idx);
                }
            });
        });
    });
}

/** Кратковременный статус-текст под кнопками (2 сек) */
function showComboStatus(spinName, text) {
    var el = document.getElementById('combo-status-' + spinName);
    if (!el) return;
    el.textContent = text;
    clearTimeout(el._t);
    el._t = setTimeout(function() { el.textContent = ''; }, 2000);
}

/** Добавить текущее выделение + выбранный символ как новое комбо */
function addCombo() {
    var ag = getActiveGrid();
    var choosenGrid = ag.choosenGrid;
    var selectedSpin = ag.selectedSpin;

    if (!choosenGrid || !selectedSpin) {
        // Нет активного спина — ищем хотя бы первый в grids-container
        var first = document.querySelector('#grids-container .content');
        if (first) { showComboStatus(first.id, '⚠ Сначала выбери спин из списка'); }
        return;
    }

    var cells = getGrid(choosenGrid);
    if (cells.length === 0) {
        showComboStatus(selectedSpin, '⚠ Выдели ячейки на гриде');
        return;
    }

    var symbolEl = document.getElementById('dropdown-container').children.item(0);
    var symbol   = symbolEl ? symbolEl.options[symbolEl.selectedIndex].value : '';
    if (!symbol) {
        showComboStatus(selectedSpin, '⚠ Выбери элемент (дропдаун)');
        return;
    }

    var combos = winCombosMap[selectedSpin] || [];
    combos.push({ winElement: symbol, winElements: cells });
    winCombosMap[selectedSpin] = combos;

    // Снимаем .selected, красим всё комбо-цветами
    refreshGridColors(choosenGrid, selectedSpin);
    renderComboList(selectedSpin);
    showComboStatus(selectedSpin, '✅ Комбо ' + combos.length + ' добавлено!');
}

/** Удалить комбо по индексу */
function removeCombo(spinName, index) {
    var combos = winCombosMap[spinName] || [];
    combos.splice(index, 1);
    winCombosMap[spinName] = combos;
    renderComboList(spinName);
    // Обновляем цвета на активном гриде
    var ag = getActiveGrid();
    if (ag.choosenGrid) refreshGridColors(ag.choosenGrid, spinName);
}

/**
 * Клик на комбо в списке — показываем только его ячейки (остальные гасим).
 * Повторный клик — возвращаем все цвета.
 */
function previewCombo(spinName, index) {
    var ag = getActiveGrid();
    if (!ag.choosenGrid) return;
    var grid = ag.choosenGrid.children[0];
    if (!grid) return;

    var combos = winCombosMap[spinName] || [];
    var combo  = combos[index];
    if (!combo) return;

    var color = COMBO_COLORS[index % COMBO_COLORS.length];

    // Если уже в режиме превью этого же комбо — возвращаем все цвета
    if (ag.choosenGrid.getAttribute('data-preview') === String(index)) {
        ag.choosenGrid.removeAttribute('data-preview');
        refreshGridColors(ag.choosenGrid, spinName);
        return;
    }
    ag.choosenGrid.setAttribute('data-preview', index);

    // Гасим все, зажигаем только это комбо
    Array.from(grid.children).forEach(function(item) {
        item.style.backgroundColor = '';
        item.style.borderColor = '';
        item.classList.remove('selected');
    });
    combo.winElements.forEach(function(pos) {
        Array.from(grid.children).forEach(function(item) {
            if (Number(item.dataset.x) === pos[0] && Number(item.dataset.y) === pos[1]) {
                item.style.backgroundColor = color;
                item.style.borderColor      = color;
            }
        });
    });
}

function clearGridSelection(choosenGrid) {
    if (!choosenGrid) return;
    Array.from(choosenGrid.children[0].children).forEach(function(item) {
        item.classList.remove('selected');
    });
}
function setGridSelection(choosenGrid, winElements) {
    if (!choosenGrid) return;
    Array.from(choosenGrid.children[0].children).forEach(function(item) {
        var x = Number(item.dataset.x);
        var y = Number(item.dataset.y);
        if (winElements.some(function(w){ return w[0]===x && w[1]===y; }))
            item.classList.add('selected');
    });
}

/** Перерисовать список комбо под гридом */
function renderComboList(spinName) {
    var list = document.getElementById('combos-list-' + spinName);
    if (!list) return;
    var combos = winCombosMap[spinName] || [];
    list.innerHTML = '';
    combos.forEach(function(combo, i) {
        var color = COMBO_COLORS[i % COMBO_COLORS.length];

        var item = document.createElement('div');
        item.className = 'combo-item';

        // Цветной кружок
        var dot = document.createElement('span');
        dot.style.cssText = 'display:inline-block;width:9px;height:9px;border-radius:50%;' +
                            'background:' + color + ';flex:none;margin-right:5px;';

        var label = document.createElement('span');
        label.className = 'combo-label';
        label.textContent = combo.winElement + ' (' + combo.winElements.length + ')';
        label.title = combo.winElements.map(function(w){ return w[0]+','+w[1]; }).join(' | ');
        label.addEventListener('click', (function(sn, idx){ return function(){ previewCombo(sn, idx); }; })(spinName, i));

        var btn = document.createElement('button');
        btn.className   = 'combo-remove';
        btn.textContent = '×';
        btn.addEventListener('click', (function(sn, idx){ return function(){ removeCombo(sn, idx); }; })(spinName, i));

        item.appendChild(dot);
        item.appendChild(label);
        item.appendChild(btn);
        list.appendChild(item);
    });
}
// ─────────────────────────────────────────────────────────────────────────────

/**
 * Создает UI отображение сетки спина
 * @param {SpinDataType} data MAINDATA["Spins"]["Spin_0"]
 */
function createGrid(data){
    var sampleData = Mapper.SpinDataMapping(data);

    const gridsContainer = document.getElementById('grids-container');

    const gridContainer = document.createElement('div');
    gridContainer.classList.add('grid-container');
    gridContainer.id = sampleData.name;

    const contentContainer = document.createElement('div');
    contentContainer.id = sampleData.name;
    contentContainer.classList.add('content');

    contentContainer.appendChild(gridContainer);
    gridsContainer.appendChild(contentContainer);

    // Динамически создаём сетку
    const heightSize = sampleData.elsByHeight;
    const widthSize  = sampleData.elsByWidth;
    gridContainer.style = `grid-template-columns: repeat(${widthSize}, 1fr);`;
    for (let y = heightSize - 1; y >= 0; y--) {
        for (let x = 0; x < widthSize; x++) {
            const gridItem = document.createElement('div');
            gridItem.classList.add('grid-item');
            gridItem.dataset.x = x;
            gridItem.dataset.y = y;
            gridItem.addEventListener('click', function() {
                this.classList.toggle('selected');
            });
            gridContainer.appendChild(gridItem);
        }
    }

    // Combo list
    const comboList = document.createElement('div');
    comboList.id = 'combos-list-' + sampleData.name;
    contentContainer.appendChild(comboList);

    // Кнопки "+ Combo" / "Clear All"
    const comboRow = document.createElement('div');
    comboRow.className = 'combo-add-row';

    const addBtn = document.createElement('button');
    addBtn.textContent = '+ Combo';
    addBtn.title = 'Добавить выделенные ячейки как новое комбо';
    addBtn.addEventListener('click', addCombo);

    const clearBtn = document.createElement('button');
    clearBtn.textContent = 'Clear All';
    clearBtn.title = 'Очистить все комбо';
    clearBtn.addEventListener('click', function() {
        winCombosMap[sampleData.name] = [];
        renderComboList(sampleData.name);
        refreshGridColors(contentContainer, sampleData.name);
    });

    comboRow.appendChild(addBtn);
    comboRow.appendChild(clearBtn);
    contentContainer.appendChild(comboRow);

    // Статус-строка под кнопками
    var comboStatus = document.createElement('div');
    comboStatus.id = 'combo-status-' + sampleData.name;
    comboStatus.style.cssText = 'font-size:10px;min-height:13px;color:#aaa;text-align:center;margin-top:2px;';
    contentContainer.appendChild(comboStatus);

    // Инициализируем winCombosMap из данных спина (если ещё не задано)
    if (!winCombosMap[sampleData.name]) {
        winCombosMap[sampleData.name] = Array.isArray(sampleData.winCombos) ? sampleData.winCombos.slice() : [];
    }
    renderComboList(sampleData.name);

    setGrid(contentContainer, data);

    // Если есть сохранённые комбо — покрасить сразу
    if (winCombosMap[sampleData.name].length > 0) {
        refreshGridColors(contentContainer, sampleData.name);
    }

    return contentContainer;
}

/**
 * Функция записи выигрышных элементов
 * @param {HTMLElement} choosenGrid Грид спина
 * @param {SpinDataType} data Данные спина 
 */
function setGrid(choosenGrid, data){
    var sampleData = Mapper.SpinDataMapping(data)


    
    const gridItems = Object.values(choosenGrid.children[0].children)
    const array = data.winElements
    gridItems.forEach(item => {
        var target = [Number(item.dataset.x), Number(item.dataset.y)]
        var index = array.findIndex(
            item => item.length === target.length && item.every((val, idx) => val === target[idx])
        );

        if (index === -1){
            if(item.classList.contains('selected')){
                item.classList.remove('selected')
            }
        }
        else{
            if(!item.classList.contains('selected')){
                item.classList.add('selected')
            }
        }
    });
}

/**
 * Собирает информацию о выигрышных элементах
 * @param {HTMLElement} choosenGrid Грид, который отсматриваем 
 * @returns {Array} [[0,0],[0,1]]
 */
function getGrid(choosenGrid){
    var selectedItems = []

    const gridItems = Object.values(choosenGrid.children[0].children)
    gridItems.forEach(item => {
        if (item.classList.contains('selected')) {
            // if(selectedItems[item.dataset.x] == null){
            //     selectedItems[item.dataset.x] = []
            // }
            // selectedItems[item.dataset.x].push(item.dataset.y)
            selectedItems.push([Number(item.dataset.x),Number(item.dataset.y)])
        }
    });

    return selectedItems;
}

/**
 * Ищем активный spin и забираем его грид и имя
 * @returns {object} {choosenGrid, selectedSpin}
 */
function getActiveGrid(){
    const gridsContainer = document.getElementById("grids-container");
    Object.values(gridsContainer.children).forEach(item=>{
        if(item.classList.contains("active")){
            choosenGrid = item;
            selectedSpin = item.id;
        }
    })

    return {choosenGrid, selectedSpin}
}

function getUpdatesOfSpin(){
    var {choosenGrid, selectedSpin} = getActiveGrid();
    var selectedItems = [];
    var selectedSymbol = document.getElementById('dropdown-container').children.item(0)
        selectedSymbol = selectedSymbol.options[selectedSymbol.selectedIndex].value
    
    
    previousData = MAINDATA.Spins[selectedSpin]
    const result = {
        name: selectedSpin,
        isWinUpdate: false,
        isConfigUpdate: false,
        isResizeUpdate: false,
        winData: null,
        resizeData: null,
        configData: null
    }

    selectedItems = getGrid(choosenGrid);
    if(selectedItems !== previousData.winElements || selectedItem !== previousData.winElement){
        result.isWinUpdate = true
        result.winData = new WinDataType(selectedSymbol, selectedItems)
    }
    
    const data = getSpinData();
    const checks = [
        {
            keys: ["width", "height", "elsByHeight", "elsByWidth"],
            flag: "isResizeUpdate",
            createData: () => new ResizeDataType(data.width, data.height, data.elsByHeight, data.elsByWidth),
            updateField: "resizeData",
        },
        {
            keys: ["spacingVertical", "spacingHorizontal", "slowMain", "slowLines", "slowWin"],
            flag: "isConfigUpdate",
            createData: () => new ConfigDataType(data.spacingHorizontal, data.spacingVertical, data.slowMain, data.slowLines, data.slowWin),
            updateField: "configData",
        },
    ];
    
    checks.forEach(({ keys, flag, createData, updateField }) => {
        if (keys.some(key => data[key] !== previousData[key])) {
            result[flag] = true;
            result[updateField] = createData();
        }
    });

    return result;
}