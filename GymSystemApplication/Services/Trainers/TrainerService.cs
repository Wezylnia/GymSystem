using GymSystem.Application.Abstractions.Services;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Services;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Services.Trainers;

public class TrainerService : GenericCrudService<Trainer>, ITrainerService
{
    public TrainerService(BaseFactory<GenericCrudService<Trainer>> baseFactory)
        : base(baseFactory)
    {
    }
}