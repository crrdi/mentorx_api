using AutoMapper;
using FluentValidation;
using FluentValidation.AspNetCore;
using MentorX.Application.Interfaces;
using MentorX.Application.Mappings;
using MentorX.Application.Services;
using MentorX.Application.Validators;
using MentorX.Domain.Interfaces;
using MentorX.Infrastructure.Data.Repositories;
using MentorX.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace MentorX.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // AutoMapper - use assembly-based registration for AutoMapper 16.0.0
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        }, typeof(MappingProfile).Assembly);

        // FluentValidation
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<CreateMentorRequestValidator>();

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IMentorService, MentorService>();
        services.AddScoped<IInsightService, InsightService>();
        services.AddScoped<ICommentService, CommentService>();
        services.AddScoped<IConversationService, ConversationService>();
        services.AddScoped<ICreditService, CreditService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IGeminiService, GeminiService>();
        services.AddScoped<MentorX.Application.Interfaces.ISupabaseAuthService, MentorX.Infrastructure.Services.SupabaseAuthService>();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // UnitOfWork
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Infrastructure
        services.AddInfrastructure(configuration);

        return services;
    }
}
