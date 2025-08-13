namespace SmartAIProxy.Models.DTO;

public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
}

public class ChannelResponse
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public double PricePerToken { get; set; }
    public int DailyLimit { get; set; }
    public int Priority { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class RuleResponse
{
    public string Name { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public int Priority { get; set; }
}

public class HealthResponse
{
    public string Status { get; set; } = "healthy";
    public string Uptime { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
}

public class OpenAIChatRequest
{
    public string Model { get; set; } = string.Empty;
    public List<OpenAIMessage> Messages { get; set; } = new();
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 150;
    public bool Stream { get; set; } = false;
}

public class OpenAIMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public class OpenAIChatResponse
{
    public string Id { get; set; } = string.Empty;
    public string Object { get; set; } = "chat.completion";
    public long Created { get; set; }
    public string Model { get; set; } = string.Empty;
    public List<OpenAIChoice> Choices { get; set; } = new();
    public OpenAIUsage? Usage { get; set; }
}

public class OpenAIChoice
{
    public int Index { get; set; }
    public OpenAIMessage Message { get; set; } = new();
    public string FinishReason { get; set; } = string.Empty;
}

public class OpenAIUsage
{
    public int PromptTokens { get; set; }
    public int CompletionTokens { get; set; }
    public int TotalTokens { get; set; }
}

public class ErrorResponse
{
    public ErrorInfo Error { get; set; } = new();
}

public class ErrorInfo
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
}