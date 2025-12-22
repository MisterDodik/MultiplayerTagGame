package events

import (
	"encoding/json"
)

type Event struct {
	Type    string          `json:"type"`
	Payload json.RawMessage `json:"payload"`
}

var (
	BroadcastToClients = "server_msg"

	JoinLobby       = "join_lobby"
	PopulateLobby   = "populate_lobby"
	DepopulateLobby = "depopulate_lobby"

	ChatroomMsg = "chatroom_msg"

	StartGame       = "start_game"
	ExitToLobby     = "exit_to_lobby"
	CloseConnection = "close_connection"
	SpawnObstacle   = "spawn_grid_obstacle"
	RemoveObstacle  = "remove_grid_obstacle"
	UpdateColor     = "update_color_from_server"

	UpdatePositionFromClient = "update_position_from_client"
	UpdatePositionFromServer = "update_position_from_server"
	HunterAttack             = "hunter_attack"

	SendInfoEvent = "send_info"

	EndGameUpdateScore = "end_game"
	UpdateScore        = "update_single_score"
)
