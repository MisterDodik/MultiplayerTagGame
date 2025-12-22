package handlers

import (
	"encoding/json"
	"fmt"
	"log"
	"time"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
	"github.com/MisterDodik/MultiplayerGame/internal/network"
)

type ChatMessageToClient struct {
	From    string `json:"from"`
	SentAt  string `json:"sentAt"`
	Message string `json:"message"`
}

func ChatMsgFromClientHandler(e events.Event, c *network.Client) error {
	var clientMsg string
	err := json.Unmarshal(e.Payload, &clientMsg)
	if err != nil {
		return err
	}

	if len(clientMsg) < 1 {
		network.SendInfo(c, network.ShortChatMsg)
		return fmt.Errorf("message too short")
	}
	log.Println(clientMsg)

	response := ChatMessageToClient{
		From:    c.Username,
		Message: clientMsg,
		SentAt:  time.Now().Format(time.TimeOnly),
	}
	jsonResponse, err := json.Marshal(response)
	if err != nil {
		return err
	}
	responseEvt := &events.Event{
		Type:    events.ChatroomMsg,
		Payload: jsonResponse,
	}
	network.BroadcastMessageToAllClients(c, responseEvt)
	return nil
}
