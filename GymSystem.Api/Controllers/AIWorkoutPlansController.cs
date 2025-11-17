using GymSystem.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AIWorkoutPlansController : ControllerBase
{
    private readonly IAIWorkoutPlanService _aiWorkoutPlanService;
    private readonly ILogger<AIWorkoutPlansController> _logger;

    public AIWorkoutPlansController(
        IAIWorkoutPlanService aiWorkoutPlanService,
        ILogger<AIWorkoutPlansController> logger)
    {
        _aiWorkoutPlanService = aiWorkoutPlanService;
        _logger = logger;
    }

    /// <summary>
    /// Yeni workout planı oluşturur
    /// </summary>
    [HttpPost("generate-workout")]
    [AllowAnonymous] // MVC'den internal çağrı için
    public async Task<IActionResult> GenerateWorkoutPlan([FromBody] GenerateWorkoutPlanRequest request)
    {
        try
        {
            // MemberId validation
            if (request.MemberId <= 0)
            {
                return BadRequest(new { error = "Geçersiz member ID." });
            }

            var plan = await _aiWorkoutPlanService.GenerateWorkoutPlanAsync(
                request.MemberId,
                request.Height,
                request.Weight,
                request.BodyType,
                request.Goal,
                request.PhotoBase64
            );

            // Entity'den DTO'ya dönüştür (circular reference önlemek için)
            var dto = new
            {
                plan.Id,
                plan.MemberId,
                plan.PlanType,
                plan.Height,
                plan.Weight,
                plan.BodyType,
                plan.Goal,
                plan.AIGeneratedPlan,
                plan.ImageUrl,
                plan.AIModel,
                plan.IsActive,
                plan.CreatedAt,
                MemberName = $"{plan.Member?.FirstName} {plan.Member?.LastName}"
            };

            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workout planı oluşturulurken hata");
            return StatusCode(500, new { error = "Plan oluşturulurken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Yeni diet planı oluşturur
    /// </summary>
    [HttpPost("generate-diet")]
    [AllowAnonymous] // MVC'den internal çağrı için
    public async Task<IActionResult> GenerateDietPlan([FromBody] GenerateDietPlanRequest request)
    {
        try
        {
            // MemberId validation
            if (request.MemberId <= 0)
            {
                return BadRequest(new { error = "Geçersiz member ID." });
            }

            var plan = await _aiWorkoutPlanService.GenerateDietPlanAsync(
                request.MemberId,
                request.Height,
                request.Weight,
                request.BodyType,
                request.Goal,
                request.PhotoBase64
            );

            // Entity'den DTO'ya dönüştür (circular reference önlemek için)
            var dto = new
            {
                plan.Id,
                plan.MemberId,
                plan.PlanType,
                plan.Height,
                plan.Weight,
                plan.BodyType,
                plan.Goal,
                plan.AIGeneratedPlan,
                plan.ImageUrl,
                plan.AIModel,
                plan.IsActive,
                plan.CreatedAt,
                MemberName = $"{plan.Member?.FirstName} {plan.Member?.LastName}"
            };

            return Ok(dto);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Diet planı oluşturulurken hata");
            return StatusCode(500, new { error = "Plan oluşturulurken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Belirli bir üyenin tüm planlarını getirir
    /// </summary>
    [HttpGet("member/{memberId}")]
    [AllowAnonymous] // MVC'den internal çağrı için
    public async Task<IActionResult> GetMemberPlans(int memberId)
    {
        try
        {
            if (memberId <= 0)
            {
                return BadRequest(new { error = "Geçersiz member ID." });
            }

            var plans = await _aiWorkoutPlanService.GetMemberPlansAsync(memberId);
            
            // Entity'den DTO'ya dönüştür (circular reference önlemek için)
            var dtos = plans.Select(p => new
            {
                p.Id,
                p.MemberId,
                p.PlanType,
                p.Height,
                p.Weight,
                p.BodyType,
                p.Goal,
                p.AIGeneratedPlan,
                p.ImageUrl,
                p.AIModel,
                p.IsActive,
                p.CreatedAt,
                MemberName = $"{p.Member?.FirstName} {p.Member?.LastName}"
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Üye planları getirilirken hata. Member ID: {MemberId}", memberId);
            return StatusCode(500, new { error = "Planlar getirilirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Plan detayını getirir
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous] // MVC'den internal çağrı için
    public async Task<IActionResult> GetPlanById(int id)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "Geçersiz plan ID." });
            }

            var plan = await _aiWorkoutPlanService.GetPlanByIdAsync(id);
            if (plan == null)
            {
                return NotFound(new { error = "Plan bulunamadı." });
            }

            // Entity'den DTO'ya dönüştür (circular reference önlemek için)
            var dto = new
            {
                plan.Id,
                plan.MemberId,
                plan.PlanType,
                plan.Height,
                plan.Weight,
                plan.BodyType,
                plan.Goal,
                plan.AIGeneratedPlan,
                plan.ImageUrl,
                plan.AIModel,
                plan.IsActive,
                plan.CreatedAt,
                MemberName = $"{plan.Member?.FirstName} {plan.Member?.LastName}"
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plan getirilirken hata. Plan ID: {PlanId}", id);
            return StatusCode(500, new { error = "Plan getirilirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Planı siler (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [AllowAnonymous] // MVC'den internal çağrı için
    public async Task<IActionResult> DeletePlan(int id)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "Geçersiz plan ID." });
            }

            var result = await _aiWorkoutPlanService.DeletePlanAsync(id);
            
            if (!result)
            {
                return NotFound(new { error = "Plan bulunamadı veya silinemedi." });
            }

            return Ok(new { message = "Plan başarıyla silindi.", success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Plan silinirken hata. Plan ID: {PlanId}", id);
            return StatusCode(500, new { error = "Plan silinirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Tüm planları getirir (Admin)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllPlans()
    {
        try
        {
            var plans = await _aiWorkoutPlanService.GetAllPlansAsync();
            
            // Entity'den DTO'ya dönüştür (circular reference önlemek için)
            var dtos = plans.Select(p => new
            {
                p.Id,
                p.MemberId,
                p.PlanType,
                p.Height,
                p.Weight,
                p.BodyType,
                p.Goal,
                p.AIGeneratedPlan,
                p.ImageUrl,
                p.AIModel,
                p.IsActive,
                p.CreatedAt,
                MemberName = $"{p.Member?.FirstName} {p.Member?.LastName}"
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tüm planlar getirilirken hata");
            return StatusCode(500, new { error = "Planlar getirilirken bir hata oluştu." });
        }
    }
}

// Request Models
public record GenerateWorkoutPlanRequest(
    int MemberId,
    decimal Height,
    decimal Weight,
    string? BodyType,
    string Goal,
    string? PhotoBase64
);

public record GenerateDietPlanRequest(
    int MemberId,
    decimal Height,
    decimal Weight,
    string? BodyType,
    string Goal,
    string? PhotoBase64
);