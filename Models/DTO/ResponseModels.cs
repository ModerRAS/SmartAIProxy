namespace SmartAIProxy.Models.DTO;

/// <summary>
/// API响应基类
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// 请求是否成功
    /// </summary>
    public bool Success { get; set; }
    
    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 响应数据
    /// </summary>
    public object? Data { get; set; }
}

/// <summary>
/// 通道响应类
/// </summary>
public class ChannelResponse
{
    /// <summary>
    /// 通道名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 通道类型
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// 通道端点
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;
    
    /// <summary>
    /// 每个令牌的价格
    /// </summary>
    public double PricePerToken { get; set; }
    
    /// <summary>
    /// 每日限制
    /// </summary>
    public int DailyLimit { get; set; }
    
    /// <summary>
    /// 优先级
    /// </summary>
    public int Priority { get; set; }
    
    /// <summary>
    /// 通道状态
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// 规则响应类
/// </summary>
public class RuleResponse
{
    /// <summary>
    /// 规则名称
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// 规则关联的通道名称
    /// </summary>
    public string Channel { get; set; } = string.Empty;
    
    /// <summary>
    /// 规则表达式
    /// </summary>
    public string Expression { get; set; } = string.Empty;
    
    /// <summary>
    /// 规则优先级
    /// </summary>
    public int Priority { get; set; }
}

/// <summary>
/// 健康检查响应类
/// </summary>
public class HealthResponse
{
    /// <summary>
    /// 系统状态
    /// </summary>
    public string Status { get; set; } = "healthy";
    
    /// <summary>
    /// 系统运行时间
    /// </summary>
    public string Uptime { get; set; } = string.Empty;
    
    /// <summary>
    /// 系统版本
    /// </summary>
    public string Version { get; set; } = "1.0.0";
}

/// <summary>
/// OpenAI聊天请求类
/// </summary>
public class OpenAIChatRequest
{
    /// <summary>
    /// 使用的模型名称
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// 消息列表
    /// </summary>
    public List<OpenAIMessage> Messages { get; set; } = new();
    
    /// <summary>
    /// 温度参数，控制随机性
    /// </summary>
    public double Temperature { get; set; } = 0.7;
    
    /// <summary>
    /// 最大令牌数
    /// </summary>
    public int MaxTokens { get; set; } = 150;
    
    /// <summary>
    /// 是否使用流式响应
    /// </summary>
    public bool Stream { get; set; } = false;
}

/// <summary>
/// OpenAI消息类
/// </summary>
public class OpenAIMessage
{
    /// <summary>
    /// 消息角色（system、user、assistant）
    /// </summary>
    public string Role { get; set; } = "user";
    
    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// OpenAI聊天响应类
/// </summary>
public class OpenAIChatResponse
{
    /// <summary>
    /// 响应ID
    /// </summary>
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// 响应对象类型
    /// </summary>
    public string Object { get; set; } = "chat.completion";
    
    /// <summary>
    /// 响应创建时间戳
    /// </summary>
    public long Created { get; set; }
    
    /// <summary>
    /// 使用的模型
    /// </summary>
    public string Model { get; set; } = string.Empty;
    
    /// <summary>
    /// 响应选项列表
    /// </summary>
    public List<OpenAIChoice> Choices { get; set; } = new();
    
    /// <summary>
    /// 令牌使用统计
    /// </summary>
    public OpenAIUsage? Usage { get; set; }
}

/// <summary>
/// OpenAI选项类
/// </summary>
public class OpenAIChoice
{
    /// <summary>
    /// 选项索引
    /// </summary>
    public int Index { get; set; }
    
    /// <summary>
    /// 选项消息
    /// </summary>
    public OpenAIMessage Message { get; set; } = new();
    
    /// <summary>
    /// 完成原因
    /// </summary>
    public string FinishReason { get; set; } = string.Empty;
}

/// <summary>
/// OpenAI使用统计类
/// </summary>
public class OpenAIUsage
{
    /// <summary>
    /// 提示令牌数
    /// </summary>
    public int PromptTokens { get; set; }
    
    /// <summary>
    /// 完成令牌数
    /// </summary>
    public int CompletionTokens { get; set; }
    
    /// <summary>
    /// 总令牌数
    /// </summary>
    public int TotalTokens { get; set; }
}

/// <summary>
/// 错误响应类
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// 错误信息
    /// </summary>
    public ErrorInfo Error { get; set; } = new();
}

/// <summary>
/// 错误信息类
/// </summary>
public class ErrorInfo
{
    /// <summary>
    /// 错误代码
    /// </summary>
    public string Code { get; set; } = string.Empty;
    
    /// <summary>
    /// 错误消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// 错误类型
    /// </summary>
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// 登录请求类
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// 用户名
    /// </summary>
    public string Username { get; set; } = string.Empty;
    
    /// <summary>
    /// 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// 登录响应类
/// </summary>
public class LoginResponse
{
    /// <summary>
    /// 访问令牌
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;
    
    /// <summary>
    /// 令牌类型
    /// </summary>
    public string TokenType { get; set; } = "Bearer";
    
    /// <summary>
    /// 过期时间（秒）
    /// </summary>
    public int ExpiresIn { get; set; }
}