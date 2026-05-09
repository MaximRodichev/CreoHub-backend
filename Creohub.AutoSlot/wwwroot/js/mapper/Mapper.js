class Mapper{
    static AutoSlotMapping(data){
        return new AutoSlotDataType(
            data.name,
            data.Elements,
            data.Spins
        )
    }

    static SpinDataMapping(data){
        var {name, width, height, elsByHeight, elsByWidth, spacingVertical, spacingHorizontal, slowMain, slowWin, slowLines, winCombos} = data
        return new SpinDataType(
            name,
            width,
            height,
            elsByHeight,
            elsByWidth,
            spacingHorizontal,
            spacingVertical,
            slowWin,
            slowLines,
            slowMain,
            Array.isArray(winCombos) ? winCombos : []
        )
    }
}