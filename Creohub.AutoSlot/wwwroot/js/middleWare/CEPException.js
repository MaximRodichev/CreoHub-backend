/**
 * Класс реализующий вызов Alert функций, связанных с JSX
 */
class CEPException {
    /**
     * "Композиции элементов не были найдены в папке AutoSlot/Symbols/SymbolComps"
     */
    static SymbolCompsNotFound(){
        alert("Композиции элементов не были найдены в папке AutoSlot/Symbols/SymbolComps")
    }
    /**
     * "В папке AutoSlot/Symbols/GIF's отстутсвуют файлы элементов"
     */
    static GIFsFolderNotFound(){
        alert("В папке AutoSlot/Symbols/GIF's отстутсвуют файлы элементов");
    }
    
    static NoFoundSpinToRemove(){
        alert("Ломать не строить, нечего ломать, создай спин сначала")
    }
}