package main

import (
	"encoding/json"
	"log"

	"github.com/gorilla/websocket"
)

type ClientList map[*Client]bool
type Client struct {
	//username, id, hp...
	manager *Manager
	conn    *websocket.Conn
	egress  chan []byte

	lobbyName string
	username  string
	id        string

	lobby *GameServer
}

func NewClient(conn *websocket.Conn, manager *Manager, username, lobbyName, id string, lobby *GameServer) *Client {
	return &Client{
		manager:   manager,
		conn:      conn,
		egress:    make(chan []byte),
		lobbyName: lobbyName,
		username:  username,
		id:        id,
		lobby:     lobby,
	}
}

func (c *Client) ReadMessage() {
	defer func() {
		c.manager.removeClient(c)
	}()
	for {
		_, payload, err := c.conn.ReadMessage()

		if err != nil {
			if websocket.IsUnexpectedCloseError(err, websocket.CloseGoingAway, websocket.CloseAbnormalClosure) {
				log.Printf("error reading message: %v", err)
			}
			log.Println(err)
			break
		}

		log.Println("payload je: ", string(payload))
		var evt Event
		if err := json.Unmarshal(payload, &evt); err != nil {
			log.Printf("error unmarshaling payload sent by client: %v", err)
			continue
		}

		_ = c.manager.parseEvent(evt, c)
	}
}

func (c *Client) WriteMessage() {
	defer func() {
		c.manager.removeClient(c)
	}()

	//nolint:gosimple
	for {
		select {
		case message, ok := <-c.egress:
			if !ok {
				if err := c.conn.WriteMessage(websocket.CloseMessage, nil); err != nil {
					log.Println("connection closed: ", err)
				}
				return
			}
			if err := c.conn.WriteMessage(websocket.TextMessage, message); err != nil {
				log.Println(err)
			}
			log.Println("sent message")

		default:
			_ = 0
		}
	}
}
