using GymSystem.Application.Abstractions.Services;
using GymSystem.Application.Factory.Managers;
using GymSystem.Application.Services.Generic;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Services.GymLocations;

public class GymLocationService : GenericCrudService<GymLocation>, IGymLocationService
{
    public GymLocationService(BaseFactory<GenericCrudService<GymLocation>> baseFactory)
        : base(baseFactory)
    {
    }
}