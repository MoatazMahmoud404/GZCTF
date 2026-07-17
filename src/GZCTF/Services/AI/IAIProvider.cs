namespace GZCTF.Services.AI;

/// <summary>
/// AI message role
/// </summary>
public enum AiMessageRole
{
    System,
    User,
    Assistant
}

/// <summary>
/// A single message in an AI conversation
/// </summary>
public record AiMessage(AiMessageRole Role, string Content);

/// <summary>
/// Request to send to an AI provider
/// </summary>
public record AiRequest(List<AiMessage> Messages, double? Temperature = null, int? MaxTokens = null);

/// <summary>
/// Response from an AI provider
/// </summary>
public record AiResponse(string Content, string? FinishReason = null, int? PromptTokens = null, int? CompletionTokens = null);

/// <summary>
/// Provider-agnostic interface for AI chat completions
/// </summary>
public interface IAIProvider
{
    /// <summary>
    /// Send a chat completion request to the configured AI provider
    /// </summary>
    /// <param name="request">The chat completion request</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>AI response with content and metadata</returns>
    Task<AiResponse> SendAsync(AiRequest request, CancellationToken ct = default);

    /// <summary>
    /// Check if the provider is properly configured and reachable
    /// </summary>
    Task<bool> HealthCheckAsync(CancellationToken ct = default);
}
