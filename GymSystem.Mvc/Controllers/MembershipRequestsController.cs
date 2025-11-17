using GymSystem.Domain.Entities;
using GymSystem.Mvc.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace GymSystem.Mvc.Controllers;

[Authorize]
public class MembershipRequestsController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MembershipRequestsController> _logger;

    public MembershipRequestsController(
        IHttpClientFactory httpClientFactory,
        ILogger<MembershipRequestsController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    // Member: Kendi taleplerini görüntüle
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Index()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var memberId = GetCurrentMemberId();

            if (memberId == null)
            {
                ViewBag.ErrorMessage = "Member bilgisi bulunamadı.";
                return View(new List<MembershipRequestViewModel>());
            }

            // Member bilgilerini al
            var memberResponse = await client.GetAsync($"/api/members/{memberId}");
            if (memberResponse.IsSuccessStatusCode)
            {
                var memberContent = await memberResponse.Content.ReadAsStringAsync();
                var memberInfo = JsonSerializer.Deserialize<MemberInfoDto>(memberContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (memberInfo != null)
                {
                    var hasActiveMembership = memberInfo.MembershipEndDate.HasValue && 
                                             memberInfo.MembershipEndDate.Value > DateTime.Now;
                    
                    ViewBag.HasActiveMembership = hasActiveMembership;
                    ViewBag.MembershipEndDate = memberInfo.MembershipEndDate;
                    
                    if (hasActiveMembership)
                    {
                        ViewBag.DaysRemaining = (memberInfo.MembershipEndDate.Value - DateTime.Now).Days;
                    }
                }
            }

            var response = await client.GetAsync($"/api/membershiprequests/member/{memberId}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var requests = JsonSerializer.Deserialize<List<MembershipRequestViewModel>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) 
                    ?? new List<MembershipRequestViewModel>();

                // Member bilgilerini map et
                foreach (var request in requests)
                {
                    if (request.Member != null)
                    {
                        request.MemberName = $"{request.Member.FirstName} {request.Member.LastName}";
                    }
                    if (request.GymLocation != null)
                    {
                        request.GymLocationName = request.GymLocation.Name;
                        request.GymLocationAddress = request.GymLocation.Address;
                    }
                }

                return View(requests);
            }
            else
            {
                ViewBag.ErrorMessage = "Talepler yüklenirken bir hata oluştu.";
                return View(new List<MembershipRequestViewModel>());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Üyelik talepleri listesi alınırken hata oluştu");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<MembershipRequestViewModel>());
        }
    }

    // Member: Yeni talep oluştur
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Create()
    {
        try
        {
            var memberId = GetCurrentMemberId();
            if (memberId == null)
            {
                ViewBag.ErrorMessage = "Member bilgisi bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // Member bilgilerini ve aktif üyelik durumunu kontrol et
            var client = _httpClientFactory.CreateClient("GymApi");
            var memberResponse = await client.GetAsync($"/api/members/{memberId}");

            if (memberResponse.IsSuccessStatusCode)
            {
                var memberContent = await memberResponse.Content.ReadAsStringAsync();
                var memberInfo = JsonSerializer.Deserialize<MemberInfoDto>(memberContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Aktif üyelik kontrolü
                if (memberInfo != null && memberInfo.MembershipEndDate.HasValue && 
                    memberInfo.MembershipEndDate.Value > DateTime.Now)
                {
                    var daysRemaining = (memberInfo.MembershipEndDate.Value - DateTime.Now).Days;
                    TempData["ErrorMessage"] = 
                        $"Zaten aktif bir üyeliğiniz bulunmaktadır. " +
                        $"Üyeliğiniz {memberInfo.MembershipEndDate.Value:dd.MM.yyyy} tarihinde sona erecek " +
                        $"({daysRemaining} gün kaldı). " +
                        $"Mevcut üyeliğiniz bitmeden yeni talep oluşturamazsınız.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var model = new CreateMembershipRequestViewModel
            {
                MemberId = memberId.Value
            };

            // Salonları yükle
            await LoadGymLocations(model);

            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Talep oluşturma sayfası yüklenirken hata");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Create(CreateMembershipRequestViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadGymLocations(model);
            return View(model);
        }

        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var memberId = GetCurrentMemberId();

            if (memberId == null)
            {
                ModelState.AddModelError("", "Member bilgisi bulunamadı.");
                await LoadGymLocations(model);
                return View(model);
            }

            var requestData = new
            {
                MemberId = memberId.Value,
                GymLocationId = model.GymLocationId,
                Duration = model.Duration,
                Price = model.Price,
                Notes = model.Notes
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("/api/membershiprequests/create", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Üyelik talebiniz başarıyla oluşturuldu! Onay bekliyor.";
                return RedirectToAction(nameof(Index));
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                try
                {
                    var errorObj = JsonSerializer.Deserialize<JsonElement>(errorContent);
                    if (errorObj.TryGetProperty("error", out var errorMsg))
                    {
                        ModelState.AddModelError("", errorMsg.GetString() ?? "Talep oluşturulurken bir hata oluştu.");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Talep oluşturulurken bir hata oluştu.");
                    }
                }
                catch
                {
                    ModelState.AddModelError("", "Talep oluşturulurken bir hata oluştu.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Üyelik talebi oluşturulurken hata");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
        }

        await LoadGymLocations(model);
        return View(model);
    }

    // Member: Talep sil (sadece Pending)
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.DeleteAsync($"/api/membershiprequests/{id}");

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Talep başarıyla silindi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Talep silinirken bir hata oluştu.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Talep silinirken hata. ID: {Id}", id);
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // Admin/GymOwner: Tüm talepleri yönet
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Manage()
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.GetAsync("/api/membershiprequests/pending");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var requests = JsonSerializer.Deserialize<List<MembershipRequestViewModel>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<MembershipRequestViewModel>();

                // Navigation properties'i map et
                foreach (var request in requests)
                {
                    if (request.Member != null)
                    {
                        request.MemberName = $"{request.Member.FirstName} {request.Member.LastName}";
                    }
                    if (request.GymLocation != null)
                    {
                        request.GymLocationName = request.GymLocation.Name;
                        request.GymLocationAddress = request.GymLocation.Address;
                    }
                }

                return View(requests);
            }
            else
            {
                ViewBag.ErrorMessage = "Talepler yüklenirken bir hata oluştu.";
                return View(new List<MembershipRequestViewModel>());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bekleyen talepler alınırken hata");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<MembershipRequestViewModel>());
        }
    }

    // Admin/GymOwner: Talebi onayla
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Approve(int id, string? adminNotes)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bilgisi bulunamadı.";
                return RedirectToAction(nameof(Manage));
            }

            var requestData = new
            {
                UserId = userId.Value,
                AdminNotes = adminNotes
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync($"/api/membershiprequests/{id}/approve", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Talep başarıyla onaylandı!";
            }
            else
            {
                TempData["ErrorMessage"] = "Talep onaylanırken bir hata oluştu.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Talep onaylanırken hata. ID: {Id}", id);
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Manage));
    }

    // Admin/GymOwner: Talebi reddet
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Reject(int id, string? adminNotes)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var userId = GetCurrentUserId();

            if (userId == null)
            {
                TempData["ErrorMessage"] = "Kullanıcı bilgisi bulunamadı.";
                return RedirectToAction(nameof(Manage));
            }

            var requestData = new
            {
                UserId = userId.Value,
                AdminNotes = adminNotes
            };

            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestData),
                Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync($"/api/membershiprequests/{id}/reject", jsonContent);

            if (response.IsSuccessStatusCode)
            {
                TempData["SuccessMessage"] = "Talep reddedildi.";
            }
            else
            {
                TempData["ErrorMessage"] = "Talep reddedilirken bir hata oluştu.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Talep reddedilirken hata. ID: {Id}", id);
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Manage));
    }

    // Helper: Mevcut MemberId'yi al
    private int? GetCurrentMemberId()
    {
        var memberIdClaim = User.FindFirst("MemberId")?.Value;
        if (int.TryParse(memberIdClaim, out var memberId))
        {
            return memberId;
        }
        return null;
    }

    // Helper: Mevcut UserId'yi al
    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    // Helper: Salonları yükle
    private async Task LoadGymLocations(CreateMembershipRequestViewModel model)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GymApi");
            var response = await client.GetAsync("/api/gymlocations");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var gyms = JsonSerializer.Deserialize<List<GymLocationSelectItem>>(content,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? new List<GymLocationSelectItem>();

                // Fiyatları sabit tut (gerçek senaryoda database'den gelir)
                foreach (var gym in gyms)
                {
                    gym.OneMonthPrice = 500;
                    gym.ThreeMonthsPrice = 1350; // %10 indirim
                    gym.SixMonthsPrice = 2400; // %20 indirim
                }

                model.AvailableGyms = gyms;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Salonlar yüklenirken hata");
            model.AvailableGyms = new List<GymLocationSelectItem>();
        }
    }
}

// Helper DTO for Member Info
public class MemberInfoDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime MembershipStartDate { get; set; }
    public DateTime? MembershipEndDate { get; set; }
    public bool IsActive { get; set; }
}