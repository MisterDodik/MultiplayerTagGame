package network

import (
	"encoding/json"
	"fmt"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
)

type Info struct {
	Message string `json:"message"`
}

var (
	ReconnectionUnavailable = "Reconnection is currently not supported!"
	ShortChatMsg            = "Message too short!"
	GameNotStarted          = "The game has not started yet!"
	ShortLobbyName          = "Lobby name too short!"
	MalformedLobbyName      = "Only numbers are allowed in lobby name!"
)

func SendInfo(c *Client, messageType string) {
	evt := &events.Event{
		Type: events.SendInfoEvent,
		Payload: json.RawMessage(
			[]byte(fmt.Sprintf(`{"message":"%s"}`, messageType)),
		),
	}

	BroadcastMessageToSingleClient(c, evt)
}
