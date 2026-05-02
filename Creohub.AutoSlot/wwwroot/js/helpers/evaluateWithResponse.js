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