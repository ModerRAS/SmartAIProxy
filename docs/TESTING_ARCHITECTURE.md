# SmartAIProxy 测试架构设计

## 测试分层架构

```
SmartAIProxy/
├── Tests/
│   ├── Unit/                     # 单元测试
│   │   ├── Config/
│   │   │   ├── ConfigModelTests.cs
│   │   │   └── ConfigurationServiceTests.cs
│   │   ├── Controllers/
│   │   │   ├── AuthControllerTests.cs
│   │   │   ├── AdminControllerTests.cs
│   │   │   └── HealthControllerTests.cs
│   │   ├── Core/
│   │   │   ├── Channels/
│   │   │   ├── Config/
│   │   │   ├── Providers/
│   │   │   └── Rules/
│   │   │       └── RuleEngineTests.cs
│   │   ├── Middleware/
│   │   │   └── ProxyMiddlewareTests.cs
│   │   └── Models/
│   │       ├── Channels/
│   │       ├── Config/
│   │       ├── DTO/
│   │       └── Rules/
│   ├── Integration/              # 集成测试
│   │   ├── Server/
│   │   │   ├── ServerStartupTests.cs
│   │   │   ├── ConfigReloadTests.cs
│   │   │   └── GracefulShutdownTests.cs
│   │   ├── Api/
│   │   │   ├── CompleteFlowTests.cs
│   │   │   ├── ChannelSwitchingTests.cs
│   │   │   └── ErrorHandlingTests.cs
│   │   ├── FaultTolerance/
│   │   │   ├── CircuitBreakerTests.cs
│   │   │   ├── RetryMechanismTests.cs
│   │   │   └── FailoverTests.cs
│   │   └── Performance/
│   │       ├── ConcurrencyTests.cs
│   │       ├── MemoryTests.cs
│   │       └── LoadTests.cs
│   ├── E2E/                       # 端到端测试
│   │   ├── BasicForwardingTests.cs
│   │   ├── RuleSelectionTests.cs
│   │   ├── HotUpdateTests.cs
│   │   └── StressTests.cs
│   ├── Fixtures/                  # 测试数据
│   │   ├── Config/
│   │   │   ├── valid_config.yaml
│   │   │   ├── invalid_config.yaml
│   │   │   └── complex_config.yaml
│   │   ├── MockServers/
│   │   │   ├── OpenAIMockServer.cs
│   │   │   ├── ClaudeMockServer.cs
│   │   │   └── GenericAIMockServer.cs
│   │   └── TestData/
│   │       ├── providers.yaml
│   │       ├── rules.yaml
│   │       └── api_responses.yaml
│   └── Tools/                     # 测试工具
│       ├── MockFactory.cs
│       ├── TestServer.cs
│       ├── HttpClientWrapper.cs
│       └── BenchmarkRunner.cs
└── .github/
    └── workflows/
        ├── ci.yml
        ├── coverage.yml
        └── performance.yml
```

## 测试工具链设计

### 1. 测试基础设施

#### Mock服务器工厂
```csharp
// Tests/Tools/MockFactory.cs
public class MockServerFactory : IDisposable
{
    private readonly Dictionary<string, TestServer> _servers;
    
    public MockServerFactory()
    {
        _servers = new Dictionary<string, TestServer>();
    }
    
    public TestServer CreateOpenAIMock();
    public TestServer CreateClaudeMock();
    public TestServer CreateErrorMock(double errorRate);
    public void Cleanup();
    
    public void Dispose()
    {
        Cleanup();
    }
}
```

#### 测试HTTP客户端
```csharp
// Tests/Tools/HttpClientWrapper.cs
public class HttpClientWrapper : IDisposable
{
    private readonly HttpClient _client;
    private readonly string _baseURL;
    
    public HttpClientWrapper(string baseURL)
    {
        _baseURL = baseURL;
        _client = new HttpClient();
    }
    
    public async Task<HttpResponseMessage> GetAsync(string path, Dictionary<string, string> headers = null);
    public async Task<HttpResponseMessage> PostAsync(string path, object body, Dictionary<string, string> headers = null);
    public HttpClientWrapper WithTimeout(TimeSpan timeout);
    
    public void Dispose()
    {
        _client?.Dispose();
    }
}
```

#### 基准测试运行器
```csharp
// Tests/Tools/BenchmarkRunner.cs
public class BenchmarkRunner
{
    public int Concurrency { get; set; }
    public TimeSpan Duration { get; set; }
    public int Requests { get; set; }
    
    public BenchmarkRunner()
    {
        Concurrency = 1;
        Duration = TimeSpan.FromSeconds(30);
        Requests = 1000;
    }
    
    public BenchmarkResult RunBenchmark(Func<HttpContext, Task> handler);
    public BenchmarkResult RunConcurrentTest(Func<HttpContext, Task> handler);
}
```

### 2. 测试框架扩展

#### 增强的测试套件
```csharp
// Tests/Tools/APITestSuite.cs
public class APITestSuite : IDisposable
{
    protected IConfiguration Config { get; private set; }
    protected TestServer MockServer { get; private set; }
    protected HttpClientWrapper HttpClient { get; private set; }
    protected IServiceProvider Services { get; private set; }
    
    [TestInitialize]
    public virtual void Setup()
    {
        // 初始化测试环境
        var builder = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Testing.json")
            .AddEnvironmentVariables();
            
        Config = builder.Build();
        
        // 设置测试服务
        var services = new ServiceCollection();
        ConfigureServices(services);
        
        Services = services.BuildServiceProvider();
        
        // 初始化HTTP客户端
        HttpClient = new HttpClientWrapper("http://localhost");
    }
    
    [TestCleanup]
    public virtual void Cleanup()
    {
        HttpClient?.Dispose();
        MockServer?.Dispose();
        Services?.Dispose();
    }
    
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // 配置测试服务
    }
    
    public void Dispose()
    {
        Cleanup();
    }
}
```

#### 响应断言助手
```csharp
// Tests/Tools/HTTPAssertions.cs
public static class HTTPAssertions
{
    public static void ShouldHaveStatus(this HttpResponseMessage response, int expectedStatusCode)
    {
        Assert.AreEqual(expectedStatusCode, (int)response.StatusCode,
            $"Expected status code {expectedStatusCode}, but got {(int)response.StatusCode}");
    }
    
    public static void ShouldContainHeader(this HttpResponseMessage response, string key, string expectedValue)
    {
        Assert.IsTrue(response.Headers.Contains(key), $"Header '{key}' not found");
        var values = response.Headers.GetValues(key);
        Assert.IsTrue(values.Contains(expectedValue),
            $"Header '{key}' does not contain value '{expectedValue}'");
    }
    
    public static async Task ShouldHaveJSONBody<T>(this HttpResponseMessage response, T expected)
    {
        var content = await response.Content.ReadAsStringAsync();
        var actual = JsonConvert.DeserializeObject<T>(content);
        Assert.AreEqual(expected, actual);
    }
    
    public static void ShouldHaveErrorRate(this List<HttpResponseMessage> responses, double maxErrorRate)
    {
        var errorCount = responses.Count(r => !r.IsSuccessStatusCode);
        var actualErrorRate = (double)errorCount / responses.Count;
        Assert.IsTrue(actualErrorRate <= maxErrorRate,
            $"Error rate {actualErrorRate:P} exceeds maximum allowed {maxErrorRate:P}");
    }
}
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
```csharp
// Tests/Fixtures/MockServers/OpenAIMockServer.cs
public class OpenAIMockServer : IDisposable
{
    private readonly TestServer _server;
    private readonly List<APICall> _calls;
    private bool _shouldError;
    private TimeSpan _delay;
    
    public OpenAIMockServer()
    {
        _calls = new List<APICall>();
        _shouldError = false;
        _delay = TimeSpan.Zero;
        
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(this);
        
        var app = builder.Build();
        ConfigureMockEndpoints(app);
        
        _server = new TestServer(app);
    }
    
    public OpenAIMockServer ShouldError(bool enabled)
    {
        _shouldError = enabled;
        return this;
    }
    
    public OpenAIMockServer WithDelay(TimeSpan delay)
    {
        _delay = delay;
        return this;
    }
    
    public int GetCallCount() => _calls.Count;
    
    public void Reset() => _calls.Clear();
    
    private void ConfigureMockEndpoints(WebApplication app)
    {
        // 配置Mock端点
    }
    
    public void Dispose()
    {
        _server?.Dispose();
    }
}
```

#### Claude Mock服务
```csharp
// Tests/Fixtures/MockServers/ClaudeMockServer.cs
public class ClaudeMockServer : IDisposable
{
    private readonly TestServer _server;
    private readonly Dictionary<string, string> _responses;
    private double _errorRate;
    
    public ClaudeMockServer()
    {
        _responses = new Dictionary<string, string>();
        _errorRate = 0.0;
        
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton(this);
        
        var app = builder.Build();
        ConfigureMockEndpoints(app);
        
        _server = new TestServer(app);
    }
    
    public void SetResponse(string endpoint, string response)
    {
        _responses[endpoint] = response;
    }
    
    public void SetErrorRate(double rate)
    {
        _errorRate = rate;
    }
    
    public int GetRequestCount()
    {
        // 返回请求计数
        return 0;
    }
    
    private void ConfigureMockEndpoints(WebApplication app)
    {
        // 配置Mock端点
    }
    
    public void Dispose()
    {
        _server?.Dispose();
    }
}
```

## 测试执行策略

### 1. 单元测试执行
```bash
# 运行所有单元测试
dotnet test --filter "TestCategory!=Integration&TestCategory!=E2E" --verbosity normal --collect:"XPlat Code Coverage"

# 运行特定模块单元测试
dotnet test Tests/Unit/ --verbosity normal --collect:"XPlat Code Coverage" --results-directory TestResults

# 生成覆盖率报告
dotnet reportgenerator -reports:TestResults/coverage.xml -targetdir:CoverageReport -reporttypes:Html
```

### 2. 集成测试执行
```bash
# 运行集成测试
dotnet test --filter "TestCategory=Integration" --verbosity normal

# 运行服务启动集成测试
dotnet test --filter "TestCategory=Integration&DisplayName~ServerStartup" --verbosity normal

# 运行API完整流程测试
dotnet test --filter "TestCategory=Integration&DisplayName~CompleteFlow" --verbosity normal
```

### 3. 端到端测试执行
```bash
# 运行E2E测试
dotnet test --filter "TestCategory=E2E" --verbosity normal

# 运行基准测试
dotnet run --project Tests/Benchmark --configuration Release
```

### 4. 性能测试执行
```bash
# 运行并发测试
dotnet test --filter "TestCategory=Performance" --verbosity normal

# 运行内存分析测试
dotnet test --filter "TestCategory=Performance" --verbosity normal --collect:"XPlat Code Coverage" --settings:coverlet.runsettings
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
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 9.0.x
          
      - name: Restore dependencies
        run: dotnet restore
        
      - name: Build
        run: dotnet build --no-restore
        
      - name: Run Unit Tests
        run: dotnet test --no-build --verbosity normal --collect:"XPlat Code Coverage" --results-directory TestResults
        
      - name: Run Integration Tests
        run: dotnet test --no-build --filter "TestCategory=Integration" --verbosity normal
        
      - name: Generate Coverage Report
        run: dotnet reportgenerator -reports:TestResults/coverage.xml -targetdir:CoverageReport -reporttypes:Html
        
      - name: Upload Coverage
        uses: codecov/codecov-action@v3
        with:
          file: ./TestResults/coverage.xml
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
        
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 9.0.x
        
      - name: Restore dependencies
        run: dotnet restore
        
      - name: Build
        run: dotnet build --configuration Release --no-restore
        
      - name: Run Benchmark
        run: dotnet run --project Tests/Benchmark --configuration Release
        
      - name: Generate Report
        run: dotnet run --project Tools/PerformanceReport --configuration Release
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
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: 9.0.x
        
      - name: Restore dependencies
        run: dotnet restore
        
      - name: Test with Coverage
        run: dotnet test --no-build --collect:"XPlat Code Coverage" --results-directory TestResults
        
      - name: Check Coverage
        run: |
          dotnet reportgenerator -reports:TestResults/coverage.xml -targetdir:CoverageReport -reporttypes:Xml
          COVERAGE=$(grep LineCoverage CoverageReport/coverage.xml | cut -d'"' -f2)
          if (( $(echo "$COVERAGE < 80" | bc -l) )); then
            echo "Coverage below 80%: $COVERAGE%"
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