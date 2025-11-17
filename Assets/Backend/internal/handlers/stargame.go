package handlers

import (
	"errors"
	"log"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
	"github.com/MisterDodik/MultiplayerGame/internal/network"
)

func StartGameHandler(e events.Event, c *network.Client) error {
	log.Println("game started")

	if c.Lobby == nil {
		return errors.New("client is not in lobby")
	}
	c.Lobby.StartGame()

	network.BroadcastMessageToAllClients(c, &e) //sends an already existing start_game event to everyone
	return nil
}
