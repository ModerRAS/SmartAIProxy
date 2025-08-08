// 单元测试：监控模块
package monitor

import (
	"testing"
	"github.com/stretchr/testify/assert"
)

func TestMetrics_Register(t *testing.T) {
	assert.NotNil(t, RequestTotal)
	assert.NotNil(t, RequestSuccess)
	assert.NotNil(t, RequestFail)
	assert.NotNil(t, RequestLatency)
	assert.NotNil(t, ChannelQuota)
}

func TestStartMonitorServer_Error(t *testing.T) {
	// 启动端口冲突或非法地址
	err := StartMonitorServer("invalid_addr")
	assert.Error(t, err)
}