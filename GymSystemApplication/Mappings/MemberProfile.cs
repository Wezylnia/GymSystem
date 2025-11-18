using AutoMapper;
using GymSystem.Application.Abstractions.Contract.Member;
using GymSystem.Common.Helpers;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Mappings;

public class MemberProfile : Profile {
    public MemberProfile() {
        CreateMap<Member, MemberDto>()
            .ForMember(dest => dest.CurrentGymLocationName, opt => opt.MapFrom(src => src.CurrentGymLocation != null ? src.CurrentGymLocation.Name : null));

        CreateMap<MemberDto, Member>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTimeHelper.Now))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CurrentGymLocation, opt => opt.Ignore())
            .ForMember(dest => dest.Appointments, opt => opt.Ignore())
            .ForMember(dest => dest.WorkoutPlans, opt => opt.Ignore());
    }
}