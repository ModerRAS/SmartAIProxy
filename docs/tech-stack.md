# SmartAIProxy .NET 9 技术栈选型

## 1. 核心框架

### 1.1 ASP.NET Core 9.0
**用途**: Web框架和运行时环境
**优势**:
- 高性能，支持Native AOT编译
- 跨平台支持（Windows、Linux、macOS）
- 丰富的中间件生态系统
- 内置依赖注入容器
- 强大的路由和模型绑定功能
- 支持HTTP/3协议

**版本**: .NET 9.0 (最新LTS版本)

### 1.2 C# 13
**用途**: 主要编程语言
**优势**:
- 最新语言特性支持
- 高性能和类型安全
- 丰富的标准库
- 强大的LINQ支持
- 异步编程模型完善

**项目结构**: 项目已优化为单层结构，所有代码位于根目录，便于维护和部署。

## 2. 配置管理

### 2.1 Microsoft.Extensions.Configuration
**用途**: 配置管理框架
**优势**:
- 支持多种配置源（JSON、YAML、环境变量等）
- 热重载支持
- 强类型配置绑定
- 配置验证机制

### 2.2 NetEscapades.Configuration.Yaml
**用途**: YAML配置文件支持
**优势**:
- 与现有Go版本配置文件兼容
- 支持复杂数据结构
- 良好的序列化/反序列化性能

## 3. 表达式引擎

### 3.1 NCalc
**用途**: 路由规则表达式计算
**优势**:
- 成熟稳定的表达式引擎
- 支持数学和逻辑运算
- 可扩展的函数和参数
- 良好的性能表现
- 与Go的govaluate语法相似

**替代方案**:
- **System.Linq.Expressions**: .NET内置表达式树，但需要更多自定义开发
- **Microsoft.CodeAnalysis.Scripting**: Roslyn脚本引擎，功能强大但较重

## 4. 监控和指标

### 4.1 prometheus-net.AspNetCore
**用途**: Prometheus指标收集和导出
**优势**:
- 与Prometheus生态系统完全兼容
- 自动收集ASP.NET Core指标
- 支持自定义指标定义
- 高性能指标收集

### 4.2 OpenTelemetry .NET
**用途**: 分布式追踪和指标收集
**优势**:
- 标准化的可观测性框架
- 支持多种后端系统
- 与Prometheus集成良好
- 自动仪表化ASP.NET Core

## 5. 日志系统

### 5.1 Serilog
**用途**: 结构化日志记录
**优势**:
- 丰富的输出目标支持
- 结构化日志格式
- 良好的性能表现
- 灵活的配置选项

### 5.2 Microsoft.Extensions.Logging
**用途**: 日志抽象层
**优势**:
- .NET标准日志接口
- 与Serilog无缝集成
- 支持多种日志级别

## 6. 安全认证

### 6.1 Microsoft.AspNetCore.Authentication.JwtBearer
**用途**: JWT令牌认证
**优势**:
- 标准化的JWT处理
- 与ASP.NET Core深度集成
- 支持多种令牌验证选项

### 6.2 Microsoft.AspNetCore.Authentication
**用途**: API密钥认证
**优势**:
- 灵活的认证方案扩展
- 支持自定义认证处理器

## 7. 容错和重试

### 7.1 Polly
**用途**: 弹性和瞬态故障处理
**优势**:
- 成熟的容错库
- 支持重试、熔断、超时等模式
- 策略组合和链式调用
- 与ASP.NET Core集成良好

### 7.2 Microsoft.Extensions.Http.Polly
**用途**: HTTP客户端容错扩展
**优势**:
- 为HttpClient提供Polly集成
- 简化HTTP请求的容错处理

## 8. HTTP客户端

### 8.1 System.Net.Http.HttpClient
**用途**: HTTP请求发送
**优势**:
- .NET内置HTTP客户端
- 支持连接池和复用
- 异步操作支持
- 与Polly集成良好

### 8.2 System.Net.Http.Json
**用途**: JSON序列化/反序列化
**优势**:
- .NET 5+内置JSON扩展
- 高性能System.Text.Json集成
- 简化的JSON操作API

## 9. 数据验证

### 9.1 System.ComponentModel.DataAnnotations
**用途**: 数据注解验证
**优势**:
- .NET标准验证框架
- 与ASP.NET Core模型绑定集成
- 支持自定义验证属性

### 9.2 FluentValidation
**用途**: 流畅API验证
**优势**:
- 更灵活的验证规则定义
- 支持复杂验证逻辑
- 与ASP.NET Core集成良好

## 10. 测试框架

### 10.1 xUnit.net
**用途**: 单元测试框架
**优势**:
- .NET生态系统主流测试框架
- 并行测试执行
- 丰富的断言库
- 良好的IDE集成

### 10.2 Moq
**用途**: 模拟框架
**优势**:
- LINQ到Mocks语法
- 强类型模拟
- 行为验证支持

### 10.3 Microsoft.AspNetCore.Mvc.Testing
**用途**: 集成测试
**优势**:
- 真实HTTP服务器测试
- 与ASP.NET Core控制器集成
- 内存中测试服务器

### 10.4 Testcontainers
**用途**: 容器化集成测试
**优势**:
- 真实依赖环境测试
- 支持多种数据库和中间件
- 自动化容器生命周期管理

## 11. 性能优化

### 11.1 System.Text.Json
**用途**: JSON序列化
**优势**:
- .NET内置高性能JSON库
- 比Newtonsoft.Json性能更好
- 内存使用更少

### 11.2 MemoryCache
**用途**: 内存缓存
**优势**:
- .NET内置缓存实现
- 支持过期策略
- 线程安全

### 11.3 System.Threading.Channels
**用途**: 高性能消息传递
**优势**:
- 生产者-消费者模式支持
- 内存高效
- 异步操作支持

## 12. 开发工具

### 12.1 Visual Studio / VS Code
**用途**: IDE支持
**优势**:
- 强大的调试功能
- IntelliSense代码补全
- 项目模板和脚手架

### 12.2 Docker
**用途**: 容器化部署
**优势**:
- 一致的运行环境
- 简化部署流程
- 资源隔离

### 12.3 Kubernetes
**用途**: 容器编排
**优势**:
- 自动扩缩容
- 服务发现
- 负载均衡

## 13. CI/CD工具

### 13.1 GitHub Actions
**用途**: 持续集成/持续部署
**优势**:
- 与GitHub深度集成
- 丰富的Action生态系统
- 并行执行支持

### 13.2 SonarQube
**用途**: 代码质量分析
**优势**:
- 静态代码分析
- 代码覆盖率报告
- 技术债务评估

## 14. 性能测试工具

### 14.1 k6
**用途**: 负载测试
**优势**:
- JavaScript脚本编写
- 实时指标展示
- 云原生支持

### 14.2 wrk
**用途**: HTTP基准测试
**优势**:
- 高性能HTTP测试工具
- 多线程支持
- Lua脚本扩展

## 15. 技术选型理由总结

### 性能优先
选择.NET 9和ASP.NET Core是因为它们提供了卓越的性能表现，特别是在高并发场景下。Native AOT编译进一步减少了启动时间和内存占用。

### 生态系统成熟
选择经过生产验证的库（如Polly、Serilog、xUnit）确保了稳定性和社区支持。

### 兼容性考虑
保持与Go版本的API兼容性是关键要求，因此选择的库和框架都能支持这种兼容性需求。

### 开发效率
利用.NET生态系统丰富的工具和库可以显著提高开发效率，减少重复造轮子的工作。

### 云原生友好
所选技术栈天然支持容器化部署和云原生架构，便于在现代基础设施上运行。