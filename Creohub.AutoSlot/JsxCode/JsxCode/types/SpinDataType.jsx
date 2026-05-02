function SpinDataType(
    name,
    width,
    height,
    elsByHeight,
    elsByWidth,
    spacingHorizontal,
    spacingVertical,
    winElement,
    winElements,
    slowWin,
    slowLines,
    slowMain
){
    if (name === null || name === undefined || name.split("Spin_").length === 1) {
        throw new Error("Имя спина не может быть пустым или должно соответствовать правилу Spin_*");
    }
    this.name = name;

    if (width < 0) {
        throw new Error("Ширина спина не может быть отрицательной");
    }
    this.width = width;

    if (height < 0) {
        throw new Error("Высота спина не может быть отрицательной");
    }
    this.height = height;

    if (elsByHeight < 0) {
        throw new Error("Количество элементов по высоте спина не может быть отрицательным");
    }
    this.elsByHeight = elsByHeight;

    if (elsByWidth < 0) {
        throw new Error("Количество элементов по ширине спина не может быть отрицательным");
    }
    this.elsByWidth = elsByWidth;

    this.spacingHorizontal = spacingHorizontal || 0; // Значение по умолчанию: 0
    this.spacingVertical = spacingVertical || 0; // Значение по умолчанию: 0

    if (!Array.isArray(winElements)) {
        throw new Error("Выигрышные элементы должны быть массивом");
    }
    this.winElements = winElements;

    if (typeof winElement !== "string") {
        throw new Error("Ошибка: winElement должен быть строкой");
    }
    this.winElement = winElement;

    this.slowLines = slowLines
    this.slowMain = slowMain
    this.slowWin = slowWin
}


/**
 * Функция создающая текстовое представление данных о спине для последующей записи в текстовый слой в композиции спина
 */
SpinDataType.prototype.stringify = function(){
    var a = '{"name": "' + this.name + '",'
    a = a + '"width": ' + this.width + ','
    a = a + '"height": ' + this.height + ','
    a = a + '"elsByHeight": ' + this.elsByHeight + ','
    a = a + '"elsByWidth": ' + this.elsByWidth + ','
    a = a + '"spacingHorizontal": ' + this.spacingHorizontal + ','
    a = a + '"spacingVertical": ' + this.spacingVertical + ','
    a = a + '"winElement": "' + this.winElement + '",'
    a = a + '"winElements": ' + JSON.stringify(this.winElements) + ','
    a = a + '"slowMain": ' + this.slowMain + ',' 
    a = a + '"slowWin": ' + this.slowWin + ',' 
    a = a + '"slowLines": ' + this.slowLines + "}"
    return a;
}

/**
 * Функция для перегона данных о спине, подгруженных в текстовом файле в объект SpinDataType
 */
SpinDataType.parse = function(data){
    if (typeof data === "string") {
        data = JSON.parse(data);
    }

    return new SpinDataType(
        data.name,
        data.width,
        data.height,
        data.elsByHeight,
        data.elsByWidth,
        data.spacingHorizontal,
        data.spacingVertical,
        data.winElement,
        data.winElements,
        data.slowWin,
        data.slowLines,
        data.slowMain
    );
}

SpinDataType.prototype.analyzeParse = function(){
    var a = {
        name: this.name,
        
        winElements: this.winElements,
        winElement: this.winElement,
        
        width: this.width,
        elsByWidth: this.elsByWidth,
        spacingHorizontal: this.spacingHorizontal,

        height: this.height,
        elsByHeight: this.elsByHeight,
        spacingVertical: this.spacingVertical,

        slowLines: this.slowLines,
        slowMain: this.slowMain,
        slowWin: this.slowWin
    }


    return a;
}

SpinDataType.prototype.update = function(data){
    thisData = JSON.parse(this.stringify())
    for(x in data){
        if(thisData[x] !== undefined){
            thisData[x] = data[x]
        }else{
            alert("undefined in update SpinDataType")
        }
    }
    return SpinDataType.parse(thisData)
}