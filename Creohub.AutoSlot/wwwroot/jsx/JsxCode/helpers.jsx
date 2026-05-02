/*
    Глобальная переменная с дополнительными функциями
*/
var Helper = {
    /**
        Поиск папки в трее АЕ проекта по имени. [folderName]
        Возможность поиска из под другой папки. [fromFolder]
    */
    findFolderByName: function(folderName, fromFolder){
        if(fromFolder == undefined || fromFolder == null){
            fromFolder = app.project
        }
        try{
            for(var x=1; x <= fromFolder.numItems; x++){
                if(fromFolder.item(x) instanceof FolderItem && fromFolder.item(x).name == folderName){
                    return fromFolder.item(x);
                }
            }
        }
        catch(error){
            alert("Exception handled at findFolderByNameFunction");
            alert(error);
            return null;
        }
        return null;
    },
    /**
        Поиск композиции в трее АЕ проекта по имени. [compName]
        Возможность поиска из под другой композиции. [parentItem]
    */
    findCompByName: function(compName, parentItem) {
        try{
            // Check if the parentItem is valid
            
            if (parentItem instanceof CompItem && parentItem.name === compName) {
                return parentItem;
                
            } else if (parentItem instanceof FolderItem) {
                // Iterate through the items in the folder
                for (var i = 1; i <= parentItem.numItems; i++) {
                    var currentItem = parentItem.item(i);
                    // Recursively search for compositions within items (including folders)
                    var foundComp = this.findCompByName(compName, currentItem);
                    if (foundComp !== null) {
                        return foundComp;
                    }
                }
            }
        }catch(error){alert("Exception handled at findCompByNameFunc"); alert(error);}
        return null;
    },
    /**
        Получение рандомного значения из диапазона
    */
    random: function(min, max) {
        return Math.floor(Math.random() * (max - min)) + min;
    },
    lengthOfDict: function(item){
        var len = 0;
        for (var key in item) {
            if (this.hasOwnProperty.call(item,key)) {
                len++;
            }
        }
        return len 
    },
    /**
     * Проверка на undefined
     * @param {any} item Переменная для проверки
     * @param {value} undefinedValue Значение если undefined=true
     * @returns 
     */
    undefinedCheker: function(item, undefinedValue){
        return item == undefined ? item : undefinedValue 
    }
};

/**
    JSON
*/
var JSON = {
    parse: function (sJSON) { return eval("(" + sJSON + ")"); },
    stringify: function (vContent) {
        if (vContent instanceof Object) {
            var sOutput = "";
            if (vContent.constructor === Array) {
                for (var nId = 0; nId < vContent.length; sOutput += this.stringify(vContent[nId]) + ",", nId++);
                return "[" + sOutput.substr(0, sOutput.length - 1) + "]";
            }
            if (vContent.toString !== Object.prototype.toString) {
                return "\"" + vContent.toString().replace(/"/g, "\\$&") + "\"";
            }
            for (var sProp in vContent) {
                sOutput += "\"" + sProp.replace(/"/g, "\\$&") + "\":" + this.stringify(vContent[sProp]) + ",";
            }
            return "{" + sOutput.substr(0, sOutput.length - 1) + "}";
        }
        return typeof vContent === "string" ? "\"" + vContent.replace(/"/g, "\\$&") + "\"" : String(vContent);
    },
    write: function(data, path){
        var jsonString = this.stringify(data, null, "\t");
        
        var jsonFile = new File(path);
        
        jsonFile.open("w");
        
        jsonFile.write(jsonString);
        
        jsonFile.close();
    },
    read: function(path){
        
        jsonFile = File(path);
        
        jsonFile.open("r");
        
        var jsonString = jsonFile.read();
        
        jsonFile.close();
        return this.parse(jsonString);
    }
};

/**
    Дописанная функция, возвращающая все элементы папки, как массив объектов
*/
FolderItem.prototype.getArray = function(){
    var returns = []
    for(var x=1; x<=this.numItems;x++){
        
        returns.push(this.item(x));
    }
    return returns
}
/**
    Дописанная функция, возвращающая элемент по имени
*/
FolderItem.prototype.itemByName = function(name){
    for(var folderItemIndex_=1; folderItemIndex_<=this.numItems; folderItemIndex_++){
        if( this.item(folderItemIndex_).name == name ){
            return this.item(folderItemIndex_)
        }
    }
}
/**
    Дописанная функция, возвращающая все элементы коллекции слоев, как массив объектов слоев
*/
LayerCollection.prototype.getArray = function(){
    var returns = []
    for(var x=1; x<=this.length;x++){
        
        returns.push(this[x]);
    }
    return returns
}