/*
    Класс Линии, управляется классом Spin
*/
function Line(name, self){
    this.self = self
    this.LineNull = this.self.SpinComp.layer(name)
    this.LineElements = []

    if(this.LineNull != undefined){
        this.getElements()
    }
    else{
        this.LineNull = this.self.SpinComp.layers.addNull()
        this.LineNull.name = name
        this.configure()
    }
}
/*
    Функция конфигурирования линии в соответсвии с конфигурационным  словарем
*/
Line.prototype.configure = function(){
    var configureData = this.self.data
    var lineObject = this.LineNull.transform;
    var thisSlow = configureData.slowLines * parseInt(this.LineNull.name.split("_")[1]);
    
    lineObject.position.dimensionsSeparated = true;
    
    // lineObject.xPosition.expressionEnabled = true;
    lineObject.xPosition.setValue((this.self.SpinComp.width - configureData.spacingHorizontal) / (configureData.elsByWidth+1) * (parseInt(this.LineNull.name.split("_")[1])+1) + configureData.spacingHorizontal/2);
    // lineObject.xPosition.expression = EXPRESSIONS_V2.nullOfLine_xPosition(this.self.self.name)

    lineObject.yPosition.expressionEnabled = true;
    //self + null_ + spacing * (countLines/2+1)'
    lineObject.yPosition.setValue(-parseInt(configureData.spacingVertical));
    lineObject.yPosition.expression = EXPRESSIONS_V2.nullOfLine_yPosition_NoControlled(thisSlow)

    lineObject.scale.expressionEnabled = true;
    lineObject.scale.expression = EXPRESSIONS_V2.nullOfLine_scale_NoControlled(thisSlow)

    lineObject.rotation.expressionEnabled = true;
    lineObject.rotation.expression = EXPRESSIONS_V2.nullOfLine_rotation_NoControlled(thisSlow, this.self.self.SlotComp.name)
}
/*
    Функция подгрузки элементов относящихся к этой линии
*/
Line.prototype.getElements = function(){
    var allNumbers = []
    for(q = 1;q <= this.self.SpinComp.layers.length; q++){
        var t = this.self.SpinComp.layer(q).name.split("_Item_")
        if(t.length == 2){
            
            allNumbers.push(parseInt(t[1]))
        }
    }
    for(var q = 0; q <= Math.max.apply(null, allNumbers); q++){
        var t = this.self.SpinComp.layer(this.LineNull.name + "_Item_" + String(q))
        this.LineElements.push(t)
    }
}
/*
    Функция добавления элемента в конец линии
*/
Line.prototype.addElement = function(){
    var footage = this.self.SpinComp.layers.add(this.self.self.createElement());
        footage.name = this.LineNull.name + "_Item_" + String(this.LineElements.length);
        footage.moveAfter(this.LineNull)
    this.configureElement(footage, this.self.data);
    this.LineElements.push(footage);
}
/*
    Функция конфигурирования одиночного элемента
    ConfigureData = {"spacingVertical": 40, "spacingHorizontal": 40, "slowMain": 0, "slowLines": 0}
*/
Line.prototype.configureElement = function(element){
    var configureData = this.self.data
    var target = this.LineElements.length == 0 ? this.LineNull.name : this.LineElements[this.LineElements.length - 1].name
    var paramCoef = target.indexOf("Item") == -1 ? 0 : parseInt(target.split("_Item_")[1])
    element.transform.position.dimensionsSeparated = true;
    if(element.canSetTimeRemapEnabled){
        element.timeRemapEnabled = true;
        element.timeRemap.expression = '0';
    }
    // element.transform.xPosition.expressionEnabled = true;
    //(thisComp.width - spacing) / countLines * (indexThis+1) + spacing/2;
    element.transform.xPosition.setValue((this.self.SpinComp.width - configureData.spacingHorizontal) / (configureData.elsByWidth+1) * (parseInt(this.LineNull.name.split("_")[1])+1) + configureData.spacingHorizontal/2);
    // element.transform.xPosition.expression = EXPRESSIONS_V2.element_xPosition(this.LineNull.name);
    
    element.transform.yPosition.expressionEnabled = true;
    //start_ = (thisComp.height + spacing) / (countLines+1) + spacing;
    var coefficientY = -(this.self.SpinComp.height - configureData.spacingVertical*2)/(configureData.elsByHeight+1)
    element.transform.yPosition.setValue(coefficientY+(coefficientY*parseInt(element.name.split("_Item_")[1])));
    element.transform.yPosition.expression = EXPRESSIONS_V2.element_yPositionNew(this.LineNull.name, configureData.slowMain*parseInt(element.name.split("_Item_")[1]))

    element.transform.scale.expressionEnabled = true;
    element.transform.scale.expression = EXPRESSIONS_V2.element_scale(this.LineNull.name, configureData.slowMain*paramCoef)
    // "target =" + this.LineElements[this.LineElements.length-1].name + "\/"+ 
    element.transform.rotation.expressionEnabled = true;
    element.transform.rotation.expression = EXPRESSIONS_V2.element_rotation(this.LineNull.name, configureData.slowMain*paramCoef)

    element.motionBlur = true;
}
/*
    Функция смены конфигурации всех элементов относящихся к данной линии
    ConfigureData = {"spacingVertical": 40, "spacingHorizontal": 40, "slowMain": 0, "slowLines": 0}
*/
Line.prototype.changeLineElementsConfig = function(){
    for(var k=0; k<this.LineElements.length; k++){
        if(this.LineElements[k] == null){
            continue
        }
        // alert(this.LineElements[k].name)
        this.changeElementConfig(this.LineElements[k])
    }
    this.configure()
}
/*
    Функция смены конфигурации элемента относящихся к данной линии
    ConfigureData = {"spacingVertical": 40, "spacingHorizontal": 40, "slowMain": 0, "slowLines": 0}
*/
Line.prototype.changeElementConfig = function(element){
    try{
        // alert(element.name)
        var configureData = this.self.data;
        var target = parseInt(element.name.split("_Item_")[1]) == 0 ? 
        this.LineNull.name : 
        this.LineNull.name + "_Item_" + String(parseInt(element.name.split("_Item_")[1]) - 1);

        var paramCoef = target.indexOf("Item") == -1 ? 0 : parseInt(target.split("_Item_")[1])
        
        var coefficientY = -(this.self.SpinComp.height - configureData.spacingVertical*2)/(configureData.elsByHeight+1)
        element.transform.xPosition.setValue((this.self.SpinComp.width - configureData.spacingHorizontal) / (configureData.elsByWidth+1) * (parseInt(this.LineNull.name.split("_")[1])+1) + configureData.spacingHorizontal/2);
        element.transform.yPosition.setValue(coefficientY+(coefficientY*parseInt(element.name.split("_Item_")[1])));
        
        element.transform.yPosition.expression = EXPRESSIONS_V2.element_yPositionNew(this.LineNull.name, configureData.slowMain*parseInt(element.name.split("_Item_")[1]))
        element.transform.scale.expression = EXPRESSIONS_V2.element_scale(this.LineNull.name, configureData.slowMain*paramCoef)
        element.transform.rotation.expression = EXPRESSIONS_V2.element_rotation(this.LineNull.name, configureData.slowMain*paramCoef)
    }
    catch(err){alert(err)}
}
/*
    Функция сброса элементов линии
    Spin.resetElement()
*/
Line.prototype.resetLine = function(){
    for(var elemIndex=0; elemIndex < this.LineElements.length; elemIndex++){
        this.self.resetElement(this.LineElements[elemIndex]);
    }
}
/*
    Удаляет последний элемент линии
*/
Line.prototype.removeLastElement = function(){
    if(this.LineElements.length == 0){
        return;
    }
    var cashe = this.LineElements.pop();
    cashe.remove()
}
/*
    Удаляет всю линию (элементы и сам нулл объект)
*/
Line.prototype.remove = function(){
    for(var x=0; x<this.LineElements.length; x++){
        this.LineElements[x].remove()
    }
    this.LineNull.remove()
}
Line.prototype.reverse = function(){
    // for(x=0; x>this.LineElements.length; x++){
    //     this.LineElements[x].moveToEnd();
    // }
    // alert(this.LineNull.name)
    this.LineNull.moveToEnd();
    for(var x=this.LineElements.length-1; x>-1; x--){
        this.LineElements[x].moveToBeginning();
        // this.LineElements[x].moveAfter(this.LineNull)
    }
}