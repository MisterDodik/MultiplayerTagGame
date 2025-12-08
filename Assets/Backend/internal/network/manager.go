package network

import (
	"encoding/json"
	"errors"
	"fmt"
	"log"
	"math/rand/v2"
	"net/http"
	"sync"
	"time"

	"github.com/MisterDodik/MultiplayerGame/internal/events"
	"github.com/gorilla/websocket"
)

var upgrader = websocket.Upgrader{
	ReadBufferSize:  1024,
	WriteBufferSize: 1024,
	CheckOrigin: func(r *http.Request) bool {
		return true
	},
}

type Manager struct {
	//clients ClientList
	Games  GameServerList
	Events map[string]func(events.Event, *Client) error
	Otps   RetentionMap
	sync.RWMutex
}

var (
	gameTickRate         = 50 * time.Millisecond
	startPositionOriginX = 4
	startPositionOriginY = 0
)

func NewManager() *Manager {
	m := &Manager{
		// clients: make(ClientList),
		Games:  make(GameServerList),
		Events: make(map[string]func(events.Event, *Client) error),
		Otps:   NewRetentionMap(),
	}

	// m.initHandlers()

	return m
}

// func (m *Manager) initHandlers() {
// 	m.Events[events.JoinLobby] = handlers.JoinLobbyHandler
// 	m.Events[events.StartGame] = handlers.StartGameHandler
// 	m.Events[events.ChatroomMsg] = handlers.ChatMsgFromClientHandler
// }

func (m *Manager) parseEvent(e events.Event, c *Client) error {
	event, ok := m.Events[e.Type]
	if !ok {
		return errors.New("unknown event type")
	}
	if err := event(e, c); err != nil {
		log.Printf("error handling event: %v;  err: %v", e, err)
		return err
	}
	return nil
}

type LoginPayload struct {
	Seed     string `json:"seed"`
	Username string `json:"username"`
}

func (m *Manager) Login(w http.ResponseWriter, r *http.Request) {
	var payload LoginPayload

	if err := json.NewDecoder(r.Body).Decode(&payload); err != nil {
		log.Printf("error decoding login request body %v", err)
		w.WriteHeader(http.StatusUnauthorized)
		return
	}

	if _, ok := m.Games[payload.Seed]; ok { //otherwise the desired gameserver is empty (non existent)
		if m.Games[payload.Seed].IsStarted {
			w.WriteHeader(http.StatusUnauthorized)
			_, _ = w.Write([]byte("game already started"))
			return
		}

		count := 0
		for c := range m.Games[payload.Seed].Clients {
			if c.LobbyName == payload.Seed {
				count++
			}
		}

		if count >= 8 {
			w.WriteHeader(http.StatusUnauthorized)
			_, _ = w.Write([]byte("lobby full"))
			return
		}
	}

	otp, err := m.Otps.NewOTP(payload.Username, payload.Seed)
	if err != nil {
		log.Println(err)
		w.WriteHeader(http.StatusUnauthorized)
		return
	}
	m.Otps[otp.key] = *otp

	type otpResponse struct {
		OTP string `json:"otp"`
	}
	res := otpResponse{
		OTP: otp.key,
	}
	data, err := json.Marshal(res)

	if err != nil {
		log.Printf("error marshaling login response %v", err)
		w.WriteHeader(http.StatusUnauthorized)
		return
	}

	w.WriteHeader(http.StatusOK)
	_, _ = w.Write(data)
}
func (m *Manager) ServeWS(w http.ResponseWriter, r *http.Request) {
	var userName string
	var lobbyName string

	otp := r.URL.Query().Get("otp")
	if otp == "" || !m.Otps.ValidateOTP(otp, &userName, &lobbyName) {
		w.WriteHeader(http.StatusUnauthorized)
		return
	}

	conn, err := upgrader.Upgrade(w, r, nil)
	if err != nil {
		log.Println("error upgrading the conn: ", err)
		return
	}
	log.Println("new connection")

	var lobby *GameServer
	val, ok := m.Games[lobbyName]
	if !ok {
		lobby = m.NewGameServer(lobbyName, 60, 30, .5, -15, -7.5)
		//lobby = m.NewGameServer(lobbyName, 6, 3, .5, -15, -7.5)
	} else {
		lobby = val
	}

	R, G, B := rand.IntN(255), rand.IntN(255), rand.IntN(255)
	newColor := fmt.Sprintf("#%02X%02X%02X", R, G, B)
	client := NewClient(conn, m, userName, lobbyName, otp, lobby, newColor)

	m.addClient(lobbyName, client)

	go client.ReadMessage()
	go client.WriteMessage()

	lbJson, _ := json.Marshal(lobbyName)

	evt := events.Event{
		Type:    events.JoinLobby,
		Payload: json.RawMessage(lbJson),
	}
	if err := m.parseEvent(evt, client); err != nil {
		m.removeClient(client)
	}

}

func (m *Manager) addClient(lobbyName string, client *Client) {
	m.Lock()
	defer m.Unlock()

	m.Games[lobbyName].Clients[client] = true

	log.Println("new client")
}

func (m *Manager) removeClient(client *Client) {
	m.Lock()
	defer m.Unlock()

	if _, ok := m.Games[client.LobbyName].Clients[client]; ok {

		if err := m.parseEvent(events.Event{Type: events.DepopulateLobby, Payload: json.RawMessage{}}, client); err != nil {
			log.Println(err)
		}
		client.Conn.Close()
		delete(m.Games[client.LobbyName].Clients, client)

		log.Println("client removed")
	}
}
