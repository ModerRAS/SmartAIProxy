# SmartAIProxy .NET 9 API 规范

## 1. 网关API接口

### 1.1 聊天完成接口
**端点**: `POST /v1/chat/completions`
**描述**: 转发聊天完成请求到AI提供商

**请求头**:
```
Authorization: Bearer {api_key}
Content-Type: application/json
```

**请求体**:
```json
{
  "model": "gpt-3.5-turbo",
  "messages": [
    {
      "role": "user",
      "content": "Hello!"
    }
  ],
  "temperature": 0.7,
  "max_tokens": 150,
  "stream": false
}
```

**响应体** (成功):
```json
{
  "id": "chatcmpl-123",
  "object": "chat.completion",
  "created": 1677652288,
  "model": "gpt-3.5-turbo",
  "choices": [
    {
      "index": 0,
      "message": {
        "role": "assistant",
        "content": "Hello! How can I help you today?"
      },
      "finish_reason": "stop"
    }
  ],
  "usage": {
    "prompt_tokens": 9,
    "completion_tokens": 12,
    "total_tokens": 21
  }
}
```

**响应体** (流式):
```
data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1677652288,"model":"gpt-3.5-turbo","choices":[{"index":0,"delta":{"role":"assistant"},"finish_reason":null}]}

data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1677652288,"model":"gpt-3.5-turbo","choices":[{"index":0,"delta":{"content":"Hello"},"finish_reason":null}]}

data: {"id":"chatcmpl-123","object":"chat.completion.chunk","created":1677652288,"model":"gpt-3.5-turbo","choices":[{"index":0,"delta":{"content":"!"},"finish_reason":null}]}

data: [DONE]
```

### 1.2 补全接口
**端点**: `POST /v1/completions`
**描述**: 转发文本补全请求到AI提供商

**请求头**:
```
Authorization: Bearer {api_key}
Content-Type: application/json
```

**请求体**:
```json
{
  "model": "text-davinci-003",
  "prompt": "Say this is a test",
  "max_tokens": 7,
  "temperature": 0
}
```

**响应体**:
```json
{
  "id": "cmpl-uqkvlQyYK7bGYrRHQ0eXlWi7",
  "object": "text_completion",
  "created": 1589478378,
  "model": "text-davinci-003",
  "choices": [
    {
      "text": "\n\nThis is indeed a test",
      "index": 0,
      "logprobs": null,
      "finish_reason": "length"
    }
  ],
  "usage": {
    "prompt_tokens": 5,
    "completion_tokens": 7,
    "total_tokens": 12
  }
}
```

### 1.3 嵌入接口
**端点**: `POST /v1/embeddings`
**描述**: 转发嵌入请求到AI提供商

**请求头**:
```
Authorization: Bearer {api_key}
Content-Type: application/json
```

**请求体**:
```json
{
  "model": "text-embedding-ada-002",
  "input": "The food was delicious and the waiter..."
}
```

**响应体**:
```json
{
  "object": "list",
  "data": [
    {
      "object": "embedding",
      "embedding": [
        0.0023064255,
        -0.009327292,
        -0.0028842222
      ],
      "index": 0
    }
  ],
  "model": "text-embedding-ada-002",
  "usage": {
    "prompt_tokens": 8,
    "total_tokens": 8
  }
}
```

### 1.4 模型列表接口
**端点**: `GET /v1/models`
**描述**: 获取可用模型列表

**响应体**:
```json
{
  "object": "list",
  "data": [
    {
      "id": "gpt-3.5-turbo",
      "object": "model",
      "created": 1677610602,
      "owned_by": "openai",
      "permission": [...]
    },
    {
      "id": "claude-2",
      "object": "model",
      "created": 1677610602,
      "owned_by": "anthropic",
      "permission": [...]
    }
  ]
}
```

### 1.5 健康检查接口
**端点**: `GET /healthz`
**描述**: 检查网关服务健康状态

**响应体**:
```json
{
  "status": "ok"
}
```

**响应状态码**:
- 200: 服务健康
- 503: 服务不健康

## 2. 管理API接口

### 2.1 通道管理接口

#### 获取所有通道
**端点**: `GET /api/channels`
**描述**: 获取所有配置的AI通道

**响应体**:
```json
{
  "success": true,
  "message": "Channels retrieved successfully",
  "data": [
    {
      "name": "Free Channel A",
      "type": "openai",
      "endpoint": "https://api.openai.com/v1",
      "api_key": "sk-free",
      "price_per_token": 0,
      "daily_limit": 10000,
      "priority": 1,
      "status": "active"
    }
  ]
}
```

#### 添加或更新通道
**端点**: `POST /api/channels`
**描述**: 添加新通道或更新现有通道

**请求体**:
```json
{
  "name": "New Channel",
  "type": "openai",
  "endpoint": "https://api.openai.com/v1",
  "api_key": "sk-new",
  "price_per_token": 0.01,
  "daily_limit": 50000,
  "priority": 2,
  "status": "active"
}
```

**响应体**:
```json
{
  "success": true,
  "message": "Channel updated successfully",
  "data": {
    "name": "New Channel",
    "type": "openai",
    "endpoint": "https://api.openai.com/v1",
    "api_key": "sk-new",
    "price_per_token": 0.01,
    "daily_limit": 50000,
    "priority": 2,
    "status": "active"
  }
}
```

### 2.2 规则管理接口

#### 获取所有规则
**端点**: `GET /api/rules`
**描述**: 获取所有配置的路由规则

**响应体**:
```json
{
  "success": true,
  "message": "Rules retrieved successfully",
  "data": [
    {
      "name": "Free Priority",
      "channel": "Free Channel A",
      "expression": "day_tokens_used < daily_limit",
      "priority": 1
    }
  ]
}
```

#### 添加或更新规则
**端点**: `POST /api/rules`
**描述**: 添加新规则或更新现有规则

**请求体**:
```json
{
  "name": "New Rule",
  "channel": "New Channel",
  "expression": "time_of_day >= '00:00' AND time_of_day <= '06:00'",
  "priority": 2
}
```

**响应体**:
```json
{
  "success": true,
  "message": "Rule updated successfully",
  "data": {
    "name": "New Rule",
    "channel": "New Channel",
    "expression": "time_of_day >= '00:00' AND time_of_day <= '06:00'",
    "priority": 2
  }
}
```

### 2.3 配置管理接口

#### 获取当前配置
**端点**: `GET /api/config`
**描述**: 获取当前服务配置

**响应体**:
```json
{
  "success": true,
  "message": "Configuration retrieved successfully",
  "data": {
    "server": {
      "listen": "0.0.0.0:8080",
      "timeout": 30,
      "max_connections": 1000
    },
    "channels": [...],
    "rules": [...],
    "monitor": {
      "enable": true,
      "prometheus_listen": "0.0.0.0:9100"
    }
  }
}
```

### 2.4 健康检查接口
**端点**: `GET /health`
**描述**: 检查管理API健康状态

**响应体**:
```json
{
  "success": true,
  "message": "Admin API is running",
  "data": {
    "status": "healthy",
    "uptime": "2h30m",
    "version": "1.0.0"
  }
}
```

### 2.5 监控指标接口
**端点**: `GET /metrics`
**描述**: 获取Prometheus监控指标

**响应体** (文本格式):
```
# HELP smartaiproxy_requests_total Total number of requests
# TYPE smartaiproxy_requests_total counter
smartaiproxy_requests_total{channel="openai",status="success"} 1234
smartaiproxy_requests_total{channel="claude",status="success"} 567

# HELP smartaiproxy_request_duration_seconds Request duration in seconds
# TYPE smartaiproxy_request_duration_seconds histogram
smartaiproxy_request_duration_seconds_bucket{channel="openai",le="0.1"} 1000
smartaiproxy_request_duration_seconds_bucket{channel="openai",le="0.5"} 1200
smartaiproxy_request_duration_seconds_bucket{channel="openai",le="+Inf"} 1234
```

## 3. 错误响应格式

### 3.1 标准错误响应
```json
{
  "error": {
    "code": "invalid_api_key",
    "message": "Invalid API key provided",
    "type": "authentication_error"
  }
}
```

### 3.2 常见错误码
| 错误码 | HTTP状态码 | 描述 |
|--------|------------|------|
| invalid_api_key | 401 | API密钥无效 |
| rate_limit_exceeded | 429 | 请求频率超限 |
| invalid_request | 400 | 请求格式错误 |
| model_not_found | 404 | 模型不存在 |
| service_unavailable | 503 | 服务不可用 |
| timeout | 504 | 请求超时 |

## 4. 认证和授权

### 4.1 网关API认证
- 使用 `Authorization: Bearer {api_key}` 头
- API密钥在配置文件中定义
- 支持多个API密钥

### 4.2 管理API认证
- 使用JWT令牌认证
- 令牌包含角色信息
- 支持令牌刷新

## 5. 请求/响应头处理

### 5.1 保留头
以下头信息在转发过程中保留：
- `Content-Type`
- `User-Agent`
- `Accept`
- `Accept-Encoding`

### 5.2 过滤头
以下头信息在转发过程中移除：
- `Authorization` (替换为通道API密钥)
- `Host` (替换为目标主机)
- `Connection` (根据需要重新设置)

## 6. 流式响应处理

### 6.1 数据格式
- 使用 `text/event-stream` 内容类型
- 每个数据块以 `data: ` 开头
- 流结束以 `data: [DONE]` 标记

### 6.2 错误处理
流中的错误以以下格式发送：
```
data: {"error": {"code": "context_length_exceeded", "message": "Maximum context length exceeded"}}
```

## 7. 限流和配额

### 7.1 限流响应
当请求频率超过限制时返回：
```json
{
  "error": {
    "code": "rate_limit_exceeded",
    "message": "Rate limit exceeded. Please try again in 30 seconds.",
    "type": "rate_limit_error"
  }
}
```

### 7.2 配额限制响应
当通道配额用完时返回：
```json
{
  "error": {
    "code": "quota_exceeded",
    "message": "Daily quota exceeded for this channel",
    "type": "quota_error"
  }
}
```