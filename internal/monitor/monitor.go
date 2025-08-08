// Prometheus 监控采集与暴露接口，仅采集核心指标
package monitor

import (
	"net/http"
	"time"
	
	"github.com/prometheus/client_golang/prometheus"
	"github.com/prometheus/client_golang/prometheus/promhttp"
)

var (
	RequestTotal = prometheus.NewCounterVec(
		prometheus.CounterOpts{
			Name: "service_request_total",
			Help: "总请求量",
		},
		[]string{"channel"},
	)
	RequestSuccess = prometheus.NewCounterVec(
		prometheus.CounterOpts{
			Name: "service_request_success",
			Help: "成功请求量",
		},
		[]string{"channel"},
	)
	RequestFail = prometheus.NewCounterVec(
		prometheus.CounterOpts{
			Name: "service_request_fail",
			Help: "失败请求量",
		},
		[]string{"channel"},
	)
	RequestLatency = prometheus.NewHistogramVec(
		prometheus.HistogramOpts{
			Name:    "service_request_latency_ms",
			Help:    "请求延迟（毫秒）",
			Buckets: prometheus.LinearBuckets(10, 20, 10),
		},
		[]string{"channel"},
	)
	ChannelQuota = prometheus.NewGaugeVec(
		prometheus.GaugeOpts{
			Name: "service_channel_quota",
			Help: "渠道额度",
		},
		[]string{"channel"},
	)
)

func init() {
	prometheus.MustRegister(RequestTotal)
	prometheus.MustRegister(RequestSuccess)
	prometheus.MustRegister(RequestFail)
	prometheus.MustRegister(RequestLatency)
	prometheus.MustRegister(ChannelQuota)
}

// 启动监控服务，暴露 /metrics
func StartMonitorServer(addr string) error {
	http.Handle("/metrics", promhttp.Handler())
	return http.ListenAndServe(addr, nil)
}

// RecordRequest 记录请求指标
func RecordRequest(channel string, success bool, latency time.Duration) {
	RequestTotal.WithLabelValues(channel).Inc()
	if success {
		RequestSuccess.WithLabelValues(channel).Inc()
	} else {
		RequestFail.WithLabelValues(channel).Inc()
	}
	RequestLatency.WithLabelValues(channel).Observe(float64(latency.Milliseconds()))
}

// UpdateChannelQuota 更新渠道额度指标
func UpdateChannelQuota(channel string, quota float64) {
	ChannelQuota.WithLabelValues(channel).Set(quota)
}