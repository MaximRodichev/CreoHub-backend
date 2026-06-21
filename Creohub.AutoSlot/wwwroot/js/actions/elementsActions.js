let clickTimer = null;
let isLongPress = false;

function handleMouseDown() {
    isLongPress = false;
    clickTimer = setTimeout(async () => {
        isLongPress = true;
        reskinSymbols();
    }, 500);
}

function handleMouseUp() {
    if (clickTimer) {
        clearTimeout(clickTimer);
        if (!isLongPress) { wrapSymbols(); }
    }
}

document.addEventListener('DOMContentLoaded', function() {
    var buttonSymbols = document.getElementById("btn-symbols");
    if (buttonSymbols) {
        buttonSymbols.addEventListener("mousedown", handleMouseDown);
        buttonSymbols.addEventListener("mouseup", handleMouseUp);
    }
});




async function wrapSymbols() {
    adobeMiddleWare_.wrapElements();
    await adobeMiddleWare_.Analyze();
    initializeSymbolsScroll();
    loadSymbolThumbnails();
    renderSymbolPicker();
}

/**
 * Оборотка символов в композы, работа c middleWare
 * @returns 
 */
async function reskinSymbols(){
    if(!confirm("Заменить элементы новыми из папки AutoSlot / Symbols / GIFs_Reskin?\nСтарые элементы заменятся по позиции, лишние новые добавятся, лишние старые удалятся.")){
        return;
    }
    adobeMiddleWare_.reskinElements();
    await adobeMiddleWare_.Analyze();
    initializeSymbolsScroll();
    loadSymbolThumbnails();
    renderSymbolPicker();
}
function resizeElements(){
    try{
        widthInput = document.getElementById("elementsWidth").value.replace(" px", "");
        heightInput = document.getElementById("elementsHeight").value.replace(" px", "");
        if(widthInput == null || heightInput == null || widthInput < 50 || heightInput < 50){
            alert("Поля ширины и высоты пустые или меньше 50ти пикселей")
            return;
        }
        adobeMiddleWare_.setElementsSizes(parseInt(widthInput), parseInt(heightInput));
    }catch(ex){alert("Не удалось изменить размеры композиций")}
}
function setConfigurationToElements(){
    adobeMiddleWare_.setJSON()
}
async function getConfigurationOfElements(){
    let result = await adobeMiddleWare_.exportJSON()
    let shell;
    await csInterface.evalScript("shell()", (response) => {
        shell = response;
    })
    await sendRequest(
        "POST",
        window.location.origin+`/data/send?model=${result}`,
        null,
        shell
    )
}