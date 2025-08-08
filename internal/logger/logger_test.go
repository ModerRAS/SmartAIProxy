// 单元测试：日志模块
package logger

import (
	"testing"
	"github.com/stretchr/testify/assert"
	"SmartAIProxy/internal/config"
)

func TestInitLogger_Normal(t *testing.T) {
	cfg := config.LogConfig{
		File:  "/tmp/test.log",
		Level: "info",
	}
	InitLogger(cfg)
	assert.NotNil(t, Logger)
}

func TestInfo_Error_Debug_Sync(t *testing.T) {
	cfg := config.LogConfig{
		File:  "/tmp/test.log",
		Level: "info",
	}
	InitLogger(cfg)
	Info("info test")
	Error("error test")
	Debug("debug test")
	Sync()
}