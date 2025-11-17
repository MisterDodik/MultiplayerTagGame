package handlers

import (
	"github.com/MisterDodik/MultiplayerGame/internal/events"
	"github.com/MisterDodik/MultiplayerGame/internal/network"
)

var EventHandlers = map[string]func(events.Event, *network.Client) error{
	events.JoinLobby:       JoinLobbyHandler,
	events.DepopulateLobby: LeftLobbyHandler,
	events.StartGame:       StartGameHandler,
	events.ChatroomMsg:     ChatMsgFromClientHandler,
}
