package network

import (
	"encoding/json"
	"log"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
)

func BroadcastMessageToAllClients(c *Client, e *events.Event) {
	sendEventJSON, err := json.Marshal(&e)
	if err != nil {
		log.Println("error marshaling new send event ", err)
		return
	}

	log.Println(string(sendEventJSON))
	for client := range c.Lobby.Clients {
		if client.LobbyName == c.LobbyName {
			client.Egress <- sendEventJSON
		}
	}
}
func BroadcastMessageToSingleClient(c *Client, e *events.Event) {
	sendEventJSON, err := json.Marshal(&e)
	if err != nil {
		log.Println("error marshaling new send event ", err)
		return
	}
	c.Egress <- sendEventJSON
}
