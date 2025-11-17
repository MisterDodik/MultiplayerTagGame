package handlers

import (
	"log"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
	"github.com/MisterDodik/MultiplayerGame/internal/network"
)

func StartGameHandler(e events.Event, c *network.Client) error {
	log.Println("game started")

	network.BroadcastMessageToAllClients(c, &e) //sends an already existing start_game event to everyone
	return nil
}
