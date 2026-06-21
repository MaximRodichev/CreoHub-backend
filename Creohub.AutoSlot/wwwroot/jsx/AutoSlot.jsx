/**
 * Класс для управления целым слотом:
 * - Элементами слота
 * - Его слотами (сущности, управляющие чередями спинов)
 * - Его спинами
 * 
 * При инициализации в первый раз:
 * - Создает Папку AutoSlot, элементы (Gifs и PreCompositions), композицию WinAnimation
 * 
 * При инициализации в последующие разы:
 * - Подкачивает все ресурсы с помощью find функций Helper'а
 */
function AutoSlot(){
    this.spinsCollection = {}
    this.name = "AutoSlot"
    if(Helper.findFolderByName(this.name) == null){
        this.AutoSlotFolder = app.project.items.addFolder(this.name)
        this.SymbolsFolder = this.AutoSlotFolder.items.addFolder("Symbols");
        this.GifsFolder = this.SymbolsFolder.items.addFolder("GIFs");
        this.ReskinFolder = this.SymbolsFolder.items.addFolder("GIFs_Reskin");
        this.SymbolCompsFolder = this.SymbolsFolder.items.addFolder("SymbolComps");
        this.WinAnimationComp = this.AutoSlotFolder.items.addComp("WinAnimation", 250, 250, 1, 40, 30);
        this.SpinsFolder = this.AutoSlotFolder.items.addFolder("Spins");
        this.SlotComp = this.AutoSlotFolder.items.addComp("Slot", 1080, 1080, 1, 40, 30);
        var solid = this.WinAnimationComp.layers.addSolid([1, 1, 1], "Adjustment Layer", this.WinAnimationComp.width, this.WinAnimationComp.height, 1);
            solid.adjustmentLayer = true;
            solid.name = "AdjustmentWinAnimation";
    }
    else{
        this.AutoSlotFolder = Helper.findFolderByName(this.name);
        this.SymbolsFolder = Helper.findFolderByName("Symbols", this.AutoSlotFolder);
        this.GifsFolder = Helper.findFolderByName("GIFs", this.SymbolsFolder);
        this.ReskinFolder = Helper.findFolderByName("GIFs_Reskin", this.SymbolsFolder);
        if(this.ReskinFolder == null && this.SymbolsFolder != null){
            this.ReskinFolder = this.SymbolsFolder.items.addFolder("GIFs_Reskin"); // авто-починка старых проектов
        }
        this.SymbolCompsFolder = Helper.findFolderByName("SymbolComps", this.SymbolsFolder);
        this.WinAnimationComp = Helper.findCompByName("WinAnimation", this.AutoSlotFolder);
        this.SpinsFolder = Helper.findFolderByName("Spins", this.AutoSlotFolder);
        this.SlotComp =  Helper.findCompByName("Slot", this.AutoSlotFolder);
        this.getSpins()
    }
}


AutoSlot.prototype.Analyze = function(){
    var slotData = {
        "Elements": {

        },
        "Spins":{

        }
    }
    // Строим карту: имя GIF → путь к файлу (для превью в панели)
    // Храним и с расширением, и без — SymbolComp обычно без расширения, footage — с
    var gifPaths = {};
    for (var g = 1; g <= this.GifsFolder.numItems; g++) {
        var gItem = this.GifsFolder.item(g);
        if (gItem.file) {
            var fsPath = String(gItem.file.fsName).replace(/\\/g, "/");
            gifPaths[gItem.name] = fsPath; // "bar.gif" → path
            var noExt = gItem.name.replace(/\.[^.]+$/, ""); // "bar" → path
            gifPaths[noExt] = fsPath;
        }
    }

    for(var symbolIndex = 1; symbolIndex<=this.SymbolCompsFolder.numItems; symbolIndex++){
        var cItem = this.SymbolCompsFolder.item(symbolIndex);
        var filePath = gifPaths[cItem.name] || null;
        slotData["Elements"][cItem.name] = new ElementDataType(cItem.name, cItem.width, cItem.height, filePath);
    }
    for(var spinIndex in this.spinsCollection){
        var spinItem = this.spinsCollection[spinIndex]
        slotData["Spins"][spinIndex] = spinItem.data.analyzeParse()
    }

    return JSON.stringify(slotData) 
}

AutoSlot.prototype.removeSpin = function(nameOfSpin){
    var spinCompToRemove = Helper.findCompByName(nameOfSpin, this.SpinsFolder);
    spinCompToRemove.remove();
    
}

/**
    Функция для создания нового слота
    Нейминг раздается со следующей логикой: Slot_0, Slot_1
    -- Созданная сущность Записывается в коллекцию слотов

    Вызвать можно следующим образом:
    -- AutoSlot.slotsCollection[nameOfCurrentSlot].function()
*/
AutoSlot.prototype.Spin = function(data){
    //{"ConfigurationSpin": {"slowLines": 0.1, "slowMain": 0.1, "spacingHorizontal": 40, "spacingVertical": 0},
    //"WinConfiguration": {"WinItem": "", "WinItems": []}}
    // var Spindata = {"spacingHorizontal": 40, "spacingVertical": 0,
    //             "ElementsByWidth": 5, "ElementsByHeight": 6,
    //             "width": 1080, "height": 1080,
    //             "slowLines": 0.1, "slowMain": 0.1, "slowWin": 0.1,
    //             "WinItem": "", "WinItems": [],}

    app.beginUndoGroup("AutoSlot: Создание спина");

    if(data == undefined || data == null){
        var SpinData = new SpinDataType(
            "Spin_" + String(Helper.lengthOfDict(this.spinsCollection)),
            1080, 1080, 6, 5, 40, 0,
            0.1, 0.1, 0.1
        );
    }
    else{
        var SpinData = new SpinDataType(
            "Spin_" + String(Helper.lengthOfDict(this.spinsCollection)),
            data.width, data.height,
            data.elsByHeight, data.elsByWidth,
            data.spacingHorizontal, data.spacingVertical,
            data.slowWin, data.slowLines, data.slowMain
        );
    }
    var a = new Spin(this, SpinData)

    this.spinsCollection[SpinData.name] = a;
    app.endUndoGroup();
    return a;
}
/**
    Функция созданная для подгрузки ранее созданных Слотов, при вторичной инициализации AutoSlot'a
*/
AutoSlot.prototype.getSpins = function(){
    if(this.SpinsFolder.numItems == 0){
        return;
    }
    for(var r=1; r<=this.SpinsFolder.numItems; r++){
        if(this.SpinsFolder.item(r).name.split("_")[0] == "Spin"){
            this.spinsCollection[this.SpinsFolder.item(r).name] = new Spin(this, {"name": this.SpinsFolder.item(r).name})
        }
    }
}
/**
    Одиночная функция с параметрами для оборачивания элемента из папки Gif's в композицию
*/
AutoSlot.prototype.wrapElement = function(element){
    var footage = element
    
    var comp = app.project.items.addComp(footage.name.split(".")[0], 250, 250,1,25,25);
    try{
        comp.addGuide(1, 50);
        comp.addGuide(1, 200);
        comp.addGuide(0, 50);
        comp.addGuide(0, 200);
    }
    catch(error){        
        alert("Error Handled at CreateGuides: ");
        alert(error);        
    }
    var layer = comp.layers.add(footage);
        layer.name = layer.name.split(".")[0]
    comp.parentFolder = this.SymbolCompsFolder;

    if(layer.canSetTimeRemapEnabled){
        layer.timeRemapEnabled = true;
        layer.timeRemap.expression = 'loopOut()';
    }     
    layer.outPoint = 60;  

    var adjComp = comp.layers.add(this.WinAnimationComp); 
        adjComp.moveToBeginning();
        adjComp.adjustmentLayer = true;   
        adjComp.collapseTransformation = true;
}
/**
    Функция проходящая по всем элементам из Gif's с помощью одиночной функции wrapElement
*/
AutoSlot.prototype.wrapElements = function(){
    if(this.SymbolCompsFolder===null){
        alert("Not Found: SymbolComps");
        return;
    }
    if(this.canReskinElements() == false){
        return;
    }

    app.beginUndoGroup("AutoSlot: Оборачивание элементов");
    var feedback_ = this.getReskinElements();
    try{feedback_=JSON.parse(feedback_)}catch(ex){alert(ex)}
    var h = feedback_["!NewSlotElements"];
    for(var i=1; i<=this.GifsFolder.numItems; i++){
        var footage = this.GifsFolder.item(i);
        if(h.indexOf(footage.name) != -1){
            try{
                this.wrapElement(footage)
            }catch(erro){alert(erro)}
        }
    }
    app.endUndoGroup();
    //alert("Отлично, в папке SymbolsComps были созданы композиции с вашими элементами, примените файл конфигурации или расположите элементы в созданные рамки вручную! После данных действий в Настройках (Settings) сможете сохранить конфигурацию элементов и применять в будущем")
}
/**
    Функция возвращающая рандомный игровой элемент из Папки с композициями элементов
    Warn: Папка с композициями элементов не должна быть пустой (WrapElements)
*/
AutoSlot.prototype.createElement = function(){
    var targetFolder = this.SymbolCompsFolder;
    if(targetFolder.numItems == 0){
        alert("ERROR: NOT FOUND ELEMENTS")
        return
    }
    do{
        var item = targetFolder.item(Helper.random(1,targetFolder.numItems))
    }while(item.name.indexOf("special") != -1 || item.name.indexOf("scatter") != -1 || item.name.indexOf("multiplier") != -1)
    return item;
}
/**
    Функция для замены элементов на новые.
    Warn: Папка с композициями элементов не должна быть пустой (WrapElements)
    Using: В папку с Gifs закидываем новые элементы, функция сама поймет какие элементы новые и заменит, удалит или добавит новые композиции
    ТРЕБУЕТ ДОРАБОТКИ (Новые элементы не добавятся на слот, удаленные сделают дырки в слоте)
*/
AutoSlot.prototype.reskinElements = function(){
    if(this.ReskinFolder == null){ alert("Не найдена папка GIFs_Reskin"); return; }

    // Новый набор берём из отдельной папки GIFs_Reskin — туда монитор авто-врапа не лезет.
    var reskinItems = this.ReskinFolder.getArray();
    var comps = this.SymbolCompsFolder.getArray();
    if(reskinItems.length === 0){ alert("Папка GIFs_Reskin пуста — нечего заменять."); return; }

    app.beginUndoGroup("AutoSlot: Рескин элементов");
    var report = { replaced: 0, created: 0, removed: 0 };
    var n = Math.min(reskinItems.length, comps.length);

    // 1) Замена по позиции: i-й новый элемент → i-й прекомп (in-place, анимация цела)
    for(var i=0; i<n; i++){
        try{
            var comp  = comps[i];
            var layer = comp.layer(comp.name);          // футадж-слой внутри прекомпа
            var oldSource = layer.source;               // старый footage-айтем
            var baseName  = reskinItems[i].name.split(".")[0];

            layer.replaceSource(reskinItems[i], false);
            layer.name = baseName;
            comp.name  = baseName;
            if(layer.canSetTimeRemapEnabled){
                layer.timeRemapEnabled = false;
                layer.timeRemapEnabled = true;
            }

            reskinItems[i].parentFolder = this.GifsFolder;  // новый гиф → в канонический GIFs
            if(oldSource && oldSource.parentFolder === this.GifsFolder){ oldSource.remove(); } // старый айтем из проекта
            report.replaced++;
        }catch(e){ alert("reskin replace error: " + e.toString()); }
    }

    // 2) Лишние новые (пары не нашлось) → создаём новые прекомпы
    for(var j=n; j<reskinItems.length; j++){
        try{
            this.wrapElement(reskinItems[j]);
            reskinItems[j].parentFolder = this.GifsFolder;
            report.created++;
        }catch(e){ alert("reskin create error: " + e.toString()); }
    }

    // 3) Лишние старые прекомпы (пары не нашлось) → удаляем вместе со старым гифом
    for(var k=n; k<comps.length; k++){
        try{
            var oldLayer = comps[k].layer(comps[k].name);
            var src = oldLayer ? oldLayer.source : null;
            comps[k].remove();
            if(src && src.parentFolder === this.GifsFolder){ src.remove(); }
            report.removed++;
        }catch(e){ alert("reskin remove error: " + e.toString()); }
    }

    app.endUndoGroup();
    alert("Рескин завершён.\nЗаменено: " + report.replaced + "\nСоздано: " + report.created + "\nУдалено: " + report.removed);
}
/**
    Функция возвращающая словарь с 
    ["CurrentSlotElements"] -- Элементы которые сейчас используются и хотим заменить
    ["!NewSlotElements"] -- Новые элементы которые заменят старые
*/
AutoSlot.prototype.getReskinElements = function(){
    try{
        var feedback = {}
        feedback["CurrentSlotElements"] = []
        feedback["!NewSlotElements"] = []
        var allGifs = this.GifsFolder.getArray()
        var allSymbolComps = this.SymbolCompsFolder.getArray();
        var allSymbolCompsNames = allSymbolComps.map(function(elem){
            return elem.name
        })
        allGifs.map(function(elem){
            if(allSymbolCompsNames.indexOf(elem.name.split(".")[0]) != -1){
                feedback["CurrentSlotElements"].push(elem.name);
            }
            else{
                feedback["!NewSlotElements"].push(elem.name);
            }
        })
        feedback = JSON.stringify(feedback);
        return feedback;
    }catch(ex){alert("execute Exception at getReskinElements()\n" + (ex.message));}
}
/**
    Функция возвращающая True/False о наличии новых элементов и возможности замены старых на них
*/
AutoSlot.prototype.canReskinElements = function(){
    var feedback_ = this.getReskinElements();
    try{feedback_ = JSON.parse(feedback_)}catch(ex){}
    if(feedback_["!NewSlotElements"].length == 0){
        return false;
    }
    return true;
}
/**
    Функция применяющая JSON конфигурацию элементов, для их правильной позиции в соответствии с композициями
*/
AutoSlot.prototype.setJSON = function(){
    var folder = this.SymbolCompsFolder
    if(folder == null){
        alert("SymbolCompsFolder is null")
        return;
    }
    
    var jsonFile = File.openDialog("Выберите соответствующий вашим элементам файл конфигурации");
    if (jsonFile instanceof File) {
        
        var jsonData = JSON.read(String(jsonFile))
        if(jsonData["AutoSlot"] == null || jsonData["AutoSlot"] == undefined){
            alert("Данный конфигурационный файл не подходит для AutoSlot")
            return;
        }
        if(jsonData["AutoSlot"] != "Symbols"){
            alert("Данный конфигурационный файл предназначен для " + (jsonData["AutoSlot"]))
            return;
        }
        app.beginUndoGroup("AutoSlot: Применение JSON конфигурации");
        for(var x=1; x <= folder.numItems; x++ ){
            var comp = folder.item(x);
            var footage = comp.layer(comp.name);

            var coefficientW = comp.width / jsonData[comp.name]["sourceRectSize"][0];
            var coefficientH = comp.height / jsonData[comp.name]["sourceRectSize"][1];

            var setterScale = [jsonData[comp.name]["scale"][0]*coefficientW, jsonData[comp.name]["scale"][1]*coefficientH];
            var setterPosition = [jsonData[comp.name]["position"][0]*coefficientW, jsonData[comp.name]["position"][1]*coefficientH]
            footage.property("Scale").setValue(setterScale);
            footage.property("Position").setValue(setterPosition);
            footage.property("Rotation").setValue(jsonData[comp.name]["rotation"]);
        }
        app.endUndoGroup();
    }
    else{
        alert("Invalid JSON file.");
    }
}
/**
    Функция сохраняющая JSON конфигурацию элементов, для их правильной позиции в соответствии с композициями
*/
AutoSlot.prototype.exportJSON = function(){
    if(this.SymbolCompsFolder.numItems == 0){
        return;
    }
    var compData = {"AutoSlot": "Symbols"};
    var symbolCompsFolder = this.SymbolCompsFolder;
    var path = []
    for (var i = 1; i <= symbolCompsFolder.numItems; i++) {
        
        if(symbolCompsFolder.item(i) instanceof CompItem){
            var currentComp = symbolCompsFolder.item(i);
            
            for (var j = 1; j <= currentComp.numLayers; j++){
                var item = symbolCompsFolder.item(i).layer(j);
                
                if(currentComp.name === item.name.split(".")[0]){
                    var itemName = item.name.split(".")[0];

                    var sourceScaleX = currentComp.width;
                    var sourceScaleY = currentComp.height;

                    var layer = symbolCompsFolder.item(i).layer(j);

                    var positionProperty = layer.property("Position").value;

                    var positionX = positionProperty[0];
                    var positionY = positionProperty[1];

                    var scaleProperty = layer.property("Scale").value;
                    var scaleX = scaleProperty[0];
                    var scaleY = scaleProperty[1];

                    var rotation = layer.property("Rotation").value;

                    compData[itemName] = {
                        "sourceRectSize": [
                            sourceScaleX,
                            sourceScaleY
                        ],
                        "position": [
                            positionX,
                            positionY
                        ],
                        "scale": [
                            scaleX,
                            scaleY
                        ],
                        "rotation": rotation
                    };
                    if(path[0] === String(layer.source.file).replace(item.name, "")){
                    }else{
                        
                        path.push(String(layer.source.file).replace(item.name, ""))
                    }
                    break;
                }
            }
        }
    }
    
    path = path.length === 1 ? path[0].replace("/.mov", "").replace("/.gif", "").replace("/.png", "").replace("/.webp", "").replace("/.jpg", "") : String(Folder.selectDialog());
    var pathResult = path + "/config.json"
    
    JSON.write(compData, pathResult);
    alert("Ваш конфиг сохранен по данному пути:\n"+path)
    
    return JSON.stringify(compData);
}
/**
    Функция изменяющая размер композиции элемента, для лучшей оптимизации работы
    Принимает параметр newSize = {"width": ?, "height": ?}
*/
AutoSlot.prototype.resizeElement = function(itemComp, newSize){
    var coefficientW = newSize["width"] / itemComp.width
    var coefficientH = newSize["height"] / itemComp.height
    
    itemComp.width = newSize["width"]
    itemComp.height = newSize["height"]

    var guideSpaceWidth = itemComp.width * .2;
    var guideSpaceHeigth = itemComp.height * .2;
    if(itemComp.guides.length==4){
        itemComp.setGuide(guideSpaceWidth, 0);
        itemComp.setGuide(itemComp.width - guideSpaceWidth, 1);
        itemComp.setGuide(itemComp.height - guideSpaceHeigth, 2);
        itemComp.setGuide(guideSpaceHeigth, 3);
    }
    
    for(var j=1; j<=itemComp.layers.length; j++){
        var lastPos = itemComp.layer(j).property("Position").value
        var lastScale = itemComp.layer(j).property("Scale").value
        itemComp.layer(j).property("Position").setValue([lastPos[0] * coefficientW, lastPos[1] * coefficientH])
        itemComp.layer(j).property("Scale").setValue([lastScale[0]*coefficientW, lastScale[1]*coefficientH])
    }
}
/**
    Функция проходящаяся по всем элементам функцией resizeElement()
    Принимает параметр newSize = {"width": ?, "height": ?}
*/
AutoSlot.prototype.resizesElements = function(newSize){
    app.beginUndoGroup("AutoSlot: Ресайз элементов");
    try{
        try{newSize = JSON.parse(newSize)}catch(ex){}
        for(var x=1; x<= this.SymbolCompsFolder.numItems; x++){
            var thisItem = this.SymbolCompsFolder.item(x);
            this.resizeElement(thisItem, newSize)
        }
    }catch(ex){alert("resizeElements error:" + (ex))}
    app.endUndoGroup();
}
