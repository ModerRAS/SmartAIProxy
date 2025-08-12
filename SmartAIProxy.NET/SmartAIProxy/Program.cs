using Serilog;
using SmartAIProxy.Core.Channels;
using SmartAIProxy.Core.Config;
using SmartAIProxy.Core.Rules;
using SmartAIProxy.Middleware;
using Prometheus;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/smartaiproxy-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    
    // Configure logging
    builder.Host.UseSerilog();

    // Add services
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    
    // Add custom services
    builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();
    builder.Services.AddSingleton<IRuleEngine, RuleEngine>();
    builder.Services.AddSingleton<IChannelService, ChannelService>();
    
    // Add HTTP client factory
    builder.Services.AddHttpClient();
    
    // Add authentication
    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer(options =>
        {
            // Configuration will be done in Configure method
        });
    
    // Add authorization
    builder.Services.AddAuthorization();
    
    // Add health checks
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseSerilogRequestLogging();
    
    // Add Prometheus metrics
    app.UseMetricServer();
    app.UseHttpMetrics();

    app.UseHttpsRedirection();
    
    // Add authentication and authorization middleware
    app.UseAuthentication();
    app.UseAuthorization();
    
    // Add custom proxy middleware
    app.UseMiddleware<ProxyMiddleware>();
    
    app.MapControllers();
    
    // Add health check endpoint
    app.MapHealthChecks("/healthz");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}