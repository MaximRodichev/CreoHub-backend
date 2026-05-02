function startJoystick() {
    const grid = document.getElementById("Spin_1");
    const gridItems = Object.values(grid.children[0].children);
    gridItems.forEach(item => {
        item.classList.toggle("joystick");
    });
    const gridsContext = document.getElementById("grids-container-text");
    gridsContext.innerText = "Joystick Items" == gridsContext.innerHTML ? "Select Items" : "Joystick Items"
}

function preLoadJoystick(thisGrid) {
    const grid = getActiveGrid()["choosenGrid"];
    const gridItems = Array.from(grid.children[0].children); // Получаем все элементы
    const thisX = parseInt(thisGrid.dataset.x);
    const thisY = parseInt(thisGrid.dataset.y);


    gridItems.forEach(gridItem => {
        const itemX = parseInt(gridItem.dataset.x);
        const itemY = parseInt(gridItem.dataset.y);

        // Проверяем, является ли элемент соседом (разница по x или y ровно 1)
        if (
            (itemX === thisX && Math.abs(itemY - thisY) === 1) || 
            (itemY === thisY && Math.abs(itemX - thisX) === 1)
        ) {
            gridItem.classList.toggle('preload')
            gridItem.classList.remove('locked')
        }else{
            gridItem.classList.toggle('locked')
        }
    });
}

function submitJoystick(){
    const grid = getActiveGrid()["choosenGrid"];
    const gridItems = Array.from(grid.children[0].children); // Получаем все элементы

    gridItems.forEach(gridItem => {
        const itemX = parseInt(gridItem.dataset.x);
        const itemY = parseInt(gridItem.dataset.y);
        if(gridItem.classList.contains('preload')){
            gridItem.classList.add('locked')
            gridItem.classList.remove('preload');   
        }
        if(gridItem.classList.contains('joystick') && gridItem.classList.contains('selected')){
            gridItem.classList.add('submit')
        }
    });
    
    const gridsContext = document.getElementById("grids-container-text");
    gridsContext.innerText = "Submit Joystick"
}

function clearJoystick(){
    const grid = getActiveGrid()["choosenGrid"];
    const gridItems = Array.from(grid.children[0].children); // Получаем все элементы
    gridItems.forEach(gridItem => {
        if(!gridItem.classList.contains("submit")){
            gridItem.classList.toggle("locked")
        }
    });
}

function getJoystickElement(x,y){
    const grid = getActiveGrid()["choosenGrid"];
    const gridItems = Array.from(grid.children[0].children); // Получаем все элементы
    gridItems.forEach(item=>{
        if(item.dataset.x == x && item.dataset.y == y){
            return item;
        }
    })
}
