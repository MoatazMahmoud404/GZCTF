using System.Text.Json;
using System.Text.Json.Serialization;

namespace GZCTF.Services.AI;

/// <summary>
/// Parses and validates structured JSON responses from AI providers.
/// Handles malformed JSON, missing fields, and out-of-range values gracefully.
/// </summary>
public class AiResponseParser
{
    readonly ILogger<AiResponseParser> _logger;

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public AiResponseParser(ILogger<AiResponseParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parse a hint generation response from the AI
    /// </summary>
    public AiHintResult ParseHintResponse(string rawContent)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            _logger.LogWarning("Empty AI hint response");
            return AiHintResult.Failed("Empty response from AI provider");
        }

        try
        {
            // Try to extract JSON from code blocks if present
            var json = ExtractJson(rawContent);
            var hint = JsonSerializer.Deserialize<AiHintDto>(json, JsonOptions);

            if (hint is null || string.IsNullOrWhiteSpace(hint.Hint))
            {
                _logger.LogWarning("AI hint response missing required fields");
                return AiHintResult.Failed("Response missing hint content");
            }

            return AiHintResult.Success(
                hint.Hint,
                Math.Clamp(hint.Progression, 1, 5),
                hint.Type ?? "direction"
            );
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI hint response: {Content}", rawContent[..Math.Min(rawContent.Length, 200)]);
            return AiHintResult.Failed("Failed to parse AI response");
        }
    }

    /// <summary>
    /// Parse a grading response from the AI
    /// </summary>
    public AiGradingResult ParseGradingResponse(string rawContent, int maxScore)
    {
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            _logger.LogWarning("Empty AI grading response");
            return AiGradingResult.Failed("Empty response from AI provider");
        }

        try
        {
            var json = ExtractJson(rawContent);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Extract total score
            int totalScore = 0;
            if (root.TryGetProperty("score", out var scoreElem))
            {
                totalScore = scoreElem.GetInt32();
            }

            // Clamp to max score
            totalScore = Math.Clamp(totalScore, 0, maxScore);

            // Extract breakdown
            var breakdown = new Dictionary<string, int>();
            if (root.TryGetProperty("breakdown", out var breakdownElem) && breakdownElem.ValueKind == JsonValueKind.Object)
            {
                foreach (var category in breakdownElem.EnumerateObject())
                {
                    if (category.Value.ValueKind == JsonValueKind.Number)
                    {
                        breakdown[category.Name] = category.Value.GetInt32();
                    }
                }
            }

            // Extract feedback
            var feedback = new List<string>();
            if (root.TryGetProperty("feedback", out var feedbackElem) && feedbackElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in feedbackElem.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                        feedback.Add(item.GetString() ?? string.Empty);
                }
            }

            // Extract strengths
            var strengths = new List<string>();
            if (root.TryGetProperty("strengths", out var strengthsElem) && strengthsElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in strengthsElem.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                        strengths.Add(item.GetString() ?? string.Empty);
                }
            }

            // Extract weaknesses
            var weaknesses = new List<string>();
            if (root.TryGetProperty("weaknesses", out var weaknessesElem) && weaknessesElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in weaknessesElem.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                        weaknesses.Add(item.GetString() ?? string.Empty);
                }
            }

            return AiGradingResult.Success(totalScore, breakdown, feedback, strengths, weaknesses);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI grading response");
            return AiGradingResult.Failed("Failed to parse AI response");
        }
    }

    /// <summary>
    /// Extract JSON from raw content, handling markdown code blocks
    /// </summary>
    static string ExtractJson(string raw)
    {
        var trimmed = raw.Trim();

        // Handle ```json ... ``` blocks
        const string jsonStart = "```json";
        const string jsonEnd = "```";
        int startIdx = trimmed.IndexOf(jsonStart, StringComparison.OrdinalIgnoreCase);
        if (startIdx >= 0)
        {
            startIdx += jsonStart.Length;
            int endIdx = trimmed.IndexOf(jsonEnd, startIdx, StringComparison.OrdinalIgnoreCase);
            if (endIdx >= 0)
            {
                return trimmed[startIdx..endIdx].Trim();
            }
            return trimmed[startIdx..].Trim();
        }

        // Try raw JSON parse
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
            return trimmed;

        // Find first { and last }
        int firstBrace = trimmed.IndexOf('{');
        int lastBrace = trimmed.LastIndexOf('}');
        if (firstBrace >= 0 && lastBrace > firstBrace)
            return trimmed[firstBrace..(lastBrace + 1)];

        return trimmed;
    }

    #region DTOs

    class AiHintDto
    {
        public string? Hint { get; set; }
        public int Progression { get; set; } = 1;
        public string? Type { get; set; }
    }

    #endregion
}

/// <summary>
/// Result of AI hint generation
/// </summary>
public record AiHintResult
{
    public bool IsSuccess { get; init; }
    public string? Hint { get; init; }
    public int Progression { get; init; }
    public string? Type { get; init; }
    public string? Error { get; init; }

    public static AiHintResult Success(string hint, int progression, string type) => new()
    {
        IsSuccess = true,
        Hint = hint,
        Progression = progression,
        Type = type
    };

    public static AiHintResult Failed(string error) => new()
    {
        IsSuccess = false,
        Error = error
    };
}

/// <summary>
/// Result of AI writeup grading
/// </summary>
public record AiGradingResult
{
    public bool IsSuccess { get; init; }
    public int Score { get; init; }
    public Dictionary<string, int>? Breakdown { get; init; }
    public List<string>? Feedback { get; init; }
    public List<string>? Strengths { get; init; }
    public List<string>? Weaknesses { get; init; }
    public string? Error { get; init; }

    public static AiGradingResult Success(int score, Dictionary<string, int>? breakdown,
        List<string>? feedback, List<string>? strengths, List<string>? weaknesses) => new()
    {
        IsSuccess = true,
        Score = score,
        Breakdown = breakdown,
        Feedback = feedback,
        Strengths = strengths,
        Weaknesses = weaknesses
    };

    public static AiGradingResult Failed(string error) => new()
    {
        IsSuccess = false,
        Error = error
    };
}
