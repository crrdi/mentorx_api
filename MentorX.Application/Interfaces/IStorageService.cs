namespace MentorX.Application.Interfaces;

public interface IStorageService
{
    /// <summary>
    /// Uploads a mentor avatar image to storage and returns the public URL.
    /// </summary>
    /// <param name="mentorId">The mentor ID (used as filename)</param>
    /// <param name="imageBytes">PNG image bytes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Public URL of the uploaded avatar, or null on failure</returns>
    Task<string?> UploadMentorAvatarAsync(Guid mentorId, byte[] imageBytes, CancellationToken cancellationToken = default);
}
