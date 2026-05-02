class SpinDataType {
    constructor(
        name,
        width,
        height,
        elsByHeight,
        elsByWidth,
        spacingHorizontal = 0,
        spacingVertical = 0,
        winElement,
        winElements,
        slowWin,
        slowLines,
        slowMain
    ) {
        if (!name || name.split("Spin_").length === 1) {
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

        this.spacingHorizontal = spacingHorizontal;
        this.spacingVertical = spacingVertical;

        if (!Array.isArray(winElements)) {
            throw new Error("Выигрышные элементы должны быть массивом");
        }
        this.winElements = winElements;

        if (typeof winElement !== "string") {
            throw new Error("Ошибка: winElement должен быть строкой");
        }
        this.winElement = winElement;

        this.slowLines = slowLines;
        this.slowMain = slowMain;
        this.slowWin = slowWin;
    }

    // Factory method for alternate initialization
    static fromData(name, resizeData, configData, winData) {
        const { width, height, elsByHeight, elsByWidth } = resizeData;
        const { slowWin, slowLines, slowMain, spacingHorizontal = 0, spacingVertical = 0 } = configData;
        const { winElement, winElements } = winData;

        return new SpinDataType(
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
        );
    }
}