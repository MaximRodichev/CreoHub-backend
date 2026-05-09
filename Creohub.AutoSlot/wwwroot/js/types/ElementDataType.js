class ElementDataType{
    constructor(
        name,
        width,
        height,
        filePath
    ){
        this.name     = name;
        this.width    = width;
        this.height   = height;
        this.filePath = filePath != null ? filePath : null;
    }
}