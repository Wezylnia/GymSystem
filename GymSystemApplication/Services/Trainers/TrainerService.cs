using GymSystem.Application.Abstractions.Services;
using GymSystem.Application.Factory.Managers;
using GymSystem.Application.Services.Generic;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Services.Trainers;

public class TrainerService : GenericCrudService<Trainer>, ITrainerService
{
    public TrainerService(BaseFactory<GenericCrudService<Trainer>> baseFactory)
        : base(baseFactory)
    {
    }
}
