using GymSystem.Application.Abstractions.Contract.Member;
using GymSystem.Application.Abstractions.Services;
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
    public async Task<IActionResult> GetAll() {
        var response = await _memberService.GetAllMembersWithGymLocationAsync();

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
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id) {
        var response = await _memberService.DeleteAsync(id);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return NoContent();
    }
}