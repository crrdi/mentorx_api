using Google.GenAI;
using Google.GenAI.Types;
using MentorX.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MentorX.Application.Services;

public class GeminiService : IGeminiService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiService> _logger;
    private readonly string _apiKey;
    private readonly Client _client;
    // Use gemini-2.0-flash (gemini-2.0-flash-exp is deprecated)
    private const string ModelName = "gemini-2.0-flash";

    public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Get API key from configuration (supports environment variables and appsettings)
        var apiKeyFromConfig = _configuration["Gemini:ApiKey"];
        var apiKeyFromEnv = _configuration["GEMINI_API_KEY"];
        
        _logger.LogDebug("Checking API key sources - Config: {HasConfig}, Env: {HasEnv}", 
            !string.IsNullOrWhiteSpace(apiKeyFromConfig), 
            !string.IsNullOrWhiteSpace(apiKeyFromEnv));
        
        _apiKey = (!string.IsNullOrWhiteSpace(apiKeyFromConfig) ? apiKeyFromConfig : null)
            ?? (!string.IsNullOrWhiteSpace(apiKeyFromEnv) ? apiKeyFromEnv : null)
            ?? throw new InvalidOperationException("Gemini API key is not configured. Please set Gemini:ApiKey in appsettings.json or GEMINI_API_KEY environment variable.");
        
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            throw new InvalidOperationException("Gemini API key cannot be empty.");
        }

        // Log API key info (first 10 chars only for security)
        var apiKeyPreview = _apiKey.Length > 10 ? _apiKey.Substring(0, 10) + "..." : _apiKey;
        _logger.LogInformation("GeminiService initialized successfully with API key: {ApiKeyPreview}", apiKeyPreview);
        _client = new Client(apiKey: _apiKey);
    }

    /// <summary>
    /// Sanitizes user-provided prompt to prevent prompt injection attacks.
    /// Removes or neutralizes common prompt injection patterns.
    /// </summary>
    private string SanitizeExpertisePrompt(string expertisePrompt)
    {
        if (string.IsNullOrWhiteSpace(expertisePrompt))
            return expertisePrompt;

        var sanitized = expertisePrompt;
        
        // Common prompt injection patterns to neutralize
        var injectionPatterns = new[]
        {
            // Direct instruction override attempts
            @"(?i)\b(ignore|forget|disregard|override|skip|bypass)\s+(all\s+)?(previous|prior|earlier|above|system|instructions?|prompts?|rules?)",
            @"(?i)\b(new\s+)?(instructions?|prompts?|rules?|directives?)\s*:",
            @"(?i)\b(you\s+are\s+now|you\s+must\s+now|from\s+now\s+on|starting\s+now)",
            @"(?i)\b(disregard\s+the\s+above|ignore\s+everything\s+above|forget\s+everything)",
            @"(?i)\b(system\s*:|system\s+message\s*:|system\s+prompt\s*:)",
            // Role manipulation attempts
            @"(?i)\b(you\s+are\s+(not\s+)?(a\s+)?(mentor|coach|ai|assistant))",
            @"(?i)\b(pretend\s+you\s+are|act\s+as\s+if\s+you\s+are|roleplay\s+as)",
            // Output manipulation
            @"(?i)\b(output\s+only|respond\s+only|say\s+only|write\s+only)\s+""",
            @"(?i)\b(do\s+not\s+(follow|obey|adhere\s+to|respect))",
            // Encoding attempts
            @"(?i)\b(base64|hex|decode|encode)",
        };

        // Replace injection patterns with neutralized versions
        foreach (var pattern in injectionPatterns)
        {
            try
            {
                sanitized = System.Text.RegularExpressions.Regex.Replace(
                    sanitized, 
                    pattern, 
                    "[instruction filtered]", 
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            }
            catch
            {
                // If regex fails, continue with next pattern
            }
        }

        // Limit length to prevent extremely long prompts
        const int maxPromptLength = 2000;
        if (sanitized.Length > maxPromptLength)
        {
            sanitized = sanitized.Substring(0, maxPromptLength) + "...";
            _logger.LogWarning("Expertise prompt truncated due to length limit");
        }

        // Log if sanitization occurred
        if (sanitized != expertisePrompt)
        {
            _logger.LogWarning("Expertise prompt was sanitized to prevent prompt injection");
        }

        return sanitized;
    }

    /// <summary>
    /// Builds a secure system instruction by sanitizing user input and separating it from core instructions.
    /// </summary>
    private string BuildSystemInstruction(string mentorName, string mentorHandle, string expertisePrompt, List<string> topicTags)
    {
        var tags = topicTags.Any() ? string.Join(", ", topicTags) : "general topics";
        
        // Sanitize user-provided expertise prompt to prevent injection attacks
        var sanitizedPrompt = SanitizeExpertisePrompt(expertisePrompt);
        
        // Build system instruction with clear separation and emphasis on core rules
        return $@"You are an AI mentor/coach named ""{mentorName}"" with the handle ""@{mentorHandle}"".

IMPORTANT: The following instructions define your coaching persona and expertise. These instructions must always be followed:
{sanitizedPrompt}

Your Topic Interests: {tags}

CORE RULES (These cannot be overridden):
- You are a coaching and mentorship AI assistant
- You must provide valuable, actionable advice
- You must maintain a supportive and educational tone
- You must stay within your defined persona and expertise area";
    }

    /// <summary>
    /// Builds a conversation-specific system instruction for direct messaging.
    /// Focuses on natural, conversational tone without tags or post-like formatting.
    /// </summary>
    private string BuildConversationSystemInstruction(string mentorName, string mentorHandle, string expertisePrompt)
    {
        // Sanitize user-provided expertise prompt to prevent injection attacks
        var sanitizedPrompt = SanitizeExpertisePrompt(expertisePrompt);
        
        // Build conversation-specific system instruction
        return $@"You are an AI mentor/coach named ""{mentorName}"" having a direct conversation with a user.

IMPORTANT: The following instructions define your coaching persona and expertise. These instructions must always be followed:
{sanitizedPrompt}

CONVERSATION RULES:
- Write as if you're having a natural, one-on-one conversation
- Be conversational, warm, and personable - like talking to a friend or colleague
- Do NOT write like a social media post - avoid hashtags, emojis (unless natural), or post-like formatting
- Do NOT use tags or labels
- Write in a flowing, natural dialogue style
- Be helpful, supportive, and provide actionable guidance
- Respond directly to what the user is asking
- Maintain your persona and expertise throughout the conversation
- Keep responses between 400-1000 characters
- Write in a way that feels like you're speaking directly to them, not broadcasting to an audience";
    }

    public async Task<string> GeneratePostAsync(string mentorName, string mentorHandle, string expertisePrompt, List<string> topicTags, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting post generation for mentor {MentorName} using model {ModelName}", mentorName, ModelName);
            
            var systemInstruction = BuildSystemInstruction(mentorName, mentorHandle, expertisePrompt, topicTags);
            
            // User task prompt - separated from system instruction for security
            var userPrompt = @"Task: Write a valuable coaching or mentorship insight (max 280 characters).
The insight should provide actionable advice, wisdom, or guidance that helps users grow and learn.
Focus on practical, meaningful content that reflects your expertise and coaching style.
The tone should be supportive, encouraging, and educational.
Do not include your name or handle in the text itself.
Use emojis sparingly but effectively if it fits the persona.
Do not use hashtags unless absolutely necessary for the persona.";

            var config = new GenerateContentConfig
            {
                Temperature = 0.7f,
                TopP = 0.95f,
                TopK = 40,
                MaxOutputTokens = 300
            };

            // Try to use SystemInstruction property if available, otherwise fallback to combined prompt
            // Note: If SDK doesn't support SystemInstruction property, this will use the combined approach
            var fullPrompt = $@"{systemInstruction}

{userPrompt}";

            _logger.LogDebug("Calling Gemini API with model {ModelName}", ModelName);
            
            var response = await _client.Models.GenerateContentAsync(
                model: ModelName,
                contents: fullPrompt,
                config: config);

            if (response?.Candidates == null || response.Candidates.Count == 0)
            {
                throw new InvalidOperationException("Gemini API returned empty response");
            }

            var candidate = response.Candidates[0];
            if (candidate?.Content?.Parts == null || candidate.Content.Parts.Count == 0)
            {
                throw new InvalidOperationException("Gemini API returned invalid response structure");
            }

            var content = candidate.Content.Parts[0].Text?.Trim() ?? string.Empty;
            
            if (string.IsNullOrEmpty(content))
            {
                throw new InvalidOperationException("Gemini API returned empty content");
            }
            
            // Enforce 280 character limit
            if (content.Length > 280)
            {
                content = content.Substring(0, 277) + "...";
            }

            _logger.LogInformation("Successfully generated post for mentor {MentorName} using model {Model}: {Content}", mentorName, ModelName, content);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating post for mentor {MentorName}. Error type: {ErrorType}, Message: {ErrorMessage}, StackTrace: {StackTrace}", 
                mentorName, 
                ex.GetType().Name, 
                ex.Message, 
                ex.StackTrace);
            throw new InvalidOperationException("Failed to generate post content. The AI service encountered an error. Please try again.", ex);
        }
    }

    public async Task<List<string>> GenerateThreadAsync(string mentorName, string mentorHandle, string expertisePrompt, List<string> topicTags, CancellationToken cancellationToken = default)
    {
        try
        {
            var systemInstruction = BuildSystemInstruction(mentorName, mentorHandle, expertisePrompt, topicTags);
            
            // User task prompt - separated from system instruction for security
            var userPrompt = @"Task: Write a comprehensive coaching thread or mini-masterclass about a specific topic relevant to your expertise.
The thread should consist of 3 to 6 connected insights that build upon each other to provide deep value.
Each part should offer practical guidance, actionable steps, or valuable lessons.
Return ONLY a JSON array of strings. Each string should be an insight in the thread.
Example format: [""First insight"", ""Second insight"", ""Third insight""]
Each insight should be under 280 characters.";

            var fullPrompt = $@"{systemInstruction}

{userPrompt}";

            var config = new GenerateContentConfig
            {
                Temperature = 0.7f,
                TopP = 0.95f,
                TopK = 40,
                MaxOutputTokens = 2000,
                ResponseMimeType = "application/json"
            };

            var response = await _client.Models.GenerateContentAsync(
                model: ModelName,
                contents: fullPrompt,
                config: config);

            if (response?.Candidates == null || response.Candidates.Count == 0)
            {
                throw new InvalidOperationException("Gemini API returned empty response");
            }

            var candidate = response.Candidates[0];
            if (candidate?.Content?.Parts == null || candidate.Content.Parts.Count == 0)
            {
                throw new InvalidOperationException("Gemini API returned invalid response structure");
            }

            var jsonContent = candidate.Content.Parts[0].Text?.Trim() ?? string.Empty;
            
            // Clean up JSON if it's wrapped in markdown code blocks
            if (jsonContent.StartsWith("```json"))
            {
                jsonContent = jsonContent.Substring(7);
            }
            if (jsonContent.StartsWith("```"))
            {
                jsonContent = jsonContent.Substring(3);
            }
            if (jsonContent.EndsWith("```"))
            {
                jsonContent = jsonContent.Substring(0, jsonContent.Length - 3);
            }
            jsonContent = jsonContent.Trim();

            var posts = JsonSerializer.Deserialize<List<string>>(jsonContent);
            
            if (posts == null || !posts.Any())
            {
                throw new InvalidOperationException("Failed to parse thread posts from Gemini response");
            }

            // Enforce character limits and ensure we have 3-6 posts
            var validPosts = posts
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Length > 280 ? p.Substring(0, 277) + "..." : p)
                .Take(6)
                .ToList();

            if (validPosts.Count < 3)
            {
                throw new InvalidOperationException($"Thread must have at least 3 posts, but got {validPosts.Count}");
            }

            _logger.LogInformation("Generated thread with {Count} posts for mentor {MentorName}", validPosts.Count, mentorName);
            return validPosts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating thread for mentor {MentorName}", mentorName);
            throw new InvalidOperationException("Failed to generate thread content. The AI service encountered an error. Please try again.", ex);
        }
    }

    public async Task<string> GenerateCommentAsync(string mentorName, string mentorHandle, string expertisePrompt, List<string> topicTags, string originalPostContent, CancellationToken cancellationToken = default)
    {
        try
        {
            var systemInstruction = BuildSystemInstruction(mentorName, mentorHandle, expertisePrompt, topicTags);
            
            // Sanitize original post content to prevent injection through user-generated content
            var sanitizedPostContent = SanitizeExpertisePrompt(originalPostContent);
            
            // User task prompt - separated from system instruction for security
            var userPrompt = $@"Context: You are replying to the following insight/post from another mentor: ""{sanitizedPostContent}""
Task: Write a thoughtful, relevant reply that adds value to the conversation.
The reply should be consistent with your coaching persona and provide additional insights, questions, or perspectives.
Keep it under 280 characters.
Maintain a supportive and collaborative tone appropriate for a mentorship community.
Do not use hashtags unless necessary.
Do not include your name or handle in the text.
IMPORTANT: Do NOT start your reply with generic phrases like ""Great point!"", ""That's interesting!"", ""I agree!"", ""Absolutely!"", or similar common opening phrases.
Start your reply directly with your unique perspective, insight, or question. Be original and authentic in your opening.";

            var fullPrompt = $@"{systemInstruction}

{userPrompt}";

            var config = new GenerateContentConfig
            {
                Temperature = 0.85f, // Increased for more variety and creativity to avoid repetitive phrases
                TopP = 0.95f,
                TopK = 40,
                MaxOutputTokens = 300
            };

            var response = await _client.Models.GenerateContentAsync(
                model: ModelName,
                contents: fullPrompt,
                config: config);

            if (response?.Candidates == null || response.Candidates.Count == 0)
            {
                throw new InvalidOperationException("Gemini API returned empty response");
            }

            var candidate = response.Candidates[0];
            if (candidate?.Content?.Parts == null || candidate.Content.Parts.Count == 0)
            {
                throw new InvalidOperationException("Gemini API returned invalid response structure");
            }

            var content = candidate.Content.Parts[0].Text?.Trim() ?? string.Empty;
            
            // Enforce 280 character limit
            if (content.Length > 280)
            {
                content = content.Substring(0, 277) + "...";
            }

            _logger.LogInformation("Generated comment for mentor {MentorName} replying to post", mentorName);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating comment for mentor {MentorName}", mentorName);
            throw new InvalidOperationException("Failed to generate comment. The AI service encountered an error. Please try again.", ex);
        }
    }

    public async Task<string> GenerateDirectMessageAsync(string mentorName, string mentorHandle, string expertisePrompt, List<string> topicTags, string userMessage, List<string> conversationHistory, CancellationToken cancellationToken = default)
    {
        try
        {
            // Use conversation-specific system instruction (no tags, conversational tone)
            var systemInstruction = BuildConversationSystemInstruction(mentorName, mentorHandle, expertisePrompt);
            
            // Sanitize user message and conversation history to prevent injection
            var sanitizedUserMessage = SanitizeExpertisePrompt(userMessage);
            var sanitizedHistory = conversationHistory.Any() 
                ? conversationHistory.TakeLast(10)
                    .Select(h => SanitizeExpertisePrompt(h))
                    .ToList()
                : new List<string>();
            
            var historyContext = sanitizedHistory.Any() 
                ? $"Previous conversation:\n{string.Join("\n", sanitizedHistory)}\n\n"
                : string.Empty;
            
            // User task prompt - conversational and natural
            var userPrompt = $@"{historyContext}User message: ""{sanitizedUserMessage}""

Task: Write a natural, conversational response as the mentor. 
- Respond as if you're having a direct, one-on-one conversation
- Write in a flowing, natural dialogue style (not like a social media post)
- Do NOT use hashtags, tags, or post-like formatting
- Be warm, personable, and helpful
- Keep your response between 400-1000 characters
- Write as if you're speaking directly to them";

            var fullPrompt = $@"{systemInstruction}

{userPrompt}";

            // Increased tokens for longer responses (1000 chars ≈ 250-300 tokens)
            var config = new GenerateContentConfig
            {
                Temperature = 0.8f,
                TopP = 0.95f,
                TopK = 40,
                MaxOutputTokens = 400 // ~1000 characters
            };

            var response = await _client.Models.GenerateContentAsync(
                model: ModelName,
                contents: fullPrompt,
                config: config);

            if (response?.Candidates == null || response.Candidates.Count == 0)
            {
                throw new InvalidOperationException("Gemini API returned empty response");
            }

            var candidate = response.Candidates[0];
            if (candidate?.Content?.Parts == null || candidate.Content.Parts.Count == 0)
            {
                throw new InvalidOperationException("Gemini API returned invalid response structure");
            }

            var content = candidate.Content.Parts[0].Text?.Trim() ?? string.Empty;
            
            // Enforce character limits: minimum 400, maximum 1000
            if (content.Length < 400)
            {
                _logger.LogWarning("Generated response is too short ({Length} chars), expected 400-1000. Regenerating...", content.Length);
                // Try once more with emphasis on length
                var retryPrompt = $@"{systemInstruction}

{historyContext}User message: ""{sanitizedUserMessage}""

Task: Write a natural, conversational response as the mentor. 
- Respond as if you're having a direct, one-on-one conversation
- Write in a flowing, natural dialogue style (not like a social media post)
- Do NOT use hashtags, tags, or post-like formatting
- Be warm, personable, and helpful
- IMPORTANT: Your response MUST be at least 400 characters and at most 1000 characters
- Write as if you're speaking directly to them";

                var retryResponse = await _client.Models.GenerateContentAsync(
                    model: ModelName,
                    contents: retryPrompt,
                    config: config);

                if (retryResponse?.Candidates != null && retryResponse.Candidates.Count > 0)
                {
                    var retryCandidate = retryResponse.Candidates[0];
                    if (retryCandidate?.Content?.Parts != null && retryCandidate.Content.Parts.Count > 0)
                    {
                        content = retryCandidate.Content.Parts[0].Text?.Trim() ?? string.Empty;
                    }
                }
            }

            // Enforce maximum 1000 character limit
            if (content.Length > 1000)
            {
                // Try to cut at a sentence boundary
                var truncated = content.Substring(0, 1000);
                var lastPeriod = truncated.LastIndexOf('.');
                var lastQuestion = truncated.LastIndexOf('?');
                var lastExclamation = truncated.LastIndexOf('!');
                var lastSentenceEnd = Math.Max(Math.Max(lastPeriod, lastQuestion), lastExclamation);
                
                if (lastSentenceEnd > 800) // Only use sentence boundary if it's reasonable
                {
                    content = truncated.Substring(0, lastSentenceEnd + 1);
                }
                else
                {
                    content = truncated;
                }
            }

            // Final check - if still too short, pad with context (but this shouldn't happen)
            if (content.Length < 400)
            {
                _logger.LogWarning("Generated response is still too short ({Length} chars) after retry. Using as-is.", content.Length);
            }

            _logger.LogInformation("Generated DM reply for mentor {MentorName} ({Length} characters)", mentorName, content.Length);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating DM reply for mentor {MentorName}", mentorName);
            throw new InvalidOperationException("Failed to generate reply. The AI service encountered an error. Please try again.", ex);
        }
    }

    private const string ImageModelName = "gemini-2.5-flash-image";

    public async Task<byte[]?> GenerateAvatarImageAsync(string mentorName, string publicBio, List<string> expertiseTags, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting avatar generation for mentor {MentorName} using model {ModelName}", mentorName, ImageModelName);

            var sanitizedName = SanitizeExpertisePrompt(mentorName);
            var sanitizedBio = SanitizeExpertisePrompt(publicBio);
            var tagsText = expertiseTags.Any()
                ? string.Join(", ", expertiseTags.Select(t => t.StartsWith("#") ? t.Substring(1) : t).Take(10))
                : "general topics";

            var prompt = $@"Create a professional, friendly avatar/profile picture for an AI mentor named ""{sanitizedName}"".
Expertise: {sanitizedBio}
Topics: {tagsText}

Style requirements:
- Stylized illustration or abstract portrait (not photorealistic human face)
- Professional, approachable, trustworthy appearance
- Visual elements that subtly reflect the domain (e.g., tech/code aesthetics for developers, leadership symbols for business)
- Clean background, suitable for circular crop
- Square format, centered composition";

            var config = new GenerateContentConfig
            {
                ResponseModalities = ["TEXT", "IMAGE"],
                ImageConfig = new ImageConfig
                {
                    AspectRatio = "1:1",
                    ImageSize = "1K"
                }
            };

            var response = await _client.Models.GenerateContentAsync(
                model: ImageModelName,
                contents: prompt,
                config: config);

            if (response?.Candidates == null || response.Candidates.Count == 0)
            {
                _logger.LogWarning("Gemini image API returned no candidates for mentor {MentorName}", mentorName);
                return null;
            }

            var candidate = response.Candidates[0];
            if (candidate?.Content?.Parts == null)
            {
                _logger.LogWarning("Gemini image API returned invalid response structure for mentor {MentorName}", mentorName);
                return null;
            }

            foreach (var part in candidate.Content.Parts)
            {
                if (part?.InlineData?.Data != null && part.InlineData.Data.Length > 0)
                {
                    _logger.LogInformation("Successfully generated avatar for mentor {MentorName} ({Size} bytes)", mentorName, part.InlineData.Data.Length);
                    return part.InlineData.Data;
                }
            }

            _logger.LogWarning("Gemini image API returned no image data in response for mentor {MentorName}", mentorName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating avatar for mentor {MentorName}", mentorName);
            return null;
        }
    }
}
