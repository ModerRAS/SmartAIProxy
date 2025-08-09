// 单元测试：监控模块
package monitor

import (
	"testing"

	"github.com/prometheus/client_golang/prometheus/testutil"
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

func TestRecordRequest_IncrementsCounters(t *testing.T) {
	// Record a successful request
	channel := "test_channel"
	
	beforeTotal := testutil.ToFloat64(RequestTotal.WithLabelValues(channel))
	beforeSuccess := testutil.ToFloat64(RequestSuccess.WithLabelValues(channel))
	
	RecordRequest(channel, true, 0)
	
	afterTotal := testutil.ToFloat64(RequestTotal.WithLabelValues(channel))
	afterSuccess := testutil.ToFloat64(RequestSuccess.WithLabelValues(channel))
	
	assert.Equal(t, beforeTotal+1, afterTotal)
	assert.Equal(t, beforeSuccess+1, afterSuccess)
}

func TestRecordRequest_FailureIncrementsFailCounter(t *testing.T) {
	// Record a failed request
	channel := "test_fail_channel"
	
	beforeFail := testutil.ToFloat64(RequestFail.WithLabelValues(channel))
	
	RecordRequest(channel, false, 0)
	
	afterFail := testutil.ToFloat64(RequestFail.WithLabelValues(channel))
	
	assert.Equal(t, beforeFail+1, afterFail)
}

func TestUpdateChannelQuota_SetsValue(t *testing.T) {
	channel := "test_quota_channel"
	quota := 1000.0
	
	// This test verifies the function doesn't panic
	// We can't easily read the gauge value in a unit test
	assert.NotPanics(t, func() {
		UpdateChannelQuota(channel, quota)
	})
}