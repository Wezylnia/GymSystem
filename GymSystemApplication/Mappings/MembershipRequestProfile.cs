using AutoMapper;
using GymSystem.Application.Abstractions.Contract.MembershipRequest;
using GymSystem.Common.Helpers;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Mappings;

/// <summary>
/// AutoMapper profile for MembershipRequest mappings
/// </summary>
public class MembershipRequestProfile : Profile
{
    public MembershipRequestProfile()
    {
        // Entity -> DTO
        CreateMap<MembershipRequest, MembershipRequestDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.Member != null ? $"{src.Member.FirstName} {src.Member.LastName}" : null))
            .ForMember(dest => dest.MemberEmail, opt => opt.MapFrom(src => src.Member != null ? src.Member.Email : null))
            .ForMember(dest => dest.GymLocationName, opt => opt.MapFrom(src => src.GymLocation != null ? src.GymLocation.Name : null))
            .ForMember(dest => dest.GymLocationAddress, opt => opt.MapFrom(src => src.GymLocation != null ? src.GymLocation.Address : null));

        // DTO -> Entity
        CreateMap<MembershipRequestDto, MembershipRequest>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Enum.Parse<MembershipRequestStatus>(src.Status)))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTimeHelper.Now))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.Member, opt => opt.Ignore())
            .ForMember(dest => dest.GymLocation, opt => opt.Ignore());
    }
}
