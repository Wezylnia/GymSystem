using GymSystem.Common.ServiceRegistration;
using GymSystem.Common.Services;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Abstractions.Services;

public interface ITrainerService : IGenericCrudService<Trainer>, IApplicationService
{
}
