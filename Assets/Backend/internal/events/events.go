package events

import (
	"encoding/json"
)

type Event struct {
	Type    string          `json:"type"`
	Payload json.RawMessage `json:"payload"`
}

var (
	JoinLobby          = "join_lobby"
	PopulateLobby      = "populate_lobby"
	DepopulateLobby    = "depopulate_lobby"
	StartGame          = "start_game"
	ChatroomMsg        = "chatroom_msg"
	BroadcastToClients = "server_msg"
)
