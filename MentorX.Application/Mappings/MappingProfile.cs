using AutoMapper;
using MentorX.Domain.Entities;
using MentorX.Application.DTOs.Responses;
using MentorX.Domain.Enums;

namespace MentorX.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserResponse>();

        // Mentor mappings
        CreateMap<Mentor, MentorResponse>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.Code))
            .ForMember(dest => dest.ExpertiseTags, opt => opt.MapFrom(src => src.MentorTags.Select(mt => mt.Tag.Name).ToList()))
            .ForMember(dest => dest.IsFollowing, opt => opt.Ignore());

        CreateMap<Mentor, MentorSummaryResponse>()
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role.Code));

        // Insight mappings
        CreateMap<Insight, InsightResponse>()
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString().ToLower()))
            .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.InsightTags.Select(it => it.Tag.Name).ToList()))
            .ForMember(dest => dest.Mentor, opt => opt.MapFrom(src => src.Mentor))
            .ForMember(dest => dest.IsLiked, opt => opt.Ignore());

        // Comment mappings
        CreateMap<Comment, CommentResponse>()
            .ForMember(dest => dest.Author, opt => opt.Ignore());

        // Conversation mappings
        CreateMap<Conversation, ConversationResponse>()
            .ForMember(dest => dest.Mentor, opt => opt.MapFrom(src => src.Mentor));

        // Message mappings
        CreateMap<Message, MessageResponse>()
            .ForMember(dest => dest.Sender, opt => opt.Ignore());

        // CreditPackage mappings
        CreateMap<CreditPackage, CreditPackageResponse>();
    }
}
