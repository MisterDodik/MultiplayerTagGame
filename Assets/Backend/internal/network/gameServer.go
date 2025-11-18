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

/*
client salje event u formatu:

	{
		type: updatePos
		payload: {
					input vector2
				}
	}

onda se ovdje pozove funkcija koja for loopom prodje kroz sve igrace u lobiju
calculate_pos(event.payload.input)

broadcasttoeveryone(client.newpos)

	func calculate_pos(input vector2){
		newPosX = client.currentX + input.x * speed
		newPosY = client.currentY + input.y * speed


		//cekira collision
		for p in players_in_lobby{
			if p = client
				continue
			if rastojanje(p.posX, newPosX) < sizeof_client + sizeof_p{
				newPosX = client.currentX
			}
			if rastojanje(p.posY, newPosY) < sizeof_client + sizeof_p{
				newPosY = client.currentY
			}
		}

		return (newPosX, newPosY)
	}
*/
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
