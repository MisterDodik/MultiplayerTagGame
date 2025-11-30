package network

import (
	"encoding/json"
	"log"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
	"github.com/gorilla/websocket"
)

type ClientList map[*Client]bool
type Client struct {
	//username, id, hp...
	Manager *Manager
	Conn    *websocket.Conn
	Egress  chan []byte

	LobbyName string
	Username  string
	Id        string

	Lobby          *GameServer
	ClientGameData *ClientGameData
	GameStarted    bool
	Color          string
}

type ClientGameData struct {
	PosX      float64
	PosY      float64
	RotationZ float64
	Radius    float64
	Speed     float64
}

func (c *Client) NewClientGameData() *ClientGameData {
	data := &ClientGameData{
		RotationZ: 0,
		Radius:    .12,
		Speed:     .03,
	}

	data.PosX = float64(startPositionOriginX) + float64(len(c.Lobby.Clients)%4)*0.5
	data.PosY = float64(startPositionOriginY) - float64(len(c.Lobby.Clients)/4)*0.5
	log.Println(data.PosX, data.PosY)
	return data
}

func NewClient(conn *websocket.Conn, manager *Manager, username, lobbyName, id string, lobby *GameServer, color string) *Client {
	c := &Client{
		Manager:   manager,
		Conn:      conn,
		Egress:    make(chan []byte),
		LobbyName: lobbyName,
		Username:  username,

		Id:          id,
		Lobby:       lobby,
		GameStarted: false,
		Color:       color,
	}
	c.ClientGameData = c.NewClientGameData()
	return c
}

func (c *Client) ReadMessage() {
	defer func() {
		c.Manager.removeClient(c)
	}()
	for {
		_, payload, err := c.Conn.ReadMessage()

		if err != nil {
			if websocket.IsUnexpectedCloseError(err, websocket.CloseGoingAway, websocket.CloseAbnormalClosure) {
				log.Printf("error reading message: %v", err)
			}
			log.Println(err)
			break
		}

		var evt events.Event
		if err := json.Unmarshal(payload, &evt); err != nil {
			log.Printf("error unmarshaling payload sent by client: %v", err)
			continue
		}
		if evt.Type != events.UpdatePositionFromClient {
			log.Println("payload je: ", string(payload))
		}
		_ = c.Manager.parseEvent(evt, c)
	}
}

func (c *Client) WriteMessage() {
	defer func() {
		c.Manager.removeClient(c)
	}()

	//nolint:gosimple
	for {
		select {
		case message, ok := <-c.Egress:
			if !ok {
				if err := c.Conn.WriteMessage(websocket.CloseMessage, nil); err != nil {
					log.Println("connection closed: ", err)
				}
				return
			}
			if err := c.Conn.WriteMessage(websocket.TextMessage, message); err != nil {
				log.Println(err)
			}
			log.Println("sent message")

		default:
			_ = 0
		}
	}
}
