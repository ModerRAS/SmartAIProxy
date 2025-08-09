# SmartAIProxy 测试架构设计

## 测试分层架构

```
SmartAIProxy/
├── tests/
│   ├── unit/                     # 单元测试
│   │   ├── config/
│   │   │   ├── config_test.go
│   │   │   └── config_suite_test.go
│   │   ├── gateway/
│   │   │   ├── gateway_test.go
│   │   │   ├── proxy_handler_test.go
│   │   │   └── forward_request_test.go
│   │   ├── router/
│   │   │   ├── router_test.go
│   │   │   ├── selection_test.go
│   │   │   └── rules_test.go
│   │   ├── admin/
│   │   │   ├── admin_test.go
│   │   │   └── handlers_test.go
│   │   ├── monitor/
│   │   │   ├── monitor_test.go
│   │   │   └── metrics_test.go
│   │   ├── fault/
│   │   │   ├── fault_test.go
│   │   │   └── tolerance_test.go
│   │   ├── security/
│   │   │   ├── auth_test.go
│   │   │   ├── audit_test.go
│   │   │   └── limiter_test.go
│   │   ├── logger/
│   │   │   └── logger_test.go
│   │   └── provider/
│   │       └── provider_test.go
│   ├── integration/              # 集成测试
│   │   ├── server/
│   │   │   ├── server_startup_test.go
│   │   │   ├── config_reload_test.go
│   │   │   └── graceful_shutdown_test.go
│   │   ├── api/
│   │   │   ├── complete_flow_test.go
│   │   │   ├── channel_switching_test.go
│   │   │   └── error_handling_test.go
│   │   ├── fault_tolerance/
│   │   │   ├── circuit_breaker_test.go
│   │   │   ├── retry_mechanism_test.go
│   │   │   └── failover_test.go
│   │   └── performance/
│   │       ├── concurrency_test.go
│   │       ├── memory_test.go
│   │       └── load_test.go
│   ├── e2e/                       # 端到端测试
│   │   ├── basic_forwarding_test.go
│   │   ├── rule_selection_test.go
│   │   ├── hot_update_test.go
│   │   └── stress_test.go
│   ├── fixtures/                  # 测试数据
│   │   ├── config/
│   │   │   ├── valid_config.yaml
│   │   │   ├── invalid_config.yaml
│   │   │   └── complex_config.yaml
│   │   ├── mock_servers/
│   │   │   ├── openai_mock.go
│   │   │   ├── claude_mock.go
│   │   │   └── generic_ai_mock.go
│   │   └── test_data/
│   │       ├── providers.yaml
│   │       ├── rules.yaml
│   │       └── api_responses.yaml
│   └── tools/                     # 测试工具
│       ├── mock_factory.go
│       ├── test_server.go
│       ├── http_client.go
│       └── benchmark_runner.go
└── .github/
    └── workflows/
        ├── ci.yml
        ├── coverage.yml
        └── performance.yml
```

## 测试工具链设计

### 1. 测试基础设施

#### Mock服务器工厂
```go
// tests/tools/mock_factory.go
type MockServerFactory struct {
    servers map[string]*httptest.Server
}

func NewMockServerFactory() *MockServerFactory
func (f *MockServerFactory) CreateOpenAIMock() *httptest.Server
func (f *MockServerFactory) CreateClaudeMock() *httptest.Server
func (f *MockServerFactory) CreateErrorMock(errorRate float64) *httptest.Server
func (f *MockServerFactory) Cleanup()
```

#### 测试HTTP客户端
```go
// tests/tools/http_client.go
type TestHTTPClient struct {
    client *http.Client
    baseURL string
}

func NewTestHTTPClient(baseURL string) *TestHTTPClient
func (c *TestHTTPClient) Get(path string, headers map[string]string) (*http.Response, error)
func (c *TestHTTPClient) Post(path string, body interface{}, headers map[string]string) (*http.Response, error)
func (c *TestHTTPClient) WithTimeout(timeout time.Duration) *TestHTTPClient
```

#### 基准测试运行器
```go
// tests/tools/benchmark_runner.go
type BenchmarkRunner struct {
    concurrency int
    duration    time.Duration
    requests    int
}

func NewBenchmarkRunner() *BenchmarkRunner
func (r *BenchmarkRunner) RunBenchmark(handler http.Handler) *BenchmarkResult
func (r *BenchmarkRunner) RunConcurrentTest(handler http.Handler) *BenchmarkResult
```

### 2. 测试框架扩展

#### 增强的测试套件
```go
// tests/tools/test_suite.go
type APITestSuite struct {
    suite.Suite
    config     *config.Config
    mockServer *httptest.Server
    httpClient *TestHTTPClient
}

func (s *APITestSuite) SetupSuite()
func (s *APITestSuite) SetupTest()
func (s *APITestSuite) TearDownTest()
func (s *APITestSuite) TearDownSuite()
```

#### 响应断言助手
```go
// tests/tools/assertions.go
type HTTPAssertions struct {
    suite *suite.Suite
}

func NewHTTPAssertions(suite *suite.Suite) *HTTPAssertions
func (a *HTTPAssertions) ShouldHaveStatus(resp *http.Response, expected int)
func (a *HTTPAssertions) ShouldContainHeader(resp *http.Response, key, expected string)
func (a *HTTPAssertions) ShouldHaveJSONBody(resp *http.Response, expected interface{})
func (a *HTTPAssertions) ShouldHaveErrorRate(results []*http.Response, maxErrorRate float64)
```

## 测试数据管理

### 1. 配置测试数据

#### 有效配置
```yaml
# tests/fixtures/config/valid_config.yaml
server:
  listen: "127.0.0.1:0"  # 使用0让系统分配端口
  timeout: 30
  max_connections: 100

channels:
  - name: "openai_test"
    type: "openai"
    endpoint: "http://localhost:3000"
    api_key: "test-key"
    price_per_token: 0.01
    priority: 1
    daily_limit: 10000

rules:
  - name: "always_use_openai"
    channel: "openai_test"
    expression: "true"

monitor:
  enable: true
  prometheus_listen: "127.0.0.1:0"
```

#### 复杂配置
```yaml
# tests/fixtures/config/complex_config.yaml
server:
  listen: "127.0.0.1:0"
  timeout: 60
  max_connections: 1000

channels:
  - name: "free_channel"
    type: "openai"
    endpoint: "http://localhost:3001"
    api_key: "free-key"
    price_per_token: 0
    priority: 10
    daily_limit: 1000
  - name: "paid_channel"
    type: "openai"
    endpoint: "http://localhost:3002"
    api_key: "paid-key"
    price_per_token: 0.02
    priority: 5
    daily_limit: 50000
  - name: "night_discount"
    type: "claude"
    endpoint: "http://localhost:3003"
    api_key: "night-key"
    price_per_token: 0.015
    discount_time: ["00:00-06:00"]
    discount_price: 0.008
    priority: 7

rules:
  - name: "free_priority"
    channel: "free_channel"
    expression: "day_tokens_used < daily_limit"
    priority: 10
  - name: "night_discount"
    channel: "night_discount"
    expression: "time >= '00:00' && time <= '06:00'"
    priority: 15
  - name: "paid_fallback"
    channel: "paid_channel"
    expression: "true"
    priority: 5
```

### 2. Mock服务配置

#### OpenAI Mock服务
```go
// tests/fixtures/mock_servers/openai_mock.go
type OpenAIMockServer struct {
    *httptest.Server
    calls        []APICall
    shouldError  bool
    delay        time.Duration
}

func NewOpenAIMockServer() *OpenAIMockServer
func (s *OpenAIMockServer) ShouldError(enabled bool) *OpenAIMockServer
func (s *OpenAIMockServer) WithDelay(delay time.Duration) *OpenAIMockServer
func (s *OpenAIMockServer) GetCallCount() int
func (s *OpenAIMockServer) Reset()
```

#### Claude Mock服务
```go
// tests/fixtures/mock_servers/claude_mock.go
type ClaudeMockServer struct {
    *httptest.Server
    responses map[string]string
    errorRate float64
}

func NewClaudeMockServer() *ClaudeMockServer
func (s *ClaudeMockServer) SetResponse(endpoint, response string)
func (s *ClaudeMockServer) SetErrorRate(rate float64)
func (s *ClaudeMockServer) GetRequestCount() int
```

## 测试执行策略

### 1. 单元测试执行
```bash
# 运行所有单元测试
go test ./tests/unit/... -v -cover

# 运行特定模块单元测试
go test ./tests/unit/gateway -v -coverprofile=gateway_coverage.out

# 生成覆盖率报告
go tool cover -html=gateway_coverage.out -o coverage.html
```

### 2. 集成测试执行
```bash
# 运行集成测试
go test ./tests/integration/... -v -tags=integration

# 运行服务启动集成测试
go test ./tests/integration/server -v -run TestServerStartup

# 运行API完整流程测试
go test ./tests/integration/api -v -run TestAPICompleteFlow
```

### 3. 端到端测试执行
```bash
# 运行E2E测试
go test ./tests/e2e/... -v -tags=e2e

# 运行基准测试
go test ./tests/integration/performance -v -bench=. -benchmem
```

### 4. 性能测试执行
```bash
# 运行并发测试
go test -race ./tests/integration/performance

# 运行内存分析测试
go test -cpuprofile=cpu.out -memprofile=mem.out ./tests/integration/performance
```

## CI/CD 集成设计

### 1. GitHub Actions 工作流

#### CI/CD 管道
```yaml
# .github/workflows/ci.yml
name: CI/CD Pipeline

on:
  push:
    branches: [ main, master, develop ]
  pull_request:
    branches: [ main, master ]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v3
      
      - name: Setup Go
        uses: actions/setup-go@v3
        with:
          go-version: 1.21
          
      - name: Run Unit Tests
        run: go test ./... -v -covermode=count -coverprofile=coverage.out
        
      - name: Run Integration Tests
        run: go test ./tests/integration/... -v -tags=integration
        
      - name: Upload Coverage
        uses: codecov/codecov-action@v3
        with:
          file: ./coverage.out
```

#### 性能监控工作流
```yaml
# .github/workflows/performance.yml
name: Performance Monitoring

on:
  schedule:
    - cron: '0 */6 * * *'  # 每6小时运行一次

jobs:
  benchmark:
    runs-on: ubuntu-latest
    steps:
      - name: Checkout
        uses: actions/checkout@v3
        
      - name: Setup Go
        uses: actions/setup-go@v3
        
      - name: Run Benchmark
        run: go test ./tests/integration/performance -bench=. -benchmem -benchtime=10s
        
      - name: Generate Report
        run: go tool pprof -text mem.out > memory_report.txt
```

### 2. 代码质量门禁

#### 覆盖率要求
```yaml
# .github/workflows/coverage.yml
name: Coverage Check

on:
  pull_request:
    branches: [ main, master ]

jobs:
  coverage:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-go@v3
        
      - name: Test with Coverage
        run: go test ./... -covermode=count -coverprofile=coverage.out
        
      - name: Check Coverage
        run: |
          go tool cover -func=coverage.out > coverage.txt
          TOTAL=$(grep "total:" coverage.txt | awk '{print $3}' | tr -d '%')
          if [ "$TOTAL" -lt 80 ]; then
            echo "Coverage below 80%: $TOTAL%"
            exit 1
          fi
```

## 测试质量保证

### 1. 测试代码审查清单
- [ ] 每个测试用例都有明确的目标
- [ ] 测试名称描述性且符合约定
- [ ] 测试隔离，不相互依赖
- [ ] Mock和Stub使用合理
- [ ] 断言明确且有意义
- [ ] 边界条件得到测试
- [ ] 错误场景得到测试
- [ ] 性能测试包含基准

### 2. 测试维护策略
- **定期更新**: 每月审查和更新测试用例
- **性能监控**: 跟踪测试执行时间趋势
- **覆盖率监控**: 确保覆盖率不下降
- **Mock服务维护**: 定期更新API响应格式
- **测试数据管理**: 维护测试数据的最新性

### 3. 测试报告生成
- **HTML覆盖率报告**: 可视化的代码覆盖率
- **性能报告**: 响应时间和吞吐量指标
- **错误报告**: 测试失败详情和修复建议
- **趋势报告**: 测试执行时间变化趋势