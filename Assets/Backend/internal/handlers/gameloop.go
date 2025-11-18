package handlers

import (
	"encoding/json"
	"math"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
	"github.com/MisterDodik/MultiplayerGame/internal/network"
)

type PositionUpdatePayload struct {
	InputX float64 `json:"inputX"`
	InputY float64 `json:"inputY"`
}

func UpdatePositionHandler(e events.Event, c *network.Client) error {
	var payload PositionUpdatePayload
	if err := json.Unmarshal(e.Payload, &payload); err != nil {
		return err
	}

	calculate_pos(c, payload.InputX, payload.InputY)
	return nil
}

func calculate_pos(c *network.Client, inputX, inputY float64) {
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

func collides(c *network.Client, x, y float64) bool {
	for p := range c.Lobby.Clients {
		if p == c {
			continue
		}

		dx := x - p.ClientGameData.PosX
		dy := y - p.ClientGameData.PosY

		dist := math.Sqrt(dx*dx + dy*dy)

		if dist < c.ClientGameData.Radius+p.ClientGameData.Radius {
			return true
		}
	}

	return false
}
