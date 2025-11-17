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

    public MembershipRequestsController(
        IMembershipRequestService membershipRequestService,
        ILogger<MembershipRequestsController> logger)
    {
        _membershipRequestService = membershipRequestService;
        _logger = logger;
    }

    /// <summary>
    /// Yeni üyelik talebi oluşturur
    /// </summary>
    [HttpPost("create")]
    [AllowAnonymous] // MVC'den internal çağrı için
    public async Task<IActionResult> CreateRequest([FromBody] CreateMembershipRequestDto request)
    {
        try
        {
            if (request.MemberId <= 0)
            {
                return BadRequest(new { error = "Geçersiz member ID." });
            }

            if (request.GymLocationId <= 0)
            {
                return BadRequest(new { error = "Geçersiz gym location ID." });
            }

            var membershipRequest = await _membershipRequestService.CreateRequestAsync(
                request.MemberId,
                request.GymLocationId,
                request.Duration,
                request.Price,
                request.Notes
            );

            return Ok(membershipRequest);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Üyelik talebi oluşturulurken hata");
            return StatusCode(500, new { error = "Talep oluşturulurken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Belirli bir üyenin tüm taleplerini getirir
    /// </summary>
    [HttpGet("member/{memberId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMemberRequests(int memberId)
    {
        try
        {
            if (memberId <= 0)
            {
                return BadRequest(new { error = "Geçersiz member ID." });
            }

            var requests = await _membershipRequestService.GetMemberRequestsAsync(memberId);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Üye talepleri getirilirken hata. Member ID: {MemberId}", memberId);
            return StatusCode(500, new { error = "Talepler getirilirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Belirli bir salonun tüm taleplerini getirir
    /// </summary>
    [HttpGet("gym/{gymLocationId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetGymLocationRequests(int gymLocationId)
    {
        try
        {
            if (gymLocationId <= 0)
            {
                return BadRequest(new { error = "Geçersiz gym location ID." });
            }

            var requests = await _membershipRequestService.GetGymLocationRequestsAsync(gymLocationId);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Salon talepleri getirilirken hata. Gym ID: {GymId}", gymLocationId);
            return StatusCode(500, new { error = "Talepler getirilirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Tüm talepleri getirir (Admin)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllRequests()
    {
        try
        {
            var requests = await _membershipRequestService.GetAllRequestsAsync();
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tüm talepler getirilirken hata");
            return StatusCode(500, new { error = "Talepler getirilirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Bekleyen talepleri getirir
    /// </summary>
    [HttpGet("pending")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPendingRequests()
    {
        try
        {
            var requests = await _membershipRequestService.GetPendingRequestsAsync();
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bekleyen talepler getirilirken hata");
            return StatusCode(500, new { error = "Talepler getirilirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Talep detayını getirir
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRequestById(int id)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "Geçersiz talep ID." });
            }

            var request = await _membershipRequestService.GetRequestByIdAsync(id);
            if (request == null)
            {
                return NotFound(new { error = "Talep bulunamadı." });
            }

            return Ok(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Talep getirilirken hata. Request ID: {RequestId}", id);
            return StatusCode(500, new { error = "Talep getirilirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Talebi onaylar
    /// </summary>
    [HttpPost("{id}/approve")]
    [AllowAnonymous]
    public async Task<IActionResult> ApproveRequest(int id, [FromBody] ApproveRejectDto dto)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "Geçersiz talep ID." });
            }

            if (dto.UserId <= 0)
            {
                return BadRequest(new { error = "Geçersiz kullanıcı ID." });
            }

            var result = await _membershipRequestService.ApproveRequestAsync(id, dto.UserId, dto.AdminNotes);
            if (!result)
            {
                return NotFound(new { error = "Talep bulunamadı." });
            }

            return Ok(new { message = "Talep onaylandı." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Talep onaylanırken hata. Request ID: {RequestId}", id);
            return StatusCode(500, new { error = "Talep onaylanırken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Talebi reddeder
    /// </summary>
    [HttpPost("{id}/reject")]
    [AllowAnonymous]
    public async Task<IActionResult> RejectRequest(int id, [FromBody] ApproveRejectDto dto)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "Geçersiz talep ID." });
            }

            if (dto.UserId <= 0)
            {
                return BadRequest(new { error = "Geçersiz kullanıcı ID." });
            }

            var result = await _membershipRequestService.RejectRequestAsync(id, dto.UserId, dto.AdminNotes);
            if (!result)
            {
                return NotFound(new { error = "Talep bulunamadı." });
            }

            return Ok(new { message = "Talep reddedildi." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Talep reddedilirken hata. Request ID: {RequestId}", id);
            return StatusCode(500, new { error = "Talep reddedilirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Talebi siler
    /// </summary>
    [HttpDelete("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> DeleteRequest(int id)
    {
        try
        {
            if (id <= 0)
            {
                return BadRequest(new { error = "Geçersiz talep ID." });
            }

            var result = await _membershipRequestService.DeleteRequestAsync(id);
            if (!result)
            {
                return NotFound(new { error = "Talep bulunamadı." });
            }

            return Ok(new { message = "Talep silindi." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Talep silinirken hata. Request ID: {RequestId}", id);
            return StatusCode(500, new { error = "Talep silinirken bir hata oluştu." });
        }
    }
}

// Request DTOs
public record CreateMembershipRequestDto(
    int MemberId,
    int GymLocationId,
    MembershipDuration Duration,
    decimal Price,
    string? Notes
);

public record ApproveRejectDto(
    int UserId,
    string? AdminNotes
);
