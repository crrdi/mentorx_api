using MentorX.Application.DTOs.Requests;

namespace MentorX.Application.Interfaces;

public interface IRevenueCatWebhookService
{
    Task<(bool Success, bool Processed, string? Error)> ProcessWebhookAsync(RevenueCatWebhookRequest request, CancellationToken cancellationToken = default);
}
