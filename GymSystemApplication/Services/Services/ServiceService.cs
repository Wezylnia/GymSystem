using GymSystem.Application.Abstractions.Services;
using GymSystem.Application.Factory.Managers;
using GymSystem.Application.Services.Generic;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Services.Services;

public class ServiceService : GenericCrudService<Service>, IServiceService
{
    public ServiceService(BaseFactory<GenericCrudService<Service>> baseFactory)
        : base(baseFactory)
    {
    }
}
