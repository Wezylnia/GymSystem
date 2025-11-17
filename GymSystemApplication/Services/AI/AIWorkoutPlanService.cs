using GymSystem.Application.Abstractions.Services;
using GymSystem.Common.Helpers;
using GymSystem.Domain.Entities;
using GymSystem.Persistance.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GymSystem.Application.Services.AI;

public class AIWorkoutPlanService : IAIWorkoutPlanService
{
    private readonly GymDbContext _context;
    private readonly IGeminiApiService _geminiApiService;
    private readonly ILogger<AIWorkoutPlanService> _logger;

    public AIWorkoutPlanService(
        GymDbContext context,
        IGeminiApiService geminiApiService,
        ILogger<AIWorkoutPlanService> logger)
    {
        _context = context;
        _geminiApiService = geminiApiService;
        _logger = logger;
    }

    public async Task<AIWorkoutPlan> GenerateWorkoutPlanAsync(int memberId, decimal height, decimal weight, 
        string? bodyType, string goal, string? photoBase64 = null)
    {
        // Member kontrolü
        var member = await _context.Set<Member>().FindAsync(memberId);
        if (member == null)
        {
            throw new ArgumentException($"Member ID {memberId} bulunamadı.");
        }

        string aiPlan;
        try
        {
            // AI'dan plan al
            aiPlan = await _geminiApiService.GenerateWorkoutPlanAsync(height, weight, bodyType, goal, photoBase64);
            
            if (string.IsNullOrWhiteSpace(aiPlan))
            {
                throw new InvalidOperationException("AI'dan boş plan döndü.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API'den plan alınırken hata. Member ID: {MemberId}", memberId);
            throw new InvalidOperationException("AI planı oluşturulamadı. Lütfen daha sonra tekrar deneyin.", ex);
        }

        // Database'e kaydet
        var workoutPlan = new AIWorkoutPlan
        {
            MemberId = memberId,
            PlanType = "Workout",
            Height = height,
            Weight = weight,
            BodyType = bodyType,
            Goal = goal,
            AIGeneratedPlan = aiPlan,
            AIModel = !string.IsNullOrEmpty(photoBase64) ? "gemini-2.0-flash-exp-vision" : "gemini-2.0-flash-exp",
            CreatedAt = DateTimeHelper.Now,
            IsActive = true
        };

        _context.Set<AIWorkoutPlan>().Add(workoutPlan);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Workout planı oluşturuldu. Member ID: {MemberId}, Plan ID: {PlanId}", 
            memberId, workoutPlan.Id);

        return workoutPlan;
    }

    public async Task<AIWorkoutPlan> GenerateDietPlanAsync(int memberId, decimal height, decimal weight, 
        string? bodyType, string goal, string? photoBase64 = null)
    {
        // Member kontrolü
        var member = await _context.Set<Member>().FindAsync(memberId);
        if (member == null)
        {
            throw new ArgumentException($"Member ID {memberId} bulunamadı.");
        }

        string aiPlan;
        try
        {
            // AI'dan plan al
            aiPlan = await _geminiApiService.GenerateDietPlanAsync(height, weight, bodyType, goal, photoBase64);
            
            if (string.IsNullOrWhiteSpace(aiPlan))
            {
                throw new InvalidOperationException("AI'dan boş plan döndü.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini API'den plan alınırken hata. Member ID: {MemberId}", memberId);
            throw new InvalidOperationException("AI planı oluşturulamadı. Lütfen daha sonra tekrar deneyin.", ex);
        }

        // Database'e kaydet
        var dietPlan = new AIWorkoutPlan
        {
            MemberId = memberId,
            PlanType = "Diet",
            Height = height,
            Weight = weight,
            BodyType = bodyType,
            Goal = goal,
            AIGeneratedPlan = aiPlan,
            AIModel = !string.IsNullOrEmpty(photoBase64) ? "gemini-2.0-flash-exp-vision" : "gemini-2.0-flash-exp",
            CreatedAt = DateTimeHelper.Now,
            IsActive = true
        };

        _context.Set<AIWorkoutPlan>().Add(dietPlan);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Diet planı oluşturuldu. Member ID: {MemberId}, Plan ID: {PlanId}", 
            memberId, dietPlan.Id);

        return dietPlan;
    }

    public async Task<List<AIWorkoutPlan>> GetMemberPlansAsync(int memberId)
    {
        return await _context.Set<AIWorkoutPlan>()
            .Where(p => p.MemberId == memberId && p.IsActive)
            .Include(p => p.Member)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<AIWorkoutPlan?> GetPlanByIdAsync(int id)
    {
        return await _context.Set<AIWorkoutPlan>()
            .Include(p => p.Member)
            .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);
    }

    public async Task<bool> DeletePlanAsync(int id)
    {
        try
        {
            var plan = await _context.Set<AIWorkoutPlan>().FindAsync(id);
            if (plan == null)
            {
                return false;
            }

            plan.IsActive = false;
            plan.UpdatedAt = DateTimeHelper.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("AI Plan silindi. Plan ID: {PlanId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plan silinirken hata oluştu. Plan ID: {PlanId}", id);
            return false;
        }
    }

    public async Task<List<AIWorkoutPlan>> GetAllPlansAsync()
    {
        return await _context.Set<AIWorkoutPlan>()
            .Where(p => p.IsActive)
            .Include(p => p.Member)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}
