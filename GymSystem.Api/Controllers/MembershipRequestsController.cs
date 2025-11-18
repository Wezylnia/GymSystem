using GymSystem.Application.Abstractions.Services;
using GymSystem.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MembershipRequestsController : ControllerBase
{
    private readonly IMembershipRequestService _membershipRequestService;
    private readonly ILogger<MembershipRequestsController> _logger;

    public MembershipRequestsController(IMembershipRequestService membershipRequestService, ILogger<MembershipRequestsController> logger)
    {
        _membershipRequestService = membershipRequestService;
        _logger = logger;
    }

    [HttpPost("create")]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> CreateRequest([FromBody] CreateMembershipRequestDto request)
    {
        if (request.MemberId <= 0)
            return BadRequest(new { error = "Geçersiz member ID" });

        if (request.GymLocationId <= 0)
            return BadRequest(new { error = "Geçersiz gym location ID" });

        var response = await _membershipRequestService.CreateRequestAsync(request.MemberId, request.GymLocationId, request.Duration, request.Price, request.Notes);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage ?? "Talep oluşturulamadı" });

        return Ok(response.Data);
    }

    [HttpGet("member/{memberId}")]
    [Authorize(Roles = "Member,Admin,GymOwner")]
    public async Task<IActionResult> GetMemberRequests(int memberId)
    {
        if (memberId <= 0)
            return BadRequest(new { error = "Geçersiz member ID" });

        var response = await _membershipRequestService.GetMemberRequestsAsync(memberId);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("gym/{gymLocationId}")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> GetGymLocationRequests(int gymLocationId)
    {
        if (gymLocationId <= 0)
            return BadRequest(new { error = "Geçersiz gym location ID" });

        var response = await _membershipRequestService.GetGymLocationRequestsAsync(gymLocationId);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllRequests()
    {
        var response = await _membershipRequestService.GetAllRequestsAsync();

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("pending")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> GetPendingRequests()
    {
        var response = await _membershipRequestService.GetPendingRequestsAsync();

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        return Ok(response.Data);
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,GymOwner,Member")]
    public async Task<IActionResult> GetRequestById(int id)
    {
        if (id <= 0)
            return BadRequest(new { error = "Geçersiz talep ID" });

        var response = await _membershipRequestService.GetRequestByIdAsync(id);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, response.Error);

        if (response.Data == null)
            return NotFound(new { error = "Talep bulunamadı" });

        return Ok(response.Data);
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> ApproveRequest(int id, [FromBody] ApproveRejectDto dto)
    {
        if (id <= 0)
            return BadRequest(new { error = "Geçersiz talep ID" });

        if (dto.UserId <= 0)
            return BadRequest(new { error = "Geçersiz kullanıcı ID" });

        var response = await _membershipRequestService.ApproveRequestAsync(id, dto.UserId, dto.AdminNotes);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage ?? "Talep onaylanamadı" });

        return Ok(new { message = "Talep onaylandı" });
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> RejectRequest(int id, [FromBody] ApproveRejectDto dto)
    {
        if (id <= 0)
            return BadRequest(new { error = "Geçersiz talep ID" });

        if (dto.UserId <= 0)
            return BadRequest(new { error = "Geçersiz kullanıcı ID" });

        var response = await _membershipRequestService.RejectRequestAsync(id, dto.UserId, dto.AdminNotes);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage ?? "Talep reddedilemedi" });

        return Ok(new { message = "Talep reddedildi" });
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Member,Admin")]
    public async Task<IActionResult> DeleteRequest(int id)
    {
        if (id <= 0)
            return BadRequest(new { error = "Geçersiz talep ID" });

        var response = await _membershipRequestService.DeleteRequestAsync(id);

        if (!response.IsSuccessful)
            return StatusCode(response.Error?.StatusCode ?? 500, new { error = response.Error?.ErrorMessage ?? "Talep silinemedi" });

        return Ok(new { message = "Talep silindi" });
    }
}

public class CreateMembershipRequestDto
{
    public int MemberId { get; set; }
    public int GymLocationId { get; set; }
    public MembershipDuration Duration { get; set; }
    public decimal Price { get; set; }
    public string? Notes { get; set; }
}

public class ApproveRejectDto
{
    public int UserId { get; set; }
    public string? AdminNotes { get; set; }
}
