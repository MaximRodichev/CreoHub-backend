document.addEventListener("DOMContentLoaded", async function() {
    let auth = await sendMessageToParentWithResponse("shell()");
    document.cookie = `authReq=${auth}; path=/; secure; samesite=None; httponly`;
})

function displayError(message, style){
    let span = document.getElementById("errors")
    if(message.contains("fetch")){
        message="Возникла ошибка. Разрешите скрипту выход в сеть:\nPreference => Scripting&Expression => Allow Script to Write Files..."
    }
    span.innerHTML = message;
    span.style.display = style;
}

async function submitCode(){
    let input = document.getElementById('code');
    let inputValue = input.value;
    let auth = await sendMessageToParentWithResponse("shell()");
    if (inputValue.match(/^[0-9]{4}$/)) {
        sendRequest("POST", `/auth/verify?code=${inputValue}`, {
            "auth": auth,
            "ip": ""
        },auth)
            .then(response=>{
                console.log(response);
                window.location.reload();
            })

            .catch(err=>{
                console.error(err);
                displayError(err, "block");
            })
    }
    if (inputValue.match(/^[A-z]$/)) {
        input.value = "";
    }
}

async function getCode(){
    openURL(`https://t.me/CreoHelper_bot?start=autoslot`) 
}

document.getElementById('code').addEventListener("input", async ()=>{
    await submitCode();
});