using GymSystem.Application.Abstractions.Services.IBodyMeasurement;
using GymSystem.Application.Abstractions.Services.IBodyMeasurement.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BodyMeasurementsController : ControllerBase {
    private readonly IBodyMeasurementService _measurementService;

    public BodyMeasurementsController(IBodyMeasurementService measurementService) {
        _measurementService = measurementService;
    }

    /// <summary>
    /// Üyenin tüm ölçümlerini getirir
    /// </summary>
    [HttpGet("member/{memberId}")]
    [Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> GetMemberMeasurements(int memberId) {
        if (memberId <= 0)
            return BadRequest(new { error = "Geçersiz member ID." });

        var response = await _measurementService.GetMemberMeasurementsAsync(memberId);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage });

        return Ok(response.Data);
    }

    /// <summary>
    /// Belirli bir ölçümü getirir
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> GetById(int id) {
        if (id <= 0)
            return BadRequest(new { error = "Geçersiz ID." });

        var response = await _measurementService.GetByIdAsync(id);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage });

        return Ok(response.Data);
    }

    /// <summary>
    /// Yeni ölçüm ekler
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Create([FromBody] BodyMeasurementDto dto) {
        if (dto.MemberId <= 0)
            return BadRequest(new { error = "Geçersiz member ID." });

        if (dto.Height <= 0 || dto.Weight <= 0)
            return BadRequest(new { error = "Boy ve kilo deðerleri sýfýrdan büyük olmalýdýr." });

        // Decimal precision fix - 1 ondalýk basamak
        dto.Height = Math.Round(dto.Height, 1);
        dto.Weight = Math.Round(dto.Weight, 1);

        var response = await _measurementService.CreateAsync(dto);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage });

        return CreatedAtAction(nameof(GetById), new { id = response.Data?.Id }, response.Data);
    }

    /// <summary>
    /// Ölçümü günceller
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Update(int id, [FromBody] BodyMeasurementDto dto) {
        if (id != dto.Id)
            return BadRequest(new { error = "ID uyuþmazlýðý." });

        if (dto.Height <= 0 || dto.Weight <= 0)
            return BadRequest(new { error = "Boy ve kilo deðerleri sýfýrdan büyük olmalýdýr." });

        // Decimal precision fix - 1 ondalýk basamak
        dto.Height = Math.Round(dto.Height, 1);
        dto.Weight = Math.Round(dto.Weight, 1);

        var response = await _measurementService.UpdateAsync(dto);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage });

        return Ok(response.Data);
    }

    /// <summary>
    /// Ölçümü siler
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Delete(int id) {
        if (id <= 0)
            return BadRequest(new { error = "Geçersiz ID." });

        var response = await _measurementService.DeleteAsync(id);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage });

        return NoContent();
    }

    /// <summary>
    /// Grafik verileri için ölçümleri getirir
    /// </summary>
    [HttpGet("chart/{memberId}")]
    [Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> GetChartData(int memberId, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null) {
        if (memberId <= 0)
            return BadRequest(new { error = "Geçersiz member ID." });

        var response = await _measurementService.GetChartDataAsync(memberId, startDate, endDate);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage });

        return Ok(response.Data);
    }
}
