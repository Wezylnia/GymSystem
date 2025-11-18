using GymSystem.Application.Abstractions.Contract.AI;
using GymSystem.Application.Abstractions.Services.IAIWorkoutPlan;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AIWorkoutPlansController : ControllerBase {
    private readonly IAIWorkoutPlanService _aiWorkoutPlanService;

    public AIWorkoutPlansController(IAIWorkoutPlanService aiWorkoutPlanService) {
        _aiWorkoutPlanService = aiWorkoutPlanService;
    }

    [HttpPost("generate-workout")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> GenerateWorkoutPlan([FromBody] AIWorkoutPlanDto request) {
        if (request.MemberId <= 0)
            return BadRequest(new { error = "Geçersiz member ID." });

        var response = await _aiWorkoutPlanService.GenerateWorkoutPlanAsync(request);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage });

        return Ok(response.Data);
    }

    [HttpPost("generate-diet")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> GenerateDietPlan([FromBody] AIWorkoutPlanDto request) {
        if (request.MemberId <= 0)
            return BadRequest(new { error = "Geçersiz member ID." });

        var response = await _aiWorkoutPlanService.GenerateDietPlanAsync(request);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage });

        return Ok(response.Data);
    }

    [HttpGet("member/{memberId}")]
    [Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> GetMemberPlans(int memberId) {
        if (memberId <= 0)
            return BadRequest(new { error = "Geçersiz member ID." });

        var response = await _aiWorkoutPlanService.GetMemberPlansAsync(memberId);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage });

        return Ok(response.Data);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> GetPlanById(int id) {
        if (id <= 0)
            return BadRequest(new { error = "Geçersiz plan ID." });

        var response = await _aiWorkoutPlanService.GetPlanByIdAsync(id);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage });

        if (response.Data == null)
            return NotFound(new { error = "Plan bulunamadı." });

        return Ok(response.Data);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> DeletePlan(int id) {
        if (id <= 0)
            return BadRequest(new { error = "Geçersiz plan ID." });

        var response = await _aiWorkoutPlanService.DeletePlanAsync(id);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage });

        return Ok(new { message = response.Message, success = true });
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllPlans() {
        var response = await _aiWorkoutPlanService.GetAllPlansAsync();

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage });

        return Ok(response.Data);
    }
}