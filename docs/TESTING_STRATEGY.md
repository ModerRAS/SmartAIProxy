# SmartAIProxy 全面测试策略

## 当前测试覆盖率分析

### 覆盖率现状 (50.0% 总体)
- **internal/admin**: 41.9% - 管理接口覆盖率较低，主要缺失实际处理逻辑
- **internal/config**: 68.0% - 配置模块较好，但热更新测试缺失
- **internal/fault**: 69.7% - 容错机制部分实现，主要方法有待完善
- **internal/gateway**: 12.3% - 核心网关逻辑严重缺失测试
- **internal/logger**: 81.0% - 日志模块较好，达到目标
- **internal/monitor**: 53.8% - 监控指标部分实现
- **internal/provider**: 88.9% - 渠道商管理基本完整
- **internal/router**: 59.6% - 路由核心逻辑有待增强
- **internal/rule**: 93.9% - 规则引擎较为完整
- **internal/security**: 87.0% - 安全模块较好

## 测试目标

### 总体目标
- **单元测试覆盖率**: >90%
- **集成测试覆盖率**: >85%
- **端到端测试**: 覆盖主要业务场景
- **性能测试**: 并发请求处理能力

### 分层测试策略

#### 1. 单元测试（Unit Tests）
- **目标**: 验证每个函数的独立逻辑
- **覆盖率**: 每个文件>90%覆盖率
- **范围**: 所有public方法 +关键private方法
- **依赖**: 使用接口隔离，mock外部依赖

#### 2. 集成测试（Integration Tests）
- **目标**: 验证模块间交互
- **覆盖率**: 80%的模块组合场景
- **范围**: 
  - 配置加载 + 服务启动
  - API转发完整流程
  - 渠道商选择 + 路由决策
  - 故障容错完整链
  - 监控指标验证

#### 3. 端到端测试（E2E Tests）
- **目标**: 验证完整业务流程
- **场景**:
  1. 简单API转发
  2. 规则匹配选择
  3. 多渠道商故障切换
  4. 热更新验证
  5. 并发压力测试

#### 4. 性能测试（Performance Tests）
- **并发测试**: 1000个并发请求
- **内存测试**: 内存泄漏检测
- **响应时间**: P99 < 100ms

## 测试实现计划

### Phase 1: 单元测试增强
1. **gateway模块**：核心转发逻辑测试
2. **monitor模块**：指标收集验证
3. **admin模块**：管理接口测试
4. **router模块**：路由决策逻辑

### Phase 2: 集成测试
1. HTTP服务集成测试
2. 配置热更新测试
3. 故障链路测试
4. 并发测试

### Phase 3: 端到端测试
1. 完整业务场景
2. 性能基准测试
3. 压力测试
4. 回归测试

### Phase 4: CI/CD集成
1. GitHub Actions工作流
2. 自动化代码审查
3. 部署验证
4. 监控告警

## 测试用例设计

### 单元测试用例

#### 1. Gateway模块测试
```go
// TestProxyHandler 完整API转发测试
TestProxyHandler_BasicForward()
TestProxyHandler_ChannelSelection()
TestProxyHandler_ErrorHandling()
TestProxyHandler_TimeoutHandling()
TestProxyHandler_MetricCollection()

// TestForwardRequest 函数级别的测试
TestForwardRequest_Successful()
TestForwardRequest_Timeout()
TestForwardRequest_Retry()
TestForwardRequest_MetricRecording()
```

#### 2. 规则引擎测试
```go
TestRouter_SelectBestProvider_ComplexRules()
TestRouter_SelectBestProvider_EmptyProviders()
TestRouter_SelectBestProvider_AllInactive()
TestRouter_SelectBestProvider_PriorityLogic()
```

#### 3. 配置热更新测试
```go
TestConfig_HotReload()
TestConfig_InvalidUpdate()
TestConfig_ComplexRules_Update()
```

### 集成测试用例

#### 1. 服务启动流程
```go
TestServerStartup_Success()
TestServerStartup_InvalidConfig()
TestServerStartup_PortConflict()
```

#### 2. API转发集成
```go
TestAPICompleteFlow()
TestAPI_RoundRobinSelection()
TestAPI_QuotaExhaustion()
TestAPI_CircuitBreaker()
```

#### 3. 故障容错测试
```go
TestFaultTolerance_SingleProviderFail()
TestFaultTolerance_MultipleProviderFail()
TestFaultTolerance_RetrySuccess()
TestFaultTolerance_CircuitOpenClose()
```

## 测试工具和技术

### 单元测试工具
- **测试框架**: Go标准testing包
- **断言库**: testify/assert, require
- **Mock框架**: testify/mock, gomock
- **覆盖率**: go tool cover

### 集成测试工具
- **HTTP测试**: httptest, httpmock
- **服务测试**: testify/suite
- **数据库测试**: 内存配置
- **并发测试**: go test -race

### 性能测试工具
- **Benchmark**: go test -bench
- **压力测试**: vegeta
- **内存分析**: go tool pprof
- **并发测试**: go test -race

### 端到端测试工具
- **API测试**: resty, httpie
- **性能基准**: wrk, hey
- **监控**: Prometheus + Grafana
- **告警**: Prometheus Alertmanager

## 测试数据管理

### Mock数据生成
```go
// 配置测试数据
testConfig := &config.Config{
    Server: config.ServerConfig{
        Listen:         "0.0.0.0:8080",
        Timeout:        30,
        MaxConnections: 100,
    },
    Channels: []config.ChannelConfig{
        {
            Name:      "测试渠道1",
            Type:      "openai",
            Endpoint:  "http://localhost:3000",
            APIKey:    "test-key-1",
            Price:     0.01,
            Priority:  1,
        },
    },
    Rules: []config.RuleConfig{
        {
            Name:       "简单规则",
            Channel:    "测试渠道1",
            Expression: "true",
        },
    },
}
```

### 测试隔离策略
1. **内存配置**: 使用内存中的配置，避免文件依赖
2. **临时端口**: 使用端口0绑定，避免端口冲突
3. **Mock服务**: 创建模拟的下游服务
4. **清理函数**: 自动清理测试状态和文件

## 测试执行计划