/**
 * Создание нового спина, работа с middleWare и UI
 */
async function addSpin(){
    if(Object.keys(MAINDATA.Elements).length > 0){
        if(Object.keys(MAINDATA.Spins).length > 0){
            await copySpin();
        }
        else{
            await adobeMiddleWare_.addSpin();
        }
        await adobeMiddleWare_.Analyze();
        createGrid(Object.values(MAINDATA.Spins).slice(-1)[0])
        createOrUpdateDropdownSpinItems();
        csInterface.evalScript(`Helper.findCompByName("Spin_${Object.keys(MAINDATA.Spins).length - 1}", Helper.findFolderByName("Spins")).openInViewer().setActive();`)
    }
    else{
        CEPException.SymbolCompsNotFound();
    }
}

async function copySpin(){
    let lastspin = MAINDATA.Spins["Spin_"+(Object.keys(MAINDATA.Spins).length-1)];
    console.log();
    await adobeMiddleWare_.copySpin(lastspin);
} 

async function removeSpin(){
    if(Object.keys(MAINDATA.Spins).length > 0){
        Array.from(document.getElementById('grids-container').children).forEach((item)=>{
            if(item.classList.contains('active')){
                adobeMiddleWare_.removeSpin(item.id);
                item.remove();
                let spinSelectorOptionsList = document.getElementById("spinSelector").children.item(0);
                for(x = 0; x < spinSelectorOptionsList.length; x++){
                    if(spinSelectorOptionsList[x].innerText === item.id){ spinSelectorOptionsList[x].remove() }
                }
            }
        })
        await adobeMiddleWare_.Analyze();
    }
    else{
        CEPException.NoFoundSpinToRemove();
    }
}