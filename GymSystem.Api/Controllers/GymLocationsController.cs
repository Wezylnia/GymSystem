using GymSystem.Application.Abstractions.Services;
using GymSystem.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GymLocationsController : ControllerBase
{
    private readonly IGymLocationService _gymLocationService;
    private readonly ILogger<GymLocationsController> _logger;

    public GymLocationsController(IGymLocationService gymLocationService, ILogger<GymLocationsController> logger)
    {
        _gymLocationService = gymLocationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var response = await _gymLocationService.GetAllAsync();
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var response = await _gymLocationService.GetByIdAsync(id);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Create(GymLocation gymLocation)
    {
        var response = await _gymLocationService.CreateAsync(gymLocation);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return CreatedAtAction(nameof(Get), new { id = response.Data!.Id }, response.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, GymLocation gymLocation)
    {
        if (id != gymLocation.Id)
        {
            return BadRequest(new { ErrorMessage = "GymLocation ID mismatch", ErrorCode = "VALIDATION_001" });
        }

        var response = await _gymLocationService.UpdateAsync(id, gymLocation);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _gymLocationService.DeleteAsync(id);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return NoContent();
    }
}
