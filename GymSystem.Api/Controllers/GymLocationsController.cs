using GymSystem.Application.Abstractions.Services;
using GymSystem.Common.Helpers;
using GymSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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
    [AllowAnonymous]
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
    [AllowAnonymous]
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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateGymLocationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var gymLocation = new GymLocation
        {
            Name = dto.Name,
            Address = dto.Address,
            City = dto.City,
            PhoneNumber = dto.PhoneNumber,
            Email = dto.Email,
            Description = dto.Description,
            IsActive = true,
            CreatedAt = DateTimeHelper.Now
        };

        var response = await _gymLocationService.CreateAsync(gymLocation);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return CreatedAtAction(nameof(Get), new { id = response.Data!.Id }, response.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateGymLocationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (id != dto.Id)
        {
            return BadRequest(new { ErrorMessage = "GymLocation ID mismatch", ErrorCode = "VALIDATION_001" });
        }

        var existingResponse = await _gymLocationService.GetByIdAsync(id);
        if (!existingResponse.IsSuccessful || existingResponse.Data == null)
        {
            return NotFound(new { ErrorMessage = $"GymLocation with ID {id} not found", ErrorCode = "NOT_FOUND_001" });
        }

        var gymLocation = existingResponse.Data;
        gymLocation.Name = dto.Name;
        gymLocation.Address = dto.Address;
        gymLocation.City = dto.City;
        gymLocation.PhoneNumber = dto.PhoneNumber;
        gymLocation.Email = dto.Email;
        gymLocation.Description = dto.Description;
        gymLocation.IsActive = dto.IsActive;
        gymLocation.UpdatedAt = DateTimeHelper.Now;

        var response = await _gymLocationService.UpdateAsync(id, gymLocation);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
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

// DTOs
public class CreateGymLocationDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Description { get; set; }
}

public class UpdateGymLocationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
