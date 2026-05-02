function JOYSTICK(){
    this.MarkerUP = new MarkerValue("Up");
    this.MarkerUP.duration = 0.8
    this.MarkerLeft = new MarkerValue("left");
    this.MarkerLeft.duration = 0.8
    this.MarkerRight = new MarkerValue("right");
    this.MarkerRight.duration = 0.8
    this.MarkerWin = new MarkerValue("Win")
}

JOYSTICK.MarkerRight = function(){
    var a = new MarkerValue("right");
    a.duration = 0.8;
    return a
}
JOYSTICK.MarkerUP = function(){
    var a = new MarkerValue("Up");
    a.duration = 0.8;
    return a
}
JOYSTICK.MarkerLeft = function(){
    var a = new MarkerValue("left");
    a.duration = 0.8;
    return a
}
JOYSTICK.MarkerWin = function(){
    var a = new MarkerValue("Win");
    return a
}



/**
 * Проверка на выигрышность
 * @param {AVLayer} selectedLayer 
 * @returns {Boolean} true/false
 */
JOYSTICK.checkIfWin = function(selectedLayer){
    for(var MarkerkeyIndex=1; MarkerkeyIndex <= selectedLayer.marker.numKeys; MarkerkeyIndex++){
        if(selectedLayer.marker.keyValue(MarkerkeyIndex).comment == "Win"){
            return true;
        }
    }
    return false;
}
/**
 * Проверка на трушность слоя: это игровой элемент и он не выигрышный
 * @param {AVLayer} selectedLayer 
 * @returns {Boolean} true/false
 */
JOYSTICK.checkIfTrueLayer = function(selectedLayer){
    if(selectedLayer instanceof AVLayer && selectedLayer.nullLayer == false && selectedLayer.name.split("_Item_").length == 2){
        if(this.checkIfWin(selectedLayer)){
            return false
        }
        return true
    }
    return false
}
/**
 * Устанавливает Y expression для последующей работы
 * @param {AVLayer} selectedLayer 
 */
JOYSTICK.SetYJoystick_expression = function(selectedLayer){
    if(this.checkIfTrueLayer(selectedLayer)){
        var previousExpression = selectedLayer.transform.yPosition.expression;
        if(previousExpression.split("var swapParam = transform.yPosition").length == 1){
            var executeTarget = previousExpression
                            .replace('thisComp.layer("', "")
                            .split('"')[0]
            var executeParam = parseFloat(previousExpression
                                    .replace(') + transform.yPosition;', "")
                                    .split('.valueAtTime(time-')[1])
            selectedLayer.transform.yPosition.expressionEnabled = false
            var destination = selectedLayer.transform.yPosition.value
            selectedLayer.transform.yPosition.expressionEnabled = true
            
            destination = parseFloat(destination/(parseInt(selectedLayer.name.split("_Item_")[1])+1))
            var paramExtended = parseInt(selectedLayer.name.split("_Item_")[1]) != 0 ? (executeParam / parseInt(selectedLayer.name.split("_Item_")[1])) : 0;
            selectedLayer.transform.yPosition.expression = EXPRESSIONS_V2.element_Y_joystick(executeTarget, 
                                            executeParam,
                                            paramExtended,
                                            selfData["ElementsByHeight"], 
                                            destination)
        }
    }
}
/**
 * Устанавливает X expression для последующей работы
 * @param {AVLayer} selectedLayer 
 * @param {*} compSelected 
 */
JOYSTICK.SetXJoystick_expression = function(selectedLayer, compSelected){
    if(this.checkIfTrueLayer(selectedLayer)){
        var previousExpression = selectedLayer.transform.xPosition.expression;
        if(previousExpression.split("var swapParam = transform.yPosition").length == 1){
            gParam = (compSelected.layer("Line_1").transform.xPosition.value - compSelected.layer("Line_0").transform.xPosition.value).toFixed(2)
            selectedLayer.transform.xPosition.expression = EXPRESSIONS_V2.element_X_joystick(gParam)
        }
    }
}
/**
 * Установка маркера UP
 */
JOYSTICK.Up = function(){
    if(app.project.activeItem instanceof CompItem){
        var compSelected = app.project.activeItem;
        selfData = JSON.parse(String(compSelected.layer("CONFIGURATION, DONOTDELETE").text.sourceText.value))
        var timeToPaste = app.project.activeItem.time;
        for(x=0; x<compSelected.selectedLayers.length; x++){
                var selectedLayer = compSelected.selectedLayers[x];
                this.SetYJoystick_expression(selectedLayer)
                selectedLayer.marker.setValueAtTime(timeToPaste, JOYSTICK.MarkerUP());
        }
    }
}
/**
 * Установка Down анимации
 * @param {Number} FallIndex 
 */
JOYSTICK.Down = function(FallIndex){
    if(app.project.activeItem instanceof CompItem){
        var compSelected = app.project.activeItem;
        selfData = JSON.parse(String(compSelected.layer("CONFIGURATION, DONOTDELETE").text.sourceText.value))
        var timeToPaste = app.project.activeItem.time;
        for(x=0; x<compSelected.selectedLayers.length; x++){
            var selectedLayer = compSelected.selectedLayers[x];
            this.SetYJoystick_expression(selectedLayer)
            MarkerDown = new MarkerValue("Fall_"+String(FallIndex));
            MarkerDown.duration = 0.8
            selectedLayer.marker.setValueAtTime(timeToPaste, MarkerDown);
        }
    }
}
/**
 * Установка left Анимации
 */
JOYSTICK.Left = function(){
    if(app.project.activeItem instanceof CompItem){
        var compSelected = app.project.activeItem;
        selfData = JSON.parse(String(compSelected.layer("CONFIGURATION, DONOTDELETE").text.sourceText.value))
        var timeToPaste = app.project.activeItem.time;
        for(x=0; x<compSelected.selectedLayers.length; x++){
            var selectedLayer = compSelected.selectedLayers[x];
            this.SetXJoystick_expression(selectedLayer,compSelected)
            this.SetYJoystick_expression(selectedLayer)
            selectedLayer.marker.setValueAtTime(timeToPaste, JOYSTICK.MarkerLeft());
        }
    }
}
/**
 * Установка right маркера
 */
JOYSTICK.Right = function(){
    if(app.project.activeItem instanceof CompItem){
        var compSelected = app.project.activeItem;
        selfData = JSON.parse(String(compSelected.layer("CONFIGURATION, DONOTDELETE").text.sourceText.value))
        var timeToPaste = app.project.activeItem.time;
        for(x=0; x<compSelected.selectedLayers.length; x++){
            var selectedLayer = compSelected.selectedLayers[x];
            this.SetXJoystick_expression(selectedLayer,compSelected)
            this.SetYJoystick_expression(selectedLayer)
            selectedLayer.marker.setValueAtTime(timeToPaste, JOYSTICK.MarkerRight());
        }
    }
}
/**
 * Установка Win маркера
 */
JOYSTICK.Win = function(){
    if(app.project.activeItem instanceof CompItem){
        var compSelected = app.project.activeItem;
        var selectedItems = compSelected.selectedLayers;
        // var selfSpin = new Spin(compSelected.name, compSelected);
        // selfData = JSON.parse(String(compSelected.layer("CONFIGURATION, DONOTDELETE").text.sourceText.value))
        selfSlot = new Spin(new AutoSlot(), {name: compSelected.name})
        Lines_ = {}
        for(k = 0; k < selfSlot.Lines.length; k++){
            Lines_[selfSlot.Lines[k].LineNull.name] = selfSlot.Lines[k]
        }
        var timeToPaste = app.project.activeItem.time;
        compares = []
        analitic = {}
        for(x=0; x<selectedItems.length; x++){
            var selectedLayer = selectedItems[x];
            if(selectedLayer.timeRemapEnabled == false){
                selectedLayer.timeRemapEnabled = true
                selectedLayer.timeRemap.expressionEnabled = true
            }
            selectedLayer.timeRemap.expression = EXPRESSIONS_V2.element_Win_Joystick()
            selectedLayer.marker.setValueAtTime(timeToPaste, JOYSTICK.MarkerWin());
            
            // HOW DO IT?
            Analyze = {"Indexes": {"left": 0, "right": 0, "Up": 0, "Fall": 0, "Win": 0}}
            for(markerKey = 1; markerKey <= selectedLayer.property("Marker").numKeys; markerKey++){
                marker = selectedLayer.property("Marker").keyValue(markerKey);
                Analyze["Indexes"][marker.comment.split("_")[0]] = parseInt(Analyze["Indexes"][marker.comment.split("_")[0]]) + parseInt(marker.comment.split("_")[0] == undefined ? 1 : marker.comment.split("_")[1]);
                alert(marker.comment)
                alert()
            }
            curName = selectedLayer.name.split("_");
            line = parseInt(curName[1])+(Analyze["Indexes"]["right"] - Analyze["Indexes"]["left"])
            compares.push({"SourceName": selectedLayer.name, "JoystickName": ("Line_" + String(line) + "_Item_" + String(item))})
            Lines_["Line_" + String(line)].addElement()
            g = compSelected.selectedLayers[0]
            m = new MarkerValue("Fall_"+String(Analyze["Indexes"]["Fall"] - Analyze["Indexes"]["Up"]))
            m.duration = 0.8
            g.marker.setValueAtTime(timeToPaste - 1,m)
            item = parseInt(curName[3])+(Analyze["Indexes"]["Up"] - Analyze["Indexes"]["Fall"] < 0 ? 0 : Analyze["Indexes"]["Up"] - Analyze["Indexes"]["Fall"])
            if(analitic[line] == undefined){
                analitic[line] = [item]
            }
            else{analitic[line].push(item)}
        }
        alert(JSON.stringify(analitic))
        for(x in analitic){
            analitic[x].sort()
            for(i = 0; i<analitic[x].length; i++){
                if(analitic[x][i] == undefined){continue}
                if(analitic[x].indexOf(analitic[x][i] + 1) != -1){
                    delete analitic[x][i]
                    continue
                }
                indent = 0;
                while(true){
                    indent = indent+1
                    nameItem = "Line_" + (x) + "_Item_" + (analitic[x][i]+indent)
                    if(analitic[x][i]+indent == (analitic[x][i+1])){break;}
                    if(compSelected.layer(nameItem) == null){break;}
                    // alert("Спускаю " + (nameItem) + " на " + (i+1) + " элементов вниз")
                    fallsLayer = compSelected.layer(nameItem)
                    this.SetYJoystick_expression(fallsLayer)
                    MarkerDown = new MarkerValue("Fall_"+String(i+1));
                    MarkerDown.duration = 0.8
                    fallsLayer.marker.setValueAtTime(timeToPaste + 0.3 + (0.1 * indent), MarkerDown);
                }
            }
        }
    }
}
/**
 * Очистка
 * @param {String} name 
 */
JOYSTICK.Clear = function(name){
    compSelected = app.project.activeItem;
    if(name != null || name != undefined){
        //logic
    } 
    if(compSelected instanceof CompItem){
        selfData = JSON.parse(String(compSelected.layer("CONFIGURATION, DONOTDELETE").text.sourceText.value))
        for(x=1; x<=compSelected.numLayers; x++){
            var selectedLayer = compSelected.layer(x);
            marker = selectedLayer.property("Marker")
            for(markerKey = 1; markerKey <= marker.numKeys; markerKey++){
                marker.removeKey(markerKey);
            }
        }
    }
}
