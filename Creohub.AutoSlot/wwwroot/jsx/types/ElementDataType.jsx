function ElementDataType(
    name,
    width,
    height,
    filePath
){
    this.name     = name;
    this.width    = width;
    this.height   = height;
    this.filePath = (filePath != undefined && filePath != null) ? filePath : null;
}