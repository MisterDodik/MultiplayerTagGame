window.onload = function () {
    document.getElementById("chatroom-selection").onsubmit = function() {changeChatRoom(""); return false;};
    document.getElementById("chatroom-message").onsubmit = sendMessage;
    document.getElementById("login-form").onsubmit = auth;
};

let currentRoom = ""
let username = ""

class Event {
    constructor(type, payload){
        this.type = type
        this.payload = payload
    }
}

class SendMessageEvent {
    constructor(message, from){
        this.message = message
        this.from = from
    }
}

class ReceivedMessageEvent {
    constructor(message, from, sentAt){
        this.message = message
        this.from = from
        this.sentAt = sentAt
    }
}
class ChatRoomChangeEvent{
    constructor(name){
        this.name = name
    }
}

function sendEvent(eventName, data){
    const event = new Event(eventName, data)
    conn.send(JSON.stringify(event))
}



function changeChatRoom(initRoom){
    let room = initRoom
    if (room == ""){
        var roomInput = document.getElementById("chatroom");
        room = roomInput.value
        if (room=="" || room == currentRoom)  return; 
    }

    currentRoom = room
    textarea = document.getElementById("chatmessages");
    textarea.innerHTML = ""
    textarea.scrollTop = textarea.scrollHeight;
    
    document.getElementById("chat-header").textContent = "Current Chat: "+currentRoom;

    const event = new ChatRoomChangeEvent(room)

    sendEvent("room_change", event)

    return false
}

function routeEvent(event){

    if (event.type === undefined) {
        alert("no 'type' field in event");
    }
    switch (event.type) {
        case "write_event":
            const messageEvent = Object.assign(new ReceivedMessageEvent, event.payload);
            appendChatMessage(messageEvent);
            break;
        case "room_change":
            textarea.innerHTML = textarea.innerHTML + "\n" + event.payload;
            textarea.scrollTop = textarea.scrollHeight;
            break;
        default:
            alert("unsupported message type");
            break;
    }
    return false
}

function appendChatMessage(messageEvent){
    const formattedString = (messageEvent.sentAt && messageEvent.from)
                            ? `${messageEvent.sentAt} ${messageEvent.from}: ${messageEvent.message}`
                            : `${messageEvent.message}`
    textarea = document.getElementById("chatmessages");
    textarea.innerHTML = textarea.innerHTML + "\n" + formattedString;
    textarea.scrollTop = textarea.scrollHeight;
}

function sendMessage(){
    var newmessage = document.getElementById("message");
    const msg = newmessage.value
    if (msg=="")  return false; 
    let payload = new SendMessageEvent(msg, username)   
    sendEvent("send_event", payload)
    return false
}

function auth(){
    var roomInput = document.getElementById("chatroom");
    const room = roomInput.value
    if (room=="")  return; 
    
    var usernameInput = document.getElementById("username");
    const user = usernameInput.value
    if (user=="")  return; 
    
    fetch("/login", {
        method : 'POST',
        headers :{
            'Accept' : 'application/json',
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            username:user,
            chatroom:room
        }),
        mode: 'cors',
    })
    .then(res =>{
        if (res.ok){
            return res.json();
        }
        else
            throw 'unauthorized'
    })
    .then(data => {
        username = user
        connectWS(data.otp)
    })
    .catch(error =>{
        alert(error)
    })


    
    return false
}

let conn;
function connectWS(otp){
    if (conn !== undefined){
        conn.close()
    }
    if (window["WebSocket"]) {
        console.log("supports websockets");
        // Connect to websocket
        conn = new WebSocket("ws://" + document.location.host + "/ws?otp="+otp);
        
    } else {   
        alert("Not supporting websockets");
    }
    conn.onopen = () => {
        console.log('WebSocket connection established.');
        document.getElementById("change-chatroom-btn").hidden = false
        changeChatRoom(currentRoom)
    };
    
    conn.onclose = () => {
        console.log('WebSocket connection closed.');
        document.getElementById("change-chatroom-btn").hidden = true
    };
    
    conn.onmessage = function(evt){
        jsonData = JSON.parse(evt.data)
        const event = Object.assign(new Event, jsonData);
        routeEvent(event)
    }

    return false
}


