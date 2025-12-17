package network

import (
	"encoding/json"
	"log"
	"math/rand/v2"
	"slices"
	"time"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
)

type GameServerList map[string]*GameServer
type GameServer struct {
	Settings *GameSettings

	Clients   ClientList
	IsStarted bool

	Grid          [][]GridData
	GridObstacles []IntPair
	GridRows      int
	GridCols      int
	GridCellSize  float32
	GridOriginX   float32
	GridOriginY   float32
}
type GameSettings struct {
	HunterAttackRange  float64
	BarricadeSpawnRate time.Duration
}

func (gs *GameServer) NewGameSettings(hunterAttackRange float64) *GameSettings {
	return &GameSettings{
		HunterAttackRange:  hunterAttackRange,
		BarricadeSpawnRate: time.Second,
	}
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
type IntPair struct {
	X int
	Y int
}

func (gs *GameServer) initObstacles(obstacleCoverage int, c *Client) error {
	if obstacleCoverage == 0 {
		obstacleCoverage = 20
	}

	used := make(map[IntPair]struct{})
	for len(used) < gs.GridCols*gs.GridRows*obstacleCoverage/100 {
		x := rand.IntN(gs.GridCols-2) + 1 //excludes edges
		y := rand.IntN(gs.GridRows-2) + 1

		if gs.checkNeighborObstacles(x, y, 3) {
			used[IntPair{X: x, Y: y}] = struct{}{}
		}
	}
	gs.GridObstacles = make([]IntPair, 0, len(used))
	for val := range used {
		gs.GridObstacles = append(gs.GridObstacles, val)
		gs.Grid[val.X][val.Y].HasObstacle = true
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
func (gs *GameServer) removeObstacle(c *Client) error {
	var obstacle Obstacle
	randomIndex := rand.IntN(len(gs.GridObstacles))

	x := gs.GridObstacles[randomIndex].X
	y := gs.GridObstacles[randomIndex].Y
	if gs.Grid[x][y].HasObstacle {
		gs.Grid[x][y].HasObstacle = false
		obstacle = Obstacle{
			CellSize: gs.GridCellSize,
			PosX:     gs.Grid[x][y].CenterX,
			PosY:     gs.Grid[x][y].CenterY,
		}
		gs.GridObstacles = slices.Delete(gs.GridObstacles, randomIndex, randomIndex+1)
	}

	jsonData, err := json.Marshal(obstacle)
	if err != nil {
		return err
	}
	evt := events.Event{
		Type:    events.RemoveObstacle,
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

	//ovdje stavi mzd da se iz managera salju, al nek zasad ostanu hardcoded vrijednosti
	gs.Settings = gs.NewGameSettings(0.5)
	m.Games[lobbyName] = gs
	gs.initGrid()

	return gs
}

func (gs *GameServer) initializators(c *Client) {
	if gs.initObstacles(20, c) != nil {
		log.Println("obstacles didnt spawn correctly")
	}

	hunterIndex := rand.IntN(len(gs.Clients))
	counter := 0
	for client := range gs.Clients {
		client.SetHunter(hunterIndex == counter) //resets previous and sets new isHunter state
		for {
			i := rand.IntN(gs.GridCols)
			j := rand.IntN(gs.GridRows)

			//if !gs.Grid[i][j].HasObstacle && gs.checkNeighborObstacles(i, j, 3) {
			if !gs.Grid[i][j].HasObstacle {
				client.SetPosition(gs.Grid[i][j].CenterX, gs.Grid[i][j].CenterY)
				log.Println("OVO JE POCETNA POZICIJA:", i, j)
				_ = gs.updatePlayerPositions(c)
				break
			}
		}
		counter++
	}

}
func (gs *GameServer) StartGame(c *Client) {
	log.Println("game loop started")
	mainTicker := time.NewTicker(gameTickRate)

	barricadeTicker := time.NewTicker(gs.Settings.BarricadeSpawnRate)
	defer func() {
		mainTicker.Stop()
		barricadeTicker.Stop()
	}()

	gs.initializators(c)
	for range mainTicker.C {
		select {
		case <-mainTicker.C:
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
		case <-barricadeTicker.C:
			_ = gs.removeObstacle(c)
			if len(gs.GridObstacles) == 0 {
				barricadeTicker.Stop()
			}
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

var dirs = [][2]int{{-1, -1}, {0, -1}, {1, -1}, {1, 0}, {1, 1}, {0, 1}, {-1, 1}, {-1, 0}}

func (gs *GameServer) checkNeighborObstacles(x, y, allowed int) bool {

	counter := 0
	for _, d := range dirs {
		dx := x + d[0]
		dy := y + d[1]

		if dx < 0 || dy < 0 || dx >= gs.GridCols || dy >= gs.GridRows {
			continue
		}
		if gs.Grid[dx][dy].HasObstacle {
			counter++
			if counter > allowed {
				return false
			}
		}
	}

	return true
}
