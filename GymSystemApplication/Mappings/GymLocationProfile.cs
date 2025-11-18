using AutoMapper;
using GymSystem.Application.Abstractions.Contract.GymLocation;
using GymSystem.Common.Helpers;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Mappings;

public class GymLocationProfile : Profile {
    public GymLocationProfile() {
        // Entity -> DTO
        CreateMap<GymLocation, GymLocationDto>()
            .ForMember(dest => dest.Capacity, opt => opt.Ignore());

        // DTO -> Entity
        CreateMap<GymLocationDto, GymLocation>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.City, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTimeHelper.Now))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.Trainers, opt => opt.Ignore())
            .ForMember(dest => dest.Services, opt => opt.Ignore())
            .ForMember(dest => dest.WorkingHours, opt => opt.Ignore());
    }
}