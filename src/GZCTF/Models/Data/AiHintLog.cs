using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GZCTF.Models.Data;

/// <summary>
/// Log of AI hint requests for auditing and rate limiting
/// </summary>
public class AiHintLog
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Challenge ID
    /// </summary>
    [Required]
    public int ChallengeId { get; set; }

    /// <summary>
    /// Participation ID (team)
    /// </summary>
    [Required]
    public int ParticipationId { get; set; }

    /// <summary>
    /// User ID who requested the hint
    /// </summary>
    [Required]
    public Guid UserId { get; set; }

    /// <summary>
    /// The hint text returned by the AI
    /// </summary>
    public string? HintText { get; set; }

    /// <summary>
    /// Progression level (1-5)
    /// </summary>
    public int Progression { get; set; }

    /// <summary>
    /// Hint type (tooling, technique, concept, direction)
    /// </summary>
    public string? HintType { get; set; }

    /// <summary>
    /// Whether the hint was successfully generated
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if generation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Time the hint was requested
    /// </summary>
    public DateTimeOffset RequestTimeUtc { get; set; } = DateTimeOffset.UtcNow;

    // Navigation properties
    [JsonIgnore]
    public Participation? Participation { get; set; }

    [JsonIgnore]
    public GameChallenge? Challenge { get; set; }
}
