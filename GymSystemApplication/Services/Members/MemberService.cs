using GymSystem.Application.Abstractions.Services;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Services;
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