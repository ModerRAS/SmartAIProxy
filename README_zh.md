# SmartAIProxy .NET 9 实现版本

[![.NET 9](https://img.shields.io/badge/.NET-9.0-blue)](https://dotnet.microsoft.com/download/dotnet/9.0)
[![Build Status](https://github.com/ModerRAS/SmartAIProxy/workflows/CI/CD%20Pipeline/badge.svg)](https://github.com/ModerRAS/SmartAIProxy/actions)
[![License](https://img.shields.io/github/license/ModerRAS/SmartAIProxy)](LICENSE)

SmartAIProxy 是一个高性能、可扩展的 AI API 网关服务，可将 API 请求转发到主要的 AI 模型（OpenAI、Anthropic Claude、Google Gemini 等）。这是基于 .NET 9 的完整实现版本，项目结构已优化，所有代码现在位于根目录。

## 🌟 核心特性

### 🔧 智能 API 网关
- **多模型支持**: 支持 OpenAI、Anthropic Claude、Google Gemini 等主流 AI 模型
- **智能路由**: 基于规则、配额和定价的智能通道选择
- **请求转发**: 高效的 HTTP 请求转发，保持头部和正文完整
- **流式响应**: 支持流式响应处理，适用于聊天和完成 API

### 🧠 智能路由引擎
- **表达式规则**: 基于 NCalc 表达式的路由规则引擎
- **优先级管理**: 支持基于优先级的通道选择
- **动态路由**: 根据请求内容和上下文动态选择最佳通道
- **故障转移**: 自动故障转移和默认通道回退机制

### ⚙️ 配置管理
- **YAML 配置**: 采用 YAML 格式的配置文件，易于阅读和维护
- **热重载**: 支持配置文件热重载，无需重启服务
- **多环境支持**: 支持开发、测试、生产环境的不同配置
- **默认配置**: 首次运行时自动生成默认配置文件

### 🛡️ 安全特性
- **双因素认证**: JWT 令牌认证（管理 API）+ API 密钥认证（网关）
- **速率限制**: 基于 API 密钥的请求速率限制
- **安全传输**: HTTPS 支持和安全的凭据处理
- **访问控制**: 基于角色的访问控制和端点保护

### 📊 监控和日志
- **Prometheus 集成**: 完整的指标收集和暴露
- **结构化日志**: 基于 Serilog 的高性能结构化日志记录
- **实时监控**: 实时健康检查和性能监控
- **可视化支持**: 支持 Grafana 仪表板和告警

### 🚀 部署和运维
- **Docker 容器化**: 多阶段 Docker 构建，优化镜像大小
- **Docker Compose**: 完整的监控栈编排（Prometheus + Grafana）
- **CI/CD 管道**: GitHub Actions 自动化构建、测试、部署
- **配置管理**: 灵活的配置管理和环境变量支持

## 🏗️ 系统架构

SmartAIProxy 采用清洁架构模式，包含以下层次：

1. **API 层**: ASP.NET Core 控制器和中间件
2. **核心层**: 业务逻辑服务（配置、通道、规则）
3. **模型层**: 数据传输对象和配置模型
4. **中间件层**: 自定义代理中间件用于请求转发

```
SmartAIProxy/
├── SmartAIProxy/                    # 主应用
│   ├── Controllers/                # API 控制器
│   ├── Core/                       # 核心业务逻辑
│   │   ├── Channels/              # 通道管理服务
│   │   ├── Config/                # 配置管理服务
│   │   └── Rules/                 # 规则引擎
│   ├── Middleware/                 # 自定义中间件
│   ├── Models/                     # 数据模型和 DTO
│   ├── config/                     # 配置文件
│   ├── monitoring/                 # 监控配置
│   ├── Dockerfile                 # Docker 配置
│   ├── docker-compose.yml         # 编排配置
│   └── Program.cs                 # 应用入口点
├── SmartAIProxy.Tests/             # 测试套件
│   ├── Unit/                      # 单元测试
│   ├── Integration/               # 集成测试
│   └── Controllers/              # 控制器测试
├── docs/                          # 文档
│   ├── architecture.md            # 架构文档
│   ├── tech-stack.md              # 技术栈
│   └── README.md                  # 文档索引
├── .github/workflows/              # CI/CD 工作流
│   └── ci.yml                     # GitHub Actions 配置
├── README.md                      # 项目文档
└── README_zh.md                   # 中文文档
```

## 🚀 快速开始

### 系统要求

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Docker](https://www.docker.com/) (可选，用于容器化部署)

### 本地开发

```bash
# 克隆仓库
git clone https://github.com/ModerRAS/SmartAIProxy.git
cd SmartAIProxy

# 构建项目
dotnet build

# 运行应用
dotnet run --project SmartAIProxy
```

应用默认在 `http://localhost:8080` 启动。

### Docker 部署

```bash
# 构建 Docker 镜像
docker build -t smartaiproxy .

# 运行容器
docker run -p 8080:8080 smartaiproxy
```

### Docker Compose 部署

```bash
# 使用 docker-compose 启动完整监控栈
docker-compose up -d
```

## ⚙️ 配置说明

应用使用 `config/smartaiproxy.yaml` 文件进行配置。首次运行时会自动创建默认配置文件。

### 主要配置项

```yaml
server:
  listen: "0.0.0.0:8080"           # 监听地址
  timeout: 300                     # 请求超时时间（秒）
  max_connections: 1000            # 最大连接数

channels:                          # AI 服务通道配置
  - name: "openai"                 # 通道名称
    url: "https://api.openai.com/v1" # API 地址
    api_key: "your-api-key"        # API 密钥
    models:                        # 支持的模型
      - "gpt-3.5-turbo"
      - "gpt-4"
    priority: 1                    # 优先级
    active: true                   # 是否激活
    weight: 100                    # 权重（配额）

rules:                             # 路由规则
  - name: "gpt-4-rule"             # 规则名称
    expression: "model == 'gpt-4'" # 表达式条件
    channel: "openai"              # 目标通道
    priority: 1                    # 优先级

monitor:                           # 监控配置
  enabled: true                    # 是否启用
  endpoint: "/metrics"             # 指标端点

security:                          # 安全配置
  jwt_secret: "your-jwt-secret"    # JWT 密钥
  api_keys:                        # API 密钥列表
    - "your-api-key"
  rate_limit:                      # 速率限制
    requests_per_minute: 60
```

## 📡 API 端点

### 网关 API

- `POST /v1/chat/completions` - 转发聊天完成请求
- `POST /v1/completions` - 转发完成请求
- `POST /v1/embeddings` - 转发嵌入请求
- `GET /healthz` - 健康检查端点

### 管理 API

- `GET /api/channels` - 获取所有配置的通道
- `POST /api/channels` - 添加或更新通道配置
- `GET /api/rules` - 获取所有路由规则
- `POST /api/rules` - 添加或更新路由规则
- `GET /api/config` - 获取当前配置
- `GET /api/health` - 管理 API 健康检查
- `POST /api/auth/login` - 登录获取 JWT 令牌

## 🧪 测试

项目包含全面的单元测试：

```bash
# 运行测试
dotnet test SmartAIProxy.Tests

# 收集代码覆盖率
dotnet test SmartAIProxy.Tests --collect:"XPlat Code Coverage"
```

测试覆盖：
- 规则引擎评估
- 通道服务操作
- 配置管理

## 📊 监控和指标

Prometheus 指标在 `/metrics` 端点暴露，包括：

- 请求计数和响应时间
- 通道使用情况和成功率
- 系统健康和资源使用情况

## 🤝 贡献指南

欢迎贡献代码！请遵循以下步骤：

1. Fork 仓库
2. 创建功能分支
3. 提交更改
4. 推送到分支
5. 创建 Pull Request

## 📄 许可证

本项目采用 MIT 许可证。详情请见 [LICENSE](LICENSE) 文件。

## 📞 支持和联系

如有问题或建议，请创建 GitHub Issue 或联系项目维护者。