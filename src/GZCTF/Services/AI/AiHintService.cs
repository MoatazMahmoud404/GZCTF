using GZCTF.Models.Data;
using GZCTF.Models.Internal;
using GZCTF.Repositories.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GZCTF.Services.AI;

/// <summary>
/// Service for generating AI-powered hints for CTF challenges.
/// Handles rate limiting, budget tracking, and audit logging.
/// </summary>
public class AiHintService
{
    readonly IAIProvider _aiProvider;
    readonly ContextAssembler _contextAssembler;
    readonly AiResponseParser _responseParser;
    readonly ILogRepository _logRepo;
    readonly IOptionsSnapshot<AiGlobalConfig> _aiConfig;
    readonly ILogger<AiHintService> _logger;

    public AiHintService(
        IAIProvider aiProvider,
        ContextAssembler contextAssembler,
        AiResponseParser responseParser,
        ILogRepository logRepo,
        IOptionsSnapshot<AiGlobalConfig> aiConfig,
        ILogger<AiHintService> logger)
    {
        _aiProvider = aiProvider;
        _contextAssembler = contextAssembler;
        _responseParser = responseParser;
        _logRepo = logRepo;
        _aiConfig = aiConfig;
        _logger = logger;
    }

    /// <summary>
    /// Check if AI hints are globally enabled
    /// </summary>
    public bool IsEnabled => _aiConfig.Value.Enabled && _aiConfig.Value.Provider is not null;

    /// <summary>
    /// Get the number of hints already used by a team for a challenge
    /// </summary>
    public async Task<int> GetHintCountAsync(int challengeId, int participationId, CancellationToken ct = default)
    {
        await using var scope = _logRepo.GetScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.AiHintLogs
            .CountAsync(h => h.ChallengeId == challengeId && h.ParticipationId == participationId && h.IsSuccess, ct);
    }

    /// <summary>
    /// Check if a team is within the cooldown window
    /// </summary>
    public async Task<bool> IsInCooldownAsync(int challengeId, int participationId, CancellationToken ct = default)
    {
        var cooldown = _aiConfig.Value.HintCooldownSeconds;
        if (cooldown <= 0)
            return false;

        await using var scope = _logRepo.GetScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var lastHint = await db.AiHintLogs
            .Where(h => h.ChallengeId == challengeId && h.ParticipationId == participationId && h.IsSuccess)
            .OrderByDescending(h => h.RequestTimeUtc)
            .FirstOrDefaultAsync(ct);

        if (lastHint is null)
            return false;

        var elapsed = DateTimeOffset.UtcNow - lastHint.RequestTimeUtc;
        return elapsed.TotalSeconds < cooldown;
    }

    /// <summary>
    /// Get the hint budget for a challenge (challenge-specific or global default)
    /// </summary>
    public int GetBudget(GameChallenge challenge)
    {
        if (challenge.AiHintBudget > 0)
            return challenge.AiHintBudget;
        return _aiConfig.Value.MaxHintsPerChallenge;
    }

    /// <summary>
    /// Generate an AI hint for a challenge
    /// </summary>
    /// <param name="challenge">The challenge</param>
    /// <param name="participationId">Team participation ID</param>
    /// <param name="userId">User requesting the hint</param>
    /// <param name="playerProgress">Optional progress context (e.g. failed attempts)</param>
    /// <param name="challengeFiles">Optional challenge source files</param>
    /// <param name="ct">Cancellation token</param>
    public async Task<AiHintResult> GenerateHintAsync(
        GameChallenge challenge,
        int participationId,
        Guid userId,
        string? playerProgress = null,
        IReadOnlyList<string>? challengeFiles = null,
        CancellationToken ct = default)
    {
        if (!IsEnabled)
            return AiHintResult.Failed("AI features are not enabled");

        if (!challenge.AiHintsEnabled)
            return AiHintResult.Failed("AI hints are not enabled for this challenge");

        // Check budget
        var usedCount = await GetHintCountAsync(challenge.Id, participationId, ct);
        var budget = GetBudget(challenge);
        if (usedCount >= budget)
            return AiHintResult.Failed($"Hint budget exhausted ({usedCount}/{budget})");

        // Check cooldown
        if (await IsInCooldownAsync(challenge.Id, participationId, ct))
            return AiHintResult.Failed("Please wait before requesting another hint");

        // Get existing hints for context
        var existingHints = await GetExistingHintsAsync(challenge.Id, participationId, ct);

        // Build the AI request
        var request = _contextAssembler.BuildHintRequest(
            challengeTitle: challenge.Title,
            challengeContent: challenge.Content,
            challengeCategory: challenge.Category.ToString(),
            challengeFiles: challengeFiles,
            playerProgress: playerProgress,
            existingHints: existingHints,
            hintBudget: usedCount
        );

        // Call AI provider
        var response = await _aiProvider.SendAsync(request, ct);

        // Parse response
        var result = _responseParser.ParseHintResponse(response.Content);

        // Log the request
        await LogHintAsync(challenge, participationId, userId, result, ct);

        return result;
    }

    /// <summary>
    /// Get hints already revealed to this team for context
    /// </summary>
    async Task<List<string>> GetExistingHintsAsync(int challengeId, int participationId, CancellationToken ct)
    {
        await using var scope = _logRepo.GetScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.AiHintLogs
            .Where(h => h.ChallengeId == challengeId && h.ParticipationId == participationId && h.IsSuccess)
            .OrderBy(h => h.RequestTimeUtc)
            .Select(h => h.HintText ?? string.Empty)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Log the hint request for auditing
    /// </summary>
    async Task LogHintAsync(GameChallenge challenge, int participationId, Guid userId,
        AiHintResult result, CancellationToken ct)
    {
        try
        {
            await using var scope = _logRepo.GetScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var log = new AiHintLog
            {
                ChallengeId = challenge.Id,
                ParticipationId = participationId,
                UserId = userId,
                HintText = result.Hint,
                Progression = result.Progression,
                HintType = result.Type,
                IsSuccess = result.IsSuccess,
                ErrorMessage = result.Error,
                RequestTimeUtc = DateTimeOffset.UtcNow
            };

            db.AiHintLogs.Add(log);
            await db.SaveChangesAsync(ct);

            if (_aiConfig.Value.LogAiRequests && result.IsSuccess)
            {
                _logger.LogInformation(
                    "AI hint generated for challenge {ChallengeId} by user {UserId}: {Hint}",
                    challenge.Id, userId, result.Hint);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log AI hint request");
        }
    }
}
