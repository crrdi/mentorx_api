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
            var hasAuth = _httpClient.DefaultRequestHeaders.Authorization != null;
            _logger.LogInformation("[RevenueCat API] Fetching customer info for app_user_id: {AppUserId}, HasAuthHeader: {HasAuth}, BaseAddress: {BaseAddress}",
                appUserId, hasAuth, _httpClient.BaseAddress);

            var response = await _httpClient.GetAsync($"/subscribers/{Uri.EscapeDataString(appUserId)}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("[RevenueCat API] Failed to fetch customer info. Status: {Status}, HasAuth: {HasAuth}, Response: {Response}", 
                    response.StatusCode, hasAuth, errorContent);
                
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }

                throw new HttpRequestException($"RevenueCat API {response.StatusCode} (hasAuth={hasAuth}): {errorContent}");
            }

            var jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("[RevenueCat API] Customer info response length: {Length}", jsonContent?.Length ?? 0);

            // RevenueCat API v1: GET /subscribers returns { subscriber } or sometimes { value: { subscriber } }
            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;
            if (!root.TryGetProperty("subscriber", out var subscriber))
            {
                if (root.TryGetProperty("value", out var value) && value.TryGetProperty("subscriber", out subscriber))
                { /* use subscriber from value */ }
                else
                {
                    _logger.LogWarning("[RevenueCat API] No subscriber object in response. Root keys: {Keys}", string.Join(", ", root.EnumerateObject().Select(p => p.Name)));
                    return null;
                }
            }

            var customerInfo = new RevenueCatCustomerInfo
            {
                AppUserId = appUserId,
                Purchases = new List<RevenueCatPurchase>()
            };

            // Extract non-subscription purchases. RevenueCat v1 uses "id" (not transaction_id) in non_subscriptions items.
            void AddPurchasesFromObject(JsonElement obj, string productKey)
            {
                if (obj.ValueKind != JsonValueKind.Array) return;
                foreach (var purchase in obj.EnumerateArray())
                {
                    var rcId = purchase.TryGetProperty("id", out var i) ? i.GetString() : null;
                    var storeTxId = purchase.TryGetProperty("store_transaction_id", out var st) ? st.GetString() : null;
                    var txId = rcId ?? storeTxId
                        ?? (purchase.TryGetProperty("transaction_id", out var t) ? t.GetString() : null)
                        ?? (purchase.TryGetProperty("original_transaction_id", out var o) ? o.GetString() : null);
                    long? purchasedAtMs = null;
                    if (purchase.TryGetProperty("purchase_date_ms", out var pdm))
                        purchasedAtMs = pdm.GetInt64();
                    else if (purchase.TryGetProperty("purchase_date", out var pd))
                    {
                        if (pd.ValueKind == JsonValueKind.String && DateTime.TryParse(pd.GetString(), out var dt))
                            purchasedAtMs = new DateTimeOffset(dt).ToUnixTimeMilliseconds();
                    }
                    customerInfo.Purchases.Add(new RevenueCatPurchase
                    {
                        ProductId = productKey,
                        TransactionId = txId,
                        StoreTransactionId = storeTxId,
                        PurchasedAtMs = purchasedAtMs,
                        Store = purchase.TryGetProperty("store", out var store) ? store.GetString() : null
                    });
                }
            }
            if (subscriber.TryGetProperty("non_subscriptions", out var nonSubscriptions))
            {
                foreach (var product in nonSubscriptions.EnumerateObject())
                    AddPurchasesFromObject(product.Value, product.Name);
            }
            if (subscriber.TryGetProperty("other_purchases", out var otherPurchases))
            {
                foreach (var product in otherPurchases.EnumerateObject())
                    AddPurchasesFromObject(product.Value, product.Name);
            }

            // Subscriptions: each key is product id, value is object with store_transaction_id / original_transaction_id
            if (subscriber.TryGetProperty("subscriptions", out var subscriptions))
            {
                foreach (var productId in subscriptions.EnumerateObject())
                {
                    var subscription = productId.Value;
                    if (!subscription.TryGetProperty("purchase_date_ms", out var purchaseDate)) continue;
                    var txId = subscription.TryGetProperty("store_transaction_id", out var st) ? st.GetString()
                        : subscription.TryGetProperty("original_transaction_id", out var ot) ? ot.GetString() : null;
                    customerInfo.Purchases.Add(new RevenueCatPurchase
                    {
                        ProductId = productId.Name,
                        TransactionId = txId,
                        PurchasedAtMs = purchaseDate.GetInt64(),
                        Store = subscription.TryGetProperty("store", out var store) ? store.GetString() : null
                    });
                }
            }

            _logger.LogInformation("[RevenueCat API] Found {Count} purchases for app_user_id: {AppUserId}. Sample: {Sample}",
                customerInfo.Purchases.Count, appUserId,
                customerInfo.Purchases.Count == 0 ? "(none)" : string.Join("; ", customerInfo.Purchases.Take(5).Select(p => $"{p.ProductId} id={p.TransactionId} storeTx={p.StoreTransactionId}")));

            return customerInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RevenueCat API] Error fetching customer info for app_user_id: {AppUserId}", appUserId);
            throw;
        }
    }

    public async Task<(bool Verified, string? ResolvedTransactionId, string? VerifiedProductId)> VerifyTransactionAsync(string appUserId, string? transactionId, string productId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("[RevenueCat API] Verifying transaction. AppUserId: {AppUserId}, TransactionId: {TransactionId}, ProductId: {ProductId}",
                appUserId, transactionId ?? "(null)", productId);

            var customerInfo = await GetCustomerInfoAsync(appUserId, cancellationToken);
            
            if (customerInfo?.Purchases == null || customerInfo.Purchases.Count == 0)
            {
                _logger.LogWarning("[RevenueCat API] No purchases found for app_user_id: {AppUserId}", appUserId);
                return (false, null, null);
            }

            // When client sends transactionId: find by transactionId across ALL purchases (match RevenueCat id or store_transaction_id).
            if (!string.IsNullOrEmpty(transactionId))
            {
                var byTransactionId = customerInfo.Purchases.FirstOrDefault(p =>
                    p.TransactionId == transactionId || p.StoreTransactionId == transactionId);
                if (byTransactionId != null)
                {
                    _logger.LogInformation("[RevenueCat API] Transaction verified by transactionId. TransactionId: {TransactionId}, RevenueCatProductId: {RcProductId}, RequestProductId: {ProductId}",
                        transactionId, byTransactionId.ProductId, productId);
                    return (true, transactionId, byTransactionId.ProductId);
                }
            }

            // No transactionId or not found: match by productId (exact or suffix e.g. credits_100)
            var productPurchases = customerInfo.Purchases
                .Where(p => !string.IsNullOrEmpty(p.TransactionId) && p.ProductId == productId)
                .OrderByDescending(p => p.PurchasedAtMs ?? 0)
                .ToList();
            if (productPurchases.Count == 0 && productId.Contains("."))
            {
                var suffix = productId.Split('.').LastOrDefault();
                productPurchases = customerInfo.Purchases
                    .Where(p => !string.IsNullOrEmpty(p.TransactionId) && (p.ProductId == productId || (p.ProductId?.EndsWith(suffix ?? "", StringComparison.OrdinalIgnoreCase) == true)))
                    .OrderByDescending(p => p.PurchasedAtMs ?? 0)
                    .ToList();
            }

            if (productPurchases.Count == 0)
            {
                var seenIds = string.Join(", ", customerInfo.Purchases.Select(p => p.ProductId ?? "(null)"));
                _logger.LogWarning("[RevenueCat API] No purchases found for productId: {ProductId}, app_user_id: {AppUserId}. RevenueCat purchase keys: {Keys}", productId, appUserId, seenIds);
                return (false, null, null);
            }

            if (!string.IsNullOrEmpty(transactionId))
            {
                var matching = productPurchases.FirstOrDefault(p => p.TransactionId == transactionId);
                if (matching != null)
                {
                    _logger.LogInformation("[RevenueCat API] Transaction verified. TransactionId: {TransactionId}, ProductId: {ProductId}", transactionId, productId);
                    return (true, transactionId, matching.ProductId);
                }
                _logger.LogWarning("[RevenueCat API] Transaction not found. TransactionId: {TransactionId}, ProductId: {ProductId}", transactionId, productId);
                return (false, null, null);
            }

            var latestPurchase = productPurchases.First();
            var resolvedId = latestPurchase.TransactionId;
            _logger.LogInformation("[RevenueCat API] No transactionId provided. Using latest purchase. ResolvedTransactionId: {ResolvedId}, ProductId: {ProductId}", resolvedId, productId);
            return (true, resolvedId, latestPurchase.ProductId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[RevenueCat API] Error verifying transaction. AppUserId: {AppUserId}, TransactionId: {TransactionId}",
                appUserId, transactionId ?? "(null)");
            return (false, null, null);
        }
    }
}
