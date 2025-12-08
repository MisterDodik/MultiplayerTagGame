package network

import (
	"encoding/json"
	"log"
	"math/rand/v2"
	"time"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
)

type GameServerList map[string]*GameServer
type GameServer struct {
	Clients   ClientList
	IsStarted bool

	Grid         [][]GridData
	GridRows     int
	GridCols     int
	GridCellSize float32
	GridOriginX  float32
	GridOriginY  float32
}

type GridData struct {
	CenterX     float32
	CenterY     float32
	HasObstacle bool
}

func (gs *GameServer) initGrid() {
	gs.Grid = make([][]GridData, gs.GridCols)
	for row := range gs.Grid {
		gs.Grid[row] = make([]GridData, gs.GridRows)
	}

	for i := 0; i < gs.GridCols; i++ {
		for j := 0; j < gs.GridRows; j++ {
			isEdge := false
			if i == gs.GridCols-1 || i == 0 || j == 0 || j == gs.GridRows-1 {
				isEdge = true
			}
			gs.Grid[i][j] = GridData{
				CenterY:     gs.GridOriginY + float32(j)*gs.GridCellSize,
				CenterX:     gs.GridOriginX + float32(i)*gs.GridCellSize,
				HasObstacle: isEdge,
			}
		}
	}
}

type Obstacle struct {
	CellSize float32 `json:"cellSize"`
	PosX     float32 `json:"posX"`
	PosY     float32 `json:"posY"`
}
type Obstacles []Obstacle

func (gs *GameServer) initObstacles(obstacleCoverage int, c *Client) error {
	if obstacleCoverage == 0 {
		obstacleCoverage = 20
	}

	type IntPair struct {
		X int
		Y int
	}
	used := make(map[IntPair]struct{})
	for len(used) < gs.GridCols*gs.GridRows*obstacleCoverage/100 {
		x := rand.IntN(gs.GridCols)
		y := rand.IntN(gs.GridRows)

		used[IntPair{X: x, Y: y}] = struct{}{}
	}
	for val := range used {
		gs.Grid[val.X][val.Y].HasObstacle = true
		log.Println(val.X, val.Y)
	}

	obstacles := make(Obstacles, 0, len(used))
	for i := 0; i < gs.GridCols; i++ {
		for j := 0; j < gs.GridRows; j++ {
			if !gs.Grid[i][j].HasObstacle {
				continue
			}
			obstacles = append(obstacles, Obstacle{
				CellSize: gs.GridCellSize,
				PosX:     gs.Grid[i][j].CenterX,
				PosY:     gs.Grid[i][j].CenterY,
			})
		}
	}

	jsonData, err := json.Marshal(obstacles)
	if err != nil {
		return err
	}
	evt := events.Event{
		Type:    events.SpawnObstacle,
		Payload: jsonData,
	}

	BroadcastMessageToAllClients(c, &evt)

	return nil
}

func (m *Manager) NewGameServer(lobbyName string, gridCols, gridRows int, gridCellSize, gridOriginX, gridOriginY float32) *GameServer {
	gs := &GameServer{
		IsStarted: false,
		Clients:   make(ClientList),

		GridCols:     gridCols,
		GridRows:     gridRows,
		GridCellSize: gridCellSize,
		GridOriginX:  gridOriginX,
		GridOriginY:  gridOriginY,
	}
	m.Games[lobbyName] = gs
	gs.initGrid()

	return gs
}

func (gs *GameServer) initializators(c *Client) {
	if gs.initObstacles(20, c) != nil {
		log.Println("obstacles didnt spawn correctly")
	}

	for client := range gs.Clients {
		for {
			i := rand.IntN(gs.GridCols)
			j := rand.IntN(gs.GridRows)

			if !gs.Grid[i][j].HasObstacle {
				client.SetPosition(gs.Grid[i][j].CenterX, gs.Grid[i][j].CenterY)
				log.Println("OVO JE POCETNA POZICIJA:", i, j)
				_ = gs.updatePlayerPositions(c)
				break
			}
		}
	}
}
func (gs *GameServer) StartGame(c *Client) {
	log.Println("game loop started")
	ticker := time.NewTicker(gameTickRate)
	defer ticker.Stop()

	gs.initializators(c)

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
	PosX float32 `json:"x"`
	PosY float32 `json:"y"`
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
