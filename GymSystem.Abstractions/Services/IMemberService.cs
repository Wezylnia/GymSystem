using GymSystem.Common.ServiceRegistration;
using GymSystem.Common.Services;
using GymSystem.Domain.Entities;

namespace GymSystem.Application.Abstractions.Services;

/// <summary>
/// Member service interface - Generic CRUD + IApplicationService
/// IApplicationService ile otomatik registration'a dahil olur
/// </summary>
public interface IMemberService : IGenericCrudService<Member>, IApplicationService
{
}
