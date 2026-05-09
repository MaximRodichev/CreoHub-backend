/**
 * Класс хранящие заготовленные обращения к JSX
 */
class CallFunctions {
    /**
     * Базовый конструктор
     * @param {string} nameOf Имя переменной, пример: AutoSlot_
     */
    constructor(nameOf) {
        this.nameOf = nameOf;
        this.predict = `var ${this.nameOf} = new AutoSlot()\n`;
    }
    
    /**
     * AutoSlot_.wrapElements()
     * @readonly
     * @type {string} 
     */
    get wrapElement() {
        return `${this.predict}${this.nameOf}.wrapElements()`;
    }
    
    /**
     * AutoSlot_.Analyze()
     * 
     * @readonly
     * @type {string}
     */
    get analyze() {
        return `${this.predict}${this.nameOf}.Analyze()`;
    }

    /**
     * AutoSlot_.Spin().createGrid()
     * 
     * @readonly
     * @type {string}
     */
    get addSpin() {
        return `${this.predict}${this.nameOf}.Spin().createGrid()`;
    }

    copySpin(dataspin) {
        return `${this.predict}${this.nameOf}.Spin(${dataspin}).createGrid()`;
    }

    /**
     * AutoSlot_.spinsCollection["${nameSpin}"].resize(${data})
     * @param {string} nameSpin "Spin_1"
     * @param {object} data spacings winitens and etc. 
     */
    resize(nameSpin, data) {
        return `${this.predict}${this.nameOf}.spinsCollection["${nameSpin}"].resize(${data})`;
    }

    /**
     * AutoSlot_.spinsCollection["${nameSpin}"].setWinGridClear(${data})
     * @param {string} nameSpin "Spin_1"
     * @param {object} data spacings winitens and etc. 
     */
    setWinGrid(nameSpin, data) {
        return `${this.predict}${this.nameOf}.spinsCollection["${nameSpin}"].setWinGridClear(${data})`;
    }

    /**
     * AutoSlot_.spinsCollection["${nameSpin}"].changeSpinElementConfig(${data})
     * @param {string} nameSpin "Spin_1"
     * @param {object} data spacings winitens and etc. 
     */
    setConfigSpin(nameSpin, data) {
        return `${this.predict}${this.nameOf}.spinsCollection["${nameSpin}"].changeSpinElementConfig(${data})`;
    }

    /**
     * AutoSlot_.getReskinElements()
     * Получает список текущих и новых элементов, готовых к замене.
     * @readonly
     * @type {string}
     */
    get getReskinData() {
        return `${this.predict}${this.nameOf}.getReskinElements()`;
    }

    
    /**
     * AutoSlot_.setJSON()
     * Устанавливает конфигурацию элементов
     * @readonly
     * @type {string}
     */
    get setJSON() {
        return `${this.predict}${this.nameOf}.setJSON()`;
    }

    
    /**
     * AutoSlot_.exportJSON()
     * Экспортирует текущую информацию о конфигурации элементов в JSON
     * @readonly
     * @type {string}
     */
    get exportJSON() {
        return `${this.predict}${this.nameOf}.exportJSON()`;
    }
    /**
     * AutoSlot_.resizesElements(${data})
     * @param {string} data JSON.stringify({"width", "height"})
     * @returns 
     */
    resizeElementsCall(data) {
        return `${this.predict}${this.nameOf}.resizesElements(${data})`;
    }
    /**
     * Делает рескин элементов CurrentSlotElements удаляет и заменяет на NewSlotElements
     *
     * @readonly
     * @type {string}
     */
    get reskinElements(){
        return `${this.predict}${this.nameOf}.reskinElements()`;
    }

    /**
     * AutoSlot_.spinsCollection["${nameSpin}"].setWinGridMulti(${winCombos})
     * @param {string} nameSpin "Spin_1"
     * @param {Array}  winCombos [{winElement, winElements}, ...]
     */
    /**
     * @param {string} nameSpin
     * @param {Array}  winCombos  [{winElement, winElements, startOffset}, ...]
     * @param {string} playMode   'fixedEnd' | 'sequential' | 'loop'
     */
    setWinGridMulti(nameSpin, winCombos, playMode) {
        return `${this.predict}${this.nameOf}.spinsCollection["${nameSpin}"].setWinGridMulti(${winCombos}, '${playMode || 'fixedEnd'}')`;
    }

    removeSpin(spinName){
        return `${this.predict}${this.nameOf}.removeSpin('${spinName}')`;
    }

    /**
     * Inline-скрипт: обходит папку AutoSlot/Symbols/GIFs и возвращает JSON {name: fsPath}
     * Не зависит от загрузки JSX-файлов.
     */
    get getGifPaths() {
        return `(function(){
            var paths = {};
            for(var i=1; i<=app.project.numItems; i++){
                var it = app.project.item(i);
                if(it instanceof FolderItem && it.name === "AutoSlot"){
                    for(var j=1; j<=it.numItems; j++){
                        var sym = it.item(j);
                        if(sym instanceof FolderItem && sym.name === "Symbols"){
                            for(var k=1; k<=sym.numItems; k++){
                                var gf = sym.item(k);
                                if(gf instanceof FolderItem && gf.name === "GIFs"){
                                    for(var g=1; g<=gf.numItems; g++){
                                        var gi = gf.item(g);
                                        if(gi.file){
                                            var nm = gi.name.replace(/\\.[^.]+$/, "");
                                            paths[nm] = String(gi.file.fsName);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
            }
            return JSON.stringify(paths);
        })()`;
    }

}