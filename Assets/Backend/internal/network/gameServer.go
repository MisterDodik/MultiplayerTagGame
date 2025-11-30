package network

import (
	"encoding/json"
	"log"
	"time"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
)

type GameServerList map[string]*GameServer

type GameServer struct {
	Clients   ClientList
	IsStarted bool
}

func (m *Manager) NewGameServer(lobbyName string) *GameServer {
	gs := &GameServer{
		IsStarted: false,
		Clients:   make(ClientList),
	}
	m.Games[lobbyName] = gs
	return gs
}

func (gs *GameServer) StartGame(c *Client) {
	log.Println("game loop started")
	ticker := time.NewTicker(gameTickRate)
	defer ticker.Stop()

	for range ticker.C {
		if err := gs.updatePlayerPositions(c); err != nil {
			log.Printf("error sending position update: %v", err)
		}

		if len(gs.Clients) == 0 { //ovdje mozes vjv staviti i 1, tj ako je samo jedan ostao onda je kraj tj on je pobijedio
			defer func() {
				gs.IsStarted = false
			}()

			var c *Client
			for key := range gs.Clients { //this retrieves the first ie the only client left in the lobby
				c = key
				break
			}
			if c == nil {
				return
			}
			evt := events.Event{
				Type:    events.EndGame,
				Payload: json.RawMessage{},
			}
			BroadcastMessageToSingleClient(c, &evt)
			return
		}
	}

}

type PositionUpdateServer struct {
	Id   string  `json:"id"`
	PosX float64 `json:"x"`
	PosY float64 `json:"y"`
}
type PlayerPositions []PositionUpdateServer

func (gs *GameServer) updatePlayerPositions(c *Client) error {
	players := make(PlayerPositions, 0, len(gs.Clients))
	for player := range gs.Clients {
		players = append(players, PositionUpdateServer{
			Id:   player.Id,
			PosX: player.ClientGameData.PosX,
			PosY: player.ClientGameData.PosY,
		})
	}

	jsonData, err := json.Marshal(players)
	if err != nil {
		return err
	}
	evt := events.Event{
		Type:    events.UpdatePositionFromServer,
		Payload: jsonData,
	}

	BroadcastMessageToAllClients(c, &evt)
	//log.Println(string(evt.Payload))/
	return nil
}
