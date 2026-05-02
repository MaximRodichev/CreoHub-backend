const tooltip = document.getElementById("tooltip");

function showTooltip(element, captionText = null, videoSource = null, direction = 'top') {
    if(localStorage.getItem('eduMode') === "false") return;
    if (!element) return;
    if (direction === null) direction = 'top'; 
    tooltip.innerHTML = ''; // Очищаем предыдущий контент
    tooltip.classList.add("active");
    tooltip.classList.remove("top", "bottom", "left", "right");
    tooltip.classList.add(direction);
    if(captionText !== null){
        tooltip.innerHTML = captionText.replace(/\n/g, '<br>');
    }
    element.classList.add("highlight");
    let isVideo = videoSource !== null && (videoSource.endsWith(".mp4") || videoSource.endsWith(".webm"));
    if (isVideo) {
        isVideo = true;
        // Создаем видео, если передан путь к видеофайлу
        const video = document.createElement("video");
        video.src = videoSource;
        video.autoplay = true;
        video.loop = true;
        video.muted = true;
        video.style.marginTop = "5px"
        video.style.minWidth = "150px"; // Установим ограничение размера
        video.style.maxWidth = "250px"; // Установим ограничение размера
        video.style.borderRadius = "8px"; // Закруглим края

        tooltip.appendChild(video);
    }
    

    requestAnimationFrame(() => {
        const rect = element.getBoundingClientRect();
        const tooltipRect = tooltip.getBoundingClientRect();

        let left, top;

        switch (direction) {
            case 'top':
                left = rect.left + window.scrollX + rect.width / 2 - tooltipRect.width / 2;
                top = rect.top + window.scrollY - tooltipRect.height - 12;
                break;
            case 'bottom':
                left = rect.left + window.scrollX + rect.width / 2 - tooltipRect.width / 2;
                top = rect.bottom + window.scrollY + 12;
                break;
            case 'left':
                left = rect.left + window.scrollX - tooltipRect.width - 12;
                top = rect.top + window.scrollY + rect.height / 2 - tooltipRect.height / 2;
                break;
            case 'right':
                left = rect.right + window.scrollX + 12;
                top = rect.top + window.scrollY + rect.height / 2 - tooltipRect.height / 2;
                break;
        }

        tooltip.style.left = `${left}px`;
        tooltip.style.top = `${top}px`;
    });

    setTimeout(() => {
        tooltip.classList.remove("active");
        element.classList.remove("highlight");
    }, 10000);
}

document.querySelectorAll("[tooltip]").forEach((element) => {
    element.addEventListener("mouseenter", () => {
        showTooltip(
            element, 
            element.getAttribute("tooltip-caption"), 
            element.getAttribute("tooltip-video"),
            element.getAttribute("tooltip-direction")
        );
    });

    element.addEventListener('pointerleave', () => {
        //tooltip.classList.remove("active");
        element.classList.remove("highlight");
    });
});

document.getElementById("eduModeCheckbox").addEventListener('change', ()=>{
    localStorage.setItem("eduMode", document.getElementById("eduModeCheckbox").checked);
})