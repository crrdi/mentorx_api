using AutoMapper;
using MentorX.Application.DTOs.Requests;
using MentorX.Application.DTOs.Responses;
using MentorX.Application.Interfaces;
using MentorX.Domain.Interfaces;
using MentorX.Domain.Enums;

namespace MentorX.Application.Services;

public class CreditService : ICreditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreditService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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
}
