using AutoMapper;
using GymSystem.Application.Abstractions.Contract.Trainer;
using GymSystem.Common.Helpers;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Mappings;

/// <summary>
/// AutoMapper profile for Trainer mappings
/// </summary>
public class TrainerProfile : Profile
{
    public TrainerProfile()
    {
        // Entity -> DTO
        CreateMap<Trainer, TrainerDto>()
            .ForMember(dest => dest.GymLocationName, opt => opt.MapFrom(src => src.GymLocation != null ? src.GymLocation.Name : null));

        // DTO -> Entity
        CreateMap<TrainerDto, Trainer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTimeHelper.Now))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.GymLocation, opt => opt.Ignore())
            .ForMember(dest => dest.Appointments, opt => opt.Ignore());
    }
}
