// Package fault 实现高可用与容错相关核心逻辑
package fault

import (
	"time"
)

// FaultError 标准化下游 AI 服务错误
type FaultError struct {
	Code    string      // 错误码
	Message string      // 错误信息
	Raw     interface{} // 原始错误内容
}

// StandardizeError 将任意错误标准化为 FaultError，原样返回
func StandardizeError(err error) *FaultError {
	if err == nil {
		return nil
	}
	var code string
	var raw interface{} = err
	// 可根据实际下游错误类型做更细致处理
	code = "DOWNSTREAM_ERROR"
	return &FaultError{
		Code:    code,
		Message: err.Error(),
		Raw:     raw,
	}
}

func (e *FaultError) Error() string {
	return e.Message
}

// RetryConfig 重试配置
type RetryConfig struct {
	MaxRetries    int           // 最大重试次数
	Backoff       time.Duration // 回退间隔
	BackoffFactor float64       // 回退倍数
}

// ResponseIntegrityChecker 响应完整性检测接口
type ResponseIntegrityChecker interface {
	Check(resp interface{}) bool
}

// FaultHandler 高可用与容错处理器接口
type FaultHandler interface {
	DoWithFaultTolerance(
		call func() (interface{}, error),
		checker ResponseIntegrityChecker,
		retryCfg *RetryConfig,
	) (interface{}, *FaultError)
}

// DefaultFaultHandler 默认实现
type DefaultFaultHandler struct{}

// DoWithFaultTolerance 包装下游调用，自动重试与完整性检测
func (h *DefaultFaultHandler) DoWithFaultTolerance(
	call func() (interface{}, error),
	checker ResponseIntegrityChecker,
	retryCfg *RetryConfig,
) (interface{}, *FaultError) {
	var lastErr error
	var resp interface{}
	var faultErr *FaultError

	maxRetries := 1
	backoff := time.Duration(0)
	backoffFactor := 1.0
	if retryCfg != nil {
		maxRetries = retryCfg.MaxRetries
		backoff = retryCfg.Backoff
		backoffFactor = retryCfg.BackoffFactor
	}

	for attempt := 0; attempt <= maxRetries; attempt++ {
		resp, lastErr = call()
		if lastErr != nil {
			faultErr = StandardizeError(lastErr)
		} else if checker != nil && !checker.Check(resp) {
			// 响应完整性异常，自动重试
			faultErr = &FaultError{
				Code:    "INTEGRITY_ERROR",
				Message: "Response integrity check failed",
				Raw:     resp,
			}
		} else {
			// 正常返回
			return resp, nil
		}
		// 若未达到最大重试次数则回退重试
		if attempt < maxRetries {
			time.Sleep(backoff)
			backoff = time.Duration(float64(backoff) * backoffFactor)
		}
	}
	return nil, faultErr
}

// ExampleIntegrityChecker 示例完整性检测器（可自定义实现）
type ExampleIntegrityChecker struct {
	MinTokens int
}

func (c *ExampleIntegrityChecker) Check(resp interface{}) bool {
	// 假设 resp 为 map[string]interface{}，检测 "tokens" 字段
	if m, ok := resp.(map[string]interface{}); ok {
		if tokens, ok := m["tokens"].(int); ok {
			return tokens >= c.MinTokens
		}
	}
	return false
}

// GetDefaultFaultHandler 获取默认高可用与容错处理器
func GetDefaultFaultHandler() FaultHandler {
	return &DefaultFaultHandler{}
}