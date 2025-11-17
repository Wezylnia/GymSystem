using GymSystem.Application.Abstractions.Services;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Services;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Services.Services;

public class ServiceService : GenericCrudService<Service>, IServiceService
{
    public ServiceService(BaseFactory<GenericCrudService<Service>> baseFactory)
        : base(baseFactory)
    {
    }
}
