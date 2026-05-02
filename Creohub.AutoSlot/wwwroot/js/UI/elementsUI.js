/**
    Инициализирует скролл элемент с элементами используемыми текущим слотом
    Обновляет текущее состояние, если скролл уже проинициализирован
*/
function initializeSymbolsScroll() {
    // Example data with different formats
    const container = document.getElementById('symbolsScroll');
    while(container.firstChild){
        container.removeChild(container.lastChild);
    }
    for (const item in MAINDATA.Elements) {
        const scrollItem = document.createElement('div');
        scrollItem.classList.add('scroll-item');
        scrollItem.textContent = item;
        scrollItem.addEventListener('click', ()=>{
            console.log(item);
            csInterface.evalScript(`Helper.findCompByName("${item}", Helper.findFolderByName("SymbolComps")).openInViewer().setActive();`)
            
        })
        container.appendChild(scrollItem);
    }
}

let elementsWidth = 250;
let elementsHeight = 250;
document.getElementById("elementsWidth").addEventListener('change', ()=>{
    let value = this.value;
        if(value !== elementsWidth){
            resizeElements();
            elementsWidth = value
        }
})
document.getElementById("elementsHeight").addEventListener('change', ()=>{
    let value = this.value;
    if(value !== elementsHeight){
        resizeElements();
        elementsHeight = value
    }
})
