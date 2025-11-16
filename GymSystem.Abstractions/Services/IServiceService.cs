using GymSystem.Common.ServiceRegistration;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Abstractions.Services;

public interface IServiceService : IGenericCrudService<Service>, IApplicationService
{
}
