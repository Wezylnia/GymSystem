using GymSystem.Application.Abstractions.Services.IMemberService;
using GymSystem.Application.Abstractions.Services.IMemberService.Contract;
using GymSystem.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MembersController : ControllerBase {
    private readonly IMemberService _memberService;
    private readonly ILogger<MembersController> _logger;

    public MembersController(IMemberService memberService, ILogger<MembersController> logger) {
        _memberService = memberService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> GetAll([FromQuery] int? gymLocationId = null) {
        ServiceResponse<List<MemberDto>> response;

        if (gymLocationId.HasValue) {
            // GymOwner için: Sadece kendi salonuna kayıtlı üyeleri getir
            response = await _memberService.GetMembersByGymLocationAsync(gymLocationId.Value);
        }
        else {
            // Admin için: Tüm üyeleri getir
            response = await _memberService.GetAllMembersWithGymLocationAsync();
        }

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,GymOwner,Member")]
    public async Task<IActionResult> Get(int id) {
        var response = await _memberService.GetByIdAsync(id);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        if (response.Data == null)
            return NotFound(new { error = "Member bulunamadı" });

        return Ok(response.Data);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Create([FromBody] MemberDto dto) {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var response = await _memberService.CreateAsync(dto);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return CreatedAtAction(nameof(Get), new { id = response.Data!.Id }, response.Data);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Update(int id, [FromBody] MemberDto dto) {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (id != dto.Id)
            return BadRequest(new { ErrorMessage = "Member ID mismatch", ErrorCode = "VALIDATION_001" });

        var response = await _memberService.UpdateAsync(id, dto);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]  // Sadece Admin silebilir
    public async Task<IActionResult> Delete(int id) {
        var response = await _memberService.DeleteAsync(id);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return NoContent();
    }

    [HttpGet("by-email/{email}")]
    [AllowAnonymous] // Register işlemi sırasında kullanılacağı için
    public async Task<IActionResult> GetByEmail(string email) {
        try {
            var response = await _memberService.GetByEmailAsync(email);

            if (!response.IsSuccessful)
                return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

            if (response.Data == null)
                return NotFound(new { error = "Member bulunamadı" });

            return Ok(response.Data);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Member email ile alınırken hata. Email: {Email}", email);
            return StatusCode(500, new { error = "Bir hata oluştu" });
        }
    }

    [HttpPost("register")]
    [AllowAnonymous] // Kayıt işlemi için herkes erişebilmeli
    public async Task<IActionResult> RegisterMember([FromBody] MemberDto dto) {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try {
            _logger.LogInformation("Register için Member kaydı oluşturuluyor. Email: {Email}", dto.Email);

            var response = await _memberService.CreateAsync(dto);

            if (!response.IsSuccessful) {
                _logger.LogError("Member kaydı oluşturulamadı. Error: {Error}", response.Error?.ErrorMessage);
                return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
            }

            _logger.LogInformation("✅ Member kaydı başarıyla oluşturuldu. ID: {Id}, Email: {Email}",
                response.Data!.Id, dto.Email);

            return CreatedAtAction(nameof(Get), new { id = response.Data!.Id }, response.Data);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "❌ Register sırasında Member kaydı oluşturulurken exception. Email: {Email}", dto.Email);
            return StatusCode(500, new { error = "Member kaydı oluşturulamadı: " + ex.Message });
        }
    }
}