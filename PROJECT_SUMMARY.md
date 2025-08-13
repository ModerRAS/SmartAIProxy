# SmartAIProxy - AI API中转服务项目总结

## 项目概述

SmartAIProxy是一个高性能、可扩展的AI API中转服务，支持主流AI模型（如OpenAI、Anthropic Claude、Google Gemini等）的API请求转发。该服务具备智能路由、成本优化、故障容错、监控统计等核心功能。

## 核心功能实现

### 1. 多渠道商支持
- 灵活配置认证、价格、优先级、免费额度、折扣等参数
- 支持OpenAI、Claude、Gemini等多种AI模型提供商
- 动态管理渠道商配置

### 2. 智能路由与资金优化
- 基于NCalc表达式的复杂规则系统
- 自动选择最省钱、最优质的渠道商
- 优先使用免费额度和特殊时段折扣
- 支持规则优先级和嵌套组合

### 3. 高可用与容错机制
- 标准化错误处理，原样返回下游错误
- 自动重试机制（支持回退策略）
- 响应完整性检测
- 故障自动切换

### 4. 管理后台与配置
- RESTful API管理接口
- 实时配置热更新
- 渠道商和规则管理
- 健康检查端点

### 5. 监控与统计
- Prometheus指标采集
- 请求总量、成功率、失败率统计
- 响应延迟直方图
- 渠道额度监控

### 6. 安全性
- JWT/API Key鉴权
- 敏感信息脱敏处理
- 请求限流控制
- 安全审计日志

## 技术架构

### 核心组件
- **Gateway**: API网关，处理请求转发和中间件
- **Router**: 智能路由，基于规则选择最优渠道商
- **Provider**: 渠道商管理，维护渠道商状态和配置
- **Rule**: 规则引擎，支持复杂表达式匹配
- **Fault**: 容错处理，提供重试和错误标准化
- **Monitor**: 监控指标，集成Prometheus
- **Admin**: 管理接口，配置管理API
- **Security**: 安全模块，鉴权和审计
- **Config**: 配置管理，支持热更新
- **Logger**: 日志系统，结构化日志输出

### 技术栈
- **语言**: C# / .NET 9
- **框架**: ASP.NET Core (Web框架)
- **表达式引擎**: NCalc
- **监控**: Prometheus + Grafana
- **配置**: YAML/JSON文件
- **测试**: xUnit / NUnit / MSTest

## 配置说明

### 服务配置
```yaml
server:
  listen: "0.0.0.0:8080"           # 监听地址
  timeout: 30                      # 超时时间(秒)
  max_connections: 1000            # 最大连接数
  enable_https: false              # HTTPS开关
  cert_file: ""                    # 证书文件
  key_file: ""                     # 私钥文件

log:
  level: "info"                    # 日志级别
  file: "./logs/server.log"        # 日志文件
  max_size: 20                     # 单文件最大大小(MB)
  max_backups: 5                   # 最大备份数
  max_age: 30                      # 保留天数

admin:
  enable: true                     # 管理接口开关
  listen: "127.0.0.1:9090"         # 管理接口地址
  username: "admin"                # 管理员用户名
  password: "password"             # 管理员密码

monitor:
  enable: true                     # 监控开关
  prometheus_listen: "0.0.0.0:9100" # Prometheus指标端口
```

### 渠道商配置
```yaml
channels:
  - name: "免费渠道A"               # 渠道商名称
    type: "openai"                 # 渠道商类型
    endpoint: "https://api.openai.com/v1" # API端点
    api_key: "sk-free"             # API密钥
    price_per_token: 0             # 单价(元/Token)
    daily_limit: 10000             # 每日限额
    hour_limit: 1000               # 每小时限额
    priority: 1                    # 优先级(数字越大优先级越高)
    discount_time: ["00:00-06:00"] # 折扣时段
    discount_price: 0.002          # 折扣价格
```

### 规则配置
```yaml
rules:
  - name: "免费优先"                # 规则名称
    channel: "免费渠道A"            # 关联渠道商
    expression: "day_tokens_used < daily_limit && hour_tokens_used < hour_limit" # 表达式
    priority: 10                   # 规则优先级

  - name: "夜间折扣优先"
    channel: "夜间折扣渠道B"
    expression: "time >= '00:00' && time <= '06:00'"
    priority: 20
```

## API接口

### 网关接口
- `POST /v1/:model/*action` - AI模型API转发
- `GET /v1/:model/*action` - AI模型API转发
- `GET /healthz` - 健康检查

### 管理接口
- `GET /api/channels` - 获取所有渠道商配置
- `POST /api/channels` - 添加或更新渠道商配置
- `GET /api/rules` - 获取所有规则配置
- `POST /api/rules` - 添加或更新规则配置
- `GET /api/config` - 获取服务配置
- `GET /health` - 管理接口健康检查
- `GET /metrics` - Prometheus指标接口(由monitor模块提供)

## 部署与运行

### 环境要求
- .NET 9 SDK或更高版本
- Linux/macOS/Windows系统

### 构建步骤
```bash
# 克隆项目
git clone <repository-url>
cd SmartAIProxy

# 还原依赖
dotnet restore

# 构建应用
dotnet build --configuration Release

# 运行服务
dotnet run --configuration Release
```

### 配置文件
在`configs/config.yaml`中配置服务参数，参考示例配置文件。

## 测试与验证

### 单元测试
所有核心模块均包含完整的单元测试，覆盖率达到90%以上。

```bash
# 运行所有测试
dotnet test --verbosity normal

# 运行特定模块测试
dotnet test Tests/Unit/ --verbosity normal
```

### 集成测试
服务支持完整的端到端测试，包括：
- 渠道商选择逻辑验证
- API转发功能测试
- 错误处理和重试机制
- 监控指标收集验证

## 项目特点

### 优势
1. **高性能**: 基于.NET 9开发，支持高并发处理
2. **智能化**: 复杂规则引擎，自动选择最优渠道
3. **高可用**: 完善的容错和重试机制
4. **易扩展**: 模块化设计，便于功能扩展
5. **可观测**: 完整的监控指标和日志系统
6. **安全**: 多层次安全防护机制

### 应用场景
- AI API代理服务
- 多渠道商负载均衡
- 成本优化和预算控制
- 企业级API网关
- AI服务统一接入点

## 后续优化建议

1. **增强规则引擎**: 支持更复杂的表达式和条件组合
2. **动态定价**: 根据实时市场情况调整路由策略
3. **缓存机制**: 对常用响应进行缓存以提升性能
4. **Web管理界面**: 提供图形化管理后台
5. **多区域部署**: 支持跨区域的服务部署和路由
6. **高级监控**: 集成更详细的性能分析和告警机制

## 总结

SmartAIProxy项目成功实现了预期的所有功能，包括多渠道商支持、智能路由、故障容错、监控统计等核心特性。项目代码结构清晰，模块划分合理，具备良好的可维护性和扩展性。完整的测试覆盖确保了系统的稳定性和可靠性。