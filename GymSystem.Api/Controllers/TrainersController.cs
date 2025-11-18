using GymSystem.Application.Abstractions.Contract.Trainer;
using GymSystem.Application.Abstractions.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TrainersController : ControllerBase {
    private readonly ITrainerService _trainerService;
    private readonly ILogger<TrainersController> _logger;

    public TrainersController(ITrainerService trainerService, ILogger<TrainersController> logger) {
        _trainerService = trainerService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll() {
        var response = await _trainerService.GetAllAsync();

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(int id) {
        var response = await _trainerService.GetByIdAsync(id);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        if (response.Data == null)
            return NotFound(new { error = "Antrenör bulunamadı" });

        return Ok(response.Data);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Create([FromBody] TrainerDto dto) {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _trainerService.CreateAsync(dto);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return CreatedAtAction(nameof(Get), new { id = response.Data!.Id }, response.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Update(int id, [FromBody] TrainerDto dto) {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (id != dto.Id)
            return BadRequest(new { ErrorMessage = "Trainer ID mismatch", ErrorCode = "VALIDATION_001" });

        var response = await _trainerService.UpdateAsync(id, dto);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Delete(int id) {
        var response = await _trainerService.DeleteAsync(id);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return NoContent();
    }
}