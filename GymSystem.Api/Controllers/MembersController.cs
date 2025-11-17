using GymSystem.Application.Abstractions.Services;
using GymSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembersController : ControllerBase
{
    private readonly IMemberService _memberService;
    private readonly ILogger<MembersController> _logger;

    public MembersController(IMemberService memberService, ILogger<MembersController> logger)
    {
        _memberService = memberService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        // GymLocation ile birlikte getir
        var response = await _memberService.GetAllMembersWithGymLocationAsync();
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var response = await _memberService.GetByIdAsync(id);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return Ok(response.Data);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateMemberDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var member = new Member
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            MembershipStartDate = dto.MembershipStartDate,
            MembershipEndDate = dto.MembershipEndDate,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        var response = await _memberService.CreateAsync(member);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return CreatedAtAction(nameof(Get), new { id = response.Data!.Id }, response.Data);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateMemberDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        if (id != dto.Id)
        {
            return BadRequest(new { ErrorMessage = "Member ID mismatch", ErrorCode = "VALIDATION_001" });
        }

        var existingResponse = await _memberService.GetByIdAsync(id);
        if (!existingResponse.IsSuccessful || existingResponse.Data == null)
        {
            return NotFound(new { ErrorMessage = $"Member with ID {id} not found", ErrorCode = "NOT_FOUND_001" });
        }

        var member = existingResponse.Data;
        member.FirstName = dto.FirstName;
        member.LastName = dto.LastName;
        member.Email = dto.Email;
        member.PhoneNumber = dto.PhoneNumber;
        member.MembershipStartDate = dto.MembershipStartDate;
        member.MembershipEndDate = dto.MembershipEndDate;
        member.UpdatedAt = DateTime.Now;

        var response = await _memberService.UpdateAsync(id, member);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var response = await _memberService.DeleteAsync(id);
        
        if (!response.IsSuccessful)
        {
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);
        }

        return NoContent();
    }
}

// DTOs
public class CreateMemberDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime MembershipStartDate { get; set; } = DateTime.Now;
    public DateTime? MembershipEndDate { get; set; }
}

public class UpdateMemberDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime MembershipStartDate { get; set; }
    public DateTime? MembershipEndDate { get; set; }
}