package network

import (
	"encoding/json"
	"fmt"
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
	PosX         float32
	PosY         float32
	Width        float32
	Height       float32
	Speed        float32
	IsHunter     bool
	Score        int64
	LastSentTick int
}

func (c *Client) SetHunter(isHunter bool) {
	if isHunter {
		c.Color = "#ff0000ff"
	} else {
		c.Color = "#04ff00ff"
	}
	c.ClientGameData.IsHunter = isHunter

	evt := &events.Event{
		Type: events.UpdateColor,
		Payload: json.RawMessage(
			[]byte(fmt.Sprintf(`{"id":"%s", "colorHex":"%s", "isHunter": "%v"}`, c.Id, c.Color, c.ClientGameData.IsHunter)),
		),
	}
	if isHunter {
		c.Lobby.ToHunt--
		c.Lobby.Hunters++
	}
	BroadcastMessageToAllClients(c, evt)
}
func (c *Client) UpdateScore(amount int) {
	c.ClientGameData.Score += int64(amount)
	evt := &events.Event{
		Type: events.UpdateScore,
		Payload: json.RawMessage(
			[]byte(fmt.Sprintf(`{"id":"%s", "score":"%v", "amount":"%v"}`, c.Id, c.ClientGameData.Score, amount)),
		),
	}
	BroadcastMessageToSingleClient(c, evt)
}
func (c *Client) NewClientGameData() *ClientGameData {
	data := &ClientGameData{
		Width:        .3,
		Height:       .3,
		Speed:        3,
		IsHunter:     false,
		Score:        0,
		LastSentTick: 0,
	}

	data.PosX = float32(startPositionOriginX) + float32(len(c.Lobby.Clients)%4)*0.5
	data.PosY = float32(startPositionOriginY) - float32(len(c.Lobby.Clients)/4)*0.5
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
func (c *Client) SetPosition(posX, posY float32) {
	c.ClientGameData.PosX = posX
	c.ClientGameData.PosY = posY
}

func (c *Client) ReadMessage() {
	defer func() {
		c.Manager.RemoveClient(c)
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
		c.Manager.RemoveClient(c)
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
			//log.Println("sent message")

		default:
			_ = 0
		}
	}
}
