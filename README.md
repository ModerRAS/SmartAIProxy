# SmartAIProxy .NET 9 实现

[![.NET 9](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Build Status](https://github.com/ModerRAS/SmartAIProxy/workflows/CI/CD%20Pipeline/badge.svg)](https://github.com/ModerRAS/SmartAIProxy/actions)
[![License](https://img.shields.io/github/license/ModerRAS/SmartAIProxy)](LICENSE)

这是SmartAIProxy的完整.NET 9实现，一个高性能、可扩展的AI API网关服务，用于将API请求转发到主要的AI模型（OpenAI、Anthropic Claude、Google Gemini等）。

[English Documentation](README_en.md)

## 功能特点

- **API网关**：通过智能路由将请求转发到AI服务提供商
- **智能路由**：基于规则、配额和定价选择最佳通道
- **配置管理**：基于YAML的配置，支持热重载
- **通道管理**：支持多个AI提供商，具有配额跟踪功能
- **规则引擎**：使用NCalc的基于表达式的路由规则
- **管理API**：用于管理通道和规则的RESTful端点
- **安全性**：管理端点的JWT身份验证，网关的API密钥身份验证
- **监控**：Prometheus指标集成
- **容错性**：重试机制和断路器模式
- **速率限制**：按API密钥的速率限制
- **日志记录**：使用Serilog的结构化日志记录

## 架构

应用程序遵循清洁架构模式，包含以下层次：

1. **API层**：ASP.NET Core控制器和中间件
2. **核心层**：业务逻辑服务（配置、通道、规则）
3. **模型层**：数据传输对象和配置模型
4. **中间件层**：用于请求转发的自定义代理中间件

## 技术栈

- **框架**：ASP.NET Core 9.0
- **配置**：YAML配置文件，支持热重载
- **规则引擎**：NCalc表达式引擎
- **监控**：Prometheus指标
- **日志**：Serilog结构化日志
- **测试**：xUnit单元测试框架
- **容器化**：Docker支持

## 项目结构

```
SmartAIProxy/
├── SmartAIProxy/                    # 主应用程序
│   ├── Controllers/                # API控制器
│   ├── Core/                       # 核心业务逻辑
│   │   ├── Channels/              # 通道管理服务
│   │   ├── Config/                # 配置管理服务
│   │   └── Rules/                 # 规则引擎
│   ├── Middleware/                 # 自定义中间件
│   ├── Models/                     # 数据模型和DTO
│   ├── config/                     # 配置文件
│   ├── monitoring/                 # 监控配置
│   ├── Dockerfile                 # Docker配置
│   ├── docker-compose.yml         # 编排配置
│   └── Program.cs                 # 应用程序入口点
├── SmartAIProxy.Tests/             # 测试套件
│   ├── Unit/                      # 单元测试
│   ├── Integration/               # 集成测试
│   └── Controllers/              # 控制器测试
├── docs/                          # 文档
│   ├── architecture.md            # 架构文档
│   ├── tech-stack.md              # 技术栈
│   └── README.md                  # 文档索引
├── .github/workflows/              # CI/CD工作流
│   └── ci.yml                     # GitHub Actions配置
├── README.md                      # 项目文档
└── README_zh.md                   # 中文文档
```

## 快速开始

### 前提条件

- .NET 9 SDK
- Docker（可选，用于容器化部署）

### 构建应用程序

```bash
dotnet build
```

### 运行应用程序

```bash
dotnet run --project SmartAIProxy
```

应用程序默认将在端口8080上启动。

### 运行测试

```bash
dotnet test SmartAIProxy.Tests
```

## 配置

应用程序使用位于`config`目录中的YAML配置文件。首次运行时会创建默认配置文件。

### 主要配置部分

- **server**：服务器设置（监听地址、超时、最大连接数）
- **channels**：AI服务提供商配置
- **rules**：带有表达式的路由规则
- **monitor**：Prometheus监控设置
- **security**：身份验证和速率限制设置

### 示例配置

```yaml
server:
  listen: "0.0.0.0:8080"
  timeout: 30
  max_connections: 1000

channels:
  - name: "免费通道A"
    type: "openai"
    endpoint: "https://api.openai.com/v1"
    api_key: "your-openai-api-key"
    price_per_token: 0
    daily_limit: 10000
    priority: 1
    status: "active"
    model_mapping:
      "gpt-3.5-turbo": "gpt-4"

  - name: "付费通道B"
    type: "openai"
    endpoint: "https://api.openai.com/v1"
    api_key: "your-openai-api-key"
    price_per_token: 0.01
    daily_limit: 50000
    priority: 2
    status: "active"

rules:
  - name: "免费优先"
    channel: "免费通道A"
    expression: "day_tokens_used < daily_limit"
    priority: 1

  - name: "折扣时段"
    channel: "免费通道A"
    expression: "time_of_day >= '00:00' AND time_of_day <= '06:00'"
    priority: 2

monitor:
  enable: true
  prometheus_listen: "0.0.0.0:9100"

security:
  auth:
    jwt:
      secret: "your-secret-key-here"
      issuer: "SmartAIProxy"
      audience: "SmartAIProxy-Client"
      expiry_minutes: 60
    api_keys:
      default: "your-api-key-here"
  rate_limit:
    requests_per_minute: 60
    burst: 10
```

## API端点

### 网关API

- `POST /v1/chat/completions` - 转发聊天完成请求
- `POST /v1/completions` - 转发完成请求
- `POST /v1/embeddings` - 转发嵌入请求
- `GET /healthz` - 健康检查端点

### 管理API

- `GET /api/channels` - 获取所有配置的通道
- `POST /api/channels` - 添加或更新通道配置
- `GET /api/rules` - 获取所有路由规则
- `POST /api/rules` - 添加或更新路由规则
- `GET /api/config` - 获取当前配置
- `GET /api/health` - 管理API健康检查
- `POST /api/auth/login` - 登录获取JWT令牌

### API使用示例

#### 身份验证

在您的请求的Authorization头中包含您的API密钥：

```bash
Authorization: Bearer your-api-key-here
```

#### 聊天完成

```bash
curl -X POST http://localhost:8080/v1/chat/completions \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer your-api-key-here" \
  -d '{
    "model": "gpt-3.5-turbo",
    "messages": [
      {
        "role": "user",
        "content": "你好，最近怎么样？"
      }
    ]
  }'
```

#### 模型列表

```bash
curl -X GET http://localhost:8080/v1/models \
  -H "Authorization: Bearer your-api-key-here"
```

#### 管理API登录

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "username": "admin",
    "password": "admin123"
  }'
```

## Docker支持

应用程序包含用于容器化部署的Docker配置：

```bash
docker build -t smartaiproxy .
docker run -p 8080:8080 smartaiproxy
```

## 监控

Prometheus指标在`/metrics`端点上公开。应用程序包含以下内置指标：
- 请求计数和响应时间
- 通道使用率和成功率
- 系统健康和资源使用情况

## 测试

项目包含全面的单元测试，涵盖：
- 规则引擎评估
- 通道服务操作
- 配置管理

运行测试：
```bash
dotnet test
```

## 贡献

1. Fork仓库
2. 创建功能分支
3. 提交您的更改
4. 推送到分支
5. 创建拉取请求

## 许可证

本项目采用MIT许可证。