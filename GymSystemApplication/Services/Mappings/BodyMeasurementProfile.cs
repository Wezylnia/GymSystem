using AutoMapper;
using GymSystem.Application.Abstractions.Services.IBodyMeasurement.Contract;
using GymSystem.Common.Helpers;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Services.Mappings;

public class BodyMeasurementProfile : Profile {
    public BodyMeasurementProfile() {
        CreateMap<BodyMeasurement, BodyMeasurementDto>()
            .ForMember(dest => dest.MemberName,
                opt => opt.MapFrom(src => src.Member != null ? $"{src.Member.FirstName} {src.Member.LastName}" : null))
            .ForMember(dest => dest.HeightChange, opt => opt.Ignore())
            .ForMember(dest => dest.WeightChange, opt => opt.Ignore());

        CreateMap<BodyMeasurementDto, BodyMeasurement>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Member, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTimeHelper.Now))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));
    }
}
