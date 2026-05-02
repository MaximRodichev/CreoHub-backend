function sendRequest(method, url, body, option){
    const headers = {
        'Content-Type': 'application/json',
        "X-Affilate": `${option}`
    }
    return fetch(url, {
        method: method,
        body: JSON.stringify(body),
        headers: headers,
    }).then(response => {
        if(!response.ok){
            return response.text().then(errorData => {
                throw errorData;
            })
        }
        return response.text();
    })
}