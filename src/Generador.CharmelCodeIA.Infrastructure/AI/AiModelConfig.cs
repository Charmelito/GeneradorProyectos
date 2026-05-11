namespace Generador.CharmelCodeIA.Infrastructure.AI;

public sealed class AiModelConfig
{
    public string Provider { get; set; } = "DeepSeek";
    public string ModelId { get; set; } = "deepseek-chat";
    public string ApiKey { get; set; } = string.Empty;
    public Uri Endpoint { get; set; } = new("https://api.deepseek.com/v1");
    public float Temperature { get; set; } = 0.2f;
    public int MaxTokens { get; set; } = 8192;
    public string? OrganizationId { get; set; }
    public Dictionary<string, string> AdditionalHeaders { get; set; } = new();

    public static AiModelConfig DeepSeek(string apiKey, string modelId = "deepseek-chat") => new()
    {
        Provider = "DeepSeek",
        ModelId = modelId,
        ApiKey = apiKey,
        Endpoint = new Uri("https://api.deepseek.com/v1"),
        Temperature = 0.2f,
        MaxTokens = 8192
    };

    public static AiModelConfig AzureOpenAI(string endpoint, string apiKey, string deploymentName) => new()
    {
        Provider = "AzureOpenAI",
        ModelId = deploymentName,
        ApiKey = apiKey,
        Endpoint = new Uri(endpoint),
        Temperature = 0.2f,
        MaxTokens = 8192
    };

    public static AiModelConfig OpenAI(string apiKey, string modelId = "gpt-4o") => new()
    {
        Provider = "OpenAI",
        ModelId = modelId,
        ApiKey = apiKey,
        Endpoint = null!,
        Temperature = 0.2f,
        MaxTokens = 8192
    };
}
