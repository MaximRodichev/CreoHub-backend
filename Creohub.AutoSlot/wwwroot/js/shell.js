document.addEventListener("DOMContentLoaded", async function() {
    await fetchJSX("other/browserTips.jsx")
})

function openURL(url){
    if(!url.startsWith("http")){
        url = window.location.origin + url;
    }
    window.parent.postMessage({openUrl: `openPage('${url}')`}, "*");

}