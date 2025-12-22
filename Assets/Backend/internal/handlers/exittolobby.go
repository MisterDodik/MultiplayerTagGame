package handlers

import (
	"errors"
	"log"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
	"github.com/MisterDodik/MultiplayerGame/internal/network"
)

func ExitToLobbyHandler(e events.Event, c *network.Client) error {
	//reset score

	if !c.Lobby.IsStarted {
		network.SendInfo(c, network.GameNotStarted)
		return errors.New("the game has not started yet")
	}
	log.Println(c.Lobby.ActivePlayers, len(c.Lobby.Clients))
	c.GameStarted = false
	c.Lobby.ActivePlayers--

	if !c.ClientGameData.IsHunter {
		c.Lobby.ToHunt--
	} else {
		c.Lobby.Hunters--
	}
	evt := &events.Event{
		Type:    events.ExitToLobby,
		Payload: []byte("null"),
	}

	network.BroadcastMessageToSingleClient(c, evt)
	return nil
}
func CloseConnectionHandler(e events.Event, c *network.Client) error {
	c.Manager.RemoveClient(c)
	return nil
}
