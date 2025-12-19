using AutoMapper;
using GymSystem.Mvc.Helpers;
using GymSystem.Mvc.Models;
using GymSystem.Mvc.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Mvc.Controllers;

[Authorize]
public class MembersController : Controller {
    private readonly ApiHelper _apiHelper;
    private readonly IMapper _mapper;
    private readonly ILogger<MembersController> _logger;

    public MembersController(ApiHelper apiHelper, IMapper mapper, ILogger<MembersController> logger) {
        _apiHelper = apiHelper;
        _mapper = mapper;
        _logger = logger;
    }

    // GET: Members
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Index() {
        try {
            List<ApiMemberDto> apiMembers;

            // GymOwner ise sadece kendi salonuna kayıtlı üyeleri göster
            if (User.IsInRole("GymOwner")) {
                var gymLocationId = GetCurrentGymLocationId();
                if (gymLocationId == null) {
                    ViewBag.ErrorMessage = "Salon bilgisi bulunamadı.";
                    return View(new List<MemberViewModel>());
                }

                apiMembers = await _apiHelper.GetListAsync<ApiMemberDto>(
                    $"{ApiEndpoints.Members}?gymLocationId={gymLocationId.Value}");

                ViewBag.IsGymOwner = true;
                ViewBag.GymLocationId = gymLocationId.Value;
            }
            else {
                // Admin ise tüm üyeleri göster
                apiMembers = await _apiHelper.GetListAsync<ApiMemberDto>(ApiEndpoints.Members);
                ViewBag.IsGymOwner = false;
            }

            // AutoMapper ile ViewModel'e map et
            var members = _mapper.Map<List<MemberViewModel>>(apiMembers);

            return View(members);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üyeler listelenirken hata oluştu");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<MemberViewModel>());
        }
    }

    // GET: Members/Create
    [Authorize(Roles = "Admin")]
    public IActionResult Create() {
        return View();
    }

    // POST: Members/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(CreateMemberViewModel model) {
        if (!ModelState.IsValid) {
            return View(model);
        }

        try {
            var (success, errorMessage) = await _apiHelper.PostAsync(ApiEndpoints.Members, model);

            if (success) {
                TempData["SuccessMessage"] = "Üye başarıyla eklendi!";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError("", $"Üye eklenirken bir hata oluştu. {errorMessage}");
                return View(model);
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üye eklenirken hata oluştu");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            return View(model);
        }
    }

    // GET: Members/Edit/5
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Edit(int id) {
        try {
            var apiMember = await _apiHelper.GetAsync<ApiMemberDto>(ApiEndpoints.MemberById(id));

            if (apiMember == null) {
                TempData["ErrorMessage"] = "Üye bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            // GymOwner ise, sadece kendi salonuna kayıtlı ve AKTİF üyeliği olan kişileri düzenleyebilir
            if (User.IsInRole("GymOwner")) {
                var gymLocationId = GetCurrentGymLocationId();
                if (gymLocationId == null || apiMember.CurrentGymLocationId != gymLocationId) {
                    TempData["ErrorMessage"] = "Bu üyeyi düzenleme yetkiniz yok.";
                    return RedirectToAction(nameof(Index));
                }

                // Aktif üyelik kontrolü
                var hasActiveMembership = apiMember.MembershipEndDate.HasValue && 
                                          apiMember.MembershipEndDate.Value > DateTime.Now;
                if (!hasActiveMembership) {
                    TempData["ErrorMessage"] = "Sadece aktif üyeliği olan kişileri düzenleyebilirsiniz.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var model = _mapper.Map<EditMemberViewModel>(apiMember);
            return View(model);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üye düzenleme sayfası yüklenirken hata. ID: {Id}", id);
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Members/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Edit(int id, EditMemberViewModel model) {
        if (id != model.Id) {
            TempData["ErrorMessage"] = "ID uyuşmazlığı.";
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid) {
            return View(model);
        }

        try {
            // GymOwner ise, sadece kendi salonuna kayıtlı ve AKTİF üyeliği olan kişileri güncelleyebilir
            if (User.IsInRole("GymOwner")) {
                var apiMember = await _apiHelper.GetAsync<ApiMemberDto>(ApiEndpoints.MemberById(id));
                var gymLocationId = GetCurrentGymLocationId();

                if (apiMember == null || gymLocationId == null || apiMember.CurrentGymLocationId != gymLocationId) {
                    TempData["ErrorMessage"] = "Bu üyeyi güncelleme yetkiniz yok.";
                    return RedirectToAction(nameof(Index));
                }

                // Aktif üyelik kontrolü
                var hasActiveMembership = apiMember.MembershipEndDate.HasValue && 
                                          apiMember.MembershipEndDate.Value > DateTime.Now;
                if (!hasActiveMembership) {
                    TempData["ErrorMessage"] = "Sadece aktif üyeliği olan kişileri güncelleyebilirsiniz.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var (success, errorMessage) = await _apiHelper.PutAsync(
                ApiEndpoints.MemberById(id), model);

            if (success) {
                TempData["SuccessMessage"] = "Üye başarıyla güncellendi!";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError("", $"Üye güncellenirken bir hata oluştu. {errorMessage}");
                return View(model);
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üye güncellenirken hata. ID: {Id}", id);
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            return View(model);
        }
    }

    // GET: Members/Details/5
    [Authorize(Roles = "Admin,GymOwner")]
    public async Task<IActionResult> Details(int id) {
        try {
            var apiMember = await _apiHelper.GetAsync<ApiMemberDto>(ApiEndpoints.MemberById(id));

            if (apiMember == null) {
                TempData["ErrorMessage"] = "Üye bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var model = _mapper.Map<MemberViewModel>(apiMember);
            return View(model);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üye detayları yüklenirken hata. ID: {Id}", id);
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    // POST: Members/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]  // Sadece Admin silebilir
    public async Task<IActionResult> Delete(int id) {
        try {
            var (success, errorMessage) = await _apiHelper.DeleteAsync(
                ApiEndpoints.MemberById(id));

            if (success) {
                TempData["SuccessMessage"] = "Üye ve ilişkili tüm kayıtlar başarıyla silindi!";
            }
            else {
                TempData["ErrorMessage"] = $"Üye silinirken bir hata oluştu. {errorMessage}";
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Üye silinirken hata. ID: {Id}", id);
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    #region Helpers

    private int? GetCurrentGymLocationId() {
        var gymLocationIdClaim = User.FindFirst("GymLocationId")?.Value;
        return int.TryParse(gymLocationIdClaim, out var gymLocationId) ? gymLocationId : null;
    }

    #endregion
}