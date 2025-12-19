using AutoMapper;
using GymSystem.Application.Abstractions.Services.IAppointmentService.Contract;
using GymSystem.Common.Helpers;

namespace GymSystem.Application.Services.Appointments.Mappings;

/// <summary>
/// AutoMapper profile for Appointment mappings
/// </summary>
public class AppointmentProfile : Profile
{
    public AppointmentProfile()
    {
        // Entity -> DTO
        CreateMap<Domain.Entities.Appointment, AppointmentDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.Member != null ? $"{src.Member.FirstName} {src.Member.LastName}" : null))
            .ForMember(dest => dest.TrainerName, opt => opt.MapFrom(src => src.Trainer != null ? src.Trainer.FirstName + " " + src.Trainer.LastName : null))
            .ForMember(dest => dest.ServiceName, opt => opt.MapFrom(src => src.Service != null ? src.Service.Name : null))
            .ForMember(dest => dest.GymLocationName, opt => opt.MapFrom(src => src.Service != null && src.Service.GymLocation != null ? src.Service.GymLocation.Name : null));

        // DTO -> Entity
        CreateMap<AppointmentDto, Domain.Entities.Appointment>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Member, opt => opt.Ignore())
            .ForMember(dest => dest.Trainer, opt => opt.Ignore())
            .ForMember(dest => dest.Service, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTimeHelper.Now))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.Status, opt => opt.Ignore()); // Enum dönüşümü service'te yapılacak
    }
}