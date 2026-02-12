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
                        var txId = purchase.TryGetProperty("transaction_id", out var t) ? t.GetString()
                            : purchase.TryGetProperty("original_transaction_id", out var o) ? o.GetString()
                            : purchase.TryGetProperty("id", out var i) ? i.GetString()
                            : null;
                        var purchaseObj = new RevenueCatPurchase
                        {
                            ProductId = productId.Name,
                            TransactionId = txId,
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
            
            if (customerInfo?.Purchases == null || customerInfo.Purchases.Count == 0)
            {
                _logger.LogWarning("[RevenueCat API] No purchases found for app_user_id: {AppUserId}", appUserId);
                return (false, null);
            }

            // When client sends transactionId: RevenueCat may list the purchase under package id ($rc_credits_100) not store product id (com.xxx.credits_100).
            // So find by transactionId across ALL purchases first; package lookup in backend uses request.ProductId.
            if (!string.IsNullOrEmpty(transactionId))
            {
                var byTransactionId = customerInfo.Purchases.FirstOrDefault(p => p.TransactionId == transactionId);
                if (byTransactionId != null)
                {
                    _logger.LogInformation("[RevenueCat API] Transaction verified by transactionId. TransactionId: {TransactionId}, RevenueCatProductId: {RcProductId}, RequestProductId: {ProductId}",
                        transactionId, byTransactionId.ProductId, productId);
                    return (true, transactionId);
                }
            }

            // No transactionId or not found by id: match by productId (RevenueCat may use store product id or package id like $rc_credits_100)
            var productPurchases = customerInfo.Purchases
                .Where(p => !string.IsNullOrEmpty(p.TransactionId) && p.ProductId == productId)
                .OrderByDescending(p => p.PurchasedAtMs ?? 0)
                .ToList();

            // If no exact productId match, RevenueCat might use package key e.g. $rc_credits_100; try matching by productId containing the store id suffix
            if (productPurchases.Count == 0 && productId.Contains("."))
            {
                var suffix = productId.Split('.').LastOrDefault(); // e.g. credits_100
                productPurchases = customerInfo.Purchases
                    .Where(p => !string.IsNullOrEmpty(p.TransactionId) && (p.ProductId == productId || p.ProductId?.EndsWith(suffix ?? "", StringComparison.OrdinalIgnoreCase) == true))
                    .OrderByDescending(p => p.PurchasedAtMs ?? 0)
                    .ToList();
            }

            if (productPurchases.Count == 0)
            {
                var seenIds = string.Join(", ", customerInfo.Purchases.Select(p => p.ProductId ?? "(null)"));
                _logger.LogWarning("[RevenueCat API] No purchases found for productId: {ProductId}, app_user_id: {AppUserId}. RevenueCat purchase keys: {Keys}", productId, appUserId, seenIds);
                return (false, null);
            }

            if (!string.IsNullOrEmpty(transactionId))
            {
                var matching = productPurchases.FirstOrDefault(p => p.TransactionId == transactionId);
                if (matching != null)
                {
                    _logger.LogInformation("[RevenueCat API] Transaction verified. TransactionId: {TransactionId}, ProductId: {ProductId}", transactionId, productId);
                    return (true, transactionId);
                }
                _logger.LogWarning("[RevenueCat API] Transaction not found. TransactionId: {TransactionId}, ProductId: {ProductId}", transactionId, productId);
                return (false, null);
            }

            // No transactionId: use latest purchase for this product
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
