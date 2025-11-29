using AutoMapper;
using GymSystem.Mvc.Models;
using GymSystem.Mvc.Models.Dtos;

namespace GymSystem.Mvc.Mappings;

/// <summary>
/// MVC için AutoMapper profile - API DTO'larını ViewModel'lere map eder
/// </summary>
public class MvcMappingProfile : Profile {
    public MvcMappingProfile() {
        // ApiMemberDto -> MemberViewModel
        CreateMap<ApiMemberDto, MemberViewModel>()
            .ForMember(dest => dest.CurrentGymLocationName,
                opt => opt.MapFrom(src => src.CurrentGymLocationName)); // Direkt flat field'dan al

        // ApiMemberDto -> EditMemberViewModel
        CreateMap<ApiMemberDto, EditMemberViewModel>();

        // EditMemberViewModel -> ApiMemberDto (PUT için)
        CreateMap<EditMemberViewModel, ApiMemberDto>();

        // CreateMemberViewModel -> ApiMemberDto (POST için)
        CreateMap<CreateMemberViewModel, ApiMemberDto>();

        // ApiTrainerDto -> TrainerViewModel
        CreateMap<ApiTrainerDto, TrainerViewModel>();

        // TrainerViewModel -> ApiTrainerDto (POST/PUT için)
        CreateMap<TrainerViewModel, ApiTrainerDto>();

        // ApiServiceDto -> ServiceViewModel
        CreateMap<ApiServiceDto, ServiceViewModel>();

        // ServiceViewModel -> ApiServiceDto (POST/PUT için)
        CreateMap<ServiceViewModel, ApiServiceDto>();

        // ApiGymLocationFullDto -> GymLocationViewModel
        CreateMap<ApiGymLocationFullDto, GymLocationViewModel>();

        // GymLocationViewModel -> ApiGymLocationFullDto (POST/PUT için)
        CreateMap<GymLocationViewModel, ApiGymLocationFullDto>();

        // ApiAppointmentDto -> AppointmentViewModel
        CreateMap<ApiAppointmentDto, AppointmentViewModel>()
            .ForMember(dest => dest.MemberName,
                opt => opt.MapFrom(src => 
                    !string.IsNullOrEmpty(src.MemberName) 
                        ? src.MemberName 
                        : src.Member != null ? $"{src.Member.FirstName} {src.Member.LastName}" : string.Empty))
            .ForMember(dest => dest.TrainerName,
                opt => opt.MapFrom(src => 
                    !string.IsNullOrEmpty(src.TrainerName) 
                        ? src.TrainerName 
                        : src.Trainer != null ? $"{src.Trainer.FirstName} {src.Trainer.LastName}" : string.Empty))
            .ForMember(dest => dest.ServiceName,
                opt => opt.MapFrom(src => 
                    !string.IsNullOrEmpty(src.ServiceName) 
                        ? src.ServiceName 
                        : src.Service != null ? src.Service.Name : string.Empty))
            .ForMember(dest => dest.GymLocationName,
                opt => opt.MapFrom(src => 
                    !string.IsNullOrEmpty(src.GymLocationName) 
                        ? src.GymLocationName 
                        : src.Service != null ? src.Service.GymLocationName : string.Empty));

        // ApiAIWorkoutPlanDto -> AIWorkoutPlanViewModel
        CreateMap<ApiAIWorkoutPlanDto, AIWorkoutPlanViewModel>()
            .ForMember(dest => dest.MemberName,
                opt => opt.MapFrom(src => src.Member != null ? $"{src.Member.FirstName} {src.Member.LastName}" : string.Empty));

        // ApiMembershipRequestDto -> MembershipRequestViewModel
        CreateMap<ApiMembershipRequestDto, MembershipRequestViewModel>()
            .ForMember(dest => dest.Status,
                opt => opt.MapFrom(src => src.StatusEnum)) // String'den enum'a dönüştür
            .ForMember(dest => dest.MemberName, 
                opt => opt.MapFrom(src => src.MemberName ?? "Bilinmiyor"))
            .ForMember(dest => dest.GymLocationName, 
                opt => opt.MapFrom(src => src.GymLocationName ?? "Bilinmiyor"))
            .ForMember(dest => dest.GymLocationAddress, 
                opt => opt.MapFrom(src => src.GymLocationAddress ?? "Bilinmiyor"));
    }
}