using GymSystem.Application.Abstractions.Services.IServiceService;
using GymSystem.Application.Abstractions.Services.IServiceService.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServicesController : ControllerBase {
    private readonly IServiceService _serviceService;
    private readonly ILogger<ServicesController> _logger;

    public ServicesController(IServiceService serviceService, ILogger<ServicesController> logger) {
        _serviceService = serviceService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll() {
        var response = await _serviceService.GetAllAsync();

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(int id) {
        var response = await _serviceService.GetByIdAsync(id);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        if (response.Data == null)
            return NotFound(new { error = "Hizmet bulunamadı" });

        return Ok(response.Data);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Create([FromBody] ServiceDto dto) {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _serviceService.CreateAsync(dto);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return CreatedAtAction(nameof(Get), new { id = response.Data!.Id }, response.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Update(int id, [FromBody] ServiceDto dto) {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (id != dto.Id)
            return BadRequest(new { ErrorMessage = "Service ID mismatch", ErrorCode = "VALIDATION_001" });

        var response = await _serviceService.UpdateAsync(id, dto);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Delete(int id) {
        var response = await _serviceService.DeleteAsync(id);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return NoContent();
    }
}
