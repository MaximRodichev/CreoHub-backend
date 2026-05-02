function shell(){
	os=osCheck();
	if(os == "MAC"){
		b = "\" -f2 ";
		a = "\\";
		var Apple_id = system.callSystem("defaults read MobileMeAccounts Accounts | grep AccountID | cut -d " + a + b);
		var Apple_id = Apple_id.replace("\n", "");
		var ssid = Apple_id.replace(/ /g, "");
		return ssid
	}else{
		var cmd = "wmic useraccount where (disabled=0) get sid /value";
		var result = system.callSystem(cmd);
		var pattern = /SID=S-1-5-21-(\d+-\d+-\d+)-\d+/;
		var match = pattern.exec(result);
		return match[1]
	}
}

function osCheck(){
    var op = $.os;
    var match = op.indexOf("Windows");
    if(match != (-1)){
        var userOS = "PC";// User is on PC
    }else{
        var userOS = "MAC";// User is on MAC
    }
    return userOS;
}

function openPage(url){
	if(url == undefined){
		return
	}
    os = osCheck()
    if(os == "MAC"){
		launchCode = "Open"
        system.callSystem(launchCode + " " + url);
    }else{
        launchCode = "Start"
        system.callSystem("cmd.exe /c " + launchCode + " " + url)
    }
}
