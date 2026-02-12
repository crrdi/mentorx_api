using AutoMapper;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;
using MentorX.Application.Interfaces;
using MentorX.Domain.Interfaces;
using MentorX.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace MentorX.Application.Services;

public class CreditService : ICreditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IRevenueCatApiService _revenueCatApiService;
    private readonly ILogger<CreditService> _logger;

    public CreditService(IUnitOfWork unitOfWork, IMapper mapper, IRevenueCatApiService revenueCatApiService, ILogger<CreditService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _revenueCatApiService = revenueCatApiService;
        _logger = logger;
    }

    public async Task<List<CreditPackageResponse>> GetPackagesAsync()
    {
        var packages = await _unitOfWork.CreditPackages.GetAllAsync();
        return _mapper.Map<List<CreditPackageResponse>>(packages);
    }

    public async Task<int> GetBalanceAsync(Guid userId)
    {
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        return user.Credits;
    }

    public async Task<PurchaseCreditsResponse> PurchaseCreditsAsync(Guid userId, PurchaseCreditsRequest request)
    {
        if (!Guid.TryParse(request.PackageId, out var packageId))
        {
            throw new KeyNotFoundException("Invalid package ID");
        }

        var package = await _unitOfWork.CreditPackages.GetByIdAsync(packageId);
        if (package == null)
        {
            throw new KeyNotFoundException("Package not found");
        }

        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found");
        }

        // Calculate credits with bonus
        var creditsToAdd = package.Credits;
        if (package.BonusPercentage.HasValue)
        {
            creditsToAdd = (int)(package.Credits * (1 + package.BonusPercentage.Value / 100.0));
        }

        user.Credits += creditsToAdd;
        await _unitOfWork.Users.UpdateAsync(user);

        // Create transaction record
        var transaction = new Domain.Entities.CreditTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = CreditTransactionType.Purchase,
            Amount = creditsToAdd,
            BalanceAfter = user.Credits,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _unitOfWork.CreditTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        return new PurchaseCreditsResponse
        {
            Success = true,
            CreditsAdded = creditsToAdd,
            NewBalance = user.Credits,
            User = _mapper.Map<UserResponse>(user)
        };
    }

    public async Task<VerifyRevenueCatPurchaseResponse> PurchaseCreditsFromRevenueCatAsync(Guid userId, VerifyRevenueCatPurchaseRequest request)
    {
        _logger.LogInformation("[CreditService] PurchaseCreditsFromRevenueCatAsync called. UserId: {UserId}, TransactionId: {TransactionId}, ProductId: {ProductId}",
            userId, request.TransactionId, request.ProductId);

        // Get user
        var user = await _unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            _logger.LogWarning("[CreditService] User not found: {UserId}", userId);
            return new VerifyRevenueCatPurchaseResponse
            {
                Success = false,
                Verified = false,
                Error = "User not found"
            };
        }

        // Idempotency check: Check if transaction already processed
        var existingTransaction = (await _unitOfWork.CreditTransactions.FindAsync(
            t => t.TransactionId == request.TransactionId && t.UserId == userId)).FirstOrDefault();

        if (existingTransaction != null)
        {
            _logger.LogInformation("[CreditService] Transaction already processed. TransactionId: {TransactionId}, UserId: {UserId}, CreditsAdded: {Credits}",
                request.TransactionId, userId, existingTransaction.Amount);
            return new VerifyRevenueCatPurchaseResponse
            {
                Success = true,
                Verified = true,
                CreditsAdded = existingTransaction.Amount,
                NewBalance = user.Credits,
                Error = "Transaction already processed"
            };
        }

        // Verify transaction with RevenueCat API
        var appUserId = user.Id.ToString(); // Use User.Id as app_user_id
        var isVerified = await _revenueCatApiService.VerifyTransactionAsync(
            appUserId, 
            request.TransactionId, 
            request.ProductId);

        if (!isVerified)
        {
            _logger.LogWarning("[CreditService] Transaction verification failed. TransactionId: {TransactionId}, ProductId: {ProductId}, UserId: {UserId}",
                request.TransactionId, request.ProductId, userId);
            return new VerifyRevenueCatPurchaseResponse
            {
                Success = false,
                Verified = false,
                Error = "Transaction verification failed. Transaction not found in RevenueCat or product mismatch."
            };
        }

        _logger.LogInformation("[CreditService] Transaction verified successfully. TransactionId: {TransactionId}, ProductId: {ProductId}",
            request.TransactionId, request.ProductId);

        // Find package by RevenueCat product ID
        var package = (await _unitOfWork.CreditPackages.FindAsync(
            p => p.RevenueCatProductId == request.ProductId)).FirstOrDefault();

        if (package == null)
        {
            _logger.LogWarning("[CreditService] Package not found for RevenueCat product ID: {ProductId}", request.ProductId);
            return new VerifyRevenueCatPurchaseResponse
            {
                Success = false,
                Verified = true,
                Error = $"Package not found for product ID: {request.ProductId}"
            };
        }

        // Calculate credits with bonus
        var creditsToAdd = package.Credits;
        if (package.BonusPercentage.HasValue)
        {
            creditsToAdd = (int)(package.Credits * (1 + package.BonusPercentage.Value / 100.0));
        }

        _logger.LogInformation("[CreditService] Adding {Credits} credits to user {UserId}. Package: {PackageName}, Current Balance: {CurrentBalance}",
            creditsToAdd, userId, package.Name, user.Credits);

        // Add credits
        var oldCredits = user.Credits;
        user.Credits += creditsToAdd;
        await _unitOfWork.Users.UpdateAsync(user);

        // Create transaction record with transaction ID for idempotency
        var transaction = new Domain.Entities.CreditTransaction
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = CreditTransactionType.Purchase,
            Amount = creditsToAdd,
            BalanceAfter = user.Credits,
            TransactionId = request.TransactionId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _unitOfWork.CreditTransactions.AddAsync(transaction);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("[CreditService] Credits added successfully. UserId: {UserId}, Credits: {OldCredits} -> {NewCredits} (added {Added})",
            userId, oldCredits, user.Credits, creditsToAdd);

        return new VerifyRevenueCatPurchaseResponse
        {
            Success = true,
            Verified = true,
            CreditsAdded = creditsToAdd,
            NewBalance = user.Credits
        };
    }
}
