class SpinDataType {
    constructor(
        name,
        width,
        height,
        elsByHeight,
        elsByWidth,
        spacingHorizontal = 0,
        spacingVertical   = 0,
        slowWin,
        slowLines,
        slowMain,
        winCombos = []
    ) {
        if (!name || name.split("Spin_").length === 1) {
            throw new Error("Имя спина не может быть пустым или должно соответствовать правилу Spin_*");
        }
        this.name = name;

        if (width < 0)     throw new Error("Ширина спина не может быть отрицательной");
        this.width = width;

        if (height < 0)    throw new Error("Высота спина не может быть отрицательной");
        this.height = height;

        if (elsByHeight < 0) throw new Error("Количество элементов по высоте не может быть отрицательным");
        this.elsByHeight = elsByHeight;

        if (elsByWidth < 0)  throw new Error("Количество элементов по ширине не может быть отрицательным");
        this.elsByWidth = elsByWidth;

        this.spacingHorizontal = spacingHorizontal;
        this.spacingVertical   = spacingVertical;

        this.slowLines = slowLines;
        this.slowMain  = slowMain;
        this.slowWin   = slowWin;

        this.winCombos = Array.isArray(winCombos) ? winCombos : [];
    }

    static fromData(name, resizeData, configData, winCombos = []) {
        const { width, height, elsByHeight, elsByWidth } = resizeData;
        const { slowWin, slowLines, slowMain, spacingHorizontal = 0, spacingVertical = 0 } = configData;

        return new SpinDataType(
            name, width, height, elsByHeight, elsByWidth,
            spacingHorizontal, spacingVertical,
            slowWin, slowLines, slowMain,
            Array.isArray(winCombos) ? winCombos : []
        );
    }
}