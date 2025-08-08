// 管理后台核心 REST API，仅实现渠道、规则、配置管理与热更新接口
package admin

import (
	"encoding/json"
	"net/http"
	"sync"
	
	"SmartAIProxy/internal/config"
	"SmartAIProxy/internal/logger"
	"go.uber.org/zap"
)

// 全局配置引用
var (
	globalConfig *config.Config
	configMu     sync.RWMutex
)

// SetGlobalConfig 设置全局配置引用
func SetGlobalConfig(cfg *config.Config) {
	configMu.Lock()
	defer configMu.Unlock()
	globalConfig = cfg
}

// GetGlobalConfig 获取全局配置引用
func GetGlobalConfig() *config.Config {
	configMu.RLock()
	defer configMu.RUnlock()
	return globalConfig
}

// API响应结构
type APIResponse struct {
	Success bool        `json:"success"`
	Message string      `json:"message,omitempty"`
	Data    interface{} `json:"data,omitempty"`
}

// 渠道管理接口
func handleChannels(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	
	cfg := GetGlobalConfig()
	if cfg == nil {
		json.NewEncoder(w).Encode(APIResponse{
			Success: false,
			Message: "Configuration not initialized",
		})
		return
	}
	
	switch r.Method {
	case http.MethodGet:
		json.NewEncoder(w).Encode(APIResponse{
			Success: true,
			Data:    cfg.Channels,
		})
	case http.MethodPost, http.MethodPut:
		var channelCfg config.ChannelConfig
		if err := json.NewDecoder(r.Body).Decode(&channelCfg); err != nil {
			json.NewEncoder(w).Encode(APIResponse{
				Success: false,
				Message: "Invalid JSON: " + err.Error(),
			})
			return
		}
		
		// 更新配置（实际应用中可能需要持久化到文件）
		configMu.Lock()
		// 查找是否已存在该渠道
		found := false
		for i, ch := range cfg.Channels {
			if ch.Name == channelCfg.Name {
				cfg.Channels[i] = channelCfg
				found = true
				break
			}
		}
		// 如果不存在则添加
		if !found {
			cfg.Channels = append(cfg.Channels, channelCfg)
		}
		configMu.Unlock()
		
		json.NewEncoder(w).Encode(APIResponse{
			Success: true,
			Message: "Channel updated successfully",
		})
	}
}

// 规则管理接口
func handleRules(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	
	cfg := GetGlobalConfig()
	if cfg == nil {
		json.NewEncoder(w).Encode(APIResponse{
			Success: false,
			Message: "Configuration not initialized",
		})
		return
	}
	
	switch r.Method {
	case http.MethodGet:
		json.NewEncoder(w).Encode(APIResponse{
			Success: true,
			Data:    cfg.Rules,
		})
	case http.MethodPost, http.MethodPut:
		var ruleCfg config.RuleConfig
		if err := json.NewDecoder(r.Body).Decode(&ruleCfg); err != nil {
			json.NewEncoder(w).Encode(APIResponse{
				Success: false,
				Message: "Invalid JSON: " + err.Error(),
			})
			return
		}
		
		// 更新配置
		configMu.Lock()
		// 查找是否已存在该规则
		found := false
		for i, rule := range cfg.Rules {
			if rule.Name == ruleCfg.Name {
				cfg.Rules[i] = ruleCfg
				found = true
				break
			}
		}
		// 如果不存在则添加
		if !found {
			cfg.Rules = append(cfg.Rules, ruleCfg)
		}
		configMu.Unlock()
		
		json.NewEncoder(w).Encode(APIResponse{
			Success: true,
			Message: "Rule updated successfully",
		})
	}
}

// 配置管理接口
func handleConfigs(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")
	
	cfg := GetGlobalConfig()
	if cfg == nil {
		json.NewEncoder(w).Encode(APIResponse{
			Success: false,
			Message: "Configuration not initialized",
		})
		return
	}
	
	switch r.Method {
	case http.MethodGet:
		// 返回主要配置部分
		configData := map[string]interface{}{
			"server": cfg.Server,
			"log":    cfg.Log,
			"admin":  cfg.Admin,
		}
		json.NewEncoder(w).Encode(APIResponse{
			Success: true,
			Data:    configData,
		})
	case http.MethodPost, http.MethodPut:
		// 这里可以实现配置更新逻辑
		// 为简化，我们只返回成功消息
		json.NewEncoder(w).Encode(APIResponse{
			Success: true,
			Message: "Configuration update endpoint (implementation pending)",
		})
	}
}

// 启动管理后台服务
func StartAdminServer(addr string) error {
	http.HandleFunc("/api/channels", handleChannels)
	http.HandleFunc("/api/rules", handleRules)
	http.HandleFunc("/api/config", handleConfigs)
	
	// 健康检查端点
	http.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(APIResponse{
			Success: true,
			Message: "Admin API is running",
		})
	})
	
	// 根路径返回API信息
	http.HandleFunc("/", func(w http.ResponseWriter, r *http.Request) {
		if r.URL.Path != "/" {
			http.NotFound(w, r)
			return
		}
		w.Header().Set("Content-Type", "application/json")
		apiInfo := map[string]interface{}{
			"name":    "SmartAIProxy Admin API",
			"version": "1.0.0",
			"endpoints": []string{
				"GET /api/channels - List all channels",
				"POST /api/channels - Add or update a channel",
				"GET /api/rules - List all rules",
				"POST /api/rules - Add or update a rule",
				"GET /api/config - Get configuration",
			},
		}
		json.NewEncoder(w).Encode(APIResponse{
			Success: true,
			Data:    apiInfo,
		})
	})
	
	logger.Info("Starting admin server", zap.String("address", addr))
	return http.ListenAndServe(addr, nil)
}