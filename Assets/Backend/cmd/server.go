package main

import (
	"log"
	"net/http"

	"github.com/MisterDodik/MultiplayerGame/internal/handlers"
	"github.com/MisterDodik/MultiplayerGame/internal/network"
)

func setupAPI() {
	manager := network.NewManager()
	manager.Events = handlers.EventHandlers
	//http.Handle("/", http.FileServer(http.Dir("./frontend")))

	http.HandleFunc("/ws", manager.ServeWS)
	http.HandleFunc("/login", manager.Login)
}
func main() {
	setupAPI()
	log.Println(http.ListenAndServe(":8080", nil))
}
