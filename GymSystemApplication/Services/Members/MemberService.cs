using GymSystem.Application.Abstractions.Services;
using GymSystem.Application.Factory.Managers;
using GymSystem.Application.Services.Generic;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Services.Members;

/// <summary>
/// Member service - Sadece Generic CRUD kullanır
/// </summary>
public class MemberService : GenericCrudService<Member>, IMemberService
{
    public MemberService(BaseFactory<GenericCrudService<Member>> baseFactory)
        : base(baseFactory)
    {
    }
}