## Multiplayer Tag Game (Prototype)

Real-time multiplayer game prototype built with:
- Golang WebSocket server
- Unity client

### Features
- WebSocket-based real-time communication
- Client-server architecture
- Player state synchronization
- Infected / tag-style gameplay logic
- Supports 2–8 players per lobby

### How to Run
1. Start the Golang server:  go run ./...
2. Launch the Unity client or game build
3. Enter a username and lobby seed
4. Click **Join Lobby**

### Controls
- **WASD** – Move
- **Space** – Infect nearby players (when infected)

### Gameplay
Players start as either infected or non-infected.
- Non-infected players' goal is to survive as long as possible
- Infected players have to chase and infect other players
