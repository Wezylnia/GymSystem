using AutoMapper;
using GymSystem.Mvc.Helpers;
using GymSystem.Mvc.Models;
using GymSystem.Mvc.Models.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymSystem.Mvc.Controllers;

[Authorize(Policy = "AdminOrGymOwner")]
public class GymLocationsController : Controller {
    private readonly ApiHelper _apiHelper;
    private readonly IMapper _mapper;
    private readonly ILogger<GymLocationsController> _logger;

    public GymLocationsController(ApiHelper apiHelper, IMapper mapper, ILogger<GymLocationsController> logger) {
        _apiHelper = apiHelper;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IActionResult> Index() {
        try {
            var apiGyms = await _apiHelper.GetListAsync<ApiGymLocationFullDto>(ApiEndpoints.GymLocations);

            // AutoMapper ile ViewModel'e map et
            var gyms = _mapper.Map<List<GymLocationViewModel>>(apiGyms);

            // GymOwner ise sadece kendi salonunu göster
            if (User.IsInRole("GymOwner")) {
                var gymLocationId = User.FindFirst("GymLocationId")?.Value;
                if (int.TryParse(gymLocationId, out var locationId)) {
                    gyms = gyms.Where(g => g.Id == locationId).ToList();
                }
            }

            return View(gyms);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Spor salonları listesi alınırken hata oluştu");
            ViewBag.ErrorMessage = "Bir hata oluştu: " + ex.Message;
            return View(new List<GymLocationViewModel>());
        }
    }

    [Authorize(Policy = "AdminOnly")]
    public IActionResult Create() {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Create(GymLocationViewModel model) {
        if (!ModelState.IsValid)
            return View(model);

        try {
            var (success, errorMessage) = await _apiHelper.PostAsync(ApiEndpoints.GymLocations, model);

            if (success) {
                TempData["SuccessMessage"] = "Spor salonu başarıyla eklendi!";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError("", $"Spor salonu eklenirken bir hata oluştu. {errorMessage}");
                return View(model);
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Spor salonu eklenirken hata oluştu");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id) {
        // GymOwner yetki kontrolü
        if (User.IsInRole("GymOwner")) {
            var gymLocationId = User.FindFirst("GymLocationId")?.Value;
            if (!int.TryParse(gymLocationId, out var locationId) || locationId != id) {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        try {
            var gym = await _apiHelper.GetAsync<ApiGymLocationFullDto>(ApiEndpoints.GymLocationById(id));

            if (gym == null) {
                TempData["ErrorMessage"] = "Spor salonu bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = _mapper.Map<GymLocationViewModel>(gym);
            return View(viewModel);
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Spor salonu detayı alınırken hata oluştu");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, GymLocationViewModel model) {
        // GymOwner yetki kontrolü
        if (User.IsInRole("GymOwner")) {
            var gymLocationId = User.FindFirst("GymLocationId")?.Value;
            if (!int.TryParse(gymLocationId, out var locationId) || locationId != id) {
                return RedirectToAction("AccessDenied", "Account");
            }
        }

        if (id != model.Id)
            return BadRequest();

        if (!ModelState.IsValid)
            return View(model);

        try {
            var (success, errorMessage) = await _apiHelper.PutAsync(ApiEndpoints.GymLocationById(id), model);

            if (success) {
                TempData["SuccessMessage"] = "Spor salonu başarıyla güncellendi!";
                return RedirectToAction(nameof(Index));
            }
            else {
                ModelState.AddModelError("", $"Spor salonu güncellenirken bir hata oluştu. {errorMessage}");
                return View(model);
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Spor salonu güncellenirken hata oluştu");
            ModelState.AddModelError("", "Bir hata oluştu: " + ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Delete(int id) {
        try {
            var (success, errorMessage) = await _apiHelper.DeleteAsync(ApiEndpoints.GymLocationById(id));

            if (success) {
                TempData["SuccessMessage"] = "Spor salonu başarıyla silindi!";
            }
            else {
                TempData["ErrorMessage"] = $"Spor salonu silinirken bir hata oluştu. {errorMessage}";
            }
        }
        catch (Exception ex) {
            _logger.LogError(ex, "Spor salonu silinirken hata oluştu");
            TempData["ErrorMessage"] = "Bir hata oluştu: " + ex.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}