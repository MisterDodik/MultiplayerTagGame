package handlers

import (
	"errors"
	"log"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
	"github.com/MisterDodik/MultiplayerGame/internal/network"
)

func StartGameHandler(e events.Event, c *network.Client) error {

	if c.Lobby == nil {
		return errors.New("client is not in a lobby")
	}
	c.GameStarted = true
	log.Println("game started")
	go c.Lobby.StartGame(c)

	network.BroadcastMessageToAllClients(c, &e) //sends an already existing start_game event to everyone
	return nil
}
