using AutoMapper;
using GymSystem.Mvc.Helpers;
using GymSystem.Mvc.Models;
using GymSystem.Mvc.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Mvc.Controllers;

[Authorize]
public class MembershipRequestsController : Controller {
    private readonly ApiHelper _apiHelper;
    private readonly IMapper _mapper;
    private readonly ILogger<MembershipRequestsController> _logger;

    public MembershipRequestsController(ApiHelper apiHelper, IMapper mapper, ILogger<MembershipRequestsController> logger) {
        _apiHelper = apiHelper;
        _mapper = mapper;
        _logger = logger;
    }

    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Index() {
        try {
            var memberId = GetCurrentMemberId();
            if (memberId == null) {
                ViewBag.ErrorMessage = "Member bilgisi bulunamadı.";
                return View(new List<MembershipRequestViewModel>());
            }

            // Member bilgilerini al
            var memberInfo = await _apiHelper.GetAsync<MemberInfoDto>(ApiEndpoints.MemberById(memberId.Value));
            if (memberInfo != null) {
                var hasActiveMembership = memberInfo.MembershipEndDate.HasValue &&
                                         memberInfo.MembershipEndDate.Value > DateTime.Now;

                ViewBag.HasActiveMembership = hasActiveMembership;
                ViewBag.MembershipEndDate = memberInfo.MembershipEndDate;

                if (hasActiveMembership) {
                    ViewBag.DaysRemaining = (memberInfo.MembershipEndDate.Value - DateTime.Now).Days;
                }
            }

            // Talepleri al
            var apiRequests = await _apiHelper.GetListAsync<ApiMembershipRequestDto>(
                ApiEndpoints.MembershipRequestsByMember(memberId.Value));

            // AutoMapper ile map et
            var requests = _mapper.Map<List<MembershipRequestViewModel>>(apiRequests);

            return View(requests);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üyelik talepleri listesi alınırken hata oluştu");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<MembershipRequestViewModel>());
        }
    }

    // Member: Yeni talep oluştur
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Create() {
        try {
            var memberId = GetCurrentMemberId();
            if (memberId == null) {
                ViewBag.ErrorMessage = "Member bilgisi bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // Aktif üyelik kontrolü
            var memberInfo = await _apiHelper.GetAsync<MemberInfoDto>(ApiEndpoints.MemberById(memberId.Value));
            if (memberInfo != null && memberInfo.MembershipEndDate.HasValue &&
                memberInfo.MembershipEndDate.Value > DateTime.Now) {
                var daysRemaining = (memberInfo.MembershipEndDate.Value - DateTime.Now).Days;
                TempData["ErrorMessage"] =
                    $"Zaten aktif bir üyeliğiniz bulunmaktadır. " +
                    $"Üyeliğiniz {memberInfo.MembershipEndDate.Value:dd.MM.yyyy} tarihinde sona erecek " +
                    $"({daysRemaining} gün kaldı). " +
                    $"Mevcut üyeliğiniz bitmeden yeni talep oluşturamazsınız.";
                return RedirectToAction(nameof(Index));
            }

            var model = new CreateMembershipRequestViewModel {
                MemberId = memberId.Value
            };

            await LoadGymLocations(model);
            return View(model);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Talep oluşturma sayfası yüklenirken hata");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Create(CreateMembershipRequestViewModel model) {
        if (!ModelState.IsValid) {
            await LoadGymLocations(model);
            return View(model);
        }

        try {
            var memberId = GetCurrentMemberId();
            if (memberId == null) {
                ModelState.AddModelError("", "Member bilgisi bulunamadı.");
                await LoadGymLocations(model);
                return View(model);
            }

            var requestData = new {
                MemberId = memberId.Value,
                GymLocationId = model.GymLocationId,
                Duration = model.Duration,
                Price = model.Price,
                Notes = model.Notes
            };

            var (success, errorMessage) = await _apiHelper.PostAsync(
                ApiEndpoints.MembershipRequestsCreate, requestData);

            if (success) {
                TempData["SuccessMessage"] = "Üyelik talebiniz başarıyla oluşturuldu! Onay bekliyor.";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError("", $"Talep oluşturulurken bir hata oluştu. {errorMessage}");
            }
        }
        catch (Exception ex) {
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
    public async Task<IActionResult> Delete(int id) {
        try {
            var (success, errorMessage) = await _apiHelper.DeleteAsync(
                ApiEndpoints.MembershipRequestById(id));

            if (success) {
                TempData["SuccessMessage"] = "Talep başarıyla silindi.";
            }
            else {
                TempData["ErrorMessage"] = $"Talep silinirken bir hata oluştu. {errorMessage}";
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Talep silinirken hata. ID: {Id}", id);
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    // Admin/GymOwner: Tüm talepleri yönet
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Manage() {
        try {
            var apiRequests = await _apiHelper.GetListAsync<ApiMembershipRequestDto>(
                ApiEndpoints.MembershipRequestsPending);

            // AutoMapper ile map et
            var requests = _mapper.Map<List<MembershipRequestViewModel>>(apiRequests);

            return View(requests);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Bekleyen talepler alınırken hata");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<MembershipRequestViewModel>());
        }
    }

    // Admin/GymOwner: Talebi onayla
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Approve(int id, string? adminNotes) {
        try {
            var userId = GetCurrentUserId();
            if (userId == null) {
                TempData["ErrorMessage"] = "Kullanıcı bilgisi bulunamadı.";
                return RedirectToAction(nameof(Manage));
            }

            var requestData = new {
                UserId = userId.Value,
                AdminNotes = adminNotes
            };

            var (success, errorMessage) = await _apiHelper.PostAsync(
                ApiEndpoints.MembershipRequestApprove(id), requestData);

            if (success) {
                TempData["SuccessMessage"] = "Talep başarıyla onaylandı!";
            }
            else {
                TempData["ErrorMessage"] = $"Talep onaylanırken bir hata oluştu. {errorMessage}";
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Talep onaylanırken hata. ID: {Id}", id);
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Manage));
    }

    // Admin/GymOwner: Talebi reddet
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Reject(int id, string? adminNotes) {
        try {
            var userId = GetCurrentUserId();
            if (userId == null) {
                TempData["ErrorMessage"] = "Kullanıcı bilgisi bulunamadı.";
                return RedirectToAction(nameof(Manage));
            }

            var requestData = new {
                UserId = userId.Value,
                AdminNotes = adminNotes
            };

            var (success, errorMessage) = await _apiHelper.PostAsync(
                ApiEndpoints.MembershipRequestReject(id), requestData);

            if (success) {
                TempData["SuccessMessage"] = "Talep reddedildi.";
            }
            else {
                TempData["ErrorMessage"] = $"Talep reddedilirken bir hata oluştu. {errorMessage}";
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Talep reddedilirken hata. ID: {Id}", id);
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Manage));
    }

    #region Helpers

    private int? GetCurrentMemberId() {
        var memberIdClaim = User.FindFirst("MemberId")?.Value;
        return int.TryParse(memberIdClaim, out var memberId) ? memberId : null;
    }

    private int? GetCurrentUserId() {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    private async Task LoadGymLocations(CreateMembershipRequestViewModel model) {
        try {
            var gyms = await _apiHelper.GetListAsync<GymLocationSelectItem>(ApiEndpoints.GymLocations);

            foreach (var gym in gyms) {
                gym.OneMonthPrice = 500;
                gym.ThreeMonthsPrice = 1350; // %10 indirim
                gym.SixMonthsPrice = 2400; // %20 indirim
            }

            model.AvailableGyms = gyms;
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Salonlar yüklenirken hata");
            model.AvailableGyms = new List<GymLocationSelectItem>();
        }
    }

    #endregion
}