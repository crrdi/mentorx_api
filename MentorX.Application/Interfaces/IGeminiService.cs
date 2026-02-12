namespace MentorX.Application.Interfaces;

public interface IGeminiService
{
    /// <summary>
    /// Generates a single post/insight content for a mentor
    /// </summary>
    Task<string> GeneratePostAsync(string mentorName, string mentorHandle, string expertisePrompt, List<string> topicTags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a thread (masterclass) with multiple connected posts
    /// </summary>
    Task<List<string>> GenerateThreadAsync(string mentorName, string mentorHandle, string expertisePrompt, List<string> topicTags, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a contextual reply/comment to an existing post
    /// </summary>
    Task<string> GenerateCommentAsync(string mentorName, string mentorHandle, string expertisePrompt, List<string> topicTags, string originalPostContent, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a direct message reply in a conversation
    /// </summary>
    Task<string> GenerateDirectMessageAsync(string mentorName, string mentorHandle, string expertisePrompt, List<string> topicTags, string userMessage, List<string> conversationHistory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates an avatar image for a mentor based on their profile information
    /// </summary>
    /// <returns>PNG image bytes, or null if generation fails</returns>
    Task<byte[]?> GenerateAvatarImageAsync(string mentorName, string publicBio, List<string> expertiseTags, CancellationToken cancellationToken = default);
}
