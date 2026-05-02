//window.parent.postMessage("alert('hello')", "*");

const dev = false;
var MAINDATA = new AutoSlotDataType(
    "AutoSlot",
    {
        "scatter_symbolscatter_symbolscatter_symbol": new ElementDataType("scatter_symbol", 500, 500),
        "wild_symbolwild_symbolwild_symbol": new ElementDataType("wild_symbol", 500, 500),
        "simple_Symbol01": new ElementDataType("simple_Symbol01", 500, 500),
        "bonus_Symbol": new ElementDataType("bonus_Symbol", 500, 500),
        "bonus_Symbol2": new ElementDataType("bonus_Symbol2", 500, 500)
    },
    {
        "Spin_1": new SpinDataType("Spin_1", 1080, 1080, 5, 6, 120, 120, "", [], 0.1, 0.1, 0.1),
        "Spin_2": new SpinDataType("Spin_2", 1080, 1920, 3, 1, 12, 11, "", [], 0.2, 0.1, 0.1),
        "Spin_3": new SpinDataType("Spin_3", 720, 640, 4, 7, 132, 124, "", [], 0.3, 0.1, 0.1),
        "Spin_4": new SpinDataType("Spin_4", 1920, 720, 8, 3, 1612, 124, "", [], 0.44, 0.1, 0.1)
    }
);
const adobeMiddleWare_ = new iframeMiddleWare("AutoSlot_");

async function loadJsxScripts() {
    const map = {
        "localization": "common/localization.jsx",
        "helpers": "helpers.jsx",
        "SpinDataType": "types/SpinDataType.jsx",
        "ElementDataType": "types/ElementDataType.jsx",
        "ResizeDataType": "types/ResizeDataType.jsx",
        "ConfigDataType": "types/ConfigDataType.jsx",
        "WinDataType": "types/WinDataType.jsx",
        "Expressions": "Expressions.jsx",
        "Line": "Line.jsx",
        "Spin": "Spin.jsx",
        "AutoSlot": "AutoSlot.jsx",
    };

    const promises = Object.entries(map).map(async ([key, filePath]) => {
        try {
            return await fetchJSX(filePath)
        } catch (error) {
            console.error(error);
            return "";
        }
    });

    await Promise.all(promises);
}



function showContent(tabNumber) {
    if(tabNumber == 4){
        g = document.getElementById('content' + tabNumber).classList;
        if(g.contains('active')){
            g.remove('active')
            return;
        }
        g.add('active');
        return;
    }
    // Скрываем все содержимое
    const contents = document.querySelectorAll('.content');
    contents.forEach(content => content.classList.remove('active'));

    // Убираем активное состояние у всех вкладок
    const tabs = document.querySelectorAll('.tab');
    tabs.forEach(tab => tab.classList.remove('active'));

    // Показываем содержимое для выбранной вкладки
    document.getElementById('content' + tabNumber).classList.add('active');

    // Добавляем активное состояние выбранной вкладке
    tabs[tabNumber - 1].classList.add('active');
}

document.addEventListener('DOMContentLoaded', async function() {
    const spinner = document.getElementById("loading-spinner");

    // Показываем спиннер
    spinner.style.display = "block";
    document.getElementById("eduModeCheckbox").checked = localStorage.getItem("eduMode") !== "false";
    try {
        await loadJsxScripts();

        var x = new AutoSlotDataType(
            "AutoSlot",
            {
                "element_01": new ElementDataType("element_01", 500, 500),
                "element_02": new ElementDataType("element_01", 500, 500),
                "element_03": new ElementDataType("element_01", 500, 500),
            },
            {
                "Spin_1": SpinDataType.fromData(
                    "Spin_1",
                    new ResizeDataType(1080, 1080, 5, 6),
                    new ConfigDataType(-100, -100, 0.1, 1, 0.5),
                    new WinDataType("", [])
                )
            }
        );
        
            try { await adobeMiddleWare_.Analyze(); } catch (e) { console.error(e); }
    
            Object.keys(MAINDATA["Spins"]).forEach((elemnt) => {
                createGrid(MAINDATA["Spins"][elemnt]);
            });
        
        createOrUpdateDropdownWinItems();
        createOrUpdateDropdownSpinItems();
        initializeSymbolsScroll();

        if (dev) {
            const selectElement = document.getElementById("spin-dropdown");
            selectElement.value = "Spin_1";
            const changeEvent = new Event('change');
            document.getElementById("spinSelector").dispatchEvent(changeEvent);
        }

    } catch (error) {
        console.error("Ошибка загрузки:", error);
        alert("Ошибка при загрузке данных.");
    } finally {
        // Скрываем спиннер после загрузки
        spinner.style.display = "none";
        showTooltip(
            document.getElementById("symbolsScroll"),
            'Добавьте ваши элементы!\nAutoSlot/Symbols/Gifs',
            null,
            'right'
        );
    }
});

document.getElementById("spinSelector").addEventListener('change', function(){
    const selected = this.children.item(0);

    Array.from(document.getElementById('grids-container').children).forEach((item)=>{
        if(item.classList.contains('active')){
            item.classList.remove('active');
        }
        if(item.id === selected.value){
            item.classList.add('active');
            csInterface.evalScript(`Helper.findCompByName("${item.id}", Helper.findFolderByName("Spins")).openInViewer().setActive();`)
        }
    })
    setSpinData(MAINDATA.Spins[selected.value]);
    const winSelector = document.getElementById('dropdown-container').children.item(0)
    Object.values(winSelector.options).forEach(
        item=>item.selected =
            item.value === MAINDATA["Spins"][selected.value]["winElement"]
    )
});

document.getElementById("submit-button").addEventListener('click', async function(){
    var d = getUpdatesOfSpin();
    console.log(JSON.stringify(d, '\n', 2));


    if(d.isWinUpdate){
        adobeMiddleWare_.setWinGrid(d.name, d.winData);
    }
    if(d.isResizeUpdate){
        adobeMiddleWare_.resize(d.name, d.resizeData)
    }
    if(d.isConfigUpdate){
        adobeMiddleWare_.setConfigSpin(d.name, d.configData)
    }
    await adobeMiddleWare_.Analyze();

    var {choosenGrid, selectedSpin} = getActiveGrid();

    choosenGrid.remove();
    createGrid(MAINDATA.Spins[selectedSpin]).classList.add("active");

});

// document.getElementById("grids-container-text").addEventListener("click", function(){
//     if(this.innerText == "Submit Joystick"){
//         writeActionJoystick();
//         // console.log(getJoystickSubmits());
//         clearJoystick(getActiveGrid()["choosenGrid"]);
//         this.innerText = "Joystick Items";
//     }
//     if(this.innerText == "Boom Joystick"){
//         boomActionJoystick();
//         this.innerText = "Joystick Items"
//     }
// })


