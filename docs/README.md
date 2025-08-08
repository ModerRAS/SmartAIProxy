---

## 智能 AI API 中转服务项目需求（完整 Prompt）

### 项目背景与目标

请用 Go 语言实现一个高性能、可扩展的 AI API 中转服务，支持主流 AI 模型（如 OpenAI、Anthropic Claude、Google Gemini、Aspects 等）的 API 请求转发。  
服务需智能选择最省钱、最优质的渠道商，自动应对免费额度、付费价格、特殊时段折扣等复杂场景，提升资金利用率和服务稳定性。

---

### 功能需求

1. 多渠道商支持，灵活配置认证、价格、优先级、免费额度、折扣等参数。
2. 复杂规则系统，表达式支持 govaluate/expr，规则可嵌套组合。
3. 智能路由与资金优化，自动统计和选择最优渠道。
4. 高可用与容错，错误原样返回，支持输出完整性检测与自动重试。
5. 配置与管理，支持 YAML/JSON 配置、热更新、管理后台、监控统计。
6. 安全性，API 鉴权、日志安全。
7. 服务基础配置，监听端口、超时、最大连接数、HTTPS、日志、管理后台、监控等。

---

### 技术选型建议

- 后端语言：Go
- 表达式引擎：govaluate 或 expr
- 配置管理：YAML/JSON 文件
- 监控与统计：Prometheus、Grafana
- API 网关：Gin、Echo
- 单元测试：Go 原生 testing 框架

---

### 配置文件示例（YAML）

```yaml
server:
  listen: "0.0.0.0:8080"
  timeout: 30
  max_connections: 1000
  enable_https: false
  cert_file: ""
  key_file: ""

log:
  level: "info"
  file: "./logs/server.log"
  max_size: 20
  max_backups: 5
  max_age: 30

admin:
  enable: true
  listen: "127.0.0.1:9090"
  username: "admin"
  password: "password"

channels:
  - name: "免费渠道A"
    type: "openai"
    endpoint: "https://api.openai.com/v1"
    api_key: "sk-free"
    price_per_token: 0
    daily_limit: 10000
    hour_limit: 1000
    priority: 1

  - name: "夜间折扣渠道B"
    type: "claude"
    endpoint: "https://api.anthropic.com/v1"
    api_key: "claude-night"
    price_per_token: 0.005
    discount_time: ["00:00-06:00"]
    discount_price: 0.002
    priority: 2

  - name: "普通付费渠道C"
    type: "openai"
    endpoint: "https://api.openai.com/v1"
    api_key: "sk-paid"
    price_per_token: 0.01
    priority: 3

rules:
  - name: "免费优先"
    channel: "免费渠道A"
    expression: "day_tokens_used &lt; daily_limit && hour_tokens_used &lt; hour_limit"

  - name: "夜间折扣优先"
    channel: "夜间折扣渠道B"
    expression: "time &gt;= '00:00' && time &lt;= '06:00'"

  - name: "最便宜优先"
    channel: "普通付费渠道C"
    expression: "true"

error_handling:
  check_output_complete: true
  retry_on_incomplete: 2
  return_standard_error: true

monitor:
  enable: true
  prometheus_listen: "0.0.0.0:9100"
```

---

### 启动指示

1. **安装 Go 环境**
   - 推荐使用 Go 1.20 及以上版本。
   - Windows 用户可从 [Go 官网](https://go.dev/dl/)下载安装包。
   - 安装后，配置好环境变量 `GOPATH` 和 `GOROOT`。

2. **依赖安装**
   - 使用 `go mod init` 初始化项目。
   - 使用 `go get` 安装所需依赖（如 gin、govaluate、expr、prometheus 等）。

3. **编写单元测试**
   - 所有核心模块需编写单元测试，覆盖主要逻辑和异常处理。
   - 使用 Go 原生 `testing` 框架，测试文件以 `_test.go` 结尾。
   - 确保测试覆盖率高，所有测试通过后再提交代码。

4. **启动服务**
   - 使用 `go run main.go` 或编译后 `./yourapp` 启动服务。
   - 服务启动后，监听配置文件中的端口，支持 API 请求转发和管理后台。

5. **提交要求**
   - 代码需通过所有单元测试，确保无明显 bug。
   - 配置文件、启动说明、依赖列表需完整。

---

### 自动编程提示词（Prompt）

&gt; 请用 Go 语言实现一个高性能的 AI API 中转服务，支持 OpenAI、Anthropic Claude、Google Gemini 等主流模型的 API 请求转发。  
&gt; 服务需支持多渠道商配置、复杂规则表达（如 govaluate/expr），智能选择最省钱的渠道，优先用免费额度、特殊时段折扣，自动统计和切换。  
&gt; 保证 API 行为与官方一致，错误原样返回，下游不会因模型意外中断而崩溃。  
&gt; 配置文件采用 YAML/JSON，规则用表达式字段。  
&gt; 支持自动重试、标准错误响应、热更新、监控统计、日志、管理后台等功能。  
&gt; 推荐使用 Gin/Echo 框架，govaluate/expr 表达式引擎，Prometheus 监控。  
&gt; 编写完善的单元测试，确保所有测试通过后再提交。  
&gt; 请输出项目结构、核心代码、配置示例、启动说明和关键实现思路。

---

如需更详细的代码结构或某一部分的实现，请随时补充说明！
