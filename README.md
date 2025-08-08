# SmartAIProxy

高性能、可扩展的AI API中转服务，支持主流AI模型（如OpenAI、Anthropic Claude、Google Gemini等）的API请求转发。

## 功能特性

- **多渠道商支持**: 灵活配置认证、价格、优先级、免费额度、折扣等参数
- **智能路由**: 基于复杂规则表达式自动选择最省钱、最优质的渠道商
- **高可用**: 标准化错误处理、自动重试、故障容错与自动切换
- **监控统计**: Prometheus指标采集，实时监控服务状态
- **安全管理**: JWT/API Key鉴权，敏感信息脱敏，请求限流
- **配置管理**: YAML/JSON配置，支持热更新和管理后台

## 技术栈

- **语言**: Go 1.20+
- **框架**: Gin
- **表达式引擎**: govaluate
- **监控**: Prometheus
- **配置**: YAML/JSON

## 快速开始

### 环境要求
- Go 1.20或更高版本

### 安装部署

```bash
# 克隆项目
git clone <repository-url>
cd SmartAIProxy

# 安装依赖
go mod tidy

# 构建应用
go build -o smartaiproxy main.go

# 运行服务
./smartaiproxy
```

### 配置文件

编辑 `configs/config.yaml` 文件配置服务参数：

```yaml
server:
  listen: "0.0.0.0:8080"
  timeout: 30
  max_connections: 1000

channels:
  - name: "免费渠道A"
    type: "openai"
    endpoint: "https://api.openai.com/v1"
    api_key: "your-api-key"
    price_per_token: 0
    daily_limit: 10000

rules:
  - name: "免费优先"
    channel: "免费渠道A"
    expression: "day_tokens_used < daily_limit"
```

## API接口

### AI模型转发
```
POST /v1/chat/completions
GET /v1/models
```

### 管理接口
```
GET /api/channels    # 获取渠道商列表
POST /api/channels   # 更新渠道商配置
GET /api/rules       # 获取规则列表
POST /api/rules      # 更新规则配置
GET /api/config      # 获取服务配置
```

### 监控接口
```
GET /metrics         # Prometheus指标
GET /healthz         # 健康检查
```

## 项目结构

```
SmartAIProxy/
├── configs/           # 配置文件
├── internal/          # 核心模块
│   ├── admin/        # 管理后台
│   ├── config/       # 配置管理
│   ├── fault/        # 容错处理
│   ├── gateway/      # API网关
│   ├── logger/       # 日志系统
│   ├── monitor/      # 监控指标
│   ├── provider/     # 渠道商管理
│   ├── router/       # 智能路由
│   ├── rule/         # 规则引擎
│   └── security/     # 安全模块
├── logs/             # 日志文件
└── main.go           # 程序入口
```

## 测试

运行单元测试：

```bash
go test ./... -v
```

## 许可证

MIT License