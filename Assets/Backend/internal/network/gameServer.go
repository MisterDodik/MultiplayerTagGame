package network

import (
	"encoding/json"
	"log"
	"time"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
)

type GameServerList map[string]*GameServer

type GameServer struct {
	Clients ClientList
}

func (m *Manager) NewGameServer(lobbyName string) *GameServer {
	gs := &GameServer{
		Clients: make(ClientList),
	}
	m.Games[lobbyName] = gs
	return gs
}

func (gs *GameServer) StartGame(c *Client) {
	ticker := time.NewTicker(gameTickRate)
	defer ticker.Stop()

	go func() {
		for range ticker.C {
			if err := gs.updatePlayerPositions(c); err != nil {
				log.Printf("error sending position update: %v", err)
			}
		}
	}()

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
	log.Println(string(jsonData))

	BroadcastMessageToAllClients(c, &evt)

	return nil
}
