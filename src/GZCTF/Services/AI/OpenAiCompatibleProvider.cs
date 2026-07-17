using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using GZCTF.Models.Internal;
using Microsoft.Extensions.Options;

namespace GZCTF.Services.AI;

/// <summary>
/// OpenAI-compatible API provider (works with OpenAI, OpenCode, Ollama, vLLM, LM Studio, etc.)
/// </summary>
public class OpenAiCompatibleProvider : IAIProvider, IDisposable
{
    readonly HttpClient _httpClient;
    readonly AiProviderConfig _config;
    readonly ILogger<OpenAiCompatibleProvider> _logger;

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OpenAiCompatibleProvider(IOptionsSnapshot<AiGlobalConfig> aiConfig,
        ILogger<OpenAiCompatibleProvider> logger)
    {
        _config = aiConfig.Value.Provider ?? new AiProviderConfig();
        _logger = logger;
        _httpClient = new HttpClient();

        if (!string.IsNullOrEmpty(_config.BaseUrl))
        {
            var baseUrl = _config.BaseUrl.TrimEnd('/');
            if (!baseUrl.EndsWith("/v1"))
                baseUrl += "/v1";
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        if (!string.IsNullOrEmpty(_config.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _config.ApiKey);
        }
    }

    /// <summary>
    /// Send a chat completion request to OpenAI-compatible API
    /// </summary>
    public async Task<AiResponse> SendAsync(AiRequest request, CancellationToken ct = default)
    {
        var body = new
        {
            model = _config.Model,
            messages = request.Messages.Select(m => new
            {
                role = m.Role switch
                {
                    AiMessageRole.System => "system",
                    AiMessageRole.User => "user",
                    AiMessageRole.Assistant => "assistant",
                    _ => "user"
                },
                content = m.Content
            }),
            temperature = request.Temperature ?? _config.Temperature,
            max_tokens = request.MaxTokens ?? _config.MaxTokens
        };

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(_config.TimeoutSeconds));

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/chat/completions", body, JsonOptions, cts.Token);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cts.Token);

            if (result?.Choices is null || result.Choices.Count == 0)
            {
                _logger.LogWarning("AI provider returned no choices");
                return new AiResponse(string.Empty, "no_choices");
            }

            var choice = result.Choices[0];
            return new AiResponse(
                choice.Message?.Content ?? string.Empty,
                choice.FinishReason,
                result.Usage?.PromptTokens,
                result.Usage?.CompletionTokens
            );
        }
        catch (TaskCanceledException)
        {
            _logger.LogWarning("AI request timed out after {Timeout}s", _config.TimeoutSeconds);
            return new AiResponse(string.Empty, "timeout");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "AI request failed: {Message}", ex.Message);
            return new AiResponse(string.Empty, "error");
        }
    }

    /// <summary>
    /// Check if the provider is reachable by listing models
    /// </summary>
    public async Task<bool> HealthCheckAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/models", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public void Dispose() => _httpClient.Dispose();

    #region DTOs

    class ChatCompletionResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }

    class Choice
    {
        [JsonPropertyName("message")]
        public Message? Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }

        [JsonPropertyName("index")]
        public int Index { get; set; }
    }

    class Message
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int? PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int? CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int? TotalTokens { get; set; }
    }

    #endregion
}
