class Mapper{
    static AutoSlotMapping(data){
        return new AutoSlotDataType(
            data.name,
            data.Elements,
            data.Spins
        )
    }

    static SpinDataMapping(data){
        var {name, width, height, elsByHeight, elsByWidth, spacingVertical, spacingHorizontal, winElement, winElements, slowMain, slowWin, slowLines, winCombos} = data
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
            slowMain,
            winCombos != null ? winCombos : []
        )
    }
}