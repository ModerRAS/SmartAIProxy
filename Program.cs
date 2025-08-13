using Serilog;
using SmartAIProxy.Core.Channels;
using SmartAIProxy.Core.Config;
using SmartAIProxy.Core.Rules;
using SmartAIProxy.Middleware;
using Prometheus;

/// <summary>
/// SmartAIProxy 应用程序入口点
/// 这是一个高性能、可扩展的AI API网关服务，用于将API请求转发到主要的AI模型（OpenAI、Anthropic Claude、Google Gemini等）
/// </summary>
public class Program
{
    /// <summary>
    /// 应用程序主入口点
    /// </summary>
    /// <param name="args">命令行参数</param>
    public static void Main(string[] args)
    {
        // 配置Serilog日志记录器
        // 日志将同时输出到控制台和文件，文件按天滚动
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .WriteTo.File("logs/smartaiproxy-.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        try
        {
            // 创建Web应用程序构建器
            var builder = WebApplication.CreateBuilder(args);
            
            // 配置主机使用Serilog进行日志记录
            builder.Host.UseSerilog();

            // 添加基础服务
            builder.Services.AddControllers();          // 添加MVC控制器服务
            builder.Services.AddEndpointsApiExplorer(); // 添加API端点探索器
            builder.Services.AddSwaggerGen();         // 添加Swagger生成器
            
            // 添加自定义核心服务
            builder.Services.AddSingleton<IConfigurationService, ConfigurationService>(); // 配置服务
            builder.Services.AddSingleton<IRuleEngine, RuleEngine>();                     // 规则引擎
            builder.Services.AddSingleton<IChannelService, ChannelService>();               // 通道服务
            
            // 添加HTTP客户端工厂，用于创建HTTP客户端实例
            builder.Services.AddHttpClient();
            
            // 添加JWT身份验证服务
            builder.Services.AddAuthentication("Bearer")
                .AddJwtBearer(options =>
                {
                    // JWT配置将在Configure方法中完成
                });
            
            // 添加授权服务
            builder.Services.AddAuthorization();
            
            // 添加健康检查服务
            builder.Services.AddHealthChecks();

            // 构建应用程序
            var app = builder.Build();

            // 配置HTTP请求管道
            if (app.Environment.IsDevelopment())
            {
                // 在开发环境中启用Swagger和SwaggerUI
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // 使用Serilog请求日志记录中间件
            app.UseSerilogRequestLogging();
            
            // 添加Prometheus指标监控
            app.UseMetricServer();  // 启动Prometheus指标服务器
            app.UseHttpMetrics();   // 收集HTTP指标

            // 启用HTTPS重定向
            app.UseHttpsRedirection();
            
            // 添加身份验证和授权中间件
            app.UseAuthentication(); // 身份验证中间件
            app.UseAuthorization();  // 授权中间件
            
            // 添加自定义代理中间件，用于处理AI API请求转发
            app.UseMiddleware<ProxyMiddleware>();
            
            // 映射控制器路由
            app.MapControllers();
            
            // 添加健康检查端点
            app.MapHealthChecks("/healthz");

            // 运行应用程序
            app.Run();
        }
        catch (Exception ex)
        {
            // 记录致命错误
            Log.Fatal(ex, "应用程序意外终止");
        }
        finally
        {
            // 关闭并刷新日志记录器
            Log.CloseAndFlush();
        }
    }
}