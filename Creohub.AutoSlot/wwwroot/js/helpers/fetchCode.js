// Загружает JSX файл как статику и отправляет в CEP через postMessage
// Файлы лежат в /_content/Creohub.AutoSlot/jsx/
async function fetchJSX(filePath) {
    try {
        const response = await fetch(`/_content/Creohub.AutoSlot/jsx/${filePath}`);
        if (!response.ok) {
            console.error(`fetchJSX: не удалось загрузить ${filePath} (${response.status})`);
            return;
        }
        const jsx = await response.text();
        window.parent.postMessage({ jsx }, '*');
        return jsx;
    } catch (err) {
        console.error(`fetchJSX error:`, err);
    }
}
