// internal/gateway/gateway.go
package gateway

import (
	"bytes"
	"encoding/json"
	"fmt"
	"io"
	"log"
	"net/http"
	"strings"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/didip/tollbooth/v7"
	"github.com/didip/tollbooth_gin"
	"github.com/golang-jwt/jwt/v4"
	"SmartAIProxy/internal/config"
	"SmartAIProxy/internal/fault"
	"SmartAIProxy/internal/logger"
	"SmartAIProxy/internal/monitor"
	"SmartAIProxy/internal/provider"
	"SmartAIProxy/internal/rule"
	"SmartAIProxy/internal/router"
	"go.uber.org/zap"
)

// 全局路由器实例
var (
	providerManager *provider.ProviderManager
	ruleManager     *rule.RuleManager
	routerInstance  *router.Router
)

// ForwardResult 用于封装转发结果
type ForwardResult struct {
	StatusCode  int
	ContentType string
	Body        []byte
}

// 初始化路由器和配置
func InitializeGateway(cfg *config.Config) {
	providerManager = provider.NewProviderManager()
	ruleManager = rule.NewRuleManager()
	
	// 从配置加载渠道商
	for _, channelCfg := range cfg.Channels {
		provider := &provider.Provider{
			ID:       channelCfg.Name,
			Type:     provider.ProviderType(channelCfg.Type),
			Quota:    float64(channelCfg.DailyLimit), // 初始额度设为每日限制
			Price:    channelCfg.PricePerToken,
			Priority: channelCfg.Priority,
			Discount: calculateDiscount(channelCfg.DiscountTime),
			Status:   provider.StatusActive,
			Meta: map[string]interface{}{
				"endpoint": channelCfg.Endpoint,
				"api_key":  channelCfg.APIKey,
			},
		}
		providerManager.AddProvider(provider)
	}
	
	// 从配置加载规则
	for _, ruleCfg := range cfg.Rules {
		rule := &rule.Rule{
			ID:         ruleCfg.Name,
			Expr:       ruleCfg.Expression,
			Priority:   len(cfg.Rules) - len(cfg.Rules), // 按配置顺序
			ProviderID: ruleCfg.Channel,
		}
		ruleManager.AddRule(rule)
	}
	
	// 创建路由器实例
	routerInstance = router.NewRouter(providerManager, ruleManager)
}

// 计算折扣（简化版本）
func calculateDiscount(discountTime []string) float64 {
	if len(discountTime) > 0 {
		// 简化逻辑：如果设置了折扣时间，返回默认折扣
		return 0.1
	}
	return 0.0
}

// JWT 鉴权中间件
func AuthMiddleware() gin.HandlerFunc {
	return func(c *gin.Context) {
		authHeader := c.GetHeader("Authorization")
		if authHeader == "" {
			c.AbortWithStatusJSON(http.StatusUnauthorized, gin.H{"error": "Missing Authorization header"})
			return
		}
		tokenString := strings.TrimPrefix(authHeader, "Bearer ")
		// 这里仅做结构校验，实际密钥与 claims 校验可扩展
		_, err := jwt.Parse(tokenString, func(token *jwt.Token) (interface{}, error) {
			// TODO: 替换为实际密钥
			return []byte("your-secret-key"), nil
		})
		if err != nil {
			c.AbortWithStatusJSON(http.StatusUnauthorized, gin.H{"error": "Invalid token"})
			return
		}
		c.Next()
	}
}

// API Key 鉴权中间件（可选，示例）
func ApiKeyMiddleware() gin.HandlerFunc {
	return func(c *gin.Context) {
		apiKey := c.GetHeader("X-API-Key")
		if apiKey == "" || apiKey != "your-api-key" {
			c.AbortWithStatusJSON(http.StatusUnauthorized, gin.H{"error": "Invalid API Key"})
			return
		}
		c.Next()
	}
}

// 限流中间件（基于 IP）
func RateLimitMiddleware() gin.HandlerFunc {
	limiter := tollbooth.NewLimiter(10, nil) // 10 req/sec
	return tollbooth_gin.LimitHandler(limiter)
}

// 请求预处理：参数校验与日志
func PreprocessMiddleware() gin.HandlerFunc {
	return func(c *gin.Context) {
		// 简单参数校验示例
		if c.Request.Method == http.MethodPost && c.ContentType() != "application/json" {
			c.AbortWithStatusJSON(http.StatusBadRequest, gin.H{"error": "Content-Type must be application/json"})
			return
		}
		// 日志记录
		log.Printf("[%s] %s %s from %s", time.Now().Format(time.RFC3339), c.Request.Method, c.Request.URL.Path, c.ClientIP())
		c.Next()
	}
}

// 错误与异常处理
func ErrorHandler() gin.HandlerFunc {
	return func(c *gin.Context) {
		c.Next()
		if len(c.Errors) > 0 {
			// 统一错误输出
			c.JSON(-1, gin.H{"error": c.Errors[0].Error()})
		}
	}
}

// 请求转发到路由模块
func ProxyHandler() gin.HandlerFunc {
	return func(c *gin.Context) {
		// 参数收集与预处理
		params := make(map[string]interface{})
		params["method"] = c.Request.Method
		params["path"] = c.Request.URL.Path
		params["model"] = c.Param("model")
		params["action"] = c.Param("action")
		
		// 添加时间参数用于规则匹配
		now := time.Now()
		params["time"] = now.Format("15:04")
		params["hour"] = now.Hour()
		params["day"] = now.Weekday().String()
		
		// 添加客户端信息
		params["client_ip"] = c.ClientIP()
		
		// 收集请求体
		if c.Request.Method == http.MethodPost || c.Request.Method == http.MethodPut {
			bodyBytes, err := io.ReadAll(c.Request.Body)
			if err != nil {
				c.AbortWithStatusJSON(http.StatusBadRequest, gin.H{"error": "Failed to read request body"})
				return
			}
			// 恢复请求体供后续使用
			c.Request.Body = io.NopCloser(bytes.NewBuffer(bodyBytes))
			
			// 尝试解析JSON
			var body map[string]interface{}
			if err := json.Unmarshal(bodyBytes, &body); err == nil {
				for k, v := range body {
					params[k] = v
				}
			}
		}
		
		// 调用路由模块自动选择渠道商
		if routerInstance == nil {
			c.AbortWithStatusJSON(http.StatusInternalServerError, gin.H{"error": "Router not initialized"})
			return
		}
		
		result, err := routerInstance.SelectBestProvider(params)
		if err != nil {
			logger.Error("Failed to select provider", zap.Error(err))
			c.AbortWithStatusJSON(http.StatusBadGateway, gin.H{"error": err.Error()})
			return
		}
		
		if result == nil || result.Provider == nil {
			c.AbortWithStatusJSON(http.StatusServiceUnavailable, gin.H{"error": "No available provider"})
			return
		}
		
		// 获取选中的渠道商信息
		selectedProvider := result.Provider
		endpoint, _ := selectedProvider.Meta["endpoint"].(string)
		apiKey, _ := selectedProvider.Meta["api_key"].(string)
		
		// 构造目标URL
		targetURL := endpoint + c.Request.URL.Path
		if c.Request.URL.RawQuery != "" {
			targetURL += "?" + c.Request.URL.RawQuery
		}
		
		// 转发请求到选中的渠道商
		forwardRequest(c, targetURL, apiKey, selectedProvider.ID)
	}
}

// 转发请求到目标服务
func forwardRequest(c *gin.Context, targetURL string, apiKey string, channelName string) {
	startTime := time.Now()
	
	// 创建容错处理器
	faultHandler := fault.GetDefaultFaultHandler()
	
	// 配置重试策略
	retryConfig := &fault.RetryConfig{
		MaxRetries:    2,
		Backoff:       1 * time.Second,
		BackoffFactor: 2.0,
	}
	
	// 将原始转发逻辑包装在容错处理中
	result, faultErr := faultHandler.DoWithFaultTolerance(
		func() (interface{}, error) {
			return forwardRequestWithClient(c, targetURL, apiKey)
		},
		nil, // 暂时不使用完整性检查
		retryConfig,
	)
	
	// 记录监控指标
	latency := time.Since(startTime)
	success := faultErr == nil
	
	if faultErr != nil {
		logger.Error("Failed to forward request after retries", zap.Error(faultErr))
		c.AbortWithStatusJSON(http.StatusBadGateway, gin.H{"error": faultErr.Message})
	} else {
		// 返回成功结果
		if forwardResult, ok := result.(ForwardResult); ok {
			c.Data(forwardResult.StatusCode, forwardResult.ContentType, forwardResult.Body)
		}
	}
	
	// 记录监控指标
	monitor.RecordRequest(channelName, success, latency)
}

// forwardRequestWithClient 执行实际的发送请求操作
func forwardRequestWithClient(c *gin.Context, targetURL string, apiKey string) (ForwardResult, error) {
	// 创建新的请求
	req, err := http.NewRequest(c.Request.Method, targetURL, c.Request.Body)
	if err != nil {
		return ForwardResult{}, fmt.Errorf("failed to create request: %w", err)
	}
	
	// 复制请求头
	for key, values := range c.Request.Header {
		for _, value := range values {
			req.Header.Add(key, value)
		}
	}
	
	// 设置目标服务的API Key
	if apiKey != "" {
		req.Header.Set("Authorization", "Bearer "+apiKey)
	}
	
	// 发送请求
	client := &http.Client{
		Timeout: 300 * time.Second, // 5分钟超时
	}
	
	resp, err := client.Do(req)
	if err != nil {
		return ForwardResult{}, fmt.Errorf("failed to send request: %w", err)
	}
	defer resp.Body.Close()
	
	// 读取响应
	body, err := io.ReadAll(resp.Body)
	if err != nil {
		return ForwardResult{}, fmt.Errorf("failed to read response: %w", err)
	}
	
	// 返回结果
	return ForwardResult{
		StatusCode:  resp.StatusCode,
		ContentType: resp.Header.Get("Content-Type"),
		Body:        body,
	}, nil
}

// 网关入口
func StartGateway(cfg *config.Config) {
	// 初始化网关
	InitializeGateway(cfg)
	
	// 启动监控服务如果启用
	if cfg.Monitor.Enable {
		go func() {
			logger.Info("Starting monitor server", zap.String("address", cfg.Monitor.PrometheusListen))
			if err := monitor.StartMonitorServer(cfg.Monitor.PrometheusListen); err != nil {
				logger.Error("Failed to start monitor server", zap.Error(err))
			}
		}()
	}
	
	r := gin.Default()
	r.Use(ErrorHandler())
	r.Use(RateLimitMiddleware())
	r.Use(PreprocessMiddleware())
	r.Use(AuthMiddleware())
	// 如需 API Key 校验可启用
	// r.Use(ApiKeyMiddleware())

	// 标准 HTTP 接口，支持主流 AI 模型 API 请求转发
	r.POST("/v1/:model/*action", ProxyHandler())
	r.GET("/v1/:model/*action", ProxyHandler())

	// 健康检查
	r.GET("/healthz", func(c *gin.Context) {
		c.JSON(http.StatusOK, gin.H{"status": "ok"})
	})

	addr := cfg.Server.Listen
	logger.Info("Starting gateway server", zap.String("address", addr))
	if err := r.Run(addr); err != nil {
		log.Fatalf("Gateway start failed: %v", err)
	}
}