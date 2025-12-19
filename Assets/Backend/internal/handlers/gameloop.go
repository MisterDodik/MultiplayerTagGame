package handlers

import (
	"encoding/json"
	"log"
	"math"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
	"github.com/MisterDodik/MultiplayerGame/internal/network"
)

type PositionUpdatePayload struct {
	InputX float32 `json:"inputX"`
	InputY float32 `json:"inputY"`
}

func HunterAttackHandler(e events.Event, c *network.Client) error {
	if !c.Lobby.IsStarted {
		return nil
	}
	for player := range c.Lobby.Clients {
		if player == c {
			continue
		}

		if calculateDistance(c, player.ClientGameData.PosX, player.ClientGameData.PosY) < c.Lobby.Settings.HunterAttackRange {
			player.SetHunter(true)
			return nil
		}
		log.Println(calculateDistance(c, player.ClientGameData.PosX, player.ClientGameData.PosY), c.Lobby.Settings.HunterAttackRange)
	}
	log.Printf("no players were close enough")
	return nil
}
func UpdatePositionHandler(e events.Event, c *network.Client) error {
	if !c.Lobby.IsStarted {
		return nil
	}
	var payload PositionUpdatePayload
	if err := json.Unmarshal(e.Payload, &payload); err != nil {
		return err
	}
	calculate_pos(c, payload.InputX, payload.InputY)
	return nil
}

func calculate_pos(c *network.Client, inputX, inputY float32) {
	length := float32(math.Sqrt(float64(inputX*inputX + inputY*inputY)))
	if length > 0 {
		inputX /= length
		inputY /= length
	}
	if inputX > 1 {
		inputX = 1
	}
	if inputX < -1 {
		inputX = -1
	}
	if inputY > 1 {
		inputY = 1
	}
	if inputY < -1 {
		inputY = -1
	}

	newPosX := inputX * c.ClientGameData.Speed
	newPosY := inputY * c.ClientGameData.Speed

	tryX := c.ClientGameData.PosX + newPosX
	tryY := c.ClientGameData.PosY + newPosY

	if !collides(c, tryX, tryY) {
		c.ClientGameData.PosX = tryX
		c.ClientGameData.PosY = tryY
		return
	}

	if !collides(c, tryX, c.ClientGameData.PosY) {
		c.ClientGameData.PosX = tryX
		return
	}

	if !collides(c, c.ClientGameData.PosX, tryY) {
		c.ClientGameData.PosY = tryY
		return
	}

}

func collides(c *network.Client, potentialX, potentialY float32) bool {

	//collision with other players
	for player := range c.Lobby.Clients {
		if player == c || !player.GameStarted {
			continue
		}

		// if float32(math.Abs(float64(player.ClientGameData.PosX)))+player.ClientGameData.Width/2 <= (potentialX) {
		// 	return true
		// }
		// if float32(math.Abs(float64(player.ClientGameData.PosY)))+player.ClientGameData.Height/2 <= (potentialY) {
		// 	return true
		// }
		overlapX := partialAABB(player.ClientGameData.PosX, player.ClientGameData.Width/2, potentialX, c.ClientGameData.Width/2)
		overlapY := partialAABB(player.ClientGameData.PosY, player.ClientGameData.Height/2, potentialY, c.ClientGameData.Height/2)

		if overlapX && overlapY {
			return true
		}
	}

	for _, col := range c.Manager.Games[c.LobbyName].Grid {
		for _, item := range col {
			if !item.HasObstacle {
				continue
			}

			overlapX := partialAABB(item.CenterX, c.Manager.Games[c.LobbyName].GridCellSize/2, potentialX, c.ClientGameData.Width/2)
			overlapY := partialAABB(item.CenterY, c.Manager.Games[c.LobbyName].GridCellSize/2, potentialY, c.ClientGameData.Height/2)

			if overlapX && overlapY {
				return true
			}
		}
	}

	return false
}
func partialAABB(ax, ad, bx, bd float32) bool {
	return ax+ad >= bx-bd && //nacrtaj sliku kad ne bude jasno :)
		bx+bd >= ax-ad
}

func calculateDistance(c *network.Client, x, y float32) float64 {
	dx := float64(c.ClientGameData.PosX - x)
	dy := float64(c.ClientGameData.PosY - y)
	return math.Sqrt(dx*dx + dy*dy)
}
