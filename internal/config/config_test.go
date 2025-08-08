// 单元测试：配置模块
package config

import (
	"testing"
	"github.com/stretchr/testify/assert"
)

func TestLoadConfig_Normal(t *testing.T) {
	// 正常路径，需准备测试配置文件
	cfg, err := LoadConfig("testdata/config.yaml")
	assert.NoError(t, err)
	assert.NotNil(t, cfg)

	// Validate some values
	assert.Equal(t, "0.0.0.0:8080", cfg.Server.Listen)
	assert.Equal(t, 3, len(cfg.Channels))
	assert.Equal(t, "免费渠道A", cfg.Channels[0].Name)
}

func TestLoadConfig_Error(t *testing.T) {
	// 异常路径
	cfg, err := LoadConfig("not_exist.yaml")
	assert.Error(t, err)
	assert.Nil(t, cfg)
}

func TestGetConfig(t *testing.T) {
	// 测试获取配置
	_, err := LoadConfig("testdata/config.yaml")
	assert.NoError(t, err)
	cfg := GetConfig()
	assert.NotNil(t, cfg)
	assert.Equal(t, "0.0.0.0:8080", cfg.Server.Listen)
}