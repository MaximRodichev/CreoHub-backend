const csInterface = new CSInterface();
const targetFolderName = "GIFs";
let lastItemCount = -1;
let spinCreated = false;

function checkForNewFiles() {
    if(!spinCreated) {
        csInterface.evalScript(
            `Helper.findFolderByName("${targetFolderName}") ? Helper.findFolderByName("${targetFolderName}").numItems : -1`,
            async function (itemCount) {
                if (itemCount == -1) {
                    console.log("Folder not found: " + targetFolderName);
                } else {
                    if (itemCount !== lastItemCount && lastItemCount !== -1) {
                        csInterface.evalScript("new AutoSlot().wrapElements()");
                        let response = await adobeMiddleWare_.Analyze();
                        spinCreated = response.Spins.length > 0
                        initializeSymbolsScroll();
                        createOrUpdateDropdownWinItems();
                    }
                    lastItemCount = itemCount;
                }
                setTimeout(checkForNewFiles, 1000); // Проверка каждую секунду
            }
        );
    }
}

// Запускаем мониторинг
checkForNewFiles();
