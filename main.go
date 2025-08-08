package main

import (
	"fmt"
	"log"

	"SmartAIProxy/internal/admin"
	"SmartAIProxy/internal/config"
	"SmartAIProxy/internal/gateway"
	"SmartAIProxy/internal/logger"
	"go.uber.org/zap"
)

func main() {
	// Load configuration
	cfg, err := config.LoadConfig("configs/config.yaml")
	if err != nil {
		log.Fatalf("Failed to load configuration: %v", err)
	}

	// Initialize logger
	logger.InitLogger(cfg.Log)
	defer logger.Sync()

	// Set global config for admin API
	admin.SetGlobalConfig(cfg)

	// Start admin server if enabled
	if cfg.Admin.Enable {
		go func() {
			if err := admin.StartAdminServer(cfg.Admin.Listen); err != nil {
				logger.Error("Failed to start admin server", zap.Error(err))
			}
		}()
	}

	// Start the gateway server
	logger.Info(fmt.Sprintf("Starting gateway server on %s", cfg.Server.Listen))
	gateway.StartGateway(cfg)
}