package main

import (
	"encoding/json"
	"errors"
	"log"
	"net/http"
	"sync"

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
	games  GameServerList
	events map[string]EventHandler
	otps   RetentionMap
	sync.RWMutex
}

func NewManager() *Manager {
	m := &Manager{
		// clients: make(ClientList),
		games:  make(GameServerList),
		events: make(map[string]EventHandler),
		otps:   NewRetentionMap(),
	}

	m.initHandlers()

	return m
}

func (m *Manager) initHandlers() {
	m.events[JoinLobby] = JoinLobbyHandler
	m.events[StartGame] = StartGameHandler
	m.events[ChatroomMsg] = ChatMsgFromClientHandler
}

func (m *Manager) parseEvent(e Event, c *Client) error {
	event, ok := m.events[e.Type]
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

func (m *Manager) login(w http.ResponseWriter, r *http.Request) {
	var payload LoginPayload

	if err := json.NewDecoder(r.Body).Decode(&payload); err != nil {
		log.Printf("error decoding login request body %v", err)
		w.WriteHeader(http.StatusUnauthorized)
		return
	}

	if _, ok := m.games[payload.Seed]; ok { //otherwise the desired gameserver is empty (non existent)
		count := 0
		for c := range m.games[payload.Seed].clients {
			if c.lobbyName == payload.Seed {
				count++
			}
		}

		if count >= 8 {
			w.WriteHeader(http.StatusUnauthorized)
			_, _ = w.Write([]byte("lobby full"))
			return
		}
	}

	otp, err := m.otps.NewOTP(payload.Username, payload.Seed)
	if err != nil {
		log.Println(err)
		w.WriteHeader(http.StatusUnauthorized)
		return
	}
	m.otps[otp.key] = *otp

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
func (m *Manager) serveWS(w http.ResponseWriter, r *http.Request) {
	var userName string
	var lobbyName string

	otp := r.URL.Query().Get("otp")
	if otp == "" || !m.otps.ValidateOTP(otp, &userName, &lobbyName) {
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
	val, ok := m.games[lobbyName]
	if !ok {
		lobby = m.NewGameServer(lobbyName)
	} else {
		lobby = val
	}

	client := NewClient(conn, m, userName, lobbyName, otp, lobby)

	m.addClient(lobbyName, client)

	go client.ReadMessage()
	go client.WriteMessage()

	lbJson, _ := json.Marshal(lobbyName)

	evt := Event{
		Type:    JoinLobby,
		Payload: json.RawMessage(lbJson),
	}
	if err := m.parseEvent(evt, client); err != nil {
		m.removeClient(client)
	}
}

func (m *Manager) addClient(lobbyName string, client *Client) {
	m.Lock()
	defer m.Unlock()

	m.games[lobbyName].clients[client] = true

	log.Println("new client")
}

func (m *Manager) removeClient(client *Client) {
	m.Lock()
	defer m.Unlock()

	if _, ok := m.games[client.lobbyName].clients[client]; ok {

		if err := updateLobby(client, DepopulateLobby); err != nil {
			log.Println(err)
		}
		client.conn.Close()
		delete(m.games[client.lobbyName].clients, client)

		log.Println("client removed")
	}
}
