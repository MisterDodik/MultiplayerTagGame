package handlers

import (
	"encoding/json"
	"fmt"
	"unicode"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
	"github.com/MisterDodik/MultiplayerGame/internal/network"
)

func JoinLobbyHandler(e events.Event, c *network.Client) error {
	var lobbyName string
	err := json.Unmarshal(e.Payload, &lobbyName)
	if err != nil {
		return err
	}
	if len(lobbyName) < 1 {
		return fmt.Errorf("lobby name too short")
	}
	for _, r := range lobbyName {
		if !unicode.IsDigit(r) {
			return fmt.Errorf("only numbers are allowed in lobby name, but found %q", r)
		}
	}

	c.LobbyName = lobbyName

	jsonMsg, err := json.Marshal(lobbyName)
	if err != nil {
		return err
	}

	responseEvt := &events.Event{
		Type:    events.JoinLobby,
		Payload: jsonMsg,
	}
	network.BroadcastMessageToSingleClient(c, responseEvt)

	if err := UpdateLobby(c, events.PopulateLobby); err != nil {
		return err
	}
	return nil
}
func LeftLobbyHandler(e events.Event, c *network.Client) error {
	if err := UpdateLobby(c, events.DepopulateLobby); err != nil {
		return err
	}
	return nil
}
func UpdateLobby(c *network.Client, action string) error {
	evt := &events.Event{
		Type: action,
		Payload: json.RawMessage(
			[]byte(fmt.Sprintf(`{"username":"%s","id":"%s"}`, c.Username, c.Id)),
		),
	}
	network.BroadcastMessageToAllClients(c, evt)

	if action == events.DepopulateLobby {
		return nil
	}
	for other := range c.Lobby.Clients {
		if other == c {
			continue
		}
		evt := &events.Event{
			Type: action,
			Payload: json.RawMessage(
				[]byte(fmt.Sprintf(`{"username":"%s","id":"%s"}`, other.Username, other.Id)),
			),
		}
		network.BroadcastMessageToSingleClient(c, evt)
	}
	return nil
}
