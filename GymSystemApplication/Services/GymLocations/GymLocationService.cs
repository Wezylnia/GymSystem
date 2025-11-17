using GymSystem.Application.Abstractions.Services;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Services;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Services.GymLocations;

public class GymLocationService : GenericCrudService<GymLocation>, IGymLocationService
{
    public GymLocationService(BaseFactory<GenericCrudService<GymLocation>> baseFactory)
        : base(baseFactory)
    {
    }
}