using System.Text;

namespace GZCTF.Services.AI;

/// <summary>
/// Builds structured prompts from context data for AI requests.
/// This is the central component that assembles system + user messages
/// from platform data (challenges, submissions, logs, etc.).
/// </summary>
public class ContextAssembler
{
    readonly ILogger<ContextAssembler> _logger;

    public ContextAssembler(ILogger<ContextAssembler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Build a prompt for AI hint generation
    /// </summary>
    /// <param name="challengeTitle">Challenge title</param>
    /// <param name="challengeContent">Challenge description/content</param>
    /// <param name="challengeCategory">Challenge category</param>
    /// <param name="challengeFiles">Challenge source files content (optional)</param>
    /// <param name="playerProgress">Player's progress context (optional)</param>
    /// <param name="existingHints">Existing hints already revealed (optional)</param>
    /// <param name="hintBudget">How many hints were requested so far (0-based)</param>
    /// <returns>A ready-to-send AI request</returns>
    public AiRequest BuildHintRequest(
        string challengeTitle,
        string challengeContent,
        string challengeCategory,
        IReadOnlyList<string>? challengeFiles = null,
        string? playerProgress = null,
        IReadOnlyList<string>? existingHints = null,
        int hintBudget = 0)
    {
        var systemPrompt = BuildHintSystemPrompt();
        var userPrompt = BuildHintUserPrompt(
            challengeTitle, challengeContent, challengeCategory,
            challengeFiles, playerProgress, existingHints, hintBudget);

        return new AiRequest(
            new List<AiMessage>
            {
                new(AiMessageRole.System, systemPrompt),
                new(AiMessageRole.User, userPrompt)
            },
            Temperature: 0.5,
            MaxTokens: 1024
        );
    }

    /// <summary>
    /// Build a prompt for AI writeup grading
    /// </summary>
    public AiRequest BuildGradingRequest(
        string challengeTitle,
        string challengeContent,
        string officialSolution,
        string rubric,
        int maxScore,
        string studentWriteup,
        string? customPrompt = null)
    {
        var systemPrompt = customPrompt ?? BuildGradingSystemPrompt(rubric, maxScore);
        var userPrompt = BuildGradingUserPrompt(
            challengeTitle, challengeContent, officialSolution, studentWriteup);

        return new AiRequest(
            new List<AiMessage>
            {
                new(AiMessageRole.System, systemPrompt),
                new(AiMessageRole.User, userPrompt)
            },
            Temperature: 0.3,
            MaxTokens: 2048
        );
    }

    #region Hint Prompt Builders

    static string BuildHintSystemPrompt() => """
You are an expert CTF challenge hint generator. Your role is to provide helpful,
progressive hints that guide players toward solving challenges without giving away
the full solution.

Rules:
1. Hints should be progressive — start vague, get more specific with each hint
2. Never reveal the flag or exact solution
3. Suggest tools, techniques, or areas to investigate
4. If the player has attempted multiple times, tailor hints to their apparent roadblock
5. Keep hints concise (1-3 sentences)
6. Return ONLY valid JSON in this exact format:
   {
     "hint": "Your hint text here",
     "progression": 1,
     "type": "tooling|technique|concept|direction"
   }
7. "progression" indicates how close this hint gets to the solution (1 = subtle nudge, 5 = nearly direct)
8. "type" categorizes the hint for display
""";

    static string BuildHintUserPrompt(
        string title, string content, string category,
        IReadOnlyList<string>? files, string? progress,
        IReadOnlyList<string>? hints, int budget) 
    {
        var sb = new StringBuilder();
        sb.AppendLine("## Challenge Information");
        sb.AppendLine($"Title: {title}");
        sb.AppendLine($"Category: {category}");
        sb.AppendLine($"Content: {content}");
        sb.AppendLine();

        if (files is { Count: > 0 })
        {
            sb.AppendLine("## Challenge Source Files");
            for (int i = 0; i < files.Count; i++)
            {
                sb.AppendLine($"--- File {i + 1} ---");
                sb.AppendLine(files[i]);
                sb.AppendLine();
            }
        }

        if (!string.IsNullOrEmpty(progress))
        {
            sb.AppendLine("## Player Progress");
            sb.AppendLine(progress);
            sb.AppendLine();
        }

        if (hints is { Count: > 0 })
        {
            sb.AppendLine("## Previously Given Hints");
            for (int i = 0; i < hints.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {hints[i]}");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"This is hint request #{budget + 1}. Generate the next progressive hint.");

        return sb.ToString();
    }

    #endregion

    #region Grading Prompt Builders

    static string BuildGradingSystemPrompt(string rubric, int maxScore) => $"""
You are an expert CTF writeup grader. Evaluate the student's writeup against the
official solution and rubric. Be fair — reward correct methodology even if different
from the official solution. Never penalize valid alternative approaches.

Rubric:
{rubric}

Maximum Score: {maxScore}

Return ONLY valid JSON in this exact format:
{{
  "score": <total score>,
  "breakdown": {{
    "category_name": <score>
  }},
  "feedback": ["point 1", "point 2"],
  "strengths": ["strength 1", "strength 2"],
  "weaknesses": ["weakness 1", "weakness 2"]
}}
""";

    static string BuildGradingUserPrompt(
        string title, string content, string solution, string writeup) => $"""
Challenge Title: {title}
Challenge Content: {content}

Official Solution:
{solution}

Student Writeup:
{writeup}

Evaluate the student's writeup against the official solution using the rubric.
""";

    #endregion

    /// <summary>
    /// Sanitize sensitive data from context before sending to AI
    /// </summary>
    public static string Sanitize(string input)
    {
        // Strip potential flag patterns
        return System.Text.RegularExpressions.Regex.Replace(
            input,
            @"flag\{[^}]+\}|CTF\{[^}]+\}|[A-Za-z0-9+/=]{40,}",
            "[REDACTED]"
        );
    }
}
