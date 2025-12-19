using AutoMapper;
using GymSystem.Application.Abstractions.Contract.AI;
using GymSystem.Common.Helpers;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Services.AI.Profile;

public class AIWorkoutPlanProfile : Profile {
    public AIWorkoutPlanProfile() {
        CreateMap<AIWorkoutPlan, AIWorkoutPlanDto>()
            .ForMember(dest => dest.MemberName,
                opt => opt.MapFrom(src => src.Member != null ? $"{src.Member.FirstName} {src.Member.LastName}" : null))
            .ForMember(dest => dest.PhotoBase64, opt => opt.Ignore());

        CreateMap<AIWorkoutPlanDto, AIWorkoutPlan>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Member, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTimeHelper.Now))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.AIGeneratedPlan, opt => opt.Ignore())
            .ForMember(dest => dest.AIModel, opt => opt.Ignore())
            .ForMember(dest => dest.PlanType, opt => opt.Ignore());
    }
}