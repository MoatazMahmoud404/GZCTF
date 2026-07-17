using System.Text.Json.Serialization;

// ReSharper disable UnusedMember.Global
// ReSharper disable ClassNeverInstantiated.Global

namespace GZCTF.Models.Internal;

/// <summary>
/// Response model returned by the AI hint endpoint
/// </summary>
public class AiHintResponseModel
{
    /// <summary>
    /// The generated hint text
    /// </summary>
    [JsonPropertyName("hint")]
    public string Hint { get; set; } = string.Empty;

    /// <summary>
    /// Progression level (1-5) indicating how close the hint is to the solution
    /// </summary>
    [JsonPropertyName("progression")]
    public int Progression { get; set; }

    /// <summary>
    /// Hint category: tooling, technique, concept, or direction
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Number of hints already used by the team for this challenge
    /// </summary>
    [JsonPropertyName("usedCount")]
    public int UsedCount { get; set; }

    /// <summary>
    /// Total hint budget for this challenge
    /// </summary>
    [JsonPropertyName("budget")]
    public int Budget { get; set; }
}
