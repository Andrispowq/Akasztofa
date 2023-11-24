
const website_clientID = uuidv4();
var connection;
var loginInfo;

init();

async function init()
{
    window.onbeforeunload = async function()
    {
        await onclose();
    }

    window.beforeunload = async function(e)
    {
        await onclose();  
    }
}

async function login()
{
    const username = document.getElementById("username").value;
    const password = document.getElementById("password").value;
    if(username == "" || password == "")
    {
        document.getElementById("response").textContent = "ERROR: specify username and password!";
        return null;
    }

    const connId = connection["connectionID"];
    loginInfo = await GetRequest("?type=login&plain=true&connectionID=" + connId + "&username=" + username + "&password=" + password);

    if(loginInfo["result"] = "true")
    {
        window.location.href = 'website/game.html';
    }
}

async function logout()
{
    if(loginInfo == null)
        return;

    const sessionId = loginInfo["sessionID"];
    await GetRequest("?type=logout&noanswer=true&sessionID=" + sessionId);
}

async function GetRequest(query)
{
    const response = await get_request(query);
    const json = JSON.parse(response);
    document.getElementById("response").innerHTML = json["message"];
    return json;
}

function get_request(query)
{
    return new Promise((resolve, reject) => {
        $.get("http://192.168.100.20:6969/" + query, function(data, status)
        {
            resolve(data)
        })
    })
}

function uuidv4() 
{
    return "10000000-1000-4000-8000-100000000000".replace(/[018]/g, c =>
      (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
    );
}

async function onclose()
{
    await logout();
    disconnect = await GetRequest("?type=disconnect&noanswer=true&connectionID=" + connection["connectionID"]);
    console.log(disconnect);
}

async function onload()
{
    await logout();
    connection = await GetRequest("?type=connect&noanswer=true&clientID=" + website_clientID);
    console.log(connection);
}
  