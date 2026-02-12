using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MentorX.Application.DTOs.Responses;
using MentorX.Application.Interfaces;

namespace MentorX.Infrastructure.Services;

public class RevenueCatApiService : IRevenueCatApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RevenueCatApiService> _logger;

    public RevenueCatApiService(HttpClient httpClient, IConfiguration configuration, ILogger<RevenueCatApiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        // BaseAddress, X-Platform header, and Authorization header are configured in ServiceCollectionExtensions
        // No need to set them here as HttpClient factory handles it
    }

    public async Task<RevenueCatCustomerInfo?> GetCustomerInfoAsync(string appUserId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[RevenueCat API] Fetching customer info for app_user_id: {AppUserId}", appUserId);

            var response = await _httpClient.GetAsync($"/subscribers/{Uri.EscapeDataString(appUserId)}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("[RevenueCat API] Failed to fetch customer info. Status: {Status}, Response: {Response}", 
                    response.StatusCode, errorContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw new HttpRequestException($"RevenueCat API error: {response.StatusCode} - {errorContent}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("[RevenueCat API] Customer info response: {Response}", jsonContent);

            // RevenueCat API v1 response structure
            // We need to parse the subscriber object and extract purchases
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            if (!root.TryGetProperty("subscriber", out var subscriber))
            {
                _logger.LogWarning("[RevenueCat API] No subscriber object in response");
                return null;
            }

            var customerInfo = new RevenueCatCustomerInfo
            {
                AppUserId = appUserId,
                Purchases = new List<RevenueCatPurchase>()
            };

            // Extract non-subscription purchases from subscriber.non_subscriptions
            if (subscriber.TryGetProperty("non_subscriptions", out var nonSubscriptions))
            {
                foreach (var productId in nonSubscriptions.EnumerateObject())
                {
                    foreach (var purchase in productId.Value.EnumerateArray())
                    {
                        var purchaseObj = new RevenueCatPurchase
                        {
                            ProductId = productId.Name,
                            TransactionId = purchase.TryGetProperty("transaction_id", out var txId) ? txId.GetString() : null,
                            PurchasedAtMs = purchase.TryGetProperty("purchase_date_ms", out var purchaseDate) ? purchaseDate.GetInt64() : null,
                            Store = purchase.TryGetProperty("store", out var store) ? store.GetString() : null
                        };
                        customerInfo.Purchases.Add(purchaseObj);
                    }
                }
            }

            // Also check subscriptions for one-time purchases
            if (subscriber.TryGetProperty("subscriptions", out var subscriptions))
            {
                foreach (var productId in subscriptions.EnumerateObject())
                {
                    var subscription = productId.Value;
                    if (subscription.TryGetProperty("purchase_date_ms", out var purchaseDate))
                    {
                        var purchaseObj = new RevenueCatPurchase
                        {
                            ProductId = productId.Name,
                            TransactionId = subscription.TryGetProperty("original_transaction_id", out var txId) ? txId.GetString() : null,
                            PurchasedAtMs = purchaseDate.GetInt64(),
                            Store = subscription.TryGetProperty("store", out var store) ? store.GetString() : null
                        };
                        customerInfo.Purchases.Add(purchaseObj);
                    }
                }
            }

            _logger.LogInformation("[RevenueCat API] Found {Count} purchases for app_user_id: {AppUserId}", 
                customerInfo.Purchases.Count, appUserId);

            return customerInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RevenueCat API] Error fetching customer info for app_user_id: {AppUserId}", appUserId);
            throw;
        }
    }

    public async Task<(bool Verified, string? ResolvedTransactionId)> VerifyTransactionAsync(string appUserId, string? transactionId, string productId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[RevenueCat API] Verifying transaction. AppUserId: {AppUserId}, TransactionId: {TransactionId}, ProductId: {ProductId}",
                appUserId, transactionId ?? "(null)", productId);

            var customerInfo = await GetCustomerInfoAsync(appUserId, cancellationToken);
            
            if (customerInfo?.Purchases == null)
            {
                _logger.LogWarning("[RevenueCat API] No purchases found for app_user_id: {AppUserId}", appUserId);
                return (false, null);
            }

            // Purchases for this product (order by most recent)
            var productPurchases = customerInfo.Purchases
                .Where(p => p.ProductId == productId && !string.IsNullOrEmpty(p.TransactionId))
                .OrderByDescending(p => p.PurchasedAtMs ?? 0)
                .ToList();

            if (productPurchases.Count == 0)
            {
                _logger.LogWarning("[RevenueCat API] No purchases found for productId: {ProductId}, app_user_id: {AppUserId}", productId, appUserId);
                return (false, null);
            }

            // If transactionId provided, verify it exists and matches product
            if (!string.IsNullOrEmpty(transactionId))
            {
                var matchingPurchase = productPurchases.FirstOrDefault(p => p.TransactionId == transactionId);
                if (matchingPurchase != null)
                {
                    _logger.LogInformation("[RevenueCat API] Transaction verified. TransactionId: {TransactionId}, ProductId: {ProductId}",
                        transactionId, productId);
                    return (true, transactionId);
                }
                _logger.LogWarning("[RevenueCat API] Transaction not found. TransactionId: {TransactionId}, ProductId: {ProductId}", transactionId, productId);
                return (false, null);
            }

            // No transactionId: use latest purchase for this product (client may not have transaction ID)
            var latestPurchase = productPurchases.First();
            var resolvedId = latestPurchase.TransactionId;
            _logger.LogInformation("[RevenueCat API] No transactionId provided. Using latest purchase. ResolvedTransactionId: {ResolvedId}, ProductId: {ProductId}, PurchasedAt: {PurchasedAt}",
                resolvedId, productId, latestPurchase.PurchasedAtMs);
            return (true, resolvedId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RevenueCat API] Error verifying transaction. AppUserId: {AppUserId}, TransactionId: {TransactionId}",
                appUserId, transactionId ?? "(null)");
            return (false, null);
        }
    }
}
