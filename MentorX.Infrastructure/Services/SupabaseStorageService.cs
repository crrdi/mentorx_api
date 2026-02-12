using MentorX.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Supabase.Storage;

namespace MentorX.Infrastructure.Services;

public class SupabaseStorageService : IStorageService
{
    private const string MentorAvatarsBucket = "mentor-avatars";

    private readonly SupabaseService _supabaseService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SupabaseStorageService> _logger;

    public SupabaseStorageService(
        SupabaseService supabaseService,
        IConfiguration configuration,
        ILogger<SupabaseStorageService> logger)
    {
        _supabaseService = supabaseService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string?> UploadMentorAvatarAsync(Guid mentorId, byte[] imageBytes, CancellationToken cancellationToken = default)
    {
        try
        {
            var path = $"{mentorId}.png";
            var bucket = _supabaseService.Client.Storage.From(MentorAvatarsBucket);

            var options = new Supabase.Storage.FileOptions
            {
                ContentType = "image/png",
                Upsert = true
            };

            await bucket.Upload(imageBytes, path, options);

            var publicUrl = bucket.GetPublicUrl(path, null);
            _logger.LogInformation("Successfully uploaded mentor avatar for {MentorId} to {Url}", mentorId, publicUrl);
            return publicUrl;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload mentor avatar for {MentorId}", mentorId);
            return null;
        }
    }
}
