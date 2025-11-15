package main

type GameServerList map[string]*GameServer

type GameServer struct {
	clients ClientList
}

func (m *Manager) NewGameServer(lobbyName string) *GameServer {
	gs := &GameServer{
		clients: make(ClientList),
	}
	m.games[lobbyName] = gs
	return gs
}
