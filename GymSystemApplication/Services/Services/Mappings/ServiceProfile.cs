using AutoMapper;
using GymSystem.Application.Abstractions.Services.IServiceService.Contract;
using GymSystem.Common.Helpers;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Services.Services.Mappings;

/// <summary>
/// AutoMapper profile for Service mappings
/// </summary>
public class ServiceProfile : Profile {
    public ServiceProfile() {
        // Entity -> DTO
        CreateMap<Service, ServiceDto>()
            .ForMember(dest => dest.GymLocationName, opt => opt.MapFrom(src => src.GymLocation != null ? src.GymLocation.Name : null));

        // DTO -> Entity
        CreateMap<ServiceDto, Service>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTimeHelper.Now))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.GymLocation, opt => opt.Ignore())
            .ForMember(dest => dest.Appointments, opt => opt.Ignore());
    }
}
