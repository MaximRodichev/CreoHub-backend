function sendMessageToParentWithResponse(script) {
    return new Promise((resolve, reject) => {
        const requestId = Date.now().toString(); // Уникальный ID запроса

        // Добавляем обработчик ответа
        function handleMessage(event) {
            if (event.data.requestId === requestId) { // Проверяем ID запроса
                window.removeEventListener("message", handleMessage); // Удаляем обработчик
                resolve(event.data.response); // Возвращаем результат
            }
        }

        window.addEventListener("message", handleMessage);

        // Отправляем скрипт в родительское окно
        window.parent.postMessage({script, requestId}, "*");
    });
}
function sendMessageToParent(script){
    window.parent.postMessage({script}, "*");
}

/**
 * Запрашивает у CEP extension превью файлов как base64 через Node.js fs.
 * @param {string[]} paths  Массив абсолютных путей к файлам (локальная ОС)
 * @returns {Promise<Object>} { filePath: "data:image/gif;base64,..." | null }
 */
function requestThumbnails(paths) {
    return new Promise(function(resolve, reject) {
        if (!paths || paths.length === 0) { resolve({}); return; }

        var requestId = Date.now().toString() + '_' + Math.floor(Math.random() * 9999);

        function handleMessage(event) {
            if (event.data && event.data.type === "thumbnail" && event.data.requestId === requestId) {
                window.removeEventListener("message", handleMessage);
                resolve(event.data.thumbnails || {});
            }
        }

        window.addEventListener("message", handleMessage);
        window.parent.postMessage({ type: "getThumbnail", paths: paths, requestId: requestId }, "*");

        // Таймаут 15 секунд (GIF могут быть большими)
        setTimeout(function() {
            window.removeEventListener("message", handleMessage);
            reject(new Error("Thumbnail request timeout"));
        }, 15000);
    });
}