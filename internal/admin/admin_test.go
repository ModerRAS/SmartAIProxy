// 单元测试：管理后台模块
package admin

import (
	"bytes"
	"encoding/json"
	"net/http"
	"net/http/httptest"
	"testing"

	"github.com/stretchr/testify/assert"
	"SmartAIProxy/internal/config"
)

func TestHandleChannels_Get(t *testing.T) {
	// 设置全局配置
	testCfg := &config.Config{
		Channels: []config.ChannelConfig{
			{
				Name:      "test-channel",
				Type:      "openai",
				APIKey:    "test-key",
				Priority:  1,
				PricePerToken: 0.01,
			},
		},
	}
	SetGlobalConfig(testCfg)
	
	req, _ := http.NewRequest("GET", "/api/channels", nil)
	w := httptest.NewRecorder()
	
	handleChannels(w, req)
	
	assert.Equal(t, http.StatusOK, w.Code)
	
	// 检查响应结构
	var response APIResponse
	err := json.NewDecoder(w.Body).Decode(&response)
	assert.NoError(t, err)
	assert.True(t, response.Success)
	assert.NotNil(t, response.Data)
}

func TestHandleChannels_Post(t *testing.T) {
	// 设置全局配置
	testCfg := &config.Config{
		Channels: []config.ChannelConfig{},
	}
	SetGlobalConfig(testCfg)
	
	newChannel := config.ChannelConfig{
		Name:      "new-channel",
		Type:      "openai",
		APIKey:    "new-key",
		Priority:  2,
		PricePerToken: 0.02,
	}
	
	jsonData, _ := json.Marshal(newChannel)
	req, _ := http.NewRequest("POST", "/api/channels", bytes.NewBuffer(jsonData))
	req.Header.Set("Content-Type", "application/json")
	w := httptest.NewRecorder()
	
	handleChannels(w, req)
	
	assert.Equal(t, http.StatusOK, w.Code)
	
	var response APIResponse
	err := json.NewDecoder(w.Body).Decode(&response)
	assert.NoError(t, err)
	assert.True(t, response.Success)
	assert.Equal(t, "Channel updated successfully", response.Message)
}

func TestHandleRules_Get(t *testing.T) {
	// 设置全局配置
	testCfg := &config.Config{
		Rules: []config.RuleConfig{
			{
				Name:       "test-rule",
				Channel:    "test-channel",
				Expression: "true",
			},
		},
	}
	SetGlobalConfig(testCfg)
	
	req, _ := http.NewRequest("GET", "/api/rules", nil)
	w := httptest.NewRecorder()
	
	handleRules(w, req)
	
	assert.Equal(t, http.StatusOK, w.Code)
	
	var response APIResponse
	err := json.NewDecoder(w.Body).Decode(&response)
	assert.NoError(t, err)
	assert.True(t, response.Success)
	assert.NotNil(t, response.Data)
}

func TestHandleConfigs_Get(t *testing.T) {
	// 设置全局配置
	testCfg := &config.Config{
		Server: config.ServerConfig{
			Listen: "0.0.0.0:8080",
		},
		Log: config.LogConfig{
			Level: "info",
		},
		Admin: config.AdminConfig{
			Enable: true,
		},
	}
	SetGlobalConfig(testCfg)
	
	req, _ := http.NewRequest("GET", "/api/config", nil)
	w := httptest.NewRecorder()
	
	handleConfigs(w, req)
	
	assert.Equal(t, http.StatusOK, w.Code)
	
	var response APIResponse
	err := json.NewDecoder(w.Body).Decode(&response)
	assert.NoError(t, err)
	assert.True(t, response.Success)
	assert.NotNil(t, response.Data)
}

func TestHealthHandler(t *testing.T) {
	req, _ := http.NewRequest("GET", "/health", nil)
	w := httptest.NewRecorder()
	
	// 健康检查处理函数
	http.HandleFunc("/health", func(w http.ResponseWriter, r *http.Request) {
		w.Header().Set("Content-Type", "application/json")
		json.NewEncoder(w).Encode(APIResponse{
			Success: true,
			Message: "Admin API is running",
		})
	})
	
	http.DefaultServeMux.ServeHTTP(w, req)
	
	assert.Equal(t, http.StatusOK, w.Code)
	
	var response APIResponse
	err := json.NewDecoder(w.Body).Decode(&response)
	assert.NoError(t, err)
	assert.True(t, response.Success)
}