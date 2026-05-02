function Spin(self, SpinData){
    this.self = self;
    this.name = SpinData.name;
    this.Lines = []
    this.MainLine = null;
    
    //Если уже существует композиция и её передали аргументом self
    if(self instanceof CompItem){
        this.SpinComp = self
        this.getLines();
        this.configurationLayer = this.SpinComp.layer("CONFIGURATION, DONOTDELETE")
        this.data = SpinDataType.parse(String(this.configurationLayer.text.sourceText.value))
        return;
    }
    // если не существует композиции
    if(Helper.findCompByName(this.name, this.self.SpinsFolder) == null){
        this.data = SpinData
        if(this.data == undefined){
            alert("error at spin create, not found configuration data")
            return;
        }
        this.SpinComp = this.self.SpinsFolder.items.addComp(this.name, this.data.width, this.data.height, 1, 20, 25);
        this.configurationLayer = this.SpinComp.layers.addText()
            this.configurationLayer.name = "CONFIGURATION, DONOTDELETE"
            this.configurationLayer.text.sourceText.setValue(this.data.stringify()) 
        this.configureSpin()
    }
    // если не передали через self но смогли найти
    else{
        this.SpinComp = Helper.findCompByName(this.name, this.self.SpinsFolder)
        this.getLines();
        this.configurationLayer = this.SpinComp.layer("CONFIGURATION, DONOTDELETE")
        this.data = SpinDataType.parse(String(this.configurationLayer.text.sourceText.value))
    }
}
/**
    Функция конфигурации Spin, создает маркеры начала и конца анимации
    Добавляет спин в Slot, закидывает нужные зависимости через Slider
*/
Spin.prototype.configureSpin = function(){
    if(this.self.SpinsFolder === null || this.self.SlotComp === null){
        return
    }
    if(this.SpinComp == null){
        return
    }
    
    var myMarker = new MarkerValue("StartAnimation");
        this.SpinComp.markerProperty.setValueAtTime(2, myMarker)
    
    var myMarker = new MarkerValue("EndAnimation");
        this.SpinComp.markerProperty.setValueAtTime(5, myMarker)

    var footage = this.self.SlotComp.layers.add(this.SpinComp)

    var spaceTime = Helper.lengthOfDict(this.self.spinsCollection)
        footage.startTime = 5*spaceTime
    
    var sliderEffect = footage.effect.addProperty("ADBE Slider Control");
        sliderEffect.name = "yPosition"
    var sliderProp = sliderEffect.property("ADBE Slider Control-0001");
        sliderProp.setValueAtTime(0+footage.startTime,0);
        sliderProp.setValueAtTime(1.5+footage.startTime,this.SpinComp.height+100);
        sliderProp.setValueAtTime(2+footage.startTime,this.SpinComp.height);

        sliderProp.setValueAtTime(4+footage.startTime,this.SpinComp.height);
        sliderProp.setValueAtTime(4.5+footage.startTime,this.SpinComp.height-100);
        sliderProp.setValueAtTime(6+footage.startTime,this.SpinComp.height*2);

    for(var i=1; i<=sliderProp.numKeys; i++){
        
        sliderProp.setInterpolationTypeAtKey(i, KeyframeInterpolationType.BEZIER, KeyframeInterpolationType.BEZIER);
    }
    var sliderEffect = footage.effect.addProperty("ADBE Point Control");
        sliderEffect.name = "Scale";
        sliderEffect.property("ADBE Point Control-0001").setValue([100,100]);


    var sliderEffect = footage.effect.addProperty("ADBE Slider Control");
        sliderEffect.name = "Rotation";
}
/**
    Функция ресайза спина (Заменяется Ширина и высота композиции, кол-во элементов по высоте и ширине)
    newDataComp = {"width": ?, "height": ?, "ElementsByHeight": ?, "ElementsByWidth": ?}
    newDataElements = {"spacingVertical": 40, "spacingHorizontal": 40, "slowMain": 0, "slowLines": 0}
*/
Spin.prototype.resize = function(resizeData){

    if(this.data.elsByHeight > resizeData.elsByHeight){
        for(k = 0; k<this.data.elsByHeight-resizeData.elsByHeight; k++){
            this.removeElementsFromLines()
        }
    }
    if(this.data.elsByWidth > resizeData.elsByWidth){
        for(k=0; k<this.data.elsByWidth-resizeData.elsByWidth; k++){
            this.removeLine()
        }
    }
    if(this.data.elsByHeight < resizeData.elsByHeight){
        for(k = 0; k<resizeData.elsByHeight-this.data.elsByHeight; k++){
            this.addElementsToLines()
        }
    }
    this.data.elsByHeight = resizeData.elsByHeight
    if(this.data.elsByWidth < resizeData.elsByWidth){
        for(var k = 0; k<resizeData.elsByWidth-this.data.elsByWidth; k++){
            this.addLine()
        }
    }

    this.data = this.data.update(resizeData);
    this.configurationLayer.text.sourceText.setValue(this.data.stringify());
    this.SpinComp.width = this.data.width;
    this.SpinComp.height = this.data.height;

    this.changeSpinElementConfig();    
}
/**
    Функция добавления новой линии
*/
Spin.prototype.addLine = function(){
    var thisIndex = this.Lines.length;
    this.Lines.push(new Line("Line_"+String(thisIndex), this));
    for(var x = 0; x < this.data.elsByHeight; x++){
        this.Lines[thisIndex].addElement();
    }
    this.Lines[thisIndex].LineNull.moveToBeginning();
}
/**
    Функция добавления элемента к каждой линии спина
*/
Spin.prototype.addElementsToLines = function(){
    for(var x=0; x<this.Lines.length; x++){
        this.Lines[x].addElement()
    }
}
/**
    Функция создание сетки элементов.
    Параметры берутся из Spin класса this.data
*/
Spin.prototype.createGrid = function(data){
    if(this.self.SymbolCompsFolder.numItems == 0){
        alert("ERROR: NOT FOUND SYMBOL COMPS")
        return;
    }
    else{
        for(var y = 0; y  < this.data.elsByWidth; y++){
            this.addLine()
        }
        this.createMainLine();
    }
    
}
/**
    Функция создание нуля Main, связывающего нас с композицией Slot 
*/
Spin.prototype.createMainLine = function(){
    if(this.SpinComp.layer("MainLine") != null){
        return;
    }
    var nullLayer = this.SpinComp.layers.addNull();
        nullLayer.name = "MainLine";
        nullLayer.transform.position.dimensionsSeparated = true;
        nullLayer.transform.xPosition.setValue(0);
        nullLayer.transform.yPosition.setValue(0);
        nullLayer.transform.yPosition.expressionEnabled = true;
        nullLayer.transform.yPosition.expression = EXPRESSIONS_V2.mainLine_yPosition(this.self.SlotComp.name);
        nullLayer.transform.scale.expression = EXPRESSIONS_V2.mainLine_scale(this.self.SlotComp.name);
        nullLayer.transform.rotation.expression = EXPRESSIONS_V2.mainLine_rotation(this.self.SlotComp.name);

    var slider = nullLayer.effect.addProperty("ADBE Slider Control");
        slider.name = "ScaleControl";
        slider.property("ADBE Slider Control-0001").setValue(100);
    
    this.MainLine = nullLayer;
    
}
/*
    Функция подгрузки старых данных (а именно линий элементов)
*/
Spin.prototype.getLines = function(){
    var allNumbers = []
    for(g=1; g<=this.SpinComp.layers.length;g++){
        var t = this.SpinComp.layer(g).name.split("_")
        if(t.length == 2){
            if(t[0] == "Line"){
                
                allNumbers.push(parseInt(t[1]))
                // this.Lines.push(new Line(this.SpinComp.layer(g).name, this))
            }
        }
    };
    for(var g=0; g < allNumbers.length; g++){
        if(this.SpinComp.layer("Line_"+String(g)) != undefined){
            this.Lines.push(new Line("Line_"+String(g), this))
        }
    }
    this.MainLine = this.SpinComp.layer("MainLine")
}
/*
    Функция установки конфигурации выигрышного элемента
    element передается из внешней функции
    DisposalIndex передается из внешней функции и отвечает за порядковый номер выигрышного элемента
*/
Spin.prototype.setWinElementConfiguration = function(element, DisposalIndex){
    if(element.canSetTimeRemapEnabled){
        element.timeRemap.expression = EXPRESSIONS_V2.element_winAnimation(this.data.slowWin, DisposalIndex);
    }
}
/*
    Функция установки конфигурации выигрышных элементов Spin
    "winItems":[
                    {
                        "Line":3,
                        "Item":5
                    },
                    {
                        "Line":3,
                        "Item":7
                    }
                ]
    "winItem": "? (string, name of Item)"
*/
Spin.prototype.setWinGrid = function(winData){
    if(winData.winElement == null || winData.winElements == null){
        return;
    }
    var winElement = this.self.SymbolCompsFolder.itemByName(winData.winElement)
    for(key=0;key<winData.winElements.length;key++){
        x = winData.winElements[key][0]
        y = winData.winElements[key][1]
        var nm = "Line_" + String(x) + "_Item_" + String(y)
        var element = this.SpinComp.layer(nm);
        if(element == null){
            continue;
        }
        this.setWinElementConfiguration(element, x);
        this.setWinElement(element, winElement);
        element.moveToBeginning()       
    }

    this.data = this.data.update(winData);
    this.configurationLayer.text.sourceText.setValue(this.data.stringify());
}

Spin.prototype.setWinGridClear = function(winData){
    this.clearSpin(this.data.winElement, true);
    this.setWinGrid(winData);
    this.clearSpin(this.data.winElement, false);
}

/**
 * Функция очисти игрового поля от определенного спина
 * @param {String} fromElementName Какой элемент будет сброшен и заменен
 * @param {Boolean} all true - замена происходит со всеми элементами, false - только с выигрышными
 */
Spin.prototype.clearSpin = function(fromElementName, all){
    for(var x=1; x<this.SpinComp.numLayers; x++){
        //Если элемент выигрышен
        if(this.SpinComp.layer(x).timeRemap.expression != "0"){
            if(all){
                this.resetElement(this.SpinComp.layer(x), fromElementName)
            }
        }
        //Если элемент не выигрышен, но его имя совпадает с тем от которого мы чистим
        else if(this.SpinComp.layer(x).source.name == fromElementName){
            this.resetElement(this.SpinComp.layer(x), fromElementName)
        }
    }
}
/*
    Установка (replaceSource) элемента на выигрышныйэ элемент
*/
Spin.prototype.setWinElement = function(elementCurrent, elementWin){
    if(elementCurrent == null || elementWin == null){
        return
    }
    elementCurrent.replaceSource(elementWin, false)
    elementCurrent.timeRemapEnabled = false;
    elementCurrent.timeRemapEnabled = true;
}
/*
    Функция сбрасывающая элемент к изначальной конфигурации и заменяющая его, учитывая исключение excluding (имя элемента)
    excluding может быть null, тогда исключением станет имя самого элемента которого мы заменяем
*/
Spin.prototype.resetElement = function(element, excluding){
    if(excluding == undefined || excluding == null){
        excluding = element.name
    }
    do{
        element.replaceSource(this.self.createElement(), false);
    }while(element.name == excluding)
    if(element.canSetTimeRemapEnabled){
        element.timeRemapEnabled = false;
        element.timeRemapEnabled = true;    
        element.timeRemap.expression = '0';
    }
}
/*
    Функция обеспечивающая сброс всех элементов спина
*/
Spin.prototype.resetGrid = function(){
    for(var line_ = 0; line_ < this.Lines.length; line_++){
        this.Lines[line_].resetLine()
    }
}
/* 
    Функция для удаления линии
*/
Spin.prototype.removeLine = function(){
    var cashe = this.Lines.pop()
    cashe.remove();
}
/* 
    Функция для удаления последних элементов из линий
*/
Spin.prototype.removeElementsFromLines = function(){
    for(var x=0; x<this.Lines.length; x++){
        this.Lines[x].removeLastElement();
    }
}
/* 
    Функция меняющая конфигурацию элементов спина
    configureData = {"spacingVertical": 40, "spacingHorizontal": 40, "slowMain": 0, "slowLines": 0}
*/
Spin.prototype.changeSpinElementConfig = function(configureData){
    if(configureData != undefined){
        this.data = this.data.update(configureData)
        this.configurationLayer.text.sourceText.setValue(this.data.stringify())
    }
    for(var l = 0; l < this.Lines.length ;l++){
        // alert(this.Lines[l].LineNull.name)
        this.Lines[l].changeLineElementsConfig()
    }
}
Spin.prototype.reverse = function(){
    for(var l = this.Lines.length-1; l > -1; l--){
        this.Lines[l].reverse();
    }
    this.MainLine.moveToBeginning();
    this.configurationLayer.moveToEnd();
}

// new Spin().resize()
// new Spin().reverse()
// new Spin().setWinGrid()

