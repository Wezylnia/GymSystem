using GymSystem.Application.Abstractions.Services;
using GymSystem.Common.Helpers;
using GymSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly IServiceService _serviceService;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(IServiceService serviceService, ILogger<ServicesController> logger)
    {
        _serviceService = serviceService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll()
    {
        var response = await _serviceService.GetAllAsync();
        
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
        var response = await _serviceService.GetByIdAsync(id);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Create([FromBody] CreateServiceDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var service = new Service
        {
            Name = dto.Name,
            Description = dto.Description,
            DurationMinutes = dto.DurationMinutes,
            Price = dto.Price,
            GymLocationId = dto.GymLocationId,
            IsActive = true,
            CreatedAt = DateTimeHelper.Now
        };

        var response = await _serviceService.CreateAsync(service);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return CreatedAtAction(nameof(Get), new { id = response.Data!.Id }, response.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateServiceDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (id != dto.Id)
        {
            return BadRequest(new { ErrorMessage = "Service ID mismatch", ErrorCode = "VALIDATION_001" });
        }

        var existingResponse = await _serviceService.GetByIdAsync(id);
        if (!existingResponse.IsSuccessful || existingResponse.Data == null)
        {
            return NotFound(new { ErrorMessage = $"Service with ID {id} not found", ErrorCode = "NOT_FOUND_001" });
        }

        var service = existingResponse.Data;
        service.Name = dto.Name;
        service.Description = dto.Description;
        service.DurationMinutes = dto.DurationMinutes;
        service.Price = dto.Price;
        service.GymLocationId = dto.GymLocationId;
        service.IsActive = dto.IsActive;
        service.UpdatedAt = DateTimeHelper.Now;

        var response = await _serviceService.UpdateAsync(id, service);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _serviceService.DeleteAsync(id);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return NoContent();
    }
}

// DTOs
public class CreateServiceDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public int GymLocationId { get; set; }
}

public class UpdateServiceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public int GymLocationId { get; set; }
    public bool IsActive { get; set; }
}
