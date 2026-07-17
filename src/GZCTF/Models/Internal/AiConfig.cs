using System.ComponentModel.DataAnnotations;

namespace GZCTF.Models.Internal;

/// <summary>
/// AI provider type
/// </summary>
public enum AiProviderType
{
    /// <summary>
    /// OpenAI-compatible API (OpenAI, OpenCode, vLLM, LM Studio, etc.)
    /// </summary>
    OpenAiCompatible,

    /// <summary>
    /// Anthropic Claude
    /// </summary>
    Anthropic,

    /// <summary>
    /// Google Gemini
    /// </summary>
    Gemini,

    /// <summary>
    /// Ollama (local models)
    /// </summary>
    Ollama,

    /// <summary>
    /// OpenRouter
    /// </summary>
    OpenRouter,

    /// <summary>
    /// Custom / other
    /// </summary>
    Custom
}

/// <summary>
/// AI provider configuration
/// </summary>
public class AiProviderConfig
{
    /// <summary>
    /// AI provider type
    /// </summary>
    public AiProviderType Provider { get; set; } = AiProviderType.OpenAiCompatible;

    /// <summary>
    /// Base URL of the AI API endpoint
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Model name to use
    /// </summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// Temperature for generation (0.0 - 2.0)
    /// </summary>
    [Range(0.0, 2.0)]
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// Maximum tokens to generate
    /// </summary>
    [Range(1, 128_000)]
    public int MaxTokens { get; set; } = 4096;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    [Range(1, 300)]
    public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// AI settings (stored via ConfigService)
/// </summary>
public class AiGlobalConfig
{
    /// <summary>
    /// Whether AI features are enabled globally
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Default provider configuration
    /// </summary>
    public AiProviderConfig? Provider { get; set; } = new();

    /// <summary>
    /// Maximum hint budget per challenge (number of AI hints allowed)
    /// </summary>
    [Range(0, 20)]
    public int MaxHintsPerChallenge { get; set; } = 3;

    /// <summary>
    /// Cooldown between hint requests per team in seconds
    /// </summary>
    [Range(0, 3600)]
    public int HintCooldownSeconds { get; set; } = 120;

    /// <summary>
    /// Whether to log all AI requests and responses for auditing
    /// </summary>
    public bool LogAiRequests { get; set; } = true;
}
