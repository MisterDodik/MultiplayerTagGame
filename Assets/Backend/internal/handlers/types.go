package handlers

import (
	"github.com/MisterDodik/MultiplayerGame/internal/events"
	"github.com/MisterDodik/MultiplayerGame/internal/network"
)

var EventHandlers = map[string]func(events.Event, *network.Client) error{
	events.JoinLobby:                JoinLobbyHandler,
	events.DepopulateLobby:          LeftLobbyHandler,
	events.ChatroomMsg:              ChatMsgFromClientHandler,
	events.StartGame:                StartGameHandler,
	events.UpdatePositionFromClient: UpdatePositionHandler,

	//events.EndGame:                EndGameHandler,
}
