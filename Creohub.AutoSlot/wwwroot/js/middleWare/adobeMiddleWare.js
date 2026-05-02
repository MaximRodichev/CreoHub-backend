/**
 * Класс реализующий работу с JSX через CEP Panel
 */
class adobeMiddleWare {
    /**
     * Базовый конструктор
     * @param {string} nameOf "AutoSlot_"
     */
    constructor(nameOf){
        this.callFunctions = new CallFunctions(nameOf);
    };
    /**
     * @returns {boolean} Находимся ли мы в среде AE или нет
     */
    isAdobe() {
        try{ return true;}
        catch(err){return false;}
    }
    /**
     * Реализует бэкдор на получение данных от JSX функций
     * @param {string} script 
     * @returns {any} Данные полученные от JSX
     */
    evaluateWithResponseAsync(script){
        return new Promise((resolve, reject) => {
            new CSInterface().evalScript(script, (result) => {
                if (result) {
                    resolve(result);
                } else {
                    reject('Ошибка: нет результата');
                }
            });
        });
    }

    /**
     * Инициализация AutoSlot
     */
    startUp() {
        if (this.isAdobe()){
            window.parent.postMessage({script: `${this.predict}`});
            //this.csInterface.evalScript(`${this.predict}`);    
        }
    }
    /**
        Функция оборачивания элементов в композиции
    */
    wrapElements() {
        if (this.isAdobe()){
            window.parent.postMessage({script: `${this.callFunctions.wrapElement}`}, "*");
            //this.csInterface.evalScript(`${this.callFunctions.wrapElement}`);
        }else{alert("Not AE: wrapElements")}
   }
   /**
    * Функция вызывающая Analyze в JSX
    * @returns {object} Объект с полными данными о текущем состояниее AutoSlot
    */
    async Analyze(){
        let result = "";
        if (this.isAdobe()){
            window.parent.postMessage({script: `${this.callFunctions.analyze}`}, "analyze");
            //result = await evaluateWithResponseAsync(`${this.callFunctions.analyze}`)
            // this.csInterface.evalScript(`${this.predict}`);
            // window.parent.postMessage("alert('hello')", "*");
            const responseData = JSON.parse(result);
            MAINDATA = Mapper.AutoSlotMapping(responseData);
            console.log(responseData);
            return responseData;
        }
        //Если мы не в среде Adobe
        else{
        }
   }
    /**
     * Вызывает функцию получения текущего состояния элементов в слоте
     * @returns {object} Словарь с !NewSlotElements & CurrentSlotElements
     */
    async getReskinElements(){
        var result = "";
        if(this.isAdobe()){
            result = await this.evaluateWithResponseAsync(`${this.callFunctions.getReskinData}`)
            const responseData = JSON.parse(result);
            return responseData;
        }else{alert("Not AE: getReskinElements")}
    }
   /**
        Функция добавления новой композиции спина
    */
    addSpin(){
        if(this.isAdobe()){
            this.csInterface.evalScript(`${this.callFunctions.addSpin}`)
        }else{alert("Not AE: addSpin")}
   }
   /**
    * Устанавливает конфигурацию из JSON файла
    */
    setJSON(){
        if(this.isAdobe()){
            this.csInterface.evalScript(`${this.callFunctions.setJSON}`)
        }else{alert("Not AE: setJSON")}
   }
   /**
    * Сохраняет конфиг элементов в JSON
    */
    exportJSON(){
        if(this.isAdobe()){
            this.csInterface.evalScript(`${this.callFunctions.exportJSON}`)
        }else{alert("Not AE: exportJSON")}
    }
   /**
    * Устанавливает новые размеры игровых элементов
    * @param {String} spinName 
    * @param {ConfigDataType} resizeData 
    */
    resize(spinName, resizeData){
        if(this.isAdobe()){
            this.csInterface.evalScript(`${this.callFunctions.resize(spinName, JSON.stringify(resizeData))}`)
        }else{console.alert(JSON.stringify(resizeData, null, 2))}
   }
   /**
    * Задает новые размеры всем прекомпам элементов
    * @param {number} width 
    * @param {number} height 
    */
    setElementsSizes(width, height){
        if(this.isAdobe()){
            this.csInterface.evalScript(`${this.callFunctions.resizeElementsCall(JSON.stringify({"width": width, "height": height}))}`)
        }else(alert(JSON.stringify({"width": width, "height": height})))
   }
   /**
    * Делает рескин элементов CurrentSlotElements удаляет и заменяет на NewSlotElements
    */
   reskinElements(){
        if(this.isAdobe()){
            this.csInterface.evalScript(`${this.callFunctions.reskinElements}`)
        }else{alert("Not AE: reskinElements")}
   }
   /**
    * Установка выигрышной игры
    * @param {String} spinName 
    * @param {WinDataType} winData 
    */
   setWinGrid(spinName, winData){
        if(this.isAdobe()){
            this.csInterface.evalScript(`${this.callFunctions.setWinGrid(spinName, JSON.stringify(winData))}`)
        }else{console.alert(JSON.stringify(winData, null, 2))}
   }
   /**
    * Задает новый конфиг элементам
    * @param {String} spinName 
    * @param {WinDataType} winData 
    */
   setConfigSpin(spinName, configData){
    if(this.isAdobe()){
        this.csInterface.evalScript(`${this.callFunctions.setConfigSpin(spinName, JSON.stringify(configData))}`)
    }else{console.alert(JSON.stringify(configData, null, 2))}
}
}