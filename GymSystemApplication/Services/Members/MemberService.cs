using GymSystem.Application.Abstractions.Services;
using GymSystem.Common.Factory.Managers;
using GymSystem.Common.Models;
using GymSystem.Common.Services;
using GymSystem.Domain.Entities;
using GymSystem.Persistance.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Services.Members;

/// <summary>
/// Member service - Generic CRUD + Custom methods
/// </summary>
public class MemberService : GenericCrudService<Member>, IMemberService
{
    private readonly GymDbContext _context;
    private readonly ILogger<MemberService> _customLogger;

    public MemberService(
        BaseFactory<GenericCrudService<Member>> baseFactory,
        GymDbContext context,
        ILogger<MemberService> logger)
        : base(baseFactory)
    {
        _context = context;
        _customLogger = logger;
    }

    public async Task<ServiceResponse<IEnumerable<Member>>> GetAllMembersWithGymLocationAsync()
    {
        try
        {
            // DbContext'ten direkt include ile al
            var members = await _context.Set<Member>()
                .Include(m => m.CurrentGymLocation)
                .Where(m => m.IsActive)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return _responseHelper.SetSuccess<IEnumerable<Member>>(members);
        }
        catch (Exception ex)
        {
            _customLogger.LogError(ex, "Member'lar GymLocation ile birlikte alınırken hata oluştu");
            return _responseHelper.SetError<IEnumerable<Member>>(
                null, 
                "Member'lar alınırken bir hata oluştu", 
                500, 
                "MEMBER_ERROR_001");
        }
    }
}